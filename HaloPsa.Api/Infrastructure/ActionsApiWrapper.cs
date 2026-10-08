using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Actions;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Read-only access to ticket actions
/// </summary>
/// <param name="actionsApi">The underlying Actions API</param>
public class ActionsApiWrapper(IActionsApi actionsApi) : IActionsApi
{
	/// <inheritdoc />
	public Task<ActionsResponse> GetAllAsync(ActionFilter filter, CancellationToken cancellationToken)
		=> actionsApi.GetAllAsync(filter, cancellationToken);

	/// <summary>
	/// Gets every action on a ticket
	/// </summary>
	/// <param name="ticketId">The ticket id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The ticket's actions</returns>
	public Task<ActionsResponse> GetForTicketAsync(int ticketId, CancellationToken cancellationToken)
		=> actionsApi.GetAllAsync(new ActionFilter { TicketId = ticketId }, cancellationToken);
}
