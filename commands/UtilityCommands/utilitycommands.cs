using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Stormtility.config;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace Stormtility.commands.UtilityCommands
{
    public class utilitycommands
    {
        [SlashCommandGroup("utility", "Utility commands")]
        [Command("utility")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        [Description("Utility commands")]
        public class UtilityCommandsGroup
        {
            [SlashCommand("rank", "Rank every server member the member role")]
            [Command("rank")]
            [Description("Rank every server member with the member role")]
            public async Task rank(SlashCommandContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
                {
                    await ctx.RespondAsync("You do not have permission to use this command.");
                    return;
                }

                await ctx.DeferResponseAsync(true);

                ulong memberRoleID = 1370383562515284090;
                var memberRole = await ctx.Interaction.Guild.GetRoleAsync(memberRoleID);

                int rolesGranted = 0;

                foreach(var member in ctx.Interaction.Guild.Members)
                {
                    if(member.Value.IsBot) { return; }
                    if (!member.Value.Roles.Contains(memberRole))
                    {
                        rolesGranted++;
                        await member.Value.GrantRoleAsync(memberRole);
                    }
                }

                await ctx.EditResponseAsync(new DSharpPlus.Entities.DiscordWebhookBuilder().WithContent($"Granted the Member role to {rolesGranted} members."));
            }

            [SlashCommand("togglemorse", "Toggle morse code translation on or off")]
            [Command("togglemorse")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            [Description("Toggle morse code translation on or off.")]
            public async Task ToggleMorseCommand(SlashCommandContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
                {
                    await ctx.RespondAsync("You do not have permission to use this command.");
                    return;
                }
                vars.morseTranslateEnabled = !vars.morseTranslateEnabled;
                string status = vars.morseTranslateEnabled ? "enabled" : "disabled";
                await ctx.RespondAsync($"Morse code translation has been {status}.", true);
            }

            [SlashCommand("toggleAI", "Toggle AI and what channel it should be in")]
            [Command("toggleAI")]
            [Description("Toggle AI in your current channel.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task toggleAI(SlashCommandContext ctx)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }
                if(!vars.aiToggle)
                {
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("AI enabled."));
                    vars.aiToggle = true;
                    vars.AIChannel = ctx.Interaction.Channel.Id;
                } else
                {
                    vars.aiToggle = false;
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("AI disabled"));
                }
            }

            [SlashCommand("changePrompt", "Change the prompt to a funny prompt")]
            [Command("changePrompt")]
            [Description("Change the prompt the AI should utilize")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task changePrompt(SlashCommandContext ctx, [Parameter("prompt")][Description("The prompt")] string prompt)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Changed promt to: {prompt}").AsEphemeral());
                vars.AIPrompt = prompt;  
            }

            [SlashCommand("whitelist", "Add someone to AI whitelist")]
            [Command("whitelist")]
            [Description("Add someone to the whitelist for the AI")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task whitelist(SlashCommandContext ctx, [Parameter("action")][Description("Action to perform.")] string action, [Parameter("user")][Description("User to execute action on")] DiscordUser user)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
                {
                    await ctx.RespondAsync("You do not have permission to use this command.", true);
                    return;
                }

                if(action.ToLower() == "remove")
                {
                    vars.userIdsAI.Remove(user.Id);
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Removed {user.Username} from whitelist").AsEphemeral());
                } else if (action.ToLower() == "add")
                {
                    vars.userIdsAI.Add(user.Id);
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Added {user.Username} to whitelist").AsEphemeral());
                } else {
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Invalid action, use 'add' or 'remove'").AsEphemeral());
                }
            }

            [SlashCommand("clearhistory", "clear chat history (wipe ai brain)")]
            [Command("clearhistory")]
            [Description("wipe ai brain")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task clearhistory(SlashCommandContext ctx)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }

                vars.chatHistory = [];
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Cleared history").AsEphemeral());
            }
        }
    }
}