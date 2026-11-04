using SharedKernel.Domain;

namespace Content.Domain.Entities;

/// <summary>
/// 系列实体
/// </summary>
public class Series : Entity, IAggregateRoot
{
    /// <summary>
    /// 系列名称
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 系列描述
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// 封面图片URL
    /// </summary>
    public string CoverUrl { get; private set; } = string.Empty;

    /// <summary>
    /// 主题色
    /// </summary>
    public string Theme { get; private set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; private set; } = string.Empty;

    private Series()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Series"/> class.
    /// </summary>
    /// <param name="name">系列名称</param>
    /// <param name="description">系列描述</param>
    /// <param name="coverUrl">封面图片URL</param>
    /// <param name="theme">主题色</param>
    /// <param name="author">作者</param>
    public Series(
        string name,
        string description = "",
        string coverUrl = "",
        string theme = "",
        string author = "")
        : base(SnowflakeIdGenerator.NextId())
    {
        Name = name;
        Description = description;
        CoverUrl = coverUrl;
        Theme = theme;
        Author = author;
    }

    /// <summary>
    /// 更新系列信息
    /// </summary>
    /// <param name="name">系列名称</param>
    /// <param name="description">系列描述</param>
    /// <param name="coverUrl">封面图片URL</param>
    /// <param name="theme">主题色</param>
    public void Update(string name, string description, string coverUrl, string theme)
    {
        Name = name;
        Description = description;
        CoverUrl = coverUrl;
        Theme = theme;
    }
}
