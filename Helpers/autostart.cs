using Microsoft.Data.Sqlite;

namespace Stormtility.Helpers
{
    public class Autostart
    {
        public static async Task CreateTable()
        {
            string activeticketsconn = "Data Source=activetickets.db";
            string activeticketssql = @"
                CREATE TABLE IF NOT EXISTS activetickets (
                    member TEXT NOT NULL,
                    channelId TEXT NOT NULL UNIQUE
                );";

            string blacklistsconn = "Data Source=blacklists.db";
            string blacklistssql = @"
                CREATE TABLE IF NOT EXISTS blacklists (member INTEGER NOT NULL UNIQUE,
                    reason TEXT NOT NULL);";

            string ticketscountconn = "Data Source=tickets.db";
            string ticketscountsql = @"
                CREATE TABLE IF NOT EXISTS tickets (
                    type TEXT NOT NULL UNIQUE,
                    count INTEGER NOT NULL
                );";

            string mutesconn = "Data Source=mutecounter.db";
            string mutesconnsql = @"
                CREATE TABLE IF NOT EXISTS mutecounter (
                    user INTEGER PRIMARY KEY,
                    count INTEGER
                );";

            string staffactivityconn = "Data Source=staffactivity.db";
            string staffactivitysql = @"
                CREATE TABLE IF NOT EXISTS staffactivity (
                    user INTEGER PRIMARY KEY,
                    messagessent INTEGER DEFAULT 0,
                    actionstaken INTEGER DEFAULT 0,
                    vcminutes INTEGER DEFAULT 0,
                    lastactive TEXT)";

            try
            {
                using (var connection = new SqliteConnection(staffactivityconn))
                {
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(staffactivitysql, connection);
                    await command.ExecuteNonQueryAsync();
                }
                using (var connection = new SqliteConnection(activeticketsconn))
                {
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(activeticketssql, connection);
                    await command.ExecuteNonQueryAsync();
                }

                using(var connection = new SqliteConnection(blacklistsconn))
                {
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(blacklistssql, connection);
                    await command.ExecuteNonQueryAsync();
                }

                using(var connection = new SqliteConnection(ticketscountconn))
                {
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(ticketscountsql, connection);
                    await command.ExecuteNonQueryAsync();
                }

                using(var connection = new SqliteConnection(mutesconn))
                {
                    await connection.OpenAsync();
                    using var command = new SqliteCommand(mutesconnsql, connection);
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
            }
        }
    }
}
