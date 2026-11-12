using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

public partial class TicketsApiTests
{
	/// <summary>
	/// Runs <paramref name="operation"/> against a ticket the sandbox is asked to supply.
	///
	/// Sandboxes decline in two distinct ways, and the tests using this helper tolerate both: the
	/// sandbox may not permit creating a ticket at all (nothing to test against, so the test simply
	/// returns), and it may refuse the operation itself with one of
	/// <paramref name="toleratedFailureStatusCodes"/>. Passing no status codes means any API failure
	/// is a real failure.
	/// </summary>
	private async Task RunAgainstTestTicketAsync(
		Func<Ticket, Task> operation,
		int[]? toleratedFailureStatusCodes = null)
	{
		Ticket ticket;
		try
		{
			ticket = await EnsureTestTicketExistsAsync(CancellationToken.None);
		}
		catch (InvalidOperationException)
		{
			// Skip test if we can't create tickets in this sandbox
			return;
		}

		try
		{
			await operation(ticket);
		}
		catch (HaloApiException ex) when (toleratedFailureStatusCodes is not null)
		{
			AssertUnsupportedOperation(ex, toleratedFailureStatusCodes);
		}
	}

	/// <summary>
	/// Ensures tickets exist for the client filter test, creating a test ticket when none do, and
	/// returns the ticket list the test should continue from (or <see langword="null"/> when there
	/// are still no tickets and the test should skip).
	/// </summary>
	private async Task<TicketsResponse?> EnsureTestTicketsExistForClientFilterAsync(
		TicketsResponse allTickets)
	{
		if (allTickets.Tickets.Count > 0)
		{
			return allTickets;
		}

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
			return null;
		}

		// If we still have no tickets, skip the test
		return allTickets.Tickets.Count == 0 ? null : allTickets;
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
	/// Asserts that an operation came back with the ticket it was addressed to. Update, close and
	/// assign all answer with an <see cref="UpdateTicketResponse"/>, and all three need this much
	/// checked before they look at the one field they are actually about.
	/// </summary>
	private static void AssertRespondsWithTicket(UpdateTicketResponse response, int expectedTicketId)
	{
		_ = response.Should().NotBeNull();
		_ = response.Ticket.Should().NotBeNull();
		_ = response.Ticket.Id.Should().Be(expectedTicketId);
	}

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
		return await CreateTestTicketAsync(
			"Test Ticket",
			"Test ticket created for integration testing",
			cancellationToken);
	}

	/// <summary>
	/// Creates a test ticket with the given summary and details, surfacing a skip signal when the
	/// sandbox will not let us create tickets at all.
	/// </summary>
	private async Task<Ticket> CreateTestTicketAsync(
		string summary,
		string details,
		CancellationToken cancellationToken)
	{
		var createRequest = await BuildCreateTicketRequestAsync(
			summary,
			details,
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
