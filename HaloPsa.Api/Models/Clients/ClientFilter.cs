using Refit;

namespace HaloPsa.Api.Models.Clients;

/// <summary>
/// Filter options for client queries
/// </summary>
/// <remarks>
/// Every property is sent under the query parameter name Halo itself documents (the <c>[AliasAs]</c> value),
/// because Halo binds parameters case-insensitively but not across underscores.
/// </remarks>
public record ClientFilter
{
	/// <summary>
	/// Number of clients to return when not paginating
	/// </summary>
	[AliasAs("count")]
	public int? Count { get; init; }

	/// <summary>
	/// Whether to use pagination
	/// </summary>
	/// <remarks>
	/// Without pagination Halo caps the list it returns, however many clients exist.
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
	/// Whether to include inactive clients
	/// </summary>
	[AliasAs("includeinactive")]
	public bool? IncludeInactive { get; init; }

	/// <summary>
	/// Custom fields to include in the response, as a comma-separated list of custom field IDs
	/// </summary>
	[AliasAs("include_custom_fields")]
	public string? IncludeCustomFields { get; init; }

	/// <summary>
	/// Filter by clients matching this search string
	/// </summary>
	[AliasAs("search")]
	public string? Search { get; init; }

	/// <summary>
	/// Whether to exclude the internal client
	/// </summary>
	[AliasAs("exclude_internal")]
	public bool? ExcludeInternal { get; init; }
}
