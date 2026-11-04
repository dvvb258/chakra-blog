using Content.Application.Series.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Series.Commands;

/// <summary>
/// 创建专栏系列命令
/// </summary>
/// <param name="Request">创建专栏参数</param>
/// <param name="Author">作者（博主用户名）</param>
[AuditLog(AuditModules.Series, "新建专栏")]
public sealed record CreateSeriesCommand(
    CreateSeriesRequest Request,
    string? Author = null) : ICommand<long>;
