using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Teams;

/// <summary>
/// Represents a team in the Halo system
/// </summary>
public record Team : NamedLookupBase
{
	/// <summary>
	/// The parent team ID if this is a sub-team
	/// </summary>
	[JsonPropertyName("parentteam")]
	public int? ParentTeamId { get; init; }

	/// <summary>
	/// Whether this team is a department
	/// </summary>
	[JsonPropertyName("isdepartment")]
	public bool IsDepartment { get; init; }

	/// <summary>
	/// The email address for the team
	/// </summary>
	[JsonPropertyName("emailaddress")]
	public string? EmailAddress { get; init; }

	/// <summary>
	/// The phone number for the team
	/// </summary>
	[JsonPropertyName("phonenumber")]
	public string? PhoneNumber { get; init; }
}
