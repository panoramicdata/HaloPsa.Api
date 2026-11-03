using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.CustomFields;
using HaloPsa.Api.Models.Tickets;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins the JSON a <see cref="CreateTicketRequest"/> produces against Halo's field names. Halo ignores a
/// member it does not recognise, so a wrong name fails silently on the wire and only a test like this catches it.
/// </summary>
public class CreateTicketWireTests
{
	private const string CreatedJson = """{"ticket":{"id":901,"summary":"S","status_id":1,"client_id":21,"user_id":9}}""";

	private static (TicketsApiWrapper Wrapper, CapturingHandler Handler) Create()
	{
		var handler = new CapturingHandler(CreatedJson);
		return (new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient())), handler);
	}

	private static JsonElement SentBody(CapturingHandler handler) => JsonDocument.Parse(handler.RequestBodies[^1]!).RootElement;

	[Fact]
	public async Task CreateAsync_PostsAJsonArrayHoldingOneTicket()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		_ = handler.Methods.Should().Equal(HttpMethod.Post);
		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets");
		_ = SentBody(handler).ValueKind.Should().Be(JsonValueKind.Array);
		_ = SentBody(handler).GetArrayLength().Should().Be(1);
	}

	[Fact]
	public async Task CreateAsync_SendsEveryFieldUnderHalosFaultsName()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.CreateAsync(new CreateTicketRequest
		{
			Summary = "S", Details = "D", DetailsHtml = "<p>D</p>", Status = 2, Priority = 3, ClientId = 21, SiteId = 44, UserId = 9,
			AgentId = 5, TeamId = 6, CategoryId = 7, TicketTypeId = 8,
			DateOccurred = new DateTime(2026, 11, 3, 10, 15, 0, DateTimeKind.Utc), TargetDate = new DateTime(2026, 11, 6, 10, 15, 0, DateTimeKind.Utc),
			Source = 1, CustomFields = [new CustomFieldInput { Id = 1, Value = "cisco-sa-test" }], AssetIds = [77], Tags = ["cvss"]
		}, CancellationToken.None);

		var ticket = SentBody(handler)[0];
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
			"summary", "details", "details_html", "status_id", "priority_id", "client_id", "site_id", "user_id", "agent_id", "team_id",
			"categoryid_1", "tickettype_id", "dateoccurred", "targetdate", "source", "customfields", "assets", "tags");
		_ = ticket.GetProperty("client_id").GetInt32().Should().Be(21);
		_ = ticket.GetProperty("site_id").GetInt32().Should().Be(44);
		_ = ticket.GetProperty("user_id").GetInt32().Should().Be(9);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(2);
		_ = ticket.GetProperty("priority_id").GetInt32().Should().Be(3);
		_ = ticket.GetProperty("categoryid_1").GetInt32().Should().Be(7);
		_ = ticket.GetProperty("tickettype_id").GetInt32().Should().Be(8);
		_ = ticket.GetProperty("dateoccurred").GetString().Should().Be("2026-11-03T10:15:00Z");
		_ = ticket.GetProperty("customfields").GetRawText().Should().Be("""[{"id":1,"value":"cisco-sa-test"}]""");
		_ = ticket.GetProperty("assets").GetRawText().Should().Be("""[{"id":77}]""");
		_ = ticket.GetProperty("tags").GetRawText().Should().Be("""[{"text":"cvss"}]""");
	}

	[Fact]
	public async Task CreateAsync_OmitsUnsetAndObsoleteFields()
	{
		var (wrapper, handler) = Create();
#pragma warning disable CS0618 // proving the obsolete members are never sent
		_ = await wrapper.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9, SuppressEmails = true, Notes = "n" }, CancellationToken.None);
#pragma warning restore CS0618

		_ = SentBody(handler)[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("summary", "client_id", "user_id");
	}

	[Fact]
	public async Task CreateAsync_WithAList_SendsEveryRequestInOneArray()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.CreateAsync(
			[new CreateTicketRequest { Summary = "A", ClientId = 21, UserId = 9 }, new CreateTicketRequest { Summary = "B", ClientId = 22, UserId = 9 }],
			CancellationToken.None);

		_ = handler.Requests.Should().HaveCount(1);
		_ = SentBody(handler).EnumerateArray().Select(t => t.GetProperty("summary").GetString()).Should().Equal("A", "B");
	}
}
