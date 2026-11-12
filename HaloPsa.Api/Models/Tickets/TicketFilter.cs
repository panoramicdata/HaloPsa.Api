using Refit;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Filter options for ticket queries
/// </summary>
/// <remarks>
/// Every property is sent under the parameter name in Halo's API specification. Halo ignores a
/// parameter it does not recognise and returns unfiltered results, so a missing alias fails
/// silently; <c>TicketFilterQueryStringTests</c> pins each one.
/// </remarks>
public partial record TicketFilter
{
	private const string HaloDateFormat = "yyyy-MM-ddTHH:mm:ss";

	/// <summary>
	/// Filter by ticket status IDs (comma-separated or single value)
	/// </summary>
	[AliasAs("status")]
	public string? Status { get; init; }

	/// <summary>
	/// Filter by priority IDs (comma-separated or single value). These are the <c>priorityid</c>
	/// values, not the per-SLA priority record ids.
	/// </summary>
	[AliasAs("priority")]
	public string? Priority { get; init; }

	/// <summary>
	/// Filter by client ID
	/// </summary>
	[AliasAs("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// Filter by site ID
	/// </summary>
	[AliasAs("site_id")]
	public int? SiteId { get; init; }

	/// <summary>
	/// Filter by user ID
	/// </summary>
	[AliasAs("user_id")]
	public int? UserId { get; init; }

	/// <summary>
	/// Filter by assigned agent ID
	/// </summary>
	[AliasAs("agent_id")]
	public int? AgentId { get; init; }

	/// <summary>
	/// Filter by team ID
	/// </summary>
	[AliasAs("team")]
	public int? TeamId { get; init; }

	/// <summary>
	/// Filter by first-level category ID
	/// </summary>
	[AliasAs("category_1")]
	public int? CategoryId { get; init; }

	/// <summary>
	/// Filter by ticket type ID (Halo calls this the request type)
	/// </summary>
	[AliasAs("requesttype_id")]
	public int? TicketTypeId { get; init; }

	/// <summary>
	/// Search string to filter tickets
	/// </summary>
	[AliasAs("search")]
	public string? Search { get; init; }

	/// <summary>
	/// Search string matched against the ticket summary only
	/// </summary>
	[AliasAs("search_summary")]
	public string? SearchSummary { get; init; }

	/// <summary>
	/// The date field <see cref="StartDate"/> and <see cref="EndDate"/> apply to; see
	/// <see cref="TicketDateSearch"/>. When either date is set and this is not, the wrapper searches
	/// the opened date.
	/// </summary>
	[AliasAs("datesearch")]
	public string? DateSearch { get; init; }

	/// <summary>
	/// Start of the <see cref="DateSearch"/> range
	/// </summary>
	[AliasAs("startdate")]
	[Query(Format = HaloDateFormat)]
	public DateTime? StartDate { get; init; }

	/// <summary>
	/// End of the <see cref="DateSearch"/> range
	/// </summary>
	[AliasAs("enddate")]
	[Query(Format = HaloDateFormat)]
	public DateTime? EndDate { get; init; }

	/// <summary>
	/// Only return tickets that have been updated after this date
	/// </summary>
	[AliasAs("lastupdatefromdate")]
	[Query(Format = HaloDateFormat)]
	public DateTime? LastUpdateAfter { get; init; }

	/// <summary>
	/// Only return tickets that have been updated before this date
	/// </summary>
	[AliasAs("lastupdatetodate")]
	[Query(Format = HaloDateFormat)]
	public DateTime? LastUpdateBefore { get; init; }
}

