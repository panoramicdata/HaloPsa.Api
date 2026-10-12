using AwesomeAssertions;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using HaloPsa.Api.Test.Infrastructure;
using Refit;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Reads a ticket list through Refit's real pipeline, in the shape a live tenant returned it in October 2026.
/// <c>record_count</c>, <c>page_no</c> and <c>page_size</c> are snake case, so without explicit names they
/// silently read as zero and null - which made a ticket count look like the page length.
/// </summary>
public class TicketsResponseDeserializationTests
{
	private const string PagedJson = """
		{
			"page_no": 1,
			"page_size": 2,
			"record_count": 51,
			"tickets": [ { "id": 1, "summary": "One", "status_id": 1, "client_id": 7, "user_id": 9 }, { "id": 2, "summary": "Two", "status_id": 1, "client_id": 7, "user_id": 9 } ],
			"include_children": false
		}
		""";

	private static async Task<TicketsResponse> GetAsync(string json)
	{
		var handler = new CapturingHandler(json);

		return await RestService.For<ITicketsApi>(handler.CreateClient())
			.GetAllAsync(new TicketFilter { Paginate = true, PageSize = 2, PageNo = 1 }, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task RecordCount_IsReadFromRecordCount()
		=> (await GetAsync(PagedJson)).RecordCount.Should().Be(51);

	[Fact]
	public async Task PageNoAndPageSize_AreReadFromTheirSnakeCaseNames()
	{
		var response = await GetAsync(PagedJson);

		_ = response.PageNo.Should().Be(1);
		_ = response.PageSize.Should().Be(2);
		_ = response.Tickets.Should().HaveCount(2);
	}

	[Fact]
	public async Task RecordCount_OnAnUnpagedResponse_IsThePageLength()
		=> (await GetAsync("""{ "record_count": 2, "tickets": [ { "id": 1, "summary": "One", "status_id": 1, "client_id": 7, "user_id": 9 }, { "id": 2, "summary": "Two", "status_id": 1, "client_id": 7, "user_id": 9 } ] }"""))
			.RecordCount.Should().Be(2);
}
