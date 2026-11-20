using Microsoft.Extensions.Logging;

namespace HaloPsa.Api.Test.Infrastructure;

internal sealed class ListLogger : ILogger
{
	public List<string> Messages { get; } = [];

	public List<LogLevel> Levels { get; } = [];

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull
	{
		ArgumentNullException.ThrowIfNull(state);

		return null;
	}

	public bool IsEnabled(LogLevel logLevel)
		=> logLevel != LogLevel.None;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(eventId.Id);
		ArgumentNullException.ThrowIfNull(formatter);

		Levels.Add(logLevel);
		Messages.Add($"{logLevel}: {formatter(state, exception)}");
	}
}
