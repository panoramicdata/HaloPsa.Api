using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Teams;

/// <summary>
/// Represents a team in the Halo system
/// </summary>
public record Team
{
	/// <summary>
	/// The unique identifier for the team
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name of the team
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description of the team
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The color associated with this team
	/// </summary>
	[JsonPropertyName("colour")]
	public string? Color { get; init; }

	/// <summary>
	/// Whether this team is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The parent team ID if this is a sub-team
	/// </summary>
	[JsonPropertyName("parentteam")]
	public int? ParentTeamId { get; init; }

	/// <summary>
	/// The order in which this team appears in lists
	/// </summary>
	[JsonPropertyName("order")]
	public int Order { get; init; }

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
