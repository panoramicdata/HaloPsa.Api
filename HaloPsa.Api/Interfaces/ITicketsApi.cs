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
	/// <returns>The response: <see cref="TicketWriteResponse.Tickets"/> holds every ticket Halo returned, and <see cref="TicketWriteResponse.Ticket"/> (and <see cref="TicketWriteResponse.TicketId"/>) is the first</returns>
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
	/// <returns>The response: <see cref="TicketWriteResponse.Tickets"/> holds every ticket Halo returned, and <see cref="TicketWriteResponse.Ticket"/> (and <see cref="TicketWriteResponse.TicketId"/>) is the first</returns>
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
	/// Delete a ticket, recording why
	/// </summary>
	/// <param name="id">The ticket ID to delete</param>
	/// <param name="reason">The reason for the deletion, sent as Halo's <c>reason</c> parameter</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A task representing the delete operation</returns>
	[Delete("/api/Tickets/{id}")]
	Task DeleteAsync(
		int id,
		[Query][AliasAs("reason")] string reason,
		CancellationToken cancellationToken);
}