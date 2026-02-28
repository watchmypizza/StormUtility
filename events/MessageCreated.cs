using Stormtility.config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Stormtility.commands.Anti_Raid_Commands.calculationsanddumbassmath;

namespace Stormtility.events
{
    public class MessageCreated
    {
        private static async Task<bool> checkMessage(DiscordMessage message)
        {
            TranslateMorsecode translator = new TranslateMorsecode();
            await translator.translateMorse(message);

            List<string> blacklistedWords = new List<string> {"18", "e-girl", "condo", "csam", "dropbox", "porn", "rape", "erp", "erotic", "hanime", "hentai", "uttp"};

            string content = message.Content;

            Regex inviteRegex = new Regex(@"https://discord\.gg/([a-zA-Z0-9]+)");

            var matches = inviteRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                string inviteCode = match.Groups[1].Value;

                if (vars.inviteCache.TryGetValue(inviteCode, out var cacheEntry))
                {
                    if (DateTime.UtcNow < cacheEntry.expiresAt)
                    {
                        return cacheEntry.isBad;
                    }
                    else
                    {
                        vars.inviteCache.Remove(inviteCode);
                    }
                }

                string url = $"https://discord.com/api/v10/invites/{inviteCode}?with_counts=true";

                using var http = new HttpClient();
                var response = await http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    continue;

                string jsonText = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonText);

                string? guildName = json["guild"]?["name"]?.ToString();

                if (guildName == null)
                    continue;

                bool isBad = blacklistedWords.Any(word => guildName.Contains(word, StringComparison.OrdinalIgnoreCase));

                vars.inviteCache[inviteCode] = (isBad, DateTime.UtcNow.AddMinutes(10));

                return isBad;
            }

            return false;
        }

        public static async Task<bool> antiRaidActions(List<DiscordChannel> channels)
        {
            bool antiraid = false;

            HashSet<ulong> StaffCategoryIds = new()
            {
                1373974093191315527,
                1370149123692560577,
                1373979817174634576
            };

            HashSet<ulong> channelsToSkip = new()
            {
                1450164600384258058,
                1323692478301470752,
                1322350501081124977,
                1391814759418171645
            };

            foreach(DiscordChannel channel in channels)
            {
                TimeSpan duration = TimeSpan.FromMinutes(5);

                if (channel.ParentId.HasValue && StaffCategoryIds.Contains(channel.ParentId.Value)) { continue; }
                if(channelsToSkip.Contains(channel.Id)) { continue; }

                await channel.ModifyAsync(mod => mod.PerUserRateLimit = (int)duration.TotalSeconds);
            }

            antiraid = true;

            return antiraid;
        }

        public static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            bool result = await checkMessage(message: args.Message);
            var user = (DiscordUser)args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;

            if (result)
            {
                await args.Message.DeleteAsync();
                foreach (var role in member.Roles)
                {
                    if (role.Id == args.Guild.GetRole(vars.blacklistRole).Id)
                    {
                        return;
                    }
                    else
                    {
                        await member.GrantRoleAsync(args.Guild.GetRole(vars.blacklistRole));
                        break;
                    }
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Inappropiate invite",
                    Description = $"{member.Mention} has been blacklisted for sending following invite: {args.Message.Content}",
                    Color = DiscordColor.Red
                };
                var logChannel = args.Guild.GetChannel(vars.modLogs);
                await logChannel.SendMessageAsync(embed);
            }

            if (args.Author.IsBot)
            {
                return;
            }
            var content = args.Message.Content;

            var badPhrases = new[]
            {
                "ts = this",
                "this = ts"
            };

            var exceptions = new[]
            {
                "ts = this shit",
                "this shit = ts"
            };

            bool matchesBad = badPhrases.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));
            bool matchesException = exceptions.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));

            if (matchesBad && !matchesException)
            {
                if (args.Guild == null) return;
                if (member.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    Console.WriteLine("ts = this but staff");
                    return;
                }

                var jsonText = File.ReadAllText("mutecounter.json");
                var json = JObject.Parse(jsonText);
                var mutes = (JArray)json["mutes"];

                string userId = args.Author.Id.ToString();
                JObject? entry = null;

                foreach (JObject e in mutes)
                {
                    if (e.ContainsKey(userId))
                    {
                        entry = e;
                        break;
                    }
                }
                try
                {
                    if (entry == null)
                    {
                        entry = new JObject
                        {
                            [userId] = new JObject
                            {
                                ["mutes"] = 1
                            }
                        };

                        mutes.Add(entry);
                    }
                    else
                    {
                        int current = entry[userId]["mutes"].Value<int>();
                        entry[userId]["mutes"] = current + 1;
                    }
                } catch (OverflowException e)
                {
                    Console.WriteLine("Welp thats a big number (2,147,483,647)");
                }
                Console.WriteLine("Muted for wrong use of ts");
                File.WriteAllText("mutecounter.json", json.ToString());

                await member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(1), "wrong use of ts");
            }

            try
            {
                if(args.Channel.Id == 1178834894399471627)
                {
                    return;
                }

                if (!vars.levenshteinsCache.TryGetValue(args.Author.Id, out var queue))
                {
                    queue = new Queue<string>();
                    vars.levenshteinsCache[args.Author.Id] = queue;
                }

                if (queue.Count >= 1)
                {
                    string prevMsg = queue.Last();

                    double levenshteinresult = calculationsanddumbassmath.LevenshteinRatio(prevMsg, content);

                    if (levenshteinresult >= 0.825)
                    {
                        vars.levenshteinmessages++;
                        if (vars.levenshteinmessages >= 3)
                            await args.Message.DeleteAsync();
                    }
                    else
                    {
                        vars.levenshteinmessages = 0;
                    }
                }

                addMessageToLevenshteinCache(args.Author.Id, content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            string[] ticketTypes = { "support", "giveaway", "speedtypist", "exploiter", "appeal"};

            var channelName = args.Channel.Name;

            string matchedPrefix = null;
            int ticketId = 0;

            bool staffRole = member.Roles.Any(r => r.Id == 1370422448763437207);
            bool gamemodRole = member.Roles.Any(r => r.Id == 1474165198947356793);

            bool regExChannelName = false;

            foreach (var type in ticketTypes)
            {
                var regex = new Regex($@"^(?:.+-)?{type}-(\d+)$");
                var match = regex.Match(channelName);

                if (match.Success)
                {
                    matchedPrefix = type;
                    ticketId = int.Parse(match.Groups[1].Value);

                    if (channelName.Count(c => c == '-') >= 2) break;

                    if (staffRole || gamemodRole)
                    {
                        await args.Channel.ModifyAsync(c => c.Name = $"{args.Author.Username}-{matchedPrefix}-{ticketId}");
                        break;
                    }
                }
            }

            if (args.Channel.Id == 1450164600384258058)
            {
                if (args.Author.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers)) return;
                if (staffRole) return;
                if (args.Message.Attachments.Count <= 0) await args.Message.DeleteAsync();
                else return;
            }

            if(args.Channel.Id == 1315142575857864846)
            {
                if(args.Message.MessageType == MessageType.AutoModAlert)
                {
                    var channels = await args.Guild.GetChannelsAsync();
                    await antiRaidActions(channels.ToList());
                }
            }

            return;
        }

        public static async void addMessageToLevenshteinCache(ulong userId, string messageContent)
        {
            if(!vars.levenshteinsCache.TryGetValue(userId, out var queue))
            {
                queue = new Queue<string>();
                vars.levenshteinsCache[userId] = queue;
            }

            if(queue.Count >= 5)
            {
                queue.Dequeue();
            }

            queue.Enqueue(messageContent);
        }
    }
}