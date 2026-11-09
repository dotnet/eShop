using Npgsql;
using NpgsqlTypes;

namespace eShop.TestStack;

/// <summary>Database-level reset/seed operations. Schema is owned by the services' EF migrations and never touched.</summary>
internal sealed class PostgresStore(StackContext context)
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";
    public const string TemplateSuffix = "__template";

    /// <summary>Returns the HiLo-owning services whose sequences were used since the last reset/load.</summary>
    public async Task<IReadOnlyCollection<string>> ServicesWithDirtyHiLoAsync(CancellationToken ct)
    {
        var services = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in context.Config.HiLoSequences.GroupBy(s => s.Database))
        {
            await using var connection = await context.OpenPostgresAsync(group.Key, ct);
            foreach (var sequence in group)
            {
                await using var command = new NpgsqlCommand($"SELECT is_called FROM {ScenarioParser.ParseQualifiedName(sequence.Sequence)}", connection);
                try
                {
                    if (await command.ExecuteScalarAsync(ct) is true)
                    {
                        services.Add(sequence.Service);
                    }
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
                {
                    // Not migrated yet; nothing can be cached.
                }
            }
        }

        return services;
    }

    /// <summary>Truncates every table except the EF migrations history, and restarts all sequences.</summary>
    public async Task<int> TruncateAsync(string database, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync(database, ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var tables = new List<string>();
        await using (var command = new NpgsqlCommand("""
            SELECT format('%I.%I', n.nspname, c.relname)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('r', 'p')
              AND NOT c.relispartition
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND n.nspname NOT LIKE 'pg\_%'
              AND c.relname <> @history
              AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.objid = c.oid AND d.deptype = 'e')
            ORDER BY 1
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("history", MigrationsHistoryTable);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }
        }

        if (tables.Count > 0)
        {
            await using var truncate = new NpgsqlCommand($"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE", connection, transaction);
            await truncate.ExecuteNonQueryAsync(ct);
        }

        // Standalone sequences (EF HiLo) aren't covered by RESTART IDENTITY.
        await using (var restart = new NpgsqlCommand("""
            DO $$
            DECLARE s record;
            BEGIN
              FOR s IN
                SELECT format('%I.%I', n.nspname, c.relname) AS name
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relkind = 'S'
                  AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.objid = c.oid AND d.deptype IN ('a', 'i', 'e'))
              LOOP
                EXECUTE 'ALTER SEQUENCE ' || s.name || ' RESTART';
              END LOOP;
            END $$;
            """, connection, transaction))
        {
            await restart.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return tables.Count;
    }

    /// <summary>Applies all steps for one database in a single transaction, then moves sequences past the loaded IDs.</summary>
    public async Task<int> LoadAsync(string database, IEnumerable<PostgresStep> steps, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync(database, ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var rows = 0;

        foreach (var step in steps)
        {
            try
            {
                rows += step switch
                {
                    SqlStep sql => await ExecuteSqlAsync(connection, transaction, sql, ct),
                    TableStep table => await InsertRowsAsync(connection, transaction, table, ct),
                    _ => throw new InvalidOperationException(step.GetType().Name),
                };
            }
            catch (PostgresException ex)
            {
                throw new TestStackException(
                    $"{database}: {step.Source} failed: {ex.MessageText}{(ex.Detail is { } detail ? $" ({detail})" : "")}",
                    ExitCodes.InvalidScenario, ex);
            }
        }

        await transaction.CommitAsync(ct);
        return rows;
    }

    private static async Task<int> ExecuteSqlAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, SqlStep step, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(step.Sql, connection, transaction);
        return Math.Max(0, await command.ExecuteNonQueryAsync(ct));
    }

    /// <summary>
    /// Inserts rows via <c>json_populate_recordset</c> so Postgres converts each JSON value to the column's type.
    /// Columns not mentioned in any row keep their defaults.
    /// </summary>
    private static async Task<int> InsertRowsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, TableStep step, CancellationToken ct)
    {
        var table = ScenarioParser.ParseQualifiedName(step.Table);
        var columns = step.Rows
            .SelectMany(row => row!.AsObject().Select(pair => pair.Key))
            .Distinct(StringComparer.Ordinal)
            .Select(ScenarioParser.QuoteIdentifier)
            .ToList();
        var columnList = string.Join(", ", columns);

        await using var command = new NpgsqlCommand(
            $"INSERT INTO {table} ({columnList}) SELECT {columnList} FROM json_populate_recordset(NULL::{table}, @rows)",
            connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("rows", NpgsqlDbType.Json) { Value = step.Rows.ToJsonString() });
        return await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Moves identity/serial sequences and configured HiLo sequences to MAX(id)+1 so the services never generate
    /// IDs that collide with scenario rows.
    /// </summary>
    public async Task SyncSequencesAsync(string database, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync(database, ct);

        await using (var command = new NpgsqlCommand("""
            DO $$
            DECLARE c record; max_id bigint;
            BEGIN
              FOR c IN
                SELECT format('%I.%I', n.nspname, t.relname) AS tbl, quote_ident(a.attname) AS col,
                       pg_get_serial_sequence(format('%I.%I', n.nspname, t.relname), a.attname) AS seq
                FROM pg_attribute a
                JOIN pg_class t ON t.oid = a.attrelid
                JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE t.relkind IN ('r', 'p') AND a.attnum > 0 AND NOT a.attisdropped
                  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
                  AND pg_get_serial_sequence(format('%I.%I', n.nspname, t.relname), a.attname) IS NOT NULL
              LOOP
                EXECUTE format('SELECT MAX(%s)::bigint FROM %s', c.col, c.tbl) INTO max_id;
                PERFORM setval(c.seq, COALESCE(max_id, 0) + 1, false);
              END LOOP;
            END $$;
            """, connection))
        {
            await command.ExecuteNonQueryAsync(ct);
        }

        foreach (var sequence in context.Config.HiLoSequences.Where(s => s.Database == database))
        {
            var table = ScenarioParser.ParseQualifiedName(sequence.Table);
            var column = ScenarioParser.QuoteIdentifier(sequence.Column);
            var name = ScenarioParser.ParseQualifiedName(sequence.Sequence);
            await using var command = new NpgsqlCommand(
                $"SELECT setval(@seq::regclass, COALESCE((SELECT MAX({column})::bigint FROM {table}), 0) + 1, false)", connection);
            command.Parameters.AddWithValue("seq", name);
            await command.ExecuteScalarAsync(ct);
        }
    }

    /// <summary>Applied EF migration IDs, or null when the database or history table doesn't exist.</summary>
    public async Task<IReadOnlyList<string>?> MigrationsAsync(string database, CancellationToken ct)
    {
        try
        {
            await using var connection = await context.OpenPostgresAsync(database, ct);
            await using var locate = new NpgsqlCommand("""
                SELECT format('%I.%I', n.nspname, c.relname)
                FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname = @history AND c.relkind = 'r'
                LIMIT 1
                """, connection);
            locate.Parameters.AddWithValue("history", MigrationsHistoryTable);
            if (await locate.ExecuteScalarAsync(ct) is not string table)
            {
                return null;
            }

            await using var command = new NpgsqlCommand($"SELECT \"MigrationId\" FROM {table} ORDER BY 1", connection);
            var migrations = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                migrations.Add(reader.GetString(0));
            }

            return migrations;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            return null;
        }
    }

    public async Task<bool> DatabaseExistsAsync(string database, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync("postgres", ct);
        await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", database);
        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task DropDatabaseAsync(string database, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync("postgres", ct);
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {ScenarioParser.QuoteIdentifier(database)} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>CREATE DATABASE target TEMPLATE source (a file-level copy; requires no sessions on the source).</summary>
    public async Task CopyDatabaseAsync(string source, string target, CancellationToken ct)
    {
        await using var connection = await context.OpenPostgresAsync("postgres", ct);
        await using (var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @source AND pid <> pg_backend_pid()", connection))
        {
            terminate.Parameters.AddWithValue("source", source);
            await terminate.ExecuteNonQueryAsync(ct);
        }

        await using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {ScenarioParser.QuoteIdentifier(target)} WITH (FORCE)", connection))
        {
            await drop.ExecuteNonQueryAsync(ct);
        }

        await using var create = new NpgsqlCommand(
            $"CREATE DATABASE {ScenarioParser.QuoteIdentifier(target)} TEMPLATE {ScenarioParser.QuoteIdentifier(source)}", connection);
        await create.ExecuteNonQueryAsync(ct);
    }
}
