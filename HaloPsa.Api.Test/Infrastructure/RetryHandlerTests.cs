using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Models.Tickets;
using System.Net;
using System.Text;

namespace HaloPsa.Api.Test.Infrastructure;

public class RetryHandlerTests
{
	private const string _url = "https://example.invalid/api/Tickets";

	[Fact]
	public Task Post_IsNotRetriedOnAServerError()
		=> AssertAttemptsAsync(HttpMethod.Post, HttpStatusCode.ServiceUnavailable, expectedCalls: 1, expectedStatus: HttpStatusCode.ServiceUnavailable);

	[Fact]
	public Task Post_IsNotRetriedOnARequestTimeoutStatus()
		=> AssertAttemptsAsync(HttpMethod.Post, HttpStatusCode.RequestTimeout, expectedCalls: 1, expectedStatus: HttpStatusCode.RequestTimeout);

	[Fact]
	public async Task Post_IsNotRetriedOnATransportError()
	{
		var stub = new SequencingHandler(new HttpRequestException("connection reset"), HttpStatusCode.OK);

		var act = () => SendThroughRetryHandlerAsync(stub, HttpMethod.Post, TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<HttpRequestException>();
		_ = stub.CallCount.Should().Be(1);
	}

	[Fact]
	public Task Post_IsRetriedOnTooManyRequests()
		=> AssertAttemptsAsync(HttpMethod.Post, HttpStatusCode.TooManyRequests, expectedCalls: 2, expectedStatus: HttpStatusCode.OK);

	[Fact]
	public async Task Post_RetriedOnTooManyRequests_ResendsTheSameBody()
	{
		var stub = new SequencingHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.OK)
		{
			ResponseBody = """{"id":901,"summary":"S","status_id":1,"client_id":801,"user_id":802}"""
		};
		using var handler = CreateRetryHandler(stub);
		using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://example.invalid") };
		var psa = new PsaApi(httpClient, readOnly: false);

		var response = await psa.Tickets.CreateAsync(new CreateTicketRequest { Summary = "S", ClientId = 801, UserId = 802 }, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(2);
		_ = stub.RequestBodies[0].Should().Contain("\"client_id\":801");
		_ = stub.RequestBodies[1].Should().Be(stub.RequestBodies[0]);
		_ = response.TicketId.Should().Be(901);
	}

	[Fact]
	public Task Get_IsStillRetriedOnAServerError()
		=> AssertAttemptsAsync(HttpMethod.Get, HttpStatusCode.ServiceUnavailable, expectedCalls: 2, expectedStatus: HttpStatusCode.OK);

	[Fact]
	public Task Delete_IsStillRetriedOnAServerError()
		=> AssertAttemptsAsync(HttpMethod.Delete, HttpStatusCode.ServiceUnavailable, expectedCalls: 2, expectedStatus: HttpStatusCode.OK);

	[Fact]
	public async Task NoRequestIsRetriedOnceTheCallersTokenIsCancelled()
	{
		var stub = new SequencingHandler(new TaskCanceledException("cancelled"), HttpStatusCode.OK);

		var act = () => SendThroughRetryHandlerAsync(stub, HttpMethod.Get, new CancellationToken(canceled: true));

		_ = await act.Should().ThrowAsync<TaskCanceledException>();
		_ = stub.CallCount.Should().Be(1);
	}

	private static async Task AssertAttemptsAsync(HttpMethod method, HttpStatusCode firstOutcome, int expectedCalls, HttpStatusCode expectedStatus)
	{
		var stub = new SequencingHandler(firstOutcome, HttpStatusCode.OK);

		using var response = await SendThroughRetryHandlerAsync(stub, method, TestContext.Current.CancellationToken);

		_ = stub.CallCount.Should().Be(expectedCalls);
		_ = response.StatusCode.Should().Be(expectedStatus);
	}

	private static RetryHandler CreateRetryHandler(SequencingHandler stub)
		=> new(
			maxRetryAttempts: 2,
			retryDelay: TimeSpan.FromMilliseconds(1),
			useExponentialBackoff: false,
			maxRetryDelay: TimeSpan.FromMilliseconds(1),
			logger: null)
		{
			InnerHandler = stub
		};

	private static async Task<HttpResponseMessage> SendThroughRetryHandlerAsync(SequencingHandler stub, HttpMethod method, CancellationToken cancellationToken)
	{
		using var handler = CreateRetryHandler(stub);
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(method, _url);

		return await invoker.SendAsync(request, cancellationToken);
	}

	private sealed class SequencingHandler(params object[] outcomes) : HttpMessageHandler
	{
		private readonly Queue<object> _outcomes = new(outcomes);

		public int CallCount { get; private set; }

		public List<string?> RequestBodies { get; } = [];

		public string? ResponseBody { get; init; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

			var outcome = _outcomes.Dequeue();

			if (outcome is Exception exception)
			{
				throw exception;
			}

			var response = new HttpResponseMessage((HttpStatusCode)outcome);

			if (ResponseBody is not null)
			{
				response.Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json");
			}

			return response;
		}
	}
}
