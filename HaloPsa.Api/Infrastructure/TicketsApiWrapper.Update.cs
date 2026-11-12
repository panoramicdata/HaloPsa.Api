using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

public partial class TicketsApiWrapper
{
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
		TicketRequestValidator.ThrowIfNotATicketId(id, nameof(id));
		TicketRequestValidator.ValidateForUpdate(request);

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
			TicketRequestValidator.ThrowIfNotATicketId(request.Id, nameof(requests));
			TicketRequestValidator.ValidateForUpdate(request);
		}

		return SendUpdateAsync(requests, cancellationToken);
	}

	// An unreadable 2xx body arrives as an ApiException, which a caller would treat as an HTTP failure and retry although the update may have been applied.
	private async Task<UpdateTicketResponse> SendUpdateAsync(IReadOnlyList<UpdateTicketRequest> requests, CancellationToken cancellationToken)
	{
		var ids = string.Join(", ", requests.Select(request => request.Id));

		try
		{
			return await _ticketsApi.UpdateAsync(requests, cancellationToken).ConfigureAwait(false)
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
}
