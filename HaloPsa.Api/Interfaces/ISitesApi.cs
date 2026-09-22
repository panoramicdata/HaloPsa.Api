using HaloPsa.Api.Models.Sites;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Sites API operations
/// </summary>
public interface ISitesApi
{
	/// <summary>
	/// Gets all sites from the Halo system
	/// </summary>
	/// <param name="clientId">Filter by client ID</param>
	/// <param name="includeInactive">Whether to include inactive sites</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Response containing the list of sites</returns>
	[Get("/api/Site")]
	Task<SitesResponse> GetAllResponseAsync([Query] int? clientId, [Query] bool includeInactive, CancellationToken cancellationToken);

	/// <summary>
	/// Gets a specific site by ID
	/// </summary>
	/// <param name="id">The site ID</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The site with the specified ID</returns>
	[Get("/api/Site/{id}")]
	Task<Site> GetByIdAsync(int id, [Query] bool includeDetails, CancellationToken cancellationToken);
}