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

namespace Stormtility.events
{
    public class MemberUpdated
    {
        public static Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            if(vars.commandTriggered)
            {
                return Task.CompletedTask;
            }

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