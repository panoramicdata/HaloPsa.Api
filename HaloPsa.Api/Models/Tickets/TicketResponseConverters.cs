using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Tickets;

// Halo answers a write with a bare ticket, an array of tickets or a "ticket" wrapper; the write has happened by then.
internal abstract class TicketWriteResponseConverter<TResponse> : JsonConverter<TResponse>
	where TResponse : TicketWriteResponse
{
	private readonly string _operation;
	private readonly Func<string, string> _unreadableMessage;

	protected TicketWriteResponseConverter(string operation, Func<string, string> unreadableMessage)
	{
		_operation = operation;
		_unreadableMessage = unreadableMessage;
	}

	protected abstract TResponse Build(TicketResponseContent content);

	public override TResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> Build(TicketResponseReader.Read(ref reader, options, _operation, typeof(TResponse).Name, _unreadableMessage));

	public override void Write(Utf8JsonWriter writer, TResponse value, JsonSerializerOptions options)
		=> TicketResponseReader.Write(writer, value.Ticket, value.Success, value.Messages, options);
}

internal sealed class CreateTicketResponseConverter()
	: TicketWriteResponseConverter<CreateTicketResponse>("create", UnreadableMessage)
{
	// TicketsApiWrapper matches on this phrase to tell this error from any other deserialisation failure.
	internal const string MayHaveBeenCreated = "the ticket may have been created";

	internal static string UnreadableMessage(string id)
		=> $"Halo's ticket create response could not be read as a ticket, but {MayHaveBeenCreated} (id {id}).";

	protected override CreateTicketResponse Build(TicketResponseContent content)
		=> new() { Ticket = content.Tickets[0], Tickets = content.Tickets, Success = content.Success, Messages = content.Messages };
}

internal sealed class UpdateTicketResponseConverter()
	: TicketWriteResponseConverter<UpdateTicketResponse>("update", UnreadableMessage)
{
	// TicketsApiWrapper matches on this phrase to tell this error from any other deserialisation failure.
	internal const string MayHaveBeenApplied = "the update may have been applied";

	internal static string UnreadableMessage(string id)
		=> $"Halo's ticket update response could not be read, but {MayHaveBeenApplied} (id {id}).";

	protected override UpdateTicketResponse Build(TicketResponseContent content)
		=> new() { Ticket = content.Tickets[0], Tickets = content.Tickets, Success = content.Success, Messages = content.Messages };
}
