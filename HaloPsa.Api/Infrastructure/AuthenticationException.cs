namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Exception thrown when authentication with the Halo API fails
/// </summary>
public sealed class AuthenticationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the AuthenticationException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public AuthenticationException(string message) : base(message) { }

	/// <summary>
	/// Initializes a new instance of the AuthenticationException class with a specified error message and a reference to the inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
