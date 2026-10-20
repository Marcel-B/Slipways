using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Slipways.Infrastructure.Persistence;

/// <summary>
/// SQLite's built-in lower() only folds ASCII, so a search for "überlingen" would miss "Überlingen".
/// Replaces it on every connection with .NET's culture-invariant lower-casing, which EF uses for ToLower().
/// </summary>
public sealed class SqliteUnicodeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        Register(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            Register(sqlite);
    }

    /// <summary>For connections that are opened outside EF, such as a shared in-memory database.</summary>
    public static void Register(SqliteConnection connection) =>
        connection.CreateFunction("lower", (string? value) => value?.ToLowerInvariant(), isDeterministic: true);
}
