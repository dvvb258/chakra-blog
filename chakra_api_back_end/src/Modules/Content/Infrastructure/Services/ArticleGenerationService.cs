using Content.Application.Common;
using Content.Domain.Common;
using Content.Domain.Entities;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReverseMarkdown;
using SharedKernel.Constants;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Content.Infrastructure.Services;

/// <summary>
/// 文章生成服务（使用 AI 模型生成文章内容）
/// </summary>
public class ArticleGenerationService : IArticleGenerationService
{
    private readonly BlogDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatClient _chatClient;
    private readonly IConnectionMultiplexer? _redis;
    private readonly AISettings _aiSettings;
    private readonly ILogger<ArticleGenerationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleGenerationService"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="httpClientFactory">HTTP 客户端工厂</param>
    /// <param name="chatClient">统一聊天客户端（基于 Microsoft.Extensions.AI 抽象）</param>
    /// <param name="aiOptions">AI 服务配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="redis">可选 Redis 客户端连接</param>
    public ArticleGenerationService(
        BlogDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IChatClient chatClient,
        IOptions<AISettings> aiOptions,
        ILogger<ArticleGenerationService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _chatClient = chatClient;
        _redis = redis;
        _aiSettings = aiOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> GenerateArticleAsync(CancellationToken cancellationToken = default)
    {
        // 1. 并行读取所有 RSS 源，收集文章标题和链接
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ArticleBot/1.0)");

        // (Title, Link, EmbeddedHtml, PublishDate)
        static IEnumerable<(string Title, string Link, string EmbeddedHtml, DateTimeOffset PublishDate)> ParseFeed(string xml)
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader);
            var feed = SyndicationFeed.Load(xmlReader);
            foreach (var item in feed.Items)
            {
                var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(link)) continue;

                var embeddedHtml =
                    item.ElementExtensions
                        .ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/")
                        .FirstOrDefault()
                    ?? (item.Content as TextSyndicationContent)?.Text
                    ?? item.Summary?.Text
                    ?? string.Empty;

                yield return (item.Title.Text, link, embeddedHtml, item.PublishDate);
            }
        }

        var rssSources = (_aiSettings.RssSources != null && _aiSettings.RssSources.Count > 0)
            ? _aiSettings.RssSources
            : ["https://devblogs.microsoft.com/dotnet/feed/"];

        var fetchTasks = rssSources.Select(async rssUrl =>
        {
            try
            {
                var xml = await httpClient.GetStringAsync(rssUrl, cancellationToken);
                return ParseFeed(xml).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取或解析 RSS 源失败: {RssUrl}", rssUrl);
                return [];
            }
        });

        var allItems = (await Task.WhenAll(fetchTasks)).SelectMany(x => x).ToList();

        if (allItems.Count == 0) return false;

        // 过滤 1：去除 2 个月前的旧文章
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-2);
        var candidates = allItems.Where(x => x.PublishDate >= cutoff).ToList();
        if (candidates.Count == 0) return false;

        // 过滤 2：打乱后依次选取，跳过已存在的原文链接；使用 Redis 原子认领锁（若开启）防止多实例并发冲突
        candidates = [.. candidates.OrderBy(_ => Random.Shared.Next())];
        (string Title, string Link, string EmbeddedHtml, DateTimeOffset PublishDate)? selected = null;
        string? selectedClaimLockKey = null;
        var redisDb = _redis?.GetDatabase();

        foreach (var candidate in candidates)
        {
            // 2.1 检查数据库中是否已存在该链接
            if (await _dbContext.Articles.AnyAsync(a => a.SourceLink == candidate.Link, cancellationToken))
            {
                continue;
            }

            // 2.2 Redis 尝试原子抢占文章源认领锁（若启用了 Redis，30 分钟过期，防止多 Pod 极端并发时挑中同一个文章源）
            if (redisDb != null)
            {
                var linkHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidate.Link)));
                var claimKey = RedisKeys.ArticleSourceLinkLock(linkHash);

                var claimed = await redisDb.StringSetAsync(claimKey, Environment.MachineName, TimeSpan.FromMinutes(30), When.NotExists);
                if (!claimed)
                {
                    _logger.LogInformation("文章源 {Link} 正由其他生成任务处理中，跳过并尝试下一个候选源。", candidate.Link);
                    continue;
                }

                selectedClaimLockKey = claimKey;
            }

            selected = candidate;
            break;
        }

        if (selected is null) return false;

        bool isArticlePersisted = false;

        try
        {
            // 2. 获取文章 HTML
            //    优先使用 RSS 内嵌内容（仅文章正文），避免拉取整页 HTML 引入导航/侧边栏等噪音
            string html;
            if (!string.IsNullOrWhiteSpace(selected.Value.EmbeddedHtml))
            {
                html = selected.Value.EmbeddedHtml;
            }
            else
            {
                try
                {
                    html = await httpClient.GetStringAsync(selected.Value.Link, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取文章原文失败: {Link}", selected.Value.Link);
                    return false;
                }
            }

            var converter = new Converter();
            var markdown = MarkdownImageRegex.Replace(converter.Convert(html), string.Empty);

            // 3. 发送给 AI 生成新文章（结尾附引用原文链接）
            var generatePrompt = $"""
                请根据以下原文内容，用中文撰写一篇新的技术博客文章。
                要求：
                - 用中文重新组织和改写内容，不能直接翻译或复制
                - 保留核心技术知识点，语言通俗易懂
                - 文章结构清晰，包含标题、正文段落
                - 文章最后添加一行：> 参考原文：{selected.Value.Link}
                - 只返回 Markdown 格式的文章内容，不要包含额外说明
                - 直接返回Markdown的内容 不需用``` markdown 等包裹

                原文内容：
                {markdown[..Math.Min(markdown.Length, 10000)]}
                """;

            var instructions = "你是一个专业的技术文章写作助手，擅长将英文技术文章改写为高质量的中文技术博客。";
            var generatedContent = string.Empty;
            try
            {
                IList<ChatMessage> metaHistory =
                [
                    new(ChatRole.System, instructions),
                    new(ChatRole.User, generatePrompt),
                ];

                var metaResult = await _chatClient.GetResponseAsync(metaHistory, cancellationToken: cancellationToken);
                generatedContent = metaResult.Text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成文章正文, 调用 AI 异常");
            }

            if (string.IsNullOrWhiteSpace(generatedContent))
                return false;

            // 4. 从数据库查询所有可选的分类和专栏列表，提供给 LLM 进行精准匹配决策
            var availableCategories = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Sort)
                .Select(c => new { c.Id, c.Name, c.Description })
                .ToListAsync(cancellationToken);

            var availableSeries = await _dbContext.Series
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Name, s.Description })
                .ToListAsync(cancellationToken);

            var categoriesPromptText = availableCategories.Count > 0
                ? string.Join("\n", availableCategories.Select(c => $"- 分类ID: {c.Id} | 名称: \"{c.Name}\" | 说明: {c.Description}"))
                : "- 暂无预设分类";

            var seriesPromptText = availableSeries.Count > 0
                ? string.Join("\n", availableSeries.Select(s => $"- 专栏ID: {s.Id} | 名称: \"{s.Name}\" | 说明: {s.Description}"))
                : "- 暂无专栏系列";

            // 5. 提示 LLM 提取元数据（由 LLM 自主挑选最匹配的 categoryId 和 seriesId）
            var metaPrompt = $$"""
                分析以下文章内容，提取或生成以下元数据，以纯 JSON 对象格式返回，不要有任何多余文字或包裹符号：
                {
                  "title": "精炼准确的中文技术文章标题（30字以内）",
                  "tags": ["标签1", "标签2"],
                  "summary": "100字以内的中文摘要",
                  "categoryId": 1,
                  "seriesId": null,
                  "coverColor": "linear-gradient(135deg, #6D28D9 0%, #818CF8 100%)"
                }

                注意：不要用```json ... ```等代码块包裹返回的 JSON，直接返回纯 JSON 字符串。

                字段规则：
                - title：根据文章正文拟定一个精炼、专业且吸引人的中文标题，严禁直接返回英文原标题
                - tags：文章涉及的核心技术名称，最多 3 个（如 ".NET"、"C#"、"AI" 等英文技术词）
                - summary：100 字以内的中文摘要
                - categoryId：请从以下【可选分类列表】中，根据文章内容选择最匹配的一个分类 ID（必须返回列表中实际存在的整数 ID）：
                {{categoriesPromptText}}

                - seriesId：请从以下【可选专栏系列列表】中，判断文章是否适合归入某个专栏系列。如果高度匹配则返回对应的专栏整数 ID；如果不适合归入任何专栏系列，请务必返回 null：
                {{seriesPromptText}}

                - coverColor：根据文章主题选择一个 CSS 渐变色封面，格式固定为 linear-gradient(135deg, #XXXXXX 0%, #XXXXXX 100%)
                  可选范围（不限于此，可自由发挥）：
                  linear-gradient(135deg, #6D28D9 0%, #818CF8 100%)
                  linear-gradient(135deg, #0284C7 0%, #06B6D4 100%)
                  linear-gradient(135deg, #3730A3 0%, #6366F1 100%)
                  linear-gradient(135deg, #0D9488 0%, #2DD4BF 100%)

                文章内容：
                {{generatedContent[..Math.Min(generatedContent.Length, 3000)]}}
                """;

            var instructionsExtract = "你是一个文章元数据提取助手，只返回符合要求的 JSON，不包含任何其他内容。";
            var metaJson = string.Empty;
            try
            {
                IList<ChatMessage> metaHistory =
                [
                    new(ChatRole.System, instructionsExtract),
                    new(ChatRole.User, metaPrompt),
                ];

                var metaResult = await _chatClient.GetResponseAsync(metaHistory, cancellationToken: cancellationToken);
                metaJson = metaResult.Text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取文章元数据, 调用 AI 异常");
            }

            if (metaJson.StartsWith("```"))
            {
                var start = metaJson.IndexOf('{');
                var end = metaJson.LastIndexOf('}');
                metaJson = start >= 0 && end > start ? metaJson[start..(end + 1)] : metaJson;
            }

            ArticleMetadata? meta = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(metaJson))
                {
                    meta = JsonSerializer.Deserialize<ArticleMetadata>(metaJson, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "文章信息 JSON 解析异常，将使用默认元数据兜底保存文章内容。原始内容: {RawJson}", metaJson);
            }

            var title = !string.IsNullOrWhiteSpace(meta?.Title) ? meta.Title.Trim() : selected.Value.Title;
            var tags = meta?.Tags ?? [];
            var summary = !string.IsNullOrWhiteSpace(meta?.Summary)
                ? meta.Summary
                : (generatedContent.Length > 150 ? generatedContent[..150] + "..." : generatedContent);
            var coverColor = !string.IsNullOrWhiteSpace(meta?.CoverColor)
                ? meta.CoverColor
                : "linear-gradient(135deg, #6D28D9 0%, #818CF8 100%)";

            // 6. 验证 LLM 选出的 categoryId 和 seriesId，存在则采用，不存在则安全兜底
            long categoryId;
            if (meta?.CategoryId.HasValue == true && availableCategories.Any(c => c.Id == meta.CategoryId.Value))
            {
                categoryId = meta.CategoryId.Value;
            }
            else
            {
                // 兜底：选取首个可用分类 ID，或 1
                categoryId = availableCategories.FirstOrDefault()?.Id ?? 1;
            }

            long? seriesId = null;
            if (meta?.SeriesId.HasValue == true && availableSeries.Any(s => s.Id == meta.SeriesId.Value))
            {
                seriesId = meta.SeriesId.Value;
            }

            // 7. 阅读时长（统一按每分钟 350 字估算）
            var readingTime = Article.CalculateReadingTime(generatedContent);

            // 8. 创建文章实体并写入数据库（后台 AI 定时生成文章默认归属 System 账号且自动审核发布）
            var article = new Article(
                title: title,
                content: generatedContent,
                summary: summary,
                tags: tags,
                categoryId: categoryId,
                author: "AI生成",
                readingTime: readingTime,
                coverColor: coverColor,
                sourceLink: selected.Value.Link,
                createdByUserId: 0,
                status: Content.Domain.Enums.ArticleStatus.Published);

            if (seriesId.HasValue)
            {
                article.AddToSeries(seriesId.Value);
            }

            await _dbContext.Articles.AddAsync(article, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            isArticlePersisted = true;
            return true;
        }
        catch (DbUpdateException ex) when (IsSourceLinkDuplicateException(ex))
        {
            _logger.LogWarning("文章源 {Link} 已被其他实例先一步入库保存，本次生成跳过（捕获到唯一键冲突）。", selected.Value.Link);
            return false;
        }
        finally
        {
            // 如果未成功持久化，主动释放认领锁，允许后续调度重新尝试
            if (!isArticlePersisted && selectedClaimLockKey != null && redisDb != null)
            {
                try
                {
                    await redisDb.KeyDeleteAsync(selectedClaimLockKey);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "释放文章源认领锁异常。");
                }
            }
        }
    }

    /// <summary>
    /// 判断是否是文章源链接 source_link 的唯一索引冲突异常
    /// </summary>
    private static bool IsSourceLinkDuplicateException(DbUpdateException ex)
    {
        var message = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return message.Contains("ix_articles_source_link") || message.Contains("23505");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // 匹配 Markdown 图片语法：![alt](url)
    private static readonly Regex MarkdownImageRegex = new(@"!\[[^\]]*\]\([^)]*\)", RegexOptions.Compiled);

    private sealed class ArticleMetadata
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("categoryId")]
        public long? CategoryId { get; set; }

        [JsonPropertyName("seriesId")]
        public long? SeriesId { get; set; }

        [JsonPropertyName("coverColor")]
        public string CoverColor { get; set; } = string.Empty;
    }
}
