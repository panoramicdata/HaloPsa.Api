using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Http;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.RateLimiting;
using Microsoft.Extensions.Logging;
using Refit;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;

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
		using var handler = new RateLimitHandler(limiter, TimeSpan.FromSeconds(30), isTokenRequest: false) { InnerHandler = stub };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.halopsa.com/api/Tickets");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => invoker.SendAsync(request, cancellation.Token);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.Applied.Should().BeEmpty();
		_ = limiter.Failures.Should().BeEmpty();
		_ = limiter.Responses.Should().BeEmpty();
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
		_ = limiter.Failures.Should().BeEmpty();
		_ = limiter.Responses.Should().BeEmpty();
	}

	[Fact]
	public async Task CancellingTheTokenRequest_WithoutALimiter_IsWrappedInAnAuthenticationException()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK) { TokenDelay = TimeSpan.FromSeconds(10) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => http.GetAsync("/api/Tickets", cancellation.Token);

		var thrown = (await act.Should().ThrowExactlyAsync<AuthenticationException>()).Which;
		_ = thrown.InnerException.Should().BeAssignableTo<OperationCanceledException>();
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
	public async Task ACallerCancelledWait_GetsNoCallback_AndALimiterCountingPermitsStaysBalanced()
	{
		var limiter = new PermitCountingRateLimiter { DelayBeforeSlot = TimeSpan.FromSeconds(10) };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var handler = new RateLimitHandler(limiter, TimeSpan.FromSeconds(30), isTokenRequest: false) { InnerHandler = stub };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.halopsa.com/api/Tickets");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		var act = () => invoker.SendAsync(request, cancellation.Token);

		var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
		_ = thrown.CancellationToken.Should().Be(cancellation.Token);
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.ResponseCalls.Should().Be(0);
		_ = limiter.SendFailedCalls.Should().Be(0);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task ALimitersOwnCancellation_GetsNoCallback_IsRetried_AndALimiterCountingPermitsStaysBalanced()
	{
		var limiter = new PermitCountingRateLimiter { OwnCancellationPath = "/api/Tickets", OwnCancellationCount = 1 };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = limiter.ApplyCalls.Should().Be(3);
		_ = limiter.ResponseCalls.Should().Be(2);
		_ = limiter.SendFailedCalls.Should().Be(0);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task ALimitersOwnCancellationThatKeepsHappening_ReachesTheCallerAfterTheRetries_WithoutCallbacks()
	{
		var limiter = new PermitCountingRateLimiter { OwnCancellationPath = "/api/Tickets", OwnCancellationCount = int.MaxValue };
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<TaskCanceledException>();
		_ = limiter.ApplyCalls.Should().Be(3);
		_ = limiter.ResponseCalls.Should().Be(1);
		_ = limiter.SendFailedCalls.Should().Be(0);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task AnAttemptThatTookASlotAndTimedOut_GetsExactlyOneOnSendFailed()
	{
		var limiter = new PermitCountingRateLimiter();
		var stub = new HaloStub(Stall, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<TaskCanceledException>();
		_ = limiter.ApplyCalls.Should().Be(2);
		_ = limiter.ResponseCalls.Should().Be(1);
		_ = limiter.SendFailedCalls.Should().Be(1);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task AnAttemptThatTookASlotAndCouldNotStartItsTimeout_GetsExactlyOneOnSendFailed()
	{
		var limiter = new PermitCountingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromDays(60)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<AuthenticationException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
		_ = stub.CallCount.Should().Be(0);
		_ = limiter.ApplyCalls.Should().Be(1);
		_ = limiter.ResponseCalls.Should().Be(0);
		_ = limiter.SendFailedCalls.Should().Be(1);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task AnAttemptThatTookASlotAndFailedInTransit_GetsExactlyOneOnSendFailed()
	{
		var limiter = new PermitCountingRateLimiter();
		var stub = new HaloStub((_, _) => throw new HttpRequestException("connection reset"), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<HttpRequestException>();
		_ = limiter.ApplyCalls.Should().Be(2);
		_ = limiter.ResponseCalls.Should().Be(1);
		_ = limiter.SendFailedCalls.Should().Be(1);
		_ = limiter.LowestHeldPermits.Should().Be(0);
		_ = limiter.HeldPermits.Should().Be(0);
	}

	[Fact]
	public async Task AnApiTooManyRequests_ReachesAPsaCallerAsAHaloRateLimitException_WithItsRetryAfter()
	{
		var stub = new HaloStub((_, _) =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("slow down") };
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
			return Task.FromResult(response);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(new RecordingRateLimiter(), maxRetryAttempts: 3, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<HaloRateLimitException>()).Which;
		_ = thrown.RetryAfterSeconds.Should().Be(30);
		_ = stub.CallCount.Should().Be(2);
	}

	[Fact]
	public async Task WithoutALimiter_AnApiTooManyRequests_IsRetried()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.TooManyRequests, HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 3, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = stub.CallCount.Should().Be(3);
	}

	[Fact]
	public async Task WithoutALimiter_AnApiTooManyRequestsThatPersists_IsRetriedThenReachesAPsaCallerWithoutRetryAfter()
	{
		var stub = new HaloStub((_, _) =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("slow down") };
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
			return Task.FromResult(response);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 3, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<HaloRateLimitException>()).Which;
		_ = thrown.RetryAfterSeconds.Should().BeNull();
		_ = stub.CallCount.Should().Be(5);
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
	public async Task ATokenTooManyRequests_IsSentOnce_AndReachesAPsaCallerInsideAnApiRequestException_WithALimiter()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.TooManyRequests) { TokenRetryAfter = TimeSpan.FromSeconds(30) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 3, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<ApiRequestException>()).Which;
		var authentication = thrown.InnerException.Should().BeOfType<AuthenticationException>().Subject;
		_ = authentication.InnerException.Should().BeOfType<HaloRateLimitException>();
		_ = stub.TokenCalls.Should().Be(1);
		_ = stub.CallCount.Should().Be(1);
		_ = limiter.Applied.Should().Equal(_tokenRequest);
	}

	[Fact]
	public async Task WithoutALimiter_ATokenTooManyRequests_IsSentOnce_AndReachesAPsaCallerInsideAnApiRequestException()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.TooManyRequests) { TokenRetryAfter = TimeSpan.FromSeconds(30) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 3, TimeSpan.FromSeconds(30)), () => stub);
		var psa = new PsaApi(http, readOnly: false);

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<ApiRequestException>()).Which;
		var authentication = thrown.InnerException.Should().BeOfType<AuthenticationException>().Subject;
		_ = authentication.InnerException.Should().BeOfType<HaloRateLimitException>();
		_ = stub.TokenCalls.Should().Be(1);
		_ = stub.CallCount.Should().Be(1);
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
	public async Task WithoutALimiter_ATokenRequestThatTimesOut_SurfacesAsAnAuthenticationException()
	{
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK) { TokenDelay = TimeSpan.FromSeconds(10) };
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter: null, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<AuthenticationException>()).Which;
		_ = thrown.InnerException.Should().BeAssignableTo<OperationCanceledException>();
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
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var stub = new HaloStub(async (_, cancellationToken) =>
		{
			await cancellation.CancelAsync();
			await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
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
	public async Task TheCallerCancellingWhileATooManyRequestsBodyIsRead_SurfacesAsTheCallersCancellation()
	{
		var limiter = new RecordingRateLimiter();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var stub = new HaloStub(async (_, _) =>
		{
			await cancellation.CancelAsync();
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new RecordingContent { BodyDelay = TimeSpan.FromSeconds(10) } };
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
			return response;
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		var act = () => http.GetAsync("/api/Tickets", cancellation.Token);

		var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
		for (Exception? inner = thrown; inner is not null; inner = inner.InnerException)
		{
			_ = inner.Should().NotBeOfType<TimeoutException>();
			_ = inner.Should().NotBeOfType<HaloRateLimitException>();
		}

		_ = limiter.Failures.Should().ContainSingle().Which.Should().BeAssignableTo<OperationCanceledException>();
		_ = limiter.Responses.Should().Equal(HttpStatusCode.OK);
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
	public async Task ATooManyRequestsWhoseBodyStalls_IsReportedAsThe429_AndNeverRetried()
	{
		var limiter = new RecordingRateLimiter();
		var content = new RecordingContent { BodyDelay = TimeSpan.FromSeconds(10) };
		var stub = new HaloStub((_, _) =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = content };
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
			return Task.FromResult(response);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 3, TimeSpan.FromMilliseconds(100)), () => stub);
		var psa = new PsaApi(http, readOnly: false);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		var act = () => psa.Statuses.GetAllAsync(TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowExactlyAsync<HaloRateLimitException>()).Which;
		_ = thrown.RetryAfterSeconds.Should().Be(30);
		_ = stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
		_ = stub.CallCount.Should().Be(2);
		_ = limiter.Responses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
		_ = limiter.Failures.Should().BeEmpty();
		_ = content.IsDisposed.Should().BeTrue();
		_ = limiter.AppliedAttempts.Should().HaveCount(2);
		AssertCallbacksReceivedTheAppliedInstances(limiter);
	}

	[Fact]
	public async Task AServiceUnavailableWhoseBodyStalls_StillFailsTheAttempt_ReportingAFailureNotAResponse()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new RecordingContent { BodyDelay = TimeSpan.FromSeconds(10) } }), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		var thrown = (await act.Should().ThrowAsync<TaskCanceledException>()).Which;
		_ = thrown.InnerException.Should().BeOfType<TimeoutException>();
		_ = stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
		_ = limiter.Failures.Should().ContainSingle().Which.Should().Be<TaskCanceledException>();
		_ = limiter.Responses.Should().Equal(HttpStatusCode.OK);
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

	[Fact]
	public async Task EveryAttemptIncludingRetries_GetsADistinctAttempt_AndItsCallbackReceivesThatInstance()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 1, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Should().HaveCount(3);
		_ = limiter.AppliedAttempts.Cast<object>().Distinct(ReferenceEqualityComparer.Instance).Should().HaveCount(3);
		AssertCallbacksReceivedTheAppliedInstances(limiter);
	}

	[Fact]
	public async Task The401Resend_GetsItsOwnAttempts_AndTheirCallbacksReceiveThoseInstances()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.Unauthorized, HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Should().HaveCount(4);
		_ = limiter.AppliedAttempts.Cast<object>().Distinct(ReferenceEqualityComparer.Instance).Should().HaveCount(4);
		AssertCallbacksReceivedTheAppliedInstances(limiter);
	}

	[Fact]
	public async Task AFailedAttempt_ReachesOnSendFailedWithTheInstanceItWasAppliedWith()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Stall, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromMilliseconds(100)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<TaskCanceledException>();
		_ = limiter.AppliedAttempts.Should().HaveCount(2);
		_ = limiter.FailedAttempts.Should().ContainSingle().Which.Should().BeSameAs(limiter.AppliedAttempts[1]);
		_ = limiter.RespondedAttempts.Should().ContainSingle().Which.Should().BeSameAs(limiter.AppliedAttempts[0]);
	}

	[Fact]
	public async Task AnAttemptThatFailedInTransit_ReachesOnSendFailedWithTheInstanceItWasAppliedWith()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub((_, _) => throw new HttpRequestException("connection reset"), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		var act = () => http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<HttpRequestException>();
		_ = limiter.AppliedAttempts.Should().HaveCount(2);
		_ = limiter.FailedAttempts.Should().ContainSingle().Which.Should().BeSameAs(limiter.AppliedAttempts[1]);
		_ = limiter.RespondedAttempts.Should().ContainSingle().Which.Should().BeSameAs(limiter.AppliedAttempts[0]);
	}

	[Fact]
	public async Task OnlyTheTokenRequest_IsMarkedAsATokenRequest()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Select(attempt => (attempt.RequestUri!.AbsolutePath, attempt.IsTokenRequest))
			.Should().Equal(("/auth/token", true), ("/api/Tickets", false));
	}

	[Fact]
	public async Task AHandlerBuiltAsTheTokenHandler_MarksEveryAttemptAsATokenRequest_WhateverTheUri()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var handler = new RateLimitHandler(limiter, TimeSpan.FromSeconds(30), isTokenRequest: true) { InnerHandler = stub };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.halopsa.com/api/Tickets");

		using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Should().ContainSingle().Which.IsTokenRequest.Should().BeTrue();
	}

	[Fact]
	public async Task AHandlerBuiltAsTheMainHandler_DoesNotMarkATokenUriAsATokenRequest()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var handler = new RateLimitHandler(limiter, TimeSpan.FromSeconds(30), isTokenRequest: false) { InnerHandler = stub };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.halopsa.com/auth/token");

		using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Should().ContainSingle().Which.IsTokenRequest.Should().BeFalse();
	}

	[Fact]
	public async Task AnApiRequestToAnAuthPath_ThroughTheMainClient_IsNotMarkedAsATokenRequest()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/auth/info", TestContext.Current.CancellationToken);

		_ = limiter.AppliedAttempts.Select(attempt => (attempt.RequestUri!.AbsolutePath, attempt.IsTokenRequest))
			.Should().Equal(("/auth/info", false));
	}

	[Fact]
	public async Task AnAttempt_CarriesTheMethodAndUriOfTheRequestSent()
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub(Statuses(HttpStatusCode.OK), HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);
		using var content = new StringContent("{}");

		using var response = await http.PostAsync("/api/Tickets?count=5", content, TestContext.Current.CancellationToken);

		var tokenAttempt = limiter.AppliedAttempts[0];
		_ = tokenAttempt.Method.Should().Be(HttpMethod.Post);
		_ = tokenAttempt.RequestUri.Should().Be(new Uri("https://example.halopsa.com/auth/token"));
		var apiAttempt = limiter.AppliedAttempts[1];
		_ = apiAttempt.Method.Should().Be(HttpMethod.Post);
		_ = apiAttempt.RequestUri.Should().Be(new Uri("https://example.halopsa.com/api/Tickets?count=5"));
	}

	[Fact]
	public async Task TheResponseView_CarriesTheStatusAndARetryAfterGivenAsADelay()
	{
		var view = await SendAndGetResponseViewAsync(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)));

		_ = view.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
		_ = view.RetryAfter.Should().NotBeNull();
		_ = view.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(30));
		_ = view.RetryAfter.Date.Should().BeNull();
	}

	[Fact]
	public async Task TheResponseView_CarriesARetryAfterGivenAsADate()
	{
		var date = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

		var view = await SendAndGetResponseViewAsync(HttpStatusCode.ServiceUnavailable, new RetryConditionHeaderValue(date));

		_ = view.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
		_ = view.RetryAfter.Should().NotBeNull();
		_ = view.RetryAfter!.Date.Should().Be(date);
		_ = view.RetryAfter.Delta.Should().BeNull();
	}

	[Fact]
	public async Task TheResponseView_HasNoRetryAfterWhenTheResponseHadNone()
	{
		var view = await SendAndGetResponseViewAsync(HttpStatusCode.OK, retryAfter: null);

		_ = view.StatusCode.Should().Be(HttpStatusCode.OK);
		_ = view.RetryAfter.Should().BeNull();
	}

	[Fact]
	public void TheRateLimiterSurface_ExposesNoRequestResponseHeadersOrContent()
	{
		Type[] forbidden = [typeof(HttpRequestMessage), typeof(HttpResponseMessage), typeof(HttpHeaders), typeof(HttpContent)];
		Type[] surface = [typeof(IRateLimiter), typeof(RateLimitAttempt), typeof(RateLimitResponse)];
		const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		var exposed = surface
			.SelectMany(type => type.GetMembers(publicMembers))
			.SelectMany(ExposedTypes)
			.SelectMany(Flatten)
			.Where(type => forbidden.Any(type.IsAssignableTo))
			.Distinct()
			.ToList();

		_ = exposed.Should().BeEmpty();
	}

	[Fact]
	public void TheRateLimiterSurface_IsExactlyTheReviewedMembers()
	{
		Type[] surface = [typeof(IRateLimiter), typeof(RateLimitAttempt), typeof(RateLimitResponse)];
		const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		var members = surface
			.SelectMany(type => type.GetMembers(publicMembers).Where(member => !IsAccessorDescribedByItsOwner(type, member)).Select(member => (type, member)))
			.Select(entry => $"{entry.type.Name}.{Describe(entry.member)}")
			.ToList();

		_ = members.Should().BeEquivalentTo(
		[
			"IRateLimiter.ApplyRateLimitingAsync(RateLimitAttempt, CancellationToken): Task",
			"IRateLimiter.OnResponse(RateLimitAttempt, RateLimitResponse): Void",
			"IRateLimiter.OnSendFailed(RateLimitAttempt, Exception): Void",
			"RateLimitAttempt.IsTokenRequest: Boolean { get; }",
			"RateLimitAttempt.Method: HttpMethod { get; }",
			"RateLimitAttempt.RequestUri: Uri { get; }",
			"RateLimitResponse.RetryAfter: RetryConditionHeaderValue { get; }",
			"RateLimitResponse.StatusCode: HttpStatusCode { get; }"
		]);
	}

	private static bool IsAccessorDescribedByItsOwner(Type type, MemberInfo member)
		=> member is MethodInfo method
		&& type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Any(owner => owner switch
			{
				PropertyInfo property => property.GetMethod == method || property.SetMethod == method,
				EventInfo declaredEvent => declaredEvent.AddMethod == method || declaredEvent.RemoveMethod == method,
				_ => false
			});

	private static string Describe(MemberInfo member)
		=> member switch
		{
			PropertyInfo property => $"{property.Name}: {property.PropertyType.Name} {{ {DescribeAccessors(property)} }}",
			FieldInfo field => $"{field.Name}: {field.FieldType.Name}",
			EventInfo declaredEvent => $"{declaredEvent.Name}: {declaredEvent.EventHandlerType?.Name}",
			ConstructorInfo constructor => $"{constructor.Name}({DescribeParameters(constructor)})",
			MethodInfo method => $"{method.Name}({DescribeParameters(method)}): {method.ReturnType.Name}",
			_ => $"{member.MemberType} {member.Name}"
		};

	private static string DescribeParameters(MethodBase method)
		=> string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name));

	private static string DescribeAccessors(PropertyInfo property)
	{
		var accessors = new List<string>();

		if (property.GetMethod is { IsPublic: true })
		{
			accessors.Add("get;");
		}

		if (property.SetMethod is { IsPublic: true } setter)
		{
			var isInit = setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
			accessors.Add(isInit ? "init;" : "set;");
		}

		return string.Join(" ", accessors);
	}

	private static void AssertCallbacksReceivedTheAppliedInstances(RecordingRateLimiter limiter)
	{
		_ = limiter.FailedAttempts.Should().BeEmpty();
		_ = limiter.RespondedAttempts.Should().HaveCount(limiter.AppliedAttempts.Count);

		for (var i = 0; i < limiter.AppliedAttempts.Count; i++)
		{
			_ = limiter.RespondedAttempts[i].Should().BeSameAs(limiter.AppliedAttempts[i]);
		}
	}

	private static async Task<RateLimitResponse> SendAndGetResponseViewAsync(HttpStatusCode status, RetryConditionHeaderValue? retryAfter)
	{
		var limiter = new RecordingRateLimiter();
		var stub = new HaloStub((_, _) =>
		{
			var response = new HttpResponseMessage(status) { Content = new StringContent("{}") };
			response.Headers.RetryAfter = retryAfter;
			return Task.FromResult(response);
		}, HttpStatusCode.OK);
		using var http = DefaultHttpClientFactory.Create(CreateOptions(limiter, maxRetryAttempts: 0, TimeSpan.FromSeconds(30)), () => stub);

		using var response = await http.GetAsync("/api/Tickets", TestContext.Current.CancellationToken);

		return limiter.ResponseViews[^1];
	}

	private static IEnumerable<Type> ExposedTypes(MemberInfo member)
		=> member switch
		{
			PropertyInfo property => [property.PropertyType],
			FieldInfo field => [field.FieldType],
			MethodBase method => method.GetParameters().Select(parameter => parameter.ParameterType)
				.Concat(method is MethodInfo info ? [info.ReturnType] : []),
			_ => []
		};

	private static IEnumerable<Type> Flatten(Type type)
	{
		if (type.HasElementType)
		{
			return Flatten(type.GetElementType()!);
		}

		return type.IsGenericType ? type.GetGenericArguments().SelectMany(Flatten).Append(type) : [type];
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

	private static Func<int, CancellationToken, Task<HttpResponseMessage>> Statuses(params HttpStatusCode[] responseCodes)
		=> (call, _) => Task.FromResult(new HttpResponseMessage(responseCodes[Math.Min(call, responseCodes.Length - 1)])
		{
			Content = new StringContent("{}")
		});

	private static async Task<HttpResponseMessage> Stall(int call, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(call);
		await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <summary>
	/// An <see cref="IRateLimiter"/> that takes a permit when <see cref="ApplyRateLimitingAsync"/> returns and releases one in each callback,
	/// like a limiter bounding the requests in flight
	/// </summary>
	private sealed class PermitCountingRateLimiter : IRateLimiter
	{
		private readonly Lock _lock = new();
		private int _applyCalls;
		private int _ownCancellations;
		private int _heldPermits;
		private int _lowestHeldPermits;
		private int _responseCalls;
		private int _sendFailedCalls;

		/// <summary>
		/// How long a request waits for a slot; the wait ends early when the caller cancels
		/// </summary>
		public TimeSpan DelayBeforeSlot { get; init; } = TimeSpan.Zero;

		/// <summary>
		/// The absolute path whose first <see cref="OwnCancellationCount"/> applies throw the limiter's own <see cref="TaskCanceledException"/>
		/// </summary>
		public string? OwnCancellationPath { get; init; }

		/// <summary>
		/// How many applies for <see cref="OwnCancellationPath"/> throw the limiter's own cancellation
		/// </summary>
		public int OwnCancellationCount { get; init; }

		public int ApplyCalls => Read(ref _applyCalls);

		public int HeldPermits => Read(ref _heldPermits);

		public int LowestHeldPermits => Read(ref _lowestHeldPermits);

		public int ResponseCalls => Read(ref _responseCalls);

		public int SendFailedCalls => Read(ref _sendFailedCalls);

		public async Task ApplyRateLimitingAsync(RateLimitAttempt attempt, CancellationToken cancellationToken)
		{
			if (DelayBeforeSlot > TimeSpan.Zero)
			{
				await Task.Delay(DelayBeforeSlot, cancellationToken).ConfigureAwait(false);
			}

			lock (_lock)
			{
				_applyCalls++;

				if (attempt.RequestUri!.AbsolutePath == OwnCancellationPath && _ownCancellations < OwnCancellationCount)
				{
					_ownCancellations++;
					throw new TaskCanceledException("The limiter's own wait for a slot ran out.");
				}

				_heldPermits++;
			}
		}

		public void OnResponse(RateLimitAttempt attempt, RateLimitResponse response)
		{
			ArgumentNullException.ThrowIfNull(attempt);
			ArgumentNullException.ThrowIfNull(response);

			lock (_lock)
			{
				_responseCalls++;
				ReleasePermit();
			}
		}

		public void OnSendFailed(RateLimitAttempt attempt, Exception exception)
		{
			ArgumentNullException.ThrowIfNull(attempt);
			ArgumentNullException.ThrowIfNull(exception);

			lock (_lock)
			{
				_sendFailedCalls++;
				ReleasePermit();
			}
		}

		private void ReleasePermit()
		{
			_heldPermits--;
			_lowestHeldPermits = Math.Min(_lowestHeldPermits, _heldPermits);
		}

		private int Read(ref int field)
		{
			lock (_lock)
			{
				return field;
			}
		}
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
			_ = context;
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

		/// <summary>
		/// The token requests that reached the transport
		/// </summary>
		public int TokenCalls { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;

			if (request.RequestUri!.AbsolutePath == "/auth/token")
			{
				TokenCalls++;

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
