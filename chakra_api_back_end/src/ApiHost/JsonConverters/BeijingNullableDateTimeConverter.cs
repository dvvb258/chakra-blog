using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.Common;

namespace ApiHost.JsonConverters;

/// <summary>
/// 将响应中的所有可空 <see cref="Nullable{DateTime}"/> 统一转换为北京时间（UTC+8）后序列化。
/// </summary>
public sealed class BeijingNullableDateTimeConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return reader.GetDateTime();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        var val = value.Value;
        var utc = val.Kind == DateTimeKind.Local
            ? val.ToUniversalTime()
            : DateTime.SpecifyKind(val, DateTimeKind.Utc);

        var beijingTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneHelper.AppTimeZone);
        writer.WriteStringValue(new DateTimeOffset(beijingTime, TimeZoneHelper.AppTimeZone.GetUtcOffset(beijingTime)));
    }
}
