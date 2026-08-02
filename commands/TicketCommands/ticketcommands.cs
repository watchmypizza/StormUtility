using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stormtility.commands.TicketCommands.autocompletehelper;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using Stormtility.config;
using System.Linq.Expressions;
using Stormtility.events;
using System.Text.Json.Nodes;
using DSharpPlus.Net.Models;
using System.Net.Http.Json;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;

namespace Stormtility.commands.TicketCommands
{
    public class ticketcommands
    {
        [SlashCommandGroup("ticket", "ticket system")]
        [Command("ticket")]
        [Description("ticket system")]
        public class ticketingCommands
        {
            public static async Task<bool> permissionCheck(SlashCommandContext ctx)
            {
                bool gamemodRole = ctx.Member.Roles.Any(r => r.Id == 1474165198947356793);
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(DiscordPermission.ModerateMembers) && !gamemodRole)
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return false;
                }
                return true;
            }

            [SlashCommand("setup", "Set the ticket system up")]
            [Command("setup")]
            [Description("Set the ticket system up.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task ticketsetup(SlashCommandContext ctx)
            {
                try
                {
                    var check = await permissionCheck(ctx);

                    if(!check) { return; }

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Setting up...").AsEphemeral());

                    var select = new DiscordSelectComponent(
                        customId: "ticketoptions",
                        placeholder: "Choose an option...",
                        options: new List<DiscordSelectComponentOption>()
                        {
                            new("Support", "support", "Create a ticket for server help! (ex. reporting members, general questions for staff)", emoji:new DiscordComponentEmoji("🔧")), // emoji:new DiscordComponentEmoji("🔧")
                            new("Speedtypist Application", "speedtypist", "Create a ticket to apply for the 'Speedtypist' role.", emoji:new DiscordComponentEmoji("⌨️")), // emoji:new DiscordComponentEmoji("⌨️")
                            //new("Exploit Report", "exploiter", "Create a ticket to report an exploiter in-game.", emoji:new DiscordComponentEmoji("🚨")), // emoji:new DiscordComponentEmoji("🚨")
                        });

                    var supportEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Stormy's Shelter Support",
                        Description = "To create a ticket, make a selection below.",
                        Color = DiscordColor.Gold
                    };

                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .AddEmbed(supportEmbed)
                        .AddActionRowComponent(select));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            [SlashCommand("update", "Update the last ticket message when adding new options.")]
            [Command("update")]
            [Description("Update the last ticket message when adding new options.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task update(SlashCommandContext ctx)
            {
                bool isAdmin = await permissionCheck(ctx);
                if(!isAdmin) { return; }

                var pins = await ctx.Interaction.Channel.GetPinnedMessagesAsync();
                DiscordMessage discordMessage = pins[0];

                var select = new DiscordSelectComponent(
                customId: "ticketoptions",
                placeholder: "Choose an option...",
                options: new List<DiscordSelectComponentOption>()
                {
                    new("Support", "support", "Create a ticket for server help! (ex. reporting members, general questions for staff)", emoji:new DiscordComponentEmoji("🔧")), // emoji:new DiscordComponentEmoji("🔧")
                    new("Speedtypist Application", "speedtypist", "Create a ticket to apply for the 'Speedtypist' role.", emoji:new DiscordComponentEmoji("⌨️")), // emoji:new DiscordComponentEmoji("⌨️")
                    new("Exploiter Report", "exploiter", "Create a ticket to report an in-game exploiter.", emoji:new DiscordComponentEmoji("🚨")), // emoji:new DiscordComponentEmoji("🚨")
                    new("Game Appeal", "appeal", "Create a ticket to appeal an in-game ban.", emoji:new DiscordComponentEmoji("🔎")),
                });

                var supportEmbed = new DiscordEmbedBuilder
                {
                    Title = "Stormy's Shelter Support",
                    Description = "To create a ticket, make a selection below.",
                    Color = DiscordColor.Gold
                };

                await discordMessage.ModifyAsync(new DiscordMessageBuilder()
                    .AddEmbed(supportEmbed)
                    .AddActionRowComponent(select));

                await ctx.RespondAsync("Updated the message successfully.", true);
            }

            [SlashCommand("transcript", "View the transcript of a ticket")]
            [Command("transcript")]
            [Description("View the transcript of a ticket")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task viewTranscript(SlashCommandContext ctx, [Parameter("ticketId")][Description("The ID of the ticket to view")][Autocomplete(typeof(autocompletehandler))] string ticketId)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                await ctx.DeferResponseAsync(ephemeral: false);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Preparing transcript..."));

                var jsonText = File.Exists("transcripts.json") ? File.ReadAllText("transcripts.json") : null;

                if (string.IsNullOrEmpty(jsonText))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No transcripts found."));
                    return;
                }

                JObject json = JObject.Parse(jsonText);

                if (json["transcripts"]?[ticketId.ToString()] == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"No transcript found for ticket ID {ticketId}"));
                    return;
                }

                JObject ticketTranscript = (JObject)json?["transcripts"]?[ticketId.ToString()];

                var allMsg = new List<(string userId, string username, string content, DateTime timestamp)>();

                foreach(var userProp in ticketTranscript.Properties())
                {
                    string userId = userProp.Name;
                    var userMessages = userProp.Value["messages"] as JArray;

                    foreach(var msgObj in userMessages!)
                    {
                        string content = msgObj["content"]?.ToString() ?? "";
                        string timestampStr = msgObj["timestamp"]?.ToString() ?? "";
                        string username = msgObj["username"]?.ToString() ?? "";
                        if (DateTime.TryParse(timestampStr, out var timestamp))
                        {
                            allMsg.Add((userId, username, content, timestamp));
                        }
                    }
                }

                var orderedMsgs = allMsg.OrderBy(m => m.timestamp).ToList();

                var sb = new StringBuilder();
                foreach(var (userId, username, content, timestamp) in orderedMsgs)
                {
                    sb.AppendLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] <@{userId}> {username}: {content}");
                }

                var transcriptText = sb.ToString();
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(transcriptText));

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile($"transcript-{ticketId}.txt", ms));
            }

            [SlashCommand("close", "Prompt to close the ticket")]
            [Command("close")]
            [Description("Prompt to close the ticket")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task closeTicket(SlashCommandContext ctx)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title="Close Ticket",
                    Color=DiscordColor.Red
                };
                var button = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AddActionRowComponent(button).AsEphemeral());
            }

            [SlashCommand("delete", "Delete the current ticket.")]
            [Command("delete")]
            [Description("Delete the current ticket")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task deleteTicket(SlashCommandContext ctx)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Delete Ticket",
                    Description = "Are you sure you want to delete this ticket? This action cannot be undone.",
                    Color = DiscordColor.Red
                };
                var button = new DiscordButtonComponent(DiscordButtonStyle.Danger, "deleteticket", "Delete Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AddActionRowComponent(button).AsEphemeral());
            }

            [SlashCommand("claim", "Claim the ticket you are currently in.")]
            [Command("claim")]
            [Description("Claim the ticket you are currently in.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task claim(SlashCommandContext ctx)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "speedtypist", "appeal", "exploiter" };

                string matchedPrefix = null;
                int ticketId = 0;

                bool regExChannelName = false;

                //bool regExChannelName = Regex.IsMatch(channelName, @"^support-\d+$");
                //var match = Regex.Match(channelName, @"^support-(\d+)$");

                foreach (var type in ticketTypes)
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

                if (!regExChannelName)
                {
                    var noTicketEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Please use this inside of an open ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Successfully claimed the ticket."
                };
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());
                successEmbed.Title = $"{ctx.Member.Nickname} has claimed this ticket.";
                successEmbed.Color = DiscordColor.Gold;
                await ctx.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(successEmbed));
                await ctx.Interaction.Channel.ModifyAsync(c => c.Name = $"{ctx.User.Username}-{matchedPrefix}-{ticketId}");
                return;
            }

            [SlashCommand("add", "Add a user to the current ticket.")]
            [Command("add")]
            [Description("Add a user to the current ticket")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task addUser(SlashCommandContext ctx, [Parameter("user")][Description("The user to add to the ticket.")] DiscordUser user)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                var member = (DiscordMember)user;

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "appeal", "speedtypist", "exploiter" };

                string matchedPrefix = null;
                int ticketId = 0;

                bool regExChannelName = false;

                foreach (var type in ticketTypes)
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

                if (!regExChannelName)
                {
                    var noTicketEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Please use this inside of an open ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }

                bool overwrites = ctx.Interaction.Channel.PermissionOverwrites.Any(o => o.Id == member.Id);

                if (overwrites)
                {
                    var failEmbed = new DiscordEmbedBuilder
                    {
                        Title = "User is already added to the ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(failEmbed).AsEphemeral());
                    return;
                }

                DiscordPermissions allowPerms = new([DiscordPermission.ViewChannel, DiscordPermission.ReadMessageHistory, DiscordPermission.AttachFiles, DiscordPermission.AddReactions, DiscordPermission.EmbedLinks, DiscordPermission.SendMessages]);

                await ctx.Interaction.Channel.AddOverwriteAsync(member, allowPerms, DiscordPermissions.None);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully added {member.Username} to the ticket.",
                    Color = DiscordColor.Gold
                };

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            }

            [SlashCommand("remove", "Remove a user from the current ticket")]
            [Command("remove")]
            [Description("Remove a user from the current ticket.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task removeUser(SlashCommandContext ctx, [Parameter("User")][Description("The user to remove")] DiscordUser user)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                var member = (DiscordMember)user;

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "appeal", "speedtypist", "exploiter" };

                string matchedPrefix = null;
                int ticketId = 0;

                bool regExChannelName = false;

                foreach (var type in ticketTypes)
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

                if (!regExChannelName)
                {
                    var noTicketEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Please use this inside of an open ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }

                bool overwrites = ctx.Interaction.Channel.PermissionOverwrites.Any(o => o.Id == member.Id);

                if (!overwrites)
                {
                    var failEmbed = new DiscordEmbedBuilder
                    {
                        Title = "User is not added to the ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(failEmbed).AsEphemeral());
                    return;
                }

                await ctx.Interaction.Channel.DeleteOverwriteAsync(member);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully removed {member.Username} to the ticket.",
                    Color = DiscordColor.Gold
                };

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            }

            [SlashCommand("staff", "Create a plain ticket.")]
            [Command("staff")]
            [Description("Create a plain ticket.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task staffTicket(SlashCommandContext ctx)
            {
                var check = await permissionCheck(ctx);

                if (!check) { return; }

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Creating channel...").AsEphemeral());

                var openStaffTicketCategory = await ctx.Interaction.Guild.GetChannelAsync(1429871426386526310);

                if (openStaffTicketCategory == null) { return; }

                int ticketId = 0;

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

                ulong staffRole = 1370422448763437207;
                var staff = await ctx.Interaction.Guild.GetRoleAsync(staffRole);

                var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
                var defaultRoleOverwrites = new DiscordOverwriteBuilder(ctx.Interaction.Guild.EveryoneRole).Deny(DiscordPermission.ViewChannel);

                try {
                    var channel = await ctx.Guild.CreateTextChannelAsync($"support-{ticketId}", openStaffTicketCategory, overwrites: [staffOverwrites, defaultRoleOverwrites]);
                    
                    var supportEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Support Ticket",
                        Description = "Staff will assist you shortly, please give us a few minutes to respond.",
                        Color = DiscordColor.Aquamarine
                    };

                    var closeBtn = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                    await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created in {channel.Mention}."));
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--- STACK BEGIN ---");
                    Console.WriteLine(e);
                    Console.WriteLine("---  STACK END  ---");
                    Console.ResetColor();
                    try 
                    {
                        using HttpClient httpClient = new();
                        var url = Program.webhookURL;

                        var payload = new
                        {
                            content = $"**[ERROR]** {DateTime.Now}\n```\n{e.Message}\n{e.StackTrace?[..Math.Min(e.StackTrace.Length, 1500)]}\n```"
                        };

                        string errorjson = JsonSerializer.Serialize(payload);

                        using StringContent stringContent = new(errorjson, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync(url, stringContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[Webhook Error] Discord returned: {response.StatusCode}");
                        }
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"FAILED TO SEND WEBHOOK: {logEx.Message}");
                    }
                }

            }

            public async Task<bool> checkChannelName(SlashCommandContext ctx, DiscordChannel channel)
            {
                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "appeal", "speedtypist", "exploiter" };

                string matchedPrefix = null;
                int ticketId = 0;

                bool regExChannelName = false;

                foreach (var type in ticketTypes)
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

                if (!regExChannelName)
                {
                    var noTicketEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Please use this inside of an open ticket.",
                        Color = DiscordColor.Red
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return false;
                }
                return true;
            }

            [SlashCommand("addrole", "Add a role to the current ticket.")]
            [Command("addrole")]
            [Description("Add a role and their members into the ticket")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task addrole(SlashCommandContext ctx, [Parameter("role")][Description("The role to add to the current ticket.")] DiscordRole role)
            {
                bool permissions = await permissionCheck(ctx);
                if(!permissions) { return; }

                await ctx.DeferResponseAsync(true);

                bool isTicket = await checkChannelName(ctx, ctx.Interaction.Channel);

                if (!isTicket) { return; }

                await ctx.Interaction.Channel.AddOverwriteAsync(role, allow: new([DiscordPermission.SendMessages, DiscordPermission.ViewChannel, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks]));

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Added role {role.Name} to the channel",
                    Color = DiscordColor.Gold
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            [SlashCommand("removerole", "Remove a role from the current ticket")]
            [Command("removerole")]
            [Description("Remove a role and their members from the curent ticket.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public async Task removerole(SlashCommandContext ctx, [Parameter("role")][Description("The role to remove from the current ticket.")] DiscordRole role)
            {
                bool permissions = await permissionCheck(ctx);

                if (!permissions) { return; }

                await ctx.DeferResponseAsync(true);

                bool isTicket = await checkChannelName(ctx, ctx.Interaction.Channel);

                if (!isTicket) return;

                await ctx.Interaction.Channel.DeleteOverwriteAsync(role);

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Removed role {role.Name} from the channel",
                    Color = DiscordColor.Gold
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            //[SlashCommand("bulkdelete", "Bulk delete closed tickets in the closed tickets category.")]
            public async Task bulkDelete(SlashCommandContext ctx)
            {
                bool permissions = await permissionCheck(ctx);
                await ctx.DeferResponseAsync(true);

                if (!permissions) { return; }

                var button = new DiscordButtonComponent(DiscordButtonStyle.Danger, "bulkdelete", "Confirm Bulk Delete", emoji: new DiscordComponentEmoji("⚠️"));
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Bulk Delete Closed Tickets",
                    Description = "This will bulk delete all closed tickets in the closed tickets category. This action cannot be undone. Are you sure you want to proceed?",
                    Color = DiscordColor.Red
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddActionRowComponent(button));
                return;
            }

            [SlashCommand("forcedelete", "Force delete a ticket channel.")]
            [Command("forcedelete")]
            [Description("Force delete the current ticket you are in.")]
            public async Task forceDelete(SlashCommandContext ctx)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AsEphemeral());

                bool isAdmin = await permissionCheck(ctx);
                if(!isAdmin) 
                { 
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You do not have permission."));
                    return; 
                }

                Regex regex = new Regex("closed-[A-Za-z]+-(\\d+)");
                DiscordChannel channel = ctx.Interaction.Channel;
                var chName = channel.Name;

                if(regex.IsMatch(chName))
                {
                    AutoTranscript autoTranscript = new();
                    bool result = await autoTranscript.ProcessTranscript(ctx.Client, ctx.Interaction, channel, "delete");
                    
                    if (!result)
                    {
                        var errorEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "An unknown error occured.",
                            Color = DiscordColor.Red
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    }
                }
                else 
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This is not a valid ticket channel."));
                }
            }
        }
    }
}
