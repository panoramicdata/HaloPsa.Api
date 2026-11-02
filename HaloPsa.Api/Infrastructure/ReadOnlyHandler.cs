namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Refuses every request other than GET, HEAD and OPTIONS while <see cref="HaloClientOptions.IsReadOnly"/> is set
/// </summary>
/// <remarks>
/// <see cref="HaloClientOptions.IsReadOnly"/> is read on each request, so changing it after the client is built takes effect.
/// </remarks>
internal sealed class ReadOnlyHandler(HaloClientOptions options) : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (options.IsReadOnly
			&& request.Method != HttpMethod.Get
			&& request.Method != HttpMethod.Head
			&& request.Method != HttpMethod.Options)
		{
			throw new InvalidOperationException(
				$"This HaloPSA client is configured read-only, so {request.Method} {request.RequestUri?.AbsolutePath} was refused and no request was sent.");
		}

		return base.SendAsync(request, cancellationToken);
	}
}
