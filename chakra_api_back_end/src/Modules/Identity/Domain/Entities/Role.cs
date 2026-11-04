using SharedKernel.Domain;

namespace Identity.Domain.Entities;

/// <summary>
/// 系统角色实体
/// </summary>
public class Role : Entity, IAggregateRoot
{
    /// <summary>角色唯一代号（如 Admin, Editor）</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>角色显示名称</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>角色职能描述</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>是否为系统内置角色（系统内置角色不可被删除）</summary>
    public bool IsSystem { get; private set; }

    private Role()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// </summary>
    /// <param name="code">角色代号</param>
    /// <param name="name">角色显示名称</param>
    /// <param name="description">角色职能描述</param>
    /// <param name="isSystem">是否系统内置角色</param>
    /// <param name="id">指定ID（可选）</param>
    public Role(string code, string name, string description = "", bool isSystem = false, long? id = null)
        : base(id ?? SnowflakeIdGenerator.NextId())
    {
        Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("角色代号不能为空", nameof(code)) : code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? code : name.Trim();
        Description = description ?? string.Empty;
        IsSystem = isSystem;
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    /// <param name="name">新角色名称</param>
    /// <param name="description">新角色描述</param>
    public void Update(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        Description = description ?? string.Empty;
        SetUpdatedAt();
    }
}
