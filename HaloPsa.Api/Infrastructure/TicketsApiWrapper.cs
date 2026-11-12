using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Wrapper for the Tickets API that provides convenience methods
/// </summary>
/// <param name="ticketsApi">The underlying Tickets API</param>
/// <param name="readOnly">
/// When <see langword="true"/>, every write refuses before a request is sent. <see langword="null"/>
/// - the default - means read/write, so existing callers are unaffected. See
/// <see cref="HaloClientOptions.IsReadOnly"/>.
/// </param>
/// <remarks>
/// The read-only check lives here, on the wrapper, rather than in each caller. Every write to a
/// ticket passes through this one type, so this is the only place it can be enforced completely -
/// and enforcement that can be bypassed by calling a different overload is not enforcement.
/// </remarks>
public partial class TicketsApiWrapper(ITicketsApi ticketsApi, bool? readOnly = null) : ITicketsApi
{
	private readonly ITicketsApi _ticketsApi = ticketsApi;

	private readonly bool? _readOnly = readOnly;

	/// <summary>
	/// Gets whether this wrapper refuses writes.
	/// </summary>
	public bool IsReadOnly => _readOnly == true;

	/// <inheritdoc />
	public Task<TicketsResponse> GetAllAsync(TicketFilter? filter, CancellationToken cancellationToken)
		=> _ticketsApi.GetAllAsync(WithDefaultDateSearch(filter), cancellationToken);

	// Halo applies startdate/enddate to whichever column datesearch names, so dates without it
	// would not mean "created", as TicketFilter documents.
	internal static TicketFilter? WithDefaultDateSearch(TicketFilter? filter)
		=> filter is { DateSearch: null } && (filter.StartDate.HasValue || filter.EndDate.HasValue)
			? filter with { DateSearch = TicketDateSearch.DateOccurred }
			: filter;

	/// <inheritdoc />
	public Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken)
		=> _ticketsApi.GetAllAsync(cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
		=> _ticketsApi.GetByIdAsync(id, includeDetails, cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken)
		=> _ticketsApi.GetByIdAsync(id, cancellationToken);

	/// <inheritdoc />
	public Task DeleteAsync(int id, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(DeleteAsync));
		TicketRequestValidator.ThrowIfNotATicketId(id, nameof(id));

		return _ticketsApi.DeleteAsync(id, cancellationToken);
	}

	/// <inheritdoc />
	public Task DeleteAsync(int id, string reason, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(DeleteAsync));
		TicketRequestValidator.ThrowIfNotATicketId(id, nameof(id));

		if (string.IsNullOrWhiteSpace(reason))
		{
			throw new ArgumentException("The reason must not be blank; no request was sent.", nameof(reason));
		}

		return _ticketsApi.DeleteAsync(id, reason, cancellationToken);
	}

	/// <summary>
	/// Refuses a write when this wrapper is read-only.
	/// </summary>
	/// <remarks>
	/// Thrown rather than returned silently: a caller that believes it wrote a ticket and did not is
	/// worse off than one that sees an error, because it will report success to whoever asked.
	/// </remarks>
	private void ThrowIfReadOnly(string operation)
	{
		if (_readOnly == true)
		{
			throw new InvalidOperationException(
				$"This HaloPSA client is configured read-only, so {operation} was refused and no "
				+ "request was sent. Set HaloClientOptions.IsReadOnly to false to "
				+ "permit writes.");
		}
	}
}
