using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;

namespace HaloPsa.Api.Test;

public class ReadOnlyOptionTests
{
	private sealed class StubHandler : HttpMessageHandler
	{
		public int SentCount { get; private set; }

		public List<HttpMethod> Methods { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			SentCount++;
			Methods.Add(request.Method);
			return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
		}
	}

	private static HaloClientOptions CreateOptions(bool isReadOnly) => new()
	{
		Account = "contoso",
		ClientId = "11111111-2222-3333-4444-555555555555",
		ClientSecret = "secret",
		IsReadOnly = isReadOnly
	};

	private static (HttpClient Client, StubHandler Stub) CreateClient(HaloClientOptions options)
	{
		var stub = new StubHandler();
		var client = new HttpClient(new ReadOnlyHandler(options) { InnerHandler = stub })
		{
			BaseAddress = new Uri("https://contoso.halopsa.com")
		};

		return (client, stub);
	}

	[Fact]
	public void IsReadOnly_DefaultsToFalse()
		=> CreateOptions(isReadOnly: false).IsReadOnly.Should().BeFalse();

	[Fact]
	public void LegacyReadOnly_DefaultsToFalseAndMapsToIsReadOnly()
	{
#pragma warning disable CS0618 // The alias is what is under test
		new HaloClientOptions { Account = "a", ClientId = "b", ClientSecret = "c" }.ReadOnly.Should().BeFalse();

		var options = new HaloClientOptions { Account = "a", ClientId = "b", ClientSecret = "c", ReadOnly = true };
		options.IsReadOnly.Should().BeTrue();
		options.ReadOnly.Should().BeTrue();
#pragma warning restore CS0618
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("DELETE")]
	[InlineData("PATCH")]
	public async Task WriteRequest_WhenReadOnly_ThrowsAndSendsNothing(string method)
	{
		var (client, stub) = CreateClient(CreateOptions(isReadOnly: true));

		var act = () => client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/api/Tickets"), TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
		stub.SentCount.Should().Be(0);
	}

	[Fact]
	public async Task GetRequest_WhenReadOnly_IsStillSent()
	{
		var (client, stub) = CreateClient(CreateOptions(isReadOnly: true));

		using var response = await client.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		response.IsSuccessStatusCode.Should().BeTrue();
		stub.SentCount.Should().Be(1);
		stub.Methods.Should().ContainSingle().Which.Should().Be(HttpMethod.Get);
	}

	[Fact]
	public async Task WriteRequest_WhenNotReadOnly_IsSent()
	{
		var (client, stub) = CreateClient(CreateOptions(isReadOnly: false));

		using var response = await client.PostAsync("/api/Tickets", new StringContent("{}"), TestContext.Current.CancellationToken);

		response.IsSuccessStatusCode.Should().BeTrue();
		stub.SentCount.Should().Be(1);
	}

	[Fact]
	public async Task ChangingIsReadOnlyAfterConstruction_TakesEffectOnTheNextRequest()
	{
		var options = CreateOptions(isReadOnly: false);
		var (client, stub) = CreateClient(options);

		using var first = await client.PostAsync("/api/Tickets", new StringContent("{}"), TestContext.Current.CancellationToken);
		options.IsReadOnly = true;
		var act = () => client.PostAsync("/api/Tickets", new StringContent("{}"), TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
		stub.SentCount.Should().Be(1);
	}

	[Fact]
	public async Task ClientTicketWrite_WhenReadOnly_ThrowsBeforeAnyRequest()
	{
		using var client = new HaloClient(CreateOptions(isReadOnly: true));

		var act = () => client.Psa.Tickets.CloseAsync(1, 2, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
