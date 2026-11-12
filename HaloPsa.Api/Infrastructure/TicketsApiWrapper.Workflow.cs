using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

public partial class TicketsApiWrapper
{
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
		TicketRequestValidator.ThrowIfNotPositive(closedStatusId, nameof(closedStatusId), "Closed status id", "status");

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
		TicketRequestValidator.ThrowIfNotPositive(closedStatusId, nameof(closedStatusId), "Closed status id", "status");

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
		TicketRequestValidator.ThrowIfNotPositive(openStatusId, nameof(openStatusId), "Open status id", "status");

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
		TicketRequestValidator.ThrowIfNotPositive(agentId, nameof(agentId), "Agent id", "agent");

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
		TicketRequestValidator.ThrowIfNotPositive(agentId, nameof(agentId), "Agent id", "agent");
		TicketRequestValidator.ThrowIfNotPositive(teamId, nameof(teamId), "Team id", "team");

		return UpdateAsync(id, new UpdateTicketRequest { AgentId = agentId, TeamId = teamId }, cancellationToken);
	}
}
