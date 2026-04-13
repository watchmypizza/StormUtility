using Microsoft.Data.Sqlite;

namespace Stormtility.Helpers
{
    public class autostart
    {
        public static async Task createTable()
        {
            string conn = "Data Source=activetickets.db";
            string sql = @"
                CREATE TABLE IF NOT EXISTS activetickets (
                    member TEXT NOT NULL,
                    channelId TEXT NOT NULL UNIQUE
                );";
            
            string conn2 = "Data Source=blacklists.db";
            string sql2 = @"
                CREATE TABLE IF NOT EXISTS blacklists (member INTEGER NOT NULL UNIQUE,
                    reason TEXT NOT NULL);";

            try
            {
                using (var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                using(var connection = new SqliteConnection(conn2))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql2, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            } 
            catch (Exception e)
            {
                await logger.LOGGER(e);
            }
        }
    }
}