namespace SharedKernel.Application;

/// <summary>
/// 当前登录用户抽象（从 JWT Claims 读取）
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 用户ID（未登录返回 0）
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    string Username { get; }

    /// <summary>
    /// 是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 是否拥有指定角色
    /// </summary>
    /// <param name="role">角色名</param>
    bool IsInRole(string role);
}
