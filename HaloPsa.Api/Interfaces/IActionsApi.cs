using HaloPsa.Api.Models.Actions;
using Refit;

namespace HaloPsa.Api.Interfaces;

/// <summary>
/// Interface for reading ticket actions (notes, emails and other history)
/// </summary>
public interface IActionsApi
{
	/// <summary>
	/// Gets actions matching the filter
	/// </summary>
	/// <param name="filter">Filter options, normally including the ticket id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The matching actions</returns>
	[Get("/api/Actions")]
	Task<ActionsResponse> GetAllAsync([Query] ActionFilter filter, CancellationToken cancellationToken);
}
