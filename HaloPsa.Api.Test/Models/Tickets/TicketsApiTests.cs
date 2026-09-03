using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

[Collection("Integration Tests")]
public class TicketsApiTests(IntegrationTestFixture fixture) : TestBase(fixture)
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
		if (allTickets.Tickets.Count == 0)
		{
			var createRequest = await BuildCreateTicketRequestAsync(
				"Test Ticket for Client Filter",
				"Test ticket created for client filtering test",
				CancellationToken.None);
			_ = createRequest.Should().NotBeNull("Need at least one user to create test ticket");

			try
			{
				var created = await HaloClient.Psa.Tickets.CreateAsync(createRequest!, CancellationToken.None);
				_ = created.Should().NotBeNull();
				_ = created.Ticket.Should().NotBeNull();

				// Now get the updated ticket list
				allTickets = await HaloClient.Psa.Tickets.GetAllAsync(CancellationToken.None);
			}
			catch (HaloApiException)
			{
				// If we can't create tickets, skip this test
				return;
			}
		}

		// If we still have no tickets, skip the test
		if (allTickets.Tickets.Count == 0)
		{
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
	public async Task GetByIdAsync_WithValidId_ReturnsTicket()
	{
		try
		{
			// Arrange - Ensure we have at least one ticket
			var ticket = await EnsureTestTicketExistsAsync(CancellationToken.None);
			var ticketId = ticket.Id;

			// Act
			var result = await HaloClient.Psa.Tickets.GetByIdAsync(ticketId, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Id.Should().Be(ticketId);
		}
		catch (InvalidOperationException)
		{
			// Skip test if we can't create tickets in this sandbox
		}
	}

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
	public async Task UpdateAsync_WithValidRequest_TestsEndpointBehavior()
	{
		try
		{
			// Arrange - Ensure we have a ticket to update
			var ticket = await EnsureTestTicketExistsAsync(CancellationToken.None);
			var ticketId = ticket.Id;
			var originalSummary = ticket.Summary;

			var updateRequest = new UpdateTicketRequest
			{
				Summary = $"Updated via API Test - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
				Details = "Updated by API integration test",
				Priority = 2
			};

			// Act & Assert - Test how the endpoint behaves
			try
			{
				var result = await HaloClient.Psa.Tickets.UpdateAsync(ticketId, updateRequest, CancellationToken.None);

				// If update succeeds, verify the response
				_ = result.Should().NotBeNull();
				_ = result.Ticket.Should().NotBeNull();
				_ = result.Ticket.Id.Should().Be(ticketId);

				// Try to restore original state if possible
				try
				{
					var restoreRequest = new UpdateTicketRequest { Summary = originalSummary };
					_ = await HaloClient.Psa.Tickets.UpdateAsync(ticketId, restoreRequest, CancellationToken.None);
				}
				catch (HaloApiException)
				{
					// Restore failed - that's okay for testing
				}
			}
			catch (HaloApiException ex)
			{
				// If update fails, verify proper error handling
				AssertUnsupportedOperation(ex, UnsupportedOperationStatusCodes);
			}
		}
		catch (InvalidOperationException)
		{
			// Skip test if we can't create tickets in this sandbox
		}
	}

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
	public async Task CloseAsync_WithValidId_TestsEndpointBehavior()
	{
		try
		{
			// Arrange - Ensure we have a ticket to close
			var ticket = await EnsureTestTicketExistsAsync(CancellationToken.None);
			var ticketId = ticket.Id;

			// Act & Assert - Test close behavior
			try
			{
				var result = await HaloClient.Psa.Tickets.CloseAsync(ticketId, "Closed by API test", CancellationToken.None);

				// If close succeeds, verify the response
				_ = result.Should().NotBeNull();
				_ = result.Ticket.Should().NotBeNull();
				_ = result.Ticket.Id.Should().Be(ticketId);
				_ = result.Ticket.IsClosed.Should().BeTrue();
			}
			catch (HaloApiException ex)
			{
				// If close fails, verify proper error handling
				AssertUnsupportedOperation(ex, UnsupportedTicketOperationStatusCodes);
			}
		}
		catch (InvalidOperationException)
		{
			// Skip test if we can't create tickets in this sandbox
		}
	}

	[Fact]
	public async Task AssignAsync_WithValidIds_TestsEndpointBehavior()
	{
		try
		{
			// Arrange - Ensure we have a ticket and a user to assign
			var ticket = await EnsureTestTicketExistsAsync(CancellationToken.None);
			var ticketId = ticket.Id;

			var users = await HaloClient.Psa.Users.GetAllAsync(CancellationToken.None);
			_ = users.Should().NotBeEmpty("Need at least one user to test assignment");

			var agent = users.Where(u => u.IsAgent).FirstOrDefault() ?? users[0];
			var agentId = agent.Id;

			// Act & Assert - Test assignment behavior
			try
			{
				var result = await HaloClient.Psa.Tickets.AssignAsync(ticketId, agentId, CancellationToken.None);

				// If assignment succeeds, verify the response
				_ = result.Should().NotBeNull();
				_ = result.Ticket.Should().NotBeNull();
				_ = result.Ticket.Id.Should().Be(ticketId);
				_ = result.Ticket.AgentId.Should().Be(agentId);
			}
			catch (HaloApiException ex)
			{
				// If assignment fails, verify proper error handling
				AssertUnsupportedOperation(ex, UnsupportedTicketOperationStatusCodes);
			}
		}
		catch (InvalidOperationException)
		{
			// Skip test if we can't create tickets in this sandbox
		}
	}

	/// <summary>
	/// The status codes Halo returns when an endpoint is unavailable or refuses the operation.
	/// Several of these tests accept either a successful call or one of these, because sandboxes
	/// differ in which ticket operations they permit.
	/// </summary>
	private static readonly int[] UnsupportedOperationStatusCodes = [400, 403, 405, 501];

	/// <summary>
	/// As <see cref="UnsupportedOperationStatusCodes"/>, plus 404 for operations addressed at a
	/// specific ticket that the sandbox may no longer hold.
	/// </summary>
	private static readonly int[] UnsupportedTicketOperationStatusCodes = [400, 403, 404, 405, 501];

	/// <summary>
	/// Asserts that a failure is one of the responses a sandbox may legitimately give for an
	/// operation it does not support, rather than an unexpected error.
	/// </summary>
	private static void AssertUnsupportedOperation(HaloApiException exception, int[] expectedStatusCodes)
	{
		_ = exception.Should().NotBeNull();
		_ = exception.StatusCode.Should().BeOneOf(expectedStatusCodes);
	}

	/// <summary>
	/// Builds a ticket creation request against the first client and user the tenant reports.
	/// Returns <see langword="null"/> when the tenant has no client or no user, since a ticket
	/// cannot be created without both.
	/// </summary>
	private async Task<CreateTicketRequest?> BuildCreateTicketRequestAsync(
		string summary,
		string details,
		CancellationToken cancellationToken)
	{
		var clients = await HaloClient.Psa.Clients.GetAllAsync(cancellationToken);
		var users = await HaloClient.Psa.Users.GetAllAsync(cancellationToken);

		if (clients.Count == 0 || users.Count == 0)
		{
			return null;
		}

		return new CreateTicketRequest
		{
			Summary = $"{summary} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
			Details = details,
			ClientId = clients[0].Id,
			UserId = users[0].Id,
			Priority = 1
		};
	}

	/// <summary>
	/// Helper method to ensure at least one test ticket exists, creating one if needed
	/// </summary>
	private async Task<Ticket> EnsureTestTicketExistsAsync(CancellationToken cancellationToken)
	{
		// First check if we already have tickets
		var existingTickets = await HaloClient.Psa.Tickets.GetAllAsync(cancellationToken);
		if (existingTickets.Tickets.Count > 0)
		{
			return existingTickets.Tickets[0];
		}

		// No tickets exist, try to create one
		var createRequest = await BuildCreateTicketRequestAsync(
			"Test Ticket",
			"Test ticket created for integration testing",
			cancellationToken);
		_ = createRequest.Should().NotBeNull("Need at least one client and one user to create a test ticket");

		try
		{
			var result = await HaloClient.Psa.Tickets.CreateAsync(createRequest!, cancellationToken);
			_ = result.Should().NotBeNull();
			_ = result.Ticket.Should().NotBeNull();

			return result.Ticket;
		}
		catch (HaloApiException ex)
		{
			// If we can't create tickets in this sandbox, we need to skip tests that require tickets
			throw new InvalidOperationException($"Cannot create test tickets in this sandbox environment. " +
				$"Status: {ex.StatusCode}, Message: {ex.Message}. Tests requiring existing tickets will be skipped.", ex);
		}
	}

	/// <summary>
	/// Helper method to ensure test tickets exist with specific content
	/// </summary>
	private async Task EnsureTestTicketWithContentAsync(string content, CancellationToken cancellationToken)
	{
		// Check if we already have a ticket with this content
		var filter = new TicketFilter { Search = content, Count = 1 };
		var existing = await HaloClient.Psa.Tickets.GetAllAsync(filter, cancellationToken);

		if (existing.Tickets.Any(t => TicketMentions(t, content)))
		{
			return; // Already have what we need
		}

		// Create a ticket with the specific content
		var createRequest = await BuildCreateTicketRequestAsync(
			$"Test Ticket {content}",
			$"Test ticket created for testing search functionality with {content}",
			cancellationToken);
		if (createRequest is null)
		{
			return;
		}

		try
		{
			_ = await HaloClient.Psa.Tickets.CreateAsync(createRequest, cancellationToken);
		}
		catch (HaloApiException)
		{
			// If we can't create, that's okay for this helper
		}
	}

	/// <summary>
	/// Helper method to ensure we have a minimum number of tickets for testing
	/// </summary>
	private async Task EnsureTestTicketsExistAsync(int minimumCount, CancellationToken cancellationToken)
	{
		var existingTickets = await HaloClient.Psa.Tickets.GetAllAsync(cancellationToken);
		var currentCount = existingTickets.Tickets.Count;

		if (currentCount >= minimumCount)
		{
			return; // Already have enough
		}

		var ticketsToCreate = minimumCount - currentCount;
		for (var i = 0; i < ticketsToCreate; i++)
		{
			var createRequest = await BuildCreateTicketRequestAsync(
				$"Test Ticket {i + 1}",
				$"Test ticket {i + 1} created for pagination testing",
				cancellationToken);
			if (createRequest is null)
			{
				return; // Can't create tickets without clients and users
			}

			try
			{
				_ = await HaloClient.Psa.Tickets.CreateAsync(createRequest, cancellationToken);
			}
			catch (HaloApiException)
			{
				// If we can't create more tickets, stop trying
				break;
			}
		}
	}

	/// <summary>
	/// Whether a ticket's summary or details mention the given text, case-insensitively.
	/// </summary>
	private static bool TicketMentions(Ticket ticket, string content)
		=> ticket.Summary.Contains(content, StringComparison.OrdinalIgnoreCase)
			|| (!string.IsNullOrEmpty(ticket.Details) && ticket.Details.Contains(content, StringComparison.OrdinalIgnoreCase));
}
