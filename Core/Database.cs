using System.Data;
using Microsoft.Data.Sqlite;
using NursingBot.Logger;

namespace NursingBot.Core
{
    public static class Database
    {
        private static string? CONNECTION_STRING;

        private struct TableInfo
        {
            public string Name;
            public string Query;
        }

        private static readonly TableInfo[] TableInfos = new[]
        {
            new TableInfo {
                Name = "SERVERS",
                Query = @"CREATE TABLE 'servers' (
                        'id'	INTEGER NOT NULL,
                        'discordUID'	INTEGER NOT NULL UNIQUE,
                        'prefix'    TEXT NOT NULL DEFAULT '!',
                        'createdAt'	INTEGER NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        PRIMARY KEY('id' AUTOINCREMENT)
                    );
                "
            },
        };

        private static async Task InitTables(SqliteConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) as nTables FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (tableCount == TableInfos.Length)
                {
                    return;
                }

                await Log.Info("INITIALIZE DB");

                if (tableCount > 0)
                {
                    cmd.CommandText = "SELECT 'drop table ' || name || ';' as query FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
                    var dropCommands = await cmd.ExecuteReaderAsync();
                    List<string> queries = new();

                    while (await dropCommands.ReadAsync())
                    {
                        queries.Add(dropCommands.GetString("query"));
                    }
                    await dropCommands.DisposeAsync();

                    foreach (var query in queries)
                    {
                        cmd.CommandText = query;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await Log.Info("CLEAR TABLES");
                }

                foreach (var table in TableInfos)
                {
                    cmd.CommandText = table.Query;
                    await cmd.ExecuteNonQueryAsync();
                    await Log.Info($"ADD TABLE - {table.Name}");
                }

            }
            catch (Exception e)
            {
                await Log.Fatal(e);
            }
        }

        public static async Task Initialize(string connectionString)
        {
            CONNECTION_STRING = connectionString;
            using var conn = await Open();
            await InitTables(conn);
        }

        public static async Task<SqliteConnection> Open()
        {
            var conn = new SqliteConnection(CONNECTION_STRING);
            await conn.OpenAsync();
            return conn;
        }
    }
}