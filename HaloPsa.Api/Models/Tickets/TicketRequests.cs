using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.CustomFields;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Properties shared by the ticket create and update requests
/// </summary>
public abstract record TicketRequestBase
{
	/// <summary>
	/// The detailed description of the ticket
	/// </summary>
	[JsonPropertyName("details")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; init; }

	/// <summary>
	/// The status of the ticket (on create, defaults to the system default)
	/// </summary>
	[JsonPropertyName("status_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Status { get; init; }

	/// <summary>
	/// The priority level of the ticket (on create, defaults to 1 - Low)
	/// </summary>
	[JsonPropertyName("priority_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Priority { get; init; }

	/// <summary>
	/// The ID of the site this ticket belongs to
	/// </summary>
	[JsonPropertyName("site_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? SiteId { get; init; }

	/// <summary>
	/// The ID of the agent to assign this ticket to
	/// </summary>
	[JsonPropertyName("agent_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? AgentId { get; init; }

	/// <summary>
	/// The ID of the team to assign this ticket to
	/// </summary>
	[JsonPropertyName("team_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? TeamId { get; init; }

	/// <summary>
	/// The category of the ticket
	/// </summary>
	[JsonPropertyName("categoryid_1")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? CategoryId { get; init; }

	/// <summary>
	/// The ticket type ID
	/// </summary>
	[JsonPropertyName("tickettype_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? TicketTypeId { get; init; }

	/// <summary>
	/// Custom field values to write, one per field id
	/// </summary>
	[JsonPropertyName("customfields")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<CustomFieldInput>? CustomFields { get; init; }

	/// <summary>
	/// List of asset IDs to associate with this ticket
	/// </summary>
	[JsonPropertyName("assets")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter(typeof(IdObjectListConverter))]
	public IReadOnlyList<int>? AssetIds { get; init; }

	/// <summary>
	/// Tags to associate with this ticket
	/// </summary>
	[JsonPropertyName("tags")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter(typeof(TagTextListConverter))]
	public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// Request model for creating a new ticket
/// </summary>
public record CreateTicketRequest : TicketRequestBase
{
	/// <summary>
	/// The ticket summary/subject (required)
	/// </summary>
	[JsonPropertyName("summary")]
	[JsonPropertyOrder(-1)]
	public required string Summary { get; init; }

	/// <summary>
	/// The detailed description of the ticket, as HTML
	/// </summary>
	[JsonPropertyName("details_html")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DetailsHtml { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to (required)
	/// </summary>
	[JsonPropertyName("client_id")]
	public required int ClientId { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket (required)
	/// </summary>
	[JsonPropertyName("user_id")]
	public required int UserId { get; init; }

	/// <summary>
	/// When the ticket occurred (defaults to current time)
	/// </summary>
	[JsonPropertyName("dateoccurred")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? DateOccurred { get; init; }

	/// <summary>
	/// The date the ticket is due to be resolved by
	/// </summary>
	[JsonPropertyName("targetdate")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? TargetDate { get; init; }

	/// <summary>
	/// The source of the ticket (email, portal, phone, etc.)
	/// </summary>
	[JsonPropertyName("source")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Source { get; init; }

	/// <summary>
	/// Not supported: Halo's ticket create has no such field, so this is never sent.
	/// </summary>
	[Obsolete("Halo's ticket create has no such field; this value is never sent.")]
	[JsonIgnore]
	public bool? SuppressEmails { get; init; }

	/// <summary>
	/// Not supported: Halo's ticket create has no such field, so this is never sent.
	/// </summary>
	[Obsolete("Halo's ticket create has no such field; this value is never sent.")]
	[JsonIgnore]
	public string? Notes { get; init; }
}

/// <summary>
/// Request model for updating an existing ticket
/// </summary>
public record UpdateTicketRequest : TicketRequestBase
{
	/// <summary>
	/// The ticket to update; required on ITicketsApi's list overload. TicketsApiWrapper.UpdateAsync(id, request) replaces it with its id argument.
	/// </summary>
	[JsonPropertyName("id")]
	[JsonPropertyOrder(-2)]
	public int Id { get; init; }

	/// <summary>
	/// The ticket summary/subject
	/// </summary>
	[JsonPropertyName("summary")]
	[JsonPropertyOrder(-1)]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Summary { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to
	/// </summary>
	[JsonPropertyName("client_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? ClientId { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket
	/// </summary>
	[JsonPropertyName("user_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? UserId { get; init; }

	/// <summary>
	/// The note to record when the ticket is closed
	/// </summary>
	[JsonPropertyName("closure_note")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ClosureNote { get; init; }

	/// <summary>
	/// Not supported: Halo's ticket update has no such field, so this is never sent.
	/// </summary>
	[Obsolete("Halo's ticket update has no such field; this value is never sent.")]
	[JsonIgnore]
	public bool? SuppressEmails { get; init; }

	/// <summary>
	/// Not supported: Halo's ticket update has no such field, so this is never sent.
	/// </summary>
	[Obsolete("Halo's ticket update has no such field; this value is never sent.")]
	[JsonIgnore]
	public string? Notes { get; init; }

	/// <summary>
	/// Not supported: Halo closes a ticket by its status, so this is never sent.
	/// </summary>
	[Obsolete("Halo closes a ticket by its status; set Status to a closed status id, or use TicketsApiWrapper.CloseAsync.")]
	[JsonIgnore]
	public bool? CloseTicket { get; init; }

	/// <summary>
	/// Not supported: Halo's field is <see cref="ClosureNote"/>, so this is never sent.
	/// </summary>
	[Obsolete("Halo's field is closure_note; use ClosureNote.")]
	[JsonIgnore]
	public string? Resolution { get; init; }
}
