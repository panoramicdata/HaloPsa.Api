using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Categories;

/// <summary>
/// Represents a ticket category in the Halo system
/// </summary>
public record Category
{
	/// <summary>
	/// The unique identifier for the category
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name of the category
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description of the category
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The color associated with this category
	/// </summary>
	[JsonPropertyName("colour")]
	public string? Color { get; init; }

	/// <summary>
	/// Whether this category is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The parent category ID if this is a sub-category
	/// </summary>
	[JsonPropertyName("parentid")]
	public int? ParentId { get; init; }

	/// <summary>
	/// The order in which this category appears in lists
	/// </summary>
	[JsonPropertyName("order")]
	public int Order { get; init; }

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
