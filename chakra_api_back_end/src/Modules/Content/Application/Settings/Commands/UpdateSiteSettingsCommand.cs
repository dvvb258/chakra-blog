using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Settings.Commands;

/// <summary>
/// 更新站点设置命令
/// </summary>
/// <param name="ConfigJson">配置覆盖项 JSON</param>
[AuditLog(AuditModules.Settings, "更新站点设置")]
public sealed record UpdateSiteSettingsCommand(string ConfigJson) : ICommand;
