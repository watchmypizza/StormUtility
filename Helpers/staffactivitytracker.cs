using Microsoft.Data.Sqlite;

namespace Stormtility.Helpers
{
    public class StaffActivityTracker
    {
        public static async Task<Dictionary<string, object>> GetActivity(ulong userId)
        {
            var staffActivity = new Dictionary<string, object>();
            string conn = "Data Source=staffactivity.db";
            using (var connection = new SqliteConnection(conn))
            {
                await connection.OpenAsync();
                string command = "select * from staffactivity where user=@userid";

                using(var cmd = new SqliteCommand(command, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userId);
                    using(var reader = await cmd.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            staffActivity["MessagesSent"] = reader.GetInt32(reader.GetOrdinal("messagessent"));
                            staffActivity["ActionsTaken"] = reader.GetInt32(reader.GetOrdinal("actionstaken"));
                            staffActivity["VCMinutes"] = reader.GetInt32(reader.GetOrdinal("vcmins"));
                            staffActivity["lastActive"] = reader.GetString(reader.GetOrdinal("lastactive"));
                        }
                    }
                }

                return staffActivity;
            }
        }

        public static async Task UpsertActivity(ulong userId, Dictionary<string, Object> staffActivity)
        {
            string conn = "Data Source=staffactivity.db";
            string command = "insert into staffactivity (user, messagessent, actionstaken, vcminutes, lastactive) VALUES (@user, @messagessent, @actionstaken, @vcminutes, @lastactive) on conflict(user) do update set messagessent = excluded.messagessent, actionstaken = excluded.actionstaken, vcminutes = excluded.vcminutes, lastactive = excluded.lastactive";
        
            using(var connection = new SqliteConnection(conn))
            {
                await connection.OpenAsync();

                using(var com = new SqliteCommand(command, connection))
                {
                    com.Parameters.AddWithValue("@user", userId);
                    com.Parameters.AddWithValue("@messagessent", staffActivity["MessagesSent"]);
                    com.Parameters.AddWithValue("@actionstakne", staffActivity["ActionsTaken"]);
                    com.Parameters.AddWithValue("@vcminutes", staffActivity["VCMinutes"]);
                    com.Parameters.AddWithValue("@lastactive", staffActivity["lastActive"]);

                    await com.ExecuteNonQueryAsync();
                }
            }
        }
    }
}