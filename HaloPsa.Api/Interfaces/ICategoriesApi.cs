using HaloPsa.Api.Models.Categories;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for Halo Categories API operations
/// </summary>
public interface ICategoriesApi
{
	/// <summary>
	/// Gets all categories from the Halo system
	/// </summary>
	/// <param name="ticketTypeId">Filter by ticket type ID</param>
	/// <param name="teamId">Filter by team ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Response containing the list of categories</returns>
	[Get("/api/Category")]
	Task<CategoriesResponse> GetAllResponseAsync([Query] int? ticketTypeId, [Query] int? teamId, CancellationToken cancellationToken);

	/// <summary>
	/// Gets a specific category by ID
	/// </summary>
	/// <param name="id">The category ID</param>
	/// <param name="includeDetails">Whether to include additional details</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The category with the specified ID</returns>
	[Get("/api/Category/{id}")]
	Task<Category> GetByIdAsync(int id, [Query] bool includeDetails, CancellationToken cancellationToken);
}