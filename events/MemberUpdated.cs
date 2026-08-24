using Stormtility.config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.Exceptions;
using System.Text.Json;
using Stormtility.commands;
using Stormtility.Helpers;

namespace Stormtility.events
{
    public class MemberUpdated
    {
        private static Regex regex = new Regex("[^A-Za-z0-9()!?,.\"$':#+:;_~\\-\\s]+");
        private static async Task<bool> VerifyDisplayName(DiscordUser user)
        {
            DiscordMember discordMember = (DiscordMember)user;

            var nickname = discordMember.DisplayName;

            if (regex.IsMatch(nickname))
            {
                string replacedNick = regex.Replace(nickname, "");

                if (string.IsNullOrWhiteSpace(replacedNick))
                {
                    replacedNick = "Restricted Name";
                }

                try
                {
                    await discordMember.ModifyAsync(a => a.Nickname = replacedNick);
                }
                catch (UnauthorizedException e)
                {
                    Console.WriteLine($"Not authorized to perform action on {discordMember.Mention} ({discordMember.Username})");
                    await Logger.LOGGER(e);
                    return false;
                }
                catch (Exception e)
                {
                    await Logger.LOGGER(e);
                    return false;
                }
            }

            return true;
        }

        public static async Task<Task> Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdatedEventArgs args)
        {
            if(Vars.CommandTriggered)
            {
                return Task.CompletedTask;
            }

            _ = await VerifyDisplayName(args.Member);

            var blacklist = await args.Guild.GetRoleAsync(1397404869702320260);

            bool hadBefore = args.RolesBefore.Any(r => r.Id == blacklist.Id);
            bool hasAfter = args.RolesAfter.Any(r => r.Id == blacklist.Id);

            Slashcommands blacklists = new();

            if (hadBefore && !hasAfter)
            {
                string isBlacklisted = await blacklists.GetBlacklist(args.Member);
                if(isBlacklisted == string.Empty) { return Task.CompletedTask; }
                bool removed = await blacklists.RemoveBlacklist(args.Member);
            }

            if (!hadBefore && hasAfter)
            {
                string isBlacklisted = await blacklists.GetBlacklist(args.Member);
                if(isBlacklisted != string.Empty) { return Task.CompletedTask; }
                bool added = await blacklists.InsertBlacklist(args.Member, "Role has been given manually.");

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
