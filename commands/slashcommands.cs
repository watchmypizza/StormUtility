using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.config;
using Stormtility.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

// convert into sqlite3 tomorrow thanks peanut

namespace Stormtility.commands
{
    public class slashcommands : ApplicationCommandModule
    {

        public async Task<string> getBlacklist(DiscordMember member)
        {
            try {
                string conn = "Data Source=blacklists.db";
                string sql = "SELECT reason FROM blacklists WHERE member = @id";

                using (var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", member.Id);

                        using(var reader = await command.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            return reader?["reason"]?.ToString() ?? "";
                        }
                    }
                }
            } catch (InvalidOperationException e)
            {
                return "";
            } catch (Exception e)
            {
                await logger.LOGGER(e);
                return "";
            }
        }

        public async Task<bool> insertBlacklist(DiscordMember member, string reason)
        {
            try
            {
                string conn = "Data Source=blacklists.db";
                string sql = @"INSERT INTO blacklists (member, reason) VALUES (@id, @reason)";

                using(var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", member.Id);
                        command.Parameters.AddWithValue("@reason", reason.ToString());

                        int rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            } catch(Exception e)
            {
                await logger.LOGGER(e);
                return false;
            }
        }

        public async Task<bool> removeBlacklist(DiscordMember member)
        {
            try
            {
                string conn = "Data Source=blacklists.db";
                string sql = "DELETE FROM blacklists WHERE member = @id";

                using(var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", member.Id);

                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            } catch (Exception e)
            {
                await logger.LOGGER(e);
                return false;
            }
        }

        [SlashCommand("blacklist", "Blacklist someone")]
        public async Task blacklist(InteractionContext ctx, [Option("Member", "Specify the member to blacklist")] DiscordUser user, [Option("Reason", "The reason for the blacklist")] string reason)
        {
            vars.commandTriggered = true;

            var member = (DiscordMember) user;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            string isBlacklist = await getBlacklist((DiscordMember) user);
            if(!(isBlacklist == string.Empty))
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = "This user is already blacklisted."
                };

                errorEmbed.AddField("Reason", isBlacklist);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            bool blacklist = await insertBlacklist((DiscordMember) user, reason);

            if(!blacklist)
            {
                await logger.LOGGER(new Exception("Blacklist threw false response. (BLACKLIST COMMAND)"));
                return;
            }

            var mod_log_ch = ctx.Guild.GetChannel(vars.modLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully blacklisted {member.Mention}."
            };

            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklist",
                Color = DiscordColor.Red,
                Description = $"{member.Mention} has been blacklisted for `{reason}` by {ctx.Interaction.User.Mention}"
            };

            await ctx.DeferAsync(ephemeral:true);
            await mod_log_ch.SendMessageAsync(logEmbed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await member.GrantRoleAsync(ctx.Guild.GetRole(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("unblacklist", "Unblacklist someone that is blacklisted")]
        public async Task unblacklist(InteractionContext ctx, [Option("Member", "Specify the member to unblacklist")] DiscordUser user)
        {
            vars.commandTriggered = true;
            var member = (DiscordMember)user;
            bool roleFound = false;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            string isBlacklisted = await getBlacklist((DiscordMember) user);
            if(isBlacklisted == string.Empty)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = "This user is not blacklisted."
                };

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            bool success = await removeBlacklist((DiscordMember) user);
            if(!success)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "404 - Error",
                    Color = DiscordColor.Red,
                    Description = "An unexpected error happened while trying to process your request."
                };

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                await logger.LOGGER(new Exception("removeBlacklist() threw false in command removeBlacklist!"));
                return;
            }

            roleFound = member.Roles.Any(r => r.Id == 1397404869702320260);

            if(!roleFound)
            {
                var notblacklisted = new DiscordEmbedBuilder
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = $"{member.Mention} is not blacklisted."
                };
                await ctx.DeferAsync(ephemeral: true);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(notblacklisted));
                return;
            }

            var mod_log_ch = ctx.Guild.GetChannel(vars.modLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully unblacklisted {member.Mention}."
            };
            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Green,
                Description = $"{member.Mention} has been unblacklisted by {ctx.Interaction.User.Mention}."
            };

            await ctx.DeferAsync(ephemeral:true);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await mod_log_ch.SendMessageAsync(logEmbed);
            await member.RevokeRoleAsync(ctx.Guild.GetRole(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("checkblacklist", "Check whether a user is blacklisted or not.")]
        public async Task checkblacklist(InteractionContext ctx, [Option("User", "The user to check")] DiscordUser user)
        {
            var member = (DiscordMember)user;
            var IDString = member.Id.ToString();
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            try
            {
                string blacklist = await getBlacklist((DiscordMember) user);

                if(blacklist == string.Empty)
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Blacklist",
                        Color = DiscordColor.Red,
                        Description = "This user is not blacklisted."
                    };

                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                    return;
                }

                var successEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = $"{user.Mention} is blacklisted for `{blacklist}`"
                };

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            } catch (Exception e)
            {
                await logger.LOGGER(e);
            }
        }

        [SlashCommand("mutecounter", "Check how many times you have been muted for saying ts = this")]
        public async Task mutecounter(InteractionContext ctx)
        {
            await ctx.DeferAsync(ephemeral: true);

            int muteCount = 0;

            try
            {
                var jsonText = File.ReadAllText("mutecounter.json");
                var json = JObject.Parse(jsonText);
                var mutes = (JArray)json["mutes"];

                if (mutes != null)
                {
                    string userId = ctx.User.Id.ToString();

                    foreach (JObject entry in mutes)
                    {
                        if (entry.ContainsKey(userId))
                        {
                            muteCount = entry[userId]["mutes"].Value<int>();
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("mutecounter.json missing 'mutes' array.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading mutecounter.json: {ex}");
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "Mute Counter",
                Color = DiscordColor.Purple,
                Description = $"You have been muted {muteCount} time{(muteCount == 1 ? "" : "s")}."
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("debug", "Toggle debug mode")]
        public async Task debugMode(InteractionContext ctx)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.All))
            {
                var errorEmbed = new DiscordEmbedBuilder
                {
                    Title = "404 - No Permissions",
                    Description = "You do not have the necessary permissions to use this command.",
                    Color = DiscordColor.Red
                };
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            if (!vars.debugMode)
            {
                vars.debugMode = true;
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Enabled debug mode").AsEphemeral());
                return;
            } else
            {
                vars.debugMode = false;
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Disabled debug mode").AsEphemeral());
                return;
            }
        }
    }
}
