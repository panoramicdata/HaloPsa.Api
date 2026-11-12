using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

public partial class TicketsApiWrapper
{
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
			TicketRequestValidator.ValidateForCreate(request);
		}

		return SendCreateAsync(requests, cancellationToken);
	}

	// An unreadable 2xx body arrives as an ApiException, which a caller would treat as an HTTP failure and retry into a duplicate ticket.
	private async Task<CreateTicketResponse> SendCreateAsync(IReadOnlyList<CreateTicketRequest> requests, CancellationToken cancellationToken)
	{
		try
		{
			return await _ticketsApi.CreateAsync(requests, cancellationToken).ConfigureAwait(false)
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
}
