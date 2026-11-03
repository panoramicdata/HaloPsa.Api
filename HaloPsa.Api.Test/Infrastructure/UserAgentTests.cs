using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using System.Net;
using System.Text;

namespace HaloPsa.Api.Test.Infrastructure;

public class UserAgentTests
{
	private const string _sampleUserAgent = "ExampleProduct/1.0 Example Company";

	[Fact]
	public void ApiRequests_CarryTheConfiguredUserAgent()
	{
		using var client = new HaloClient(CreateOptions(_sampleUserAgent));

		_ = client.HttpClient.DefaultRequestHeaders.UserAgent.ToString().Should().Be(_sampleUserAgent);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ApiRequests_CarryThePackageDefault_WhenNoneIsConfigured(string? userAgent)
	{
		using var client = new HaloClient(CreateOptions(userAgent));

		_ = client.HttpClient.DefaultRequestHeaders.UserAgent.ToString().Should().Be("Halo.Api/.NET");
	}

	[Fact]
	public async Task TokenRequest_CarriesTheConfiguredUserAgent()
	{
		var tokenStub = new TokenStub();
		using var handler = new AuthenticationHandler(CreateOptions(_sampleUserAgent), tokenStub)
		{
			InnerHandler = new CapturingHandler("{}")
		};

		await SendApiRequestAsync(handler);

		var tokenRequest = tokenStub.Requests.Should().ContainSingle().Subject;
		_ = tokenRequest.Path.Should().Be("/auth/token");
		_ = tokenRequest.UserAgent.Should().Be(_sampleUserAgent);
	}

	[Fact]
	public async Task TokenRequest_CarriesThePackageDefault_WhenNoneIsConfigured()
	{
		var tokenStub = new TokenStub();
		using var handler = new AuthenticationHandler(CreateOptions(null), tokenStub)
		{
			InnerHandler = new CapturingHandler("{}")
		};

		await SendApiRequestAsync(handler);

		var tokenRequest = tokenStub.Requests.Should().ContainSingle().Subject;
		_ = tokenRequest.Path.Should().Be("/auth/token");
		_ = tokenRequest.UserAgent.Should().Be("Halo.Api/.NET");
	}

	private static HaloClientOptions CreateOptions(string? userAgent) => new()
	{
		Account = "testaccount",
		ClientId = "550e8400-e29b-41d4-a716-446655440000",
		ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000",
		UserAgent = userAgent
	};

	private static async Task SendApiRequestAsync(HttpMessageHandler handler)
	{
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://testaccount.halopsa.com/api/Tickets");

		using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private sealed record TokenRequest(string Path, string UserAgent);

	private sealed class TokenStub : HttpMessageHandler
	{
		public List<TokenRequest> Requests { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Requests.Add(new TokenRequest(request.RequestUri!.AbsolutePath, request.Headers.UserAgent.ToString()));

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					"""{"access_token":"token-1","token_type":"Bearer","expires_in":3600}""",
					Encoding.UTF8,
					"application/json")
			});
		}
	}
}
