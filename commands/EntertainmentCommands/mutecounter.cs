using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Stormtility.Helpers;

namespace Stormtility.commands
{
    public class mutecounter : ApplicationCommandModule
    {

        [SlashCommandGroup("mutes", "Mute counter group")]
        public class MCounter : ApplicationCommandModule
        {
            private async Task<Dictionary<ulong, int>> mutes()
            {
                string conn = "Data Source=mutecounter.db";
                string sql = "SELECT user, count FROM mutecounter";

                var dict = new Dictionary<ulong, int>();

                try
                {
                    using(var connection = new SqliteConnection(conn))
                    {
                        await connection.OpenAsync();

                        using(var cmd = new SqliteCommand(sql, connection))
                        {
                            using(var result = await cmd.ExecuteReaderAsync())
                            {
                                while(await result.ReadAsync())
                                {
                                    ulong user = (ulong)result.GetInt64(0);
                                    int count = result.GetInt32(1);

                                    if(!dict.ContainsKey(user))
                                    {
                                        dict.Add(user, count);
                                    } else
                                    {
                                        dict[user] += count;
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    await logger.LOGGER(e);
                }

                return dict;
            }

            [SlashCommand("mutecounter", "Check how many times you have been muted for saying ts = this")]
            public async Task mutecounter(InteractionContext ctx)
            {
                await ctx.DeferAsync(ephemeral: true);

                var totalmutes = await mutes();
                if (!totalmutes.TryGetValue(ctx.Interaction.User.Id, out int key))
                {
                    key = 0;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Mute Counter",
                    Color = DiscordColor.Purple,
                    Description = $"You have been muted {key} time{(key == 1 ? "" : "s")}."
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
        }
    }
}