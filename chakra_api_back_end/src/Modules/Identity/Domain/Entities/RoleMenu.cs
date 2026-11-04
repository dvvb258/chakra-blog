using SharedKernel.Domain;

namespace Identity.Domain.Entities;

/// <summary>
/// 角色与菜单关联映射实体
/// </summary>
public class RoleMenu : Entity
{
    /// <summary>
    /// 角色 ID
    /// </summary>
    public long RoleId { get; private set; }

    /// <summary>
    /// 菜单 ID
    /// </summary>
    public long MenuId { get; private set; }

    private RoleMenu()
        : base()
    {
    }

    /// <summary>
    /// 创建角色与菜单关联
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuId">菜单ID</param>
    public RoleMenu(long roleId, long menuId)
        : base(SnowflakeIdGenerator.NextId())
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色ID非法", nameof(roleId));
        }

        if (menuId <= 0)
        {
            throw new ArgumentException("菜单ID非法", nameof(menuId));
        }

        RoleId = roleId;
        MenuId = menuId;
    }
}
