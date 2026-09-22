using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Converters;

/// <summary>
/// Reads a JSON number into an <see cref="int"/> even when Halo writes it with a decimal point.
/// </summary>
/// <remarks>
/// <para>Several Halo id fields are declared as integers but are serialised as decimals - <c>GET /api/Site</c>
/// returns <c>"client_id": 21.0</c>, and <c>GET /api/Users</c> does the same for <c>site_id</c>. The default
/// <see cref="int"/> converter rejects those outright, so the whole response fails to deserialise with
/// <c>The JSON value could not be converted to System.Int32</c> and the endpoint is unusable.</para>
/// <para>A string is also accepted, because Halo returns ids as strings on some endpoints
/// (<c>GET /api/Priority</c> is one), and a caller would rather have the value than an exception.</para>
/// <para>Note that Halo uses <c>-1</c>, not <c>0</c> or <c>null</c>, as its "not set" sentinel for
/// integration id fields. This converter passes that through unchanged - deciding what an unset id means
/// belongs to the caller, not here.</para>
/// </remarks>
public sealed class FlexibleInt32Converter : JsonConverter<int>
{
	/// <inheritdoc />
	public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType switch
		{
			JsonTokenType.Number when reader.TryGetInt32(out var i) => i,
			JsonTokenType.Number => (int)reader.GetDecimal(),
			JsonTokenType.String => ParseString(reader.GetString()),
			JsonTokenType.Null => 0,
			_ => throw new JsonException($"Cannot convert {reader.TokenType} to Int32.")
		};

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);
		writer.WriteNumberValue(value);
	}

	private static int ParseString(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}

		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
		{
			return i;
		}

		return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
			? (int)d
			: throw new JsonException($"Cannot convert the string value to Int32.");
	}
}
