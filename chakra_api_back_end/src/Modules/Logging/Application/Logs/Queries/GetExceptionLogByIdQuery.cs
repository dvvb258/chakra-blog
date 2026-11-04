using Logging.Application.Logs.DTOs;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 根据ID获取系统异常日志详情
/// </summary>
/// <param name="Id">日志ID</param>
public sealed record GetExceptionLogByIdQuery(long Id) : IQuery<ExceptionLogDto?>;
