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
	/// The number of sites reported by this request.
	/// </summary>
	/// <remarks>
	/// <para>What it counts depends on whether the request paginated. Without <c>pageinate=true</c> it is the
	/// size of the list that came back, NOT the number of sites in the system - asking for <c>count=3</c>
	/// against a tenant holding 60 sites returns <c>record_count: 3</c>.</para>
	/// <para>With <c>pageinate=true</c> it is the TOTAL number of matching sites, repeated on every page
	/// (checked against a live tenant on 2026-10-06: 62 sites gave <c>record_count: 62</c> for page sizes of
	/// 60, 100 and 500 alike, and 0 for a page past the end). <c>SitesApiWrapper.GetAllPagedAsync</c> relies on
	/// this to know when it has collected everything.</para>
	/// </remarks>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}