using Content.Application.Common.Models;

namespace Content.Application.Common;

/// <summary>
/// 文章自动生成流程的只读数据访问接口
/// </summary>
public interface IArticleGenerationReadStore
{
    /// <summary>
    /// 判断文章中是否已存在指定原文链接（包含软删除文章，与数据库唯一索引语义保持一致）
    /// </summary>
    /// <param name="sourceLink">原文链接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已存在</returns>
    Task<bool> SourceLinkExistsAsync(string sourceLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未删除分类选项，按排序权重升序
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类选项集合</returns>
    Task<IReadOnlyList<ArticleGenerationOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未删除系列选项，按创建时间升序
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系列选项集合</returns>
    Task<IReadOnlyList<ArticleGenerationOption>> GetSeriesAsync(CancellationToken cancellationToken = default);
}
