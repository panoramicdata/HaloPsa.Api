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
	/// The most pages <see cref="GetAllPagedAsync"/> will request before giving up
	/// </summary>
	/// <remarks>
	/// At the default page size this allows up to 100,000 sites (50,000 if Halo caps pages at 50), far beyond any
	/// real tenant, so reaching it means Halo is not honouring the paging parameters rather than that there is more
	/// data. It is a backstop: a page made up entirely of sites already received is caught immediately, without
	/// waiting for this limit.
	/// </remarks>
	public const int MaxPages = 1000;

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
	/// Gets every site matching the filter by requesting pages until the reported total has been collected
	/// </summary>
	/// <remarks>
	/// <para>Pagination is always switched on, whatever <see cref="SiteFilter.Paginate"/> and
	/// <see cref="SiteFilter.PageNo"/> say, and <see cref="SiteFilter.Count"/> is not sent, because paging decides
	/// how many rows come back. <see cref="SiteFilter.PageSize"/> is used when set and
	/// <see cref="DefaultPageSize"/> otherwise.</para>
	/// <para>With <c>pageinate=true</c> Halo reports the TOTAL number of matching sites in
	/// <see cref="SitesResponse.RecordCount"/> on every page (checked against a live tenant: 62 sites gave
	/// <c>record_count: 62</c> on each page, however the page size was set). Pages are requested until the most
	/// recently reported total has been collected. The most recent non-zero total is used rather than the first,
	/// so a site added or removed while the listing is in progress moves the target with it, and a page reporting
	/// zero does not discard it. This does not depend on Halo honouring the page size, which matters because it
	/// may return fewer rows per page than were asked for.</para>
	/// <para>When Halo reports no total at all, pages are requested until one comes back empty. That costs one
	/// request more than stopping at the first short page, but a short page cannot be told apart from Halo capping
	/// the page size, and stopping there is exactly how a list silently ends at 50.</para>
	/// <para>A site that appears on two pages - one created or reordered while the listing is in progress - is
	/// returned once. A page made up entirely of sites already received means Halo is ignoring <c>page_no</c>,
	/// and throws.</para>
	/// </remarks>
	/// <param name="filter">Filter options, or <see langword="null"/> for every site</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Every matching site</returns>
	/// <exception cref="InvalidOperationException">A page held only sites already received, which means Halo is
	/// ignoring <c>page_no</c>; an empty page arrived before the reported total was reached, which means the
	/// pages did not line up and the list would be incomplete; or more than <see cref="MaxPages"/> pages were
	/// needed</exception>
	public async Task<IReadOnlyList<Site>> GetAllPagedAsync(SiteFilter? filter, CancellationToken cancellationToken)
	{
		var pageSize = filter?.PageSize is > 0 ? filter.PageSize.Value : DefaultPageSize;
		var sites = new List<Site>();
		var seenIds = new HashSet<int>();
		var reportedTotal = 0;

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
				reportedTotal = response.RecordCount;
			}

			if (response.Sites.Count == 0)
			{
				// Halo works out a page's offset itself. If it did so from a different page size than it returned,
				// whole runs of sites are skipped and the listing simply runs out early - so an empty page before the
				// total is reached is a fault, not the end of the data.
				if (sites.Count < reportedTotal)
				{
					throw new InvalidOperationException(
						$"Halo reported {reportedTotal} sites but returned an empty page {pageNo} after {sites.Count}; the pages "
						+ "did not line up, so the list would be incomplete.");
				}

				return sites;
			}

			var newSites = response.Sites.Where(site => seenIds.Add(site.Id)).ToList();
			if (newSites.Count == 0)
			{
				throw new InvalidOperationException(
					$"Halo returned page {pageNo} holding only sites already received; it does not appear to be honouring page_no.");
			}

			sites.AddRange(newSites);

			// With no total reported, only an empty page ends the listing - see the remarks.
			if (reportedTotal > 0 && sites.Count >= reportedTotal)
			{
				return sites;
			}
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
