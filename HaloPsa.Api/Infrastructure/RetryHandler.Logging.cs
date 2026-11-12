using Microsoft.Extensions.Logging;
using System.Net;

namespace HaloPsa.Api.Infrastructure;

internal sealed partial class RetryHandler
{
	private void LogSuccessAfterRetry(HttpRequestMessage request, HttpResponseMessage response, int attempt)
	{
		if (attempt > 0 && response.IsSuccessStatusCode && _logger?.IsEnabled(LogLevel.Information) == true)
		{
			LogSuccessOnRetry(_logger, attempt + 1, request.Method, request.RequestUri);
		}
	}

	private void LogRetryAttempt(HttpRequestMessage request, int attempt, HttpStatusCode statusCode)
	{
		if (_logger?.IsEnabled(LogLevel.Warning) == true)
		{
			LogRetryAttemptStatus(_logger, attempt + 1, CalculateDelay(attempt).TotalMilliseconds, request.Method, request.RequestUri, statusCode);
		}
	}

	private void LogFinalFailure(HttpRequestMessage request, Exception ex)
	{
		if (_logger?.IsEnabled(LogLevel.Error) == true)
		{
			LogFinalFailureException(_logger, ex, _maxRetryAttempts + 1, request.Method, request.RequestUri);
		}
	}

	private void LogRetryAfterException(HttpRequestMessage request, int attempt, Exception ex)
	{
		if (_logger?.IsEnabled(LogLevel.Warning) == true)
		{
			LogRetryAfterException(_logger, ex, attempt + 1, CalculateDelay(attempt).TotalMilliseconds, request.Method, request.RequestUri);
		}
	}

	private static readonly Action<ILogger, int, HttpMethod, Uri?, Exception?> _logSuccessOnRetry = LoggerMessage.Define<int, HttpMethod, Uri?>(
		LogLevel.Information, new EventId(1, nameof(LogSuccessOnRetry)),
		"HTTP request succeeded on attempt {Attempt} for {Method} {Uri}");

	private static readonly Action<ILogger, int, HttpMethod, Uri?, HttpStatusCode, Exception?> _logFinalFailureStatus = LoggerMessage.Define<int, HttpMethod, Uri?, HttpStatusCode>(
		LogLevel.Warning, new EventId(2, nameof(LogFinalFailureStatus)),
		"HTTP request failed after {MaxAttempts} attempts for {Method} {Uri} with status {StatusCode}");

	private static readonly Action<ILogger, int, double, HttpMethod, Uri?, HttpStatusCode, Exception?> _logRetryAttemptStatus = LoggerMessage.Define<int, double, HttpMethod, Uri?, HttpStatusCode>(
		LogLevel.Warning, new EventId(3, nameof(LogRetryAttemptStatus)),
		"HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri} (Status: {StatusCode})");

	private static readonly Action<ILogger, int, HttpMethod, Uri?, Exception?> _logFinalFailureException = LoggerMessage.Define<int, HttpMethod, Uri?>(
		LogLevel.Error, new EventId(4, nameof(LogFinalFailureException)),
		"HTTP request failed after {MaxAttempts} attempts for {Method} {Uri}");

	private static readonly Action<ILogger, int, double, HttpMethod, Uri?, Exception?> _logRetryAfterException = LoggerMessage.Define<int, double, HttpMethod, Uri?>(
		LogLevel.Warning, new EventId(5, nameof(LogRetryAfterException)),
		"HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri}");

	private static void LogSuccessOnRetry(ILogger logger, int attempt, HttpMethod method, Uri? uri)
		=> _logSuccessOnRetry(logger, attempt, method, uri, null);

	private static void LogFinalFailureStatus(ILogger logger, int maxAttempts, HttpMethod method, Uri? uri, HttpStatusCode statusCode)
		=> _logFinalFailureStatus(logger, maxAttempts, method, uri, statusCode, null);

	private static void LogRetryAttemptStatus(ILogger logger, int attempt, double delay, HttpMethod method, Uri? uri, HttpStatusCode statusCode)
		=> _logRetryAttemptStatus(logger, attempt, delay, method, uri, statusCode, null);

	private static void LogFinalFailureException(ILogger logger, Exception ex, int maxAttempts, HttpMethod method, Uri? uri)
		=> _logFinalFailureException(logger, maxAttempts, method, uri, ex);

	private static void LogRetryAfterException(ILogger logger, Exception ex, int attempt, double delay, HttpMethod method, Uri? uri)
		=> _logRetryAfterException(logger, attempt, delay, method, uri, ex);
}
