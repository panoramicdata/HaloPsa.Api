using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Records the request a Refit client composes and answers with a canned body, so the exact wire
/// shape can be asserted with no credentials and no network.
/// </summary>
internal sealed class CapturingHandler(string responseBody) : HttpMessageHandler
{
	public Uri? RequestUri { get; private set; }

	public NameValueCollection Query => HttpUtility.ParseQueryString(RequestUri?.Query ?? string.Empty);

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		RequestUri = request.RequestUri;

		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
		});
	}

	public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://sandbox.halopsa.com") };
}
