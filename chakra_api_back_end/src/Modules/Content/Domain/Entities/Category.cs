using SharedKernel.Domain;

namespace Content.Domain.Entities;

/// <summary>
/// 文章分类实体
/// </summary>
public class Category : Entity, IAggregateRoot
{
    /// <summary>
    /// 分类名称（显示在界面的中文或英文标签）
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 分类详细描述
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// 前端图标标识（如 Code2, Bot, Sparkles）
    /// </summary>
    public string Icon { get; private set; } = string.Empty;

    /// <summary>
    /// 排序权重（数值越小越靠前）
    /// </summary>
    public int Sort { get; private set; }

    private Category()
        : base()
    {
    }

    /// <summary>
    /// 创建新分类
    /// </summary>
    /// <param name="name">分类名称</param>
    /// <param name="description">分类描述</param>
    /// <param name="icon">图标</param>
    /// <param name="sort">排序号</param>
    public Category(
        string name,
        string description = "",
        string icon = "",
        int sort = 0)
        : base(SnowflakeIdGenerator.NextId())
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("分类名称不能为空", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Icon = icon?.Trim() ?? string.Empty;
        Sort = sort;
    }

    /// <summary>
    /// 更新分类属性
    /// </summary>
    /// <param name="name">分类名称</param>
    /// <param name="description">描述</param>
    /// <param name="icon">图标</param>
    /// <param name="sort">排序</param>
    public void Update(string name, string description, string icon, int sort)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("分类名称不能为空", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Icon = icon?.Trim() ?? string.Empty;
        Sort = sort;
        SetUpdatedAt();
    }
}
