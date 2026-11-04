using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Series.Commands;

/// <summary>
/// 删除专栏系列命令（软删除，并解除其下文章的专栏绑定）
/// </summary>
/// <param name="Id">专栏ID</param>
[AuditLog(AuditModules.Series, "删除专栏")]
public sealed record DeleteSeriesCommand(long Id) : ICommand;
