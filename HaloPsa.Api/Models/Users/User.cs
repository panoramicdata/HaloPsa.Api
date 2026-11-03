using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Users;

/// <summary>
/// Represents a user in the Halo system
/// </summary>
public record User
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
	/// The user's login, which Halo returns as <c>login</c>
	/// </summary>
	[JsonPropertyName("login")]
	public string? Login { get; init; }

	/// <summary>
	/// The user's first name
	/// </summary>
	[JsonPropertyName("firstname")]
	public string? FirstName { get; init; }

	/// <summary>
	/// The user's surname
	/// </summary>
	[JsonPropertyName("surname")]
	public string? Surname { get; init; }

	/// <summary>
	/// The user's second email address
	/// </summary>
	[JsonPropertyName("email2")]
	public string? EmailAddress2 { get; init; }

	/// <summary>
	/// The user's third email address
	/// </summary>
	[JsonPropertyName("email3")]
	public string? EmailAddress3 { get; init; }

	/// <summary>
	/// The ID of the agent linked to this user
	/// </summary>
	/// <remarks>
	/// Halo sends -1 when no agent is linked.
	/// </remarks>
	[JsonPropertyName("linked_agent_id")]
	public int? LinkedAgentId { get; init; }

	/// <summary>
	/// The user's login; the same value as <see cref="Login"/>
	/// </summary>
	[Obsolete("Halo's Users record has no username field; use Login.")]
	[JsonIgnore]
	public string? Username => Login;

	/// <summary>
	/// Whether the user is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The user's site/location
	/// </summary>
	[JsonPropertyName("site")]
	public string? Site { get; init; }

	/// <summary>
	/// The user's department
	/// </summary>
	[JsonPropertyName("department")]
	public string? Department { get; init; }

	/// <summary>
	/// The user's telephone number
	/// </summary>
	[JsonPropertyName("telephone")]
	public string? Telephone { get; init; }

	/// <summary>
	/// The user's mobile number
	/// </summary>
	[JsonPropertyName("mobile")]
	public string? Mobile { get; init; }

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
