namespace Identity.Application.Users.DTOs;

/// <summary>
/// 修改密码请求体
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// 原密码（若此前未设置密码则无需填写）
    /// </summary>
    public string? OldPassword { get; init; }

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}
