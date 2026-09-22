using HaloPsa.Api.Converters;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Sites;

/// <summary>
/// Represents a client site in the Halo system
/// </summary>
public record Site
{
	/// <summary>
	/// The unique identifier for the site
	/// </summary>
	[JsonPropertyName("id")]
	public int Id { get; init; }

	/// <summary>
	/// The name of the site
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// The description of the site
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	/// <summary>
	/// The client ID this site belongs to
	/// </summary>
	/// <remarks>
	/// Halo serialises this as a decimal (<c>21.0</c>), so it needs <see cref="FlexibleInt32Converter"/>
	/// rather than the default int handling, which rejects it and fails the whole response.
	/// </remarks>
	[JsonPropertyName("client_id")]
	[JsonConverter(typeof(FlexibleInt32Converter))]
	public int ClientId { get; init; }

	/// <summary>
	/// The client name this site belongs to
	/// </summary>
	[JsonPropertyName("client_name")]
	public string? ClientName { get; init; }

	/// <summary>
	/// Whether this site is active
	/// </summary>
	[JsonPropertyName("inactive")]
	public bool IsInactive { get; init; }

	/// <summary>
	/// The primary phone number for the site
	/// </summary>
	[JsonPropertyName("phonenumber")]
	public string? PhoneNumber { get; init; }

	/// <summary>
	/// The mobile phone number for the site
	/// </summary>
	[JsonPropertyName("mobilenumber")]
	public string? MobileNumber { get; init; }

	/// <summary>
	/// The email address for the site
	/// </summary>
	[JsonPropertyName("emailaddress")]
	public string? EmailAddress { get; init; }

	/// <summary>
	/// The website URL for the site
	/// </summary>
	[JsonPropertyName("website")]
	public string? Website { get; init; }

	/// <summary>
	/// The full address of the site
	/// </summary>
	[JsonPropertyName("address")]
	public string? Address { get; init; }

	/// <summary>
	/// The first line of the address
	/// </summary>
	[JsonPropertyName("addressline1")]
	public string? AddressLine1 { get; init; }

	/// <summary>
	/// The second line of the address
	/// </summary>
	[JsonPropertyName("addressline2")]
	public string? AddressLine2 { get; init; }

	/// <summary>
	/// The city of the site
	/// </summary>
	[JsonPropertyName("city")]
	public string? City { get; init; }

	/// <summary>
	/// The state or county of the site
	/// </summary>
	[JsonPropertyName("county")]
	public string? County { get; init; }

	/// <summary>
	/// The postal code of the site
	/// </summary>
	[JsonPropertyName("postcode")]
	public string? PostCode { get; init; }

	/// <summary>
	/// The country of the site
	/// </summary>
	[JsonPropertyName("country")]
	public string? Country { get; init; }

	/// <summary>
	/// Notes about the site
	/// </summary>
	[JsonPropertyName("notes")]
	public string? Notes { get; init; }

	/// <summary>
	/// Whether this is the main site for the client
	/// </summary>
	[JsonPropertyName("ismainsite")]
	public bool IsMainSite { get; init; }
}