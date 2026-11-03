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
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the creation
	/// </summary>
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
		using var document = JsonDocument.ParseValue(ref reader);
		var root = document.RootElement;

		List<Ticket> tickets;

		switch (root.ValueKind)
		{
			case JsonValueKind.Array:
				tickets = [.. root.EnumerateArray().Select(element => ReadTicket(element, options))];

				if (tickets.Count == 0)
				{
					throw new JsonException("Halo returned an empty array for a ticket create, so no ticket id can be read.");
				}

				break;
			case JsonValueKind.Object:
				tickets = [ReadTicket(UnwrapTicket(root), options)];
				break;
			default:
				throw new JsonException($"Expected an array or an object for {nameof(CreateTicketResponse)}, but found {root.ValueKind}.");
		}

		return new CreateTicketResponse { Ticket = tickets[0], Tickets = tickets };
	}

	public override void Write(Utf8JsonWriter writer, CreateTicketResponse value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize(writer, value.Ticket, options);

	private static JsonElement UnwrapTicket(JsonElement root)
	{
		foreach (var property in root.EnumerateObject())
		{
			if (string.Equals(property.Name, "ticket", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Object)
			{
				return property.Value;
			}
		}

		return root;
	}

	private static Ticket ReadTicket(JsonElement element, JsonSerializerOptions options)
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

			throw new JsonException(UnreadableMessage(id), ex);
		}
	}
}

/// <summary>
/// Response for ticket update operations
/// </summary>
public record UpdateTicketResponse
{
	/// <summary>
	/// The updated ticket
	/// </summary>
	public required Ticket Ticket { get; init; }

	/// <summary>
	/// Whether the update was successful
	/// </summary>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the update
	/// </summary>
	public IReadOnlyList<string>? Messages { get; init; }

	/// <summary>
	/// The ID of the updated ticket
	/// </summary>
	public int TicketId => Ticket.Id;
}