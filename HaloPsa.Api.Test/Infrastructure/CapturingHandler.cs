using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Records the requests a Refit client composes and answers with a canned body (or one worked out from the
/// request), so the exact wire shape can be asserted with no credentials and no network.
/// </summary>
internal sealed class CapturingHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, string> _respond;

	/// <summary>
	/// Creates a handler that answers every request with the same body
	/// </summary>
	public CapturingHandler(string responseBody)
		: this(_ => responseBody)
	{
	}

	/// <summary>
	/// Creates a handler that works out each response from the request, which lets a test simulate paging
	/// </summary>
	public CapturingHandler(Func<HttpRequestMessage, string> respond)
		=> _respond = respond;

	/// <summary>
	/// The most recent request URI
	/// </summary>
	public Uri? RequestUri { get; private set; }

	/// <summary>
	/// The query string of the most recent request
	/// </summary>
	public NameValueCollection Query => HttpUtility.ParseQueryString(RequestUri?.Query ?? string.Empty);

	/// <summary>
	/// Every request seen so far, in order
	/// </summary>
	public List<Uri> Requests { get; } = [];

	/// <summary>
	/// The most recent request URI, which must exist
	/// </summary>
	public Uri LastRequest => Requests[^1];

	/// <summary>
	/// Creates a handler that answers every request with the same JSON
	/// </summary>
	public static CapturingHandler Returning(string json) => new(json);

	/// <summary>
	/// Parses the query string of a captured request
	/// </summary>
	public static NameValueCollection ParseQuery(Uri uri) => HttpUtility.ParseQueryString(uri.Query);

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		RequestUri = request.RequestUri;
		Requests.Add(request.RequestUri!);

		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(_respond(request), Encoding.UTF8, "application/json")
		});
	}

	public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://sandbox.halopsa.com") };
}
