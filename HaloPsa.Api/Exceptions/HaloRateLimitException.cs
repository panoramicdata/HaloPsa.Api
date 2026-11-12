namespace HaloPsa.Api.Exceptions;

/// <summary>
/// Exception thrown when rate limiting is enforced (429 Too Many Requests)
/// </summary>
public class HaloRateLimitException : HaloApiException
{
	/// <summary>
	/// Number of seconds to wait before retrying
	/// </summary>
	public int? RetryAfterSeconds { get; }

	/// <summary>
	/// The rate limit that was exceeded
	/// </summary>
	public int? RateLimit { get; }

	/// <summary>
	/// Remaining requests in the current rate limit window
	/// </summary>
	public int? RemainingRequests { get; }

	/// <summary>
	/// When the rate limit window resets
	/// </summary>
	public DateTime? ResetTime { get; }

	/// <summary>
	/// Initializes a new instance of the HaloRateLimitException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public HaloRateLimitException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloRateLimitException class with a specified error message and inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloRateLimitException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloRateLimitException class with message and status code
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	public HaloRateLimitException(string message, int statusCode) : base(message, statusCode)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloRateLimitException class with rate limit information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="retryAfterSeconds">Number of seconds to wait before retrying</param>
	public HaloRateLimitException(string message, int? retryAfterSeconds) : base(message)
	{
		RetryAfterSeconds = retryAfterSeconds;
	}

	/// <summary>
	/// Initializes a new instance of the HaloRateLimitException class with detailed rate limit information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="retryAfterSeconds">Number of seconds to wait before retrying</param>
	/// <param name="rateLimit">The rate limit that was exceeded</param>
	/// <param name="remainingRequests">Remaining requests in the current rate limit window</param>
	/// <param name="resetTime">When the rate limit window resets</param>
	/// <param name="errorContext">Additional error context information</param>
	public HaloRateLimitException(
		string message,
		int? retryAfterSeconds,
		int? rateLimit,
		int? remainingRequests,
		DateTime? resetTime,
		HaloApiErrorContext errorContext)
		: base(message, errorContext)
	{
		RetryAfterSeconds = retryAfterSeconds;
		RateLimit = rateLimit;
		RemainingRequests = remainingRequests;
		ResetTime = resetTime;
	}
}
