using Refit;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Filter options for ticket queries
/// </summary>
/// <remarks>
/// Every property is sent under the parameter name in Halo's API specification. Halo ignores a
/// parameter it does not recognise and returns unfiltered results, so a missing alias fails
/// silently; <c>TicketFilterQueryStringTests</c> pins each one.
/// </remarks>
public record TicketFilter
{
	private const string HaloDateFormat = "yyyy-MM-ddTHH:mm:ss";

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
	/// Only return open tickets
	/// </summary>
	[AliasAs("open_only")]
	public bool? OpenOnly { get; init; }

	/// <summary>
	/// Only return closed tickets
	/// </summary>
	[AliasAs("closed_only")]
	public bool? ClosedOnly { get; init; }

	/// <summary>
	/// Only return tickets assigned to me
	/// </summary>
	[AliasAs("mine")]
	public bool? MyTickets { get; init; }

	/// <summary>
	/// Not supported: Halo has no unassigned-only parameter, so this is never sent.
	/// </summary>
	[Obsolete("Halo has no unassigned-only ticket parameter; this value is never sent.")]
	[JsonIgnore]
	public bool? UnassignedOnly { get; init; }

	/// <summary>
	/// Not supported on the ticket list: this is never sent.
	/// </summary>
	[Obsolete("Halo's ticket list has no includedetails parameter; this value is never sent. Use GetByIdAsync with includeDetails.")]
	[JsonIgnore]
	public bool? IncludeDetails { get; init; }

	/// <summary>
	/// Not supported: Halo's <c>includechildren</c> is a 0/1/2 filter, not a flag, so this is never sent.
	/// </summary>
	[Obsolete("Halo's includechildren is a 0/1/2 filter, not a flag; this value is never sent.")]
	[JsonIgnore]
	public bool? IncludeChildren { get; init; }

	/// <summary>
	/// The field to order by (first order)
	/// </summary>
	[AliasAs("order")]
	public string? Order { get; init; }

	/// <summary>
	/// Whether to order descending (first order)
	/// </summary>
	[AliasAs("orderdesc")]
	public bool? OrderDesc { get; init; }

	/// <summary>
	/// The field to order by (second order)
	/// </summary>
	[AliasAs("order2")]
	public string? Order2 { get; init; }

	/// <summary>
	/// Whether to order descending (second order)
	/// </summary>
	[AliasAs("orderdesc2")]
	public bool? OrderDesc2 { get; init; }

	/// <summary>
	/// Advanced search criteria (JSON string)
	/// </summary>
	[AliasAs("advanced_search")]
	public string? AdvancedSearch { get; init; }

	/// <summary>
	/// Filter by asset ID
	/// </summary>
	[AliasAs("asset_id")]
	public int? AssetId { get; init; }

	/// <summary>
	/// Filter by service ID
	/// </summary>
	[AliasAs("service_id")]
	public int? ServiceId { get; init; }

	/// <summary>
	/// Filter by supplier ID
	/// </summary>
	[AliasAs("supplier_id")]
	public int? SupplierId { get; init; }

	/// <summary>
	/// Filter by contract ID
	/// </summary>
	[AliasAs("contract_id")]
	public int? ContractId { get; init; }

	/// <summary>
	/// Include custom fields in the response (comma-separated field IDs)
	/// </summary>
	[AliasAs("include_custom_fields")]
	public string? IncludeCustomFields { get; init; }

	/// <summary>
	/// Include the SLA timer fields in the response
	/// </summary>
	[AliasAs("includeslatimer")]
	public bool? IncludeSlaTimer { get; init; }

	/// <summary>
	/// Include time taken in the response
	/// </summary>
	[AliasAs("includetimetaken")]
	public bool? IncludeTimeTaken { get; init; }

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
