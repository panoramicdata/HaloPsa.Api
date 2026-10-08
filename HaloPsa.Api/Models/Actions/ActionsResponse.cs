using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Actions;

/// <summary>
/// Response wrapper for action list operations
/// </summary>
public record ActionsResponse
{
	/// <summary>
	/// The actions returned
	/// </summary>
	[JsonPropertyName("actions")]
	public IReadOnlyList<TicketAction> Actions { get; init; } = [];

	/// <summary>
	/// The total number of matching actions
	/// </summary>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}
