using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Collections.Specialized;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins the query string a <see cref="TicketFilter"/> produces against the parameter names in
/// Specification/swagger.json. Halo ignores a parameter it does not recognise, so a wrong name
/// fails silently on the wire and only a test like this catches it.
/// </summary>
public class TicketFilterQueryStringTests
{
	private static async Task<NameValueCollection> SendAsync(TicketFilter filter)
	{
		var handler = new CapturingHandler("""{"record_count":0,"tickets":[]}""");
		var wrapper = new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient()));

		_ = await wrapper.GetAllAsync(filter, TestContext.Current.CancellationToken);

		return handler.Query;
	}

	public static TheoryData<string, TicketFilter, string, string> SingleParameterCases => new()
	{
		{ "Count", new TicketFilter { Count = 7 }, "count", "7" },
		{ "Paginate", new TicketFilter { Paginate = true }, "pageinate", "true" },
		{ "PageSize", new TicketFilter { PageSize = 50 }, "page_size", "50" },
		{ "PageNo", new TicketFilter { PageNo = 3 }, "page_no", "3" },
		{ "Status", new TicketFilter { Status = "1,2" }, "status", "1,2" },
		{ "Priority", new TicketFilter { Priority = "4" }, "priority", "4" },
		{ "ClientId", new TicketFilter { ClientId = 11 }, "client_id", "11" },
		{ "SiteId", new TicketFilter { SiteId = 12 }, "site_id", "12" },
		{ "UserId", new TicketFilter { UserId = 13 }, "user_id", "13" },
		{ "AgentId", new TicketFilter { AgentId = 14 }, "agent_id", "14" },
		{ "TeamId", new TicketFilter { TeamId = 15 }, "team", "15" },
		{ "CategoryId", new TicketFilter { CategoryId = 16 }, "category_1", "16" },
		{ "TicketTypeId", new TicketFilter { TicketTypeId = 17 }, "requesttype_id", "17" },
		{ "Search", new TicketFilter { Search = "printer" }, "search", "printer" },
		{ "SearchSummary", new TicketFilter { SearchSummary = "printer" }, "search_summary", "printer" },
		{ "OpenOnly", new TicketFilter { OpenOnly = true }, "open_only", "true" },
		{ "ClosedOnly", new TicketFilter { ClosedOnly = true }, "closed_only", "true" },
		{ "MyTickets", new TicketFilter { MyTickets = true }, "mine", "true" },
		{ "Order", new TicketFilter { Order = "dateoccurred" }, "order", "dateoccurred" },
		{ "OrderDesc", new TicketFilter { OrderDesc = true }, "orderdesc", "true" },
		{ "Order2", new TicketFilter { Order2 = "id" }, "order2", "id" },
		{ "OrderDesc2", new TicketFilter { OrderDesc2 = false }, "orderdesc2", "false" },
		{ "AdvancedSearch", new TicketFilter { AdvancedSearch = "[]" }, "advanced_search", "[]" },
		{ "AssetId", new TicketFilter { AssetId = 18 }, "asset_id", "18" },
		{ "ServiceId", new TicketFilter { ServiceId = 19 }, "service_id", "19" },
		{ "SupplierId", new TicketFilter { SupplierId = 20 }, "supplier_id", "20" },
		{ "ContractId", new TicketFilter { ContractId = 21 }, "contract_id", "21" },
		{ "IncludeCustomFields", new TicketFilter { IncludeCustomFields = "5,6" }, "include_custom_fields", "5,6" },
		{ "IncludeSlaTimer", new TicketFilter { IncludeSlaTimer = true }, "includeslatimer", "true" },
		{ "IncludeTimeTaken", new TicketFilter { IncludeTimeTaken = true }, "includetimetaken", "true" },
		{ "DateSearch", new TicketFilter { DateSearch = TicketDateSearch.DateClosed }, "datesearch", "datecleared" },
	};

	[Theory]
	[MemberData(nameof(SingleParameterCases))]
	public async Task GetAllAsync_SendsEachFilterUnderItsHaloParameterName(
		string property,
		TicketFilter filter,
		string expectedName,
		string expectedValue)
	{
		var query = await SendAsync(filter);

		_ = query[expectedName].Should().BeEquivalentTo(expectedValue, $"{property} must reach Halo as '{expectedName}'");
		_ = query.AllKeys.Should().NotContain(property, $"Halo ignores the C# name '{property}'");
	}

	[Fact]
	public async Task GetAllAsync_DatesAreSentAsIsoWithTheirDateSearchField()
	{
		var query = await SendAsync(new TicketFilter
		{
			DateSearch = TicketDateSearch.DateClosed,
			StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
			EndDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
			LastUpdateAfter = new DateTime(2026, 8, 1, 6, 30, 0, DateTimeKind.Utc),
			LastUpdateBefore = new DateTime(2026, 8, 2, 6, 30, 0, DateTimeKind.Utc),
		});

		_ = query["datesearch"].Should().Be("datecleared");
		_ = query["startdate"].Should().Be("2026-09-01T00:00:00");
		_ = query["enddate"].Should().Be("2026-09-30T23:59:59");
		_ = query["lastupdatefromdate"].Should().Be("2026-08-01T06:30:00");
		_ = query["lastupdatetodate"].Should().Be("2026-08-02T06:30:00");
	}

	[Fact]
	public async Task GetAllAsync_StartDateWithoutDateSearch_SearchesTheOpenedDateAsDocumented()
	{
		var query = await SendAsync(new TicketFilter { StartDate = new DateTime(2026, 9, 1) });

		_ = query["datesearch"].Should().Be(TicketDateSearch.DateOccurred);
	}

	[Fact]
	public async Task GetAllAsync_NoDates_SendsNoDateSearch()
	{
		var query = await SendAsync(new TicketFilter { Count = 1 });

		_ = query["datesearch"].Should().BeNull();
	}

	[Fact]
	public async Task GetAllAsync_UnsetPropertiesAreNotSent()
	{
		var query = await SendAsync(new TicketFilter());

		_ = query.AllKeys.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_PropertiesWithNoHaloEquivalentAreNotSent()
	{
#pragma warning disable CS0618 // Exercising the obsolete members is the point of this test
		var query = await SendAsync(new TicketFilter { UnassignedOnly = true, IncludeChildren = true, IncludeDetails = true });
#pragma warning restore CS0618

		_ = query.AllKeys.Should().BeEmpty();
	}
}
