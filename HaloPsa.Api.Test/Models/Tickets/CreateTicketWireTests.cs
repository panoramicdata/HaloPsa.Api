using AwesomeAssertions;
using HaloPsa.Api.Models.CustomFields;
using HaloPsa.Api.Models.Tickets;
using static HaloPsa.Api.Test.Models.Tickets.TicketWireTestSupport;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins the JSON a <see cref="CreateTicketRequest"/> produces against Halo's field names. Halo ignores a
/// member it does not recognise, so a wrong name fails silently on the wire and only a test like this catches it.
/// </summary>
public class CreateTicketWireTests
{
	private const string CreatedJson = """{"ticket":{"id":901,"summary":"S","status_id":1,"client_id":21,"user_id":9}}""";

	private static CreateTicketRequest Valid(string summary = "S", int clientId = 21, int userId = 9)
		=> new() { Summary = summary, ClientId = clientId, UserId = userId };

	[Fact]
	public async Task CreateAsync_PostsAJsonArrayHoldingOneTicket()
	{
		var (wrapper, handler) = Create(CreatedJson);
		_ = await wrapper.CreateAsync(Valid(), CancellationToken.None);

		AssertPostedAsOneElementArray(handler);
	}

	[Fact]
	public async Task CreateAsync_SendsEveryFieldUnderHalosFaultsName()
	{
		var (wrapper, handler) = Create(CreatedJson);
		_ = await wrapper.CreateAsync(new CreateTicketRequest
		{
			Summary = "S", Details = "D", DetailsHtml = "<p>D</p>", Status = 2, Priority = 3, ClientId = 21, SiteId = 44, UserId = 9,
			AgentId = 5, TeamId = 6, CategoryId = 7, TicketTypeId = 8,
			DateOccurred = new DateTime(2026, 11, 3, 10, 15, 0, DateTimeKind.Utc), TargetDate = new DateTime(2026, 11, 6, 10, 15, 0, DateTimeKind.Utc),
			Source = 1, CustomFields = [new CustomFieldInput { Id = 1, Value = "cisco-sa-test" }], AssetIds = [77], Tags = ["cvss"]
		}, CancellationToken.None);

		var ticket = SentBody(handler)[0];
		AssertNames(
			ticket,
			"summary", "details", "details_html", "status_id", "priority_id", "client_id", "site_id", "user_id", "agent_id", "team_id",
			"categoryid_1", "tickettype_id", "dateoccurred", "targetdate", "source", "customfields", "assets", "tags");
		AssertInts(ticket, ("client_id", 21), ("site_id", 44), ("user_id", 9), ("status_id", 2), ("priority_id", 3), ("categoryid_1", 7), ("tickettype_id", 8));
		_ = ticket.GetProperty("dateoccurred").GetString().Should().Be("2026-11-03T10:15:00Z");
		_ = ticket.GetProperty("customfields").GetRawText().Should().Be("""[{"id":1,"value":"cisco-sa-test"}]""");
		_ = ticket.GetProperty("assets").GetRawText().Should().Be("""[{"id":77}]""");
		_ = ticket.GetProperty("tags").GetRawText().Should().Be("""[{"text":"cvss"}]""");
	}

	[Fact]
	public async Task CreateAsync_OmitsUnsetAndObsoleteFields()
	{
		var (wrapper, handler) = Create(CreatedJson);
#pragma warning disable CS0618 // proving the obsolete members are never sent
		_ = await wrapper.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9, SuppressEmails = true, Notes = "n" }, CancellationToken.None);
#pragma warning restore CS0618

		AssertNames(SentBody(handler)[0], "summary", "client_id", "user_id");
	}

	[Fact]
	public async Task CreateAsync_WithAList_SendsEveryRequestInOneArray()
	{
		var (wrapper, handler) = Create(CreatedJson);
		_ = await wrapper.CreateAsync([Valid("A"), Valid("B", clientId: 22)], CancellationToken.None);

		_ = handler.Requests.Should().HaveCount(1);
		_ = SentBody(handler).EnumerateArray().Select(t => t.GetProperty("summary").GetString()).Should().Equal("A", "B");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task CreateAsync_WhenClientIdIsNotPositive_RefusesBeforeSending(int id)
	{
		var (wrapper, handler) = Create(CreatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.CreateAsync(Valid(clientId: id), CancellationToken.None), "*ClientId*");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task CreateAsync_WhenUserIdIsNotPositive_RefusesBeforeSending(int id)
	{
		var (wrapper, handler) = Create(CreatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.CreateAsync(Valid(userId: id), CancellationToken.None), "*UserId*");
	}

	[Fact]
	public async Task CreateAsync_WhenSiteIdIsNotPositive_RefusesBeforeSending()
	{
		var (wrapper, handler) = Create(CreatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.CreateAsync(Valid() with { SiteId = 0 }, CancellationToken.None), "*SiteId*");
	}

	[Fact]
	public async Task CreateAsync_WithAnEmptyList_RefusesBeforeSending()
	{
		var (wrapper, handler) = Create(CreatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentException>(handler, () => wrapper.CreateAsync([], CancellationToken.None), "*");
	}

	[Fact]
	public async Task CreateAsync_WhenOneOfSeveralIsInvalid_SendsNone()
	{
		var (wrapper, handler) = Create(CreatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.CreateAsync([Valid("A"), Valid("B", userId: 0)], CancellationToken.None), "*UserId*");
	}
}
