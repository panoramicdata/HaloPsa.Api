using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Actions;

/// <summary>
/// An action on a ticket: a note, email, status change or other event in its history
/// </summary>
public record TicketAction
{
	/// <summary>
	/// The action id, unique within its ticket
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The ticket this action belongs to
	/// </summary>
	[JsonPropertyName("ticket_id")]
	public int TicketId { get; init; }

	/// <summary>
	/// The action type's name, for example "Note" or "Closed"
	/// </summary>
	[JsonPropertyName("outcome")]
	public string? Outcome { get; init; }

	/// <summary>
	/// The name of whoever performed the action
	/// </summary>
	[JsonPropertyName("who")]
	public string? Who { get; init; }

	/// <summary>
	/// The agent who performed the action, if it was an agent
	/// </summary>
	[JsonPropertyName("who_agentid")]
	public int? WhoAgentId { get; init; }

	/// <summary>
	/// The note text
	/// </summary>
	[JsonPropertyName("note")]
	public string? Note { get; init; }

	/// <summary>
	/// The note as HTML
	/// </summary>
	[JsonPropertyName("note_html")]
	public string? NoteHtml { get; init; }

	/// <summary>
	/// When the action occurred
	/// </summary>
	[JsonPropertyName("datetime")]
	public DateTime? DateTime { get; init; }

	/// <summary>
	/// Hours recorded against the action
	/// </summary>
	[JsonPropertyName("timetaken")]
	public double? TimeTaken { get; init; }

	/// <summary>
	/// Whether the action is hidden from the end user
	/// </summary>
	[JsonPropertyName("hiddenfromuser")]
	public bool HiddenFromUser { get; init; }

	/// <summary>
	/// Whether the action is flagged as important
	/// </summary>
	[JsonPropertyName("important")]
	public bool Important { get; init; }

	/// <summary>
	/// The number of attachments on the action
	/// </summary>
	[JsonPropertyName("attachment_count")]
	public int? AttachmentCount { get; init; }

	/// <summary>
	/// Every field Halo returned that this record does not model
	/// </summary>
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
