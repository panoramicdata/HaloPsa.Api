using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Actions;
using HaloPsa.Api.Test.Infrastructure;
using Refit;

namespace HaloPsa.Api.Test.Models.Actions;

/// <summary>
/// Parameter and field names are from the Actions schema and <c>GET /Actions</c> in
/// Specification/swagger.json; the wrapped response shape is the live sandbox's
/// (<c>{"record_count":0,"actions":[]}</c>, 2026-10-08).
/// </summary>
public class ActionsApiTests
{
	private const string ActionsJson = """
		{
			"record_count": 2,
			"actions": [
				{ "id": 1, "ticket_id": 4321, "outcome": "Note", "who": "Ann Agent", "who_agentid": 5,
				  "note": "Rebooted the printer", "datetime": "2026-09-01T09:15:00", "timetaken": 0.25,
				  "hiddenfromuser": true, "important": false, "attachment_count": 1, "actionarrivaldate": "2026-09-01T09:15:00" },
				{ "id": 2, "ticket_id": 4321, "outcome": "Closed", "who": "Ann Agent", "note": "Fixed" }
			]
		}
		""";

	private static (ActionsApiWrapper Wrapper, CapturingHandler Handler) Create()
	{
		var handler = new CapturingHandler(ActionsJson);
		return (new ActionsApiWrapper(RestService.For<IActionsApi>(handler.CreateClient())), handler);
	}

	[Fact]
	public void HaloClient_ExposesActionsAndPriorities()
	{
		using var client = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});

		_ = client.Psa.Actions.Should().NotBeNull();
		_ = client.Psa.Priorities.Should().NotBeNull();
	}

	[Fact]
	public async Task GetForTicketAsync_RequestsActionsForThatTicket()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.GetForTicketAsync(4321, TestContext.Current.CancellationToken);

		_ = handler.RequestUri!.AbsolutePath.Should().Be("/api/Actions");
		_ = handler.Query["ticket_id"].Should().Be("4321");
	}

	[Fact]
	public async Task GetAllAsync_SendsFilterUnderHaloParameterNames()
	{
		var (wrapper, handler) = Create();

		_ = await wrapper.GetAllAsync(
			new ActionFilter { TicketId = 4321, Count = 50, ExcludeSystem = true, ExcludePrivate = true, AgentOnly = true, ConversationOnly = true, ImportantOnly = true, IncludeAttachments = true },
			TestContext.Current.CancellationToken);

		var query = handler.Query;
		_ = query["ticket_id"].Should().Be("4321");
		_ = query["count"].Should().Be("50");
		_ = query["excludesys"].Should().BeEquivalentTo("true");
		_ = query["excludeprivate"].Should().BeEquivalentTo("true");
		_ = query["agentonly"].Should().BeEquivalentTo("true");
		_ = query["conversationonly"].Should().BeEquivalentTo("true");
		_ = query["importantonly"].Should().BeEquivalentTo("true");
		_ = query["includeattachments"].Should().BeEquivalentTo("true");
		_ = query.AllKeys.Should().HaveCount(8);
	}

	[Fact]
	public async Task GetForTicketAsync_ReadsModelledAndUnmodelledFields()
	{
		var (wrapper, _) = Create();

		var response = await wrapper.GetForTicketAsync(4321, TestContext.Current.CancellationToken);

		_ = response.RecordCount.Should().Be(2);
		var first = response.Actions[0];
		_ = first.Id.Should().Be(1);
		_ = first.TicketId.Should().Be(4321);
		_ = first.Outcome.Should().Be("Note");
		_ = first.Who.Should().Be("Ann Agent");
		_ = first.WhoAgentId.Should().Be(5);
		_ = first.Note.Should().Be("Rebooted the printer");
		_ = first.DateTime.Should().Be(new DateTime(2026, 9, 1, 9, 15, 0));
		_ = first.TimeTaken.Should().Be(0.25);
		_ = first.HiddenFromUser.Should().BeTrue();
		_ = first.AttachmentCount.Should().Be(1);
		_ = first.AdditionalProperties!.Should().ContainKey("actionarrivaldate");
	}
}
