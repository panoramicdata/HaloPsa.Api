using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that provides logging capabilities for requests and responses
/// </summary>
internal sealed partial class LoggingHandler(
	ILogger logger,
	bool logRequests,
	bool logResponses) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var stopwatch = Stopwatch.StartNew();
		var requestId = Guid.NewGuid().ToString("N")[..8];

		if (logRequests)
		{
			LogHttpRequest(request, requestId);
		}

		HttpResponseMessage? response = null;
		Exception? exception = null;

		try
		{
			response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
			return response;
		}
		catch (Exception ex)
		{
			exception = ex;
			throw;
		}
		finally
		{
			stopwatch.Stop();
			LogOutcome(request, response, exception, stopwatch.Elapsed, requestId);
		}
	}

	private void LogOutcome(
		HttpRequestMessage request,
		HttpResponseMessage? response,
		Exception? exception,
		TimeSpan elapsed,
		string requestId)
	{
		if (exception != null)
		{
			LogHttpException(request, exception, elapsed, requestId);
		}
		else if (logResponses && response != null)
		{
			LogHttpResponse(response, elapsed, requestId);
		}
	}

	private void LogHttpRequest(HttpRequestMessage request, string requestId)
	{
		if (logger.IsEnabled(LogLevel.Information))
		{
			LogRequest(logger, requestId, request.Method, request.RequestUri);
		}

		if (request.Content != null && logger.IsEnabled(LogLevel.Debug))
		{
			LogRequestHeaders(logger, requestId, string.Join(", ", request.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));
		}
	}

	private void LogHttpResponse(HttpResponseMessage response, TimeSpan elapsed, string requestId)
	{
		var level = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
		if (logger.IsEnabled(level))
		{
			if (response.IsSuccessStatusCode)
				LogResponseSuccess(logger, requestId, (int)response.StatusCode, response.ReasonPhrase, elapsed.TotalMilliseconds);
			else
				LogResponseFailure(logger, requestId, (int)response.StatusCode, response.ReasonPhrase, elapsed.TotalMilliseconds);
		}

		if (!response.IsSuccessStatusCode && logger.IsEnabled(LogLevel.Debug))
		{
			LogResponseHeaders(logger, requestId, string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));
		}
	}

	private void LogHttpException(HttpRequestMessage request, Exception exception, TimeSpan elapsed, string requestId)
		=> LogException(logger, exception, requestId, request.Method, request.RequestUri, elapsed.TotalMilliseconds, exception.Message);

	[LoggerMessage(LogLevel.Information, "[{RequestId}] HTTP {Method} {Uri}")]
	private static partial void LogRequest(ILogger logger, string requestId, HttpMethod method, Uri? uri);

	[LoggerMessage(LogLevel.Debug, "[{RequestId}] Request Headers: {Headers}")]
	private static partial void LogRequestHeaders(ILogger logger, string requestId, string headers);

	[LoggerMessage(LogLevel.Information, "[{RequestId}] HTTP {StatusCode} {ReasonPhrase} in {ElapsedMs}ms")]
	private static partial void LogResponseSuccess(ILogger logger, string requestId, int statusCode, string? reasonPhrase, double elapsedMs);

	[LoggerMessage(LogLevel.Warning, "[{RequestId}] HTTP {StatusCode} {ReasonPhrase} in {ElapsedMs}ms")]
	private static partial void LogResponseFailure(ILogger logger, string requestId, int statusCode, string? reasonPhrase, double elapsedMs);

	[LoggerMessage(LogLevel.Debug, "[{RequestId}] Response Headers: {Headers}")]
	private static partial void LogResponseHeaders(ILogger logger, string requestId, string headers);

	[LoggerMessage(LogLevel.Error, "[{RequestId}] HTTP {Method} {Uri} failed after {ElapsedMs}ms: {ErrorMessage}")]
	private static partial void LogException(ILogger logger, Exception ex, string requestId, HttpMethod method, Uri? uri, double elapsedMs, string errorMessage);
}