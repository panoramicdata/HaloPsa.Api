using System.Text.Json;

namespace HaloPsa.Api.Models.Tickets;

// What a ticket write response carries: the tickets, and the wrapper's "success" and "messages" when it has them.
internal readonly record struct TicketResponseContent(IReadOnlyList<Ticket> Tickets, bool Success, IReadOnlyList<string>? Messages);

// The shape logic shared by the create and update converters: a bare ticket, an array of tickets or a "ticket" wrapper.
internal static class TicketResponseReader
{
	internal static TicketResponseContent Read(ref Utf8JsonReader reader, JsonSerializerOptions options, string operation, string responseType, Func<string, string> unreadableMessage)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var root = document.RootElement;

		switch (root.ValueKind)
		{
			case JsonValueKind.Array:
				List<Ticket> tickets = [.. root.EnumerateArray().Select(element => ReadTicket(element, options, unreadableMessage))];

				if (tickets.Count == 0)
				{
					throw new JsonException($"Halo returned an empty array for a ticket {operation}, so no ticket id can be read.");
				}

				return new TicketResponseContent(tickets, Success: true, Messages: null);
			case JsonValueKind.Object:
				return ReadObject(root, options, unreadableMessage);
			default:
				throw new JsonException($"Expected an array or an object for {responseType}, but found {root.ValueKind}.");
		}
	}

	// "success" and "messages" are read only from a wrapper: a bare ticket is the ticket's own fields.
	private static TicketResponseContent ReadObject(JsonElement root, JsonSerializerOptions options, Func<string, string> unreadableMessage)
	{
		var wrapped = ValuesNamed(root, "ticket").Where(value => value.ValueKind == JsonValueKind.Object).Select(value => (JsonElement?)value).FirstOrDefault();

		if (wrapped is null)
		{
			return new TicketResponseContent([ReadTicket(root, options, unreadableMessage)], Success: true, Messages: null);
		}

		// The first value under each name that reads wins, so an unreadable "success" or "messages" cannot hide a later one.
		var success = ValuesNamed(root, "success").Select(ReadBoolean).FirstOrDefault(flag => flag is not null) ?? true;
		var messages = ValuesNamed(root, "messages").Select(ReadMessages).FirstOrDefault(list => list is not null);

		return new TicketResponseContent([ReadTicket(wrapped.Value, options, unreadableMessage)], success, messages);
	}

	private static IEnumerable<JsonElement> ValuesNamed(JsonElement root, string name)
		=> root.EnumerateObject().Where(property => IsNamed(property, name)).Select(property => property.Value);

	// Writes the wrapper shape Read accepts, so Success and Messages survive a round trip; messages whenever not null.
	internal static void Write(Utf8JsonWriter writer, Ticket ticket, bool success, IReadOnlyList<string>? messages, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(PropertyName("ticket", options));
		JsonSerializer.Serialize(writer, ticket, options);
		writer.WriteBoolean(PropertyName("success", options), success);

		if (messages is not null)
		{
			writer.WritePropertyName(PropertyName("messages", options));
			JsonSerializer.Serialize(writer, messages, options);
		}

		writer.WriteEndObject();
	}

	private static string PropertyName(string name, JsonSerializerOptions options)
		=> options.PropertyNamingPolicy?.ConvertName(name) ?? name;

	private static bool IsNamed(JsonProperty property, string name)
		=> string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

	// A JSON boolean, the strings "true" or "false", or 0 or 1; anything else is not read, so it cannot pass for either.
	private static bool? ReadBoolean(JsonElement value) => value.ValueKind switch
	{
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
		JsonValueKind.Number when value.TryGetInt32(out var number) && number is 0 or 1 => number == 1,
		_ => null
	};

	private static List<string>? ReadMessages(JsonElement value)
	{
		List<string> messages = value.ValueKind switch
		{
			JsonValueKind.String => [value.GetString()!],
			JsonValueKind.Array => [.. value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)],
			_ => []
		};

		_ = messages.RemoveAll(string.IsNullOrWhiteSpace);

		return messages.Count == 0 ? null : messages;
	}

	private static Ticket ReadTicket(JsonElement element, JsonSerializerOptions options, Func<string, string> unreadableMessage)
	{
		try
		{
			return element.Deserialize<Ticket>(options) ?? throw new JsonException("Halo returned null for a ticket.");
		}
		catch (JsonException ex)
		{
			var id = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var idElement)
				? idElement.GetRawText()
				: "unknown";

			throw new JsonException(unreadableMessage(id), ex);
		}
	}
}