using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Projects;

/// <summary>
/// Request model for creating a new project
/// </summary>
public record CreateProjectRequest
{
	/// <summary>
	/// The project name (required)
	/// </summary>
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	/// <summary>
	/// The project description
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The client ID this project belongs to
	/// </summary>
	[JsonPropertyName("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// The project start date
	/// </summary>
	[JsonPropertyName("startdate")]
	public string? StartDate { get; init; }

	/// <summary>
	/// The project target date
	/// </summary>
	[JsonPropertyName("targetdate")]
	public string? TargetDate { get; init; }

	/// <summary>
	/// The project status
	/// </summary>
	[JsonPropertyName("status")]
	public string? Status { get; init; }

	/// <summary>
	/// The project manager user ID
	/// </summary>
	[JsonPropertyName("manager_id")]
	public int? ManagerId { get; init; }

	/// <summary>
	/// The project budget
	/// </summary>
	[JsonPropertyName("budget")]
	public decimal? Budget { get; init; }

	/// <summary>
	/// The project percentage complete
	/// </summary>
	[JsonPropertyName("percentcomplete")]
	public decimal? PercentComplete { get; init; }

	/// <summary>
	/// The project site ID
	/// </summary>
	[JsonPropertyName("site_id")]
	public int? SiteId { get; init; }

	/// <summary>
	/// Whether the project is inactive
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }
}

/// <summary>
/// Request model for updating an existing project
/// </summary>
public record UpdateProjectRequest
{
	/// <summary>
	/// The project name
	/// </summary>
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	/// <summary>
	/// The project description
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The client ID this project belongs to
	/// </summary>
	[JsonPropertyName("client_id")]
	public int? ClientId { get; init; }

	/// <summary>
	/// The project start date
	/// </summary>
	[JsonPropertyName("startdate")]
	public string? StartDate { get; init; }

	/// <summary>
	/// The project target date
	/// </summary>
	[JsonPropertyName("targetdate")]
	public string? TargetDate { get; init; }

	/// <summary>
	/// The project completion date
	/// </summary>
	[JsonPropertyName("completeddate")]
	public string? CompletedDate { get; init; }

	/// <summary>
	/// The project status
	/// </summary>
	[JsonPropertyName("status")]
	public string? Status { get; init; }

	/// <summary>
	/// The project manager user ID
	/// </summary>
	[JsonPropertyName("manager_id")]
	public int? ManagerId { get; init; }

	/// <summary>
	/// The project budget
	/// </summary>
	[JsonPropertyName("budget")]
	public decimal? Budget { get; init; }

	/// <summary>
	/// The project percentage complete
	/// </summary>
	[JsonPropertyName("percentcomplete")]
	public decimal? PercentComplete { get; init; }

	/// <summary>
	/// The project site ID
	/// </summary>
	[JsonPropertyName("site_id")]
	public int? SiteId { get; init; }

	/// <summary>
	/// Whether the project is inactive
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool? IsInactive { get; init; }
}

/// <summary>
/// Response for project creation or update operations
/// </summary>
public record CreateProjectResponse
{
	/// <summary>
	/// The created or updated project
	/// </summary>
	public required Project Project { get; init; }

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
/// Response for project update operations
/// </summary>
public record UpdateProjectResponse : CreateProjectResponse;
