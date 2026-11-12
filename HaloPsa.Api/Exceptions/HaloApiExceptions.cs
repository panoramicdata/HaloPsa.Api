namespace HaloPsa.Api.Exceptions;

/// <summary>
/// Contains error context information for Halo API exceptions
/// </summary>
public sealed record HaloApiErrorContext
{
	/// <summary>
	/// The HTTP status code associated with the error
	/// </summary>
	public int? StatusCode { get; init; }

	/// <summary>
	/// The error code from the API response
	/// </summary>
	public string? ErrorCode { get; init; }

	/// <summary>
	/// Additional error details from the API
	/// </summary>
	public Dictionary<string, object?>? Details { get; init; }

	/// <summary>
	/// The request URL that caused the error
	/// </summary>
	public string? RequestUrl { get; init; }

	/// <summary>
	/// The request method that caused the error
	/// </summary>
	public string? RequestMethod { get; init; }

	/// <summary>
	/// The exception that is the cause of the current exception
	/// </summary>
	public Exception? InnerException { get; init; }
}

/// <summary>
/// Base exception for all Halo API related errors
/// </summary>
public class HaloApiException : Exception
{
	/// <summary>
	/// The HTTP status code associated with the error
	/// </summary>
	public int? StatusCode { get; }

	/// <summary>
	/// The error code from the API response
	/// </summary>
	public string? ErrorCode { get; }

	/// <summary>
	/// Additional error details from the API
	/// </summary>
	public Dictionary<string, object?>? Details { get; }

	/// <summary>
	/// The request URL that caused the error
	/// </summary>
	public string? RequestUrl { get; }

	/// <summary>
	/// The request method that caused the error
	/// </summary>
	public string? RequestMethod { get; }

	/// <summary>
	/// Initializes a new instance of the HaloApiException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public HaloApiException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloApiException class with a specified error message and inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloApiException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloApiException class with message and status code
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	public HaloApiException(string message, int statusCode) : base(message)
	{
		StatusCode = statusCode;
	}

	/// <summary>
	/// Initializes a new instance of the HaloApiException class with detailed error information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="errorContext">Additional error context information</param>
	public HaloApiException(string message, HaloApiErrorContext errorContext)
		: base(message, errorContext.InnerException)
	{
		StatusCode = errorContext.StatusCode;
		ErrorCode = errorContext.ErrorCode;
		Details = errorContext.Details;
		RequestUrl = errorContext.RequestUrl;
		RequestMethod = errorContext.RequestMethod;
	}

	/// <summary>
	/// Initializes a new instance of the HaloApiException class with detailed error information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	/// <param name="errorCode">The error code from the API response</param>
	/// <param name="details">Additional error details from the API</param>
	/// <param name="requestUrl">The request URL that caused the error</param>
	/// <param name="requestMethod">The request method that caused the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloApiException(
		string message,
		int? statusCode,
		string? errorCode,
		Dictionary<string, object?>? details,
		string? requestUrl,
		string? requestMethod,
		Exception? innerException)
		: base(message, innerException)
	{
		StatusCode = statusCode;
		ErrorCode = errorCode;
		Details = details;
		RequestUrl = requestUrl;
		RequestMethod = requestMethod;
	}
}
