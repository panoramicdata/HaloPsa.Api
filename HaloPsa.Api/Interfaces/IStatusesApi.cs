using HaloPsa.Api.Models.Statuses;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Statuses API operations
/// </summary>
public interface IStatusesApi
{
	/// <summary>
	/// Gets all statuses from the Halo system
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Response containing the list of statuses</returns>
	[Get("/api/Status")]
	Task<StatusesResponse> GetAllResponseAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Gets a specific status by ID
	/// </summary>
	/// <param name="id">The status ID</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The status with the specified ID</returns>
	[Get("/api/Status/{id}")]
	Task<Status> GetByIdAsync(int id, [Query] bool includeDetails, CancellationToken cancellationToken);
}