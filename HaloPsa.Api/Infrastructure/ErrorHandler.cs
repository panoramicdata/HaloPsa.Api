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
		try
		{
			var response = await base.SendAsync(request, cancellationToken);
			return response;
		}
		catch (ApiException apiException)
		{
			LogApiException(_logger, apiException, apiException.StatusCode, apiException.ReasonPhrase);

			// Convert Refit ApiException to appropriate HaloApiException
			var haloException = ConvertToHaloApiException(apiException, request);
			throw haloException;
		}
		catch (Exception ex)
		{
			LogUnexpectedError(_logger, ex, request.RequestUri);
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
		var message = apiException.ReasonPhrase ?? $"API request failed with status {statusCode}";
		var requestUrl = request.RequestUri?.ToString();
		var requestMethod = request.Method.Method;

		var (details, validationErrors) = ApiErrorParser.ParseJsonContent(apiException.Content);

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
		LogLevel.Error, new EventId(1, nameof(LogApiException)),
		"API exception occurred: {StatusCode} {ReasonPhrase}");

	private static readonly Action<ILogger, Uri?, Exception?> _logUnexpectedError = LoggerMessage.Define<Uri?>(
		LogLevel.Error, new EventId(2, nameof(LogUnexpectedError)),
		"Unexpected error occurred during HTTP request to {RequestUri}");

	private static void LogApiException(ILogger logger, Exception ex, System.Net.HttpStatusCode statusCode, string? reasonPhrase)
		=> _logApiException(logger, statusCode, reasonPhrase, ex);

	private static void LogUnexpectedError(ILogger logger, Exception ex, Uri? requestUri)
		=> _logUnexpectedError(logger, requestUri, ex);
}
