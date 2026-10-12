using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Sites;
using Refit;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers the Sites client: the query parameter names <see cref="SiteFilter"/> and the legacy overload put on
/// the wire, and how <see cref="SitesApiWrapper"/> pages through the list. Halo silently returns at most 50
/// sites to an unpaged request, and with <c>pageinate=true</c> reports the TOTAL in <c>record_count</c> on every
/// page (checked against a live tenant on 2026-10-06). These tests build the real Refit client over a capturing
/// handler, so they need no network and no credentials.
/// </summary>
public class SitesApiWrapperTests
{
	private static readonly Uri _baseAddress = new("https://example.invalid");

	private static T CreateClient<T>(CapturingHandler handler)
		=> RestService.For<T>(new HttpClient(handler) { BaseAddress = _baseAddress });

	[Fact]
	public async Task SiteFilter_SendsHalosOwnParameterNames()
	{
		var handler = CapturingHandler.Returning("""{"sites":[],"record_count":0}""");
		var client = CreateClient<ISitesApi>(handler);
		var filter = new SiteFilter
		{
			Count = 10,
			Paginate = true,
			PageNo = 2,
			PageSize = 100,
			ClientId = 5,
			IncludeInactive = true,
			IncludeCustomFields = "518,519",
			Search = "london"
		};

		_ = await client.GetAllResponseAsync(filter, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Site");
		_ = handler.LastRequest.Query.Should().Be(
			"?count=10&pageinate=True&page_no=2&page_size=100&client_id=5&includeinactive=True&include_custom_fields=518%2C519&search=london");
	}

	[Fact]
	public async Task SitesLegacyOverload_SendsHalosOwnParameterNames()
	{
		var handler = CapturingHandler.Returning("""{"sites":[],"record_count":0}""");
		var client = CreateClient<ISitesApi>(handler);

		_ = await client.GetAllResponseAsync(5, includeInactive: true, CancellationToken.None);

		_ = handler.LastRequest.Query.Should().Be("?client_id=5&includeinactive=True");
	}

	[Fact]
	public async Task SitesGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7}""");
		var client = CreateClient<ISitesApi>(handler);

		_ = await client.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Site/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloReportsTheTotal_ReadsToTheEmptyPageAndReturnsEverySite()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 237));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(237);
		_ = sites.Select(s => s.Id).Should().OnlyHaveUniqueItems();
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=100",
			"?pageinate=True&page_no=2&page_size=100",
			"?pageinate=True&page_no=3&page_size=100",
			"?pageinate=True&page_no=4&page_size=100");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsAnExactMultipleOfThePageSize_StillEndsOnTheEmptyPage()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 200));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(200);
		_ = handler.Requests.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloCapsThePageBelowTheRequestedSize_StillCollectsEverySite()
	{
		// Halo is asked for 100 per page but only ever returns 50, offsetting by the 50 it returned.
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 120, maxPageSize: 50));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(120);
		_ = sites.Select(s => s.Id).Should().OnlyHaveUniqueItems();
		_ = handler.Requests.Should().HaveCount(4);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenFirstPageIsEmpty_ReturnsNothingAfterOneRequest()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 0));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().BeEmpty();
		_ = handler.Requests.Should().HaveCount(1);
	}

	[Fact]
	public async Task GetAllPagedAsync_KeepsTheCallersFilterAndPageSize_ButForcesPagination()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 2));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));
		var filter = new SiteFilter { ClientId = 5, PageSize = 10, Paginate = false, PageNo = 9, IncludeCustomFields = "518" };

		var sites = await wrapper.GetAllPagedAsync(filter, CancellationToken.None);

		_ = sites.Should().HaveCount(2);
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=10&client_id=5&include_custom_fields=518",
			"?pageinate=True&page_no=2&page_size=10&client_id=5&include_custom_fields=518");
	}

	[Fact]
	public async Task GetAllPagedAsync_DoesNotSendTheCallersCount()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 3));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		_ = await wrapper.GetAllPagedAsync(new SiteFilter { Count = 500 }, CancellationToken.None);

		_ = handler.Requests.Should().OnlyContain(r => CapturingHandler.ParseQuery(r)["count"] == null);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloIgnoresPageNo_ThrowsAtOnceInsteadOfRequestingEveryPage()
	{
		// Every page is the same 100 sites, while the total says there are 250.
		var handler = CapturingHandler.Returning(SitesJson(100, firstId: 1, recordCount: 250));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*page 2 holding only sites already received*");
		_ = handler.Requests.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloOffsetsByTheRequestedSizeButReturnsFewer_ThrowsRatherThanReturnAPartialList()
	{
		// 120 sites, 100 requested per page, 50 returned, but the offset worked out from the 100: page 1 is sites
		// 1-50, page 2 is 101-120, page 3 is empty - 70 of 120, with 51-100 never seen.
		var handler = new CapturingHandler(request =>
		{
			var query = CapturingHandler.ParseQuery(request.RequestUri!);
			var pageNo = int.Parse(query["page_no"]!);
			var requested = int.Parse(query["page_size"]!);
			var skipped = (pageNo - 1) * requested;
			var onThisPage = Math.Clamp(120 - skipped, 0, 50);
			return SitesJson(onThisPage, firstId: skipped + 1, recordCount: onThisPage == 0 ? 0 : 120);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reported 120 sites but 70 were returned before the empty page 3*");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsLowerThanTheRowsReturned_ThrowsRatherThanStopEarly()
	{
		// 250 rows exist but Halo reports 200. Stopping on the total would silently drop 50.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			var skipped = (pageNo - 1) * 100;
			var onThisPage = Math.Clamp(250 - skipped, 0, 100);
			return SitesJson(onThisPage, firstId: skipped + 1, recordCount: onThisPage == 0 ? 0 : 200);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reported 200 sites but 250 were returned*");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenASiteIsDeletedFromAnEarlierPageMidListing_RestartsAndReturnsEverySurvivingSite()
	{
		// 150 sites. Site 50 is deleted after page 1 is served, so every later row moves back one: without a restart
		// site 101 would never be served and deleted site 50 would be returned.
		var live = new SimulatedSiteList(Enumerable.Range(1, 150));
		live.AfterRequest(1, list => list.Remove(50));
		var handler = new CapturingHandler(live.Respond);
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Select(s => s.Id).Should().BeEquivalentTo(Enumerable.Range(1, 150).Where(id => id != 50));
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenASiteIsAddedAtTheFrontMidListing_RestartsAndReturnsEverySite()
	{
		var live = new SimulatedSiteList(Enumerable.Range(1, 150));
		live.AfterRequest(1, list => list.Insert(0, 1000));
		var handler = new CapturingHandler(live.Respond);
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Select(s => s.Id).Should().BeEquivalentTo(Enumerable.Range(1, 150).Append(1000));
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalNeverSettles_ThrowsAfterMaxAttempts()
	{
		// A site is added after every request, so every pass sees the total move.
		var live = new SimulatedSiteList(Enumerable.Range(1, 150));
		var nextId = 10_000;
		live.AfterEveryRequest(list => list.Add(nextId++));
		var handler = new CapturingHandler(live.Respond);
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*kept changing*{SitesApiWrapper.MaxAttempts} attempts*");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenOnePageReportsAZeroTotal_IgnoresIt()
	{
		// Halo answers record_count: 0 past the end of the list. A zero on a page with rows must not count as the
		// total moving - remove the RecordCount > 0 check and this restarts until it gives up.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return pageNo switch
			{
				1 => SitesJson(100, firstId: 1, recordCount: 150),
				2 => SitesJson(50, firstId: 101, recordCount: 0),
				_ => SitesJson(0, firstId: 151, recordCount: 0),
			};
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(150);
		_ = handler.Requests.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsNeverReached_StopsAtTheMaxPagesBackstop()
	{
		// Every page is new and never empty, so only the backstop can end it.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return SitesJson(1, firstId: pageNo, recordCount: int.MaxValue);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*page limit*");
		_ = handler.Requests.Should().HaveCount(SitesApiWrapper.MaxPages);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloReportsNoTotal_PagesUntilAnEmptyPage()
	{
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return SitesJson(pageNo switch { 1 => 100, 2 => 100, 3 => 37, _ => 0 }, firstId: ((pageNo - 1) * 100) + 1, recordCount: 0);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(237);
		_ = handler.Requests.Should().HaveCount(4);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloReportsNoTotalAndCapsPagesAt50_StillCollectsEverySite()
	{
		// The case a short-page stop gets wrong: every page is 50, which looks "short" against the 100 asked for.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			var skipped = (pageNo - 1) * 50;
			return SitesJson(Math.Clamp(120 - skipped, 0, 50), firstId: skipped + 1, recordCount: 0);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(120);
	}

	[Fact]
	public async Task GetAllAsync_ForAClient_PagesAndFiltersByClientId()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 105));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllAsync(clientId: 5, CancellationToken.None);

		_ = sites.Should().HaveCount(105);
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=100&client_id=5&includeinactive=False",
			"?pageinate=True&page_no=2&page_size=100&client_id=5&includeinactive=False",
			"?pageinate=True&page_no=3&page_size=100&client_id=5&includeinactive=False");
	}

	[Fact]
	public async Task GetAllAsync_WithNoClient_PagesAndExcludesInactiveAsBefore()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 3));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllAsync(CancellationToken.None);

		_ = sites.Should().HaveCount(3);
		_ = handler.Requests[0].Query.Should().Be("?pageinate=True&page_no=1&page_size=100&includeinactive=False");
	}

	/// <summary>
	/// A list that changes between requests, served the way Halo serves it: page N is the rows at offset
	/// (N - 1) * page_size of the list as it is at that moment, with record_count its current length.
	/// </summary>
	private sealed class SimulatedSiteList(IEnumerable<int> ids)
	{
		private readonly List<int> _ids = [.. ids];
		private readonly Dictionary<int, Action<List<int>>> _afterRequest = [];
		private Action<List<int>>? _afterEveryRequest;
		private int _requests;

		public void AfterRequest(int requestNumber, Action<List<int>> change) => _afterRequest[requestNumber] = change;

		public void AfterEveryRequest(Action<List<int>> change) => _afterEveryRequest = change;

		public string Respond(HttpRequestMessage request)
		{
			var query = CapturingHandler.ParseQuery(request.RequestUri!);
			var pageNo = int.Parse(query["page_no"]!);
			var pageSize = int.Parse(query["page_size"]!);
			var page = _ids.Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
			var json = SitesJsonFor(page, page.Count == 0 ? 0 : _ids.Count);

			_requests++;
			if (_afterRequest.TryGetValue(_requests, out var change))
			{
				change(_ids);
			}

			_afterEveryRequest?.Invoke(_ids);
			return json;
		}
	}

	/// <summary>
	/// Behaves like Halo with <c>pageinate=true</c> as checked against a live tenant: <c>record_count</c> is the
	/// total on every page, a page past the end is empty with <c>record_count: 0</c>, and the page size can be
	/// capped below what was requested.
	/// </summary>
	private static Func<HttpRequestMessage, string> SimulatedHalo(int totalSites, int maxPageSize = int.MaxValue)
		=> request =>
		{
			var query = CapturingHandler.ParseQuery(request.RequestUri!);
			var pageNo = int.Parse(query["page_no"]!);
			var pageSize = Math.Min(int.Parse(query["page_size"]!), maxPageSize);
			var skipped = (pageNo - 1) * pageSize;
			var onThisPage = Math.Clamp(totalSites - skipped, 0, pageSize);

			return SitesJson(onThisPage, firstId: skipped + 1, recordCount: onThisPage == 0 ? 0 : totalSites);
		};

	private static string SitesJson(int count, int firstId, int? recordCount = null)
	{
		var sites = Enumerable.Range(firstId, count).Select(id => $$"""{"id":{{id}},"name":"Site {{id}}","client_id":21.0}""");
		return $$"""{"sites":[{{string.Join(',', sites)}}],"record_count":{{recordCount ?? count}}}""";
	}

	private static string SitesJsonFor(IEnumerable<int> ids, int recordCount)
	{
		var sites = ids.Select(id => $$"""{"id":{{id}},"name":"Site {{id}}","client_id":21.0}""");
		return $$"""{"sites":[{{string.Join(',', sites)}}],"record_count":{{recordCount}}}""";
	}
}
