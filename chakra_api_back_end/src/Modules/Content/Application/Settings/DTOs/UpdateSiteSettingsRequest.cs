using System.Text.Json;

namespace Content.Application.Settings.DTOs;

/// <summary>
/// 更新站点设置请求体（config 为对默认配置的覆盖项，仅需提交需要覆盖的字段）
/// </summary>
public sealed record UpdateSiteSettingsRequest
{
    /// <summary>
    /// 配置覆盖项 JSON 对象
    /// </summary>
    public JsonElement? Config { get; init; }
}
