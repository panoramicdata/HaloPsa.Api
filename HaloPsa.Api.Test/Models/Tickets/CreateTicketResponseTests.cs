using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Net;
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
	public async Task CreateAsync_WhenHaloAnswersWithAnUnreadableTicket_ThrowsAnErrorNamingTheId()
	{
		var handler = new CapturingHandler("""{"id":903}""");
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		var thrown = (await act.Should().ThrowAsync<JsonException>()).WithMessage("*may have been created*903*").Which;
		_ = thrown.InnerException.Should().BeOfType<ApiException>();
	}

	[Theory]
	[InlineData("")]
	[InlineData("null")]
	[InlineData("<html><body>Gateway page</body></html>")]
	[InlineData("42")]
	[InlineData("[]")]
	public async Task CreateAsync_WhenHaloAnswersOkWithABodyThatCannotBeRead_ThrowsAnErrorSayingTheTicketMayHaveBeenCreated(string body)
	{
		var handler = new CapturingHandler(body);
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<JsonException>()).WithMessage("*may have been created*id unknown*");
	}

	[Fact]
	public async Task CreateAsync_WhenHaloAnswersWithAnHttpError_ThrowsTheHaloApiException()
	{
		var handler = new CapturingHandler("""{"message":"Something went wrong"}""") { StatusCode = HttpStatusCode.InternalServerError };
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, CancellationToken.None);

		_ = await act.Should().ThrowExactlyAsync<HaloServerException>();
	}

	[Fact]
	public async Task CreateAsync_WhenCancelled_ThrowsTheCancellation()
	{
		var handler = new CapturingHandler(T901);
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var act = async () => await psa.Tickets.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 21, UserId = 9 }, cancellation.Token);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
