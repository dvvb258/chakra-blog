using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.Common;

namespace ApiHost.JsonConverters;

/// <summary>
/// 将响应中的所有 <see cref="DateTime"/> 统一转换为北京时间（UTC+8）后序列化，
/// 输出格式为带偏移的 ISO 8601，例如 2025-01-15T18:00:00+08:00。
/// </summary>
public sealed class BeijingDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Unspecified 视为 UTC（PostgreSQL/Dapper 默认行为）
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        var beijingTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneHelper.AppTimeZone);
        writer.WriteStringValue(new DateTimeOffset(beijingTime, TimeZoneHelper.AppTimeZone.GetUtcOffset(beijingTime)));
    }
}
