using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Argument checks that stop a ticket write before any request is sent
/// </summary>
internal static class TicketRequestValidator
{
	// Halo needs at least one of a client, site or user, and works out the others from it.
	internal static void ValidateForCreate(CreateTicketRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (request.ClientId is null && request.SiteId is null && request.UserId is null)
		{
			throw new ArgumentException("A ticket needs a ClientId, SiteId or UserId; no request was sent.", nameof(request));
		}

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

	// 0 and -1 are never real Halo ids (-1 is Halo's "not set"), so a set id must be positive.
	internal static void ThrowIfNotPositive(int value, string paramName, string label, string kind)
	{
		if (value <= 0)
		{
			throw new ArgumentOutOfRangeException(paramName, $"{label} must be a positive Halo {kind} id (was {value}); no request was sent.");
		}
	}
}
