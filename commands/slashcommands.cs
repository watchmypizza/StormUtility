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
    public class Slashcommands
    {

        public async Task<string> GetBlacklist(DiscordMember member)
        {
            try {
                string conn = "Data Source=blacklists.db";
                string sql = "SELECT reason FROM blacklists WHERE member = @id";

                using var connection = new SqliteConnection(conn);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", member.Id);

                using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                return reader?["reason"]?.ToString() ?? "";
            } catch (InvalidOperationException)
            {
                return "";
            } catch (Exception e)
            {
                await Logger.LOGGER(e);
                return "";
            }
        }

        public async Task<bool> InsertBlacklist(DiscordMember member, string reason)
        {
            try
            {
                string conn = "Data Source=blacklists.db";
                string sql = @"INSERT INTO blacklists (member, reason) VALUES (@id, @reason)";

                using var connection = new SqliteConnection(conn);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", member.Id);
                command.Parameters.AddWithValue("@reason", reason.ToString());

                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            } catch(Exception e)
            {
                await Logger.LOGGER(e);
                return false;
            }
        }

        public async Task<bool> RemoveBlacklist(DiscordMember member)
        {
            try
            {
                string conn = "Data Source=blacklists.db";
                string sql = "DELETE FROM blacklists WHERE member = @id";

                using var connection = new SqliteConnection(conn);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", member.Id);

                await command.ExecuteNonQueryAsync();
                return true;
            } catch (Exception e)
            {
                await Logger.LOGGER(e);
                return false;
            }
        }

        private static async Task<bool> PermissionCheck(SlashCommandContext ctx)
        {
            bool gamemodRole = ctx.Member!.Roles.Any(r => r.Id == 1474165198947356793);
            if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(DiscordPermission.ModerateMembers) && !gamemodRole)
            {
                var permissionErrorEmbed = new DiscordEmbedBuilder
                {
                    Title = "404 - No Permissions",
                    Description = "You do not have the necessary permissions to use this command.",
                    Color = DiscordColor.Red
                };

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                return false;
            }
            return true;
        }

        [Command("blacklist")]
        [Description("Blacklist someone")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Blacklist(SlashCommandContext ctx, [Parameter("Member")][Description("Specify the member to blacklist")] DiscordMember user, [Parameter("Reason")][Description("The reason for the blacklist")] string reason)
        {
            await ctx.DeferResponseAsync(true);


            if (!await PermissionCheck(ctx))
            {
                return;
            }

            Vars.CommandTriggered = true;

            string isBlacklist = await GetBlacklist(user);
            if(!(isBlacklist == string.Empty))
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = "This user is already blacklisted."
                };

                errorEmbed.AddField("Reason", isBlacklist);

                await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            bool blacklist = await InsertBlacklist(user, reason);

            if(!blacklist)
            {
                await Logger.LOGGER(new Exception("Blacklist threw false response. (BLACKLIST COMMAND)"));
                return;
            }

            var mod_log_ch = await ctx.Guild!.GetChannelAsync(Vars.ModLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully blacklisted {user.Mention}."
            };

            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklist",
                Color = DiscordColor.Red,
                Description = $"{user.Mention} has been blacklisted for `{reason}` by {ctx.Interaction.User.Mention}"
            };

            await mod_log_ch.SendMessageAsync(logEmbed);
            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await user.GrantRoleAsync(await ctx.Guild.GetRoleAsync(1397404869702320260));
            Vars.CommandTriggered = false;
        }

        [Command("unblacklist")]
        [Description("Unblacklist someone that is blacklisted")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Unblacklist(SlashCommandContext ctx, [Parameter("Member")][Description("Specify the member to unblacklist")] DiscordMember user)
        {
            await ctx.DeferResponseAsync(true);
            bool roleFound = false;

            if (!await PermissionCheck(ctx))
            {
                return;
            }

            Vars.CommandTriggered = true;

            string isBlacklisted = await GetBlacklist(user);
            if(isBlacklisted == string.Empty)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = "This user is not blacklisted."
                };

                await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            bool success = await RemoveBlacklist(user);
            if(!success)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                {
                    Title = "404 - Error",
                    Color = DiscordColor.Red,
                    Description = "An unexpected error happened while trying to process your request."
                };

                await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                await Logger.LOGGER(new Exception("removeBlacklist() threw false in command removeBlacklist!"));
                return;
            }

            roleFound = user.Roles.Any(r => r.Id == 1397404869702320260);

            if(!roleFound)
            {
                var notblacklisted = new DiscordEmbedBuilder
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = $"{user.Mention} is not blacklisted."
                };
                await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(notblacklisted));
                return;
            }

            var mod_log_ch = await ctx.Interaction.Guild!.GetChannelAsync(Vars.ModLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully unblacklisted {user.Mention}."
            };
            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Green,
                Description = $"{user.Mention} has been unblacklisted by {ctx.Interaction.User.Mention}."
            };

            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await mod_log_ch.SendMessageAsync(logEmbed);
            await user.RevokeRoleAsync(await ctx.Interaction.Guild!.GetRoleAsync(1397404869702320260));
            Vars.CommandTriggered = false;
        }

        [Command("checkblacklist")]
        [Description("Check whether a user is blacklisted or not.")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task CheckBlacklist(SlashCommandContext ctx, [Parameter("User")][Description("The user to check")] DiscordMember user)
        {
            var member = (DiscordMember)user;
            var IDString = member.Id.ToString();
            if (!await PermissionCheck(ctx))
            {
                return;
            }

            try
            {
                string blacklist = await GetBlacklist((DiscordMember) user);

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
                await Logger.LOGGER(e);
            }
        }

        [Command("debug")]
        [Description("Debug")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task DebugMode(SlashCommandContext ctx)
        {
            if (!ctx.Member!.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.Administrator))
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

            if (!Vars.DebugMode)
            {
                Vars.DebugMode = true;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Enabled debug mode").AsEphemeral());
                return;
            } else
            {
                Vars.DebugMode = false;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Disabled debug mode").AsEphemeral());
                return;
            }
        }
    }
}
