using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility.commands.Anti_Raid_Commands
{
    public class antiraidcommands : ApplicationCommandModule
    {
        [SlashCommandGroup("antiraid", "Raid prevention commands")]
        public class antiraidcmds : ApplicationCommandModule
        {
            [SlashCommand("slowmode", "Set a custom slowmode for the channel this command is executed in.")]
            public async Task slowmode(InteractionContext ctx, [Option("Duration", "The duration of the slowmode")] string duration)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ManageChannels))
                {
                    var permissionsError = new DiscordEmbedBuilder
                    {
                        Title = "404 - Permission Error",
                        Description = "You do not have the necessary permissions to execute this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionsError).AsEphemeral());

                    return;
                }

                int value;

                if (duration.Length < 2 || !int.TryParse(duration[..^1], out value))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Invalid number in duration.").AsEphemeral());
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
                    await ctx.CreateResponseAsync("Slowmode cannot exceed 6 hours.");
                    return;
                }

                await ctx.Interaction.Channel.ModifyAsync(ch => ch.PerUserRateLimit = (int)slowmode.TotalSeconds);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully set the slowmode to {duration}",
                    Color = DiscordColor.Gold
                };

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());
                return;
            }

            [SlashCommand("massban", "Raid cleanup command, mass ban anyone and delete their minutes in a specified time frame.")]
            public async Task massban(InteractionContext ctx, [Option("Duration", "The people to ban that joined in that duration")] string duration)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.All))
                {
                    var permissionsError = new DiscordEmbedBuilder
                    {
                        Title = "404 - Permission Error",
                        Description = "You do not have the necessary permissions to execute this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionsError).AsEphemeral());

                    return;
                }

                if (duration.Length < 2 || !int.TryParse(duration[..^1], out int value))
                {
                    await ctx.CreateResponseAsync("Invalid duration format.");
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
                    await ctx.CreateResponseAsync("Invalid time unit.");
                    return;
                }

                var cutoff = DateTimeOffset.UtcNow - window;

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Collecting members...").AsEphemeral());

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
                    if (member.Permissions.HasPermission(Permissions.Administrator)) continue;

                    if (member.JoinedAt >= cutoff)
                    {
                        try
                        {
                            await member.BanAsync(
                                delete_message_days: 1,
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

            [SlashCommand("lift", "Lift slowmode of all channels again")]
            public async Task lift(InteractionContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.Administrator))
                {
                    var permissionsError = new DiscordEmbedBuilder
                    {
                        Title = "404 - Permission Error",
                        Description = "You do not have the necessary permissions to execute this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionsError).AsEphemeral());

                    return;
                }
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

            }
        }
    }
}
