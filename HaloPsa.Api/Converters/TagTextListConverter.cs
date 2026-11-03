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
		=> JsonArrayReader.Read(ref reader, "tag objects", ReadTag);

	private static string ReadTag(JsonElement item)
		=> item.ValueKind == JsonValueKind.Object
			&& item.TryGetProperty("text", out var text)
			&& text.ValueKind == JsonValueKind.String
				? text.GetString()!
				: throw new JsonException("Expected an object with a string \"text\".");

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
