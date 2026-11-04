namespace Content.Application.Categories.DTOs;

/// <summary>
/// 更新文章分类请求体
/// </summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>分类名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>分类描述</summary>
    public string? Description { get; init; }

    /// <summary>前端展示图标名称</summary>
    public string? Icon { get; init; }

    /// <summary>排序序号</summary>
    public int Sort { get; init; }
}
