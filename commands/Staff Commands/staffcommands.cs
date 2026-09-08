using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
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
                return;
            }
            CommandCooldown.RemoveCooldown(command, user.Id);
        }
    }
}
