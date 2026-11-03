using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Response wrapper for ticket collections with pagination support
/// </summary>
public record TicketsResponse
{
	/// <summary>
	/// The list of tickets returned
	/// </summary>
	public required IReadOnlyList<Ticket> Tickets { get; init; } = [];

	/// <summary>
	/// Halo's <c>record_count</c>: the TOTAL number of matching tickets when the request used
	/// <c>pageinate=true</c>, but only the number of tickets in this response when it did not
	/// </summary>
	/// <remarks>
	/// Checked against a live tenant (October 2026): 51 tickets gave <c>record_count: 51</c> paged with a page
	/// size of 1 or 2, and <c>record_count: 2</c> unpaged with <c>count=2</c>. So a total needs pagination.
	/// </remarks>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }

	/// <summary>
	/// Current page number (when using pagination)
	/// </summary>
	[JsonPropertyName("page_no")]
	public int? PageNo { get; init; }

	/// <summary>
	/// Page size used (when using pagination)
	/// </summary>
	[JsonPropertyName("page_size")]
	public int? PageSize { get; init; }

	/// <summary>
	/// Total number of pages available (when using pagination)
	/// </summary>
	public int? PageCount { get; init; }

	/// <summary>
	/// Whether there are more results available
	/// </summary>
	public bool HasMore { get; init; }

	/// <summary>
	/// Whether pagination was used for this response
	/// </summary>
	public bool IsPaginated { get; init; }
}

/// <summary>
/// Response wrapper for a single ticket operation
/// </summary>
public record TicketResponse
{
	/// <summary>
	/// The ticket data
	/// </summary>
	public required Ticket Ticket { get; init; }

	/// <summary>
	/// Whether the operation was successful
	/// </summary>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the operation
	/// </summary>
	public IReadOnlyList<string>? Messages { get; init; }
}

/// <summary>
/// Response for ticket creation operations
/// </summary>
[JsonConverter(typeof(CreateTicketResponseConverter))]
public record CreateTicketResponse
{
	/// <summary>
	/// The newly created ticket
	/// </summary>
	public required Ticket Ticket { get; init; }

	/// <summary>
	/// Every ticket Halo returned; <see cref="Ticket"/> is the first
	/// </summary>
	public IReadOnlyList<Ticket> Tickets { get; init; } = [];

	/// <summary>
	/// Whether the creation was successful
	/// </summary>
	/// <remarks>Halo's <c>success</c> when it wraps the ticket as <c>{"ticket":...}</c> and gives a JSON boolean, the string "true" or "false", or 0 or 1; otherwise true, as for a bare ticket or an array. A false value is returned, not thrown, and the ticket Halo sent is still in <see cref="Ticket"/>.</remarks>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the creation
	/// </summary>
	/// <remarks>Halo's <c>messages</c> (an array of strings, or one string) when it wraps the ticket; non-string and blank items are skipped. Null when absent or when none remain.</remarks>
	public IReadOnlyList<string>? Messages { get; init; }

	/// <summary>
	/// The ID of the newly created ticket
	/// </summary>
	public int TicketId => Ticket.Id;
}

// Halo answers a create with a bare ticket, an array of tickets or a "ticket" wrapper; the ticket exists by then.
internal sealed class CreateTicketResponseConverter : JsonConverter<CreateTicketResponse>
{
	// TicketsApiWrapper matches on this phrase to tell this error from any other deserialisation failure.
	internal const string MayHaveBeenCreated = "the ticket may have been created";

	internal static string UnreadableMessage(string id)
		=> $"Halo's ticket create response could not be read as a ticket, but {MayHaveBeenCreated} (id {id}).";

	public override CreateTicketResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var content = TicketResponseReader.Read(ref reader, options, "create", nameof(CreateTicketResponse), UnreadableMessage);

		return new CreateTicketResponse { Ticket = content.Tickets[0], Tickets = content.Tickets, Success = content.Success, Messages = content.Messages };
	}

	public override void Write(Utf8JsonWriter writer, CreateTicketResponse value, JsonSerializerOptions options)
		=> TicketResponseReader.Write(writer, value.Ticket, value.Success, value.Messages, options);
}

/// <summary>
/// Response for ticket update operations
/// </summary>
[JsonConverter(typeof(UpdateTicketResponseConverter))]
public record UpdateTicketResponse
{
	/// <summary>
	/// The updated ticket
	/// </summary>
	public required Ticket Ticket { get; init; }

	/// <summary>
	/// Every ticket Halo returned; <see cref="Ticket"/> is the first
	/// </summary>
	public IReadOnlyList<Ticket> Tickets { get; init; } = [];

	/// <summary>
	/// Whether the update was successful
	/// </summary>
	/// <remarks>Halo's <c>success</c> when it wraps the ticket as <c>{"ticket":...}</c> and gives a JSON boolean, the string "true" or "false", or 0 or 1; otherwise true, as for a bare ticket or an array. A false value is returned, not thrown, and the ticket Halo sent is still in <see cref="Ticket"/>.</remarks>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the update
	/// </summary>
	/// <remarks>Halo's <c>messages</c> (an array of strings, or one string) when it wraps the ticket; non-string and blank items are skipped. Null when absent or when none remain.</remarks>
	public IReadOnlyList<string>? Messages { get; init; }

	/// <summary>
	/// The ID of the updated ticket
	/// </summary>
	public int TicketId => Ticket.Id;
}

// Halo answers an update in the same shapes as a create; the update has been applied by then.
internal sealed class UpdateTicketResponseConverter : JsonConverter<UpdateTicketResponse>
{
	// TicketsApiWrapper matches on this phrase to tell this error from any other deserialisation failure.
	internal const string MayHaveBeenApplied = "the update may have been applied";

	internal static string UnreadableMessage(string id)
		=> $"Halo's ticket update response could not be read, but {MayHaveBeenApplied} (id {id}).";

	public override UpdateTicketResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var content = TicketResponseReader.Read(ref reader, options, "update", nameof(UpdateTicketResponse), UnreadableMessage);

		return new UpdateTicketResponse { Ticket = content.Tickets[0], Tickets = content.Tickets, Success = content.Success, Messages = content.Messages };
	}

	public override void Write(Utf8JsonWriter writer, UpdateTicketResponse value, JsonSerializerOptions options)
		=> TicketResponseReader.Write(writer, value.Ticket, value.Success, value.Messages, options);
}

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
		JsonElement? ticket = null;
		bool? success = null;
		IReadOnlyList<string>? messages = null;

		foreach (var property in root.EnumerateObject())
		{
			var value = property.Value;

			if (IsNamed(property, "ticket") && value.ValueKind == JsonValueKind.Object)
			{
				ticket ??= value;
			}
			else if (IsNamed(property, "success") && ReadBoolean(value) is { } flag)
			{
				success ??= flag;
			}
			else if (IsNamed(property, "messages"))
			{
				messages ??= ReadMessages(value);
			}
		}

		return ticket is { } wrapped
			? new TicketResponseContent([ReadTicket(wrapped, options, unreadableMessage)], success ?? true, messages)
			: new TicketResponseContent([ReadTicket(root, options, unreadableMessage)], Success: true, Messages: null);
	}

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