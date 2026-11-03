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
		=> ticketsApi.GetAllAsync(WithDefaultDateSearch(filter), cancellationToken);

	// Halo applies startdate/enddate to whichever column datesearch names, so dates without it
	// would not mean "created", as TicketFilter documents.
	internal static TicketFilter? WithDefaultDateSearch(TicketFilter? filter)
		=> filter is { DateSearch: null } && (filter.StartDate.HasValue || filter.EndDate.HasValue)
			? filter with { DateSearch = TicketDateSearch.DateOccurred }
			: filter;

	/// <inheritdoc />
	public Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken)
		=> ticketsApi.GetAllAsync(cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
		=> ticketsApi.GetByIdAsync(id, includeDetails, cancellationToken);

	/// <inheritdoc />
	public Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken)
		=> ticketsApi.GetByIdAsync(id, cancellationToken);

	/// <summary>
	/// Creates a ticket, sent as the one-element array Halo's POST /api/Tickets takes
	/// </summary>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the ticket may have been created, so do not retry.
	/// The message names the ticket id when one could be read.
	/// </exception>
	public Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
		=> CreateAsync([request], cancellationToken);

	/// <inheritdoc />
	public Task<CreateTicketResponse> CreateAsync(IReadOnlyList<CreateTicketRequest> requests, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CreateAsync));
		ArgumentNullException.ThrowIfNull(requests);

		if (requests.Count == 0)
		{
			throw new ArgumentException("At least one ticket is required; no request was sent.", nameof(requests));
		}

		foreach (var request in requests)
		{
			ValidateForCreate(request);
		}

		return SendCreateAsync(requests, cancellationToken);
	}

	// An unreadable 2xx body arrives as an ApiException, which a caller would treat as an HTTP failure and retry into a duplicate ticket.
	private async Task<CreateTicketResponse> SendCreateAsync(IReadOnlyList<CreateTicketRequest> requests, CancellationToken cancellationToken)
	{
		try
		{
			return await ticketsApi.CreateAsync(requests, cancellationToken).ConfigureAwait(false)
				?? throw new JsonException(CreateTicketResponseConverter.UnreadableMessage("unknown"));
		}
		catch (Exception ex) when (ex.InnerException is JsonException inner)
		{
			var message = inner.Message.Contains(CreateTicketResponseConverter.MayHaveBeenCreated, StringComparison.Ordinal)
				? inner.Message
				: CreateTicketResponseConverter.UnreadableMessage("unknown");

			throw new JsonException(message, ex);
		}
	}

	// Halo rejects an id of 0 and uses -1 for "not set", so neither may reach it as a client, user or site.
	internal static void ValidateForCreate(CreateTicketRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		ThrowIfNotPositive(request.ClientId, nameof(request), "ClientId", "client");
		ThrowIfNotPositive(request.UserId, nameof(request), "UserId", "user");

		if (request.SiteId is { } siteId)
		{
			ThrowIfNotPositive(siteId, nameof(request), "SiteId", "site");
		}
	}

	/// <summary>
	/// Updates a ticket, sent as the one-element array Halo's POST /api/Tickets takes, with <paramref name="id"/> in the body
	/// </summary>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// The message names the ticket id.
	/// </exception>
	public Task<UpdateTicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(UpdateAsync));
		ArgumentNullException.ThrowIfNull(request);
		ThrowIfNotATicketId(id, nameof(id));
		ValidateForUpdate(request);

		return SendUpdateAsync([request with { Id = id }], cancellationToken);
	}

	/// <inheritdoc />
	Task<UpdateTicketResponse> ITicketsApi.UpdateAsync(IReadOnlyList<UpdateTicketRequest> requests, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(UpdateAsync));
		ArgumentNullException.ThrowIfNull(requests);

		if (requests.Count == 0)
		{
			throw new ArgumentException("At least one ticket is required; no request was sent.", nameof(requests));
		}

		foreach (var request in requests)
		{
			ArgumentNullException.ThrowIfNull(request);
			ThrowIfNotATicketId(request.Id, nameof(requests));
			ValidateForUpdate(request);
		}

		return SendUpdateAsync(requests, cancellationToken);
	}

	// An unreadable 2xx body arrives as an ApiException, which a caller would treat as an HTTP failure and retry although the update may have been applied.
	private async Task<UpdateTicketResponse> SendUpdateAsync(IReadOnlyList<UpdateTicketRequest> requests, CancellationToken cancellationToken)
	{
		var ids = string.Join(", ", requests.Select(request => request.Id));

		try
		{
			return await ticketsApi.UpdateAsync(requests, cancellationToken).ConfigureAwait(false)
				?? throw new JsonException(UpdateTicketResponseConverter.UnreadableMessage(ids));
		}
		catch (Exception ex) when (ex.InnerException is JsonException inner)
		{
			var message = inner.Message.Contains(UpdateTicketResponseConverter.MayHaveBeenApplied, StringComparison.Ordinal)
				? inner.Message
				: UpdateTicketResponseConverter.UnreadableMessage(ids);

			throw new JsonException(message, ex);
		}
	}

	private static void ThrowIfNotATicketId(int id, string paramName)
		=> ThrowIfNotPositive(id, paramName, "Ticket id", "ticket");

	// As for a create, a client, user or site that is set must be positive; an unset one is simply not sent.
	internal static void ValidateForUpdate(UpdateTicketRequest request)
	{
		if (request.ClientId is { } clientId)
		{
			ThrowIfNotPositive(clientId, nameof(request), "ClientId", "client");
		}

		if (request.UserId is { } userId)
		{
			ThrowIfNotPositive(userId, nameof(request), "UserId", "user");
		}

		if (request.SiteId is { } siteId)
		{
			ThrowIfNotPositive(siteId, nameof(request), "SiteId", "site");
		}
	}

	// Halo rejects an id of 0 and uses -1 for "not set", so neither may reach it as an id.
	private static void ThrowIfNotPositive(int value, string paramName, string label, string kind)
	{
		if (value <= 0)
		{
			throw new ArgumentOutOfRangeException(paramName, $"{label} must be a positive Halo {kind} id (was {value}); no request was sent.");
		}
	}

	/// <inheritdoc />
	public Task DeleteAsync(int id, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(DeleteAsync));

		return ticketsApi.DeleteAsync(id, cancellationToken);
	}

	/// <summary>
	/// Closes a ticket by updating it to a closed status; Halo has no close route
	/// </summary>
	/// <param name="id">The ticket id</param>
	/// <param name="closedStatusId">The closed status to set; status ids differ per Halo instance, so read it from <see cref="IPsaApi.Statuses"/></param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The update's response</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// </exception>
	public Task<UpdateTicketResponse> CloseAsync(int id, int closedStatusId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CloseAsync));
		ThrowIfNotPositive(closedStatusId, nameof(closedStatusId), "Closed status id", "status");

		return UpdateAsync(id, new UpdateTicketRequest { Status = closedStatusId }, cancellationToken);
	}

	/// <summary>
	/// Closes a ticket by updating it to a closed status with a closure note; Halo has no close route
	/// </summary>
	/// <param name="id">The ticket id</param>
	/// <param name="closedStatusId">The closed status to set; status ids differ per Halo instance, so read it from <see cref="IPsaApi.Statuses"/></param>
	/// <param name="closureNote">The note to record on closing; it must not be blank</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The update's response</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// </exception>
	public Task<UpdateTicketResponse> CloseAsync(int id, int closedStatusId, string closureNote, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(CloseAsync));
		ThrowIfNotPositive(closedStatusId, nameof(closedStatusId), "Closed status id", "status");

		if (string.IsNullOrWhiteSpace(closureNote))
		{
			throw new ArgumentException("The closure note must not be blank; no request was sent.", nameof(closureNote));
		}

		return UpdateAsync(id, new UpdateTicketRequest { Status = closedStatusId, ClosureNote = closureNote }, cancellationToken);
	}

	/// <summary>
	/// Reopens a ticket by updating it to an open status; Halo has no reopen route
	/// </summary>
	/// <param name="id">The ticket id</param>
	/// <param name="openStatusId">The open status to set; status ids differ per Halo instance, so read it from <see cref="IPsaApi.Statuses"/></param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The update's response</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// </exception>
	public Task<UpdateTicketResponse> ReopenAsync(int id, int openStatusId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(ReopenAsync));
		ThrowIfNotPositive(openStatusId, nameof(openStatusId), "Open status id", "status");

		return UpdateAsync(id, new UpdateTicketRequest { Status = openStatusId }, cancellationToken);
	}

	/// <summary>
	/// Assigns a ticket to an agent by updating its agent; Halo has no assign route
	/// </summary>
	/// <param name="id">The ticket id</param>
	/// <param name="agentId">The agent to assign to</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The update's response</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// </exception>
	public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(AssignAsync));
		ThrowIfNotPositive(agentId, nameof(agentId), "Agent id", "agent");

		return UpdateAsync(id, new UpdateTicketRequest { AgentId = agentId }, cancellationToken);
	}

	/// <summary>
	/// Assigns a ticket to an agent and a team by updating them; Halo has no assign route
	/// </summary>
	/// <param name="id">The ticket id</param>
	/// <param name="agentId">The agent to assign to</param>
	/// <param name="teamId">The team to assign to</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The update's response</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// </exception>
	public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, int teamId, CancellationToken cancellationToken)
	{
		ThrowIfReadOnly(nameof(AssignAsync));
		ThrowIfNotPositive(agentId, nameof(agentId), "Agent id", "agent");
		ThrowIfNotPositive(teamId, nameof(teamId), "Team id", "team");

		return UpdateAsync(id, new UpdateTicketRequest { AgentId = agentId, TeamId = teamId }, cancellationToken);
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
