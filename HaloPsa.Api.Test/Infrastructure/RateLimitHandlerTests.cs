using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Http;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.RateLimiting;
using Microsoft.Extensions.Logging;
using Refit;
using System.Net;
using System.Net.Http.Headers;

namespace HaloPsa.Api.Test.Infrastructure;

public class RateLimitHandlerTests
{
	private const string _tokenRequest = "POST /auth/token";
	private const string _ticketsRequest = "GET /api/Tickets";

	[Fact]
	public async Task EveryAttempt_IsPaced_IncludingRetries()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = limiter.Applied.Should().Equal(_tokenRequest, _ticketsRequest, _ticketsRequest);
		_ = limiter.Responses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
	}

	[Fact]
	public async Task The401Resend_IsPaced()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.Unauthorized, HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = limiter.Applied.Should().Equal(_tokenRequest, _ticketsRequest, _tokenRequest, _ticketsRequest);
	}

	[Fact]
	public async Task TheTokenRequest_IsPaced()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = limiter.Applied.Should().ContainSingle(request => request == _tokenRequest);
	}

	[Fact]
	public async Task TheTimeout_StartsAfterTheSlotIsAcquired()
	{
		var limiter = new RecordingRateLimiter { DelayBeforeSlot = TimeSpan.FromMilliseconds(300) };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(200)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = limiter.Failures.Should().BeEmpty();
	}

	[Fact]
	public async Task AStalledAttempt_TimesOutWithATimeoutInner()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(async (_, cancellationToken) =>
		{
			await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowAsync<TaskCanceledException>()).Which;
		var timeout = thrown.InnerException.Should().BeOfType<TimeoutException>().Subject;
		_ = timeout.InnerException.Should().BeAssignableTo<OperationCanceledException>();
		_ = limiter.Failures.Should().ContainSingle().Which.Should().Be<TaskCanceledException>();
	}

	[Fact]
	public async Task CancellingWhileWaitingForASlot_NeverSends()
	{
		var limiter = new RecordingRateLimiter { DelayBeforeSlot = TimeSpan.FromSeconds(10) };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var handler = new RateLimitHandler(limiter, TimeSpan.FromSeconds(30)) { InnerHandler = stub };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.halopsa.com/api/Tickets");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => invoker.SendAsync(request, cancellation.Token);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.Applied.Should().BeEmpty();
		_ = limiter.Failures.Should().ContainSingle();
	}

	[Fact]
	public async Task CancellingTheTokenRequestsWaitForASlot_ReachesTheCallerAsACancellation()
	{
		var limiter = new RecordingRateLimiter { DelayBeforeSlot = TimeSpan.FromSeconds(10) };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => http.GetAsync("/api/Tickets", cancellation.Token);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.Applied.Should().BeEmpty();
		_ = limiter.Failures.Should().ContainSingle().Which.Should().BeAssignableTo<OperationCanceledException>();
	}

	[Fact]
	public async Task CancellingTheTokenRequest_WithoutALimiter_ReachesTheCallerAsACancellation()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK) { TokenDelay = TimeSpan.FromSeconds(10) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => http.GetAsync("/api/Tickets", cancellation.Token);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task ATokenRequestThatTimesOutOnItsOwn_StillSurfacesAsAnAuthenticationException()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK) { TokenDelay = TimeSpan.FromSeconds(10) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<AuthenticationException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TaskCanceledException>().Which.InnerException.Should().BeOfType<TimeoutException>();
		_ = limiter.Failures.Should().ContainSingle();
	}

	[Fact]
	public async Task AGiveUp_PropagatesUnwrapped_AndTakesNoSlot()
	{
		var limiter = new RecordingRateLimiter { ThrowOnApply = new HaloRequestNotSentException("busy") };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<HaloRequestNotSentException>();
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.Applied.Should().BeEmpty();
		_ = limiter.Failures.Should().BeEmpty();
		_ = limiter.Responses.Should().BeEmpty();
	}

	[Fact]
	public async Task AGiveUpOnTheTokenRequest_ReachesAPsaCallerUnwrapped()
	{
		var limiter = new RecordingRateLimiter { ThrowOnApply = new HaloRequestNotSentException("busy"), ThrowOnApplyForPath = "/auth/token" };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<HaloRequestNotSentException>();
		_ = stub.CallCount.Should().Be(0);
	}

	[Fact]
	public async Task AGiveUpOnTheApiRequest_ReachesAPsaCallerUnwrapped_AfterTheTokenWasGranted()
	{
		var limiter = new RecordingRateLimiter { ThrowOnApply = new HaloRequestNotSentException("busy"), ThrowOnApplyForPath = "/api/Status" };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<HaloRequestNotSentException>();
		_ = limiter.Applied.Should().Equal(_tokenRequest);
		_ = stub.CallCount.Should().Be(1);
	}

	[Fact]
	public async Task ATokenTooManyRequests_ReachesAPsaCallerInsideAnApiRequestException()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.TooManyRequests) { TokenRetryAfter = TimeSpan.FromSeconds(30) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<ApiRequestException>()).Which;
		var authentication = thrown.InnerException.Should().BeOfType<AuthenticationException>().Subject;
		var rateLimit = authentication.InnerException.Should().BeOfType<HaloRateLimitException>().Subject;
		_ = rateLimit.RetryAfterSeconds.Should().Be(30);
	}

	[Fact]
	public async Task AGiveUp_IsNotLoggedAtError_WithRequestAndResponseLoggingOn()
	{
		var logger = new ListLogger();
		var limiter = new RecordingRateLimiter { ThrowOnApply = new HaloRequestNotSentException("busy"), ThrowOnApplyForPath = "/api/Tickets" };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		var options = CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30), logger, logRequestsAndResponses: true);
		using var http = DefaultHttpClientFactory.Create(options, () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<HaloRequestNotSentException>();
		_ = logger.Levels.Should().NotContain(LogLevel.Error);
		_ = logger.Levels.Should().Contain(LogLevel.Warning);
	}

	[Fact]
	public async Task WithoutALimiter_ATokenRequestThatTimesOut_SurfacesAsATimeoutNotAnAuthenticationException()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK) { TokenDelay = TimeSpan.FromSeconds(10) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<TaskCanceledException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
	}

	[Fact]
	public async Task AGetThatTimesOutPerAttempt_IsRetried_EachRetryTakingASlot()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Stall, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<TaskCanceledException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
		_ = limiter.Applied.Should().Equal(_tokenRequest, _ticketsRequest, _ticketsRequest);
		_ = limiter.Failures.Should().Equal(typeof(TaskCanceledException), typeof(TaskCanceledException));
	}

	[Fact]
	public async Task APostThatTimesOutPerAttempt_IsNotRetried()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Stall, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromMilliseconds(100)), () => stub);
		using var content = new StringContent("{}");

		var act = () => http.PostAsync("/api/Tickets", content, TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<TaskCanceledException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
		_ = limiter.Applied.Should().Equal(_tokenRequest, "POST /api/Tickets");
		_ = limiter.Failures.Should().ContainSingle();
	}

	[Fact]
	public async Task TheCallerCancellingMidAttempt_SurfacesAsTheCallersCancellation_NotATimeout()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Stall, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => http.GetAsync("/api/Tickets", cancellation.Token);

		var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
		for (Exception? inner = thrown; inner is not null; inner = inner.InnerException)
		{
			_ = inner.Should().NotBeOfType<TimeoutException>();
		}

		_ = limiter.Applied.Should().Equal(_tokenRequest, _ticketsRequest);
		_ = limiter.Failures.Should().ContainSingle().Which.Should().BeAssignableTo<OperationCanceledException>();
	}

	[Fact]
	public async Task ABodyThatStallsPastTheTimeout_FailsTheAttemptAndDisposesTheResponse()
	{
		var limiter = new RecordingRateLimiter();
		var content = new RecordingContent { BodyDelay = TimeSpan.FromSeconds(10) };
		var stub = new HaloStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowAsync<TaskCanceledException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
		_ = stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
		_ = limiter.Failures.Should().ContainSingle().Which.Should().Be<TaskCanceledException>();
		_ = limiter.Responses.Should().Equal(HttpStatusCode.OK);
		_ = content.IsDisposed.Should().BeTrue();
	}

	[Fact]
	public async Task AnOnResponseThatThrows_DisposesTheResponse()
	{
		var limiter = new RecordingRateLimiter { ThrowOnResponse = new InvalidOperationException("limiter bug"), ThrowOnResponseForPath = "/api/Tickets" };
		var content = new RecordingContent();
		var stub = new HaloStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<InvalidOperationException>();
		_ = content.IsDisposed.Should().BeTrue();
	}

	[Fact]
	public async Task ATokenTooManyRequests_ReachesOnResponse()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.TooManyRequests);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowExactlyAsync<AuthenticationException>();
		_ = limiter.Responses.Should().Equal(HttpStatusCode.TooManyRequests);
	}

	[Fact]
	public void WithoutALimiter_TheOuterTimeoutIsRequestTimeout()
	{
		var timeout = TimeSpan.FromSeconds(12);

		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 0, timeout), () => new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK));

		_ = http.Timeout.Should().Be(timeout);
	}

	[Fact]
	public void WithALimiter_TheOuterTimeoutIsInfinite()
	{
		using var http = DefaultHttpClientFactory.Create(
			CreateOptions(new RecordingRateLimiter(), maxRetryAttempts: 0, TimeSpan.FromSeconds(12)),
			() => new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK));

		_ = http.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
	}

	private static HaloClientOptions CreateOptions(IRateLimiter? limiter, int maxRetryAttempts, TimeSpan requestTimeout)
		=> CreateOptions(limiter, maxRetryAttempts, requestTimeout, logger: null, logRequestsAndResponses: false);

	private static HaloClientOptions CreateOptions(IRateLimiter? limiter, int maxRetryAttempts, TimeSpan requestTimeout, ILogger? logger, bool logRequestsAndResponses)
		=> new()
		{
			Logger = logger,
			EnableRequestLogging = logRequestsAndResponses,
			EnableResponseLogging = logRequestsAndResponses,
			Account = "example",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000",
			RateLimiter = limiter,
			MaxRetryAttempts = maxRetryAttempts,
			RetryDelay = TimeSpan.FromMilliseconds(1),
			RequestTimeout = requestTimeout
		};

	private static Func<int, CancellationToken, Task<HttpResponseMessage>> Statuses(params HttpStatusCode[] statuses)
		=> (call, _) => Task.FromResult(new HttpResponseMessage(statuses[Math.Min(call, statuses.Length - 1)])
		{
			Content = new StringContent("{}")
		});

	private static async Task<HttpResponseMessage> Stall(int call, CancellationToken cancellationToken)
	{
		await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <summary>
	/// A body that takes <see cref="BodyDelay"/> to be written, honouring cancellation, and records its disposal
	/// </summary>
	private sealed class RecordingContent : HttpContent
	{
		public TimeSpan BodyDelay { get; init; } = TimeSpan.Zero;

		public bool IsDisposed { get; private set; }

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
			=> SerializeToStreamAsync(stream, context, CancellationToken.None);

		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
		{
			if (BodyDelay > TimeSpan.Zero)
			{
				await Task.Delay(BodyDelay, cancellationToken).ConfigureAwait(false);
			}

			await stream.WriteAsync("{}"u8.ToArray(), cancellationToken).ConfigureAwait(false);
		}

		protected override bool TryComputeLength(out long length)
		{
			length = -1;
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	private sealed class HaloStub(Func<int, CancellationToken, Task<HttpResponseMessage>> api, HttpStatusCode tokenStatus) : HttpMessageHandler
	{
		private int _apiCalls;

		/// <summary>
		/// How long a token request takes to be answered; the wait ends early when the request is cancelled
		/// </summary>
		public TimeSpan TokenDelay { get; init; } = TimeSpan.Zero;

		/// <summary>
		/// The Retry-After sent with a non-OK token response, when set
		/// </summary>
		public TimeSpan? TokenRetryAfter { get; init; }

		/// <summary>
		/// Every request that reached the transport, token requests included
		/// </summary>
		public int CallCount { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;

			if (request.RequestUri!.AbsolutePath == "/auth/token")
			{
				if (TokenDelay > TimeSpan.Zero)
				{
					await Task.Delay(TokenDelay, cancellationToken).ConfigureAwait(false);
				}

				var tokenResponse = new HttpResponseMessage(tokenStatus)
				{
					Content = new StringContent(tokenStatus == HttpStatusCode.OK
						? """{"access_token":"stub-token","token_type":"Bearer","expires_in":3600}"""
						: "slow down")
				};

				if (TokenRetryAfter is { } retryAfter)
				{
					tokenResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
				}

				return tokenResponse;
			}

			return await api(_apiCalls++, cancellationToken).ConfigureAwait(false);
		}
	}
}
