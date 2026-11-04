using Content.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Exceptions;
using SharedKernel.Domain;

namespace Content.Infrastructure.Services;

/// <summary>
/// 本地磁盘静态文件存储服务实现（开箱即用，零外部云存储依赖）
/// </summary>
public sealed class LocalStorageService : IStorageService
{
    private readonly LocalStorageSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
    /// </summary>
    /// <param name="options">本地存储配置选项</param>
    /// <param name="environment">Web 宿主环境</param>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    /// <param name="logger">日志记录器</param>
    public LocalStorageService(
        IOptions<LocalStorageSettings> options,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalStorageService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // 1. 确定物理保存根路径
        var uploadDir = string.IsNullOrWhiteSpace(_settings.UploadDir) ? "uploads" : _settings.UploadDir.Trim();
        var physicalBasePath = Path.IsPathRooted(uploadDir)
            ? uploadDir
            : Path.Combine(_environment.ContentRootPath, uploadDir);

        // 按年月建立归档子目录（例如 uploads/2026/03/）
        var now = DateTime.UtcNow;
        var relativeSubDir = Path.Combine(now.ToString("yyyy"), now.ToString("MM"));
        var physicalTargetDir = Path.Combine(physicalBasePath, relativeSubDir);

        try
        {
            if (!Directory.Exists(physicalTargetDir))
            {
                Directory.CreateDirectory(physicalTargetDir);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "创建本地存储子目录无写入权限: {Path}", physicalTargetDir);
            throw new BusinessException($"本地存储目录 [{physicalTargetDir}] 无写入权限，请检查容器挂载目录的用户权限（可尝试在 docker-compose 中为后端服务设置 user: '0:0' 或执行 chmod 777）。", 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建本地存储子目录失败: {Path}", physicalTargetDir);
            throw new BusinessException($"创建本地存储目录失败: {ex.Message}", 500);
        }

        var uniqueId = SnowflakeIdGenerator.NextId().ToString();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".jpg";
        }

        var finalFileName = $"{uniqueId}{ext}";
        var physicalFilePath = Path.Combine(physicalTargetDir, finalFileName);

        // 2. 原生高效安全落盘（由展示端 Next.js 自动进行 WebP/AVIF 动态自适应压缩转码）
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        try
        {
            await using (var fileStream = new FileStream(physicalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "写入本地媒体文件无写入权限: {Path}", physicalFilePath);
            throw new BusinessException($"本地存储文件 [{physicalFilePath}] 写入失败，存储目录权限不足。", 500);
        }

        _logger.LogInformation("文件已成功保存至本地存储: {Path}", physicalFilePath);

        // 3. 构建公开访问 URL：若显式配置了 PublicUrl 则拼接前缀，否则直接返回标准根相对路径（如 /uploads/2026/10/xxx.jpg）
        // 相对路径天然绑定当前站点，彻底解耦主机名与端口，换域名、换端口、加 HTTPS 绝不产生死链
        var requestPath = string.IsNullOrWhiteSpace(_settings.RequestPath) ? "/uploads" : _settings.RequestPath.Trim();
        if (!requestPath.StartsWith('/')) requestPath = "/" + requestPath;
        requestPath = requestPath.TrimEnd('/');

        var urlSubPath = $"{requestPath}/{now:yyyy}/{now:MM}/{finalFileName}";

        if (!string.IsNullOrWhiteSpace(_settings.PublicUrl))
        {
            return $"{_settings.PublicUrl.TrimEnd('/')}{urlSubPath}";
        }

        return urlSubPath;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        try
        {
            var uploadDir = string.IsNullOrWhiteSpace(_settings.UploadDir) ? "uploads" : _settings.UploadDir.Trim();
            var physicalBasePath = Path.IsPathRooted(uploadDir)
                ? uploadDir
                : Path.Combine(_environment.ContentRootPath, uploadDir);

            var requestPath = string.IsNullOrWhiteSpace(_settings.RequestPath) ? "/uploads" : _settings.RequestPath.Trim();
            if (!requestPath.StartsWith('/')) requestPath = "/" + requestPath;

            var pathOnly = Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
                ? uri.AbsolutePath
                : fileUrl;

            if (pathOnly.StartsWith(requestPath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = pathOnly[requestPath.Length..].TrimStart('/', '\\');
                var physicalPath = Path.Combine(physicalBasePath, relativePath);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    _logger.LogInformation("本地文件已成功删除: {Path}", physicalPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "尝试删除本地媒体文件失败: {Url}", fileUrl);
        }

        return Task.CompletedTask;
    }
}
