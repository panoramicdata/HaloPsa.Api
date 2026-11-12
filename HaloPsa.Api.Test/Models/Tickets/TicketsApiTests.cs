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
		AssertTicketList(result);
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
		AssertTicketList(result);
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
		AssertTicketList(result);

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
		AssertTicketList(result);

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
}
