using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Categories;

/// <summary>
/// Represents a ticket category in the Halo system
/// </summary>
public record Category : NamedLookupBase
{
	/// <summary>
	/// The parent category ID if this is a sub-category
	/// </summary>
	[JsonPropertyName("parentid")]
	public int? ParentId { get; init; }

	/// <summary>
	/// Whether this category requires approval
	/// </summary>
	[JsonPropertyName("requiresapproval")]
	public bool RequiresApproval { get; init; }

	/// <summary>
	/// The ticket type ID this category belongs to
	/// </summary>
	[JsonPropertyName("tickettype_id")]
	public int? TicketTypeId { get; init; }

	/// <summary>
	/// The team ID assigned to this category
	/// </summary>
	[JsonPropertyName("team_id")]
	public int? TeamId { get; init; }

	/// <summary>
	/// The agent ID assigned to this category
	/// </summary>
	[JsonPropertyName("agent_id")]
	public int? AgentId { get; init; }
}
