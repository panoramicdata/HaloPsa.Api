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
		=> JsonArrayReader.Read(ref reader, "id objects", ReadId);

	private static int ReadId(JsonElement item)
		=> item.ValueKind == JsonValueKind.Object
			&& item.TryGetProperty("id", out var id)
			&& id.ValueKind == JsonValueKind.Number
			&& id.TryGetInt32(out var value)
				? value
				: throw new JsonException("Expected an object with an integer \"id\".");

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
