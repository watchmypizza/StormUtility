using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Stormtility.config;
using System.ComponentModel.Design;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace Stormtility.commands.UtilityCommands
{
    public class utilitycommands : ApplicationCommandModule
    {
        [SlashCommandGroup("utility", "Utility commands")]
        public class UtilityCommandsGroup : ApplicationCommandModule
        {
            [SlashCommand("ping", "Check the bot's latency")]
            public async Task PingCommand(InteractionContext ctx)
            {
                var latency = ctx.Client.Ping;
                await ctx.CreateResponseAsync($"Pong! Latency: {latency} ms", true);
            }

            [SlashCommand("rank", "Rank every server member the member role")]
            public async Task rank(InteractionContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    await ctx.CreateResponseAsync("You do not have permission to use this command.");
                    return;
                }

                await ctx.DeferAsync(true);

                ulong memberRoleID = 1370383562515284090;
                var memberRole = ctx.Interaction.Guild.GetRole(memberRoleID);

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
            public async Task ToggleMorseCommand(InteractionContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    await ctx.CreateResponseAsync("You do not have permission to use this command.");
                    return;
                }
                vars.morseTranslateEnabled = !vars.morseTranslateEnabled;
                string status = vars.morseTranslateEnabled ? "enabled" : "disabled";
                await ctx.CreateResponseAsync($"Morse code translation has been {status}.", true);
            }

            [SlashCommand("toggleAI", "Toggle AI and what channel it should be in")]
            public async Task toggleAI(InteractionContext ctx)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }
                if(!vars.aiToggle)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("AI enabled."));
                    vars.aiToggle = true;
                    vars.AIChannel = ctx.Interaction.Channel.Id;
                } else
                {
                    vars.aiToggle = false;
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("AI disabled"));
                }
            }

            [SlashCommand("changePrompt", "Change the prompt to a funny prompt")]
            public async Task changePrompt(InteractionContext ctx, [Option("prompt", "The prompt")] string prompt)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Changed promt to: {prompt}").AsEphemeral());
                vars.AIPrompt = prompt;  
            }

            [SlashCommand("whitelist", "Add someone to AI whitelist")]
            public async Task whitelist(InteractionContext ctx, [Option("action", "Action")] string action, [Option("user", "User to execute action on")] DiscordUser user)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    await ctx.CreateResponseAsync("You do not have permission to use this command.");
                    return;
                }

                if(action.ToLower() == "remove")
                {
                    vars.userIdsAI.Remove(user.Id);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Removed {user.Username} from whitelist").AsEphemeral());
                } else if (action.ToLower() == "add")
                {
                    vars.userIdsAI.Add(user.Id);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Added {user.Username} to whitelist").AsEphemeral());
                } else {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                        new DiscordInteractionResponseBuilder().WithContent("Invalid action, use 'add' or 'remove'").AsEphemeral());
                }
            }

            [SlashCommand("clearhistory", "clear chat history (wipe ai brain)")]
            public async Task clearhistory(InteractionContext ctx)
            {
                if(ctx.User.Id != 1116315001330880602)
                {
                    return;
                }

                vars.chatHistory = [];
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Cleared history").AsEphemeral());
            }
        }
    }
}