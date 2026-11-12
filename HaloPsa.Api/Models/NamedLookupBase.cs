using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models;

/// <summary>
/// Properties shared by Halo's ordered, colour-coded lookup records such as categories and teams
/// </summary>
public abstract record NamedLookupBase
{
	/// <summary>
	/// The unique identifier
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The color associated with this record
	/// </summary>
	[JsonPropertyName("colour")]
	public string? Color { get; init; }

	/// <summary>
	/// Whether this record is inactive
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The order in which this record appears in lists
	/// </summary>
	[JsonPropertyName("order")]
	public int Order { get; init; }
}
