using MediatR;

namespace SharedKernel.Application.CQRS;

/// <summary>
/// 命令处理器接口（有返回值）
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TResponse">返回类型</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// 命令处理器接口（无返回值）
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}
