using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Converters;

/// <summary>
/// Writes a list of ids as the array of <c>{"id":n}</c> objects Halo takes, and reads that shape back.
/// </summary>
internal sealed class IdObjectListConverter : JsonConverter<IReadOnlyList<int>>
{
	/// <inheritdoc />
	public override IReadOnlyList<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Expected an array of id objects, but found {reader.TokenType}.");
		}

		var ids = new List<int>();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			var item = document.RootElement;

			if (item.ValueKind != JsonValueKind.Object
				|| !item.TryGetProperty("id", out var id)
				|| !id.TryGetInt32(out var value))
			{
				throw new JsonException("Expected an object with an integer \"id\".");
			}

			ids.Add(value);
		}

		return ids;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, IReadOnlyList<int> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (var id in value)
		{
			writer.WriteStartObject();
			writer.WriteNumber("id", id);
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
	}
}
