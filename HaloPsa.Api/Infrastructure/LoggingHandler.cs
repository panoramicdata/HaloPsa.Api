using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that provides logging capabilities for requests and responses
/// </summary>
internal sealed class LoggingHandler(
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
			{
				LogResponseSuccess(logger, requestId, (int)response.StatusCode, response.ReasonPhrase, elapsed.TotalMilliseconds);
			}
			else
			{
				LogResponseFailure(logger, requestId, (int)response.StatusCode, response.ReasonPhrase, elapsed.TotalMilliseconds);
			}
		}

		if (!response.IsSuccessStatusCode && logger.IsEnabled(LogLevel.Debug))
		{
			LogResponseHeaders(logger, requestId, string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));
		}
	}

	private void LogHttpException(HttpRequestMessage request, Exception exception, TimeSpan elapsed, string requestId)
		=> LogException(logger, exception, requestId, request.Method, request.RequestUri, elapsed.TotalMilliseconds, exception.Message);

	private static readonly Action<ILogger, string, HttpMethod, Uri?, Exception?> _logRequest = LoggerMessage.Define<string, HttpMethod, Uri?>(
		LogLevel.Information,
		new EventId(1, nameof(LogRequest)),
		"[{RequestId}] HTTP {Method} {Uri}");

	private static readonly Action<ILogger, string, string, Exception?> _logRequestHeaders = LoggerMessage.Define<string, string>(
		LogLevel.Debug,
		new EventId(2, nameof(LogRequestHeaders)),
		"[{RequestId}] Request Headers: {Headers}");

	private static readonly Action<ILogger, string, int, string?, double, Exception?> _logResponseSuccess = LoggerMessage.Define<string, int, string?, double>(
		LogLevel.Information,
		new EventId(3, nameof(LogResponseSuccess)),
		"[{RequestId}] HTTP {StatusCode} {ReasonPhrase} in {ElapsedMs}ms");

	private static readonly Action<ILogger, string, int, string?, double, Exception?> _logResponseFailure = LoggerMessage.Define<string, int, string?, double>(
		LogLevel.Warning,
		new EventId(4, nameof(LogResponseFailure)),
		"[{RequestId}] HTTP {StatusCode} {ReasonPhrase} in {ElapsedMs}ms");

	private static readonly Action<ILogger, string, string, Exception?> _logResponseHeaders = LoggerMessage.Define<string, string>(
		LogLevel.Debug,
		new EventId(5, nameof(LogResponseHeaders)),
		"[{RequestId}] Response Headers: {Headers}");

	private static readonly Action<ILogger, string, HttpMethod, Uri?, double, string, Exception?> _logException = LoggerMessage.Define<string, HttpMethod, Uri?, double, string>(
		LogLevel.Error,
		new EventId(6, nameof(LogException)),
		"[{RequestId}] HTTP {Method} {Uri} failed after {ElapsedMs}ms: {ErrorMessage}");

	private static void LogRequest(ILogger logger, string requestId, HttpMethod method, Uri? uri)
		=> _logRequest(logger, requestId, method, uri, null);

	private static void LogRequestHeaders(ILogger logger, string requestId, string headers)
		=> _logRequestHeaders(logger, requestId, headers, null);

	private static void LogResponseSuccess(ILogger logger, string requestId, int statusCode, string? reasonPhrase, double elapsedMs)
		=> _logResponseSuccess(logger, requestId, statusCode, reasonPhrase, elapsedMs, null);

	private static void LogResponseFailure(ILogger logger, string requestId, int statusCode, string? reasonPhrase, double elapsedMs)
		=> _logResponseFailure(logger, requestId, statusCode, reasonPhrase, elapsedMs, null);

	private static void LogResponseHeaders(ILogger logger, string requestId, string headers)
		=> _logResponseHeaders(logger, requestId, headers, null);

	private static void LogException(ILogger logger, Exception ex, string requestId, HttpMethod method, Uri? uri, double elapsedMs, string errorMessage)
		=> _logException(logger, requestId, method, uri, elapsedMs, errorMessage, ex);
}
