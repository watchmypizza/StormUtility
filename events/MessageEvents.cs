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
using Stormtility.Helpers;
using System.Runtime.InteropServices;
using System.Reflection;
using Stormtility.commands;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using DSharpPlus.SlashCommands;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Stormtility.events
{
    public class MessageEvents
    {
        private static async Task<bool> checkMessage(DiscordMessage message)
        {
            bool debug = RuntimeInformation.ProcessArchitecture == Architecture.X64 || RuntimeInformation.ProcessArchitecture == Architecture.X86;
            TranslateMorsecode translator = new TranslateMorsecode();
            await translator.translateMorse(message);

            List<string> blacklistedWords = new List<string> {"18", "e-girl", "condo", "csam", "dropbox", "porn", "rape", "erp", "erotic", "hanime", "hentai", "uttp", "cp", "private", "cam"};

            string content = message.Content;

            Regex inviteRegex = new Regex(@"discord(?:app)?.[a-zA-Z]+\/(?:invite)?(attachments)?\/?([A-Za-z0-9]+)");

            var matches = inviteRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                string inviteCode = match.Groups[2].Value;

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
                if(debug)
                {
                    await logger.debugLog($"Name: {guildName}");
                }
                Regex nameRegex = new Regex(@"[^A-Za-z0-9 \/!?\']+");
                if (guildName == null)
                    continue;

                bool isBad = blacklistedWords.Any(word => guildName.Contains(word, StringComparison.OrdinalIgnoreCase));
                
                if(nameRegex.IsMatch(guildName) && debug)
                {
                    await logger.debugLog($"Bad Name {guildName}");
                    isBad = true;
                }


                vars.inviteCache[inviteCode] = (isBad, DateTime.UtcNow.AddMinutes(10));

                return isBad;
            }

            return false;
        }

        private static async Task<bool> checkPingSpam(DiscordMessage message)
        {
            Regex rex = new Regex(@"<@[0-9]+>");
            int i = 0;
            foreach(var match in rex.Matches(message.Content))
            {
                i++;
                if(i >= 5)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<bool> bioCheck(DiscordMessage message)
        {
            Regex checkMessageForBio = new Regex(@"([ '""\/\\!?:*A-Za-z0-9]+)");
            var match = checkMessageForBio.Match(message.Content.ToLower());
            if(match.Success)
            {
                if(match.Groups[1].Value.Contains("check") && match.Groups[1].Value.Contains("bio"))
                {
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> deleteAnyInvite(DiscordMessage message)
        {
            try {
                if(message.Author.IsBot)
                {
                    return false;
                }

                string[] msgContent = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                StringBuilder contentBuilder = new();
                string builtCb;
                foreach(string str in msgContent)
                {
                    contentBuilder.Append(str);
                }
                builtCb = contentBuilder.ToString();

                Regex inviteRegex = new Regex(@"(http|https)?(?:[\:\/]+)?(?:[a-zA-Z]+\.|)?([A-Za-z0-9]+)(?:app)?\.[a-zA-Z]+\/(?:invite)?(emoji|attachment|channels)?\/?([A-Za-z0-9]+)");
                var matches = inviteRegex.Matches(builtCb);

                List<string> whitelistedLinks = ["tenor", "klipy", "giphy", "medal", "youtube", "onlinesequencer"];
                DiscordMember user = await message.Channel.Guild.GetMemberAsync(message.Author.Id, true);
                bool hasMediaPerms = user.PermissionsIn(message.Channel).HasPermission(Permissions.EmbedLinks | Permissions.AttachFiles);

                bool linkViolationFound = false;

                foreach(Match match in matches) {
                    if(match.Groups[3].Success)
                    {
                        continue;
                    }

                    bool isWhitelisted = whitelistedLinks.Any(v => match.Groups[2].Value.Trim().Contains(v, StringComparison.OrdinalIgnoreCase));

                    if(isWhitelisted && hasMediaPerms) { continue; }

                    if(match.Groups[4].Success || match.Success)
                    {
                        linkViolationFound = true;
                        break;
                    }

                    if(!match.Success) { continue; }
                }

                if(linkViolationFound)
                {
                    await checkMessage(message);
                    return true;
                }

                var biocheck = await bioCheck(message);
                if(biocheck)
                {
                    await checkMessage(message);
                    return true;
                }

                return false;
            } catch(Exception e)
            {
                await logger.LOGGER(e);
                return true;
            }
        }

        private class Payload
        {
            public string model {get; set;}
            public JsonArray messages { get; set; }
            public bool stream {get; set;}
            public bool think { get; set; }
        }

        private class OllamaResponse
        {
            public OllamaMessage message { get; set; }
        }

        private class OllamaMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }


        private static async Task<bool> sendApiRequest(DiscordMessage message, MessageCreateEventArgs ctx)
        {
            string newMessage = $"<{message.CreationTimestamp.ToString("O")}> ({message.Author.Username}): {message.Content}";

            bool isInWhitelist = vars.userIdsAI.Exists(u => ctx.Author.Id == u);
            if (!isInWhitelist) return false;
            if (!vars.aiToggle || vars.AIChannel != ctx.Channel.Id) return true;

            vars.chatHistory.Add(new { role = "user", content = newMessage });

            using HttpClient httpClient = new();
            string url = "http://192.168.2.120:11434/api/chat";

            var messagesArray = new JsonArray();

            messagesArray.Add(JsonNode.Parse(JsonSerializer.Serialize(new { role = "system", content = vars.AIPrompt })));

            foreach (var msg in vars.chatHistory)
                messagesArray.Add(JsonNode.Parse(JsonSerializer.Serialize(msg)));

            var payload = new Payload { model = "qwen3:8b", messages = messagesArray, stream = false, think = true };

            try
            {
                var response = await httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                string botReply = result?.message?.content ?? "none";

                vars.chatHistory.Add(new { role = "assistant", content = botReply });

                await ctx.Message.RespondAsync(botReply);
                return true;
            }
            catch (Exception e)
            {
                await logger.LOGGER(e);
                return false;
            }
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

        public static async Task Client_MessageUpdated(DiscordClient sender, MessageUpdateEventArgs args)
        {
            bool result = await deleteAnyInvite(message: args.Message);
            var user = (DiscordUser)args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;

            if(result)
            {
                if(member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }
                DiscordChannel channelLog = args.Guild.GetChannel(1315142575857864846);
                DiscordMember suki = await args.Guild.GetMemberAsync(845103036317433867);
                await channelLog.SendMessageAsync(new DiscordMessageBuilder().WithContent(suki.Mention).WithAllowedMentions(Mentions.All));
                await channelLog.SendMessageAsync($"`{args.Message.Content.ToString()}` sent by {args.Message.Author.Mention}\nMessage before edited was `{args.MessageBefore.Content}`");
                await args.Message.DeleteAsync();
            }
        }

        public static async Task<int> getCurCount(ulong userId)
        {
            string conn = "Data Source=mutecounter.db";
            string sql = "SELECT count WHERE user=@id FROM mutecounter";

            try
            {
                using(var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var cmd = new SqliteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        var result = await cmd.ExecuteScalarAsync();

                        if(result == null || result == DBNull.Value)
                        {
                            return 0;
                        }

                        return Convert.ToInt32(result);
                    }
                }
            } catch(Exception e)
            {
                await logger.LOGGER(e);
                return 7777;
            }
        }
        public static async Task insertMuteDB(ulong userId)
        {
            try
            {
                string connection = "Data Source=mutecounter.db";
                string sql = "INSERT INTO mutecounter (user, count) VALUES (@id, 1) ON CONFLICT(user) DO UPDATE SET count = count + 1";

                using(var conn = new SqliteConnection(connection))
                {
                    await conn.OpenAsync();

                    using(var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            } catch(Exception e)
            {
                await logger.LOGGER(e);
            }
        }

        public static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            bool result = await deleteAnyInvite(message: args.Message);
            var user = (DiscordUser)args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;
            bool isConsideredSpamPing = await checkPingSpam(args.Message);

            if(result)
            {
                if(member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }
                DiscordChannel channelLog = args.Guild.GetChannel(1315142575857864846);
                DiscordMember suki = await args.Guild.GetMemberAsync(845103036317433867);
                await channelLog.SendMessageAsync(new DiscordMessageBuilder().WithContent(suki.Mention).WithAllowedMentions(Mentions.All));
                await channelLog.SendMessageAsync($"{args.Message.Content.ToString()} sent by {args.Message.Author.Mention}");
                await args.Message.DeleteAsync();
            }

            if(isConsideredSpamPing)
            {
                if(member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }

                await args.Message.DeleteAsync();
            }

            if(vars.aiToggle)
            {
                await sendApiRequest(args.Message, args);
            }
            // if (result)
            // {
            //     await args.Message.DeleteAsync();
            //     foreach (var role in member.Roles)
            //     {
            //         if (role.Id == args.Guild.GetRole(vars.blacklistRole).Id)
            //         {
            //             return;
            //         }
            //         else
            //         {
            //             slashcommands blacklistfunc = new slashcommands();

            //             string blacklisted = await blacklistfunc.getBlacklist((DiscordMember)args.Message.Author);
            //             if(blacklisted != string.Empty) { return; }
            //             bool success = await blacklistfunc.insertBlacklist((DiscordMember)args.Message.Author, $"Inappropiate invite: `{args.Message.Content}`");
            //             DiscordRole blacklistRole = args.Guild.GetRole(vars.blacklistRole);
            //             await member.GrantRoleAsync(blacklistRole);

            //             break;
            //         }
            //     }
            //     var embed = new DiscordEmbedBuilder
            //     {
            //         Title = "Inappropiate invite",
            //         Description = $"{member.Mention} has been blacklisted for sending following invite: {args.Message.Content}",
            //         Color = DiscordColor.Red
            //     };
            //     var logChannel = args.Guild.GetChannel(vars.modLogs);
            //     await logChannel.SendMessageAsync(embed);
            // }

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
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.Administrator))
                {
                    Console.WriteLine("ts = this but staff");
                    return;
                }

                try
                {
                    await insertMuteDB(args.Message.Author.Id);
                } catch (OverflowException e)
                {
                    Console.WriteLine("Welp thats a big number (2,147,483,647)");
                }
                Console.WriteLine("Muted for wrong use of ts");

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

            string[] ticketTypes = { "support", "giveaway", "speedtypist", "exploiter", "appeal" };

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

            if(args.Channel.Id == 1368208863018614866)
            {
                if(args.Message.MessageType == MessageType.ApplicationCommand) return;
                if(args.Author.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers)) return;
                if (staffRole) return;
                else await args.Message.DeleteAsync();
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