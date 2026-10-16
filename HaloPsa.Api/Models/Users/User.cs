using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Users;

/// <summary>
/// Read-only base for <see cref="User"/> holding its optional string properties.
/// </summary>
public abstract record UserBase(
	string? userSite,
	string? userDepartment,
	string? userTelephone,
	string? userMobile)
{
	/// <summary>
	/// The user's site/location
	/// </summary>
	[JsonPropertyName("site")]
	public string? Site => userSite;

	/// <summary>
	/// The user's department
	/// </summary>
	[JsonPropertyName("department")]
	public string? Department => userDepartment;

	/// <summary>
	/// The user's telephone number
	/// </summary>
	[JsonPropertyName("telephone")]
	public string? Telephone => userTelephone;

	/// <summary>
	/// The user's mobile number
	/// </summary>
	[JsonPropertyName("mobile")]
	public string? Mobile => userMobile;
}

/// <summary>
/// Represents a user in the Halo system
/// </summary>
public record User(UserBase userBase) : UserBase(userBase)
{
	/// <summary>
	/// The unique identifier for the user
	/// </summary>
	[JsonPropertyName("id")]
	public required int Id { get; init; }

	/// <summary>
	/// The user's name
	/// </summary>
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	/// <summary>
	/// The user's email address
	/// </summary>
	[JsonPropertyName("emailaddress")]
	public string? EmailAddress { get; init; }

	/// <summary>
	/// The user's username/login
	/// </summary>
	[JsonPropertyName("username")]
	public string? Username { get; init; }

	/// <summary>
	/// Whether the user is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// Whether this user is an agent
	/// </summary>
	[JsonPropertyName("isagent")]
	public bool IsAgent { get; init; }

	/// <summary>
	/// Whether this user is an important user
	/// </summary>
	[JsonPropertyName("isimportantuser")]
	public bool IsImportantUser { get; init; }

	/// <summary>
	/// The client ID this user belongs to
	/// </summary>
	[JsonPropertyName("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// The client name this user belongs to
	/// </summary>
	[JsonPropertyName("client_name")]
	public string? ClientName { get; init; }
}
