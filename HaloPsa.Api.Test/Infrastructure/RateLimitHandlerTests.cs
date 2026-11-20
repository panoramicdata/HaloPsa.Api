using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Http;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.RateLimiting;
using System.Net;

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
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
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
	public void WithoutALimiter_TheChainAndTimeoutAreUnchanged()
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
		=> new()
		{
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

	private sealed class HaloStub(Func<int, CancellationToken, Task<HttpResponseMessage>> api, HttpStatusCode tokenStatus) : HttpMessageHandler
	{
		private int _apiCalls;

		/// <summary>
		/// Every request that reached the transport, token requests included
		/// </summary>
		public int CallCount { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;

			if (request.RequestUri!.AbsolutePath == "/auth/token")
			{
				return new HttpResponseMessage(tokenStatus)
				{
					Content = new StringContent(tokenStatus == HttpStatusCode.OK
						? """{"access_token":"stub-token","token_type":"Bearer","expires_in":3600}"""
						: "slow down")
				};
			}

			return await api(_apiCalls++, cancellationToken).ConfigureAwait(false);
		}
	}
}
