using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.commands;
using Stormtility.commands.Anti_Raid_Commands;
using Stormtility.commands.EconomyCommands;
using Stormtility.commands.MakeItAQuote;
using Stormtility.commands.TicketCommands;
using Stormtility.config;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stormtility
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension CommandBuilder { get; set; }
        private static SlashCommandsExtension Slash { get; set; }
        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            Client = new DiscordClient(discordConfig);
            Client.Ready += Client_Ready;
            Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            Client.MessageCreated += Client_MessageCreated;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;

            JobCache.Load();
            _ = JobPayout.RunDailyJobAsyncPayout();

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };
            CommandBuilder = Client.UseCommandsNext(commandsConfig);
            CommandBuilder.RegisterCommands<TestCommands>();

            Slash = Client.UseSlashCommands();
            Slash.RegisterCommands<slashcommands>();
            Slash.RegisterCommands<EconomyCommands>();
            Slash.RegisterCommands<ticketcommands>();
            Slash.RegisterCommands<antiraidcommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            try
            {
                if (args.Id == "ticketoptions")
                {
                    var selected = args.Values.FirstOrDefault();

                    if (selected == "support")
                    {
                        int ticketId = 0;

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating support channel...").AsEphemeral());

                        var jsonText = File.ReadAllText("tickets.json");
                        var json = JObject.Parse(jsonText);
                        var entries = (JArray)json["tickets"];

                        foreach (JObject entry in entries)
                        {
                            var supportObj = entry["support"];
                            if (supportObj != null)
                            {
                                ticketId = supportObj["id"]!.Value<int>();
                                ticketId += 1;
                                supportObj["id"] = ticketId;
                            }
                            break;
                        }

                        File.WriteAllText("tickets.json", json.ToString());

                        ulong catId = 1429871426386526310;
                        var cat = args.Interaction.Guild.GetChannel(catId);
                        ulong staffRole = 1370422448763437207;
                        var staff = args.Interaction.Guild.GetRole(staffRole);
                        ulong ticketRole = 1387529661323677827;
                        var tickets = args.Interaction.Guild.GetRole(ticketRole);

                        var member = (DiscordMember)args.Interaction.User;

                        var userOverwrites = new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                        var channel = await args.Interaction.Guild.CreateChannelAsync($"support-{ticketId}", ChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);

                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

                        var supportEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Support Ticket",
                            Description = "Staff will assist you shortly, please give us a few minutes to respond.",
                            Color = DiscordColor.Aquamarine
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}"));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    }
                    else if (selected == "speedtypist")
                    {
                        int ticketId = 0;

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating speedtypist channel...").AsEphemeral());

                        var jsonText = File.ReadAllText("tickets.json");
                        var json = JObject.Parse(jsonText);
                        var entries = (JArray)json["tickets"];

                        foreach (JObject entry in entries)
                        {
                            var supportObj = entry["speedtypist"];
                            if (supportObj != null)
                            {
                                ticketId = supportObj["id"]!.Value<int>();
                                ticketId += 1;
                                supportObj["id"] = ticketId;
                            }
                            break;
                        }

                        File.WriteAllText("tickets.json", json.ToString());

                        ulong catId = 1429871426386526310;
                        var cat = args.Interaction.Guild.GetChannel(catId);
                        ulong staffRole = 1370422448763437207;
                        var staff = args.Interaction.Guild.GetRole(staffRole);
                        ulong ticketRole = 1387529661323677827;
                        var tickets = args.Interaction.Guild.GetRole(ticketRole);

                        var member = (DiscordMember)args.Interaction.User;

                        var userOverwrites = new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                        var channel = await args.Interaction.Guild.CreateChannelAsync($"speedtypist-{ticketId}", ChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);

                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

                        var supportEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Speedtypist Application",
                            Description = "Staff will assist you shortly, please give us a few minutes to respond.",
                            Color = DiscordColor.Aquamarine
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}"));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    } else if (selected == "giveaway")
                    {
                        int ticketId = 0;

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating giveaway channel...").AsEphemeral());

                        var jsonText = File.ReadAllText("tickets.json");
                        var json = JObject.Parse(jsonText);
                        var entries = (JArray)json["tickets"];

                        foreach (JObject entry in entries)
                        {
                            var supportObj = entry["giveaway"];
                            if (supportObj != null)
                            {
                                ticketId = supportObj["id"]!.Value<int>();
                                ticketId += 1;
                                supportObj["id"] = ticketId;
                            }
                            break;
                        }

                        File.WriteAllText("tickets.json", json.ToString());

                        ulong catId = 1429871426386526310;
                        var cat = args.Interaction.Guild.GetChannel(catId);
                        ulong staffRole = 1370422448763437207;
                        var staff = args.Interaction.Guild.GetRole(staffRole);
                        ulong ticketRole = 1387529661323677827;
                        var tickets = args.Interaction.Guild.GetRole(ticketRole);

                        var member = (DiscordMember)args.Interaction.User;

                        var userOverwrites = new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                        var channel = await args.Interaction.Guild.CreateChannelAsync($"giveaway-{ticketId}", ChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);

                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

                        var supportEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Giveaway Support",
                            Description = "Staff will assist you shortly, please give us a few minutes to respond.",
                            Color = DiscordColor.Aquamarine
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji:new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}"));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    }
                }

                if(args.Id == "closeticket")
                {
                    var channelName = args.Interaction.Channel.Name;

                    if(channelName.Contains("closed-"))
                    {
                        var closedEmbed = new DiscordEmbedBuilder
                        {
                            Title = "This ticket is already closed.",
                            Color = DiscordColor.Red
                        };

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(closedEmbed).AsEphemeral());
                        return;
                    }

                    string[] ticketTypes = { "support", "giveaway", "speedtypist" };

                    string matchedPrefix = null;
                    int ticketId = 0;

                    bool regExChannelName = false;

                    //bool regExChannelName = Regex.IsMatch(channelName, @"^support-\d+$");
                    //var match = Regex.Match(channelName, @"^support-(\d+)$");

                    foreach(var type in ticketTypes)
                    {
                        var regex = new Regex($@"^(?:.+-)?{type}-(\d+)$");
                        var match = regex.Match(channelName);

                        if (match.Success)
                        {
                            matchedPrefix = type;
                            regExChannelName = true;
                            ticketId = int.Parse(match.Groups[1].Value);
                            break;
                        }
                    }

                    if (regExChannelName)
                    {
                        ulong closedCat = 1429871639658762281;
                        var closedCategory = args.Interaction.Guild.GetChannel(closedCat);
                        var member = (DiscordMember)args.Interaction.User;
                        if (member.PermissionsIn(args.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = "Closing Ticket...",
                                Description = "Adding transcript to log and then closing...",
                                Color = DiscordColor.Red
                            };
                            var newChName = channelName.Replace($"{matchedPrefix}-", $"closed-{matchedPrefix}-");

                            ulong staff = 1370422448763437207;
                            var staffRole = args.Interaction.Guild.GetRole(staff);
                            var staffOverwrites = new DiscordOverwriteBuilder(staffRole).Deny(Permissions.SendMessages).Allow(Permissions.AccessChannels);
                            var defaultOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                            await args.Interaction.Channel.ModifyAsync(ch => ch.Name = newChName);
                            await args.Interaction.Channel.ModifyAsync(ch => ch.Parent = closedCategory);
                            await args.Interaction.Channel.ModifyAsync(ch => ch.PermissionOverwrites = [staffOverwrites, defaultOverwrites]);
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Closing...").AsEphemeral());
                            await args.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(embed));

                            var jsonText = File.Exists("transcripts.json") ? File.ReadAllText("transcripts.json") : null;
                            JObject json;

                            if (string.IsNullOrEmpty(jsonText))
                            {
                                json = new JObject
                                {
                                    ["transcripts"] = new JObject()
                                };
                            }
                            else
                            {
                                json = JObject.Parse(jsonText);

                                if (json["transcripts"]?.Type == JTokenType.Array)
                                {
                                    json["transcripts"] = new JObject();
                                }
                            }

                            JObject transcripts = (JObject)(json["transcripts"] ?? new JObject());

                            var messages = await args.Interaction.Channel.GetMessagesAsync(1000);
                            var orderedMessages = messages.Reverse().ToList();

                            JObject ticketTranscript = new JObject();
                            
                            foreach(var msg in orderedMessages)
                            {
                                if (string.IsNullOrWhiteSpace(msg.Content))
                                {
                                    continue;
                                }
                                if(msg.Author.IsBot)
                                {
                                    continue;
                                }

                                string userId = msg.Author.Id.ToString();
                                if (ticketTranscript[userId] == null)
                                {
                                    ticketTranscript[userId] = new JObject
                                    {
                                        ["messages"] = new JArray()
                                    };
                                }

                                ((JArray)ticketTranscript[userId]!["messages"]).Add(new JObject
                                {
                                    ["username"] = msg.Author.Username,
                                    ["content"] = msg.Content,
                                    ["timestamp"] = msg.CreationTimestamp.UtcDateTime.ToString("o")
                                });
                            }

                            transcripts[$"{matchedPrefix}-{ticketId}"] = ticketTranscript;
                            json["transcripts"] = transcripts;

                            File.WriteAllText("transcripts.json", json.ToString());

                            return;
                        } else
                        {
                            var permissionErrorEmbed = new DiscordEmbedBuilder
                            {
                                Title = "404 - No Permissions",
                                Description = "You do not have the necessary permissions to perform this action.",
                                Color = DiscordColor.Red
                            };
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                            return;
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<bool> checkMessage(DiscordMessage message)
        {
            List<string> blacklistedWords = new List<string> {"18", "e-girl", "condo", "csam", "dropbox", "porn", "rape", "erp", "erotic", "hanime", "hentai"};

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

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
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

            string[] ticketTypes = { "support", "giveaway", "speedtypist" };

            var channelName = args.Channel.Name;

            string matchedPrefix = null;
            int ticketId = 0;

            bool staffRole = member.Roles.Any(r => r.Id == 1370422448763437207);

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

                    if (staffRole)
                    {
                        await args.Channel.ModifyAsync(c => c.Name = $"{args.Author.Username}-{matchedPrefix}-{ticketId}");
                        break;
                    }
                }
            }

            if (args.Channel.Id == 1450164600384258058)
            {
                bool staff = member.Roles.Any(r => r.Id == 1370422448763437207);
                if (args.Author.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers)) return;
                if (staff) return;
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

        private static Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
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


        private static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            await sender.UpdateStatusAsync(new DiscordActivity("Watching stitchy", ActivityType.Watching), UserStatus.DoNotDisturb, DateTimeOffset.UtcNow);
            Console.WriteLine($"Logged in as {Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}");
            return;
        }
    }
}
