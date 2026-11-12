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

	private static async Task AssertSendsAsync(Func<TicketsApiWrapper, Task> call, params (string Name, object Value)[] expected)
	{
		var (wrapper, handler) = Create();

		await call(wrapper);

		var ticket = SentTicket(handler);
		_ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["id", .. expected.Select(e => e.Name)]);
		_ = ticket.GetProperty("id").GetInt32().Should().Be(901);

		foreach (var (name, value) in expected)
		{
			var property = ticket.GetProperty(name);
			_ = property.ToString().Should().Be(value.ToString());
		}
	}

	private static async Task AssertRefusedAsync<TException>(string paramName, params Func<TicketsApiWrapper, Task>[] calls)
		where TException : ArgumentException
	{
		var (wrapper, handler) = Create();

		foreach (var call in calls)
		{
			var thrown = await FluentActions.Awaiting(() => call(wrapper)).Should().ThrowAsync<TException>();
			_ = thrown.Which.ParamName.Should().Be(paramName);
		}

		_ = handler.Requests.Should().BeEmpty();
	}

	[Fact]
	public Task CloseAsync_SendsTheClosedStatus()
		=> AssertSendsAsync(w => w.CloseAsync(901, 31, CancellationToken.None), ("status_id", 31));

	[Fact]
	public Task CloseAsync_WithANote_SendsTheClosureNote()
		=> AssertSendsAsync(w => w.CloseAsync(901, 31, "Fixed", CancellationToken.None), ("status_id", 31), ("closure_note", "Fixed"));

	[Fact]
	public Task ReopenAsync_SendsTheOpenStatus()
		=> AssertSendsAsync(w => w.ReopenAsync(901, 32, CancellationToken.None), ("status_id", 32));

	[Fact]
	public Task AssignAsync_SendsTheAgent()
		=> AssertSendsAsync(w => w.AssignAsync(901, 5, CancellationToken.None), ("agent_id", 5));

	[Fact]
	public Task AssignAsync_WithATeam_SendsTheAgentAndTeam()
		=> AssertSendsAsync(w => w.AssignAsync(901, 5, 6, CancellationToken.None), ("agent_id", 5), ("team_id", 6));

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public Task CloseAsync_WhenTheStatusIdIsNotPositive_RefusesBeforeSending(int closedStatusId)
		=> AssertRefusedAsync<ArgumentOutOfRangeException>(
			"closedStatusId",
			w => w.CloseAsync(901, closedStatusId, CancellationToken.None),
			w => w.CloseAsync(901, closedStatusId, "Fixed", CancellationToken.None));

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public Task ReopenAsync_WhenTheStatusIdIsNotPositive_RefusesBeforeSending(int openStatusId)
		=> AssertRefusedAsync<ArgumentOutOfRangeException>("openStatusId", w => w.ReopenAsync(901, openStatusId, CancellationToken.None));

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public Task AssignAsync_WhenTheAgentIdIsNotPositive_RefusesBeforeSending(int agentId)
		=> AssertRefusedAsync<ArgumentOutOfRangeException>(
			"agentId",
			w => w.AssignAsync(901, agentId, CancellationToken.None),
			w => w.AssignAsync(901, agentId, 6, CancellationToken.None));

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public Task AssignAsync_WhenTheTeamIdIsNotPositive_RefusesBeforeSending(int teamId)
		=> AssertRefusedAsync<ArgumentOutOfRangeException>("teamId", w => w.AssignAsync(901, 5, teamId, CancellationToken.None));

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public Task CloseAsync_WhenTheNoteIsBlank_RefusesBeforeSending(string note)
		=> AssertRefusedAsync<ArgumentException>("closureNote", w => w.CloseAsync(901, 31, note, CancellationToken.None));

	[Fact]
	public Task CloseAsync_WhenTheNoteIsNull_RefusesBeforeSending()
		=> AssertRefusedAsync<ArgumentException>("closureNote", w => w.CloseAsync(901, 31, null!, CancellationToken.None));
}
