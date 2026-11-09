using System.Text.Json;
using SharedKernel.Application.CQRS;

namespace Content.Application.Settings.Queries;

/// <summary>
/// 获取站点设置覆盖项查询（公开访问）
/// </summary>
public sealed record GetSiteSettingsQuery : IQuery<JsonElement>;
