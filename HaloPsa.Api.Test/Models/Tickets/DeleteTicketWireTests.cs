using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Test.Infrastructure;
using Refit;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Pins what deleting a ticket sends: <c>DELETE /api/Tickets/{id}</c> with Halo's optional <c>reason</c> query
/// parameter, and no request at all for an id Halo would reject.
/// </summary>
public class DeleteTicketWireTests
{
	private static (TicketsApiWrapper Wrapper, CapturingHandler Handler) Create()
	{
		var handler = new CapturingHandler("");
		return (new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient())), handler);
	}

	private static void AssertDeleteSent(CapturingHandler handler, string query)
	{
		_ = handler.Methods.Should().Equal(HttpMethod.Delete);
		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets/901");
		_ = query.Length == 0
			? handler.LastRequest.Query.Should().BeEmpty()
			: handler.LastRequest.Query.Should().Be(query);
		_ = handler.RequestBodies.Should().Equal([null]);
	}

	private static async Task AssertRefusedReasonAsync<TException>(Func<TicketsApiWrapper, Task> act)
		where TException : ArgumentException
	{
		var (wrapper, handler) = Create();

		var thrown = await FluentActions.Awaiting(() => act(wrapper)).Should().ThrowAsync<TException>();

		_ = thrown.Which.ParamName.Should().Be("reason");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteAsync_SendsADeleteForTheTicket()
	{
		var (wrapper, handler) = Create();

		await wrapper.DeleteAsync(901, CancellationToken.None);

		AssertDeleteSent(handler, "");
	}

	[Fact]
	public async Task DeleteAsync_WithAReason_SendsHalosReasonParameter()
	{
		var (wrapper, handler) = Create();

		await wrapper.DeleteAsync(901, "duplicate", CancellationToken.None);

		AssertDeleteSent(handler, "?reason=duplicate");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task DeleteAsync_WhenIdIsNotPositive_RefusesBeforeSending(int id)
	{
		var (wrapper, handler) = Create();

		var withoutReason = async () => await wrapper.DeleteAsync(id, CancellationToken.None);
		var withReason = async () => await wrapper.DeleteAsync(id, "duplicate", CancellationToken.None);

		_ = (await withoutReason.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("id");
		_ = (await withReason.Should().ThrowAsync<ArgumentOutOfRangeException>()).Which.ParamName.Should().Be("id");
		_ = handler.Requests.Should().BeEmpty();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public Task DeleteAsync_WhenTheReasonIsBlank_RefusesBeforeSending(string reason)
		=> AssertRefusedReasonAsync<ArgumentException>(wrapper => wrapper.DeleteAsync(901, reason, CancellationToken.None));

	[Fact]
	public Task DeleteAsync_WhenTheReasonIsNull_RefusesBeforeSending()
		=> AssertRefusedReasonAsync<ArgumentException>(wrapper => wrapper.DeleteAsync(901, null!, CancellationToken.None));
}
