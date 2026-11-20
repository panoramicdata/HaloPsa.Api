using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;

namespace HaloPsa.Api.Test.Infrastructure;

public class AuthenticationTests
{
	[Fact]
	public void HaloClientOptions_WithValidCredentials_ValidatesSuccessfully()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000"
		};

		// Act & Assert - Should not throw
		options.Validate();
	}

	[Fact]
	public void HaloClientOptions_WithInvalidClientId_ThrowsFormatException()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "invalid-guid",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000"
		};

		// Act & Assert
		var act = options.Validate;
		_ = act.Should().Throw<FormatException>()
			.WithMessage("*ClientId must be a valid GUID format*");
	}

	[Fact]
	public void AuthenticationHandler_WithValidCredentials_ShouldValidateOptions()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000",
			Logger = NullLogger.Instance
		};

		// Act & Assert - Verify the options are valid
		options.Validate();
		_ = options.Should().NotBeNull();
		_ = options.Account.Should().Be("testaccount");
	}

	[Fact]
	public async Task TokenRequest_TooManyRequests_ThrowsAuthenticationExceptionWithRateLimitInner()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000"
		};
		using var handler = new AuthenticationHandler(options, new TooManyRequestsTokenStub())
		{
			InnerHandler = new CapturingHandler("{}")
		};
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://testaccount.halopsa.com/api/Tickets");

		// Act
		var act = async () => await invoker.SendAsync(request, TestContext.Current.CancellationToken);

		// Assert
		var thrown = (await act.Should().ThrowExactlyAsync<AuthenticationException>()).Which;
		var inner = thrown.InnerException.Should().BeOfType<HaloRateLimitException>().Subject;
		_ = inner.RetryAfterSeconds.Should().Be(30);
		_ = inner.StatusCode.Should().Be(429);
	}

	private sealed class TooManyRequestsTokenStub : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
			{
				Content = new StringContent("slow down")
			};
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
			return Task.FromResult(response);
		}
	}
}