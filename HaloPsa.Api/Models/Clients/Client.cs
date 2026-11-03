using HaloPsa.Api.Models.CustomFields;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Clients;

/// <summary>
/// Represents a client entity in the Halo system
/// </summary>
public record Client
{
	/// <summary>
	/// The unique identifier for the client
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name of the client
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Whether the client is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The client's top-level status
	/// </summary>
	[JsonPropertyName("toplevel_id")]
	public int? TopLevelId { get; init; }

	/// <summary>
	/// The client's website URL
	/// </summary>
	[JsonPropertyName("website")]
	public string? Website { get; init; }

	/// <summary>
	/// The client's notes
	/// </summary>
	[JsonPropertyName("notes")]
	public string? Notes { get; init; }

	/// <summary>
	/// The custom field values on this client
	/// </summary>
	/// <remarks>
	/// A list read returns only the custom fields asked for with <see cref="ClientFilter.IncludeCustomFields"/>; a read by id returns them when <c>includeDetails</c> is true.
	/// </remarks>
	[JsonPropertyName("customfields")]
	public IReadOnlyList<CustomFieldValue>? CustomFields { get; init; }
}