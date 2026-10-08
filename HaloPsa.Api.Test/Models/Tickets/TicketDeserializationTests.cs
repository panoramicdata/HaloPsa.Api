using AwesomeAssertions;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Test.Infrastructure;
using Refit;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Deserialises a ticket through Refit's real pipeline. Field names are from the Faults schema in
/// Specification/swagger.json.
/// </summary>
public class TicketDeserializationTests
{
	private const string TicketJson = """
		{
			"id": 4321,
			"summary": "Printer offline",
			"status_id": 2,
			"client_id": 7,
			"user_id": 9,
			"categoryid_1": 16,
			"category_1": "Account Administration",
			"onhold": true,
			"dateoccurred": "2026-09-01T08:00:00",
			"dateclosed": "2026-09-03T17:30:00",
			"fixbydate": "2026-09-02T08:00:00",
			"slastate": "I",
			"slatimeleft": -3.5,
			"timetaken": 1.25,
			"customfields": [ { "id": 152, "name": "CFRef", "value": "ABC" } ]
		}
		""";

	private static async Task<HaloPsa.Api.Models.Tickets.Ticket> GetAsync()
	{
		var handler = new CapturingHandler(TicketJson);

		return await RestService.For<ITicketsApi>(handler.CreateClient())
			.GetByIdAsync(4321, includeDetails: true, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task CategoryId_IsReadFromCategoryid1()
		=> (await GetAsync()).CategoryId.Should().Be(16);

	[Fact]
	public async Task IsOnHold_IsReadFromOnhold()
		=> (await GetAsync()).IsOnHold.Should().BeTrue();

	[Fact]
	public async Task IsClosed_FollowsDateclosed()
		=> (await GetAsync()).IsClosed.Should().BeTrue();

	[Fact]
	public async Task UnmodelledFields_ArePreservedInAdditionalProperties()
	{
		var ticket = await GetAsync();

		_ = ticket.AdditionalProperties.Should().NotBeNull();
		_ = ticket.AdditionalProperties!["fixbydate"].GetString().Should().Be("2026-09-02T08:00:00");
		_ = ticket.AdditionalProperties["slastate"].GetString().Should().Be("I");
		_ = ticket.AdditionalProperties["slatimeleft"].GetDouble().Should().Be(-3.5);
		_ = ticket.AdditionalProperties["timetaken"].GetDouble().Should().Be(1.25);
	}

	[Fact]
	public async Task ModelledFields_AreNotDuplicatedInAdditionalProperties()
		=> (await GetAsync()).AdditionalProperties!.Keys.Should().NotContain(["id", "summary", "categoryid_1", "onhold", "customfields"]);
}
