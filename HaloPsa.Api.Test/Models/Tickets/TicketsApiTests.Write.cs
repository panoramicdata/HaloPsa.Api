using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

public partial class TicketsApiTests
{
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
}
