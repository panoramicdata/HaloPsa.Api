using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

public partial class TicketsApiTests
{
	[Fact]
	public Task CloseAsync_WithValidId_TestsEndpointBehavior()
		=> RunAgainstTestTicketAsync(async ticket =>
		{
			// Halo's status ids differ per instance, so take a closed one from the statuses it defines
			var statuses = await HaloClient.Psa.Statuses.GetAllAsync(CancellationToken.None);
			var closedStatus = statuses.First(status => status.IsClosed);

			// Act
			var result = await HaloClient.Psa.Tickets.CloseAsync(ticket.Id, closedStatus.Id, "Closed by API test", CancellationToken.None);

			// Assert - If close succeeds, verify the response
			AssertRespondsWithTicket(result, ticket.Id);
			_ = result.Ticket.IsClosed.Should().BeTrue();
		}, UnsupportedTicketOperationStatusCodes);

	[Fact]
	public async Task AssignAsync_WithValidIds_TestsEndpointBehavior()
	{
		// Arrange - Pick an agent to assign to, preferring a real agent over any user
		var users = await HaloClient.Psa.Users.GetAllAsync(CancellationToken.None);
		_ = users.Should().NotBeEmpty("Need at least one user to test assignment");

		var agent = users.FirstOrDefault(u => u.IsAgent) ?? users[0];

		await RunAgainstTestTicketAsync(async ticket =>
		{
			// Act
			var result = await HaloClient.Psa.Tickets.AssignAsync(ticket.Id, agent.Id, CancellationToken.None);

			// Assert - If assignment succeeds, verify the response
			AssertRespondsWithTicket(result, ticket.Id);
			_ = result.Ticket.AgentId.Should().Be(agent.Id);
		}, UnsupportedTicketOperationStatusCodes);
	}
}
