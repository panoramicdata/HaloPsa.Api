using Refit;

namespace HaloPsa.Api.Models.Users;

/// <summary>
/// Filter options for user queries
/// </summary>
/// <remarks>
/// Every property is sent under the query parameter name Halo itself documents (the <c>[AliasAs]</c> value),
/// because Halo binds parameters case-insensitively but not across underscores.
/// </remarks>
public record UserFilter
{
	/// <summary>
	/// Whether to use pagination
	/// </summary>
	/// <remarks>
	/// Without pagination Halo caps the list it returns, however many users exist.
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
	/// Filter by users matching this search string
	/// </summary>
	[AliasAs("search")]
	public string? Search { get; init; }

	/// <summary>
	/// Filter by users belonging to this client
	/// </summary>
	[AliasAs("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// Whether to include inactive users
	/// </summary>
	[AliasAs("includeinactive")]
	public bool? IncludeInactive { get; init; }

	/// <summary>
	/// Whether to exclude users who are agents
	/// </summary>
	[AliasAs("exclude_agents")]
	public bool? ExcludeAgents { get; init; }
}
