namespace Identity.Application.Users.DTOs;

/// <summary>
/// 更新个人资料请求体
/// </summary>
public sealed record UpdateProfileRequest
{
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
}
