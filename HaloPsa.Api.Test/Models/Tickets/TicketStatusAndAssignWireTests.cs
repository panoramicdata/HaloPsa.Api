using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins what closing, reopening and assigning a ticket send. Halo has no close, reopen or assign route:
/// each is the update POST with the ticket's id, so the wire is the update's, carrying only what was asked for.
/// </summary>
public class TicketStatusAndAssignWireTests
{
	private const string UpdatedJson = """{"id":901,"summary":"S","status_id":2,"client_id":21,"user_id":9}""";

	private static (TicketsApiWrapper Wrapper, CapturingHandler Handler) Create()
	{
		var handler = new CapturingHandler(UpdatedJson);
		return (new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient())), handler);
	}

	private static JsonElement SentTicket(CapturingHandler handler)
	{
		_ = handler.Methods.Should().Equal(HttpMethod.Post);
		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets");

		var body = JsonDocument.Parse(handler.RequestBodies[^1]!).RootElement;
		_ = body.ValueKind.Should().Be(JsonValueKind.Array);
		_ = body.GetArrayLength().Should().Be(1);

		return body[0];
	}

	[Fact]
	public async Task CloseAsync_SendsTheClosedStatus()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.CloseAsync(901, 31, CancellationToken.None);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status_id");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(31);
	}

	[Fact]
	public async Task CloseAsync_WithANote_SendsTheClosureNote()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.CloseAsync(901, 31, "Fixed", CancellationToken.None);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status_id", "closure_note");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(31);
		_ = ticket.GetProperty("closure_note").GetString().Should().Be("Fixed");
	}

	[Fact]
	public async Task ReopenAsync_SendsTheOpenStatus()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.ReopenAsync(901, 32, CancellationToken.None);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status_id");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("status_id").GetInt32().Should().Be(32);
	}

	[Fact]
	public async Task AssignAsync_SendsTheAgent()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.AssignAsync(901, 5, CancellationToken.None);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "agent_id");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("agent_id").GetInt32().Should().Be(5);
	}

	[Fact]
	public async Task AssignAsync_WithATeam_SendsTheAgentAndTeam()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.AssignAsync(901, 5, 6, CancellationToken.None);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "agent_id", "team_id");
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);
		_ = ticket.GetProperty("agent_id").GetInt32().Should().Be(5);
		_ = ticket.GetProperty("team_id").GetInt32().Should().Be(6);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task CloseAsync_WhenTheStatusIdIsNotPositive_RefusesBeforeSending(int closedStatusId)
	{
		var (wrapper, handler) = Create();

		var withoutNote = async () => await wrapper.CloseAsync(901, closedStatusId, CancellationToken.None);
		var withNote = async () => await wrapper.CloseAsync(901, closedStatusId, "Fixed", CancellationToken.None);

		_ = (await withoutNote.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("closedStatusId");
		_ = (await withNote.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("closedStatusId");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task ReopenAsync_WhenTheStatusIdIsNotPositive_RefusesBeforeSending(int openStatusId)
	{
		var (wrapper, handler) = Create();

		var act = async () => await wrapper.ReopenAsync(901, openStatusId, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("openStatusId");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task AssignAsync_WhenTheAgentIdIsNotPositive_RefusesBeforeSending(int agentId)
	{
		var (wrapper, handler) = Create();

		var withoutTeam = async () => await wrapper.AssignAsync(901, agentId, CancellationToken.None);
		var withTeam = async () => await wrapper.AssignAsync(901, agentId, 6, CancellationToken.None);

		_ = (await withoutTeam.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("agentId");
		_ = (await withTeam.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("agentId");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task AssignAsync_WhenTheTeamIdIsNotPositive_RefusesBeforeSending(int teamId)
	{
		var (wrapper, handler) = Create();

		var act = async () => await wrapper.AssignAsync(901, 5, teamId, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("teamId");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task CloseAsync_WhenTheNoteIsBlank_RefusesBeforeSending(string note)
	{
		var (wrapper, handler) = Create();

		var act = async () => await wrapper.CloseAsync(901, 31, note, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("closureNote");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Fact]
	public async Task CloseAsync_WhenTheNoteIsNull_RefusesBeforeSending()
	{
		var (wrapper, handler) = Create();

		var act = async () => await wrapper.CloseAsync(901, 31, null!, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("closureNote");
		_ = handler.Requests.Should().BeEmpty();
	}
}
