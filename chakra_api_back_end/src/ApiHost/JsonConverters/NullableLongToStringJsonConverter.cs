using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiHost.JsonConverters;

/// <summary>
/// 将可空 64 位长整数 (<see cref="Nullable{Int64}"/>) 序列化为 JSON 字符串，防止 JavaScript 端 Number 精度丢失。
/// 反序列化时同时兼容 Null、字符串和原生数字格式。
/// </summary>
public sealed class NullableLongToStringJsonConverter : JsonConverter<long?>
{
    /// <inheritdoc />
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            if (long.TryParse(str, out var value))
            {
                return value;
            }

            throw new JsonException($"无法将字符串 \"{str}\" 解析为可空 64 位长整型 (Int64)。");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"非法的 Token 类型 \"{reader.TokenType}\"，期望数字、数值字符串或 null。");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
