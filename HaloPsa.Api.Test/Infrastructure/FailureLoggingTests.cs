using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Http;
using HaloPsa.Api.Infrastructure;
using Refit;
using System.Net;
using System.Text;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// A failure handed to the caller is the caller's to log: the library logs it at Debug at most, with no exception attached (#96).
/// </summary>
public class FailureLoggingTests
{
	private const string _invalidClientBody = """{"error":"invalid_client"}""";
	private const string _tokenBody = """{"access_token":"stub-token","token_type":"Bearer","expires_in":3600}""";

	[Fact]
	public async Task BadCredentials_LogNothingAtWarningOrAbove_AndAttachNoException()
	{
		var logger = new ListLogger();
		var stub = new HaloStub(_ => Respond(HttpStatusCode.Unauthorized, _invalidClientBody), _ => Respond(HttpStatusCode.OK, "[]"));
		using var http = DefaultHttpClientFactory.Create(CreateOptions(logger, logRequestsAndResponses: false), () => stub);
		var psa = new PsaApi(http);

		var act = () => psa.TicketTypes.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<ApiRequestException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<AuthenticationException>()
			.Which.Message.Should().Contain("Unauthorized").And.Contain(_invalidClientBody);
		_ = stub.ApiCalls.Should().Be(0);
		AssertNothingAtWarningOrAbove(logger);
	}

	[Fact]
	public async Task TokenTransportFailure_LogsNothingAtWarningOrAbove_AndAttachesNoException()
	{
		var logger = new ListLogger();
		var stub = new HaloStub(_ => throw new HttpRequestException("No such host is known."), _ => Respond(HttpStatusCode.OK, "[]"));
		using var http = DefaultHttpClientFactory.Create(CreateOptions(logger, logRequestsAndResponses: false), () => stub);
		var psa = new PsaApi(http);

		var act = () => psa.TicketTypes.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<ApiRequestException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<AuthenticationException>()
			.Which.InnerException.Should().BeOfType<HttpRequestException>();
		AssertNothingAtWarningOrAbove(logger);
	}

	[Fact]
	public async Task NotFound_LogsNothingAtWarningOrAbove_AndAttachesNoException()
	{
		var logger = new ListLogger();
		var stub = new HaloStub(_ => Respond(HttpStatusCode.OK, _tokenBody), _ => Respond(HttpStatusCode.NotFound, "\"Ticket type 42 does not exist\""));
		using var http = DefaultHttpClientFactory.Create(CreateOptions(logger, logRequestsAndResponses: false), () => stub);
		var psa = new PsaApi(http);

		var act = () => psa.TicketTypes.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<HaloNotFoundException>()).Which;
		_ = thrown.StatusCode.Should().Be(404);
		_ = thrown.Message.Should().Contain("Ticket type 42 does not exist");
		AssertNothingAtWarningOrAbove(logger);
	}

	[Fact]
	public async Task BadCredentials_WithRequestAndResponseLoggingOn_LogNoErrorAndAttachNoException()
	{
		var logger = new ListLogger();
		var stub = new HaloStub(_ => Respond(HttpStatusCode.Unauthorized, _invalidClientBody), _ => Respond(HttpStatusCode.OK, "[]"));
		using var http = DefaultHttpClientFactory.Create(CreateOptions(logger, logRequestsAndResponses: true), () => stub);
		var psa = new PsaApi(http);

		var act = () => psa.TicketTypes.GetAllAsync(TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<ApiRequestException>();
		_ = logger.Levels.Should().NotContain(level => level >= LogLevel.Error);
		_ = logger.Exceptions.Should().AllSatisfy(exception => exception.Should().BeNull());
	}

	private static void AssertNothingAtWarningOrAbove(ListLogger logger)
	{
		_ = logger.Levels.Should().NotContain(level => level >= LogLevel.Warning, string.Join(Environment.NewLine, logger.Messages));
		_ = logger.Exceptions.Should().AllSatisfy(exception => exception.Should().BeNull());
	}

	private static HttpResponseMessage Respond(HttpStatusCode statusCode, string body)
		=> new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

	private static HaloClientOptions CreateOptions(ListLogger logger, bool logRequestsAndResponses)
		=> new()
		{
			Logger = logger,
			EnableRequestLogging = logRequestsAndResponses,
			EnableResponseLogging = logRequestsAndResponses,
			Account = "example",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000",
			RetryDelay = TimeSpan.FromMilliseconds(1)
		};

	private sealed class HaloStub(Func<HttpRequestMessage, HttpResponseMessage> token, Func<HttpRequestMessage, HttpResponseMessage> api) : HttpMessageHandler
	{
		public int ApiCalls { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (request.RequestUri!.AbsolutePath == "/auth/token")
			{
				return Task.FromResult(token(request));
			}

			ApiCalls++;
			return Task.FromResult(api(request));
		}
	}
}
