using HaloPsa.Api.Models.Teams;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Teams API operations
/// </summary>
public interface ITeamsApi
{
	/// <summary>
	/// Gets all teams from the Halo system
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Response containing the list of teams</returns>
	[Get("/api/Team")]
	Task<TeamsResponse> GetAllResponseAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Gets a specific team by ID
	/// </summary>
	/// <param name="id">The team ID</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The team with the specified ID</returns>
	[Get("/api/Team/{id}")]
	Task<Team> GetByIdAsync(int id, [Query] bool includeDetails, CancellationToken cancellationToken);
}