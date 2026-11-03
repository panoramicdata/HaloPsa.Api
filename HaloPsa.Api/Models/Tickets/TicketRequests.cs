using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.CustomFields;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Request model for creating a new ticket
/// </summary>
public record CreateTicketRequest
{
	/// <summary>
	/// The ticket summary/subject (required)
	/// </summary>
	[JsonPropertyName("summary")]
	public required string Summary { get; init; }

	/// <summary>
	/// The detailed description of the ticket
	/// </summary>
	[JsonPropertyName("details")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; init; }

	/// <summary>
	/// The detailed description of the ticket, as HTML
	/// </summary>
	[JsonPropertyName("details_html")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DetailsHtml { get; init; }

	/// <summary>
	/// The initial status of the ticket (defaults to system default)
	/// </summary>
	[JsonPropertyName("status_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Status { get; init; }

	/// <summary>
	/// The priority level of the ticket (defaults to 1 - Low)
	/// </summary>
	[JsonPropertyName("priority_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Priority { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to (required)
	/// </summary>
	[JsonPropertyName("client_id")]
	public required int ClientId { get; init; }

	/// <summary>
	/// The ID of the site this ticket belongs to
	/// </summary>
	[JsonPropertyName("site_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? SiteId { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket (required)
	/// </summary>
	[JsonPropertyName("user_id")]
	public required int UserId { get; init; }

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
public record UpdateTicketRequest
{
	/// <summary>
	/// The ticket summary/subject
	/// </summary>
	public string? Summary { get; init; }

	/// <summary>
	/// The detailed description of the ticket
	/// </summary>
	public string? Details { get; init; }

	/// <summary>
	/// The status of the ticket
	/// </summary>
	public int? Status { get; init; }

	/// <summary>
	/// The priority level of the ticket
	/// </summary>
	public int? Priority { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to
	/// </summary>
	public int? ClientId { get; init; }

	/// <summary>
	/// The ID of the site this ticket belongs to
	/// </summary>
	public int? SiteId { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket
	/// </summary>
	public int? UserId { get; init; }

	/// <summary>
	/// The ID of the agent to assign this ticket to
	/// </summary>
	public int? AgentId { get; init; }

	/// <summary>
	/// The ID of the team to assign this ticket to
	/// </summary>
	public int? TeamId { get; init; }

	/// <summary>
	/// The category of the ticket
	/// </summary>
	public int? CategoryId { get; init; }

	/// <summary>
	/// The ticket type ID
	/// </summary>
	public int? TicketTypeId { get; init; }

	/// <summary>
	/// Custom fields for the ticket
	/// </summary>
	public Dictionary<string, object?>? CustomFields { get; init; }

	/// <summary>
	/// List of asset IDs to associate with this ticket
	/// </summary>
	public IReadOnlyList<int>? AssetIds { get; init; }

	/// <summary>
	/// Tags to associate with this ticket
	/// </summary>
	public IReadOnlyList<string>? Tags { get; init; }

	/// <summary>
	/// Whether to suppress email notifications
	/// </summary>
	public bool? SuppressEmails { get; init; }

	/// <summary>
	/// Additional notes to add during update
	/// </summary>
	public string? Notes { get; init; }

	/// <summary>
	/// Close the ticket with this update
	/// </summary>
	public bool? CloseTicket { get; init; }

	/// <summary>
	/// Resolution summary when closing the ticket
	/// </summary>
	public string? Resolution { get; init; }
}
