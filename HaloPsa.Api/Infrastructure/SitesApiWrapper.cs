using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Sites;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Wrapper for the Sites API that provides convenience methods
/// </summary>
/// <param name="sitesApi">The underlying Sites API</param>
public class SitesApiWrapper(ISitesApi sitesApi) : ISitesApi
{
	/// <summary>
	/// The page size used by <see cref="GetAllPagedAsync"/> when the filter does not set one
	/// </summary>
	public const int DefaultPageSize = 100;

	/// <summary>
	/// The most pages <see cref="GetAllPagedAsync"/> will request in one pass before giving up
	/// </summary>
	/// <remarks>
	/// At the default page size this allows up to 100,000 sites (50,000 if Halo caps pages at 50), far beyond any
	/// real tenant, so reaching it means Halo is not honouring the paging parameters rather than that there is more
	/// data. It is a backstop: a page made up entirely of sites already received is caught immediately, without
	/// waiting for this limit.
	/// </remarks>
	public const int MaxPages = 1000;

	/// <summary>
	/// How many times <see cref="GetAllPagedAsync"/> starts the listing again because Halo's reported total changed
	/// part-way through, before giving up
	/// </summary>
	public const int MaxAttempts = 3;

	/// <summary>
	/// Gets all sites as a convenient list
	/// </summary>
	/// <remarks>
	/// Pages through the whole list, because Halo silently returns at most 50 sites to an unpaged request.
	/// </remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of sites</returns>
	public Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken)
		=> GetAllPagedAsync(new SiteFilter { IncludeInactive = false }, cancellationToken);

	/// <summary>
	/// Gets all sites for a specific client
	/// </summary>
	/// <remarks>
	/// Pages through the whole list, because Halo silently returns at most 50 sites to an unpaged request.
	/// </remarks>
	/// <param name="clientId">The client ID to filter by</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of sites for the specified client</returns>
	public Task<IReadOnlyList<Site>> GetAllAsync(int clientId, CancellationToken cancellationToken)
		=> GetAllPagedAsync(new SiteFilter { ClientId = clientId, IncludeInactive = false }, cancellationToken);

	/// <summary>
	/// Gets every site matching the filter, reading page by page until Halo returns an empty page, and checks the
	/// result against the total Halo reports
	/// </summary>
	/// <remarks>
	/// <para>Pagination is always switched on, whatever <see cref="SiteFilter.Paginate"/> and
	/// <see cref="SiteFilter.PageNo"/> say, and <see cref="SiteFilter.Count"/> is not sent, because paging decides
	/// how many rows come back. <see cref="SiteFilter.PageSize"/> is used when set and
	/// <see cref="DefaultPageSize"/> otherwise.</para>
	/// <para><b>It reads to an empty page and never stops on the total alone.</b> With <c>pageinate=true</c> Halo
	/// reports the TOTAL number of matching sites in <see cref="SitesResponse.RecordCount"/> on every page (checked
	/// against a live tenant: 62 sites gave <c>record_count: 62</c> on each page, however the page size was set).
	/// That total is used only as a cross-check, because stopping on it cannot prove the list is complete: a total
	/// lower than the rows the filter really matches would end the listing early with no error.</para>
	/// <para><b>A total that moves restarts the listing.</b> Pages are offsets into a list that can change while it
	/// is being read. A site added or deleted part-way through shifts every later row, so one row is skipped or
	/// served twice, and the two cases can look identical from here. The total changes in both, so any page reporting
	/// a different non-zero total from page 1 starts the listing again, up to <see cref="MaxAttempts"/> times. A page
	/// reporting zero (Halo's answer past the end) is ignored.</para>
	/// <para><b>A finished listing must match its total.</b> If the collected count differs from the total, the
	/// pages did not line up - for instance Halo worked out offsets from a different page size than it returned -
	/// and the call throws rather than return a list it knows is incomplete. When Halo reports no total at all there
	/// is nothing to check against, and the empty page is the only end; that assumes Halo offsets pages by the size
	/// it returns, which held on the live tenant.</para>
	/// <para>A site seen twice is returned once. A page made up entirely of sites already received, with the total
	/// unchanged, means Halo is ignoring <c>page_no</c>, and throws.</para>
	/// </remarks>
	/// <param name="filter">Filter options, or <see langword="null"/> for every site</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Every matching site</returns>
	/// <exception cref="InvalidOperationException">Halo is ignoring <c>page_no</c>; the collected sites did not
	/// match the reported total; the total kept changing for <see cref="MaxAttempts"/> attempts; or more than
	/// <see cref="MaxPages"/> pages were needed</exception>
	public async Task<IReadOnlyList<Site>> GetAllPagedAsync(SiteFilter? filter, CancellationToken cancellationToken)
	{
		var pageSize = filter?.PageSize is > 0 ? filter.PageSize.Value : DefaultPageSize;

		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			var (sites, totalMoved) = await ReadEveryPageAsync(filter, pageSize, cancellationToken);
			if (!totalMoved)
			{
				return sites;
			}
		}

		throw new InvalidOperationException(
			$"The site list kept changing while it was being read ({MaxAttempts} attempts); stopping rather than return a list that may be incomplete.");
	}

	private async Task<(IReadOnlyList<Site> Sites, bool TotalMoved)> ReadEveryPageAsync(
		SiteFilter? filter,
		int pageSize,
		CancellationToken cancellationToken)
	{
		var sites = new List<Site>();
		var seenIds = new HashSet<int>();
		int? firstTotal = null;

		for (var pageNo = 1; pageNo <= MaxPages; pageNo++)
		{
			var pageFilter = (filter ?? new SiteFilter()) with
			{
				Count = null,
				Paginate = true,
				PageSize = pageSize,
				PageNo = pageNo
			};

			var response = await sitesApi.GetAllResponseAsync(pageFilter, cancellationToken);

			if (response.RecordCount > 0)
			{
				firstTotal ??= response.RecordCount;
				if (response.RecordCount != firstTotal)
				{
					return (sites, true);
				}
			}

			if (response.Sites.Count == 0)
			{
				if (firstTotal is { } total && sites.Count != total)
				{
					throw new InvalidOperationException(
						$"Halo reported {total} sites but {sites.Count} were returned before the empty page {pageNo}; the pages "
						+ "did not line up, so the list would be incomplete.");
				}

				return (sites, false);
			}

			var newSites = response.Sites.Where(site => seenIds.Add(site.Id)).ToList();
			if (newSites.Count == 0)
			{
				throw new InvalidOperationException(
					$"Halo returned page {pageNo} holding only sites already received; it does not appear to be honouring page_no.");
			}

			sites.AddRange(newSites);
		}

		throw new InvalidOperationException(
			$"Halo still had sites to return after {MaxPages} pages of up to {pageSize}; stopping at the page limit.");
	}

	/// <inheritdoc />
	public Task<SitesResponse> GetAllResponseAsync(int? clientId, bool includeInactive, CancellationToken cancellationToken)
		=> sitesApi.GetAllResponseAsync(clientId, includeInactive, cancellationToken);

	/// <inheritdoc />
	public Task<SitesResponse> GetAllResponseAsync(SiteFilter? filter, CancellationToken cancellationToken)
		=> sitesApi.GetAllResponseAsync(filter, cancellationToken);

	/// <inheritdoc />
	public Task<Site> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
		=> sitesApi.GetByIdAsync(id, includeDetails, cancellationToken);

	/// <summary>
	/// Gets a specific site by ID with default parameters
	/// </summary>
	/// <param name="id">The site ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The site with the specified ID</returns>
	public Task<Site> GetByIdAsync(int id, CancellationToken cancellationToken)
		=> GetByIdAsync(id, includeDetails: false, cancellationToken);
}
