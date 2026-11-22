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
	private readonly List<RateLimitAttempt> _appliedAttempts = [];
	private readonly List<RateLimitAttempt> _respondedAttempts = [];
	private readonly List<RateLimitAttempt> _failedAttempts = [];
	private readonly List<RateLimitResponse> _responseViews = [];
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
	/// When set, <see cref="ThrowOnApply"/> only applies to requests with this absolute path
	/// </summary>
	public string? ThrowOnApplyForPath { get; init; }

	/// <summary>
	/// When set, thrown from <see cref="OnResponse"/> for requests with path <see cref="ThrowOnResponseForPath"/>
	/// </summary>
	public Exception? ThrowOnResponse { get; init; }

	/// <summary>
	/// The absolute path <see cref="ThrowOnResponse"/> applies to
	/// </summary>
	public string? ThrowOnResponseForPath { get; init; }

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
	/// The attempt passed to <see cref="ApplyRateLimitingAsync"/> each time a slot was granted
	/// </summary>
	public IReadOnlyList<RateLimitAttempt> AppliedAttempts
	{
		get
		{
			lock (_lock)
			{
				return [.. _appliedAttempts];
			}
		}
	}

	/// <summary>
	/// The attempt passed to <see cref="OnResponse"/> each time
	/// </summary>
	public IReadOnlyList<RateLimitAttempt> RespondedAttempts
	{
		get
		{
			lock (_lock)
			{
				return [.. _respondedAttempts];
			}
		}
	}

	/// <summary>
	/// The attempt passed to <see cref="OnSendFailed"/> each time
	/// </summary>
	public IReadOnlyList<RateLimitAttempt> FailedAttempts
	{
		get
		{
			lock (_lock)
			{
				return [.. _failedAttempts];
			}
		}
	}

	/// <summary>
	/// The response view passed to <see cref="OnResponse"/> each time
	/// </summary>
	public IReadOnlyList<RateLimitResponse> ResponseViews
	{
		get
		{
			lock (_lock)
			{
				return [.. _responseViews];
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

	public async Task ApplyRateLimitingAsync(RateLimitAttempt attempt, CancellationToken cancellationToken)
	{
		if (DelayBeforeSlot > TimeSpan.Zero)
		{
			await Task.Delay(DelayBeforeSlot, cancellationToken).ConfigureAwait(false);
		}

		if (ThrowOnApply is not null && (ThrowOnApplyForPath is null || attempt.RequestUri!.AbsolutePath == ThrowOnApplyForPath))
		{
			throw ThrowOnApply;
		}

		lock (_lock)
		{
			_applied.Add($"{attempt.Method} {attempt.RequestUri!.AbsolutePath}");
			_appliedAttempts.Add(attempt);
		}
	}

	public void OnResponse(RateLimitAttempt attempt, RateLimitResponse response)
	{
		lock (_lock)
		{
			_responses.Add(response.StatusCode);
			_respondedAttempts.Add(attempt);
			_responseViews.Add(response);
		}

		if (ThrowOnResponse is not null && attempt.RequestUri!.AbsolutePath == ThrowOnResponseForPath)
		{
			throw ThrowOnResponse;
		}
	}

	public void OnSendFailed(RateLimitAttempt attempt, Exception exception)
	{
		lock (_lock)
		{
			_failures.Add(exception.GetType());
			_failedAttempts.Add(attempt);
		}
	}
}
