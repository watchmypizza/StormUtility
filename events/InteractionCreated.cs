using System;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stormtility.config;
using Stormtility.commands.TicketCommands.autocompletehelper;
using Newtonsoft.Json;
using DSharpPlus.Net;

namespace Stormtility.events {
    public class InteractionCreated
    {
        public static DiscordClient Client;
        public static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
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
                            Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.",
                            Color = DiscordColor.Aquamarine
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
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
                            Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.",
                            Color = DiscordColor.Aquamarine
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    } else if (selected == "exploiter")
                    {
                        int ticketId = 0;

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating exploiter report channel...").AsEphemeral());

                        var jsonText = File.ReadAllText("tickets.json");
                        var json = JObject.Parse(jsonText);
                        var entries = (JArray)json["tickets"];

                        foreach (JObject entry in entries)
                        {
                            var supportObj = entry["exploiter"];
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
                        ulong staffRole = 1474165198947356793;
                        var staff = args.Interaction.Guild.GetRole(staffRole);
                        ulong ticketRole = 1387529661323677827;
                        var tickets = args.Interaction.Guild.GetRole(ticketRole);

                        var member = (DiscordMember)args.Interaction.User;

                        var userOverwrites = new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                        var channel = await args.Interaction.Guild.CreateChannelAsync($"exploiter-{ticketId}", ChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);

                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

                        var supportEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Exploiter Report",
                            Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.\nIn the meantime, please provide proof of the player using exploits.",
                            Color = DiscordColor.Red
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji:new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    } else if (selected == "appeal")
                    {
                        int ticketId = 0;

                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating ban appeal channel...").AsEphemeral());

                        var jsonText = File.ReadAllText("tickets.json");
                        var json = JObject.Parse(jsonText);
                        var entries = (JArray)json["tickets"];

                        foreach (JObject entry in entries)
                        {
                            var supportObj = entry["appeal"];
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
                        ulong staffRole = 1474165198947356793;
                        var staff = args.Interaction.Guild.GetRole(staffRole);
                        ulong ticketRole = 1387529661323677827;
                        var tickets = args.Interaction.Guild.GetRole(ticketRole);

                        var member = (DiscordMember)args.Interaction.User;

                        var userOverwrites = new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(Permissions.AccessChannels | Permissions.SendMessages | Permissions.AttachFiles | Permissions.EmbedLinks | Permissions.UseApplicationCommands | Permissions.ReadMessageHistory);
                        var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);

                        var channel = await args.Interaction.Guild.CreateChannelAsync($"appeal-{ticketId}", ChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);

                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

                        var supportEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Ban Appeal",
                            Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.\nIn the meantime, please provide proof of your innocence.",
                            Color = DiscordColor.Red
                        };

                        var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji:new DiscordComponentEmoji("⚠️"));

                        if (!vars.debugMode)
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddComponents(closeBtn));
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    }
                }

                if (args.Id == "closeticket")
                {
                    List<string> imageURL = [];

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

                    string[] ticketTypes = { "support", "exploiter", "speedtypist", "appeal" };

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
                        if (member.PermissionsIn(args.Interaction.Channel).HasPermission(Permissions.ModerateMembers) || member.Roles.Any(r => r.Id == 1474165198947356793))
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
                                imageURL = new List<string>();

                                foreach (var attachments in msg.Attachments)
                                {
                                    imageURL.Add(attachments.Url);
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
                                    ["content"] = $"{msg.Content} {string.Join(" ", imageURL)}",
                                    ["timestamp"] = msg.CreationTimestamp.UtcDateTime.ToString("o")
                                });
                            }
                            var botId = sender?.CurrentUser?.Id.ToString() ?? "0";

                            if (vars.debugMode)
                            {
                                Console.WriteLine(botId);
                            }

                            if (ticketTranscript[botId] == null)
                            {
                                ticketTranscript[botId] = new JObject
                                {
                                    ["messages"] = new JArray()
                                };
                            }

                            ((JArray)ticketTranscript[botId]!["messages"]).Add(new JObject
                            {
                                ["username"] = sender?.CurrentUser?.Username.ToString() ?? "No Username",
                                ["content"] = $"{args.Interaction.User.Username.ToString()} Closed the ticket.",
                                ["timestamp"] = args.Interaction.CreationTimestamp.UtcDateTime.ToString("o")
                            });

                            transcripts[$"{matchedPrefix}-{ticketId}"] = ticketTranscript;
                            json["transcripts"] = transcripts;

                            File.WriteAllText("transcripts.json", json.ToString());

                            autocompletehandler.LoadTranscriptCache("transcripts.json");

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
    }
}