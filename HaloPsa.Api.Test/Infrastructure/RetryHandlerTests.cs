using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using System.Net;

namespace HaloPsa.Api.Test.Infrastructure;

public class RetryHandlerTests
{
	private const string _url = "https://example.invalid/api/Tickets";

	[Fact]
	public async Task Post_IsNotRetriedOnAServerError()
	{
		var stub = new SequencingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

		using var response = await SendAsync(stub, HttpMethod.Post, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(1);
		_ = response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Fact]
	public async Task Post_IsNotRetriedOnARequestTimeoutStatus()
	{
		var stub = new SequencingHandler(HttpStatusCode.RequestTimeout, HttpStatusCode.OK);

		using var response = await SendAsync(stub, HttpMethod.Post, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(1);
		_ = response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
	}

	[Fact]
	public async Task Post_IsNotRetriedOnATransportError()
	{
		var stub = new SequencingHandler(new HttpRequestException("connection reset"), HttpStatusCode.OK);

		var act = () => SendAsync(stub, HttpMethod.Post, TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<HttpRequestException>();
		_ = stub.CallCount.Should().Be(1);
	}

	[Fact]
	public async Task Post_IsRetriedOnTooManyRequests()
	{
		var stub = new SequencingHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.OK);

		using var response = await SendAsync(stub, HttpMethod.Post, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(2);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Get_IsStillRetriedOnAServerError()
	{
		var stub = new SequencingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

		using var response = await SendAsync(stub, HttpMethod.Get, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(2);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Delete_IsStillRetriedOnAServerError()
	{
		var stub = new SequencingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

		using var response = await SendAsync(stub, HttpMethod.Delete, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(2);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task NoRequestIsRetriedOnceTheCallersTokenIsCancelled()
	{
		var stub = new SequencingHandler(new TaskCanceledException("cancelled"), HttpStatusCode.OK);

		var act = () => SendAsync(stub, HttpMethod.Get, new CancellationToken(canceled: true));

		_ = await act.Should().ThrowAsync<TaskCanceledException>();
		_ = stub.CallCount.Should().Be(1);
	}

	private static async Task<HttpResponseMessage> SendAsync(SequencingHandler stub, HttpMethod method, CancellationToken cancellationToken)
	{
		using var handler = new RetryHandler(
			maxRetryAttempts: 2,
			retryDelay: TimeSpan.FromMilliseconds(1),
			useExponentialBackoff: false,
			maxRetryDelay: TimeSpan.FromMilliseconds(1),
			logger: null)
		{
			InnerHandler = stub
		};
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(method, _url);

		return await invoker.SendAsync(request, cancellationToken);
	}

	private sealed class SequencingHandler(params object[] outcomes) : HttpMessageHandler
	{
		private readonly Queue<object> _outcomes = new(outcomes);

		public int CallCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;

			var outcome = _outcomes.Dequeue();

			return outcome is Exception exception
				? Task.FromException<HttpResponseMessage>(exception)
				: Task.FromResult(new HttpResponseMessage((HttpStatusCode)outcome));
		}
	}
}
