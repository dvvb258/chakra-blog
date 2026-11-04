namespace Content.Application.Common;

/// <summary>
/// 通用 S3 兼容对象存储配置（支持七牛云 S3、MinIO、阿里云 OSS、AWS S3 等任意通用 S3 兼容厂商）
/// </summary>
public sealed class S3StorageSettings
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "S3Storage";

    /// <summary>
    /// S3 服务端点（如七牛云 S3: https://s3.cn-east-1.qiniucs.com, MinIO: http://localhost:9000, AWS: https://s3.us-east-1.amazonaws.com）
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// S3 服务端点别名（兼容 Endpoint 命名）
    /// </summary>
    public string Endpoint
    {
        get => ServiceUrl;
        set => ServiceUrl = value;
    }

    /// <summary>
    /// S3 访问密钥 ID（AccessKey / AccessKeyId）
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// S3 私有密钥（SecretKey / SecretAccessKey）
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称（BucketName）
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称别名（兼容 Bucket 命名）
    /// </summary>
    public string Bucket
    {
        get => BucketName;
        set => BucketName = value;
    }

    /// <summary>
    /// 存储区域（Region），例如 auto / cn-east-1 / us-east-1，默认 auto
    /// </summary>
    public string Region { get; set; } = "auto";

    /// <summary>
    /// 公开访问基础域名（如 https://cdn.yourdomain.com 或云存储自带的公开外链域名）
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否强制使用路径样式（ForcePathStyle），绝大多数兼容 S3 的第三方对象存储均需设为 true
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;
}
