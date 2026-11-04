using Identity.Domain.Events;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Constants;
using SharedKernel.Domain;

namespace Identity.Domain.Entities;

/// <summary>
/// 系统用户聚合根
/// </summary>
public class User : Entity, IAggregateRoot
{
    /// <summary>
    /// 登录用户名（唯一）
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// 电子邮箱（可选，唯一）
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// 密码安全哈希（基于 PBKDF2，纯第三方登录时可为空）
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    public string Nickname { get; private set; } = string.Empty;

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// 角色（默认 Admin）
    /// </summary>
    public string Role { get; private set; } = RoleNames.Admin;

    /// <summary>
    /// 账号是否处于激活状态（被禁用时无法登录）
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// GitHub 关联唯一标识（预留第三方 OAuth 登录支持）
    /// </summary>
    public string? GithubId { get; private set; }

    /// <summary>
    /// 最后登录时间（UTC）
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// 是否已设置本地登录密码
    /// </summary>
    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    /// <summary>
    /// EF Core 反射使用的私有无参构造函数
    /// </summary>
    private User()
        : base()
    {
    }

    /// <summary>
    /// 创建系统管理员/普通用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="nickname">用户昵称</param>
    /// <param name="role">角色</param>
    /// <param name="email">邮箱</param>
    /// <param name="avatarUrl">头像链接</param>
    public User(
        string username,
        string nickname,
        string role = RoleNames.Admin,
        string? email = null,
        string? avatarUrl = null)
        : base(SnowflakeIdGenerator.NextId())
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("用户名不能为空", nameof(username));
        }

        Username = username.Trim().ToLowerInvariant();
        Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? RoleNames.Admin : role.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        AvatarUrl = avatarUrl?.Trim();
        IsActive = true;
    }

    /// <summary>
    /// 设置/更新密码（采用 ASP.NET Core 原生加盐哈希）
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hasher">密码哈希器</param>
    public void SetPassword(string password, IPasswordHasher<User> hasher)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("密码不能为空", nameof(password));
        }

        PasswordHash = hasher.HashPassword(this, password);
        SetUpdatedAt();
        AddDomainEvent(new UserSessionInvalidatedDomainEvent(Id, "PasswordChanged"));
    }

    /// <summary>
    /// 校验明文密码是否与当前哈希匹配
    /// </summary>
    /// <param name="password">输入的明文密码</param>
    /// <param name="hasher">密码哈希器</param>
    /// <returns>密码是否正确</returns>
    public bool VerifyPassword(string password, IPasswordHasher<User> hasher)
    {
        if (string.IsNullOrEmpty(PasswordHash))
        {
            return false;
        }

        var result = hasher.VerifyHashedPassword(this, PasswordHash, password);
        return result == PasswordVerificationResult.Success
               || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>
    /// 绑定或通过 GitHub 关联第三方账号
    /// </summary>
    /// <param name="githubId">GitHub 唯一数字或用户名标识</param>
    /// <param name="avatarUrl">从 GitHub 拉取的头像</param>
    /// <param name="nickname">从 GitHub 拉取的昵称</param>
    public void BindGithub(string githubId, string? avatarUrl = null, string? nickname = null)
    {
        if (Username == "admin")
        {
            throw new InvalidOperationException("超级管理员账号禁止绑定第三方账号");
        }

        if (string.IsNullOrWhiteSpace(githubId))
        {
            throw new ArgumentException("GitHub 标识不能为空", nameof(githubId));
        }

        GithubId = githubId.Trim();
        if (!string.IsNullOrWhiteSpace(avatarUrl) && string.IsNullOrWhiteSpace(AvatarUrl))
        {
            AvatarUrl = avatarUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(nickname) && string.IsNullOrWhiteSpace(Nickname))
        {
            Nickname = nickname.Trim();
        }

        SetUpdatedAt();
    }

    /// <summary>
    /// 解除第三方 GitHub 关联（必须已设置本地登录密码）
    /// </summary>
    public void UnbindGithub()
    {
        if (string.IsNullOrEmpty(PasswordHash))
        {
            throw new InvalidOperationException("未设置登录密码，无法解除第三方账号绑定");
        }

        GithubId = null;
        SetUpdatedAt();
    }

    /// <summary>
    /// 记录登录成功时间
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// 更新个人基本信息
    /// </summary>
    /// <param name="nickname">用户昵称</param>
    /// <param name="email">电子邮箱</param>
    /// <param name="avatarUrl">头像链接</param>
    public void UpdateProfile(string nickname, string? email, string? avatarUrl)
    {
        Nickname = string.IsNullOrWhiteSpace(nickname) ? Nickname : nickname.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        AvatarUrl = avatarUrl?.Trim();
        SetUpdatedAt();
    }

    /// <summary>
    /// 停用账号
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SetUpdatedAt();
        AddDomainEvent(new UserSessionInvalidatedDomainEvent(Id, "UserDeactivated"));
    }

    /// <summary>
    /// 启用账号
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    /// <summary>
    /// 更新用户角色（限管理员操作）
    /// </summary>
    /// <param name="role">新角色名称</param>
    public void UpdateRole(string role)
    {
        if (Username == "admin" && role != RoleNames.Admin)
        {
            throw new InvalidOperationException("默认超级管理员账号角色不可修改");
        }

        var newRole = string.IsNullOrWhiteSpace(role) ? Role : role.Trim();
        if (string.Equals(Role, newRole, StringComparison.Ordinal))
        {
            return;
        }

        var previousRole = Role;
        Role = newRole;
        SetUpdatedAt();
        AddDomainEvent(new UserSessionInvalidatedDomainEvent(Id, $"RoleChanged:{previousRole}->{newRole}"));
    }
}
