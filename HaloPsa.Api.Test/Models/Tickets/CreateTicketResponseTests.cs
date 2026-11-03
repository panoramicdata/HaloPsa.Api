using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Reads a ticket create response in each shape Halo can answer with: a bare ticket, an array of tickets
/// or a <c>ticket</c> wrapper. The ticket exists in Halo by the time the response arrives, so a response
/// that cannot be read must say so rather than invite a retry.
/// </summary>
public class CreateTicketResponseTests
{
	private const string T901 = """{"id":901,"summary":"S","status_id":1,"client_id":21,"user_id":9}""";
	private const string T902 = """{"id":902,"summary":"S","status_id":1,"client_id":21,"user_id":9}""";

	private static CreateTicketResponse Read(string json) => JsonSerializer.Deserialize<CreateTicketResponse>(json)!;

	[Fact]
	public void Read_ABareTicketObject()
	{
		var response = Read(T901);

		_ = response.TicketId.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901);
	}

	[Fact]
	public void Read_AnArrayOfTickets()
	{
		var response = Read($"[{T901},{T902}]");

		_ = response.TicketId.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901, 902);
	}

	[Fact]
	public void Read_ATicketWrapper()
	{
		var response = Read($$"""{"ticket":{{T901}},"success":true}""");

		_ = response.TicketId.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901);
	}

	[Fact]
	public void Read_ATicketWrapper_WhateverTheCaseOfItsName()
		=> Read($$"""{"Ticket":{{T901}}}""").TicketId.Should().Be(901);

	[Fact]
	public void Read_AnEmptyArray_Throws()
	{
		var act = () => Read("[]");

		_ = act.Should().Throw<JsonException>().WithMessage("*empty array*");
	}

	[Fact]
	public void Read_ATicketMissingRequiredFields_ThrowsNamingTheId()
	{
		var act = () => Read("""{"id":903}""");

		_ = act.Should().Throw<JsonException>().WithMessage("*may have been created*903*");
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswersWithABareTicket_ReturnsItsId()
	{
		var handler = new CapturingHandler(T901);
		var wrapper = new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient()));

		var response = await wrapper.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		_ = response.TicketId.Should().Be(901);
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswersWithAnUnreadableTicket_TheInnerErrorNamesTheId()
	{
		var handler = new CapturingHandler("""{"id":903}""");
		var wrapper = new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient()));

		var act = async () => await wrapper.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		// Refit wraps a deserialisation failure in an ApiException, so the id is on the inner exception.
		_ = (await act.Should().ThrowAsync<ApiException>()).WithInnerException<JsonException>().WithMessage("*may have been created*903*");
	}
}
