using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Argument checks that stop a ticket write before any request is sent
/// </summary>
internal static class TicketRequestValidator
{
	// Halo rejects an id of 0 and uses -1 for "not set", so neither may reach it as a client, user or site.
	internal static void ValidateForCreate(CreateTicketRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		ThrowIfNotPositive(request.ClientId, nameof(request), "ClientId", "client");
		ThrowIfNotPositive(request.UserId, nameof(request), "UserId", "user");

		if (request.SiteId is { } siteId)
		{
			ThrowIfNotPositive(siteId, nameof(request), "SiteId", "site");
		}
	}


	internal static void ThrowIfNotATicketId(int id, string paramName)
		=> ThrowIfNotPositive(id, paramName, "Ticket id", "ticket");

	// As for a create, a client, user or site that is set must be positive; an unset one is simply not sent.
	internal static void ValidateForUpdate(UpdateTicketRequest request)
	{
		if (request.ClientId is { } clientId)
		{
			ThrowIfNotPositive(clientId, nameof(request), "ClientId", "client");
		}

		if (request.UserId is { } userId)
		{
			ThrowIfNotPositive(userId, nameof(request), "UserId", "user");
		}

		if (request.SiteId is { } siteId)
		{
			ThrowIfNotPositive(siteId, nameof(request), "SiteId", "site");
		}
	}

	// Halo rejects an id of 0 and uses -1 for "not set", so neither may reach it as an id.
	internal static void ThrowIfNotPositive(int value, string paramName, string label, string kind)
	{
		if (value <= 0)
		{
			throw new ArgumentOutOfRangeException(paramName, $"{label} must be a positive Halo {kind} id (was {value}); no request was sent.");
		}
	}
}
