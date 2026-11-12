using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Test.Infrastructure;
using Refit;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Shared arrangement and assertions for the ticket wire tests
/// </summary>
internal static class TicketWireTestSupport
{
	internal static (TicketsApiWrapper Wrapper, CapturingHandler Handler) Create(string responseJson)
	{
		var handler = new CapturingHandler(responseJson);
		return (new TicketsApiWrapper(RestService.For<ITicketsApi>(handler.CreateClient())), handler);
	}

	internal static JsonElement SentBody(CapturingHandler handler) => JsonDocument.Parse(handler.RequestBodies[^1]!).RootElement;

	internal static void AssertPostedAsOneElementArray(CapturingHandler handler)
	{
		_ = handler.Methods.Should().Equal(HttpMethod.Post);
		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets");
		_ = SentBody(handler).ValueKind.Should().Be(JsonValueKind.Array);
		_ = SentBody(handler).GetArrayLength().Should().Be(1);
	}

	internal static void AssertInts(JsonElement ticket, params (string Name, int Value)[] expected)
	{
		foreach (var (name, value) in expected)
		{
			_ = ticket.GetProperty(name).GetInt32().Should().Be(value);
		}
	}

	internal static void AssertNames(JsonElement ticket, params string[] expected)
		=> _ = ticket.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(expected);

	internal static async Task AssertRefusedBeforeSendingAsync<TException>(CapturingHandler handler, Func<Task> act, string messagePattern)
		where TException : Exception
	{
		_ = (await FluentActions.Awaiting(act).Should().ThrowAsync<TException>()).WithMessage(messagePattern);
		_ = handler.Requests.Should().BeEmpty();
	}
}
