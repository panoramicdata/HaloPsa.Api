using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Converters;

/// <summary>
/// Writes a list of tags as the array of <c>{"text":t}</c> objects Halo takes, and reads that shape back.
/// </summary>
internal sealed class TagTextListConverter : JsonConverter<IReadOnlyList<string>>
{
	/// <inheritdoc />
	public override IReadOnlyList<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Expected an array of tag objects, but found {reader.TokenType}.");
		}

		var tags = new List<string>();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			var item = document.RootElement;

			if (item.ValueKind != JsonValueKind.Object
				|| !item.TryGetProperty("text", out var text)
				|| text.ValueKind != JsonValueKind.String)
			{
				throw new JsonException("Expected an object with a string \"text\".");
			}

			tags.Add(text.GetString()!);
		}

		return tags;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (var tag in value)
		{
			writer.WriteStartObject();
			writer.WriteString("text", tag);
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
	}
}
