namespace Identity.Application.Common.Models;

/// <summary>
/// GitHub 用户个人资料信息
/// </summary>
public sealed record GitHubUserInfo
{
    /// <summary>
    /// GitHub 唯一数字标识（转换为字符串）
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// GitHub 登录用户名
    /// </summary>
    public string Login { get; init; } = string.Empty;

    /// <summary>
    /// 昵称/显示名称
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 头像地址
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// 绑定的公开或主邮箱
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 个人简介
    /// </summary>
    public string? Bio { get; init; }
}
