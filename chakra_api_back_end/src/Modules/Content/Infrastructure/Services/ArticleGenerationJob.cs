using Content.Application.Common;
using Content.Domain.Common;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Common;
using SharedKernel.Constants;
using StackExchange.Redis;

namespace Content.Infrastructure.Services;

/// <summary>
/// Hangfire 定时任务：尝试生成一篇 AI 文章，每天最多成功生成 2 篇。
/// 当配置 Redis 时使用 Redis 分布式互斥锁确保多 Pod 仅单实例执行；未开启 Redis 时自动使用单机互斥锁降级。
/// 仅当 AISettings.AutoGenerateEnabled 为 true 时才实际执行。
/// </summary>
public class ArticleGenerationJob(
    IArticleGenerationService articleGenerationService,
    IOptions<AISettings> aiOptions,
    ILogger<ArticleGenerationJob> logger,
    IConnectionMultiplexer? redis = null)
{
    private static readonly TimeSpan LockLease = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockRenewInterval = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim LocalLock = new(1, 1);
    private static int _localTodayCount = 0;
    private static DateTime _localCountDate = DateTime.MinValue;

    /// <summary>
    /// 执行文章生成任务。不配置自动重试，异常时等待下一小时的调度。
    /// </summary>
    /// <param name="cancellationToken">任务取消令牌</param>
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 0. 开关检查：默认不开启
        if (!aiOptions.Value.AutoGenerateEnabled)
        {
            logger.LogInformation("AI 定时自动撰写文章功能未开启 (AutoGenerateEnabled = false)，跳过执行。");
            return;
        }

        if (redis != null)
        {
            await ExecuteWithRedisLockAsync(redis, cancellationToken);
        }
        else
        {
            await ExecuteWithLocalLockAsync(cancellationToken);
        }
    }

    private async Task ExecuteWithRedisLockAsync(IConnectionMultiplexer redisClient, CancellationToken cancellationToken)
    {
        IDatabase? db = null;
        var lockKey = (RedisKey)RedisKeys.ArticleGenerationExecutionLock;
        var lockToken = (RedisValue)Guid.NewGuid().ToString("N");
        bool lockAcquired = false;
        CancellationTokenSource? executionCts = null;
        Task? renewalTask = null;

        try
        {
            db = redisClient.GetDatabase();

            // 1. 获取全局分布式互斥锁（租期 15 分钟，防止多 Pod 在整点并发同时执行生成任务）
            lockAcquired = await db.LockTakeAsync(lockKey, lockToken, LockLease);
            if (!lockAcquired)
            {
                logger.LogInformation("已有其他 Pod 实例正在执行文章生成任务，本实例跳过本次调度。");
                return;
            }

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelper.AppTimeZone);
            var countKey = RedisKeys.ArticleGenerationCount(localNow);

            // 2. 检查今日已生成篇数
            var currentCountVal = await db.StringGetAsync(countKey);
            long currentCount = currentCountVal.HasValue && long.TryParse((string?)currentCountVal, out var c) ? c : 0;

            if (currentCount >= 2)
            {
                logger.LogInformation("今日文章已达上限（2 篇），跳过本次执行。");
                return;
            }

            logger.LogInformation("已获取分布式执行锁，开始生成今日第 {Slot} 篇文章。", currentCount + 1);

            executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            renewalTask = RenewLockAsync(db, lockKey, lockToken, executionCts);

            bool success;
            try
            {
                success = await articleGenerationService.GenerateArticleAsync(executionCts.Token);
            }
            catch (OperationCanceledException) when (executionCts.IsCancellationRequested)
            {
                logger.LogWarning("文章生成任务被取消或分布式锁已丢失，停止本次执行。");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "文章生成过程发生异常，等待下次调度。");
                return;
            }

            if (!success)
            {
                logger.LogInformation("文章生成未成功，等待下次调度。");
                return;
            }

            // 3. 只有成功生成并入库后，才原子递增今日成功配额
            var slot = await db.StringIncrementAsync(countKey);
            if (slot == 1)
            {
                var midnight = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.AddDays(1), TimeZoneHelper.AppTimeZone);
                await db.KeyExpireAsync(countKey, midnight);
            }

            logger.LogWarning("文章生成成功，今日已成功生成 {Slot} 篇。", slot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "文章生成调度发生未捕获异常。");
        }
        finally
        {
            if (executionCts != null)
            {
                await executionCts.CancelAsync();
            }

            if (renewalTask != null)
            {
                try
                {
                    await renewalTask;
                }
                catch (OperationCanceledException)
                {
                    // 正常停止续租循环
                }
            }

            executionCts?.Dispose();

            // 4. 释放分布式互斥锁
            if (lockAcquired && db != null)
            {
                try
                {
                    await db.LockReleaseAsync(lockKey, lockToken);
                }
                catch (Exception releaseEx)
                {
                    logger.LogWarning(releaseEx, "释放文章生成任务分布式锁发生异常。");
                }
            }
        }
    }

    private async Task ExecuteWithLocalLockAsync(CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelper.AppTimeZone);
        if (_localCountDate.Date != localNow.Date)
        {
            _localCountDate = localNow.Date;
            _localTodayCount = 0;
        }

        if (_localTodayCount >= 2)
        {
            logger.LogInformation("单机模式下今日已生成达上限（2 篇），跳过本次执行。");
            return;
        }

        var acquired = await LocalLock.WaitAsync(TimeSpan.Zero, cancellationToken);
        if (!acquired)
        {
            logger.LogInformation("单机已有生成任务在执行中，跳过本次调度。");
            return;
        }

        try
        {
            logger.LogInformation("已获取单机进程内执行锁，开始尝试生成今日第 {Slot} 篇文章。", _localTodayCount + 1);
            var success = await articleGenerationService.GenerateArticleAsync(cancellationToken);
            if (success)
            {
                _localTodayCount++;
                logger.LogInformation("单机文章生成成功，今日生成计数为: {Count}", _localTodayCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "单机文章生成调度发生异常。");
        }
        finally
        {
            LocalLock.Release();
        }
    }

    private async Task RenewLockAsync(
        IDatabase db,
        RedisKey lockKey,
        RedisValue lockToken,
        CancellationTokenSource executionCts)
    {
        while (!executionCts.IsCancellationRequested)
        {
            await Task.Delay(LockRenewInterval, executionCts.Token);
            if (!await db.LockExtendAsync(lockKey, lockToken, LockLease))
            {
                logger.LogError("文章生成任务分布式锁续租失败，取消当前生成流程。");
                await executionCts.CancelAsync();
                return;
            }
        }
    }
}
