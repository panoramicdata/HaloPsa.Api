using AwesomeAssertions;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test.Models.Tickets;

public class TicketModelTests
{
	[Fact]
	public void Ticket_WithRequiredProperties_CanBeCreated()
	{
		// Arrange & Act
		var ticket = new Ticket
		{
			Id = 123,
			Summary = "Test ticket",
			Status = 1,
			ClientId = 1,
			UserId = 1
		};

		// Assert
		_ = ticket.Id.Should().Be(123);
		_ = ticket.Summary.Should().Be("Test ticket");
		_ = ticket.Status.Should().Be(1);
		_ = ticket.ClientId.Should().Be(1);
		_ = ticket.UserId.Should().Be(1);
		_ = ticket.Priority.Should().Be(1); // Default value
		_ = ticket.DateOccurred.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		_ = ticket.IsClosed.Should().BeFalse(); // No DateClosed set, so should be false
		_ = ticket.IsOnHold.Should().BeFalse(); // Default value
	}

	[Fact]
	public void Ticket_WithAllProperties_CanBeCreated()
	{
		// Arrange & Act
		var ticket = CreateTicketWithAllProperties();

		// Assert - Core properties
		_ = ticket.Id.Should().Be(456);
		_ = ticket.Summary.Should().Be("Comprehensive test ticket");
		_ = ticket.Details.Should().Be("Detailed description");
		_ = ticket.Status.Should().Be(2);
		_ = ticket.StatusName.Should().Be("In Progress");
		_ = ticket.Priority.Should().Be(3);
		_ = ticket.PriorityName.Should().Be("High");
	}

	[Fact]
	public void Ticket_WithAllProperties_HasCorrectRelationships()
	{
		// Arrange & Act
		var ticket = CreateTicketWithAllProperties();

		// Assert - Relationship properties
		_ = ticket.ClientId.Should().Be(5);
		_ = ticket.ClientName.Should().Be("Test Client");
		_ = ticket.SiteId.Should().Be(10);
		_ = ticket.SiteName.Should().Be("Main Office");
		_ = ticket.UserId.Should().Be(15);
		_ = ticket.UserName.Should().Be("John Doe");
		_ = ticket.UserEmail.Should().Be("john.doe@test.com");
		_ = ticket.AgentId.Should().Be(20);
		_ = ticket.AgentName.Should().Be("Jane Smith");
		_ = ticket.TeamId.Should().Be(25);
		_ = ticket.TeamName.Should().Be("IT Support");
		_ = ticket.CategoryId.Should().Be(30);
		_ = ticket.CategoryName.Should().Be("Hardware");
		_ = ticket.TicketTypeId.Should().Be(35);
		_ = ticket.TicketTypeName.Should().Be("Incident");
	}

	[Fact]
	public void Ticket_WithAllProperties_HasCorrectDatesAndCollections()
	{
		// Arrange & Act
		var ticket = CreateTicketWithAllProperties();

		// Assert - Date, state, and collection properties
		_ = ticket.DateOccurred.Should().NotBe(default);
		_ = ticket.LastUpdate.Should().NotBeNull();
		_ = ticket.DateClosed.Should().NotBeNull();
		_ = ticket.Source.Should().Be(1);
		_ = ticket.IsClosed.Should().BeTrue();
		_ = ticket.IsOnHold.Should().BeFalse();
		_ = ticket.CustomFields.Should().NotBeNull();
		_ = ticket.AssetIds.Should().HaveCount(3);
		_ = ticket.Tags.Should().HaveCount(2);
	}

	private static Ticket CreateTicketWithAllProperties()
	{
		var now = DateTime.UtcNow;
		return new Ticket
		{
			Id = 456,
			Summary = "Comprehensive test ticket",
			Details = "Detailed description",
			Status = 2,
			StatusName = "In Progress",
			Priority = 3,
			PriorityName = "High",
			ClientId = 5,
			ClientName = "Test Client",
			SiteId = 10,
			SiteName = "Main Office",
			UserId = 15,
			UserName = "John Doe",
			UserEmail = "john.doe@test.com",
			AgentId = 20,
			AgentName = "Jane Smith",
			TeamId = 25,
			TeamName = "IT Support",
			CategoryId = 30,
			CategoryName = "Hardware",
			TicketTypeId = 35,
			TicketTypeName = "Incident",
			DateOccurred = now,
			LastUpdate = now.AddHours(1),
			DateClosed = now.AddHours(2),
			Source = 1,
			IsOnHold = false,
			CustomFields = new Dictionary<string, object?> { ["CustomField1"] = "Value1" },
			AssetIds = new List<int> { 1, 2, 3 }.AsReadOnly(),
			Tags = new List<string> { "urgent", "hardware" }.AsReadOnly()
		};
	}

	[Fact]
	public void Ticket_IsClosed_ComputedCorrectly()
	{
		// Test when DateClosed is null
		var openTicket = new Ticket
		{
			Id = 1,
			Summary = "Open ticket",
			Status = 1,
			ClientId = 1,
			UserId = 1,
			DateClosed = null
		};

		_ = openTicket.IsClosed.Should().BeFalse();

		// Test when DateClosed has a value
		var closedTicket = new Ticket
		{
			Id = 2,
			Summary = "Closed ticket",
			Status = 2,
			ClientId = 1,
			UserId = 1,
			DateClosed = DateTime.UtcNow
		};

		_ = closedTicket.IsClosed.Should().BeTrue();
	}

	[Fact]
	public void CreateTicketRequest_WithRequiredProperties_CanBeCreated()
	{
		// Arrange & Act
		var request = new CreateTicketRequest
		{
			Summary = "New ticket",
			ClientId = 1,
			UserId = 1
		};

		// Assert
		_ = request.Summary.Should().Be("New ticket");
		_ = request.ClientId.Should().Be(1);
		_ = request.UserId.Should().Be(1);
		_ = request.Details.Should().BeNull();
		_ = request.Status.Should().BeNull();
		_ = request.Priority.Should().BeNull();
	}

	[Fact]
	public void CreateTicketRequest_WithAllProperties_CanBeCreated()
	{
		// Arrange
		var now = DateTime.UtcNow;
		var customFields = new Dictionary<string, object?> { ["Field1"] = "Value1" };
		var assetIds = new List<int> { 1, 2 }.AsReadOnly();
		var tags = new List<string> { "tag1", "tag2" }.AsReadOnly();

		// Act
		var request = new CreateTicketRequest
		{
			Summary = "Detailed new ticket",
			Details = "Full description",
			Status = 1,
			Priority = 2,
			ClientId = 3,
			SiteId = 4,
			UserId = 5,
			AgentId = 6,
			TeamId = 7,
			CategoryId = 8,
			TicketTypeId = 9,
			DateOccurred = now,
			Source = 1,
			CustomFields = customFields,
			AssetIds = assetIds,
			Tags = tags,
			SuppressEmails = true,
			Notes = "Initial notes"
		};

		// Assert
		_ = request.Summary.Should().Be("Detailed new ticket");
		_ = request.Details.Should().Be("Full description");
		_ = request.Status.Should().Be(1);
		_ = request.Priority.Should().Be(2);
		_ = request.ClientId.Should().Be(3);
		_ = request.SiteId.Should().Be(4);
		_ = request.UserId.Should().Be(5);
		_ = request.AgentId.Should().Be(6);
		_ = request.TeamId.Should().Be(7);
		_ = request.CategoryId.Should().Be(8);
		_ = request.TicketTypeId.Should().Be(9);
		_ = request.DateOccurred.Should().Be(now);
		_ = request.Source.Should().Be(1);
		_ = request.CustomFields.Should().BeEquivalentTo(customFields);
		_ = request.AssetIds.Should().BeEquivalentTo(assetIds);
		_ = request.Tags.Should().BeEquivalentTo(tags);
		_ = request.SuppressEmails.Should().BeTrue();
		_ = request.Notes.Should().Be("Initial notes");
	}

	[Fact]
	public void UpdateTicketRequest_WithPartialUpdate_CanBeCreated()
	{
		// Arrange & Act
		var request = new UpdateTicketRequest
		{
			Summary = "Updated summary",
			Priority = 3,
			AgentId = 10
		};

		// Assert
		_ = request.Summary.Should().Be("Updated summary");
		_ = request.Priority.Should().Be(3);
		_ = request.AgentId.Should().Be(10);
		_ = request.Details.Should().BeNull();
		_ = request.Status.Should().BeNull();
		_ = request.ClientId.Should().BeNull();
	}

	[Fact]
	public void UpdateTicketRequest_WithCloseTicket_CanBeCreated()
	{
		// Arrange & Act
		var request = new UpdateTicketRequest
		{
			CloseTicket = true,
			Resolution = "Issue resolved successfully",
			Notes = "Closing notes"
		};

		// Assert
		_ = request.CloseTicket.Should().BeTrue();
		_ = request.Resolution.Should().Be("Issue resolved successfully");
		_ = request.Notes.Should().Be("Closing notes");
	}

	[Fact]
	public void TicketFilter_WithDefaultValues_HasExpectedDefaults()
	{
		// Arrange & Act
		var filter = new TicketFilter();

		// Assert
		_ = filter.Count.Should().BeNull();
		_ = filter.PageNo.Should().BeNull();
		_ = filter.PageSize.Should().BeNull();
		_ = filter.Paginate.Should().BeNull();
		_ = filter.Status.Should().BeNull();
		_ = filter.Search.Should().BeNull();
		_ = filter.OpenOnly.Should().BeNull();
		_ = filter.ClosedOnly.Should().BeNull();
	}

	[Fact]
	public void TicketFilter_WithAllProperties_CanBeCreated()
	{
		// Arrange & Act
		var filter = CreateFilterWithAllProperties();

		// Assert - Pagination properties
		_ = filter.Count.Should().Be(50);
		_ = filter.PageNo.Should().Be(1);
		_ = filter.PageSize.Should().Be(25);
		_ = filter.Paginate.Should().BeTrue();
	}

	[Fact]
	public void TicketFilter_WithAllProperties_HasCorrectFilterValues()
	{
		// Arrange & Act
		var filter = CreateFilterWithAllProperties();

		// Assert - Filter properties
		_ = filter.Status.Should().Be("1,2,3");
		_ = filter.Priority.Should().Be("2,3");
		_ = filter.ClientId.Should().Be(5);
		_ = filter.SiteId.Should().Be(10);
		_ = filter.UserId.Should().Be(15);
		_ = filter.AgentId.Should().Be(20);
		_ = filter.TeamId.Should().Be(25);
		_ = filter.CategoryId.Should().Be(30);
		_ = filter.TicketTypeId.Should().Be(35);
		_ = filter.Search.Should().Be("test search");
		_ = filter.StartDate.Should().NotBeNull();
		_ = filter.EndDate.Should().NotBeNull();
		_ = filter.OpenOnly.Should().BeTrue();
		_ = filter.MyTickets.Should().BeTrue();
		_ = filter.IncludeDetails.Should().BeTrue();
		_ = filter.Order.Should().Be("dateoccurred");
		_ = filter.OrderDesc.Should().BeTrue();
		_ = filter.AssetId.Should().Be(100);
		_ = filter.ServiceId.Should().Be(200);
		_ = filter.IncludeCustomFields.Should().Be("1,2,3");
	}

	private static TicketFilter CreateFilterWithAllProperties()
	{
		var startDate = DateTime.UtcNow.AddDays(-7);
		var endDate = DateTime.UtcNow;

		return new TicketFilter
		{
			Count = 50,
			PageNo = 1,
			PageSize = 25,
			Paginate = true,
			Status = "1,2,3",
			Priority = "2,3",
			ClientId = 5,
			SiteId = 10,
			UserId = 15,
			AgentId = 20,
			TeamId = 25,
			CategoryId = 30,
			TicketTypeId = 35,
			Search = "test search",
			StartDate = startDate,
			EndDate = endDate,
			OpenOnly = true,
			MyTickets = true,
			IncludeDetails = true,
			Order = "dateoccurred",
			OrderDesc = true,
			AssetId = 100,
			ServiceId = 200,
			IncludeCustomFields = "1,2,3"
		};
	}

	[Fact]
	public void TicketsResponse_WithEmptyCollection_CanBeCreated()
	{
		// Arrange & Act
		var response = new TicketsResponse
		{
			Tickets = new List<Ticket>().AsReadOnly(),
			RecordCount = 0
		};

		// Assert
		_ = response.Tickets.Should().BeEmpty();
		_ = response.RecordCount.Should().Be(0);
		_ = response.HasMore.Should().BeFalse();
		_ = response.IsPaginated.Should().BeFalse();
	}

	[Fact]
	public void TicketsResponse_WithPagination_CanBeCreated()
	{
		// Arrange
		var tickets = new List<Ticket>
		{
			new() { Id = 1, Summary = "Ticket 1", Status = 1, ClientId = 1, UserId = 1 },
			new() { Id = 2, Summary = "Ticket 2", Status = 1, ClientId = 1, UserId = 1 }
		}.AsReadOnly();

		// Act
		var response = new TicketsResponse
		{
			Tickets = tickets,
			RecordCount = 100,
			PageNo = 1,
			PageSize = 2,
			PageCount = 50,
			HasMore = true,
			IsPaginated = true
		};

		// Assert
		_ = response.Tickets.Should().HaveCount(2);
		_ = response.RecordCount.Should().Be(100);
		_ = response.PageNo.Should().Be(1);
		_ = response.PageSize.Should().Be(2);
		_ = response.PageCount.Should().Be(50);
		_ = response.HasMore.Should().BeTrue();
		_ = response.IsPaginated.Should().BeTrue();
	}

	[Fact]
	public void TicketResponse_WithSuccess_CanBeCreated()
	{
		// Arrange
		var ticket = new Ticket
		{
			Id = 1,
			Summary = "Test ticket",
			Status = 1,
			ClientId = 1,
			UserId = 1
		};
		var messages = new List<string> { "Operation completed successfully" }.AsReadOnly();

		// Act
		var response = new TicketResponse
		{
			Ticket = ticket,
			Success = true,
			Messages = messages
		};

		// Assert
		_ = response.Ticket.Should().Be(ticket);
		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().ContainSingle("Operation completed successfully");
	}

	[Fact]
	public void CreateTicketResponse_ContainsTicketId()
	{
		// Arrange
		var ticket = new Ticket
		{
			Id = 123,
			Summary = "New ticket",
			Status = 1,
			ClientId = 1,
			UserId = 1
		};

		// Act
		var response = new CreateTicketResponse
		{
			Ticket = ticket,
			Success = true
		};

		// Assert
		_ = response.TicketId.Should().Be(123);
		_ = response.Ticket.Id.Should().Be(123);
		_ = response.Success.Should().BeTrue();
	}

	[Fact]
	public void UpdateTicketResponse_ContainsTicketId()
	{
		// Arrange
		var ticket = new Ticket
		{
			Id = 456,
			Summary = "Updated ticket",
			Status = 2,
			ClientId = 1,
			UserId = 1
		};
		var messages = new List<string> { "Ticket updated successfully" }.AsReadOnly();

		// Act
		var response = new UpdateTicketResponse
		{
			Ticket = ticket,
			Success = true,
			Messages = messages
		};

		// Assert
		_ = response.TicketId.Should().Be(456);
		_ = response.Ticket.Id.Should().Be(456);
		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().ContainSingle("Ticket updated successfully");
	}
}