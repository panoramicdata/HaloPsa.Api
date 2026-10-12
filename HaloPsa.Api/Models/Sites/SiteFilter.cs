using Refit;

namespace HaloPsa.Api.Models.Sites;

/// <summary>
/// Filter options for site queries
/// </summary>
/// <remarks>
/// Every property is sent under the query parameter name Halo itself documents (the <c>[AliasAs]</c> value),
/// because Halo binds parameters case-insensitively but not across underscores - <c>clientId</c> is ignored
/// and the call returns every site, where <c>client_id</c> filters.
/// </remarks>
public record SiteFilter
{
	/// <summary>
	/// Number of sites to return when not paginating
	/// </summary>
	[AliasAs("count")]
	public int? Count { get; init; }

	/// <summary>
	/// Whether to use pagination
	/// </summary>
	/// <remarks>
	/// Without pagination Halo silently returns at most 50 sites, however many exist.
	/// Halo spells the parameter <c>pageinate</c>.
	/// </remarks>
	[AliasAs("pageinate")]
	public bool? Paginate { get; init; }

	/// <summary>
	/// Page number to return when paginating (starts at 1)
	/// </summary>
	[AliasAs("page_no")]
	public int? PageNo { get; init; }

	/// <summary>
	/// Page size to use when paginating
	/// </summary>
	[AliasAs("page_size")]
	public int? PageSize { get; init; }

	/// <summary>
	/// Filter by client ID
	/// </summary>
	[AliasAs("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// Whether to include inactive sites
	/// </summary>
	[AliasAs("includeinactive")]
	public bool? IncludeInactive { get; init; }

	/// <summary>
	/// Custom fields to include in the response, as a comma-separated list of custom field IDs
	/// </summary>
	[AliasAs("include_custom_fields")]
	public string? IncludeCustomFields { get; init; }

	/// <summary>
	/// Filter by sites matching this search string
	/// </summary>
	[AliasAs("search")]
	public string? Search { get; init; }
}
