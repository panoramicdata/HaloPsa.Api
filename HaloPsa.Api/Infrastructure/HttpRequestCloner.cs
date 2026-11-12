namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Copies a request so it can be sent again, since an HttpRequestMessage can only be sent once
/// </summary>
internal static class HttpRequestCloner
{
	internal static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original)
	{
		var clone = new HttpRequestMessage(original.Method, original.RequestUri);

		// Copy headers
		foreach (var header in original.Headers)
		{
			_ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		// Copy content if present
		if (original.Content != null)
		{
			var contentBytes = await original.Content.ReadAsByteArrayAsync();
			clone.Content = new ByteArrayContent(contentBytes);

			// Copy content headers
			foreach (var header in original.Content.Headers)
			{
				_ = clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
		}

		// Copy other properties
		clone.Version = original.Version;
		foreach (var property in original.Options)
		{
			clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
		}

		return clone;
	}
}
