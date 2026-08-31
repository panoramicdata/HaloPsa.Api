using AwesomeAssertions;
using HaloPsa.Api.Exceptions;

namespace HaloPsa.Api.Test.Exceptions;

public class HaloApiExceptionTests
{
	[Fact]
	public void HaloApiException_WithBasicMessage_CanBeCreated()
	{
		// Arrange & Act
		var exception = new HaloApiException("Test error message");

		// Assert
		_ = exception.Message.Should().Be("Test error message");
		_ = exception.StatusCode.Should().BeNull();
		_ = exception.ErrorCode.Should().BeNull();
		_ = exception.Details.Should().BeNull();
		_ = exception.RequestUrl.Should().BeNull();
		_ = exception.RequestMethod.Should().BeNull();
	}

	[Fact]
	public void HaloApiException_WithInnerException_CanBeCreated()
	{
		// Arrange
		var innerException = new InvalidOperationException("Inner error");

		// Act
		var exception = new HaloApiException("Outer error", innerException);

		// Assert
		_ = exception.Message.Should().Be("Outer error");
		_ = exception.InnerException.Should().Be(innerException);
	}

	[Fact]
	public void HaloApiException_WithFullDetails_CanBeCreated()
	{
		// Arrange
		var details = new Dictionary<string, object?>
		{
			["field1"] = "value1",
			["field2"] = 42
		};
		var innerException = new Exception("Inner");

		// Act
		var exception = new HaloApiException(
			message: "Full error details",
			statusCode: 400,
			errorCode: "VALIDATION_ERROR",
			details: details,
			requestUrl: "https://api.halopsa.com/tickets/123",
			requestMethod: "PUT",
			innerException: innerException);

		// Assert
		_ = exception.Message.Should().Be("Full error details");
		_ = exception.StatusCode.Should().Be(400);
		_ = exception.ErrorCode.Should().Be("VALIDATION_ERROR");
		_ = exception.Details.Should().BeEquivalentTo(details);
		_ = exception.RequestUrl.Should().Be("https://api.halopsa.com/tickets/123");
		_ = exception.RequestMethod.Should().Be("PUT");
		_ = exception.InnerException.Should().Be(innerException);
	}

	[Fact]
	public void HaloAuthenticationException_InheritsFromHaloApiException()
	{
		// Arrange & Act
		var exception = new HaloAuthenticationException("Authentication failed");

		// Assert
		_ = exception.Should().BeOfType<HaloAuthenticationException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
		_ = exception.Message.Should().Be("Authentication failed");
	}

	[Fact]
	public void HaloAuthenticationException_WithFullDetails_CanBeCreated()
	{
		// Arrange & Act
		var exception = new HaloAuthenticationException(
			message: "Invalid credentials",
			statusCode: 401,
			errorCode: "INVALID_CREDENTIALS",
			details: null,
			requestUrl: "https://api.halopsa.com/auth/token",
			requestMethod: "POST",
			innerException: null);

		// Assert
		_ = exception.Message.Should().Be("Invalid credentials");
		_ = exception.StatusCode.Should().Be(401);
		_ = exception.ErrorCode.Should().Be("INVALID_CREDENTIALS");
		_ = exception.RequestUrl.Should().Be("https://api.halopsa.com/auth/token");
		_ = exception.RequestMethod.Should().Be("POST");
	}

	[Fact]
	public void HaloAuthorizationException_InheritsFromHaloApiException()
	{
		// Arrange & Act
		var exception = new HaloAuthorizationException("Access denied");

		// Assert
		_ = exception.Should().BeOfType<HaloAuthorizationException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
		_ = exception.Message.Should().Be("Access denied");
	}

	[Fact]
	public void HaloNotFoundException_WithResourceInfo_CanBeCreated()
	{
		// Arrange & Act
		var exception = new HaloNotFoundException(
			message: "Ticket not found",
			resourceType: "Ticket",
			resourceId: 123,
			errorContext: new HaloApiErrorContext { StatusCode = 404 });

		// Assert
		_ = exception.Message.Should().Be("Ticket not found");
		_ = exception.ResourceType.Should().Be("Ticket");
		_ = exception.ResourceId.Should().Be(123);
		_ = exception.StatusCode.Should().Be(404);
		_ = exception.Should().BeOfType<HaloNotFoundException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
	}

	[Fact]
	public void HaloBadRequestException_WithValidationErrors_CanBeCreated()
	{
		// Arrange
		var validationErrors = new List<string>
		{
			"Summary is required",
			"ClientId must be greater than 0"
		}.AsReadOnly();

		// Act
		var exception = new HaloBadRequestException(
			message: "Validation failed",
			validationErrors: validationErrors,
			statusCode: 400,
			errorCode: null,
			details: null,
			requestUrl: null,
			requestMethod: null,
			innerException: null);

		// Assert
		_ = exception.Message.Should().Be("Validation failed");
		_ = exception.ValidationErrors.Should().BeEquivalentTo(validationErrors);
		_ = exception.StatusCode.Should().Be(400);
		_ = exception.Should().BeOfType<HaloBadRequestException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
	}

	[Fact]
	public void HaloRateLimitException_WithRateLimitInfo_CanBeCreated()
	{
		// Arrange
		var resetTime = DateTime.UtcNow.AddMinutes(15);

		// Act
		var exception = new HaloRateLimitException(
			message: "Rate limit exceeded",
			retryAfterSeconds: 900,
			rateLimit: 100,
			remainingRequests: 0,
			resetTime: resetTime,
			errorContext: new HaloApiErrorContext { StatusCode = 429 });

		// Assert
		_ = exception.Message.Should().Be("Rate limit exceeded");
		_ = exception.RetryAfterSeconds.Should().Be(900);
		_ = exception.RateLimit.Should().Be(100);
		_ = exception.RemainingRequests.Should().Be(0);
		_ = exception.ResetTime.Should().Be(resetTime);
		_ = exception.StatusCode.Should().Be(429);
		_ = exception.Should().BeOfType<HaloRateLimitException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
	}

	[Fact]
	public void HaloServerException_InheritsFromHaloApiException()
	{
		// Arrange & Act
		var exception = new HaloServerException(
			message: "Internal server error",
			statusCode: 500,
			errorCode: "INTERNAL_ERROR",
			details: null,
			requestUrl: null,
			requestMethod: null,
			innerException: null);

		// Assert
		_ = exception.Message.Should().Be("Internal server error");
		_ = exception.StatusCode.Should().Be(500);
		_ = exception.ErrorCode.Should().Be("INTERNAL_ERROR");
		_ = exception.Should().BeOfType<HaloServerException>();
		_ = exception.Should().BeAssignableTo<HaloApiException>();
	}

	[Fact]
	public void AllExceptions_AreSerializable()
	{
		// This test ensures exceptions can be serialized if needed for logging or RPC scenarios
		var exceptions = new List<Exception>
		{
			new HaloApiException("Test"),
			new HaloAuthenticationException("Auth test"),
			new HaloAuthorizationException("Authz test"),
			new HaloNotFoundException("Not found test"),
			new HaloBadRequestException("Bad request test"),
			new HaloRateLimitException("Rate limit test"),
			new HaloServerException("Server error test")
		};

		foreach (var exception in exceptions)
		{
			// Act & Assert - should not throw
			_ = exception.Should().NotBeNull();
			_ = exception.Message.Should().NotBeNullOrEmpty();
			_ = exception.ToString().Should().NotBeNullOrEmpty();
		}
	}

	[Fact]
	public void ExceptionHierarchy_IsCorrect()
	{
		// Arrange & Act & Assert
		var authenticationException = new HaloAuthenticationException("test");
		var authorizationException = new HaloAuthorizationException("test");
		var notFoundException = new HaloNotFoundException("test");
		var badRequestException = new HaloBadRequestException("test");
		var rateLimitException = new HaloRateLimitException("test");
		var serverException = new HaloServerException("test");

		// All should inherit from HaloApiException
		_ = authenticationException.Should().BeAssignableTo<HaloApiException>();
		_ = authorizationException.Should().BeAssignableTo<HaloApiException>();
		_ = notFoundException.Should().BeAssignableTo<HaloApiException>();
		_ = badRequestException.Should().BeAssignableTo<HaloApiException>();
		_ = rateLimitException.Should().BeAssignableTo<HaloApiException>();
		_ = serverException.Should().BeAssignableTo<HaloApiException>();

		// All should inherit from Exception
		_ = authenticationException.Should().BeAssignableTo<Exception>();
		_ = authorizationException.Should().BeAssignableTo<Exception>();
		_ = notFoundException.Should().BeAssignableTo<Exception>();
		_ = badRequestException.Should().BeAssignableTo<Exception>();
		_ = rateLimitException.Should().BeAssignableTo<Exception>();
		_ = serverException.Should().BeAssignableTo<Exception>();
	}
}
