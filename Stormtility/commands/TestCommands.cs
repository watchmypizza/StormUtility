using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility.commands
{
    public class TestCommands : BaseCommandModule
    {
        [Command("test")]
        public async Task test(CommandContext ctx)
        {
            var msg = await ctx.Channel.SendMessageAsync("Commands are enabled:3");

            await Task.Delay(3000);
            await msg.DeleteAsync();
        }

        [Command("blacklist")]
        public async Task blacklist(CommandContext ctx, DiscordMember member, string reason)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            foreach(var role in member.Roles)
            {
                if (role.Id == 1397404869702320260)
                {
                    var blacklisted = new DiscordMessageBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithTitle("Blacklist")
                        .WithDescription($"{member.Mention} is already blacklisted.")
                        );
                    var origmsg2 = await ctx.Channel.SendMessageAsync(blacklisted);
                    await Task.Delay(3000);
                    await origmsg2.DeleteAsync();

                    return;
                }
            }

            var mod_log_ch = ctx.Guild.GetChannel(1373144394382966834);

            var message = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Blacklisted")
                .WithColor(DiscordColor.Red)
                .WithDescription($"Successfully blacklisted {member.Mention}.")
                );

            var logEmbed = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Blacklist")
                .WithColor(DiscordColor.Red)
                .WithDescription($"{member.Mention} has been blacklisted for `{reason}`")
                );

            var origmsg = await ctx.Channel.SendMessageAsync(message);
            await mod_log_ch.SendMessageAsync(logEmbed);
            await member.GrantRoleAsync(ctx.Guild.GetRole(1397404869702320260));
            await Task.Delay(3000);
            await origmsg.DeleteAsync();
        }
    }
}
