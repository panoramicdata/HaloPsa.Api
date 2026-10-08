using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Represents a ticket in the Halo system
/// </summary>
public record Ticket
{
	/// <summary>
	/// The unique identifier for the ticket
	/// </summary>
	[JsonPropertyName("id")]
	public required int Id { get; init; }

	/// <summary>
	/// The ticket summary/subject
	/// </summary>
	[JsonPropertyName("summary")]
	public required string Summary { get; init; }

	/// <summary>
	/// The detailed description of the ticket
	/// </summary>
	[JsonPropertyName("details")]
	public string? Details { get; init; }

	/// <summary>
	/// The current status ID of the ticket
	/// </summary>
	[JsonPropertyName("status_id")]
	public required int Status { get; init; }

	/// <summary>
	/// The status name for display purposes
	/// </summary>
	[JsonPropertyName("status_name")]
	public string? StatusName { get; init; }

	/// <summary>
	/// The priority level of the ticket
	/// </summary>
	[JsonPropertyName("priority_id")]
	public int Priority { get; init; } = 1;

	/// <summary>
	/// The priority name for display purposes
	/// </summary>
	[JsonPropertyName("priority_name")]
	public string? PriorityName { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to
	/// </summary>
	[JsonPropertyName("client_id")]
	public required int ClientId { get; init; }

	/// <summary>
	/// The name of the client for display purposes
	/// </summary>
	[JsonPropertyName("client_name")]
	public string? ClientName { get; init; }

	/// <summary>
	/// The ID of the site this ticket belongs to
	/// </summary>
	[JsonPropertyName("site_id")]
	public int? SiteId { get; init; }

	/// <summary>
	/// The name of the site for display purposes
	/// </summary>
	[JsonPropertyName("site_name")]
	public string? SiteName { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket
	/// </summary>
	[JsonPropertyName("user_id")]
	public required int UserId { get; init; }

	/// <summary>
	/// The name of the user who reported this ticket
	/// </summary>
	[JsonPropertyName("user_name")]
	public string? UserName { get; init; }

	/// <summary>
	/// The email address of the user who reported this ticket
	/// </summary>
	[JsonPropertyName("user_email")]
	public string? UserEmail { get; init; }

	/// <summary>
	/// The ID of the agent assigned to this ticket
	/// </summary>
	[JsonPropertyName("agent_id")]
	public int? AgentId { get; init; }

	/// <summary>
	/// The name of the agent assigned to this ticket
	/// </summary>
	[JsonPropertyName("agent_name")]
	public string? AgentName { get; init; }

	/// <summary>
	/// The ID of the team assigned to this ticket
	/// </summary>
	[JsonPropertyName("team_id")]
	public int? TeamId { get; init; }

	/// <summary>
	/// The name of the team assigned to this ticket
	/// </summary>
	[JsonPropertyName("team")]
	public string? TeamName { get; init; }

	/// <summary>
	/// The first-level category ID of the ticket
	/// </summary>
	[JsonPropertyName("categoryid_1")]
	public int? CategoryId { get; init; }

	/// <summary>
	/// The category name for display purposes
	/// </summary>
	[JsonPropertyName("category_1")]
	public string? CategoryName { get; init; }

	/// <summary>
	/// The ticket type ID
	/// </summary>
	[JsonPropertyName("tickettype_id")]
	public int? TicketTypeId { get; init; }

	/// <summary>
	/// The ticket type name for display purposes
	/// </summary>
	[JsonPropertyName("tickettype_name")]
	public string? TicketTypeName { get; init; }

	/// <summary>
	/// When the ticket occurred/was created
	/// </summary>
	[JsonPropertyName("dateoccurred")]
	public DateTime DateOccurred { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// When the ticket was last updated
	/// </summary>
	[JsonPropertyName("lastupdate")]
	public DateTime? LastUpdate { get; init; }

	/// <summary>
	/// When the ticket was closed (if applicable)
	/// </summary>
	[JsonPropertyName("dateclosed")]
	public DateTime? DateClosed { get; init; }

	/// <summary>
	/// The source of the ticket (email, portal, phone, etc.)
	/// </summary>
	[JsonPropertyName("source")]
	public int? Source { get; init; }

	/// <summary>
	/// SLA ID associated with this ticket
	/// </summary>
	[JsonPropertyName("sla_id")]
	public int? SlaId { get; init; }

	/// <summary>
	/// Whether the ticket is closed
	/// </summary>
	public bool IsClosed => DateClosed.HasValue;

	/// <summary>
	/// Whether the ticket is on hold
	/// </summary>
	[JsonPropertyName("onhold")]
	public bool IsOnHold { get; init; }

	/// <summary>
	/// Custom fields for the ticket (as raw JSON object)
	/// </summary>
	[JsonPropertyName("customfields")]
	public object? CustomFields { get; init; }

	/// <summary>
	/// List of assets associated with this ticket
	/// </summary>
	public IReadOnlyList<int>? AssetIds { get; init; }

	/// <summary>
	/// Tags associated with this ticket
	/// </summary>
	public IReadOnlyList<string>? Tags { get; init; }

	/// <summary>
	/// Every field Halo returned that this record does not model, such as the SLA dates and state
	/// and <c>timetaken</c>.
	/// </summary>
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}