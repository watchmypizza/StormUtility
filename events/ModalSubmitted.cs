using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;

namespace Stormtility.events
{
    public class ModalSubmitted
    {
        public static async Task Client_ModalSubmittedEvent(DiscordClient sender, ModalSubmittedEventArgs args)
        {
            if(args.Id == "gameappealmodal")
            {
                await args.Interaction.DeferAsync(true);

                var usernameInput = args.Values?["robloxusername"] as TextInputModalSubmission;
                string? username = usernameInput.Value;
                var sendToDagestan = new DiscordMessageBuilder();

                var submissionEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Game Appeal",
                    Color = DiscordColor.Aquamarine
                };

                submissionEmbed.AddField("Username", username, true);

                FileUploadModalSubmission? file = args.Values?["proof"] as FileUploadModalSubmission;

                if (file == null || file.UploadedFiles.Count == 0)
                {
                    sendToDagestan = new DiscordMessageBuilder()
                                            .AddEmbed(submissionEmbed);

                    await events.InteractionCreated.createAppealChannel(args, sendToDagestan);
                    return;
                }

                using HttpClient http = new();
                string? url = file.UploadedFiles.FirstOrDefault()?.Url;
                string? name = file.UploadedFiles.FirstOrDefault()?.FileName;

                Stream stream = await http.GetStreamAsync(url);

                sendToDagestan = new DiscordMessageBuilder()
                                            .AddEmbed(submissionEmbed)
                                            .AddFile(name ?? "Uploaded", stream);
                
                await events.InteractionCreated.createAppealChannel(args, sendToDagestan);
            }
        }
    }
}