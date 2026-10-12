using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Converters;

/// <summary>
/// Reads any JSON value into a <see cref="string"/>, so a property the Halo specification leaves untyped
/// cannot fail a whole response because one record holds a number or a null.
/// </summary>
/// <remarks>
/// <para>The conversions are:</para>
/// <list type="bullet">
/// <item><description>a string is returned as it is;</description></item>
/// <item><description>a number is returned as the text it was written with (<c>42</c>, <c>21.0</c>, <c>1.5e3</c>), which is
/// culture independent because it is the JSON token itself;</description></item>
/// <item><description><c>true</c> and <c>false</c> are returned as <c>"true"</c> and <c>"false"</c>;</description></item>
/// <item><description><c>null</c> is returned as <see langword="null"/>;</description></item>
/// <item><description>an object or an array is returned as its raw JSON text.</description></item>
/// </list>
/// <para>Writing always produces a JSON string (or <c>null</c>); the original JSON type is not preserved.</para>
/// </remarks>
public sealed class FlexibleStringConverter : JsonConverter<string?>
{
	/// <inheritdoc />
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType switch
		{
			JsonTokenType.String => reader.GetString(),
			JsonTokenType.Number => reader.HasValueSequence ? Encoding.UTF8.GetString(reader.ValueSequence) : Encoding.UTF8.GetString(reader.ValueSpan),
			JsonTokenType.True => "true",
			JsonTokenType.False => "false",
			JsonTokenType.Null => null,
			JsonTokenType.StartObject or JsonTokenType.StartArray => JsonElement.ParseValue(ref reader).GetRawText(),
			_ => throw new JsonException($"Cannot convert {reader.TokenType} to String.")
		};

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		writer.WriteStringValue(value);
	}
}
