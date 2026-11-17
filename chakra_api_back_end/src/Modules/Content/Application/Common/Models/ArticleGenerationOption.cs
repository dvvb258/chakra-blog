namespace Content.Application.Common.Models;

/// <summary>
/// 文章自动生成匹配使用的分类或系列选项
/// </summary>
public sealed record ArticleGenerationOption
{
    /// <summary>分类或系列 ID</summary>
    public long Id { get; init; }

    /// <summary>显示名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>描述</summary>
    public string Description { get; init; } = string.Empty;
}
