using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Clients;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Wrapper for Clients API that provides both raw responses and convenient array access
/// </summary>
public class ClientsApiWrapper(IClientsRefitApi clientsRefitApi) : IClientsApi
{
	/// <summary>
	/// The page size used by <see cref="GetAllPagedAsync"/> when the filter does not set one
	/// </summary>
	public static int DefaultPageSize { get; } = 100;

	/// <summary>
	/// The most pages <see cref="GetAllPagedAsync"/> will request in one pass before giving up
	/// </summary>
	public static int MaxPages { get; } = 1000;

	/// <summary>
	/// How many times <see cref="GetAllPagedAsync"/> starts the listing again because Halo's reported total changed
	/// part-way through, before giving up
	/// </summary>
	public static int MaxAttempts { get; } = 3;

	/// <summary>
	/// Get all clients - Returns unwrapped array for convenience
	/// </summary>
	public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken)
	{
		var response = await GetResponseAsync(cancellationToken);
		return response.Clients;
	}

	/// <summary>
	/// Get all clients - Returns wrapped response
	/// </summary>
	public async Task<ClientsResponse> GetResponseAsync(CancellationToken cancellationToken)
	{
		return await clientsRefitApi.GetResponseAsync(cancellationToken);
	}

	/// <summary>
	/// Get a client by ID
	/// </summary>
	public async Task<Client> GetByIdAsync(int id, CancellationToken cancellationToken)
	{
		return await clientsRefitApi.GetByIdAsync(id, cancellationToken);
	}

	/// <inheritdoc />
	public Task<ClientsResponse> GetResponseAsync(ClientFilter? filter, CancellationToken cancellationToken)
		=> clientsRefitApi.GetResponseAsync(filter, cancellationToken);

	/// <inheritdoc />
	public Task<Client> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
		=> clientsRefitApi.GetByIdAsync(id, includeDetails, cancellationToken);

	/// <summary>
	/// Gets every client matching the filter, reading page by page until Halo returns an empty page, and checks the
	/// result against the total Halo reports
	/// </summary>
	/// <remarks>
	/// Pagination is always switched on whatever the filter's <see cref="ClientFilter.Paginate"/>,
	/// <see cref="ClientFilter.PageNo"/> and <see cref="ClientFilter.Count"/> say, and
	/// <see cref="ClientFilter.PageSize"/> is used when set and <see cref="DefaultPageSize"/> otherwise. A reported
	/// total that changes part-way through restarts the listing, and a finished listing that does not match the
	/// total throws rather than return an incomplete list; the reasoning is in <see cref="SitesApiWrapper.GetAllPagedAsync"/>.
	/// </remarks>
	/// <param name="filter">Filter options, or <see langword="null"/> for every client</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Every matching client</returns>
	/// <exception cref="InvalidOperationException">Halo is ignoring <c>page_no</c>; the collected clients did not
	/// match the reported total; the total kept changing for <see cref="MaxAttempts"/> attempts; or more than
	/// <see cref="MaxPages"/> pages were needed</exception>
	public async Task<IReadOnlyList<Client>> GetAllPagedAsync(ClientFilter? filter, CancellationToken cancellationToken)
	{
		var pageSize = filter?.PageSize is > 0 ? filter.PageSize.Value : DefaultPageSize;

		return await PagedReader.ReadAllAsync<Client>(
			async (pageNo, token) =>
			{
				var response = await clientsRefitApi.GetResponseAsync(PageFilter(filter, pageSize, pageNo), token).ConfigureAwait(false);
				return (response.Clients, response.RecordCount);
			},
			client => client.Id,
			new PagedReadOptions(pageSize, MaxPages, MaxAttempts, "client", "clients"),
			cancellationToken).ConfigureAwait(false);
	}

	private static ClientFilter PageFilter(ClientFilter? filter, int pageSize, int pageNo)
		=> (filter ?? new ClientFilter()) with
		{
			Count = null,
			Paginate = true,
			PageSize = pageSize,
			PageNo = pageNo
		};
}