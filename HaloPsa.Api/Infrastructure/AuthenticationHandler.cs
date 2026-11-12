using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that manages OAuth2 authentication for Halo API requests using client credentials flow
/// </summary>
internal sealed partial class AuthenticationHandler : DelegatingHandler
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly HaloClientOptions _options;
	private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
	private readonly HttpClient _authHttpClient; // Separate client for auth requests
	private string? _accessToken;
	private DateTime _tokenExpiry = DateTime.MinValue;

	public AuthenticationHandler(HaloClientOptions options)
		: this(options, new HttpClientHandler())
	{
	}

	internal AuthenticationHandler(HaloClientOptions options, HttpMessageHandler tokenHandler)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));

		// Create a separate HttpClient for authentication requests
		// This avoids circular dependencies with the main client
		_authHttpClient = new HttpClient(tokenHandler)
		{
			BaseAddress = new Uri(_options.EffectiveBaseUrl),
			Timeout = _options.RequestTimeout
		};
		_authHttpClient.DefaultRequestHeaders.Add("User-Agent", _options.EffectiveUserAgent);
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// Don't intercept auth requests to avoid infinite loops
		if (request.RequestUri?.PathAndQuery.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) == true)
		{
			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}

		// Ensure we have a valid access token
		await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

		// Add the authorization header
		if (!string.IsNullOrEmpty(_accessToken))
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
		}

		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		// If we get a 401, try to refresh the token once
		if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_accessToken))
		{
			if (_options.Logger != null)
			{
				LogUnauthorizedRefresh(_options.Logger);
			}

			// Clear the current token and get a new one
			_accessToken = null;
			_tokenExpiry = DateTime.MinValue;

			await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

			// Create a new request message for retry (HttpRequestMessage can only be sent once)
			if (!string.IsNullOrEmpty(_accessToken))
			{
				var retryRequest = await HttpRequestCloner.CloneAsync(request);
				retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

				response.Dispose(); // Dispose the 401 response
				response = await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
			}
		}

		return response;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_tokenSemaphore.Dispose();
			_authHttpClient.Dispose();
		}

		base.Dispose(disposing);
	}

	private static readonly Action<ILogger, Exception?> _logUnauthorizedRefresh = LoggerMessage.Define(
		LogLevel.Warning, new EventId(1, nameof(LogUnauthorizedRefresh)),
		"Received 401 Unauthorized, attempting to refresh token");

	private static readonly Action<ILogger, Exception?> _logRefreshingToken = LoggerMessage.Define(
		LogLevel.Debug, new EventId(2, nameof(LogRefreshingToken)),
		"Refreshing Halo API access token using client credentials");

	private static readonly Action<ILogger, DateTime, Exception?> _logTokenRefreshed = LoggerMessage.Define<DateTime>(
		LogLevel.Debug, new EventId(3, nameof(LogTokenRefreshed)),
		"Successfully refreshed Halo API access token, expires at {Expiry}");

	private static readonly Action<ILogger, Exception?> _logTokenRefreshFailed = LoggerMessage.Define(
		LogLevel.Error, new EventId(4, nameof(LogTokenRefreshFailed)),
		"Failed to refresh Halo API access token");

	private static readonly Action<ILogger, System.Net.HttpStatusCode, string, Exception?> _logAuthFailed = LoggerMessage.Define<System.Net.HttpStatusCode, string>(
		LogLevel.Error, new EventId(5, nameof(LogAuthFailed)),
		"Authentication failed: {StatusCode} - {Content}");

	private static readonly Action<ILogger, string, Exception?> _logTokenResponse = LoggerMessage.Define<string>(
		LogLevel.Debug, new EventId(6, nameof(LogTokenResponse)),
		"Token response: {Response}");

	private static void LogUnauthorizedRefresh(ILogger logger)
		=> _logUnauthorizedRefresh(logger, null);

	private static void LogRefreshingToken(ILogger logger)
		=> _logRefreshingToken(logger, null);

	private static void LogTokenRefreshed(ILogger logger, DateTime expiry)
		=> _logTokenRefreshed(logger, expiry, null);

	private static void LogTokenRefreshFailed(ILogger logger, Exception ex)
		=> _logTokenRefreshFailed(logger, ex);

	private static void LogAuthFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string content)
		=> _logAuthFailed(logger, statusCode, content, null);

	private static void LogTokenResponse(ILogger logger, string response)
		=> _logTokenResponse(logger, response, null);
}
