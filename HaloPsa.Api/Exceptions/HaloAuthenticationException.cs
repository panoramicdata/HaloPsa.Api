namespace HaloPsa.Api.Exceptions;

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public class HaloAuthenticationException : HaloApiException
{
	/// <summary>
	/// Initializes a new instance of the HaloAuthenticationException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public HaloAuthenticationException(string message) : this(message, new HaloApiErrorContext())
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloAuthenticationException class with a specified error message and inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloAuthenticationException(string message, Exception innerException)
		: this(message, new HaloApiErrorContext { InnerException = innerException })
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloAuthenticationException class with message and status code
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	public HaloAuthenticationException(string message, int statusCode)
		: this(message, new HaloApiErrorContext { StatusCode = statusCode })
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloAuthenticationException class with detailed error information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="errorContext">Additional error context information</param>
	public HaloAuthenticationException(string message, HaloApiErrorContext errorContext) : base(message, errorContext)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloAuthenticationException class with detailed error information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	/// <param name="errorCode">The error code from the API response</param>
	/// <param name="details">Additional error details from the API</param>
	/// <param name="requestUrl">The request URL that caused the error</param>
	/// <param name="requestMethod">The request method that caused the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloAuthenticationException(
		string message,
		int? statusCode,
		string? errorCode,
		Dictionary<string, object?>? details,
		string? requestUrl,
		string? requestMethod,
		Exception? innerException)
		: this(message, ContextOf(statusCode, errorCode, details, requestUrl, requestMethod, innerException))
	{
	}

	private static HaloApiErrorContext ContextOf(
		int? statusCode,
		string? errorCode,
		Dictionary<string, object?>? details,
		string? requestUrl,
		string? requestMethod,
		Exception? innerException)
		=> new()
		{
			StatusCode = statusCode,
			ErrorCode = errorCode,
			Details = details,
			RequestUrl = requestUrl,
			RequestMethod = requestMethod,
			InnerException = innerException
		};
}
