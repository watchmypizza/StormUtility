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

namespace Stormtility.events
{
    public class MemberUpdated
    {
        public static async Task<bool> verifyDisplayName(DiscordUser user)
        {
            Regex regex = new Regex("[^A-Za-z0-9]+");
            DiscordMember discordMember = (DiscordMember)user;
            var nickname = discordMember.DisplayName;
            var match = regex.Match(nickname);

            if(match.Success)
            {
                string currentNick = nickname;
                var replacedNick = currentNick.Replace(match.Value, "");
                try {
                    await discordMember.ModifyAsync(a => a.Nickname = replacedNick ?? user.Username);
                } catch (UnauthorizedException e)
                {
                    Console.WriteLine($"Not authorized to perform action on {discordMember.Mention} ({discordMember.Username})");
                    return false;
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--- STACK BEGIN ---");
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("--- STACK SPLIT ---");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("---  STACK END  ---");
                    return false;
                }
            }

            return true;
        }

        public static async Task<Task> Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            if(vars.commandTriggered)
            {
                return Task.CompletedTask;
            }

            _ = await verifyDisplayName(args.Member);

            var blacklist = args.Guild.GetRole(1397404869702320260);
            var blacklists = "blacklists.json";

            bool hadBefore = args.RolesBefore.Any(r => r.Id == blacklist.Id);
            bool hasAfter = args.RolesAfter.Any(r => r.Id == blacklist.Id);

            var jsonText = File.ReadAllText(blacklists);
            var json = JObject.Parse(jsonText);
            var blacklisttable = (JArray)json["blacklist"];
            JObject toRemove = null;

            if (hadBefore && !hasAfter)
            {
                foreach (JObject entries in blacklisttable)
                {
                    if (entries.ContainsKey(args.Member.Id.ToString()))
                    {
                        toRemove = entries;
                        break;
                    }
                }
            }

            if (toRemove != null)
            {
                blacklisttable.Remove(toRemove);
                File.WriteAllText(blacklists, json.ToString());
                return Task.CompletedTask;
            }

            if (!hadBefore && hasAfter)
            {
                var entry = new JObject
                {
                    [args.Member.Id.ToString()] = new JObject
                    {
                        ["reason"] = "Role has been given manually."
                    }
                };

                ((JArray)json["blacklist"]).Add(entry);

                File.WriteAllText(blacklists, json.ToString());
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}