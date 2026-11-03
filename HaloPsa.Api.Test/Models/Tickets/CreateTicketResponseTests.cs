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
	public void Read_ATicketWrapperWithSuccessFalseAndMessages_ReturnsThemAndStillReturnsTheTicket()
	{
		var response = Read($$"""{"ticket":{{T901}},"success":false,"messages":["Category is mandatory","Check the client"]}""");

		_ = response.TicketId.Should().Be(901);
		_ = response.Success.Should().BeFalse();
		_ = response.Messages.Should().Equal("Category is mandatory", "Check the client");
	}

	[Fact]
	public void Read_ATicketWrapperWithASingleStringMessage_ReturnsOneMessage()
	{
		var response = Read($$"""{"ticket":{{T901}},"success":true,"messages":"Saved with a warning"}""");

		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().Equal("Saved with a warning");
	}

	[Fact]
	public void Read_ATicketWrapper_ReadsSuccessAndMessagesWhateverTheCaseOfTheirNames()
	{
		var response = Read($$"""{"Ticket":{{T901}},"Success":false,"Messages":["m"]}""");

		_ = response.Success.Should().BeFalse();
		_ = response.Messages.Should().Equal("m");
	}

	[Fact]
	public void Read_ATicketWrapper_KeepsOnlyTheNonBlankStringMessages()
	{
		var response = Read($$"""{"ticket":{{T901}},"messages":["a",null,7,{"x":1},"  ","b"]}""");

		_ = response.TicketId.Should().Be(901);
		_ = response.Messages.Should().Equal("a", "b");
	}

	[Theory]
	[InlineData("[]")]
	[InlineData("[null,3]")]
	[InlineData("null")]
	[InlineData("\"\"")]
	[InlineData("\"  \"")]
	[InlineData("""{"x":"y"}""")]
	public void Read_ATicketWrapperWithNoReadableMessage_LeavesMessagesNull(string messages)
	{
		var response = Read($$"""{"ticket":{{T901}},"messages":{{messages}}}""");

		_ = response.TicketId.Should().Be(901);
		_ = response.Messages.Should().BeNull();
	}

	[Theory]
	[InlineData("\"false\"", false)]
	[InlineData("\"TRUE\"", true)]
	[InlineData("0", false)]
	[InlineData("1", true)]
	public void Read_ATicketWrapperWhoseSuccessIsABooleanStringOrNumber_ReadsIt(string success, bool expected)
	{
		var response = Read($$"""{"ticket":{{T901}},"success":{{success}}}""");

		_ = response.Success.Should().Be(expected);
	}

	[Theory]
	[InlineData("null")]
	[InlineData("\"maybe\"")]
	[InlineData("2")]
	public void Read_ATicketWrapperWhoseSuccessCannotBeRead_KeepsTheDefault(string success)
	{
		var response = Read($$"""{"ticket":{{T901}},"success":{{success}}}""");

		_ = response.Success.Should().BeTrue();
	}

	[Fact]
	public void Read_ATicketWrapperWithNeitherSuccessNorMessages_KeepsTheDefaults()
	{
		var response = Read($$"""{"ticket":{{T901}}}""");

		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().BeNull();
	}

	[Fact]
	public void Read_ABareTicket_KeepsTheDefaults()
	{
		var response = Read(T901);

		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().BeNull();
	}

	[Fact]
	public void Read_AnArrayOfTickets_KeepsTheDefaults()
	{
		var response = Read($"[{T901},{T902}]");

		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().BeNull();
	}

	[Fact]
	public void Read_ABareTicketCarryingSuccessAndMessagesProperties_DoesNotTreatThemAsTheWrappers()
	{
		var response = Read("""{"id":901,"summary":"S","status_id":1,"client_id":21,"user_id":9,"success":false,"messages":["m"]}""");

		_ = response.Success.Should().BeTrue();
		_ = response.Messages.Should().BeNull();
	}

	[Fact]
	public void Write_ThenRead_PreservesTheTicketSuccessAndMessages()
	{
		var original = Read($$"""{"ticket":{{T901}},"success":false,"messages":["Category is mandatory","Check the client"]}""");

		var roundTripped = Read(JsonSerializer.Serialize(original));

		_ = roundTripped.TicketId.Should().Be(901);
		_ = roundTripped.Success.Should().BeFalse();
		_ = roundTripped.Messages.Should().Equal("Category is mandatory", "Check the client");
	}

	[Fact]
	public void Write_ThenRead_PreservesTheDefaultSuccessAndNullMessages()
	{
		var json = JsonSerializer.Serialize(Read(T901));

		var roundTripped = Read(json);

		_ = json.Should().NotContain("messages");
		_ = roundTripped.TicketId.Should().Be(901);
		_ = roundTripped.Success.Should().BeTrue();
		_ = roundTripped.Messages.Should().BeNull();
	}

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

	[Theory]
	[InlineData("""{"id":903}""")]
	[InlineData("""{"success":false,"messages":["m"],"ticket":{"id":903}}""")]
	public async Task CreateAsync_WhenHaloAnswersWithAnUnreadableTicket_ThrowsAnErrorNamingTheId(string body)
	{
		var handler = new CapturingHandler(body);
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
