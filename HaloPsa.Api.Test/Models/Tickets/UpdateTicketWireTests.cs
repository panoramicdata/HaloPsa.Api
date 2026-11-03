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
/// Pins the JSON an <see cref="UpdateTicketRequest"/> produces against Halo's field names. Halo has no
/// PUT for a ticket: an update is the same POST as a create, with the ticket's id in the body, and it
/// ignores a member it does not recognise, so a wrong name fails silently on the wire.
/// </summary>
public class UpdateTicketWireTests
{
	private const string UpdatedJson = """{"id":901,"summary":"S","status_id":2,"client_id":21,"user_id":9}""";

	private static (TicketsApiWrapper Wrapper, CapturingHandler Handler) Create()
	{
		var handler = new CapturingHandler(UpdatedJson);
		return (new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient())), handler);
	}

	private static JsonElement SentBody(CapturingHandler handler) => JsonDocument.Parse(handler.RequestBodies[^1]!).RootElement;

	[Fact]
	public async Task UpdateAsync_PostsAJsonArrayHoldingTheTicketWithItsId()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		_ = handler.Methods.Should().Equal(HttpMethod.Post);
		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets");
		_ = SentBody(handler).ValueKind.Should().Be(JsonValueKind.Array);
		_ = SentBody(handler).GetArrayLength().Should().Be(1);

		var ticket = SentBody(handler)[0];
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status_id");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(2);
	}

	[Fact]
	public async Task UpdateAsync_SendsEveryFieldUnderHalosFaultsName()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest
		{
			Summary = "S", Details = "D", Status = 2, Priority = 3, ClientId = 21, SiteId = 44, UserId = 9,
			AgentId = 5, TeamId = 6, CategoryId = 7, TicketTypeId = 8, ClosureNote = "Fixed",
			CustomFields = [new CustomFieldInput { Id = 1, Value = "x" }], AssetIds = [77], Tags = ["t"]
		}, CancellationToken.None);

		var ticket = SentBody(handler)[0];
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
			"id", "summary", "details", "status_id", "priority_id", "client_id", "site_id", "user_id", "agent_id", "team_id",
			"categoryid_1", "tickettype_id", "closure_note", "customfields", "assets", "tags");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("client_id").GetInt32().Should().Be(21);
		_ = ticket.GetProperty("site_id").GetInt32().Should().Be(44);
		_ = ticket.GetProperty("user_id").GetInt32().Should().Be(9);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(2);
		_ = ticket.GetProperty("priority_id").GetInt32().Should().Be(3);
		_ = ticket.GetProperty("agent_id").GetInt32().Should().Be(5);
		_ = ticket.GetProperty("team_id").GetInt32().Should().Be(6);
		_ = ticket.GetProperty("categoryid_1").GetInt32().Should().Be(7);
		_ = ticket.GetProperty("tickettype_id").GetInt32().Should().Be(8);
		_ = ticket.GetProperty("closure_note").GetString().Should().Be("Fixed");
		_ = ticket.GetProperty("customfields").GetRawText().Should().Be("""[{"id":1,"value":"x"}]""");
		_ = ticket.GetProperty("assets").GetRawText().Should().Be("""[{"id":77}]""");
		_ = ticket.GetProperty("tags").GetRawText().Should().Be("""[{"text":"t"}]""");
	}

	[Fact]
	public async Task UpdateAsync_OmitsUnsetAndObsoleteFields()
	{
		var (wrapper, handler) = Create();
#pragma warning disable CS0618 // proving the obsolete members are never sent
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Status = 2, SuppressEmails = true, Notes = "n", CloseTicket = true, Resolution = "r" }, CancellationToken.None);
#pragma warning restore CS0618

		_ = SentBody(handler)[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status_id");
	}

	[Fact]
	public async Task UpdateAsync_ReplacesAnIdSetOnTheRequestWithTheArgument()
	{
		var (wrapper, handler) = Create();
		_ = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Id = 5, Status = 2 }, CancellationToken.None);

		_ = SentBody(handler)[0].GetProperty("id").GetInt32().Should().Be(901);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task UpdateAsync_WhenIdIsNotPositive_RefusesBeforeSending(int id)
	{
		var (wrapper, handler) = Create();

		var act = async () => await wrapper.UpdateAsync(id, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage("*id*");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData("ClientId")]
	[InlineData("SiteId")]
	[InlineData("UserId")]
	public async Task UpdateAsync_WhenAClientSiteOrUserIdIsSetButNotPositive_RefusesBeforeSending(string member)
	{
		var (wrapper, handler) = Create();
		var request = member switch
		{
			"ClientId" => new UpdateTicketRequest { ClientId = 0 },
			"SiteId" => new UpdateTicketRequest { SiteId = 0 },
			_ => new UpdateTicketRequest { UserId = 0 }
		};

		var act = async () => await wrapper.UpdateAsync(901, request, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage($"*{member}*");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Fact]
	public async Task UpdateAsync_ThroughTheInterfaceList_SendsEveryRequestInOneArray()
	{
		var (wrapper, handler) = Create();

		_ = await ((ITicketsApi)wrapper).UpdateAsync(
			[new UpdateTicketRequest { Id = 901, Status = 2 }, new UpdateTicketRequest { Id = 902, Status = 3 }],
			CancellationToken.None);

		_ = handler.Requests.Should().HaveCount(1);
		_ = SentBody(handler).EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).Should().Equal(901, 902);
	}

	[Fact]
	public async Task UpdateAsync_ThroughTheInterfaceList_WhenOneOfSeveralHasNoId_SendsNone()
	{
		var (wrapper, handler) = Create();

		var act = async () => await ((ITicketsApi)wrapper).UpdateAsync(
			[new UpdateTicketRequest { Id = 901, Status = 2 }, new UpdateTicketRequest { Status = 3 }],
			CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage("*id*");
		_ = handler.Requests.Should().BeEmpty();
	}
}
