using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
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
        }
    }
}