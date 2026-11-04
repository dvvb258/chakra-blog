using SharedKernel.Domain;

namespace Identity.Domain.Events;

/// <summary>
/// 用户安全属性变化后要求撤销现有登录会话的领域事件
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Reason">会话失效原因</param>
public sealed record UserSessionInvalidatedDomainEvent(long UserId, string Reason) : IDomainEvent;
