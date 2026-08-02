using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility.commands.Anti_Raid_Commands
{
    public class antiraidcommands
    {
        [Command("antiraid")]
        [Description("Antiraid group")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public class antiraidcmds
        {
            public static async ValueTask<bool> permissionCheck(CommandContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ManageChannels))
                {
                    var permissionsError = new DiscordEmbedBuilder
                    {
                        Title = "404 - Permission Error",
                        Description = "You do not have the necessary permissions to execute this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionsError).AsEphemeral());

                    return false;
                }
                return true;
            }

            [Command("slowmode")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            [Description("Apply slowmode to a channel")]
            public static async ValueTask slowmode(CommandContext ctx, [Parameter("Duration")][Description("How long the slowmode is supposed to be")] string duration)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                int value;

                if (duration.Length < 2 || !int.TryParse(duration[..^1], out value))
                {
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Invalid number in duration.").AsEphemeral());
                    return;
                }

                char unit = char.ToLower(duration[^1]);

                TimeSpan slowmode = unit switch
                {
                    's' => TimeSpan.FromSeconds(value),
                    'm' => TimeSpan.FromMinutes(value),
                    'h' => TimeSpan.FromHours(value),
                    _ => TimeSpan.Zero
                };

                if (slowmode.TotalSeconds > 21000)
                {
                    await ctx.RespondAsync("Slowmode cannot exceed 6 hours.");
                    return;
                }

                await ctx.Channel.ModifyAsync(ch => ch.PerUserRateLimit = (int)slowmode.TotalSeconds);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully set the slowmode to {duration}",
                    Color = DiscordColor.Gold
                };

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());
                return;
            }

            [Command("massban")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            [Description("Mass ban users in a timeframe")]
            public static async ValueTask massban(SlashCommandContext ctx, [Parameter("duration")][Description("Duration of the massban")] string duration)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                if (duration.Length < 2 || !int.TryParse(duration[..^1], out int value))
                {
                    await ctx.RespondAsync("Invalid duration format.");
                    return;
                }

                char unit = char.ToLower(duration[^1]);

                TimeSpan window = unit switch
                {
                    's' => TimeSpan.FromSeconds(value),
                    'm' => TimeSpan.FromMinutes(value),
                    'h' => TimeSpan.FromHours(value),
                    _ => TimeSpan.Zero
                };

                if (window == TimeSpan.Zero)
                {
                    await ctx.RespondAsync("Invalid time unit.");
                    return;
                }

                var cutoff = DateTimeOffset.UtcNow - window;

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Collecting members...").AsEphemeral());

                int banned = 0;
                const int BAN_LIMIT = 50;

                foreach (var member in ctx.Guild.Members.Values)
                {
                    if (banned >= BAN_LIMIT)
                    {
                        return;
                    }

                    if (member.JoinedAt == null)
                    {
                        return;
                    }

                    if (member.Id == ctx.User.Id) continue;
                    if (member.Permissions.HasPermission(DiscordPermission.Administrator)) continue;

                    if (member.JoinedAt >= cutoff)
                    {
                        try
                        {
                            await member.BanAsync(
                                deleteMessageDuration: TimeSpan.FromDays(1),
                                reason: $"Massban: Suspected raid (TRIGGERED BY {ctx.Member.Username}");

                            banned++;

                            await Task.Delay(1000);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Massban complete. Banned **{banned}** users."));
            }

            [Command("lift")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            [Description("Lift slowmode of all channels again")]
            public static async ValueTask lift(SlashCommandContext ctx)
            {
                await ctx.DeferResponseAsync(true);

                var check = await permissionCheck(ctx);

                if (!check) { return; }

                HashSet<ulong> StaffCategoryIds = new()
                    {
                        1373974093191315527,
                        1370149123692560577,
                        1373979817174634576
                    };

                HashSet<ulong> channelsToSkip = new()
                    {
                        1450164600384258058,
                        1323692478301470752,
                        1322350501081124977,
                        1391814759418171645
                    };

                var channels = await ctx.Guild.GetChannelsAsync();
                foreach (DiscordChannel channel in channels)
                {
                    if (channel.ParentId.HasValue && StaffCategoryIds.Contains(channel.ParentId.Value)) { continue; }
                    if (channelsToSkip.Contains(channel.Id)) { continue; }

                    await channel.ModifyAsync(mod => mod.PerUserRateLimit = 0);
                }

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Lifted slowmode of all channels.",
                    Color = DiscordColor.Gold
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
            }
            public static async ValueTask<bool> antiRaidActions(List<DiscordChannel> channels)
            {
                bool antiraid = false;

                HashSet<ulong> StaffCategoryIds = new()
                {
                    1373974093191315527,
                    1370149123692560577,
                    1373979817174634576
                };

                HashSet<ulong> channelsToSkip = new()
                {
                    1450164600384258058,
                    1323692478301470752,
                    1322350501081124977,
                    1391814759418171645
                };

                foreach (DiscordChannel channel in channels)
                {
                    TimeSpan duration = TimeSpan.FromMinutes(5);

                    if (channel.ParentId.HasValue && StaffCategoryIds.Contains(channel.ParentId.Value)) { continue; }
                    if (channelsToSkip.Contains(channel.Id)) { continue; }

                    await channel.ModifyAsync(mod => mod.PerUserRateLimit = (int)duration.TotalSeconds);
                }

                antiraid = true;

                return antiraid;
            }

            [Command("test")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            [Description("Test the automatic slowmode feature.")]
            public static async ValueTask antiraidTest(SlashCommandContext ctx)
            {
                await ctx.DeferResponseAsync(true);

                var check = await permissionCheck(ctx);

                if (!check) { return; }

                var channels = await ctx.Guild.GetChannelsAsync();

                await antiRaidActions(channels.ToList());

                await Task.Delay(10000);

                HashSet<ulong> StaffCategoryIds = new()
                    {
                        1373974093191315527,
                        1370149123692560577,
                        1373979817174634576
                    };

                HashSet<ulong> channelsToSkip = new()
                    {
                        1450164600384258058,
                        1323692478301470752,
                        1322350501081124977,
                        1391814759418171645
                    };

                foreach (DiscordChannel channel in channels)
                {
                    if (channel.ParentId.HasValue && StaffCategoryIds.Contains(channel.ParentId.Value)) { continue; }
                    if (channelsToSkip.Contains(channel.Id)) { continue; }

                    await channel.ModifyAsync(mod => mod.PerUserRateLimit = 0);
                }

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Lifted slowmode of all channels.",
                    Color = DiscordColor.Gold
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
            }
        }
    }
}
