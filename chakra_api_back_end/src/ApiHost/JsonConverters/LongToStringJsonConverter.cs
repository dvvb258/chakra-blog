using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiHost.JsonConverters;

/// <summary>
/// 将 64 位长整数 (<see cref="long"/>) 序列化为 JSON 字符串，防止 JavaScript 端 Number 精度丢失（超过 2^53 - 1）。
/// 反序列化时同时兼容读取字符串和原生数字格式。
/// </summary>
public sealed class LongToStringJsonConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (long.TryParse(str, out var value))
            {
                return value;
            }

            throw new JsonException($"无法将字符串 \"{str}\" 解析为 64 位长整型 (Int64)。");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"非法的 Token 类型 \"{reader.TokenType}\"，期望数字或数值字符串。");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
