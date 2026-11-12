using AwesomeAssertions;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.CustomFields;
using HaloPsa.Api.Models.Tickets;
using static HaloPsa.Api.Test.Models.Tickets.TicketWireTestSupport;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins the JSON an <see cref="UpdateTicketRequest"/> produces against Halo's field names. Halo has no
/// PUT for a ticket: an update is the same POST as a create, with the ticket's id in the body, and it
/// ignores a member it does not recognise, so a wrong name fails silently on the wire.
/// </summary>
public class UpdateTicketWireTests
{
	private const string UpdatedJson = """{"id":901,"summary":"S","status_id":2,"client_id":21,"user_id":9}""";

	[Fact]
	public async Task UpdateAsync_PostsAJsonArrayHoldingTheTicketWithItsId()
	{
		var (wrapper, handler) = Create(UpdatedJson);
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		AssertPostedAsOneElementArray(handler);
		var ticket = SentBody(handler)[0];
		AssertNames(ticket, "id", "status_id");
		AssertInts(ticket, ("id", 901), ("status_id", 2));
	}

	[Fact]
	public async Task UpdateAsync_SendsEveryFieldUnderHalosFaultsName()
	{
		var (wrapper, handler) = Create(UpdatedJson);
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest
		{
			Summary = "S", Details = "D", Status = 2, Priority = 3, ClientId = 21, SiteId = 44, UserId = 9,
			AgentId = 5, TeamId = 6, CategoryId = 7, TicketTypeId = 8, ClosureNote = "Fixed",
			CustomFields = [new CustomFieldInput { Id = 1, Value = "x" }], AssetIds = [77], Tags = ["t"]
		}, CancellationToken.None);

		var ticket = SentBody(handler)[0];
		AssertNames(
			ticket,
			"id", "summary", "details", "status_id", "priority_id", "client_id", "site_id", "user_id", "agent_id", "team_id",
			"categoryid_1", "tickettype_id", "closure_note", "customfields", "assets", "tags");
		AssertInts(
			ticket,
			("id", 901), ("client_id", 21), ("site_id", 44), ("user_id", 9), ("status_id", 2), ("priority_id", 3),
			("agent_id", 5), ("team_id", 6), ("categoryid_1", 7), ("tickettype_id", 8));
		_ = ticket.GetProperty("closure_note").GetString().Should().Be("Fixed");
		_ = ticket.GetProperty("customfields").GetRawText().Should().Be("""[{"id":1,"value":"x"}]""");
		_ = ticket.GetProperty("assets").GetRawText().Should().Be("""[{"id":77}]""");
		_ = ticket.GetProperty("tags").GetRawText().Should().Be("""[{"text":"t"}]""");
	}

	[Fact]
	public async Task UpdateAsync_OmitsUnsetAndObsoleteFields()
	{
		var (wrapper, handler) = Create(UpdatedJson);
#pragma warning disable CS0618 // proving the obsolete members are never sent
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Status = 2, SuppressEmails = true, Notes = "n", CloseTicket = true, Resolution = "r" }, CancellationToken.None);
#pragma warning restore CS0618

		AssertNames(SentBody(handler)[0], "id", "status_id");
	}

	[Fact]
	public async Task UpdateAsync_ReplacesAnIdSetOnTheRequestWithTheArgument()
	{
		var (wrapper, handler) = Create(UpdatedJson);
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Id = 5, Status = 2 }, CancellationToken.None);

		AssertInts(SentBody(handler)[0], ("id", 901));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task UpdateAsync_WhenIdIsNotPositive_RefusesBeforeSending(int id)
	{
		var (wrapper, handler) = Create(UpdatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.UpdateAsync(id, new UpdateTicketRequest { Status = 2 }, CancellationToken.None), "*id*");
	}

	[Theory]
	[InlineData("ClientId")]
	[InlineData("SiteId")]
	[InlineData("UserId")]
	public async Task UpdateAsync_WhenAClientSiteOrUserIdIsSetButNotPositive_RefusesBeforeSending(string member)
	{
		var (wrapper, handler) = Create(UpdatedJson);
		var request = member switch
		{
			"ClientId" => new UpdateTicketRequest { ClientId = 0 },
			"SiteId" => new UpdateTicketRequest { SiteId = 0 },
			_ => new UpdateTicketRequest { UserId = 0 }
		};

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler, () => wrapper.UpdateAsync(901, request, CancellationToken.None), $"*{member}*");
	}

	[Fact]
	public async Task UpdateAsync_ThroughTheInterfaceList_SendsEveryRequestInOneArray()
	{
		var (wrapper, handler) = Create(UpdatedJson);

		_ = await ((ITicketsApi)wrapper).UpdateAsync(
			[new UpdateTicketRequest { Id = 901, Status = 2 }, new UpdateTicketRequest { Id = 902, Status = 3 }],
			CancellationToken.None);

		_ = handler.Requests.Should().HaveCount(1);
		_ = SentBody(handler).EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).Should().Equal(901, 902);
	}

	[Fact]
	public async Task UpdateAsync_ThroughTheInterfaceList_WhenOneOfSeveralHasNoId_SendsNone()
	{
		var (wrapper, handler) = Create(UpdatedJson);

		await AssertRefusedBeforeSendingAsync<ArgumentOutOfRangeException>(
			handler,
			() => ((ITicketsApi)wrapper).UpdateAsync(
				[new UpdateTicketRequest { Id = 901, Status = 2 }, new UpdateTicketRequest { Status = 3 }],
				CancellationToken.None),
			"*id*");
	}
}
