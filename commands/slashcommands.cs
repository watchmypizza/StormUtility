using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
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
using System.ComponentModel;
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
    public class slashcommands
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
        [Command("blacklist")]
        [Description("Blacklist someone")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task blacklist(SlashCommandContext ctx, [Parameter("Member")][Description("Specify the member to blacklist")] DiscordUser user, [Parameter("Reason")][Description("The reason for the blacklist")] string reason)
        {
            vars.commandTriggered = true;

            var member = (DiscordMember) user;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
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

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            bool blacklist = await insertBlacklist((DiscordMember) user, reason);

            if(!blacklist)
            {
                await logger.LOGGER(new Exception("Blacklist threw false response. (BLACKLIST COMMAND)"));
                return;
            }

            var mod_log_ch = await ctx.Guild.GetChannelAsync(vars.modLogs);

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

            await ctx.DeferResponseAsync(ephemeral:true);
            await mod_log_ch.SendMessageAsync(logEmbed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await member.GrantRoleAsync(await ctx.Guild.GetRoleAsync(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("unblacklist", "Unblacklist someone that is blacklisted")]
        [Command("unblacklist")]
        [Description("Unblacklist someone that is blacklisted")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task unblacklist(SlashCommandContext ctx, [Parameter("Member")][Description("Specify the member to unblacklist")] DiscordUser user)
        {
            vars.commandTriggered = true;
            var member = (DiscordMember)user;
            bool roleFound = false;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
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

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
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

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
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
                await ctx.DeferResponseAsync(ephemeral: true);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(notblacklisted));
                return;
            }

            var mod_log_ch = await ctx.Guild.GetChannelAsync(vars.modLogs);

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

            await ctx.DeferResponseAsync(ephemeral:true);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await mod_log_ch.SendMessageAsync(logEmbed);
            await member.RevokeRoleAsync(await ctx.Guild.GetRoleAsync(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("checkblacklist", "Check whether a user is blacklisted or not.")]
        [Command("checkblacklist")]
        [Description("Check whether a user is blacklisted or not.")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task checkblacklist(SlashCommandContext ctx, [Parameter("User")][Description("The user to check")] DiscordUser user)
        {
            var member = (DiscordMember)user;
            var IDString = member.Id.ToString();
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
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

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                    return;
                }

                var successEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = $"{user.Mention} is blacklisted for `{blacklist}`"
                };

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            } catch (Exception e)
            {
                await logger.LOGGER(e);
            }
        }

        [SlashCommand("debug", "Toggle debug mode")]
        [Command("debug")]
        [Description("Debug")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task debugMode(SlashCommandContext ctx)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.Administrator))
            {
                var errorEmbed = new DiscordEmbedBuilder
                {
                    Title = "404 - No Permissions",
                    Description = "You do not have the necessary permissions to use this command.",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            if (!vars.debugMode)
            {
                vars.debugMode = true;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Enabled debug mode").AsEphemeral());
                return;
            } else
            {
                vars.debugMode = false;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Disabled debug mode").AsEphemeral());
                return;
            }
        }
    }
}
