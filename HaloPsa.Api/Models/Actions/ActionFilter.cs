using Refit;

namespace HaloPsa.Api.Models.Actions;

/// <summary>
/// Filter options for action queries, sent under the parameter names in Halo's API specification
/// </summary>
public record ActionFilter
{
	/// <summary>
	/// The ticket to get actions for
	/// </summary>
	[AliasAs("ticket_id")]
	public int? TicketId { get; init; }

	/// <summary>
	/// Number of actions to return
	/// </summary>
	[AliasAs("count")]
	public int? Count { get; init; }

	/// <summary>
	/// Exclude system actions
	/// </summary>
	[AliasAs("excludesys")]
	public bool? ExcludeSystem { get; init; }

	/// <summary>
	/// Only return public actions
	/// </summary>
	[AliasAs("excludeprivate")]
	public bool? ExcludePrivate { get; init; }

	/// <summary>
	/// Only return actions performed by agents
	/// </summary>
	[AliasAs("agentonly")]
	public bool? AgentOnly { get; init; }

	/// <summary>
	/// Only return the agent-to-end-user conversation
	/// </summary>
	[AliasAs("conversationonly")]
	public bool? ConversationOnly { get; init; }

	/// <summary>
	/// Only return important actions
	/// </summary>
	[AliasAs("importantonly")]
	public bool? ImportantOnly { get; init; }

	/// <summary>
	/// Include attachment details in the response
	/// </summary>
	[AliasAs("includeattachments")]
	public bool? IncludeAttachments { get; init; }
}
