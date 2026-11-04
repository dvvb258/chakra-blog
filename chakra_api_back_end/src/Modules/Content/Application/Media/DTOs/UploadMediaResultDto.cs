namespace Content.Application.Media.DTOs;

/// <summary>
/// 媒体上传结果传输对象
/// </summary>
public sealed record UploadMediaResultDto
{
    /// <summary>
    /// 公共访问图片 URL
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; init; }
}
