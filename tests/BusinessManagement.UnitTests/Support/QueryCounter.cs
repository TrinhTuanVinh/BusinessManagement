using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class QueryCounter : DbCommandInterceptor
{
    private int _count;
    public int Count => _count;
    public List<string> Sql { get; } = new();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Record(command); return result; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    { Record(command); return new ValueTask<InterceptionResult<DbDataReader>>(result); }

    private void Record(DbCommand c)
    {
        Interlocked.Increment(ref _count);
        lock (Sql) Sql.Add(c.CommandText);
    }
}