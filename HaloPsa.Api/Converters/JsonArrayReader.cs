using System.Text.Json;

namespace HaloPsa.Api.Converters;

/// <summary>
/// The array traversal shared by the list converters; each converter supplies its own item check.
/// </summary>
internal static class JsonArrayReader
{
	/// <summary>
	/// Reads the array the reader is positioned on, turning each element into a value with <paramref name="readItem"/>
	/// </summary>
	/// <param name="reader">A reader positioned on the start of the array</param>
	/// <param name="itemsDescription">What the array holds, for the error raised when the token is not an array</param>
	/// <param name="readItem">Reads one element, throwing <see cref="JsonException"/> when it is not the expected shape</param>
	internal static List<T> Read<T>(ref Utf8JsonReader reader, string itemsDescription, Func<JsonElement, T> readItem)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Expected an array of {itemsDescription}, but found {reader.TokenType}.");
		}

		List<T> items = [];
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			items.Add(readItem(document.RootElement));
		}

		return items;
	}
}
