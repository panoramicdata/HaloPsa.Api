namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Request model for creating a new ticket
/// </summary>
public record CreateTicketRequest
{
	/// <summary>
	/// The ticket summary/subject (required)
	/// </summary>
	public required string Summary { get; init; }

	/// <summary>
	/// The detailed description of the ticket
	/// </summary>
	public string? Details { get; init; }

	/// <summary>
	/// The initial status of the ticket (defaults to system default)
	/// </summary>
	public int? Status { get; init; }

	/// <summary>
	/// The priority level of the ticket (defaults to 1 - Low)
	/// </summary>
	public int? Priority { get; init; }

	/// <summary>
	/// The ID of the client this ticket belongs to (required)
	/// </summary>
	public required int ClientId { get; init; }

	/// <summary>
	/// The ID of the site this ticket belongs to
	/// </summary>
	public int? SiteId { get; init; }

	/// <summary>
	/// The ID of the user who reported this ticket (required)
	/// </summary>
	public required int UserId { get; init; }

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
	/// When the ticket occurred (defaults to current time)
	/// </summary>
	public DateTime? DateOccurred { get; init; }

	/// <summary>
	/// The source of the ticket (email, portal, phone, etc.)
	/// </summary>
	public int? Source { get; init; }

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
	/// Additional notes to add during creation
	/// </summary>
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
