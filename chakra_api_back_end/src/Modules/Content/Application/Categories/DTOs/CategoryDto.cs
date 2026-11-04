namespace Content.Application.Categories.DTOs;

/// <summary>
/// 文章分类数据传输对象
/// </summary>
public record CategoryDto
{
    /// <summary>分类雪花ID</summary>
    public long Id { get; init; }

    /// <summary>分类显示名称（如 .NET / C#, AI Agent）</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>分类描述</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>前端图标名（如 Code2, Bot, Sparkles）</summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>排序序号</summary>
    public int Sort { get; init; }

    /// <summary>当前分类下已发布的文章总数</summary>
    public int ArticleCount { get; init; }
}
