using Microsoft.Extensions.Logging;

public class ListLogger<T> : ILogger<T>
{
    public record Entry(LogLevel Level, string Message, Exception? Exception,
                        IReadOnlyDictionary<string, object?> Values);
    public List<Entry> Entries { get; } = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = (state as IEnumerable<KeyValuePair<string, object?>>)
                   ?.ToDictionary(kv => kv.Key, kv => kv.Value)
                   ?? new Dictionary<string, object?>();
        Entries.Add(new Entry(logLevel, formatter(state, exception), exception, values));
    }

}