using Refit;

namespace HaloPsa.Api.Models.Tickets;

public partial record TicketFilter
{
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
}
