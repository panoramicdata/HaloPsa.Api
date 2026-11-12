namespace HaloPsa.Api.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found (404 Not Found)
/// </summary>
public class HaloNotFoundException : HaloApiException
{
	/// <summary>
	/// The type of resource that was not found
	/// </summary>
	public string? ResourceType { get; }

	/// <summary>
	/// The ID of the resource that was not found
	/// </summary>
	public object? ResourceId { get; }

	/// <summary>
	/// Initializes a new instance of the HaloNotFoundException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public HaloNotFoundException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloNotFoundException class with a specified error message and inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HaloNotFoundException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloNotFoundException class with message and status code
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="statusCode">The HTTP status code associated with the error</param>
	public HaloNotFoundException(string message, int statusCode) : base(message, statusCode)
	{
	}

	/// <summary>
	/// Initializes a new instance of the HaloNotFoundException class with resource information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="resourceType">The type of resource that was not found</param>
	/// <param name="resourceId">The ID of the resource that was not found</param>
	public HaloNotFoundException(string message, string? resourceType, object? resourceId) : base(message)
	{
		ResourceType = resourceType;
		ResourceId = resourceId;
	}

	/// <summary>
	/// Initializes a new instance of the HaloNotFoundException class with detailed error information
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="resourceType">The type of resource that was not found</param>
	/// <param name="resourceId">The ID of the resource that was not found</param>
	/// <param name="errorContext">Additional error context information</param>
	public HaloNotFoundException(
		string message,
		string? resourceType,
		object? resourceId,
		HaloApiErrorContext errorContext)
		: base(message, errorContext)
	{
		ResourceType = resourceType;
		ResourceId = resourceId;
	}
}
