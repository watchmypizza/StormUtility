using System.Buffers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Stormtility.commands.Anti_Raid_Commands;
using Stormtility.commands.Anti_Raid_Commands.calculationsanddumbassmath;
using Stormtility.commands.UtilityCommands;
using Stormtility.config;
using Stormtility.Helpers;

namespace Stormtility.events
{
    public class MessageEvents
    {
        private static readonly HttpClient client = new();
        private static readonly List<string> blacklistedWords =
        [
            "18", "e-girl", "condo", "csam", "dropbox", "porn", "rape",
            "erp", "erotic", "hanime", "hentai", "uttp", "cp", "private", "cam"
        ];

        private static async Task<bool> CheckMessage(DiscordMessage message)
        {
            bool debug = RuntimeInformation.ProcessArchitecture == Architecture.X64 || RuntimeInformation.ProcessArchitecture == Architecture.X86;
            TranslateMorsecode translator = new();
            await translator.translateMorse(message);

            string content = message.Content;

            var matches = InviteRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                // TODO:
                // Implement secondCheck regex
                if (match.Groups[1].Value.ToLower() != "discord") continue;
                string inviteCode = match.Groups[2].Value;

                if (Vars.inviteCache.TryGetValue(inviteCode, out var cacheEntry))
                {
                    if (DateTime.UtcNow < cacheEntry.expiresAt)
                    {
                        return cacheEntry.isBad;
                    }
                    else
                    {
                        Vars.inviteCache.Remove(inviteCode);
                    }
                }

                string url = $"https://discord.com/api/v10/invites/{inviteCode}?with_counts=true";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    continue;

                string jsonText = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonText);

                string? guildName = json["guild"]?["name"]?.ToString();
                if (debug)
                {
                    await Logger.DebugLog($"Name: {guildName}");
                }
                if (guildName == null)
                    continue;

                var member = message!.Author as DiscordMember;

                try
                {
                    await member!.TimeoutAsync(DateTimeOffset.Now.AddHours(1), $"Advertising other servers: {inviteCode}");
                }
                catch (Exception e)
                {
                    await Logger.LOGGER(e);
                }

                bool isBad = blacklistedWords.Any(word => guildName.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (NameRegex.IsMatch(guildName) && debug)
                {
                    await Logger.DebugLog($"Bad Name {guildName}");
                    isBad = true;
                }

                Vars.inviteCache[inviteCode] = (isBad, DateTime.UtcNow.AddMinutes(10));

                return isBad;
            }

            return false;
        }

        private static async Task<bool> CheckHoneyPot(DiscordMember member, DiscordMessage message) {
            if (member.PermissionsIn(message.Channel!).HasPermission(DiscordPermission.Administrator))
            {
                return false;
            }

            if (message.Channel?.Id != 1557476848357867742) { return false; } // IF NOT THEN RETURN FALSE. DO NOT MAKE SAME MISTAKE.
            try
            {
                await member.BanAsync(TimeSpan.FromDays(7), "Honeypot - Kick (AUTO BAN)");
                await member.UnbanAsync();
                return true;
            } catch(UnauthorizedException) {
                return false;
            } catch(Exception ex) {
                await Logger.LOGGER(ex);
                return false;
            }
        }

        private static bool CheckMessageForNewLines(DiscordMember member, string messageContent)
        {
            if (member.Roles.Any(r => r.Id == 1374444948228931661)) // level 5 role id
            {
                return false;
            }
            int newlineCount = messageContent.Count(c => c == '\n');

            return newlineCount >= 5;
        }

        public struct SpamPingData
        {
            public int Count { get; set; }
            public DateTime LastSeen { get; set; }
        }

        private static readonly Dictionary<ulong, SpamPingData> pingSpamCounter = [];
        private static Regex rex = new Regex(@"<@[0-9]+>");
        private static Regex nameRegex = new Regex(@"[^A-Za-z0-9 \/!?\']+");
        private static Regex checkMessageForBio = new Regex(@"([ '""\/\\!?:*A-Za-z0-9]+)");
        private static Regex inviteRegex = new Regex(@"(http|https)?(?:[\:\/]+)?(?:[a-zA-Z]+\.|)?([A-Za-z0-9]+)(?:app)?\.[a-zA-Z]+\/(?:invite)?(emoji|attachment|channels)?\/?([A-Za-z0-9]+)");
        private static readonly Regex AttachmentUrlRegex = new(
            @"https?://[^\s<>\[\]""']+\.(?:gif|png|jpe?g|webp|mp4|webm|mov)(?:\?[^\s<>\[\]""']*)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        private static Regex regex = new(@"<#([0-9]+)>");
        private static Regex ticketRegex = new(@"(?:support|appeal|exploiter|speedtypist)-(?:\d+)");

        private static async Task<bool> CheckPingSpam(DiscordMessage message)
        {
            int i = 0;
            foreach(var match in rex.Matches(message.Content))
            {
                i++;
                if(i >= 3)
                {
                    pingSpamCounter.TryGetValue(message.Author!.Id, out SpamPingData data);

                    if(data.Count > 0 && DateTime.UtcNow - data.LastSeen > TimeSpan.FromMinutes(2))
                    {
                        data = new SpamPingData { Count = 0, LastSeen = DateTime.UtcNow };
                    }

                    if(data.Count >= 5)
                    {
                        var member = (DiscordMember) message.Author;
                        try {
                            await member.TimeoutAsync(DateTime.UtcNow.AddMinutes(5), "Spam pinging.");
                        } catch(Exception ex)
                        {
                            await Logger.LOGGER(ex);
                        }
                        pingSpamCounter.Remove(message.Author.Id);
                    } else {
                        pingSpamCounter[message.Author.Id] = new SpamPingData
                        {
                            Count = data.Count + 1,
                            LastSeen = DateTime.UtcNow
                        };
                    }
                    return true;
                }
            }

            return false;
        }

        private static async Task<bool> BioCheck(DiscordMessage message)
        {
            var match = CheckMessageForBio.Match(message.Content.ToLower());
            if(match.Success)
            {
                if(match.Groups[1].Value.Contains("check") && match.Groups[1].Value.Contains("bio"))
                {
                    return true;
                }
            }
            return false;
        }

        public static Regex Rex { get => rex; set => rex = value; }
        public static Regex NameRegex { get => nameRegex; set => nameRegex = value; }
        public static Regex CheckMessageForBio { get => checkMessageForBio; set => checkMessageForBio = value; }
        public static Regex InviteRegex { get => inviteRegex; set => inviteRegex = value; }
        public static Regex Regex { get => regex; set => regex = value; }
        public static Regex TicketRegex { get => ticketRegex; set => ticketRegex = value; }

        private static bool isInTicket(DiscordMessage message) {
            //ex. ilikepeanutbutter-exploiter-id
            // unclaimed: exploiter-id

            if(TicketRegex.IsMatch(message.Channel?.Name ?? "")) {
                return true;
            }

            return false;
        }

        private static async Task<bool> DeleteAnyInvite(DiscordMessage message)
        {
            try
            {
                if (message.WebhookId is not null)
                {
                    return false;
                }

                if (message.Author!.IsBot)
                {
                    return false;
                }

                if(isInTicket(message)) { return false; }

                string[] msgContent = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                StringBuilder contentBuilder = new();
                string builtCb;
                foreach (string str in msgContent)
                {
                    contentBuilder.Append(str);
                }
                builtCb = contentBuilder.ToString();

                var matches = InviteRegex.Matches(builtCb);

                List<string> whitelistedLinks = ["tenor", "klipy", "giphy", "medal", "youtube", "onlinesequencer"];
                DiscordMember? user = message.Author as DiscordMember;
                if (user is null)
                {
                    try
                    {
                        user = await message.Channel!.Guild.GetMemberAsync(message.Author.Id);
                    }
                    catch (NotFoundException)
                    {
                        return false;
                    }
                    catch (Exception e)
                    {
                        await Logger.LOGGER(e);
                        return false;
                    }
                }
                DiscordPermissions userChannelPerms = user.PermissionsIn(message.Channel!);
                bool hasMediaPerms = userChannelPerms.HasPermission(DiscordPermission.AttachFiles)
                  && userChannelPerms.HasPermission(DiscordPermission.EmbedLinks);

                bool linkViolationFound = false;
                bool isZalgoText = Calculationsanddumbassmath.IsZalgoText(message.Content);

                if (isZalgoText)
                {
                    await message.DeleteAsync("Zalgo text detected");
                }

                foreach (Match match in matches)
                {
                    if (match.Groups[3].Success)
                    {
                        continue;
                    }

                    bool isWhitelisted = whitelistedLinks.Any(v => match.Groups[2].Value.Trim().Contains(v, StringComparison.OrdinalIgnoreCase));

                    if (isWhitelisted && hasMediaPerms) { continue; }

                    if (match.Groups[4].Success || match.Success)
                    {
                        linkViolationFound = true;
                        break;
                    }

                    if (!match.Success) { continue; }
                }

                if (linkViolationFound)
                {
                    await CheckMessage(message);
                    return true;
                }

                var biocheck = await BioCheck(message);
                if (biocheck)
                {
                    await CheckMessage(message);
                    return true;
                }

                return false;
            }
            catch (NotFoundException)
            {
                return true;
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                return true;
            }
        }

        private class Payload
        {
            public string Model {get; set;} = "";
            public JsonArray Messages { get; set; } = [];
            public bool Stream {get; set;} = new();
            public bool Think { get; set; }
        }

        private class OllamaResponse
        {
            public OllamaMessage Message { get; set; } = new();
        }

        private class OllamaMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }


        private static async Task<bool> SendApiRequest(DiscordMessage message, MessageCreatedEventArgs ctx)
        {
            string newMessage = $"<{message.CreationTimestamp.ToString("O")}> ({message.Author!.Username}): {message.Content}";

            bool isInWhitelist = Vars.UserIdsAI.Exists(u => ctx.Author.Id == u);
            if (!isInWhitelist) return false;
            if (!Vars.AiToggle || Vars.AIChannel != ctx.Channel.Id) return true;

            Vars.chatHistory.Add(new { role = "user", content = newMessage });

            using HttpClient httpClient = new();
            string url = "http://192.168.2.120:11434/api/chat";

            var messagesArray = new JsonArray();

            messagesArray.Add(JsonNode.Parse(JsonSerializer.Serialize(new { role = "system", content = Vars.AIPrompt })));

            foreach (var msg in Vars.chatHistory)
                messagesArray.Add(JsonNode.Parse(JsonSerializer.Serialize(msg)));

            var payload = new Payload { Model = "qwen3:8b", Messages = messagesArray, Stream = false, Think = true };

            try
            {
                var response = await httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                string botReply = result?.Message?.Content ?? "none";

                Vars.chatHistory.Add(new { role = "assistant", content = botReply });

                await ctx.Message.RespondAsync(botReply);
                return true;
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                return false;
            }
        }

        public static async Task<bool> AntiRaidActions(List<DiscordChannel> channels)
        {
            bool antiraid = false;

            HashSet<ulong> StaffCategoryIds =
            [
                1373974093191315527,
                1370149123692560577,
                1373979817174634576
            ];

            HashSet<ulong> channelsToSkip =
            [
                1450164600384258058,
                1323692478301470752,
                1322350501081124977,
                1391814759418171645
            ];

            foreach(DiscordChannel channel in channels)
            {
                TimeSpan duration = TimeSpan.FromMinutes(5);

                if (channel.ParentId.HasValue && StaffCategoryIds.Contains(channel.ParentId.Value)) { continue; }
                if(channelsToSkip.Contains(channel.Id)) { continue; }

                AntiRaidCommands.existingSlowmodes.Add(channel.Id, channel.PerUserRateLimit);

                await channel.ModifyAsync(mod => mod.PerUserRateLimit = (int)duration.TotalSeconds);
            }

            antiraid = true;

            return antiraid;
        }

        public static async Task Client_MessageUpdated(DiscordClient sender, MessageUpdatedEventArgs args)
        {
            if (args.Message.WebhookId is not null) return;
            bool result = await DeleteAnyInvite(message: args.Message);
            var user = (DiscordUser)args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;
            await RouteMessages(args.Message, msgBefore: args.MessageBefore!.Content, edited: true);
            await CheckSuggestion.IsValidSuggestion(sender, args.Message);
            bool isNonMultiLineMessage = CheckMessageForNewLines(member, args.Message.Content);

            if (result)
            {
                if (member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }
                DiscordChannel channelLog = await args.Guild.GetChannelAsync(1315142575857864846);
                DiscordMember suki = await args.Guild.GetMemberAsync(845103036317433867);
                await channelLog.SendMessageAsync(new DiscordMessageBuilder().WithContent(suki.Mention).WithAllowedMentions(Mentions.All));
                await channelLog.SendMessageAsync($"```diff\n- {args.MessageBefore.Content}\n+ {args.Message.Content}```\n Sent by {args.Message.Author!.Mention}");
                await args.Message.DeleteAsync();
            }

            if(isNonMultiLineMessage) { await args.Message.DeleteAsync(); }
        }

        public static async Task<int> GetCurCount(ulong userId)
        {
            string conn = "Data Source=mutecounter.db";
            string sql = "SELECT count WHERE user=@id FROM mutecounter";

            try
            {
                using var connection = new SqliteConnection(conn);
                await connection.OpenAsync();

                using var cmd = new SqliteCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", userId);
                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            } catch(Exception e)
            {
                await Logger.LOGGER(e);
                return 7777;
            }
        }
        public static async Task InsertMuteDB(ulong userId)
        {
            try
            {
                string connection = "Data Source=mutecounter.db";
                string sql = "INSERT INTO mutecounter (user, count) VALUES (@id, 1) ON CONFLICT(user) DO UPDATE SET count = count + 1";

                using var conn = new SqliteConnection(connection);
                await conn.OpenAsync();

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", userId);
                await cmd.ExecuteNonQueryAsync();
            } catch(Exception e)
            {
                await Logger.LOGGER(e);
            }
        }
        public static async Task RouteMessages(DiscordMessage message, MessageReactionRemovedEventArgs? reactionRem = null, MessageReactionAddedEventArgs? reaction = null, string msgBefore = "", bool edited = false, bool deleted = false, bool isReaction = false)
        {
            if(message.Author!.IsBot)
            {
                return;
            }

            DiscordUser user = message.Author;

            string content = message.Content;

            List<DiscordAttachment>? linksIfFound = message.Attachments?.ToList();
            if(linksIfFound != null)
            {
                foreach(var attachment in linksIfFound)
                {
                    content += $" {attachment.Url}";
                }
            }

            DiscordChannel channel = message.Channel!;

            string cc = $"In {channel!.Mention}:\n```diff\n+ {content}```";

            if(edited)
            {
                cc = $"In {channel.Mention}:\n```diff\n- {msgBefore}\n+ {content}```";
            }
            else if(deleted)
            {
                cc = $"In {channel.Mention}:\n```diff\n- {content}```";
            }
            else if(isReaction)
            {
                if(reaction != null) {
                    cc = $"User {reaction.User.Mention} reacted with {reaction.Emoji} to {reaction.Message.JumpLink}";
                } else if(reactionRem != null)
                {
                    cc = $"User {reactionRem.User.Mention} removed the reaction with {reactionRem.Emoji} to {reactionRem.Message.JumpLink}";
                }
            }

            string avatarUrl = user.GetAvatarUrl(MediaFormat.Auto) ?? user.DefaultAvatarUrl;

            var payload = new
            {
                content = cc,
                username = user.Username,
                avatar_url = avatarUrl,
            };

            string jsonString = JsonSerializer.Serialize(payload);

            try {
                var response = await client.PostAsync(Program.wbhurl2, new StringContent(jsonString, Encoding.UTF8, "application/json"));

                if(!response.IsSuccessStatusCode)
                {
                    await Logger.LOGGER(new Exception($"Webhook failed with status: {response.StatusCode}"));
                }
            } catch(Exception e)
            {
                await Logger.LOGGER(e);
                return;
            }

            return;
        }

        public static async Task ReceiveMessageAndForwardToChannel(DiscordClient client, DiscordMessage message)
        {
            if (message.Author!.IsBot) return;

            string content = message.Content;

            Match match = Regex.Match(content);
            if (!match.Success || !ulong.TryParse(match.Groups[1].Value, out ulong chId))
            {
                await Logger.ConsoleLog("Invalid channel ID in message.");
                return;
            }

            DiscordChannel? channel = await client.GetChannelAsync(chId);

            string[] splits = content.Split(":", StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length < 2)
            {
                await Logger.ConsoleLog("Invalid message format.");
                return;
            }

            try
            {
                await client.SendMessageAsync(channel, $"{splits[1]}");
            }
            catch (Exception ex)
            {
                await Logger.ConsoleLog($"Error sending message: {ex.Message}");
            }
        }
        public static async Task Client_MessageDeleted(DiscordClient sender, MessageDeletedEventArgs args)
        {
            await RouteMessages(args.Message, deleted: true);

            return;
        }

        public static async Task Client_MessageCreated(DiscordClient sender, MessageCreatedEventArgs args)
        {
            if(args.Channel.Id == 1315142575857864846)
            {
                if(args.Message.MessageType == DiscordMessageType.AutoModerationAlert)
                {
                    var channels = await args.Guild.GetChannelsAsync();
                    await AntiRaidActions([.. channels]);
                }
            }

            if (args.Message.WebhookId is not null) return;
            bool result = await DeleteAnyInvite(message: args.Message);
            var user = args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;
            bool isConsideredSpamPing = await CheckPingSpam(args.Message);
            await CheckSuggestion.IsValidSuggestion(sender, args.Message);
            bool isMultiLineMessage = CheckMessageForNewLines(member, args.Message.Content);
            if (args.Channel.Id != 1533206494181462137) { await RouteMessages(args.Message); }
            if(LinkHelper.IsBlacklisted(args.Message.Content)) { await args.Message.DeleteAsync(); }
            bool isHoneyPot = await CheckHoneyPot(member, args.Message);

            SlowmodeHelper.TryAdd(args.Channel, msg: true);
            SlowmodeHelper.MsgTrigger();

            ChatReviveHelper.ResetTime(args.Message);

            if(args.Channel.Id == 1533206494181462137)
            {
                await ReceiveMessageAndForwardToChannel(sender, args.Message);
            }

            if (Utilitycommands.afkList.Any(entry => args.Message.Content.Contains($"<@{entry}>", StringComparison.OrdinalIgnoreCase)))
            {
                await args.Message.RespondAsync("Please do not ping someome that is on the AFK list.");
            }

            if(isMultiLineMessage) { await args.Message.DeleteAsync(); }

            if(Utilitycommands.afkList.Any(entry => args.Message.Author?.Id == entry)) {
                await args.Message.Channel!.SendMessageAsync($"Welcome back {args.Message.Author!.Mention}! I removed your AFK status.");
                try
                {
                    Utilitycommands.afkList.Remove(args.Message.Author.Id);
                    string nickname = member.Nickname;
                    string[] splits = nickname.Split(" ");
                    string removeAFK = splits[0].Replace("AFK", "");
                    string newNick = string.Join(" ", splits);
                    await member.ModifyAsync(st => st.Nickname = newNick ?? "");
                } catch(UnauthorizedException) {

                } catch(Exception ex) {
                    await Logger.LOGGER(ex);
                }
            }

            if (result)
            {
                if (member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }
                DiscordChannel channelLog = await args.Guild.GetChannelAsync(1315142575857864846);
                DiscordMember suki = await args.Guild.GetMemberAsync(845103036317433867);
                await channelLog.SendMessageAsync(new DiscordMessageBuilder().WithContent(suki.Mention).WithAllowedMentions(Mentions.All));
                await channelLog.SendMessageAsync($"{args.Message.Content} sent by {args.Message.Author!.Mention}");
                await args.Message.DeleteAsync();
                ulong stickerId = 1533600293630251008;
                var sticker = await args.Guild.GetStickerAsync(stickerId);
                await args.Message.Channel!.SendMessageAsync(new DiscordMessageBuilder().WithStickers(new[] { sticker }));
            }

            if(isConsideredSpamPing)
            {
                if(member.Roles.Any(r => r.Id == 1370422448763437207 || r.Id == 1474165198947356793))
                {
                    return;
                }

                await args.Message.DeleteAsync();
            }

            if(Vars.AiToggle)
            {
                await SendApiRequest(args.Message, args);
            }
            // if (result)
            // {
            //     await args.Message.DeleteAsync();
            //     foreach (var role in member.Roles)
            //     {
            //         if (role.Id == args.Guild.GetRole(Vars.blacklistRole).Id)
            //         {
            //             return;
            //         }
            //         else
            //         {
            //             slashcommands blacklistfunc = new slashcommands();

            //             string blacklisted = await blacklistfunc.getBlacklist((DiscordMember)args.Message.Author);
            //             if(blacklisted != string.Empty) { return; }
            //             bool success = await blacklistfunc.insertBlacklist((DiscordMember)args.Message.Author, $"Inappropiate invite: `{args.Message.Content}`");
            //             DiscordRole blacklistRole = args.Guild.GetRole(Vars.blacklistRole);
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
            //     var logChannel = args.Guild.GetChannel(Vars.modLogs);
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
                "this = ts",
                "ts=this",
                "this=ts",
                "ts =this",
                "this= ts",
                "this =ts",
                "ts= this"
            };

            var exceptions = new[]
            {
                "ts = this shit",
                "this shit = ts",
                "thisshit=ts",
                "ts=thisshit",
                "this shit= ts",
                "ts= this shit",
                "ts =this shit",
                "this shit =ts"
            };

            bool matchesBad = badPhrases.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));
            bool matchesException = exceptions.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));

            if (matchesBad && !matchesException)
            {
                if (member.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(DiscordPermission.Administrator))
                {
                    Console.WriteLine("ts = this but staff");
                    return;
                }

                try
                {
                    await InsertMuteDB(args.Message.Author!.Id);
                } catch (OverflowException e)
                {
                    Console.WriteLine("Welp thats a big number (2,147,483,647)");
                    await Logger.LOGGER(e);
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

                if (!Vars.levenshteinsCache.TryGetValue(args.Author.Id, out var queue))
                {
                    queue = new Queue<string>();
                    Vars.levenshteinsCache[args.Author.Id] = queue;
                }

                if (queue.Count >= 1)
                {
                    string prevMsg = queue.Last();

                    double levenshteinresult = Calculationsanddumbassmath.LevenshteinRatio(prevMsg, content);

                    if (levenshteinresult >= 0.825)
                    {
                        Vars.levenshteinmessages++;
                        if (Vars.levenshteinmessages >= 3)
                            await args.Message.DeleteAsync();
                    }
                    else
                    {
                        Vars.levenshteinmessages = 0;
                    }
                }

                AddMessageToLevenshteinCache(args.Author.Id, content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            string[] ticketTypes = ["support", "giveaway", "speedtypist", "exploiter", "appeal"];

            var channelName = args.Channel.Name;

            string matchedPrefix = "";
            int ticketId = 0;

            bool staffRole = member.Roles.Any(r => r.Id == 1370422448763437207);
            bool gamemodRole = member.Roles.Any(r => r.Id == 1474165198947356793);

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
                if (member.PermissionsIn(args.Channel).HasPermission(DiscordPermission.ModerateMembers)) return;
                if (staffRole) return;
                if (args.Message.Attachments.Count <= 0) await args.Message.DeleteAsync();
                else return;
            }

            if(args.Channel.Id == 1368208863018614866)
            {
                if(args.Message.MessageType == DiscordMessageType.ApplicationCommand) return;
                if(args.Author.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(DiscordPermission.ModerateMembers)) return;
                if (staffRole) return;
                else await args.Message.DeleteAsync();
            }

            return;
        }

        public static async void AddMessageToLevenshteinCache(ulong userId, string messageContent)
        {
            if(!Vars.levenshteinsCache.TryGetValue(userId, out var queue))
            {
                queue = new Queue<string>();
                Vars.levenshteinsCache[userId] = queue;
            }

            if(queue.Count >= 5)
            {
                queue.Dequeue();
            }

            queue.Enqueue(messageContent);
        }
    }
}
