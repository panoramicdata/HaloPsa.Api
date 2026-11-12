using Refit;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

public partial record TicketFilter
{
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
}
