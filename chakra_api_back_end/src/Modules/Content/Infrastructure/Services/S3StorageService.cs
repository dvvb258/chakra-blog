using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Content.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Exceptions;

namespace Content.Infrastructure.Services;

/// <summary>
/// 通用 S3 兼容协议对象存储服务实现（支持七牛云 S3、MinIO、AWS S3 等通用 S3 兼容厂商）
/// </summary>
public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly S3StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;
    private AmazonS3Client? _client;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="options">S3 存储配置项</param>
    /// <param name="logger">日志记录器</param>
    public S3StorageService(
        IOptions<S3StorageSettings> options,
        ILogger<S3StorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    private AmazonS3Client GetClient()
    {
        if (_client != null) return _client;

        lock (_lock)
        {
            if (_client != null) return _client;

            if (string.IsNullOrWhiteSpace(_settings.ServiceUrl) ||
                string.IsNullOrWhiteSpace(_settings.AccessKey) ||
                string.IsNullOrWhiteSpace(_settings.SecretKey) ||
                string.IsNullOrWhiteSpace(_settings.BucketName))
            {
                throw new BusinessException("系统尚未配置 S3 兼容对象存储（ServiceUrl / AccessKey / SecretKey / BucketName），无法执行存储操作", 500);
            }

            var credentials = new BasicAWSCredentials(_settings.AccessKey.Trim(), _settings.SecretKey.Trim());
            var config = new AmazonS3Config
            {
                ServiceURL = _settings.ServiceUrl.Trim(),
                AuthenticationRegion = string.IsNullOrWhiteSpace(_settings.Region) ? "auto" : _settings.Region.Trim(),
                ForcePathStyle = _settings.ForcePathStyle,
            };

            _client = new AmazonS3Client(credentials, config);
            return _client;
        }
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var uploadContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var key = $"uploads/{DateTime.UtcNow:yyyy/MM}/{safeFileName}";

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName.Trim(),
                Key = key,
                InputStream = stream,
                ContentType = uploadContentType,
                DisablePayloadSigning = true,
            };

            await client.PutObjectAsync(request, cancellationToken);

            var publicBase = string.IsNullOrWhiteSpace(_settings.PublicUrl)
                ? $"{_settings.ServiceUrl.TrimEnd('/')}/{_settings.BucketName.Trim()}"
                : _settings.PublicUrl.TrimEnd('/');

            if (!publicBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !publicBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                publicBase = $"https://{publicBase}";
            }

            var fullUrl = $"{publicBase}/{key}";
            _logger.LogInformation("媒体文件已成功上传至 S3 兼容存储: Key={Key}, URL={Url}", key, fullUrl);

            return fullUrl;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 兼容存储上传失败: Code={ErrorCode}, Message={Message}", ex.ErrorCode, ex.Message);
            throw new BusinessException($"对象存储服务响应异常: {ex.Message}", 502);
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            _logger.LogError(ex, "S3 兼容存储上传过程发生未知异常");
            throw new BusinessException("上传文件至云存储失败，请稍后重试", 500);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var client = GetClient();

        try
        {
            string key;
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                var bucket = _settings.BucketName.Trim();

                // 若路径以 bucket 名称开头（PathStyle 路径格式），剔除 bucket 前缀
                if (path.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                {
                    key = path[(bucket.Length + 1)..];
                }
                else
                {
                    key = path;
                }
            }
            else
            {
                key = fileUrl.TrimStart('/');
            }

            var request = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName.Trim(),
                Key = key,
            };

            await client.DeleteObjectAsync(request, cancellationToken);
            _logger.LogInformation("已从 S3 兼容存储删除文件: Key={Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从 S3 兼容存储删除文件异常: URL={Url}", fileUrl);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client?.Dispose();
    }
}
