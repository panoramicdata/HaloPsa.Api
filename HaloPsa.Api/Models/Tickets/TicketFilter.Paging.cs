using Refit;

namespace HaloPsa.Api.Models.Tickets;

public partial record TicketFilter
{
	/// <summary>
	/// Number of tickets to return (default: 50, max: 1000)
	/// </summary>
	[AliasAs("count")]
	public int? Count { get; init; }

	/// <summary>
	/// Page number for pagination (when using pageinate=true)
	/// </summary>
	[AliasAs("page_no")]
	public int? PageNo { get; init; }

	/// <summary>
	/// Page size for pagination (when using pageinate=true). Halo's maximum is 100.
	/// </summary>
	[AliasAs("page_size")]
	public int? PageSize { get; init; }

	/// <summary>
	/// Whether to use pagination
	/// </summary>
	[AliasAs("pageinate")]
	public bool? Paginate { get; init; }
}
