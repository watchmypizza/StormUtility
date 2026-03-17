using System.Globalization;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json.Linq;

namespace Stormtility.events
{
    public class MemberJoined
    {
        public static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var blacklists = File.ReadAllText("blacklists.json");
            var json = JObject.Parse(blacklists);
            var entries = (JArray)json?["blacklist"];

            foreach (JObject entry in entries)
            {
                if (entry[args.Member.Id.ToString()] != null)
                {
                    var blacklistRole = args.Guild.GetRole(1397404869702320260);
                    await args.Member.GrantRoleAsync(blacklistRole);
                    break;
                }
            }

            return;
        }
    }
}