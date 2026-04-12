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
            } 
            catch (Exception e)
            {
                await logger.LOGGER(e);
            }
        }
    }
}