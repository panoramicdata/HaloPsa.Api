using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Sites;

/// <summary>
/// Response wrapper for site list operations
/// </summary>
public record SitesResponse
{
	/// <summary>
	/// The list of sites
	/// </summary>
	[JsonPropertyName("sites")]
	public IReadOnlyList<Site> Sites { get; init; } = [];

	/// <summary>
	/// The number of sites returned by this request.
	/// </summary>
	/// <remarks>
	/// This is the size of the page that came back, NOT the number of sites in the system - asking for
	/// <c>count=3</c> against a tenant holding 60 sites returns <c>record_count: 3</c>. To count the sites
	/// in a tenant, request them all and count the list.
	/// </remarks>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}