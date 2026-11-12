using Refit;

namespace HaloPsa.Api.Models.Tickets;

public partial record TicketFilter
{
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
}
