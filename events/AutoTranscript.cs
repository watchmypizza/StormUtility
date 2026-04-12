using System.Text.Json;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.commands.TicketCommands.autocompletehelper;

namespace Stormtility.events
{
    public class AutoTranscript
    {
        
        private const ulong OpenTicketCategoryId = 1429871426386526310;
        private const ulong ClosedTicketCategoryId = 1429871639658762281;
        private const ulong StaffRoleId = 1370422448763437207;
        private readonly string[] _ticketTypes = { "support", "exploiter", "speedtypist", "appeal" };

        public async Task<bool> ProcessTranscript(DiscordClient client, DiscordInteraction? interaction, DiscordChannel channel, string action = "auto")
        {
            if (!IsValidTicketChannel(channel, out string matchedPrefix, out int ticketId))
            {
                return false;
            }

            string ticketKey = $"{matchedPrefix}-{ticketId}";

            if (action == "close")
            {
                bool moved = await TryCloseTicket(interaction, channel, matchedPrefix);
                if (!moved) return false;
            }
            else if (action == "delete")
            {
                await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Deleting...").AsEphemeral());
            }

            try
            {
                await ExecuteTranscriptGeneration(client, interaction, channel, ticketKey, action);
                
                if (action == "delete")
                {
                    await channel.DeleteAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transcript Error] {ex}");
                return false;
            }
        }

        private bool IsValidTicketChannel(DiscordChannel channel, out string prefix, out int id)
        {
            prefix = string.Empty;
            id = 0;

            foreach (var type in _ticketTypes)
            {
                var match = Regex.Match(channel.Name, $@".*-{type}-(\d+)$");
                if (match.Success)
                {
                    prefix = type;
                    id = int.Parse(match.Groups[1].Value);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> TryCloseTicket(DiscordInteraction interaction, DiscordChannel channel, string prefix)
        {
            try
            {
                await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Closing...").AsEphemeral());

                var closeEmbed = new DiscordEmbedBuilder 
                { 
                    Title = "Closing Ticket...", 
                    Description = "Saving transcript and archiving...", 
                    Color = DiscordColor.Red 
                };
                await channel.SendMessageAsync(closeEmbed);

                var staffRole = interaction.Guild.GetRole(StaffRoleId);
                var overwrites = new List<DiscordOverwriteBuilder>
                {
                    new DiscordOverwriteBuilder(staffRole).Deny(Permissions.SendMessages).Allow(Permissions.AccessChannels),
                    new DiscordOverwriteBuilder(interaction.Guild.EveryoneRole).Deny(Permissions.AccessChannels)
                };

                await channel.ModifyAsync(ch => {
                    ch.Name = channel.Name.Replace($"{prefix}-", $"closed-{prefix}-");
                    ch.Parent = interaction.Guild.GetChannel(ClosedTicketCategoryId);
                    ch.PermissionOverwrites = overwrites;
                });

                return true;
            }
            catch (Exception ex)
            {
                await LogErrorToWebhook(ex);
                await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Failed to move category (Full?). Proceeding with transcript only."));
                return true; 
            }
        }

        private async Task ExecuteTranscriptGeneration(DiscordClient client, DiscordInteraction interaction, DiscordChannel channel, string ticketKey, string action)
        {
            string path = "transcripts.json";
            string jsonText = File.Exists(path) ? File.ReadAllText(path) : "{}";
            var root = JObject.Parse(jsonText);

            if (root["transcripts"] == null) root["transcripts"] = new JObject();
            var transcripts = (JObject)root["transcripts"];
            
            if (transcripts[ticketKey] == null) transcripts[ticketKey] = new JObject();
            var ticketData = (JObject)transcripts[ticketKey];

            int limit = (action == "auto") ? 100 : 1000;
            var rawMessages = await channel.GetMessagesAsync(limit);
            var sortedMessages = rawMessages.Where(m => !m.Author.IsBot).OrderBy(m => m.CreationTimestamp);

            foreach (var msg in sortedMessages)
            {
                string uid = msg.Author.Id.ToString();
                if (ticketData[uid] == null) ticketData[uid] = new JObject { ["messages"] = new JArray() };

                var userMsgs = (JArray)ticketData[uid]["messages"];
                
                
                if (userMsgs.Any(m => (string)m["messageId"] == msg.Id.ToString())) continue;

                var attachments = string.Join(" ", msg.Attachments.Select(a => a.Url));
                userMsgs.Add(new JObject
                {
                    ["username"] = msg.Author.Username,
                    ["content"] = $"{msg.Content} {attachments}".Trim(),
                    ["timestamp"] = msg.CreationTimestamp.UtcDateTime.ToString("o"),
                    ["messageId"] = msg.Id.ToString()
                });
            }

            
            if (action != "auto")
            {
                string sysId = "System";
                if (ticketData[sysId] == null) ticketData[sysId] = new JObject { ["messages"] = new JArray() };
                
                ((JArray)ticketData[sysId]["messages"]).Add(new JObject
                {
                    ["username"] = "Log",
                    ["content"] = $"{interaction.User.Username} performed action: {action.ToUpper()}",
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["messageId"] = $"SYS-{DateTime.UtcNow.Ticks}"
                });
            }

            File.WriteAllText(path, root.ToString(Formatting.Indented));
            autocompletehandler.LoadTranscriptCache(path);
        }

        private async Task LogErrorToWebhook(Exception e)
        {
            try
            {
                using HttpClient http = new();
                var payload = new { content = $"**[ERROR]** {DateTime.Now}\n```\n{e.Message}\n{e.StackTrace?[..Math.Min(e.StackTrace.Length, 500)]}```" };
                await http.PostAsync(Program.webhookURL, new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
            } catch
            {
                
            }
        }
    }
}