using HaloPsa.Api.Models.Tickets;
using Refit;
using System.Text.Json;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Tickets API operations
/// </summary>
public interface ITicketsApi
{
	/// <summary>
	/// Get multiple tickets with optional filtering and pagination
	/// </summary>
	/// <param name="filter">Filter options for the ticket query</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A collection of tickets matching the filter criteria</returns>
	[Get("/api/Tickets")]
	Task<TicketsResponse> GetAllAsync(
		[Query] TicketFilter? filter,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get multiple tickets without filter
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A collection of tickets</returns>
	[Get("/api/Tickets")]
	Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get a single ticket by ID
	/// </summary>
	/// <param name="id">The ticket ID</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The ticket with the specified ID</returns>
	[Get("/api/Tickets/{id}")]
	Task<Ticket> GetByIdAsync(
		int id,
		[Query][AliasAs("includedetails")] bool includeDetails,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get a single ticket by ID without extra details
	/// </summary>
	/// <param name="id">The ticket ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The ticket with the specified ID</returns>
	[Get("/api/Tickets/{id}")]
	Task<Ticket> GetByIdAsync(
		int id,
		CancellationToken cancellationToken);

	/// <summary>
	/// Create a new ticket
	/// </summary>
	/// <param name="request">The ticket creation request</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The newly created ticket</returns>
	[Obsolete("Halo's POST /api/Tickets takes an array; use the list overload, or TicketsApiWrapper.CreateAsync, which sends one ticket as an array.")]
	[Post("/api/Tickets")]
	Task<CreateTicketResponse> CreateAsync(
		[Body] CreateTicketRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Create tickets, sent as the array Halo's POST /api/Tickets takes
	/// </summary>
	/// <param name="requests">The ticket creation requests</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The response: <see cref="CreateTicketResponse.Tickets"/> holds every ticket Halo returned, and <see cref="CreateTicketResponse.Ticket"/> (and <see cref="CreateTicketResponse.TicketId"/>) is the first</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the ticket may have been created, so do not retry.
	/// The message names the ticket id when one could be read.
	/// </exception>
	[Post("/api/Tickets")]
	Task<CreateTicketResponse> CreateAsync(
		[Body] IReadOnlyList<CreateTicketRequest> requests,
		CancellationToken cancellationToken);

	/// <summary>
	/// Update tickets: Halo has no PUT for a ticket, so an update is the POST a create uses, with each ticket's id in its body
	/// </summary>
	/// <param name="requests">The ticket update requests, each carrying the id of the ticket to update</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The response: <see cref="UpdateTicketResponse.Tickets"/> holds every ticket Halo returned, and <see cref="UpdateTicketResponse.Ticket"/> (and <see cref="UpdateTicketResponse.TicketId"/>) is the first</returns>
	/// <exception cref="JsonException">
	/// Halo answered success but the response could not be read; the update may have been applied, so do not retry.
	/// The message names the ticket id when one could be read.
	/// </exception>
	[Post("/api/Tickets")]
	Task<UpdateTicketResponse> UpdateAsync(
		[Body] IReadOnlyList<UpdateTicketRequest> requests,
		CancellationToken cancellationToken);

	/// <summary>
	/// Delete a ticket
	/// </summary>
	/// <param name="id">The ticket ID to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A task representing the delete operation</returns>
	[Delete("/api/Tickets/{id}")]
	Task DeleteAsync(
		int id,
		CancellationToken cancellationToken);

	/// <summary>
	/// Close a ticket
	/// </summary>
	/// <param name="id">The ticket ID to close</param>
	/// <param name="resolution">Resolution summary</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The closed ticket</returns>
	[Post("/api/Tickets/{id}/close")]
	Task<UpdateTicketResponse> CloseAsync(
		int id,
		[Query] string resolution,
		CancellationToken cancellationToken);

	/// <summary>
	/// Close a ticket without resolution
	/// </summary>
	/// <param name="id">The ticket ID to close</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The closed ticket</returns>
	[Post("/api/Tickets/{id}/close")]
	Task<UpdateTicketResponse> CloseAsync(
		int id,
		CancellationToken cancellationToken);

	/// <summary>
	/// Reopen a closed ticket
	/// </summary>
	/// <param name="id">The ticket ID to reopen</param>
	/// <param name="reason">Reason for reopening</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The reopened ticket</returns>
	[Post("/api/Tickets/{id}/reopen")]
	Task<UpdateTicketResponse> ReopenAsync(
		int id,
		[Query] string reason,
		CancellationToken cancellationToken);

	/// <summary>
	/// Reopen a closed ticket without reason
	/// </summary>
	/// <param name="id">The ticket ID to reopen</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The reopened ticket</returns>
	[Post("/api/Tickets/{id}/reopen")]
	Task<UpdateTicketResponse> ReopenAsync(
		int id,
		CancellationToken cancellationToken);

	/// <summary>
	/// Assign a ticket to an agent
	/// </summary>
	/// <param name="id">The ticket ID to assign</param>
	/// <param name="agentId">The agent ID to assign to</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The assigned ticket</returns>
	[Post("/api/Tickets/{id}/assign")]
	Task<UpdateTicketResponse> AssignAsync(
		int id,
		[Query] int agentId,
		CancellationToken cancellationToken);

	/// <summary>
	/// Assign a ticket to an agent and team
	/// </summary>
	/// <param name="id">The ticket ID to assign</param>
	/// <param name="agentId">The agent ID to assign to</param>
	/// <param name="teamId">The team ID to assign to</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The assigned ticket</returns>
	[Post("/api/Tickets/{id}/assign")]
	Task<UpdateTicketResponse> AssignAsync(
		int id,
		[Query] int agentId,
		[Query] int teamId,
		CancellationToken cancellationToken);
}