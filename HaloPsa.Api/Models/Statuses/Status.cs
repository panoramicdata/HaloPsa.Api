using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Statuses;

/// <summary>
/// Represents a ticket status in the Halo system
/// </summary>
public record Status
{
	/// <summary>
	/// The unique identifier for the status
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name of the status
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description of the status
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The color associated with this status
	/// </summary>
	[JsonPropertyName("colour")]
	public string? Color { get; init; }

	/// <summary>
	/// Halo's status type, the <c>type</c> key of its Status schema; the swagger does not say what each value means
	/// </summary>
	[JsonPropertyName("type")]
	public int? Type { get; init; }

	/// <summary>
	/// Whether this status represents a closed ticket
	/// </summary>
	/// <remarks>
	/// Halo's Status schema has no <c>isclosed</c> key, so this stays false unless an installation sends one; see <see cref="Type"/>.
	/// </remarks>
	[JsonPropertyName("isclosed")]
	public bool IsClosed { get; init; }

	/// <summary>
	/// Whether this status is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// Whether this is the default status
	/// </summary>
	[JsonPropertyName("isdefault")]
	public bool IsDefault { get; init; }

	/// <summary>
	/// The order in which this status appears in lists
	/// </summary>
	[JsonPropertyName("order")]
	public int Order { get; init; }

	/// <summary>
	/// Whether this status requires approval
	/// </summary>
	[JsonPropertyName("requiresapproval")]
	public bool RequiresApproval { get; init; }

	/// <summary>
	/// Whether this status represents an awaiting status
	/// </summary>
	[JsonPropertyName("isawaiting")]
	public bool IsAwaiting { get; init; }
}