using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using Stormtility.commands.TicketCommands;
using TskLib;

namespace Stormtility.events
{
    public class AutoTranscript
    {
        private const ulong OpenTicketCategoryId = 1429871426386526310;
        private const ulong ClosedTicketCategoryId = 1429871639658762281;
        private const ulong StaffRoleId = 1370422448763437207;

        private readonly string[] _ticketTypes =
        {
            "support",
            "exploiter",
            "speedtypist",
            "appeal"
        };

        public async Task<bool> ProcessTranscript(
            DiscordClient client,
            DiscordInteraction? interaction,
            DiscordChannel channel,
            string action = "auto")
        {
            if (!IsValidTicketChannel(
                    channel,
                    out string matchedPrefix,
                    out int ticketId))
            {
                return false;
            }

            string ticketKey = $"{matchedPrefix}-{ticketId}";

            if (action == "close")
            {
                if (interaction is null)
                {
                    return false;
                }

                bool moved = await TryCloseTicket(
                    interaction,
                    channel,
                    matchedPrefix
                );

                if (!moved)
                {
                    return false;
                }
            }

            try
            {
                await ExecuteTranscriptGeneration(
                    interaction,
                    channel,
                    ticketKey,
                    action
                );

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

        private bool IsValidTicketChannel(
            DiscordChannel channel,
            out string prefix,
            out int id)
        {
            prefix = string.Empty;
            id = 0;

            foreach (string type in _ticketTypes)
            {
                Match match = Regex.Match(
                    channel.Name,
                    $@"(?:.)+?({type})-(\d+)$"
                );

                if (!match.Success)
                {
                    continue;
                }

                prefix = match.Groups[1].Value;
                id = int.Parse(match.Groups[2].Value);

                return true;
            }

            return false;
        }

        private async Task<bool> TryCloseTicket(
            DiscordInteraction interaction,
            DiscordChannel channel,
            string prefix)
        {
            try
            {
                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent("Closing...")
                        .AsEphemeral()
                );

                var closeEmbed = new DiscordEmbedBuilder
                {
                    Title = "Closing Ticket...",
                    Description = "Saving transcript and archiving...",
                    Color = DiscordColor.Red
                };

                await channel.SendMessageAsync(closeEmbed);

                var staffRole =
                    await interaction.Guild!.GetRoleAsync(StaffRoleId);

                var overwrites = new List<DiscordOverwriteBuilder>
                {
                    new DiscordOverwriteBuilder(staffRole)
                        .Deny(DiscordPermission.SendMessages)
                        .Allow(DiscordPermission.ViewChannel),

                    new DiscordOverwriteBuilder(
                        interaction.Guild.EveryoneRole
                    )
                        .Deny(DiscordPermission.ViewChannel)
                };

                await channel.ModifyAsync(async ch =>
                {
                    ch.Name = channel.Name.Replace(
                        $"{prefix}-",
                        $"closed-{prefix}-"
                    );

                    ch.Parent =
                        await interaction.Guild.GetChannelAsync(
                            ClosedTicketCategoryId
                        );

                    ch.PermissionOverwrites = overwrites;
                });

                return true;
            }
            catch (Exception ex)
            {
                await LogErrorToWebhook(ex);

                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "Failed to move category (Full?). Proceeding with transcript only."
                        )
                );

                return true;
            }
        }

        private async Task ExecuteTranscriptGeneration(
            DiscordInteraction? interaction,
            DiscordChannel channel,
            string ticketKey,
            string action)
        {
            string path = Path.Combine("transcripts", $"{ticketKey}.tsk");
            Directory.CreateDirectory("transcripts");

            if (!File.Exists(path))
            {
                await Tsk.CreateAsync(path);
            }

            int limit = action == "auto" ? 100 : 1000;

            var rawMessages = channel.GetMessagesAsync(limit);

            var sortedMessages = rawMessages
                .Where(message => !message.Author!.IsBot)
                .OrderBy(message => message.CreationTimestamp);

            await foreach (var message in sortedMessages)
            {
                string content = SanitizeForTsk(message.Content ?? string.Empty);

                string attachments = string.Join(
                    " ",
                    message.Attachments.Select(a => a.Url)
                );

                if (!string.IsNullOrWhiteSpace(attachments))
                {
                    content = $"{content} {attachments}".Trim();
                }

                await Tsk.InsertAsync(
                    path,
                    new TskMessage
                    {
                        UserId = message.Author!.Id,
                        Username = message.Author.Username,
                        Content = content,
                        Timestamp = message.CreationTimestamp.UtcDateTime.ToString("O")
                    }
                );
            }

            if (action != "auto" && interaction is not null)
            {
                await Tsk.InsertAsync(
                    path,
                    new TskMessage
                    {
                        UserId = 1,
                        Username = "System",
                        Content = $"{interaction.User.Username} performed action: {action.ToUpper()}",
                        Timestamp = DateTime.UtcNow.ToString("O")
                    }
                );
            }

            AutoCompleteHandler.LoadTranscriptFiles();
        }

        private static string SanitizeForTsk(string text)
        {
            return text
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n");
        }

        private async Task LogErrorToWebhook(Exception e)
        {
            try
            {
                using HttpClient http = new();

                var payload = new
                {
                    content =
                        $"**[ERROR]** {DateTime.Now}\n```\n" +
                        $"{e.Message}\n" +
                        $"{e.StackTrace?[..Math.Min(
                            e.StackTrace.Length,
                            500
                        )]}```"
                };

                await http.PostAsync(
                    Program.webhookURL,
                    new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"
                    )
                );
            }
            catch
            {
            }
        }
    }
}
