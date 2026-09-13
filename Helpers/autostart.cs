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

            string marriageconn = "Data Source=marriages.db";
            string marriagessql = @"
                CREATE TABLE IF NOT EXISTS marriages (
                    husband_id INTEGER DEFAULT 0,
                    wife_id INTEGER DEFAULT 0,
                    PRIMARY KEY (husband_id, wife_id)
                )";

            string bumpconn = "Data Source=bumps.db";
            string bumpsql = @"
                CREATE TABLE IF NOT EXISTS bumps (
                    user_Id INTEGER DEFAULT 0 UNIQUE,
                    bumps INTEGER,
                    lastBumper INTEGER DEFAULT 0,
                    PRIMARY KEY (user_Id)
                )";

            try
            {
                using(var connection = new SqliteConnection(bumpconn)) {
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(bumpsql, connection);
                    await command.ExecuteNonQueryAsync();
                }
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

                using (var connection = new SqliteConnection(mutesconn))
                {
                    await connection.OpenAsync();
                    using var command = new SqliteCommand(mutesconnsql, connection);
                    await command.ExecuteNonQueryAsync();
                }

                using (var connection = new SqliteConnection(marriageconn))
                {
                    await connection.OpenAsync();
                    using var command = new SqliteCommand(marriagessql, connection);
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
