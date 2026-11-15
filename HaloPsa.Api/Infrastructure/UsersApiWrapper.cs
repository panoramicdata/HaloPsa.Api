using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Users;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Wrapper for Users API that provides both raw responses and convenient array access
/// </summary>
public class UsersApiWrapper(IUsersRefitApi usersRefitApi) : IUsersApi
{
	/// <summary>
	/// Get all users - Returns unwrapped array for convenience
	/// </summary>
	public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
	{
		var response = await GetResponseAsync(cancellationToken);
		return response.Users;
	}

	/// <summary>
	/// Get all users - Returns wrapped response
	/// </summary>
	public async Task<UsersResponse> GetResponseAsync(CancellationToken cancellationToken)
	{
		return await usersRefitApi.GetResponseAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<User>> GetAllAsync(UserFilter? filter, CancellationToken cancellationToken)
		=> (await GetResponseAsync(filter, cancellationToken).ConfigureAwait(false)).Users;

	/// <inheritdoc />
	public Task<UsersResponse> GetResponseAsync(UserFilter? filter, CancellationToken cancellationToken)
		=> usersRefitApi.GetResponseAsync(filter, cancellationToken);

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
	/// Gets every user matching the filter, reading page by page until Halo returns an empty page, and checks the
	/// result against the total Halo reports
	/// </summary>
	/// <remarks>
	/// Pagination is always switched on whatever the filter's <see cref="UserFilter.Paginate"/> and
	/// <see cref="UserFilter.PageNo"/> say, and <see cref="UserFilter.PageSize"/> is used when set and
	/// <see cref="DefaultPageSize"/> otherwise. The reasoning is in <see cref="SitesApiWrapper.GetAllPagedAsync"/>.
	/// </remarks>
	/// <param name="filter">Filter options, or <see langword="null"/> for every user</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Every matching user</returns>
	/// <exception cref="InvalidOperationException">Halo is ignoring <c>page_no</c>; the collected users did not
	/// match the reported total; the total kept changing for <see cref="MaxAttempts"/> attempts; or more than
	/// <see cref="MaxPages"/> pages were needed</exception>
	public async Task<IReadOnlyList<User>> GetAllPagedAsync(UserFilter? filter, CancellationToken cancellationToken)
	{
		var pageSize = filter?.PageSize is > 0 ? filter.PageSize.Value : DefaultPageSize;

		return await PagedReader.ReadAllAsync<User>(
			async (pageNo, token) =>
			{
				var response = await usersRefitApi.GetResponseAsync(PageFilter(filter, pageSize, pageNo), token).ConfigureAwait(false);
				return (response.Users, response.RecordCount);
			},
			user => user.Id,
			new PagedReadOptions(pageSize, MaxPages, MaxAttempts, "user", "users"),
			cancellationToken).ConfigureAwait(false);
	}

	private static UserFilter PageFilter(UserFilter? filter, int pageSize, int pageNo)
		=> (filter ?? new UserFilter()) with
		{
			Paginate = true,
			PageSize = pageSize,
			PageNo = pageNo
		};

	/// <summary>
	/// Get a user by ID
	/// </summary>
	public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken)
	{
		return await usersRefitApi.GetByIdAsync(id, cancellationToken);
	}
}