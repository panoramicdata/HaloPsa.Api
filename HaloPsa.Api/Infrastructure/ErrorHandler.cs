using HaloPsa.Api.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that converts Refit API exceptions to HaloApiExceptions
/// </summary>
internal sealed class ErrorHandler(ILogger? logger) : DelegatingHandler
{
	private readonly ILogger _logger = logger ?? NullLogger.Instance;

	/// <summary>
	/// Processes HTTP requests and converts any API exceptions to HaloApiExceptions
	/// </summary>
	/// <param name="request">The HTTP request message</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The HTTP response message</returns>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// Every failure here reaches the caller, so it is logged at Debug only and never with the exception (#96)
		try
		{
			var response = await base.SendAsync(request, cancellationToken);
			return response;
		}
		catch (ApiException apiException)
		{
			LogApiException(_logger, apiException.StatusCode, apiException.ReasonPhrase);

			// Convert Refit ApiException to appropriate HaloApiException
			var haloException = ConvertToHaloApiException(apiException, request);
			throw haloException;
		}
		catch (HaloRequestNotSentException)
		{
			LogRequestNotSent(_logger, request.RequestUri);
			throw;
		}
		catch (Exception ex)
		{
			LogRequestFailed(_logger, request.RequestUri, ex);
			throw;
		}
	}

	/// <summary>
	/// Converts a Refit ApiException to the appropriate HaloApiException type
	/// </summary>
	/// <param name="apiException">The Refit API exception</param>
	/// <param name="request">The original HTTP request</param>
	/// <returns>The appropriate HaloApiException</returns>
	private static HaloApiException ConvertToHaloApiException(ApiException apiException, HttpRequestMessage request)
	{
		var statusCode = (int)apiException.StatusCode;
		var requestUrl = request.RequestUri?.ToString();
		var requestMethod = request.Method.Method;

		var (details, validationErrors) = ApiErrorParser.ParseJsonContent(apiException.Content);
		var message = ApiErrorParser.ReadStringBodyMessage(apiException.Content)
			?? apiException.ReasonPhrase
			?? $"API request failed with status {statusCode}";

		var errorContext = new HaloApiErrorContext
		{
			StatusCode = statusCode,
			Details = details,
			RequestUrl = requestUrl,
			RequestMethod = requestMethod,
			InnerException = apiException
		};

		return ApiErrorParser.CreateExceptionForStatusCode(
			statusCode, message, validationErrors,
			requestUrl, apiException.Headers, errorContext);
	}

	private static readonly Action<ILogger, System.Net.HttpStatusCode, string?, Exception?> _logApiException = LoggerMessage.Define<System.Net.HttpStatusCode, string?>(
		LogLevel.Debug, new EventId(1, nameof(LogApiException)),
		"API exception occurred: {StatusCode} {ReasonPhrase}");

	private static readonly Action<ILogger, Uri?, string, Exception?> _logRequestFailed = LoggerMessage.Define<Uri?, string>(
		LogLevel.Debug, new EventId(2, nameof(LogRequestFailed)),
		"HTTP request to {RequestUri} failed: {Reason}");

	private static readonly Action<ILogger, Uri?, Exception?> _logRequestNotSent = LoggerMessage.Define<Uri?>(
		LogLevel.Debug, new EventId(3, nameof(LogRequestNotSent)),
		"Request to {RequestUri} was not sent because the rate limiter gave up");

	private static void LogRequestNotSent(ILogger logger, Uri? requestUri)
		=> _logRequestNotSent(logger, requestUri, null);

	private static void LogApiException(ILogger logger, System.Net.HttpStatusCode statusCode, string? reasonPhrase)
		=> _logApiException(logger, statusCode, reasonPhrase, null);

	private static void LogRequestFailed(ILogger logger, Uri? requestUri, Exception ex)
		=> _logRequestFailed(logger, requestUri, $"{ex.GetType().Name}: {ex.Message}", null);
}
