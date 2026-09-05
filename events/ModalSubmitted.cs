using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Stormtility.Helpers;

namespace Stormtility.events
{
    public class ModalSubmitted
    {
        public static async Task Client_ModalSubmittedEvent(DiscordClient sender, ModalSubmittedEventArgs args)
        {
            if (args.Id == "gameappealmodal")
            {
                await args.Interaction.DeferAsync(true);

                var usernameInput = args.Values?["robloxusername"] as TextInputModalSubmission;
                string? username = usernameInput!.Value;
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

                    await events.InteractionCreated.CreateAppealChannel(args, sendToDagestan);
                    return;
                }

                using HttpClient http = new();
                string? url = file.UploadedFiles.FirstOrDefault()?.Url;
                string? name = file.UploadedFiles.FirstOrDefault()?.FileName;

                Stream stream = await http.GetStreamAsync(url);

                sendToDagestan = new DiscordMessageBuilder()
                                            .AddEmbed(submissionEmbed)
                                            .AddFile(name ?? "Uploaded", stream);

                await events.InteractionCreated.CreateAppealChannel(args, sendToDagestan);
                return;
            }
            else if (args.Id == "speedtypistmodal")
            {
                await args.Interaction.DeferAsync(true);

                var profileLinkInput = args.Values["monkeytypeprofile"] as TextInputModalSubmission;
                var videoShared = args.Values["proof"] as FileUploadModalSubmission;

                using HttpClient http = new();
                string? url = videoShared!.UploadedFiles.FirstOrDefault()?.Url;
                string? name = videoShared.UploadedFiles.FirstOrDefault()?.FileName;

                Stream fStream = await http.GetStreamAsync(url);

                var submissionEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Speedtypist Application",
                    Color = DiscordColor.Aquamarine
                };

                submissionEmbed.AddField("Userprofile", profileLinkInput?.Value ?? "No link?", true);

                var sendToDagestan = new DiscordMessageBuilder()
                                        .AddEmbed(submissionEmbed)
                                        .AddFile(name ?? "Uploaded", fStream);

                Regex isValidURL = new(@"https?\:\/\/(?:www\.)?monkeytype\.com\/profile\/[A-Za-z0-9]+");
                if (!isValidURL.IsMatch(profileLinkInput?.Value ?? "no url"))
                {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Please supply a valid URL!"));
                    return;
                }

                await InteractionCreated.HandleSpeedTypistApplicationModalReceivedState(args, sendToDagestan);
                return;
            }
            else if (args.Id == "supportmodal")
            {
                await args.Interaction.DeferAsync(true);

                var problemOverview = args.Values["problemoverview"] as TextInputModalSubmission;
                var problemDescription = args.Values["problemdescription"] as TextInputModalSubmission;
                var attachments = args.Values["attachments"] as FileUploadModalSubmission;

                var submissionEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Support Ticket",
                    Color = DiscordColor.Aquamarine
                };

                submissionEmbed.Title = problemOverview?.Value ?? "No Title";
                submissionEmbed.Description = problemDescription?.Value ?? "No Description";

                DiscordMessageBuilder sendToDagestan = new DiscordMessageBuilder()
                                                            .AddEmbed(submissionEmbed);

                using HttpClient receiveDagestan = new();

                if (attachments?.UploadedFiles.Count != 0)
                {
                    foreach (DiscordAttachment attachment in attachments!.UploadedFiles)
                    {
                        Stream fStream = await receiveDagestan.GetStreamAsync(attachment.Url);

                        sendToDagestan.AddFile(attachment.FileName ?? "NoName", fStream);
                    }
                }

                await InteractionCreated.HandleSupportModalReceivedState(args, sendToDagestan);
                return;
            }
            else if (args.Id == "exploitermodal")
            {
                await args.Interaction.DeferAsync(true);

                var reportedUser = args.Values["reportedusername"] as TextInputModalSubmission;
                var reporter = args.Values["reporterusername"] as TextInputModalSubmission;
                var attachments = args.Values["attachments"] as FileUploadModalSubmission;

                var submissionEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Exploiter Report",
                    Color = DiscordColor.Aquamarine
                };

                submissionEmbed.AddField("Reporter", reporter?.Value ?? "No reporter username", true);
                submissionEmbed.AddField("Reported User", reportedUser?.Value ?? "No reported username", true);

                DiscordMessageBuilder sendToDagestan = new DiscordMessageBuilder()
                                                            .AddEmbed(submissionEmbed);

                using HttpClient receiveDagestan = new();

                if (attachments?.UploadedFiles.Count != 0)
                {
                    foreach (DiscordAttachment attachment in attachments!.UploadedFiles)
                    {
                        Stream fStream = await receiveDagestan.GetStreamAsync(attachment.Url);

                        sendToDagestan.AddFile(attachment.FileName ?? "NoName", fStream);
                    }
                }

                await InteractionCreated.HandleExploiterReportModalReceivedStateSubmissionTicketCreationAction(args, sendToDagestan);
                return;
            }
            else if (args.Id.StartsWith("sendmessagemodal"))
            {
                await args.Interaction.DeferAsync(true);

                string[] modalIdParts = args.Id.Split(":", StringSplitOptions.TrimEntries);
                string commandName = modalIdParts[1];
                var recipientIds = args.Values["recipient"] as UserSelectMenuModalSubmission;
                var message = args.Values["message"] as TextInputModalSubmission;
                var files = args.Values["fileuploads"] as FileUploadModalSubmission;

                var recipient = await sender.GetUserAsync(recipientIds!.Ids.FirstOrDefault());

                if (recipient.Id == args.Interaction.User.Id)
                {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You cannot message yourself."));
                    return;
                }

                if (recipient.IsBot)
                {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Bots cannot DM eachother."));
                    return;
                }

                DiscordChannel logChannel = await sender.GetChannelAsync(1533206494181462137);

                var embed = new DiscordEmbedBuilder
                {
                    Title = "You have received a new message!",
                    Color = DiscordColor.Red
                };

                embed.AddField("Content:", message?.Value.ToString() ?? "Empty Message", false);
                embed.WithFooter("Anonymous to you • May be subject to moderation");

                DiscordMessageBuilder sendToDagestan = new DiscordMessageBuilder()
                                                            .AddEmbed(embed);

                using HttpClient receiveDagestan = new();

                try
                {
                    await logChannel.SendMessageAsync($"User: {args.Interaction.User.Mention} sent ```{message?.Value ?? "No message"}``` to {recipient!.Mention}");
                    foreach (DiscordAttachment attachment in files!.UploadedFiles)
                    {
                        Stream fStream = await receiveDagestan.GetStreamAsync(attachment.Url);

                        sendToDagestan.AddFile(attachment.FileName ?? "NOFILENAME", fStream);
                    }
                    await logChannel.SendMessageAsync(sendToDagestan);
                    await recipient.SendMessageAsync(sendToDagestan);
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Message sent!"));
                    CommandCooldown.AddCooldown(commandName, args.Interaction.User.Id, 12);
                    return;
                } catch (Exception e) {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong processing your request."));
                    await Logger.LOGGER(e);
                    return;
                }
            }
        }
    }
}
