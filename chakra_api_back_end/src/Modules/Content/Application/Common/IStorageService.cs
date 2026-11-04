namespace Content.Application.Common;

/// <summary>
/// 媒体与文件存储服务契约
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// 上传文件到云存储
    /// </summary>
    /// <param name="stream">文件二进制流</param>
    /// <param name="fileName">原始文件名或自定义相对路径</param>
    /// <param name="contentType">MIME 类型（如 image/webp）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>公共访问 URL</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除已上传文件
    /// </summary>
    /// <param name="fileUrl">公共访问 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
