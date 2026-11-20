using AwesomeAssertions;
using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace HaloPsa.Api.Test.Infrastructure;

public class ErrorHandlerTests
{
	[Fact]
	public async Task RequestNotSent_IsLoggedBelowError_AndRethrownUnchanged()
	{
		var thrown = new HaloRequestNotSentException("Halo is busy");
		var logger = new ListLogger();
		using var handler = new ErrorHandler(logger) { InnerHandler = new ThrowingHandler(thrown) };
		using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/api/Tickets");

		var act = () => invoker.SendAsync(request, TestContext.Current.CancellationToken);

		var caught = (await act.Should().ThrowAsync<HaloRequestNotSentException>()).Which;
		_ = caught.Should().BeSameAs(thrown);
		_ = logger.Levels.Should().ContainSingle().Which.Should().Be(LogLevel.Warning);
		_ = logger.Levels.Should().NotContain(LogLevel.Error);
	}

	private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> throw exception;
	}
}
