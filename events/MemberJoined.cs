using System.Globalization;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json.Linq;
using Stormtility.commands;
using Stormtility.config;

namespace Stormtility.events
{
    public class MemberJoined
    {
        public static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            slashcommands blacklist = new slashcommands();

            string isBlacklisted = await blacklist.getBlacklist(args.Member);
            if(isBlacklisted != string.Empty)
            {
                DiscordRole blacklistRole = args.Guild.GetRole(vars.blacklistRole);
                await args.Member.GrantRoleAsync(blacklistRole);
            }

            return;
        }
    }
}