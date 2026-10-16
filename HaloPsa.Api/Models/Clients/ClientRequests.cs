using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Clients;

/// <summary>
/// Request model for creating a new client
/// </summary>
public record CreateClientRequest
{
	/// <summary>
	/// The client name (required)
	/// </summary>
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	/// <summary>
	/// The client website
	/// </summary>
	[JsonPropertyName("website")]
	public string? Website { get; init; }

	/// <summary>
	/// The client's main telephone number
	/// </summary>
	[JsonPropertyName("telephone")]
	public string? Telephone { get; init; }

	/// <summary>
	/// The client's fax number
	/// </summary>
	[JsonPropertyName("fax")]
	public string? Fax { get; init; }

	/// <summary>
	/// The client's address
	/// </summary>
	[JsonPropertyName("address")]
	public string? Address { get; init; }

	/// <summary>
	/// The client's city
	/// </summary>
	[JsonPropertyName("city")]
	public string? City { get; init; }

	/// <summary>
	/// The client's postal code
	/// </summary>
	[JsonPropertyName("postcode")]
	public string? PostCode { get; init; }

	/// <summary>
	/// The client's country
	/// </summary>
	[JsonPropertyName("country")]
	public string? Country { get; init; }

	/// <summary>
	/// Additional notes about the client
	/// </summary>
	[JsonPropertyName("notes")]
	public string? Notes { get; init; }

	/// <summary>
	/// Whether the client is inactive
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }
}

/// <summary>
/// Request model for updating an existing client
/// </summary>
public record UpdateClientRequest
{
	/// <summary>
	/// The client name
	/// </summary>
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	/// <summary>
	/// The client website
	/// </summary>
	[JsonPropertyName("website")]
	public string? Website { get; init; }

	/// <summary>
	/// The client's main telephone number
	/// </summary>
	[JsonPropertyName("telephone")]
	public string? Telephone { get; init; }

	/// <summary>
	/// The client's fax number
	/// </summary>
	[JsonPropertyName("fax")]
	public string? Fax { get; init; }

	/// <summary>
	/// The client's address
	/// </summary>
	[JsonPropertyName("address")]
	public string? Address { get; init; }

	/// <summary>
	/// The client's city
	/// </summary>
	[JsonPropertyName("city")]
	public string? City { get; init; }

	/// <summary>
	/// The client's postal code
	/// </summary>
	[JsonPropertyName("postcode")]
	public string? PostCode { get; init; }

	/// <summary>
	/// The client's country
	/// </summary>
	[JsonPropertyName("country")]
	public string? Country { get; init; }

	/// <summary>
	/// Additional notes about the client
	/// </summary>
	[JsonPropertyName("notes")]
	public string? Notes { get; init; }

	/// <summary>
	/// Whether the client is inactive
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool? IsInactive { get; init; }
}

/// <summary>
/// Base response for client operations
/// </summary>
public record ClientOperationResponse
{
	/// <summary>
	/// The client
	/// </summary>
	public required Client Client { get; init; }

	/// <summary>
	/// Whether the operation was successful
	/// </summary>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Any messages or warnings from the operation
	/// </summary>
	public IReadOnlyList<string>? Messages { get; init; }
}

/// <summary>
/// Response for client creation operations
/// </summary>
public record CreateClientResponse : ClientOperationResponse
{
}

/// <summary>
/// Response for client update operations
/// </summary>
public record UpdateClientResponse : ClientOperationResponse
{
}
