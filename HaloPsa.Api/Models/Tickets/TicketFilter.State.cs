using Refit;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

public partial record TicketFilter
{
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
}
