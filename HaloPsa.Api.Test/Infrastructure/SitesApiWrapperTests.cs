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
	public async Task GetAllPagedAsync_WhenHaloReportsTheTotal_RequestsEachPageAndReturnsEverySite()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 237));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(237);
		_ = sites.Select(s => s.Id).Should().OnlyHaveUniqueItems();
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=100",
			"?pageinate=True&page_no=2&page_size=100",
			"?pageinate=True&page_no=3&page_size=100");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsAnExactMultipleOfThePageSize_StopsWithoutAnExtraRequest()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 200));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(200);
		_ = handler.Requests.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloCapsThePageBelowTheRequestedSize_StillCollectsEverySite()
	{
		// Halo is asked for 100 per page but only ever returns 50. A stop-on-short-page rule would quit after
		// the first page; the reported total is what keeps this going.
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 120, maxPageSize: 50));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(120);
		_ = sites.Select(s => s.Id).Should().OnlyHaveUniqueItems();
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=100",
			"?pageinate=True&page_no=2&page_size=100",
			"?pageinate=True&page_no=3&page_size=100");
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
		_ = handler.Requests.Should().HaveCount(1);
		_ = handler.LastRequest.Query.Should().Be("?pageinate=True&page_no=1&page_size=10&client_id=5&include_custom_fields=518");
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

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reported 120 sites but returned an empty page 3 after 70*");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenOneSiteShiftsOntoTheNextPage_ReturnsItOnceAndCarriesOn()
	{
		// A site created mid-listing pushes site 100 onto page 2 as well.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return pageNo == 1
				? SitesJson(100, firstId: 1, recordCount: 199)
				: SitesJson(100, firstId: 100, recordCount: 199);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(199);
		_ = sites.Select(s => s.Id).Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenALaterPageReportsNoTotal_KeepsTheLastTotalReported()
	{
		// Pages are capped at 50 while 100 are requested. If page 2's zero total were taken as "no total", the
		// listing would lose its target part-way through.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return pageNo switch
			{
				1 => SitesJson(50, firstId: 1, recordCount: 120),
				2 => SitesJson(50, firstId: 51, recordCount: 0),
				_ => SitesJson(20, firstId: 101, recordCount: 120),
			};
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(120);
		_ = handler.Requests.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsNeverReached_StopsAtTheMaxPagesBackstop()
	{
		// Every page is new, but the total is unreachable, so only the backstop can end it.
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
	public async Task GetAllPagedAsync_WhenASiteIsDeletedMidListing_FinishesAtTheNewTotal()
	{
		// 150 sites; one is deleted after page 1, so page 2 holds the remaining 49 and reports 149.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return pageNo == 1
				? SitesJson(100, firstId: 1, recordCount: 150)
				: SitesJson(49, firstId: 102, recordCount: 149);
		});
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = sites.Should().HaveCount(149);
		_ = handler.Requests.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetAllPagedAsync_DoesNotSendTheCallersCount()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 3));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		_ = await wrapper.GetAllPagedAsync(new SiteFilter { Count = 500 }, CancellationToken.None);

		_ = CapturingHandler.ParseQuery(handler.LastRequest)["count"].Should().BeNull();
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
			"?pageinate=True&page_no=2&page_size=100&client_id=5&includeinactive=False");
	}

	[Fact]
	public async Task GetAllAsync_WithNoClient_Pages()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalSites: 3));
		var wrapper = new SitesApiWrapper(CreateClient<ISitesApi>(handler));

		var sites = await wrapper.GetAllAsync(CancellationToken.None);

		_ = sites.Should().HaveCount(3);
		_ = handler.LastRequest.Query.Should().Be("?pageinate=True&page_no=1&page_size=100&includeinactive=False");
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
}
