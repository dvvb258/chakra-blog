namespace Identity.Application.Common.Interfaces;

/// <summary>
/// 校验访问令牌中的用户状态与角色是否仍然有效
/// </summary>
public interface IUserAccessValidator
{
    /// <summary>
    /// 校验用户是否存在、启用且角色与令牌声明一致
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="role">访问令牌中的角色</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否允许继续使用当前访问令牌</returns>
    Task<bool> IsAccessAllowedAsync(long userId, string role, CancellationToken cancellationToken = default);
}
