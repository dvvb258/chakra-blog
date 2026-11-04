using MediatR;

namespace SharedKernel.Application.CQRS;

/// <summary>
/// 查询接口（读操作）
/// </summary>
/// <typeparam name="TResponse">返回类型</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
