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
/// Reads a ticket update response in each shape Halo can answer with: a bare ticket, an array of tickets
/// or a <c>ticket</c> wrapper. The update has been applied by the time the response arrives, so a response
/// that cannot be read must say so rather than invite a retry.
/// </summary>
public class UpdateTicketResponseTests
{
	private const string T901 = """{"id":901,"summary":"S","status_id":2,"client_id":21,"user_id":9}""";
	private const string T902 = """{"id":902,"summary":"S","status_id":2,"client_id":21,"user_id":9}""";

	private static UpdateTicketResponse Read(string json) => JsonSerializer.Deserialize<UpdateTicketResponse>(json)!;

	[Fact]
	public void Read_ABareTicketObject()
	{
		var response = Read(T901);

		_ = response.Ticket.Id.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901);
	}

	[Fact]
	public void Read_AnArrayOfTickets()
	{
		var response = Read($"[{T901},{T902}]");

		_ = response.Ticket.Id.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901, 902);
	}

	[Fact]
	public void Read_ATicketWrapper()
	{
		var response = Read($$"""{"ticket":{{T901}},"success":true}""");

		_ = response.Ticket.Id.Should().Be(901);
		_ = response.Tickets.Select(t => t.Id).Should().Equal(901);
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

		_ = act.Should().Throw<JsonException>().WithMessage("*the update may have been applied*903*");
	}

	[Fact]
	public async Task UpdateAsync_WhenHaloAnswersWithABareTicket_ReturnsItsId()
	{
		var handler = new CapturingHandler(T901);
		var wrapper = new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient()));

		var response = await wrapper.UpdateAsync(901, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		_ = response.TicketId.Should().Be(901);
	}

	[Theory]
	[InlineData("", "901")]
	[InlineData("null", "901")]
	[InlineData("<html><body>Gateway page</body></html>", "901")]
	[InlineData("42", "901")]
	[InlineData("[]", "901")]
	[InlineData("""{"id":903}""", "903")]
	public async Task UpdateAsync_WhenHaloAnswersOkWithABodyThatCannotBeRead_ThrowsAnErrorSayingTheUpdateMayHaveBeenApplied(string body, string expectedId)
	{
		var handler = new CapturingHandler(body);
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.UpdateAsync(901, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		_ = (await act.Should().ThrowAsync<JsonException>()).WithMessage($"*the update may have been applied (id {expectedId})*");
	}

	[Fact]
	public async Task UpdateAsync_WhenHaloAnswersWithAnHttpError_ThrowsTheHaloApiException()
	{
		var handler = new CapturingHandler("""{"message":"Something went wrong"}""") { StatusCode = HttpStatusCode.InternalServerError };
		var psa = new PsaApi(handler.CreateClient(), readOnly: false);

		var act = async () => await psa.Tickets.UpdateAsync(901, new UpdateTicketRequest { Status = 2 }, CancellationToken.None);

		_ = await act.Should().ThrowExactlyAsync<HaloServerException>();
	}
}
