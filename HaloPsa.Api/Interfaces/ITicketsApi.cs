using HaloPsa.Api.Models.Tickets;
using Refit;

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
	[Post("/api/Tickets")]
	Task<CreateTicketResponse> CreateAsync(
		[Body] CreateTicketRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Update an existing ticket
	/// </summary>
	/// <param name="id">The ticket ID to update</param>
	/// <param name="request">The ticket update request</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The updated ticket</returns>
	[Put("/api/Tickets/{id}")]
	Task<UpdateTicketResponse> UpdateAsync(
		int id,
		[Body] UpdateTicketRequest request,
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