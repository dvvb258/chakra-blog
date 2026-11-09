using System.Text.Json;
using FluentValidation;

namespace Content.Application.Settings.Commands;

/// <summary>
/// 更新站点设置命令参数验证器
/// </summary>
public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    /// <summary>
    /// 配置 JSON 最大体积（64KB），避免超大报文占用数据库与页面渲染资源
    /// </summary>
    private const int MaxConfigLength = 64 * 1024;

    /// <summary>
    /// 禁止出现的字段名（防止客户端原型污染与原型链篡改）
    /// </summary>
    private static readonly string[] ForbiddenKeys = ["__proto__", "constructor", "prototype"];

    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.ConfigJson)
            .NotEmpty().WithMessage("站点配置不能为空")
            .MaximumLength(MaxConfigLength).WithMessage("站点配置内容不能超过 64KB")
            .Must(BeJsonObject).WithMessage("站点配置必须是合法的 JSON 对象")
            .Must(NotContainForbiddenKeys).WithMessage("站点配置包含非法字段名")
            .Must(HaveValidSiteUrl).WithMessage("站点地址必须是 http(s):// 开头的完整地址");
    }

    /// <summary>
    /// 校验可选的 url 字段：存在时必须是绝对 HTTP(S) 地址，避免展示端 new URL() 抛错
    /// </summary>
    private static bool HaveValidSiteUrl(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("url", out var urlElement))
            {
                return true;
            }

            if (urlElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = urlElement.GetString();
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
        catch (JsonException)
        {
            // 合法的 JSON 对象校验由其他规则负责，这里不重复报错
            return true;
        }
    }

    private static bool BeJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool NotContainForbiddenKeys(string json)
    {
        // 空值与非法 JSON 由其他规则负责报错，这里直接放行以避免重复提示
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return !ContainsForbiddenKey(document.RootElement);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static bool ContainsForbiddenKey(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (ForbiddenKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (ContainsForbiddenKey(property.Value))
                    {
                        return true;
                    }
                }

                return false;

            case JsonValueKind.Array:
                return element.EnumerateArray().Any(ContainsForbiddenKey);

            default:
                return false;
        }
    }
}
