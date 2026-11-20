namespace HaloPsa.Api.Exceptions;

/// <summary>
/// Exception thrown by an <see cref="RateLimiting.IRateLimiter"/> to give up on a request before it is sent
/// </summary>
/// <remarks>
/// It is never retried, and a rate limiter that throws it gets no <see cref="RateLimiting.IRateLimiter.OnResponse"/> or
/// <see cref="RateLimiting.IRateLimiter.OnSendFailed"/> call for that attempt. A call through <c>Psa</c> receives it as is; it is not wrapped in Refit's <c>ApiRequestException</c>.
/// </remarks>
public sealed class HaloRequestNotSentException : Exception
{
	/// <summary>
	/// Initializes a new instance of the HaloRequestNotSentException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public HaloRequestNotSentException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloRequestNotSentException class with a specified error message and inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloRequestNotSentException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
