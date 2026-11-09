using SharedKernel.Domain;

namespace Content.Domain.Entities;

/// <summary>
/// 站点设置实体（全站单行配置，以 JSON 形式保存对默认配置的覆盖项）
/// </summary>
public class SiteSetting : Entity, IAggregateRoot
{
    /// <summary>
    /// 单例行主键（全站仅一条记录）
    /// </summary>
    public const long SingletonId = 1;

    /// <summary>
    /// 配置覆盖项 JSON（仅保存需要覆盖默认值的字段）
    /// </summary>
    public string ConfigJson { get; private set; } = "{}";

    private SiteSetting()
        : base()
    {
    }

    /// <summary>
    /// 创建站点设置
    /// </summary>
    /// <param name="configJson">配置覆盖项 JSON</param>
    public SiteSetting(string configJson)
        : base(SingletonId)
    {
        Update(configJson);
    }

    /// <summary>
    /// 更新配置覆盖项
    /// </summary>
    /// <param name="configJson">配置覆盖项 JSON</param>
    public void Update(string configJson)
    {
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
        SetUpdatedAt();
    }
}
