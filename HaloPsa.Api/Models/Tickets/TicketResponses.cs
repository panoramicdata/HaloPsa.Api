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
