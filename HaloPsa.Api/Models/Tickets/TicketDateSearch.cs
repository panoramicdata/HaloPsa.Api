namespace HaloPsa.Api.Models.Tickets;

/// <summary>
/// Values for <see cref="TicketFilter.DateSearch"/>.
/// </summary>
/// <remarks>
/// These are Halo's database column names, as given in its API specification, which is why the
/// opened date is spelt "dateoccured" while the ticket JSON property is "dateoccurred".
/// </remarks>
public static class TicketDateSearch
{
	/// <summary>
	/// The date the ticket was opened.
	/// </summary>
	public const string DateOccurred = "dateoccured";

	/// <summary>
	/// The date the ticket was closed.
	/// </summary>
	public const string DateClosed = "datecleared";
}
