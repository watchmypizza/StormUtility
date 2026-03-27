using System.Security.AccessControl;
using System.Text.Json;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using Stormtility.commands.TicketCommands.autocompletehelper;
using Stormtility.config;

namespace Stormtility.events
{
    public class AutoTranscript
    {
        public async Task<bool> ProcessTranscript(DiscordClient client, DiscordInteraction? interaction, DiscordChannel channel, string action = "auto")
        {
            ulong openTicketCategoryId = 1429871426386526310;

            if (channel.ParentId != openTicketCategoryId || channel.Name.Contains("closed-")) 
            {
                System.Console.WriteLine($"[Transcript Skipped] Channel {channel.Name} is not an open ticket.");
                return false; 
            }

            var openTickets = await File.ReadAllTextAsync("activetickets.json");
            var jsonTickets = JObject.Parse(openTickets);
            string channelName = "";
            long channelId = 0;
            bool isValidTicket = jsonTickets.Properties().Any(p => (long)p.Value["channel"] == (long)interaction.ChannelId);

            if (!isValidTicket)
            {
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("This channel is not a valid ticket channel.").AsEphemeral());
                return false;
            }

            if (action == "close")
            {
                if (channelName.Contains("closed-")) return false;

                var closeEmbed = new DiscordEmbedBuilder { Title = "Closing Ticket...", Description = "Saving transcript and archiving...", Color = DiscordColor.Red };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Closing...").AsEphemeral());
                await channel.SendMessageAsync(closeEmbed);

                ulong staffRoleId = 1370422448763437207;
                ulong closedCatId = 1429871639658762281;

                var staffOverwrites = new DiscordOverwriteBuilder(interaction.Guild.GetRole(staffRoleId)).Deny(Permissions.SendMessages).Allow(Permissions.AccessChannels);
                var everyoneOverwrites = new DiscordOverwriteBuilder(interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels);
                
                await channel.ModifyAsync(ch => {
                    ch.Name = channelName.Replace($"{matchedPrefix}-", $"closed-{matchedPrefix}-");
                    ch.Parent = interaction.Guild.GetChannel(closedCatId);
                    ch.PermissionOverwrites = new List<DiscordOverwriteBuilder> { staffOverwrites, everyoneOverwrites };
                });
            }
            else if (action == "delete")
            {
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Deleting...").AsEphemeral());
            }

            try
            {
                var jsonText = File.Exists("transcripts.json") ? File.ReadAllText("transcripts.json") : null;
                JObject json = string.IsNullOrEmpty(jsonText) ? new JObject { ["transcripts"] = new JObject() } : JObject.Parse(jsonText);
                
                if (json["transcripts"]?.Type == JTokenType.Array) json["transcripts"] = new JObject();
                JObject transcripts = (JObject)json["transcripts"]!;

                string ticketKey = $"{matchedPrefix}-{ticketId}";
                JObject ticketTranscript = (JObject)(transcripts[ticketKey] ?? new JObject());

                var messages = await channel.GetMessagesAsync(action == "auto" ? 100 : 1000);
                var orderedMessages = messages.OrderBy(m => m.CreationTimestamp).ToList();

                foreach (var msg in orderedMessages)
                {
                    if (msg.Author.IsBot) continue;

                    string userId = msg.Author.Id.ToString();
                    if (ticketTranscript[userId] == null) ticketTranscript[userId] = new JObject { ["messages"] = new JArray() };

                    JArray userMsgs = (JArray)ticketTranscript[userId]["messages"]!;
                    
                    if (!userMsgs.Any(m => (string)m["messageId"] == msg.Id.ToString()))
                    {
                        var attachments = string.Join(" ", msg.Attachments.Select(a => a.Url));
                        userMsgs.Add(new JObject
                        {
                            ["username"] = msg.Author.Username,
                            ["content"] = $"{msg.Content} {attachments}".Trim(),
                            ["timestamp"] = msg.CreationTimestamp.UtcDateTime.ToString("o"),
                            ["messageId"] = msg.Id.ToString()
                        });
                    }
                }

                if (action != "auto")
                {
                    string botId = client.CurrentUser.Id.ToString();
                    if (ticketTranscript[botId] == null) ticketTranscript[botId] = new JObject { ["messages"] = new JArray() };
                    
                    ((JArray)ticketTranscript[botId]["messages"]).Add(new JObject
                    {
                        ["username"] = "System",
                        ["content"] = $"{interaction.User.Username} performed action: {action.ToUpper()}",
                        ["timestamp"] = DateTime.UtcNow.ToString("o"),
                        ["messageId"] = $"SYS-{DateTime.UtcNow.Ticks}"
                    });
                }

                transcripts[ticketKey] = ticketTranscript;
                File.WriteAllText("transcripts.json", json.ToString(Newtonsoft.Json.Formatting.Indented));
                autocompletehandler.LoadTranscriptCache("transcripts.json");

                if (action == "delete") await channel.DeleteAsync();

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Transcript Error: {e}");
                return false;
            }
        }
    }
}