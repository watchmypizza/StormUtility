using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stormtility.commands.TicketCommands
{
    public class ticketcommands : ApplicationCommandModule
    {
        [SlashCommandGroup("ticket", "ticket system")]
        public class ticketingCommands : ApplicationCommandModule
        {
            [SlashCommand("setup", "Set the ticket system up")]
            public async Task ticketsetup(InteractionContext ctx)
            {
                try
                {
                    if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.All))
                    {
                        var permissionError = new DiscordEmbedBuilder
                        {
                            Title = "404 - No Permissions",
                            Description = "You do not have the necessary permissions to use this command.",
                            Color = DiscordColor.Red
                        };

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionError).AsEphemeral());
                        return;
                    }

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Setting up...").AsEphemeral());

                    var select = new DiscordSelectComponent(
                        customId: "ticketoptions",
                        placeholder: "Choose an option...",
                        options: new List<DiscordSelectComponentOption>()
                        {
                            new("Support", "support", "Create a ticket for server help! (ex. reporting members, general questions for staff)", emoji:new DiscordComponentEmoji("🔧")), // emoji:new DiscordComponentEmoji("🔧")
                            new("Speedtypist Application", "speedtypist", "Create a ticket to apply for the 'Speedtypist' role.", emoji:new DiscordComponentEmoji("⌨️")), // emoji:new DiscordComponentEmoji("⌨️")
                        });

                    var supportEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Stormy's Shelter Support",
                        Description = "To create a ticket, make a selection below.",
                        Color = DiscordColor.Gold
                    };

                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithEmbed(supportEmbed)
                        .AddComponents(select));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            [SlashCommand("transcript", "View the transcript of a ticket")]
            public async Task viewTranscript(InteractionContext ctx, [Option("ID", "The ID of the ticket transcript to view.")] string ticketId)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return;
                }

                await ctx.DeferAsync(ephemeral: false);
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

                JObject ticketTranscript = (JObject)json["transcripts"][ticketId.ToString()];

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
            public async Task closeTicket(InteractionContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return;
                }

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "giveaway", "speedtypist" };

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

                if(!regExChannelName)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Please execute this command in an open ticket.").AsEphemeral());
                    return;
                }

                var closeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Are you sure you want to close the ticket?",
                    Color = DiscordColor.Red
                };

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(closeBtn).AsEphemeral());
            }

            [SlashCommand("claim", "Claim the ticket you are currently in.")]
            public async Task claim(InteractionContext ctx)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return;
                }

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "giveaway", "speedtypist" };

                string matchedPrefix = null;
                int ticketId = 0;

                bool regExChannelName = false;

                //bool regExChannelName = Regex.IsMatch(channelName, @"^support-\d+$");
                //var match = Regex.Match(channelName, @"^support-(\d+)$");

                foreach (var type in ticketTypes)
                {
                    var regex = new Regex($@"^{type}-(\d+)$");
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

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Successfully claimed the ticket."
                };
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());
                successEmbed.Title = $"{ctx.Member.Nickname} has claimed this ticket.";
                successEmbed.Color = DiscordColor.Gold;
                await ctx.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(successEmbed));
                await ctx.Interaction.Channel.ModifyAsync(c => c.Name = $"{ctx.User.Username}-{matchedPrefix}-{ticketId}");
                return;
            }

            [SlashCommand("add", "Add a user to the current ticket.")]
            public async Task addUser(InteractionContext ctx, [Option("user", "The user to add to the ticket.")] DiscordUser user)
            {
                if(!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return;
                }

                var member = (DiscordMember)user;

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "giveaway", "speedtypist" };

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

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }
                var allowPerms = Permissions.AccessChannels | Permissions.ReadMessageHistory | Permissions.AttachFiles | Permissions.AddReactions | Permissions.EmbedLinks | Permissions.SendMessages;

                await ctx.Interaction.Channel.AddOverwriteAsync(member, allowPerms, Permissions.None);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully added {member.Username} to the ticket.",
                    Color = DiscordColor.Gold
                };

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            }

            [SlashCommand("remove", "Remove a user from the current ticket")]
            public async Task removeUser(InteractionContext ctx, [Option("User", "The user to remove")] DiscordUser user)
            {
                if (!ctx.Member.PermissionsIn(ctx.Interaction.Channel).HasPermission(Permissions.ModerateMembers))
                {
                    var permissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionErrorEmbed).AsEphemeral());
                    return;
                }

                var member = (DiscordMember)user;

                var channelName = ctx.Interaction.Channel.Name;
                string[] ticketTypes = { "support", "giveaway", "speedtypist" };

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

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(noTicketEmbed).AsEphemeral());
                    return;
                }

                await ctx.Interaction.Channel.DeleteOverwriteAsync(member);

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Successfully removed {member.Username} to the ticket.",
                    Color = DiscordColor.Gold
                };

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());

                return;
            }
        }
    }
}
