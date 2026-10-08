using HaloPsa.Api.Models.Priorities;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Priorities API operations
/// </summary>
public interface IPrioritiesApi
{
	/// <summary>
	/// Gets all priorities from the Halo system
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Response containing the list of priorities</returns>
	[Get("/api/Priority")]
	Task<PrioritiesResponse> GetAllResponseAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Gets a specific priority by ID
	/// </summary>
	/// <param name="id">The priority record id (a GUID; see <see cref="Priority.Id"/>)</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The priority with the specified ID</returns>
	[Get("/api/Priority/{id}")]
	Task<Priority> GetByIdAsync(string id, [Query] bool includeDetails, CancellationToken cancellationToken);
}