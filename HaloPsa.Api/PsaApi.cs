using HaloPsa.Api.Exceptions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using Refit;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace HaloPsa.Api;

/// <summary>
/// Implementation of PSA API module
/// </summary>
internal sealed class PsaApi(HttpClient _httpClient, bool? readOnly = null) : IPsaApi
{
	private static readonly RefitSettings _refitSettings = new()
	{
		ExceptionFactory = ConvertApiExceptionToHaloApiException
	};

	public TicketsApiWrapper Tickets { get; } = new Lazy<TicketsApiWrapper>(() => new TicketsApiWrapper(RestService.For<ITicketsApi>(_httpClient, _refitSettings), readOnly)).Value;
	public TicketTypesApiWrapper TicketTypes { get; } = new Lazy<TicketTypesApiWrapper>(() => new TicketTypesApiWrapper(RestService.For<ITicketTypesRefitApi>(_httpClient, _refitSettings))).Value;
	public UsersApiWrapper Users { get; } = new Lazy<UsersApiWrapper>(() => new UsersApiWrapper(RestService.For<IUsersRefitApi>(_httpClient, _refitSettings))).Value;
	public ClientsApiWrapper Clients { get; } = new Lazy<ClientsApiWrapper>(() => new ClientsApiWrapper(RestService.For<IClientsRefitApi>(_httpClient, _refitSettings))).Value;
	public AssetsApiWrapper Assets { get; } = new Lazy<AssetsApiWrapper>(() => new AssetsApiWrapper(RestService.For<IAssetsRefitApi>(_httpClient, _refitSettings))).Value;
	public ProjectsApiWrapper Projects { get; } = new Lazy<ProjectsApiWrapper>(() => new ProjectsApiWrapper(RestService.For<IProjectsRefitApi>(_httpClient, _refitSettings))).Value;
	public StatusesApiWrapper Statuses { get; } = new Lazy<StatusesApiWrapper>(() => new StatusesApiWrapper(RestService.For<IStatusesApi>(_httpClient, _refitSettings))).Value;

	/// <summary>
	/// Converts Refit ApiExceptions to appropriate HaloApiExceptions
	/// </summary>
	/// <param name="httpResponseMessage">The HTTP response message</param>
	/// <returns>The appropriate HaloApiException or null if no exception should be thrown</returns>
	private static async ValueTask<Exception?> ConvertApiExceptionToHaloApiException(HttpResponseMessage httpResponseMessage)
	{
		if (httpResponseMessage.IsSuccessStatusCode)
		{
			return null;
		}

		var statusCode = (int)httpResponseMessage.StatusCode;
		var message = httpResponseMessage.ReasonPhrase ?? $"API request failed with status {statusCode}";
		var requestUrl = httpResponseMessage.RequestMessage?.RequestUri?.ToString();
		var requestMethod = httpResponseMessage.RequestMessage?.Method.Method;

		var (details, validationErrors) = await ParseResponseContentAsync(httpResponseMessage);

		var errorContext = new HaloApiErrorContext
		{
			StatusCode = statusCode,
			Details = details,
			RequestUrl = requestUrl,
			RequestMethod = requestMethod
		};

		return CreateExceptionForStatusCode(
			statusCode, message, validationErrors,
			requestUrl, httpResponseMessage, errorContext);
	}

	/// <summary>
	/// Parses error details and validation errors from HTTP response content
	/// </summary>
	private static async Task<(Dictionary<string, object?>? Details, ReadOnlyCollection<string>? ValidationErrors)> ParseResponseContentAsync(
		HttpResponseMessage httpResponseMessage)
	{
		string? content = null;
		try
		{
			content = await httpResponseMessage.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(content))
			{
				return (null, null);
			}

			var jsonDoc = JsonDocument.Parse(content);
			var details = ExtractErrorDetails(jsonDoc.RootElement);
			var validationErrors = ExtractValidationErrors(jsonDoc.RootElement);
			return (details, validationErrors);
		}
		catch (JsonException)
		{
			return (new Dictionary<string, object?> { ["rawContent"] = content }, null);
		}
	}

	/// <summary>
	/// Extracts validation errors from a JSON element's "errors" property
	/// </summary>
	private static ReadOnlyCollection<string>? ExtractValidationErrors(JsonElement rootElement)
	{
		if (!rootElement.TryGetProperty("errors", out var errorsElement))
		{
			return null;
		}

		if (errorsElement.ValueKind != JsonValueKind.Array)
		{
			return null;
		}

		var errorsList = new List<string>();
		foreach (var error in errorsElement.EnumerateArray())
		{
			if (error.ValueKind == JsonValueKind.String)
			{
				errorsList.Add(error.GetString() ?? string.Empty);
			}
		}

		return errorsList.AsReadOnly();
	}

	/// <summary>
	/// Creates the appropriate HaloApiException subclass based on HTTP status code
	/// </summary>
	private static HaloApiException CreateExceptionForStatusCode(
		int statusCode,
		string message,
		ReadOnlyCollection<string>? validationErrors,
		string? requestUrl,
		HttpResponseMessage httpResponseMessage,
		HaloApiErrorContext errorContext)
		=> statusCode switch
		{
			400 => new HaloBadRequestException($"Bad request: {message}", validationErrors, errorContext),
			401 => new HaloAuthenticationException($"Authentication failed: {message}", errorContext),
			403 => new HaloAuthorizationException($"Authorization failed: {message}", errorContext),
			404 => new HaloNotFoundException($"Resource not found: {message}",
				ExtractResourceTypeFromUrl(requestUrl), ExtractResourceIdFromUrl(requestUrl), errorContext),
			429 => new HaloRateLimitException($"Rate limit exceeded: {message}",
				ExtractRetryAfterSeconds(httpResponseMessage), null, null, null, errorContext),
			>= 500 => new HaloServerException($"Server error: {message}", errorContext),
			_ => new HaloApiException($"API error: {message}", errorContext)
		};

	/// <summary>
	/// Extracts error details from JSON response
	/// </summary>
	/// <param name="element">The JSON element to extract from</param>
	/// <returns>Dictionary of error details</returns>
	private static Dictionary<string, object?> ExtractErrorDetails(JsonElement element)
	{
		var details = new Dictionary<string, object?>();

		foreach (var property in element.EnumerateObject())
		{
			details[property.Name] = property.Value.ValueKind switch
			{
				JsonValueKind.String => property.Value.GetString(),
				JsonValueKind.Number => property.Value.TryGetInt32(out var intVal) ? intVal : property.Value.GetDouble(),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				_ => property.Value.GetRawText()
			};
		}

		return details;
	}

	/// <summary>
	/// Extracts resource type from URL (e.g., "Tickets", "Users", "Clients")
	/// </summary>
	/// <param name="url">The request URL</param>
	/// <returns>The resource type or null if not found</returns>
	private static string? ExtractResourceTypeFromUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		var uri = new Uri(url);
		var segments = uri.Segments;

		// Look for /api/{resourceType} pattern
		for (var i = 0; i < segments.Length - 1; i++)
		{
			if (segments[i].Equals("api/", StringComparison.OrdinalIgnoreCase))
			{
				var resourceSegment = segments[i + 1].TrimEnd('/');
				return resourceSegment;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts resource ID from URL (e.g., the ID in /api/Tickets/123)
	/// </summary>
	/// <param name="url">The request URL</param>
	/// <returns>The resource ID or null if not found</returns>
	private static object? ExtractResourceIdFromUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		var uri = new Uri(url);
		var segments = uri.Segments;

		// Look for /api/{resourceType}/{id} pattern
		for (var i = 0; i < segments.Length - 2; i++)
		{
			if (segments[i].Equals("api/", StringComparison.OrdinalIgnoreCase))
			{
				var idSegment = segments[i + 2].TrimEnd('/');
				return int.TryParse(idSegment, out var intId) ? intId : idSegment;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts Retry-After header value in seconds
	/// </summary>
	/// <param name="httpResponseMessage">The HTTP response message</param>
	/// <returns>Retry-After value in seconds or null</returns>
	private static int? ExtractRetryAfterSeconds(HttpResponseMessage httpResponseMessage)
	{
		if (httpResponseMessage.Headers?.TryGetValues("Retry-After", out var values) == true)
		{
			var retryAfter = values.FirstOrDefault();
			if (int.TryParse(retryAfter, out var seconds))
			{
				return seconds;
			}
		}

		return null;
	}
}
