using HaloPsa.Api.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace HaloPsa.Api.Http;

/// <summary>
/// Builds the HttpClient used by <see cref="HaloClient"/>, applying the configured handler chain
/// and default request headers.
/// </summary>
internal static class DefaultHttpClientFactory
{
	/// <summary>
	/// Creates the configured HttpClient with its handler chain and default request headers.
	/// </summary>
	/// <param name="options">Configuration options for the client</param>
	public static HttpClient Create(HaloClientOptions options)
		=> Create(options, () => new HttpClientHandler());

	/// <summary>
	/// Creates the configured HttpClient over transports supplied by <paramref name="createTransport"/>, which is called
	/// once for the main chain and once for the token client.
	/// </summary>
	/// <param name="options">Configuration options for the client</param>
	/// <param name="createTransport">Creates a transport handler</param>
	internal static HttpClient Create(HaloClientOptions options, Func<HttpMessageHandler> createTransport)
	{
		var chain = BuildHandlerChain(options, createTransport);
		var readOnlyHandler = new ReadOnlyHandler(options) { InnerHandler = chain };

		var httpClient = new HttpClient(readOnlyHandler)
		{
			BaseAddress = new Uri(options.EffectiveBaseUrl),
			// With a rate limiter each attempt carries its own timeout, which starts once its slot is acquired
			Timeout = options.RateLimiter is null ? options.RequestTimeout : Timeout.InfiniteTimeSpan
		};

		// Add default headers
		httpClient.DefaultRequestHeaders.Add("User-Agent", options.EffectiveUserAgent);
		foreach (var header in options.DefaultHeaders)
		{
			httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
		}

		return httpClient;
	}

	private static DelegatingHandler BuildHandlerChain(HaloClientOptions options, Func<HttpMessageHandler> createTransport)
	{
		// Handler order matters: authentication sits above the transport and error handling is outermost. The rate limiter, when set, is innermost so every attempt, 401 re-send and token request takes a slot.
		DelegatingHandler chain = new AuthenticationHandler(options, WithRateLimiting(options, createTransport(), isTokenRequest: true))
		{
			InnerHandler = WithRateLimiting(options, createTransport(), isTokenRequest: false)
		};

		if (options.MaxRetryAttempts > 0)
		{
			var retryHandler = new RetryHandler(
				options.MaxRetryAttempts,
				options.RetryDelay,
				options.UseExponentialBackoff,
				options.MaxRetryDelay,
				options.RateLimiter is not null,
				options.Logger)
			{
				InnerHandler = chain
			};
			chain = retryHandler;
		}

		if (options.EnableRequestLogging || options.EnableResponseLogging)
		{
			var loggingHandler = new LoggingHandler(
				options.Logger ?? NullLogger.Instance,
				options.EnableRequestLogging,
				options.EnableResponseLogging)
			{
				InnerHandler = chain
			};
			chain = loggingHandler;
		}

		// Add error handling as the outermost handler (wraps everything)
		chain = new ErrorHandler(options.Logger) { InnerHandler = chain };

		return chain;
	}

	private static HttpMessageHandler WithRateLimiting(HaloClientOptions options, HttpMessageHandler transport, bool isTokenRequest)
		=> options.RateLimiter is null
			? transport
			: new RateLimitHandler(options.RateLimiter, options.RequestTimeout, isTokenRequest) { InnerHandler = transport };
}
