using Content.Application.Series.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Series.Commands;

/// <summary>
/// 更新专栏系列命令
/// </summary>
/// <param name="Id">系列ID</param>
/// <param name="Request">更新参数</param>
[AuditLog(AuditModules.Series, "修改专栏")]
public sealed record UpdateSeriesCommand(
    long Id,
    UpdateSeriesRequest Request) : ICommand;
