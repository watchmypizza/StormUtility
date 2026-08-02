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
        public static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddedEventArgs args)
        {
            slashcommands blacklist = new slashcommands();

            string isBlacklisted = await blacklist.getBlacklist(args.Member);
            if(isBlacklisted != string.Empty)
            {
                DiscordRole blacklistRole = await args.Guild.GetRoleAsync(vars.blacklistRole);
                await args.Member.GrantRoleAsync(blacklistRole);
            }

            bool hasMemberRole = args.Member.Roles.Any(r => r.Id == 1370383562515284090);
            if(!hasMemberRole)
            {
                DiscordRole memberRole = await args.Guild.GetRoleAsync(1370383562515284090);
                await args.Member.GrantRoleAsync(memberRole);
            }

            return;
        }
    }
}