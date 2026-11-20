using HaloPsa.Api.RateLimiting;
using System.Net;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// An <see cref="IRateLimiter"/> that records what it was asked, in order, and can delay or refuse a slot
/// </summary>
internal sealed class RecordingRateLimiter : IRateLimiter
{
	private readonly Lock _lock = new();
	private readonly List<string> _applied = [];
	private readonly List<HttpStatusCode> _responses = [];
	private readonly List<Type> _failures = [];

	/// <summary>
	/// How long a request waits for a slot; the wait ends early when the caller cancels
	/// </summary>
	public TimeSpan DelayBeforeSlot { get; init; } = TimeSpan.Zero;

	/// <summary>
	/// When set, thrown instead of granting a slot
	/// </summary>
	public Exception? ThrowOnApply { get; init; }

	/// <summary>
	/// "METHOD /path" of every request that was granted a slot
	/// </summary>
	public IReadOnlyList<string> Applied
	{
		get
		{
			lock (_lock)
			{
				return [.. _applied];
			}
		}
	}

	/// <summary>
	/// Status code of every response reported through <see cref="OnResponse"/>
	/// </summary>
	public IReadOnlyList<HttpStatusCode> Responses
	{
		get
		{
			lock (_lock)
			{
				return [.. _responses];
			}
		}
	}

	/// <summary>
	/// Exception type of every failure reported through <see cref="OnSendFailed"/>
	/// </summary>
	public IReadOnlyList<Type> Failures
	{
		get
		{
			lock (_lock)
			{
				return [.. _failures];
			}
		}
	}

	public async Task ApplyRateLimitingAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (DelayBeforeSlot > TimeSpan.Zero)
		{
			await Task.Delay(DelayBeforeSlot, cancellationToken).ConfigureAwait(false);
		}

		if (ThrowOnApply is not null)
		{
			throw ThrowOnApply;
		}

		lock (_lock)
		{
			_applied.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
		}
	}

	public void OnResponse(HttpRequestMessage request, HttpResponseMessage response)
	{
		lock (_lock)
		{
			_responses.Add(response.StatusCode);
		}
	}

	public void OnSendFailed(HttpRequestMessage request, Exception exception)
	{
		lock (_lock)
		{
			_failures.Add(exception.GetType());
		}
	}
}
