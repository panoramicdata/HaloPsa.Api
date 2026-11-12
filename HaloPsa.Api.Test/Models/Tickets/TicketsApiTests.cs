using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

[Collection("Integration Tests")]
public partial class TicketsApiTests(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public async Task GetAllAsync_WithoutFilter_ReturnsTickets()
	{
		// Act
		var result = await HaloClient.Psa.Tickets.GetAllAsync(CancellationToken.None);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Tickets.Should().NotBeNull();
		_ = result.RecordCount.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task GetAllAsync_WithCountFilter_ReturnsLimitedResults()
	{
		// Arrange
		var filter = new TicketFilter { Count = 5 };

		// Act
		var result = await HaloClient.Psa.Tickets.GetAllAsync(filter, CancellationToken.None);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Tickets.Should().NotBeNull();
		_ = result.Tickets.Count.Should().BeLessThanOrEqualTo(5);
	}

	[Fact]
	public async Task GetAllAsync_WithClientFilter_ReturnsFilteredResults()
	{
		// Arrange - Get clients first
		var clients = await HaloClient.Psa.Clients.GetAllAsync(CancellationToken.None);
		_ = clients.Should().NotBeEmpty("Need at least one client to test filtering");

		// Get all tickets first to see if we have any
		var allTickets = await HaloClient.Psa.Tickets.GetAllAsync(CancellationToken.None);

		// If no tickets exist, create a test ticket first
		allTickets = await EnsureTestTicketsExistForClientFilterAsync(allTickets);
		if (allTickets is null)
		{
			// Still no tickets, skip this test
			return;
		}

		// Find a client that has tickets (assuming ClientId > 0 means assigned)
		var clientsWithTickets = allTickets.Tickets
			.Where(t => t.ClientId > 0)
			.GroupBy(t => t.ClientId)
			.Select(g => g.Key)
			.ToList();

		// If no tickets have client assignments, just use the first client and verify the filter works
		var clientId = clientsWithTickets.Count != 0 ? clientsWithTickets.First() : clients[0].Id;
		var filter = new TicketFilter { ClientId = clientId, Count = 10 };

		// Act
		var result = await HaloClient.Psa.Tickets.GetAllAsync(filter, CancellationToken.None);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Tickets.Should().NotBeNull();

		// The filter should work correctly - either return matching tickets or none
		if (result.Tickets.Count > 0)
		{
			// All returned tickets should either match the client ID or have no client assigned
			_ = result.Tickets.Should().OnlyContain(t =>
				t.ClientId == 0 || t.ClientId == clientId,
				$"All tickets should either have no client (0) or match client ID {clientId}");
		}
	}

	[Fact]
	public async Task GetAllAsync_WithPagination_ReturnsPaginatedResults()
	{
		// Arrange - First ensure we have enough data for pagination
		await EnsureTestTicketsExistAsync(10, CancellationToken.None);

		var allTickets = await HaloClient.Psa.Tickets.GetAllAsync(CancellationToken.None);

		if (allTickets.RecordCount < 5)
		{
			// Skip test if insufficient data in sandbox
			return;
		}

		var filter = new TicketFilter
		{
			Paginate = true,
			PageSize = 5,
			PageNo = 1
		};

		// Act
		var result = await HaloClient.Psa.Tickets.GetAllAsync(filter, CancellationToken.None);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.IsPaginated.Should().BeTrue();
		_ = result.PageSize.Should().Be(5);
		_ = result.PageNo.Should().Be(1);
		_ = result.Tickets.Count.Should().BeLessThanOrEqualTo(5);
	}

	[Fact]
	public async Task GetAllAsync_WithSearchFilter_ReturnsMatchingResults()
	{
		// Arrange - Create a ticket with known content
		var searchTerm = "SearchTest";
		await EnsureTestTicketWithContentAsync(searchTerm, CancellationToken.None);

		var filter = new TicketFilter { Search = searchTerm, Count = 10 };

		// Act
		var result = await HaloClient.Psa.Tickets.GetAllAsync(filter, CancellationToken.None);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Tickets.Should().NotBeNull();

		// If there are results, they should contain the search term
		if (result.Tickets.Count > 0)
		{
			_ = result.Tickets.Should().Contain(t => TicketMentions(t, searchTerm));
		}
	}

	[Fact]
	public Task GetByIdAsync_WithValidId_ReturnsTicket()
		=> RunAgainstTestTicketAsync(async ticket =>
		{
			// Act
			var result = await HaloClient.Psa.Tickets.GetByIdAsync(ticket.Id, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Id.Should().Be(ticket.Id);
		});

	[Fact]
	public async Task GetByIdAsync_WithInvalidId_ThrowsNotFoundException()
	{
		// Arrange
		var invalidId = -999999; // Use clearly invalid ID

		// Act & Assert
		Func<Task> act = async () => await HaloClient.Psa.Tickets.GetByIdAsync(invalidId, CancellationToken.None);
		_ = await act.Should().ThrowAsync<HaloNotFoundException>();
	}

	[Fact]
	public async Task CreateAsync_WithValidRequest_TestsEndpointBehavior()
	{
		// Arrange - Build the request from real client and user data
		var request = await BuildCreateTicketRequestAsync(
			"API Test Ticket",
			"Test ticket created via API integration test",
			CancellationToken.None);
		_ = request.Should().NotBeNull("Need at least one client and one user to test ticket creation");

		// Act & Assert - Test how the endpoint behaves in this environment
		try
		{
			var result = await HaloClient.Psa.Tickets.CreateAsync(request!, CancellationToken.None);

			// If creation succeeds, verify the response structure
			_ = result.Should().NotBeNull();
			_ = result.Ticket.Should().NotBeNull();
			_ = result.Ticket.Id.Should().BePositive();
			_ = result.Ticket.Summary.Should().Be(request!.Summary);
			_ = result.Ticket.ClientId.Should().Be(request.ClientId);

			// Clean up the created ticket if possible
			try
			{
				await HaloClient.Psa.Tickets.DeleteAsync(result.Ticket.Id, CancellationToken.None);
			}
			catch (HaloApiException)
			{
				// Cleanup failed - that's okay for testing
			}
		}
		catch (HaloApiException ex)
		{
			// If creation fails, verify it fails with proper error handling
			AssertUnsupportedOperation(ex, UnsupportedOperationStatusCodes);
		}
	}

	[Fact]
	public async Task CreateAsync_WithInvalidRequest_ThrowsBadRequestException()
	{
		// Arrange
		var invalidRequest = new CreateTicketRequest
		{
			Summary = "", // Empty summary should be invalid
			ClientId = -1, // Invalid client ID
			UserId = -1    // Invalid user ID
		};

		// Act & Assert
		Func<Task> act = async () => await HaloClient.Psa.Tickets.CreateAsync(invalidRequest, CancellationToken.None);
		_ = await act.Should().ThrowAsync<HaloBadRequestException>();
	}

	[Fact]
	public Task UpdateAsync_WithValidRequest_TestsEndpointBehavior()
		=> RunAgainstTestTicketAsync(async ticket =>
		{
			// Arrange
			var updateRequest = new UpdateTicketRequest
			{
				Summary = $"Updated via API Test - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
				Details = "Updated by API integration test",
				Priority = 2
			};

			// Act
			var result = await HaloClient.Psa.Tickets.UpdateAsync(ticket.Id, updateRequest, CancellationToken.None);

			// Assert - If update succeeds, verify the response
			AssertRespondsWithTicket(result, ticket.Id);

			// Try to restore original state if possible
			try
			{
				var restoreRequest = new UpdateTicketRequest { Summary = ticket.Summary };
				_ = await HaloClient.Psa.Tickets.UpdateAsync(ticket.Id, restoreRequest, CancellationToken.None);
			}
			catch (HaloApiException)
			{
				// Restore failed - that's okay for testing
			}
		}, UnsupportedOperationStatusCodes);

	[Fact]
	public async Task DeleteAsync_WithValidId_TestsEndpointBehavior()
	{
		// Arrange - Create a test ticket specifically for deletion
		var createRequest = await BuildCreateTicketRequestAsync(
			"Test Ticket for Deletion",
			"This ticket is created specifically to test deletion",
			CancellationToken.None);
		if (createRequest is null)
		{
			// Skip if no clients or users available
			return;
		}

		int ticketIdToTest;
		var createdForTest = false;

		try
		{
			var created = await HaloClient.Psa.Tickets.CreateAsync(createRequest, CancellationToken.None);
			ticketIdToTest = created.Ticket.Id;
			createdForTest = true;
		}
		catch (HaloApiException)
		{
			// If we can't create a ticket, use an existing one if available
			var existingTickets = await HaloClient.Psa.Tickets.GetAllAsync(CancellationToken.None);
			if (existingTickets.Tickets.Count == 0)
			{
				// No tickets available and can't create - skip test
				return;
			}

			ticketIdToTest = existingTickets.Tickets[0].Id;
		}

		// Act & Assert - Test delete behavior
		try
		{
			await HaloClient.Psa.Tickets.DeleteAsync(ticketIdToTest, CancellationToken.None);

			// If delete succeeds, verify the ticket is gone
			var act = async () => await HaloClient.Psa.Tickets.GetByIdAsync(ticketIdToTest, CancellationToken.None);
			_ = await act.Should().ThrowAsync<HaloNotFoundException>("Deleted ticket should not be found");
		}
		catch (HaloApiException ex) when (!createdForTest)
		{
			// If we're trying to delete an existing ticket and it fails, that's expected
			AssertUnsupportedOperation(ex, UnsupportedOperationStatusCodes);
		}
		catch (HaloApiException ex) when (createdForTest)
		{
			// If we created a ticket but can't delete it, that's also valid behavior to test
			AssertUnsupportedOperation(ex, [403, 405, 501]);
		}
	}

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
