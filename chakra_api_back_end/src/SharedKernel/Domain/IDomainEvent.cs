using MediatR;

namespace SharedKernel.Domain;

/// <summary>
/// 领域事件标记接口
/// </summary>
public interface IDomainEvent : INotification
{
}
