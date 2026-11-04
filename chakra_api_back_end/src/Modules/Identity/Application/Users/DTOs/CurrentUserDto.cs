using SharedKernel.Constants;

namespace Identity.Application.Users.DTOs;

/// <summary>
/// 当前登录用户信息数据传输对象
/// </summary>
public sealed record CurrentUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 登录用户名
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; init; } = string.Empty;

    /// <summary>
    /// 电子邮箱
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// 角色（如 Admin）
    /// </summary>
    public string Role { get; init; } = RoleNames.Admin;

    /// <summary>
    /// GitHub 关联标识
    /// </summary>
    public string? GithubId { get; init; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>
    /// 是否已设置独立登录密码
    /// </summary>
    public bool HasPassword { get; init; } = true;

    /// <summary>
    /// 当前用户允许访问的菜单路由列表（如：/、/articles 等）
    /// </summary>
    public List<string> Menus { get; init; } = [];

    /// <summary>
    /// 当前用户拥有的权限标识集合（如：dashboard:view, article:manage 等）
    /// </summary>
    public List<string> Permissions { get; init; } = [];

    /// <summary>
    /// 从 User 领域实体快速构建 DTO
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="menus">授权菜单路由</param>
    /// <param name="permissions">授权权限代号</param>
    /// <returns>CurrentUserDto 实例</returns>
    public static CurrentUserDto FromUser(Domain.Entities.User user, List<string>? menus = null, List<string>? permissions = null)
    {
        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            GithubId = user.GithubId,
            LastLoginAt = user.LastLoginAt,
            HasPassword = user.HasPassword,
            Menus = menus ?? [],
            Permissions = permissions ?? [],
        };
    }
}
