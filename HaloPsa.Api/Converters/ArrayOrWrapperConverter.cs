using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Converters;

/// <summary>
/// Reads a list response that Halo returns either as a plain array or wrapped in an object
/// carrying the array under <c>itemsPropertyName</c> and a <c>record_count</c>.
/// </summary>
/// <remarks>
/// <c>GET /api/Status</c> and <c>GET /api/Priority</c> return a plain array, which the default
/// converter rejects for a wrapper type.
/// </remarks>
public abstract class ArrayOrWrapperConverter<TResponse, TItem>(string itemsPropertyName) : JsonConverter<TResponse>
{
	/// <summary>
	/// Builds the response from the items and the record count.
	/// </summary>
	protected abstract TResponse Create(IReadOnlyList<TItem> items, int recordCount);

	/// <summary>
	/// Gets the items from a response, for writing.
	/// </summary>
	protected abstract IReadOnlyList<TItem> GetItems(TResponse value);

	/// <summary>
	/// Gets the record count from a response, for writing.
	/// </summary>
	protected abstract int GetRecordCount(TResponse value);

	/// <inheritdoc />
	public override TResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var items = JsonSerializer.Deserialize<List<TItem>>(ref reader, options) ?? [];
			return Create(items, items.Count);
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Expected an array or an object for {typeof(TResponse).Name}, but found {reader.TokenType}.");
		}

		using var document = JsonDocument.ParseValue(ref reader);
		var root = document.RootElement;

		var wrapped = root.TryGetProperty(itemsPropertyName, out var itemsElement)
			? itemsElement.Deserialize<List<TItem>>(options) ?? []
			: [];

		var recordCount = root.TryGetProperty("record_count", out var countElement) && countElement.TryGetInt32(out var count)
			? count
			: wrapped.Count;

		return Create(wrapped, recordCount);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, TResponse value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(itemsPropertyName);
		JsonSerializer.Serialize(writer, GetItems(value), options);
		writer.WriteNumber("record_count", GetRecordCount(value));
		writer.WriteEndObject();
	}
}
