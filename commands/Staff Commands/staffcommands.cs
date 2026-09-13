using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Stormtility.commands.Booster_Commands;
using Stormtility.Helpers;

namespace Stormtility.commands.Staff_Commands {
    [Command("staff")]
    [Description("Staff commands")]
    [AllowedProcessors(typeof(SlashCommandProcessor))]
    public class StaffCommands {
        public static bool CheckStaff(SlashCommandContext ctx) {
            if (((DiscordMember)ctx.Interaction.User).Roles.Any(r => r.Id == 1370422448763437207)) return true;
            return false;
        }

        [Command("removecooldown")]
        [Description("Remove cooldown from a user")]
        public static async Task RemoveCooldown(
            SlashCommandContext ctx,
            [Parameter("user")][Description("The user to remove the cooldown from")] DiscordUser user,
            [Parameter("command")][Description("The name of a specific command")] string? command = null
        )
        {
            if (!CheckStaff(ctx))
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You do not have the necessary permissions to execute this command.").AsEphemeral());
                return;
            }

            if (command == null)
            {
                CommandCooldown.RemoveCooldown(user.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Removed cooldown from {user.Mention}.").AsEphemeral());
                return;
            }
            await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Removed cooldown from {user.Mention}.").AsEphemeral());
            CommandCooldown.RemoveCooldown(command, user.Id);
        }

        [Command("bumps")]
        [Description("List the top 10 bumps")]
        public async Task ListBumps(SlashCommandContext ctx) {
            if (!CheckStaff(ctx))
            {
                await ctx.RespondAsync("You do not have necessary permissions to use this command.", true);
                return;
            }

            ConcurrentDictionary<ulong,int> bumps = await BoosterCommands.ReadAllBumpAsync() ?? [];
            var embed = new DiscordEmbedBuilder
            {
                Title = "Top 10 Bumpers",
            };

            bumps.OrderByDescending(pair => pair.Value);

            int i = 1;
            StringBuilder sb = new();

            foreach (var entry in bumps)
            {
                sb.AppendLine($"{i}. <@{entry.Key}> - **{entry.Value}** bump{(entry.Value == 1 ? "" : "s")}");
                i++;
            }

            embed.Description = sb.ToString();
            embed.Color = DiscordColor.Yellow;

            await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }
    }
}
