namespace Content.Domain.Common;

/// <summary>
/// 文章生成服务契约
/// </summary>
public interface IArticleGenerationService
{
    /// <summary>
    /// 从 RSS 源中选取文章并使用 AI 生成新的技术博客文章
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功生成并入库返回 <c>true</c>，否则返回 <c>false</c></returns>
    Task<bool> GenerateArticleAsync(CancellationToken cancellationToken = default);
}
