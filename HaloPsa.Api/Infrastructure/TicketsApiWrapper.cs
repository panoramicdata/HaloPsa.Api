using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Wrapper for the Tickets API that provides convenience methods
/// </summary>
/// <param name="ticketsApi">The underlying Tickets API</param>
/// <param name="readOnly">
/// When <see langword="true"/>, every write refuses before a request is sent. <see langword="null"/>
/// - the default - means read/write, so existing callers are unaffected. See
/// <see cref="HaloClientOptions.ReadOnly"/>.
/// </param>
/// <remarks>
/// The read-only check lives here, on the wrapper, rather than in each caller. Every write to a
/// ticket passes through this one type, so this is the only place it can be enforced completely -
/// and enforcement that can be bypassed by calling a different overload is not enforcement.
/// </remarks>
public class TicketsApiWrapper(ITicketsApi ticketsApi, bool? readOnly = null) : ITicketsApi
{
	/// <summary>
	/// Gets whether this wrapper refuses writes.
	/// </summary>
	public bool IsReadOnly => readOnly == true;

	/// <inheritdoc />
	public Task<TicketsResponse> GetAllAsync(TicketFilter? filter, CancellationToken cancellationToken)
		=> ticketsApi.GetAllAsync(filter, cancellationToken);

	/// <inheritdoc />
	public Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken)
		=> ticketsApi.GetAllAsync(cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
		=> ticketsApi.GetByIdAsync(id, includeDetails, cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken)
		=> ticketsApi.GetByIdAsync(id, cancellationToken);

	/// <inheritdoc />
	public Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CreateAsync));

		return ticketsApi.CreateAsync(request, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(UpdateAsync));

		return ticketsApi.UpdateAsync(id, request, cancellationToken);
	}

	/// <inheritdoc />
	public Task DeleteAsync(int id, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(DeleteAsync));

		return ticketsApi.DeleteAsync(id, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> CloseAsync(int id, string resolution, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CloseAsync));

		return ticketsApi.CloseAsync(id, resolution, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> CloseAsync(int id, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CloseAsync));

		return ticketsApi.CloseAsync(id, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> ReopenAsync(int id, string reason, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(ReopenAsync));

		return ticketsApi.ReopenAsync(id, reason, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> ReopenAsync(int id, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(ReopenAsync));

		return ticketsApi.ReopenAsync(id, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(AssignAsync));

		return ticketsApi.AssignAsync(id, agentId, cancellationToken);
	}

	/// <inheritdoc />
	public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, int teamId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(AssignAsync));

		return ticketsApi.AssignAsync(id, agentId, teamId, cancellationToken);
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
		if (readOnly == true)
		{
			throw new InvalidOperationException(
				$"This HaloPSA client is configured read-only, so {operation} was refused and no "
				+ "request was sent. Set HaloClientOptions.ReadOnly to false (or leave it null) to "
				+ "permit writes.");
		}
	}
}
