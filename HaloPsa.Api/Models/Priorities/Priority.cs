using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Priorities;

/// <summary>
/// Represents a ticket priority in the Halo system
/// </summary>
public record Priority
{
	/// <summary>
	/// The unique identifier of this priority record. Halo keeps one record per SLA, so this is a
	/// GUID; ticket filters and <c>priority_id</c> on a ticket use <see cref="PriorityId"/>.
	/// </summary>
	[JsonPropertyName("id")]
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// The priority id that tickets carry and ticket filters take. Shared by the same priority
	/// level across SLAs, so it is not unique within a list of priorities.
	/// </summary>
	[JsonPropertyName("priorityid")]
	public int PriorityId { get; init; }

	/// <summary>
	/// The SLA this priority record belongs to
	/// </summary>
	[JsonPropertyName("slaid")]
	public int SlaId { get; init; }

	/// <summary>
	/// Whether this priority is hidden
	/// </summary>
	[JsonPropertyName("ishidden")]
	public bool IsHidden { get; init; }

	/// <summary>
	/// The name of the priority
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description of the priority
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The color associated with this priority
	/// </summary>
	[JsonPropertyName("colour")]
	public string? Color { get; init; }

	/// <summary>
	/// The numerical priority value (lower = higher priority)
	/// </summary>
	[JsonPropertyName("priorityvalue")]
	public int PriorityValue { get; init; }

	/// <summary>
	/// Whether this priority is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// Whether this is the default priority
	/// </summary>
	[JsonPropertyName("isdefault")]
	public bool IsDefault { get; init; }

	/// <summary>
	/// The order in which this priority appears in lists
	/// </summary>
	[JsonPropertyName("order")]
	public int Order { get; init; }
}