using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Clients;
using Refit;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers the Clients client: the query parameter names <see cref="ClientFilter"/> and the by-id read put on the
/// wire, how <see cref="ClientsApiWrapper"/> pages through the list, and how a client's custom fields are read.
/// These tests build the real Refit client over a capturing handler, so they need no network and no credentials.
/// </summary>
public class ClientsApiWrapperTests
{
	private static readonly Uri _baseAddress = new("https://example.invalid");

	private static ClientsApiWrapper CreateWrapper(CapturingHandler handler)
		=> new(CreateClient(handler));

	private static IClientsRefitApi CreateClient(CapturingHandler handler)
		=> RestService.For<IClientsRefitApi>(new HttpClient(handler) { BaseAddress = _baseAddress });

	[Fact]
	public async Task ClientFilter_SendsHalosOwnParameterNames()
	{
		var handler = CapturingHandler.Returning("""{"clients":[],"record_count":0}""");
		var client = CreateClient(handler);
		var filter = new ClientFilter
		{
			Count = 10,
			Paginate = true,
			PageNo = 2,
			PageSize = 100,
			IncludeInactive = true,
			IncludeCustomFields = "101",
			Search = "acme",
			ExcludeInternal = true
		};

		_ = await client.GetResponseAsync(filter, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Client");
		_ = handler.LastRequest.Query.Should().Be(
			"?count=10&pageinate=True&page_no=2&page_size=100&includeinactive=True&include_custom_fields=101&search=acme&exclude_internal=True");
	}

	[Fact]
	public async Task ClientsGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7}""");
		var wrapper = CreateWrapper(handler);

		_ = await wrapper.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Client/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloReportsTheTotal_ReadsToTheEmptyPageAndReturnsEveryClient()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalClients: 237));
		var wrapper = CreateWrapper(handler);

		var clients = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = clients.Should().HaveCount(237);
		_ = clients.Select(c => c.Id).Should().OnlyHaveUniqueItems();
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=100",
			"?pageinate=True&page_no=2&page_size=100",
			"?pageinate=True&page_no=3&page_size=100",
			"?pageinate=True&page_no=4&page_size=100");
	}

	[Fact]
	public async Task GetAllPagedAsync_KeepsTheCallersFilter_ButForcesPagination()
	{
		var handler = new CapturingHandler(SimulatedHalo(totalClients: 2));
		var wrapper = CreateWrapper(handler);
		var filter = new ClientFilter
		{
			IncludeInactive = true,
			IncludeCustomFields = "101",
			PageSize = 10,
			Paginate = false,
			PageNo = 9,
			Count = 500
		};

		var clients = await wrapper.GetAllPagedAsync(filter, CancellationToken.None);

		_ = clients.Should().HaveCount(2);
		_ = handler.Requests.Select(r => r.Query).Should().Equal(
			"?pageinate=True&page_no=1&page_size=10&includeinactive=True&include_custom_fields=101",
			"?pageinate=True&page_no=2&page_size=10&includeinactive=True&include_custom_fields=101");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenHaloReportsNoTotal_PagesUntilAnEmptyPage()
	{
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			return ClientsJson(pageNo switch { 1 => 100, 2 => 100, 3 => 37, _ => 0 }, firstId: ((pageNo - 1) * 100) + 1, recordCount: 0);
		});
		var wrapper = CreateWrapper(handler);

		var clients = await wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = clients.Should().HaveCount(237);
		_ = handler.Requests.Should().HaveCount(4);
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalIsLowerThanTheRowsReturned_Throws()
	{
		// 250 rows exist but Halo reports 200. Stopping on the total would silently drop 50.
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			var skipped = (pageNo - 1) * 100;
			var onThisPage = Math.Clamp(250 - skipped, 0, 100);
			return ClientsJson(onThisPage, firstId: skipped + 1, recordCount: onThisPage == 0 ? 0 : 200);
		});
		var wrapper = CreateWrapper(handler);

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reported 200 clients but 250 were returned*");
	}

	[Fact]
	public async Task GetAllPagedAsync_WhenTheTotalNeverSettles_ThrowsAfterMaxAttempts()
	{
		// The reported total grows on every request, so every pass sees it move.
		var requests = 0;
		var handler = new CapturingHandler(request =>
		{
			var pageNo = int.Parse(CapturingHandler.ParseQuery(request.RequestUri!)["page_no"]!);
			requests++;
			return ClientsJson(100, firstId: ((pageNo - 1) * 100) + 1, recordCount: 1000 + requests);
		});
		var wrapper = CreateWrapper(handler);

		var act = () => wrapper.GetAllPagedAsync(filter: null, CancellationToken.None);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*client list kept changing*{ClientsApiWrapper.MaxAttempts} attempts*");
	}

	[Fact]
	public async Task Client_ReadsCustomFields_WhateverTheValueType()
	{
		// Halo sends the field id as 101.0 and the value as a number for one field and a string for another.
		var handler = CapturingHandler.Returning(
			"""{"id":21,"name":"Acme","customfields":[{"id":101.0,"name":"CFExampleField","value":1234,"display":"Jane Smith (jsmith)"},{"id":102,"name":"CFOtherField","value":"1234","display":"1234"}]}""");
		var wrapper = CreateWrapper(handler);

		var client = await wrapper.GetByIdAsync(21, includeDetails: true, CancellationToken.None);

		_ = client.CustomFields.Should().HaveCount(2);
		_ = client.CustomFields![0].Id.Should().Be(101);
		_ = client.CustomFields[0].Value!.Value.GetInt32().Should().Be(1234);
		_ = client.CustomFields[0].Display.Should().Be("Jane Smith (jsmith)");
		_ = client.CustomFields[1].Value!.Value.GetString().Should().Be("1234");
	}

	/// <summary>
	/// Behaves like Halo with <c>pageinate=true</c>: <c>record_count</c> is the total on every page, and a page past
	/// the end is empty with <c>record_count: 0</c>.
	/// </summary>
	private static Func<HttpRequestMessage, string> SimulatedHalo(int totalClients)
		=> request =>
		{
			var query = CapturingHandler.ParseQuery(request.RequestUri!);
			var pageNo = int.Parse(query["page_no"]!);
			var pageSize = int.Parse(query["page_size"]!);
			var skipped = (pageNo - 1) * pageSize;
			var onThisPage = Math.Clamp(totalClients - skipped, 0, pageSize);

			return ClientsJson(onThisPage, firstId: skipped + 1, recordCount: onThisPage == 0 ? 0 : totalClients);
		};

	private static string ClientsJson(int count, int firstId, int? recordCount)
	{
		var clients = Enumerable.Range(firstId, count).Select(id => $$"""{"id":{{id}},"name":"Client {{id}}"}""");
		return $$"""{"clients":[{{string.Join(',', clients)}}],"record_count":{{recordCount ?? count}}}""";
	}
}
