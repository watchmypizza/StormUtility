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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Stormtility.events {
    public class InteractionCreated
    {
        public static DiscordClient Client;

        public static async Task<bool> insertActiveTicket(DiscordUser user, DiscordChannel ticket)
        {
            try {
                var jsonText = File.ReadAllText("activetickets.json");
                var json = JObject.Parse(jsonText);
                var entries = (JObject)json;

                if(entries?[$"{user.Id}"] != null) return false;

                entries?[$"{user.Id}"] = new JObject
                {
                    ["channel"] = ticket.Id
                };

                File.WriteAllText("activetickets.json", entries.ToString());
                return true;
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();
                return false;
            }
        }

        public static async Task<string> getActiveTicket(DiscordUser user)
        {
            try
            {
                var jsonText = File.ReadAllText("activetickets.json");
                var json = JObject.Parse(jsonText);
                var entries = (JObject)json;

                if(entries?[$"{user.Id}"] == null) return string.Empty;

                var chId = entries?[$"{user.Id}"]["channel"];

                if(chId == null) return string.Empty;

                return chId.ToString();
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();
                return string.Empty;
            }
        }

        public static async Task<bool> deleteActiveTicketEntry(DiscordChannel channel)
        {
            try
            {
                var jsonText = File.ReadAllText("activetickets.json");
                var json = (JObject)JObject.Parse(jsonText);
                var entries = json;
                
                foreach(var prop in json.Properties())
                {
                    var entry = (JObject)prop.Value;
                    var chId = entry["channel"]?.ToString();

                    if(chId == channel.Id.ToString())
                    {
                        prop.Remove();
                        File.WriteAllText("activetickets.json", json.ToString());
                        return true;
                    }
                }

                return false;
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();
                return false;
            }
        }

        public static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            try
            {
                if (args.Id == "ticketoptions")
                {
                    var selected = args.Values.FirstOrDefault();

                    if (selected == "support")
                    {
                        string isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != string.Empty)
                        {
                            ulong.TryParse(isTicketOpenAlready, out ulong chId);
                            DiscordChannel ch = await sender.GetChannelAsync(chId);
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                            return;
                        }

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
                        _ = await insertActiveTicket(args.Interaction.User, channel);

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
                        string isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != string.Empty)
                        {
                            ulong.TryParse(isTicketOpenAlready, out ulong chId);
                            DiscordChannel ch = await sender.GetChannelAsync(chId);
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                            return;
                        }

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
                        _ = await insertActiveTicket(args.Interaction.User, channel);

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
                        string isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != string.Empty)
                        {
                            ulong.TryParse(isTicketOpenAlready, out ulong chId);
                            DiscordChannel ch = await sender.GetChannelAsync(chId);
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                            return;
                        }

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
                        _ = await insertActiveTicket(args.Interaction.User, channel);

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
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{staff.Mention}").WithAllowedMentions(Mentions.All));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    } else if (selected == "appeal")
                    {
                        string isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != string.Empty)
                        {
                            ulong.TryParse(isTicketOpenAlready, out ulong chId);
                            DiscordChannel ch = await sender.GetChannelAsync(chId);
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                            return;
                        }

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
                        _ = await insertActiveTicket(args.Interaction.User, channel);

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
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{staff.Mention}").WithAllowedMentions(Mentions.All));
                        } else
                        {
                            await channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(supportEmbed).AddComponents(closeBtn));
                        }

                        return;
                    }
                }

                if (args.Id == "deleteticket")
                {
                    _ = await deleteActiveTicketEntry(args.Interaction.Channel);
                    var autoTranscript = new AutoTranscript();
                    bool result = await autoTranscript.ProcessTranscript(sender, args.Interaction, args.Channel, "delete");
                    if (!result)
                    {
                        var errorEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "This ticket cannot be deleted.",
                            Color = DiscordColor.Red
                        };
                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                    }
                }

                if (args.Id == "closeticket")
                {
                    var member = (DiscordMember)args.Interaction.User;
                    _ = await deleteActiveTicketEntry(args.Interaction.Channel);
                    if(!member.PermissionsIn(args.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                    {
                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You cannot close this ticket.").AsEphemeral(true));
                        return;
                    }

                    var autoTranscript = new AutoTranscript();
                    bool result = await autoTranscript.ProcessTranscript(sender, args.Interaction, args.Channel, "close");
                    if (!result)
                    {
                        var errorEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "This ticket cannot be closed.",
                            Color = DiscordColor.Red
                        };
                        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                        System.Console.WriteLine($"ERROR: {result}");
                    }
                }

            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}