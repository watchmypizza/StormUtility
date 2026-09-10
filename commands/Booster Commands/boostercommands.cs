using System.ComponentModel;
using System.Runtime.InteropServices;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Data.Sqlite;
using Stormtility.Helpers;

namespace Stormtility.commands.Booster_Commands {
    [Command("booster")]
    [Description("Booster only commands")]
    [AllowedProcessors(typeof(SlashCommandProcessor))]
    public class BoosterCommands
    {
        private static ulong BoosterRole { get; } = 1373145844303527937;
        private static Random RNG { get; } = new Random();
        private static Dictionary<ulong, int> gayStore = [];

        private static async Task<bool> CheckBoosterRole(SlashCommandContext ctx)
        {
            if (((DiscordMember)ctx.Interaction.User).Roles.Any(r => r.Id == 1370390655528079442)) return true;
            if (!((DiscordMember)ctx.Interaction.User).Roles.Any(r => r.Id == BoosterRole))
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be a server booster in order to use this command.").AsEphemeral());
                return false;
            }
            return true;
        }

        [Command("kill")]
        [Description("Mute someone for 2 minutes")]
        public static async Task Kill(SlashCommandContext ctx, [Parameter("User")][Description("User to kill")] DiscordMember member)
        {
            try
            {
                if (!await CheckBoosterRole(ctx))
                {
                    return;
                }
                if (!CommandCooldown.CheckCooldown(ctx.Command.Name, ctx.Interaction.User.Id))
                {
                    await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are currently on cooldown.").AsEphemeral());
                    return;
                }

                await member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(2), $"{ctx.Interaction.User.Username} ({ctx.Interaction.User.Id}) used /kill on {member.Username} ({member.Id})");
                CommandCooldown.AddCooldown(ctx.Command.Name, ctx.User.Id, 24);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{member.Mention} has been timed out successfully.").AsEphemeral());
                return;
            }
            catch(UnauthorizedException)
            {
                CommandCooldown.RemoveCooldown(ctx.Command.Name, ctx.User.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You cannot mute this user.").AsEphemeral());
                return;
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                CommandCooldown.RemoveCooldown(ctx.Command.Name, ctx.User.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("An unknown error has occured.").AsEphemeral());
                return;
            }
        }

        [Command("gay")]
        [Description("Determines how gay a user is")]
        public static async Task GayTest(SlashCommandContext ctx, [Parameter("User")][Description("User to gay check")] DiscordMember member)
        {
            try
            {
                if (!await CheckBoosterRole(ctx))
                {
                    return;
                }

                if (!gayStore.TryGetValue(member.Id, out int gayMeter))
                {
                    gayMeter = RNG.Next(-1, 101);
                }

                if (ctx.Interaction.User.Id == 1444912002525954110)
                {
                    gayMeter = 100;
                }

                gayStore[member.Id] = gayMeter;
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{member.Mention} is {gayMeter}% gay.").AsEphemeral());
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                CommandCooldown.RemoveCooldown(ctx.Command.Name, ctx.User.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("An unknown error has occured.").AsEphemeral());
                return;
            }
        }

        [Command("hug")]
        [Description("Hug someone or make someone hug someone")]
        public async Task Hug(
            SlashCommandContext ctx,
            [Parameter("user")][Description("The user to hug:3")] DiscordUser user,
            [Parameter("hugger")][Description("The person that hugs the user:3")] DiscordUser? hugger = null
        )
        {
            if (!await CheckBoosterRole(ctx)) return;
            if (ctx.Interaction.User == user || user == hugger)
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You cannot hug yourself.").AsEphemeral());
                return;
            }

            await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{hugger?.Mention ?? ctx.Interaction.User.Mention} hugged {user.Mention} :3"));
            return;
        }

        [Command("kiss")]
        [Description("Kiss someone or make someone kiss someone forcefully:3")]
        public async Task Kiss(
            SlashCommandContext ctx,
            [Parameter("user")][Description("The user to kiss:3")] DiscordUser user,
            [Parameter("kisser")][Description("The person that kisses the user:3")] DiscordUser? kisser = null
        )
        {
            if (!await CheckBoosterRole(ctx)) return;
            if (ctx.Interaction.User == user || user == kisser)
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You cannot kiss yourself.").AsEphemeral());
                return;
            }

            await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{kisser?.Mention ?? ctx.Interaction.User.Mention} kissed {user.Mention} :3"));
            return;
        }

        [Command("marriages")]
        [Description("Marriage commands")]
        public class Marriages
        {
            public record Marriage {
                public required ulong HusbandId { get; set; }
                public required ulong WifeId { get; set; }
            }

            public static async Task UpsertMarriageBase(DiscordUser husband, DiscordUser? wife = null, ulong? wifeId = 0) // idk leave me alone
            {
                string datasource = "Data Source=marriages.db";
                using var conn = new SqliteConnection(datasource);
                await conn.OpenAsync();
                string upsertcommand = @"INSERT INTO marriages (husband_id,wife_id) VALUES (@husband,@wife) ON CONFLICT (husband_id,wife_id) DO UPDATE SET husband_id=excluded.husband_id,wife_id=excluded.wife_id";
                using var com = new SqliteCommand(upsertcommand, conn);
                com.Parameters.AddWithValue("@husband", husband.Id);
                if (wife != null)
                {
                    com.Parameters.AddWithValue("@wife", wife.Id);
                } else if(wifeId != 0) {
                    com.Parameters.AddWithValue("@wife", wifeId);
                } else {
                    return;
                }
                await com.ExecuteNonQueryAsync();
                return;
            }

            public static async Task<Marriage?> GetMarriageBase(DiscordUser user)
            {
                string datasource = "Data Source=marriages.db";
                using var conn = new SqliteConnection(datasource);
                await conn.OpenAsync();

                string selectsql = @"SELECT husband_id, wife_id FROM marriages
                                    WHERE husband_id = @userId OR wife_id = @userId
                                    LIMIT 1";

                using var com = new SqliteCommand(selectsql, conn);
                com.Parameters.AddWithValue("@userId", user.Id);

                using var reader = await com.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Marriage
                    {
                        HusbandId = (ulong)reader.GetInt64(0),
                        WifeId = (ulong)reader.GetInt64(1)
                    };
                }

                return null;
            }

            private async Task<bool> DeleteMarriage(SlashCommandContext ctx, DiscordUser user) {
                ulong PrimUserId = ctx.User.Id;
                string datasource = "Data Source=marriages.db";
                using var conn = new SqliteConnection(datasource);
                await conn.OpenAsync();

                string deleteSql = @"DELETE FROM marriages WHERE (husband_id=@userid AND wife_id=@primuserid) OR (husband_id=@primuserid AND wife_id=@userid)";
                using var com = new SqliteCommand(deleteSql, conn);

                com.Parameters.AddWithValue("@userid", user.Id);
                com.Parameters.AddWithValue("@primuserid", PrimUserId);

                int rowsAffected = await com.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }

            [Command("marry")]
            [Description("Marry someone:3")]
            public async Task Marry(
                SlashCommandContext ctx,
                [Parameter("User")][Description("User to marry")] DiscordUser user
            )
            {
                if (!await CheckBoosterRole(ctx)) return;
                if (!CommandCooldown.CheckCooldown(ctx.Command.Name, ctx.Interaction.User.Id))
                {
                    await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are currently on cooldown.").AsEphemeral());
                    return;
                }
                if (user.Id == ctx.User.Id)
                {
                    await ctx.RespondAsync("You cannot marry yourself!", true);
                    return;
                }

                if (user.IsBot)
                {
                    await ctx.RespondAsync("You cannot marry a bot!", true);
                    return;
                }

                var targetMarriage = await GetMarriageBase(user);
                if (targetMarriage != null)
                {
                    ulong partnerId = (targetMarriage.HusbandId == user.Id)
                        ? targetMarriage.WifeId
                        : targetMarriage.HusbandId;

                    await ctx.RespondAsync($"{user.Mention} is already married to <@{partnerId}>!", true);
                    return;
                }

                var authorMarriage = await GetMarriageBase(ctx.User);
                if (authorMarriage != null)
                {
                    ulong partnerId = (authorMarriage.HusbandId == ctx.User.Id)
                        ? authorMarriage.WifeId
                        : authorMarriage.HusbandId;

                    await ctx.RespondAsync($"You are already married to <@{partnerId}>!", true);
                    return;
                }

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Marriage Proposal",
                    Color = DiscordColor.Red
                };

                embedBuilder.AddField("From:", $"{ctx.Interaction.User.Mention}", true);
                var button = new DiscordButtonComponent(DiscordButtonStyle.Success, $"marriage:{ctx.Interaction.User.Id}", "Accept", false, new DiscordComponentEmoji("🎉"));
                var messageBuilder = new DiscordMessageBuilder().AddEmbed(embedBuilder).AddActionRowComponent(button);
                try
                {
                    await user.SendMessageAsync(messageBuilder);
                    await ctx.RespondAsync($"A message has been sent to {user.Mention} to consent!", true);
                    CommandCooldown.AddCooldown(ctx.Command.Name, ctx.User.Id, 12);
                } catch(UnauthorizedException) {
                    await ctx.RespondAsync("Something went wrong. Your cooldown has not been affected.", true);
                } catch(Exception ex) {
                    await ctx.RespondAsync("Something went wrong. Your cooldown has not been affected.", true);
                    await Logger.LOGGER(ex);
                }
                // await UpsertMarriageBase(ctx.User, user);
                // await ctx.RespondAsync($"Congratulations! {ctx.User.Mention} and {user.Mention} are now married! :3");
            }

            [Command("divorce")]
            [Description("Divorce someone | This command does not require you to boost the server")]
            public async Task Divorce(
                SlashCommandContext ctx,
                [Parameter("User")][Description("The uesr to divorce")] DiscordUser user
            )
            {
                bool isDeletedSuccess = await DeleteMarriage(ctx, user);
                if (!isDeletedSuccess)
                {
                    await ctx.RespondAsync("You cannot break someone out of a marriage.", true);
                    return;
                }

                await ctx.RespondAsync("You divorced successfully.", true);
                return;
            }
        }

        // TODO:
        // /bump <user>: u have a crush on someone and we store the value of that, 12 hour cooldown, automatic renewals(
        // /marry <user> <user(optional)>

        // STRUCTURE SQLITE BUMP: User (ulong) | Bumps (long) | HasAutomaticRenewalsForUserId (ulong) | LastRenewal (DateTime)
    }
}
