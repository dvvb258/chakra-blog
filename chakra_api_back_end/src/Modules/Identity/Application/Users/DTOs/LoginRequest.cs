namespace Identity.Application.Users.DTOs;

/// <summary>
/// 登录请求参数模型
/// </summary>
/// <param name="Account">登录账号（用户名或电子邮箱）</param>
/// <param name="Password">明文密码</param>
public sealed record LoginRequest(string Account, string Password);
