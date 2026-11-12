using HaloPsa.Api.Converters;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Teams;

/// <summary>
/// Response wrapper for team list operations
/// </summary>
[JsonConverter(typeof(TeamsResponseConverter))]
public record TeamsResponse
{
	/// <summary>
	/// The list of teams
	/// </summary>
	[JsonPropertyName("teams")]
	public IReadOnlyList<Team> Teams { get; init; } = [];

	/// <summary>
	/// The total number of teams in the system
	/// </summary>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}

internal sealed class TeamsResponseConverter() : ArrayOrWrapperConverter<TeamsResponse, Team>("teams")
{
	protected override TeamsResponse Create(IReadOnlyList<Team> items, int recordCount)
		=> new() { Teams = items, RecordCount = recordCount };

	protected override IReadOnlyList<Team> GetItems(TeamsResponse value) => value.Teams;

	protected override int GetRecordCount(TeamsResponse value) => value.RecordCount;
}
