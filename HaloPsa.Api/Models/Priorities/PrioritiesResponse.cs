using HaloPsa.Api.Converters;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Priorities;

/// <summary>
/// Response wrapper for priority list operations
/// </summary>
[JsonConverter(typeof(PrioritiesResponseConverter))]
public record PrioritiesResponse
{
	/// <summary>
	/// The list of priorities, one record per priority per SLA
	/// </summary>
	[JsonPropertyName("priorities")]
	public IReadOnlyList<Priority> Priorities { get; init; } = [];

	/// <summary>
	/// The total number of priorities in the system
	/// </summary>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}

internal sealed class PrioritiesResponseConverter() : ArrayOrWrapperConverter<PrioritiesResponse, Priority>("priorities")
{
	protected override PrioritiesResponse Create(IReadOnlyList<Priority> items, int recordCount)
		=> new() { Priorities = items, RecordCount = recordCount };

	protected override IReadOnlyList<Priority> GetItems(PrioritiesResponse value) => value.Priorities;

	protected override int GetRecordCount(PrioritiesResponse value) => value.RecordCount;
}
