using HaloPsa.Api.Converters;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Statuses;

/// <summary>
/// Response wrapper for status list operations
/// </summary>
[JsonConverter(typeof(StatusesResponseConverter))]
public record StatusesResponse
{
	/// <summary>
	/// The list of statuses
	/// </summary>
	[JsonPropertyName("statuses")]
	public IReadOnlyList<Status> Statuses { get; init; } = [];

	/// <summary>
	/// The total number of statuses in the system
	/// </summary>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}

internal sealed class StatusesResponseConverter() : ArrayOrWrapperConverter<StatusesResponse, Status>("statuses")
{
	protected override StatusesResponse Create(IReadOnlyList<Status> items, int recordCount)
		=> new() { Statuses = items, RecordCount = recordCount };

	protected override IReadOnlyList<Status> GetItems(StatusesResponse value) => value.Statuses;

	protected override int GetRecordCount(StatusesResponse value) => value.RecordCount;
}
