using MediatR;

namespace SharedKernel.Application.CQRS;

/// <summary>
/// 命令接口（写操作，有返回值）
/// </summary>
/// <typeparam name="TResponse">返回类型</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// 命令接口（写操作，无返回值）
/// </summary>
public interface ICommand : IRequest
{
}
