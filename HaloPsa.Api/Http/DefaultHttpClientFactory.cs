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
	{
		var chain = BuildHandlerChain(options);
		var readOnlyHandler = new ReadOnlyHandler(options) { InnerHandler = chain };

		var httpClient = new HttpClient(readOnlyHandler)
		{
			BaseAddress = new Uri(options.EffectiveBaseUrl),
			Timeout = options.RequestTimeout
		};

		// Add default headers
		httpClient.DefaultRequestHeaders.Add("User-Agent", options.EffectiveUserAgent);
		foreach (var header in options.DefaultHeaders)
		{
			httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
		}

		return httpClient;
	}

	private static DelegatingHandler BuildHandlerChain(HaloClientOptions options)
	{
		// Build the handler chain (order matters - authentication should be innermost, error handling outermost)
		DelegatingHandler chain = new AuthenticationHandler(options)
		{
			InnerHandler = new HttpClientHandler()
		};

		if (options.MaxRetryAttempts > 0)
		{
			var retryHandler = new RetryHandler(
				options.MaxRetryAttempts,
				options.RetryDelay,
				options.UseExponentialBackoff,
				options.MaxRetryDelay,
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
}
