using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Stormtility.config;
using Microsoft.Data.Sqlite;
using Stormtility.Helpers;
using DSharpPlus.Exceptions;

namespace Stormtility.events {
    public class InteractionCreated
    {
        public static DiscordClient Client = null!;

        private static async Task<int> getTicketCountAsync(string ticketType)
        {
            string conn = "Data Source=tickets.db";
            string sql = "SELECT count FROM tickets WHERE type = @type";
            try {
                using(var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@type", ticketType.ToString());
                        var result = await command.ExecuteScalarAsync();

                        if(result != null && int.TryParse(result.ToString(), out int ticketId))
                        {
                            return ticketId;
                        }
                    }
                }

                return 0;
            } catch(Exception e)
            {
                await logger.LOGGER(e);
                return 0;
            }
        }

        private static async Task<bool> updateTicketCountAsync(string ticketType)
        {
            string conn = "Data Source=tickets.db";
            string sql = "UPDATE tickets SET count = count + 1 WHERE type = @type";
            try {
                using(var connection = new SqliteConnection(conn))
                {
                    await connection.OpenAsync();

                    using(var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@type", ticketType.ToString());
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            } catch(Exception e)
            {
                await logger.LOGGER(e);
                return false;
            }
        }

        public static async Task<bool> insertActiveTicket(DiscordUser user, DiscordChannel ticket)
        {
            var connectionString = "Data Source=activetickets.db";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    string sql = "INSERT INTO activetickets (member, channelId) VALUES (@id, @channelId);";

                    await connection.OpenAsync();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", user.Id.ToString());
                        command.Parameters.AddWithValue("@channelId", ticket.Id.ToString());

                        int rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();

                _ = logger.LOGGER(e);

                return false;
            }
        }

        public static async Task<ulong> getActiveTicket(DiscordUser user)
        {
            string sqlConn = "Data Source=activetickets.db";

            try
            {
                using(var conn = new SqliteConnection(sqlConn))
                {
                    await conn.OpenAsync();

                    string sql = "SELECT channelId FROM activetickets WHERE member = @id";

                    using (var comm = new SqliteCommand(sql, conn))
                    {
                        comm.Parameters.AddWithValue("@id", user.Id.ToString());

                        var result = await comm.ExecuteScalarAsync();

                        if(result != null && ulong.TryParse(result.ToString(), out ulong channelId))
                        {
                            return channelId;
                        }
                    }

                    return 0;
                }
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();
                _ = logger.LOGGER(e);
                return 0;
            }
        }

        public static async Task<(ulong? channelId, ulong? userId)> deleteActiveTicketEntry(ulong? id = 0)
        {
            string connectionString = "Data Source=activetickets.db";
            string chid = id.ToString() ?? "0";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "DELETE FROM activetickets WHERE channelId = @id RETURNING member, channelId;";

                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", chid);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                ulong.TryParse(reader["member"].ToString(), out ulong userId);
                                ulong.TryParse(reader["channelId"].ToString(), out ulong channelId);

                                return(channelId, userId);
                            }
                        }
                    }
                    return (null, null);
                }
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("--- STACK SPLIT ---");
                Console.WriteLine(e);
                Console.WriteLine("---  STACK END  ---");
                Console.ResetColor();
                _ = logger.LOGGER(e);
                return (null, null);
            }
        }

        public static async Task createAppealChannel(ModalSubmittedEventArgs args, DiscordMessageBuilder appeal)
        {
            int ticketId = await getTicketCountAsync("appeal") + 1;

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Creating ban appeal channel..."));

            ulong catId = 1429871426386526310;
            var cat = await args.Interaction.Guild.GetChannelAsync(catId);
            ulong staffRole = 1474165198947356793;
            var staff = await args.Interaction.Guild.GetRoleAsync(staffRole);
            ulong ticketRole = 1387529661323677827;
            var tickets = await args.Interaction.Guild.GetRoleAsync(ticketRole);

            var member = (DiscordMember)args.Interaction.User;

            var userOverwrites = new DiscordOverwriteBuilder(member).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(DiscordPermission.ViewChannel);

            var channel = await args.Interaction.Guild.CreateChannelAsync($"appeal-{ticketId}", DiscordChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);
            _ = await insertActiveTicket(args.Interaction.User, channel);

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

            var supportEmbed = new DiscordEmbedBuilder
            {
                Title = "Ban Appeal",
                Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.\nIn the meantime, please provide proof of your innocence.",
                Color = DiscordColor.Red
            };

            var closeBtn = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji:new DiscordComponentEmoji("⚠️"));

            if (!vars.debugMode)
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{staff.Mention}").WithAllowedMentions(Mentions.All));
                await channel.SendMessageAsync(appeal);
            } else
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(appeal);
            }

            bool increased = await updateTicketCountAsync("appeal");
            if(increased)
            {
                return;
            }

            await logger.LOGGER(new Exception("Ticket type for support did not increase successfully."));
            return;
        }

        public static async Task bulkDeleteTickets(DiscordClient sender, ComponentInteractionCreatedEventArgs args)
        {
            await args.Interaction.DeferAsync(true);
            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Deleting channels..."));
            var autoTranscript = new AutoTranscript();
            DiscordChannel category = await args.Interaction.Guild.GetChannelAsync(1429871639658762281);
            DiscordMember mem = null!;
            bool result = true;
            foreach(DiscordChannel channel in category.Children) {
                if(channel.Name.Contains("-closed-")) {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Deleting {channel.Mention}..."));
                    await logger.debugLog("Trying to delete alrady closed ticket...");
                    var (channelId, userId) = await deleteActiveTicketEntry(args.Interaction.Channel.Id);
                    await logger.debugLog($"Received {channelId} and {userId} from deleteActiveTicketEntry");
                    if(channelId == null || userId == null)
                    {
                        await logger.debugLog("Setting values to default...");
                        channelId = args.Interaction.Channel.Id;
                        userId = args.Interaction.User.Id;
                        await logger.debugLog($"channelId is now {channelId}");
                        await logger.debugLog($"userId is now {userId}");
                    }
                    await logger.debugLog($"Grabbed {channel.Id}");
                    await logger.debugLog($"Attempting to grab member");
                    mem = await args.Guild.GetMemberAsync(userId.Value);
                    await logger.debugLog($"Grabbed {mem.Id}");
                    await logger.debugLog($"Auto transcripting ticket...");
                    result = await autoTranscript.ProcessTranscript(sender, args.Interaction, channel, "delete");
                    await logger.debugLog($"Received as result: {result}");
                } else
                {
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("CANCELLED TRYING TO DELETE NON TICKET CHANNEL."));
                    return;
                } 
                if (!result)
                {
                    continue;
                }
            }

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Finished deleting all tickets!"));
            return;
        }

        public static async Task handleSpeedTypistApplicationModalReceivedState(ModalSubmittedEventArgs args, DiscordMessageBuilder compiledMessage)
        {
            int ticketId = await getTicketCountAsync("speedtypist") + 1;

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Creating speedtypist channel..."));

            ulong catId = 1429871426386526310;
            var cat = await args.Interaction.Guild.GetChannelAsync(catId);
            ulong staffRole = 1370422448763437207;
            var staff = await args.Interaction.Guild.GetRoleAsync(staffRole);
            ulong ticketRole = 1387529661323677827;
            var tickets = await args.Interaction.Guild.GetRoleAsync(ticketRole);

            var member = (DiscordMember)args.Interaction.User;

            var userOverwrites = new DiscordOverwriteBuilder(member).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(DiscordPermission.ViewChannel);

            var channel = await args.Interaction.Guild.CreateChannelAsync($"speedtypist-{ticketId}", DiscordChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);
            _ = await insertActiveTicket(args.Interaction.User, channel);

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

            var supportEmbed = new DiscordEmbedBuilder
            {
                Title = "Speedtypist Application",
                Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.",
                Color = DiscordColor.Aquamarine
            };

            var closeBtn = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

            if (!vars.debugMode)
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
                await channel.SendMessageAsync(compiledMessage);
            } else
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(compiledMessage);
            }

            bool increased = await updateTicketCountAsync("speedtypist");
            if(increased)
            {
                return;
            }

            await logger.LOGGER(new Exception("Ticket type for support did not increase successfully."));
            return;
        }

        public static async Task handleSupportModalReceivedState(ModalSubmittedEventArgs args, DiscordMessageBuilder compiledMessage)
        {
            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Creating support channel..."));

            int ticketId = await getTicketCountAsync("support") + 1;

            ulong catId = 1429871426386526310;
            var cat = await args.Interaction.Guild.GetChannelAsync(catId);
            ulong staffRole = 1370422448763437207;
            var staff = await args.Interaction.Guild.GetRoleAsync(staffRole);
            ulong ticketRole = 1387529661323677827;
            var tickets = await args.Interaction.Guild.GetRoleAsync(ticketRole);

            var member = (DiscordMember)args.Interaction.User;

            var userOverwrites = new DiscordOverwriteBuilder(member).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(DiscordPermission.ViewChannel);

            var channel = await args.Interaction.Guild.CreateChannelAsync($"support-{ticketId}", DiscordChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);
            _ = await insertActiveTicket(args.Interaction.User, channel);

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

            var supportEmbed = new DiscordEmbedBuilder
            {
                Title = "Support Ticket",
                Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.",
                Color = DiscordColor.Aquamarine
            };

            var closeBtn = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji: new DiscordComponentEmoji("⚠️"));

            if (!vars.debugMode)
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{tickets.Mention}").WithAllowedMentions(Mentions.All));
                await channel.SendMessageAsync(compiledMessage);
            } else
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(compiledMessage);
            }

            bool increased = await updateTicketCountAsync("support");
            if(increased)
            {
                return;
            }

            await logger.LOGGER(new Exception("Ticket type for support did not increase successfully."));
            return;
        }

        public static async Task handleExploiterReportModalReceivedStateSubmissionTicketCreationAction(ModalSubmittedEventArgs args, DiscordMessageBuilder compiledMessage)
        {
            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Creating exploiter report channel..."));

            int ticketId = await getTicketCountAsync("exploiter") + 1;

            ulong catId = 1429871426386526310;
            var cat = await args.Interaction.Guild.GetChannelAsync(catId);
            ulong staffRole = 1474165198947356793;
            var staff = await args.Interaction.Guild.GetRoleAsync(staffRole);
            ulong ticketRole = 1387529661323677827;
            var tickets = await args.Interaction.Guild.GetRoleAsync(ticketRole);

            var member = (DiscordMember)args.Interaction.User;

            var userOverwrites = new DiscordOverwriteBuilder(member).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var staffOverwrites = new DiscordOverwriteBuilder(staff).Allow(new([DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.UseApplicationCommands, DiscordPermission.ReadMessageHistory]));
            var defaultRoleOverwrites = new DiscordOverwriteBuilder(args.Interaction.Guild.EveryoneRole).Deny(DiscordPermission.ViewChannel);

            var channel = await args.Interaction.Guild.CreateChannelAsync($"exploiter-{ticketId}", DiscordChannelType.Text, cat, overwrites: [userOverwrites, staffOverwrites, defaultRoleOverwrites]);
            _ = await insertActiveTicket(args.Interaction.User, channel);

            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel created at {channel.Mention}"));

            var supportEmbed = new DiscordEmbedBuilder
            {
                Title = "Exploiter Report",
                Description = $"Hello {args.Interaction.User.Mention}!\nStaff will assist you shortly, please give us a few minutes to respond.\nIn the meantime, please provide proof of the player using exploits.",
                Color = DiscordColor.Red
            };

            var closeBtn = new DiscordButtonComponent(DiscordButtonStyle.Danger, "closeticket", "Close Ticket", emoji:new DiscordComponentEmoji("⚠️"));

            if (!vars.debugMode)
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).WithContent($"{tickets.Mention}").AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"{staff.Mention}").WithAllowedMentions(Mentions.All));
                await channel.SendMessageAsync(compiledMessage);
            } else
            {
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(supportEmbed).AddActionRowComponent(closeBtn));
                await channel.SendMessageAsync(compiledMessage);
            }

            bool increased = await updateTicketCountAsync("exploiter");
            if(increased)
            {
                return;
            }

            await logger.LOGGER(new Exception("Ticket type for support did not increase successfully."));
            return;
        }

        public static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreatedEventArgs args)
        {
            try
            {
                if (args.Id == "ticketoptions")
                {
                    var selected = args.Values.FirstOrDefault();

                    if (selected == "support")
                    {
                        ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != 0)
                        {
                            try {
                                DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                                return;
                            } catch(NotFoundException ex)
                            {
                                await deleteActiveTicketEntry(id: isTicketOpenAlready);
                                await logger.LOGGER(ex);
                            }
                        }

                        DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("supportmodal")
                                            .WithTitle("Create a support ticket...")
                                            .AddTextInput(new DiscordTextInputComponent("problemoverview", "Short overview of your problem", null, true, DiscordTextInputStyle.Short, 6, 25), "A short overview of your problem.")
                                            .AddTextInput(new DiscordTextInputComponent("problemdescription", "Describe the problem in full detail.", null, true, DiscordTextInputStyle.Paragraph, 150, 4000), "Describe your problem in best detail.")
                                            .AddFileUpload(new DiscordFileUploadComponent("attachments", 0, 10, false), "Additional attachments", "Not needed if you don't have any, but is beneficial.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
        
                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                    }
                    else if (selected == "speedtypist")
                    {
                        ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != 0)
                        {
                            try {
                                DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                                return;
                            } catch(NotFoundException ex)
                            {
                                await deleteActiveTicketEntry(id: isTicketOpenAlready);
                                await logger.LOGGER(ex);
                            }
                        }

                        DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("speedtypistmodal")
                                            .WithTitle("Apply for speedtypist role...")
                                            .AddTextInput(new DiscordTextInputComponent("monkeytypeprofile", "https://monkeytype.com/profile/peanutxvreal", "", required: true, style: DiscordTextInputStyle.Short), "Your MonkeyType profile", "You can find this under Username at the top → Public Profile → Copy link in the URL bar")
                                            .AddFileUpload(new DiscordFileUploadComponent("proof", 1, 1, true), "15 Second recording of your run", "Upload a 15s Monkeytype recording. Drag your Discord window into view at the end.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
        
                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                    } else if (selected == "exploiter")
                    {
                        ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != 0)
                        {
                            try {
                                DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                                return;
                            } catch(NotFoundException ex)
                            {
                                await deleteActiveTicketEntry(id: isTicketOpenAlready);
                                await logger.LOGGER(ex);
                            }
                        }

                        var dropdownsb = new DiscordSelectComponent("spellingbeegame", "Stormy's Spelling Bee 🐝", new List<DiscordSelectComponentOption>() {
                                                                                                                    new("Stormy's Spelling Bee", "ssb", "Report someone in Stormy's Spelling Bee.", false, new DiscordComponentEmoji("🐝")),
                                                                                                                    new DiscordSelectComponentOption("Bean's Spelling Bee", "nsb", "Report someone from the original spelling bee made by Bean.", false, new DiscordComponentEmoji("🥫"))});

                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Which game are you reporting from?").AsEphemeral().AddActionRowComponent(dropdownsb));
                        return;
                    } else if (selected == "appeal")
                    {
                        ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != 0)
                        {
                            try {
                                DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                                return;
                            } catch(NotFoundException ex)
                            {
                                await deleteActiveTicketEntry(id: isTicketOpenAlready);
                                await logger.LOGGER(ex);
                            }
                        }

                        var confirmbtn = new DiscordButtonComponent(DiscordButtonStyle.Success, "confirmappeal", "Continue, create my in-game appeal.", false, new DiscordComponentEmoji("📨"));
                        var blacklistappeal = new DiscordButtonComponent(DiscordButtonStyle.Primary, "support", "Create a support ticket instead.", false, new DiscordComponentEmoji("❗"));

                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("**Please remember that this selection is for**\n# In-Game Ban Appeals\n**If you wish to proceed, click the appropriate button.**").AsEphemeral().AddActionRowComponent(confirmbtn, blacklistappeal));

                    }
                }

                if(args.Id == "confirmappeal")
                {
                    DiscordModalBuilder modal = new DiscordModalBuilder()
                                                            .WithCustomId("gameappealmodal")
                                                            .WithTitle("Create a game appeal...")
                                                            .AddTextInput(new DiscordTextInputComponent("robloxusername", "stormyxvreal", "", true, DiscordTextInputStyle.Short), "Your ROBLOX username")
                                                            .AddFileUpload(new DiscordFileUploadComponent("proof", 1, 1, false), "Screenshot or clip of you getting banned", "Incase you cannot screenshot or do not have a medal clip, leave this field out. It is not required.")
                                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree",
                                                                                new List<DiscordCheckboxGroupOption> {  new DiscordCheckboxGroupOption("agreed", "Agree to rules",
                                                                                description: "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", selectedByDefault: false)},
                                                                            1, 1, true), "Ticket Rules");
                    
                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                    return;
                }
                if (args.Id == "support")
                {
                    ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                    if(isTicketOpenAlready != 0)
                    {
                        try {
                            DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                            return;
                        } catch(NotFoundException ex)
                        {
                            await deleteActiveTicketEntry(id: isTicketOpenAlready);
                            await logger.LOGGER(ex);
                        }
                    }

                    DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("supportmodal")
                                            .WithTitle("Create a support ticket...")
                                            .AddTextInput(new DiscordTextInputComponent("problemoverview", "Short overview of your problem", null, true, DiscordTextInputStyle.Short, 6, 25), "A short overview of your problem.")
                                            .AddTextInput(new DiscordTextInputComponent("problemdescription", "Describe the problem in full detail.", null, true, DiscordTextInputStyle.Paragraph, 150, 4000), "Describe your problem in best detail.")
                                            .AddFileUpload(new DiscordFileUploadComponent("attachments", 0, 10, false), "Additional attachments", "Not needed if you don't have any, but is beneficial.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
        
                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                }
                
                if(args.Id == "spellingbeegame")
                {
                    var selected = args.Values.FirstOrDefault();

                    if(selected == "ssb")
                    {
                        ulong isTicketOpenAlready = await getActiveTicket(args.Interaction.User);

                        if(isTicketOpenAlready != 0)
                        {
                            try {
                                DiscordChannel ch = await sender.GetChannelAsync(isTicketOpenAlready);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Please continue inside of your current open ticket before opening a new one: {ch.Mention}").AsEphemeral(true));
                                return;
                            } catch(NotFoundException ex)
                            {
                                await deleteActiveTicketEntry(id: isTicketOpenAlready);
                                await logger.LOGGER(ex);
                            }
                        }

                        DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("exploitermodal")
                                            .WithTitle("Report an exploiter...")
                                            .AddTextInput(new DiscordTextInputComponent("reportedusername", "John Doe", "", true, DiscordTextInputStyle.Short), "Name or UserId of the reported user.", "Do not use the display name.")
                                            .AddTextInput(new DiscordTextInputComponent("reporterusername", "Jane Doe", "", true, DiscordTextInputStyle.Short), "Your username", "Not your display name either.")
                                            .AddFileUpload(new DiscordFileUploadComponent("attachments", 0, 10, false), "Proof of the user exploiting", "If you do not have any, leave this field out.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
            
                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                    } else
                    {
                        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Please report **spelling bee** exploiters here:\ndiscord.gg/spellingbee\n\n-# Contact server administrators if the link is invalid or expired.").AsEphemeral());
                        return;
                    }
                }

                if(args.Id == "confirmspeedtypist")
                {
                    // discarded  due to new modal
                }

                if (args.Id == "deleteticket")
                {
                    await args.Interaction.DeferAsync(true);
                    var member = (DiscordMember)args.Interaction.User;
                    if(!member.PermissionsIn(args.Interaction.Channel).HasPermission(DiscordPermission.ModerateMembers))
                    {
                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You cannot delete this ticket."));
                        return;
                    }

                    var autoTranscript = new AutoTranscript();
                    DiscordChannel channel = args.Interaction.Channel;
                    DiscordMember mem = null!;
                    bool deletedWhileClosed = false;
                    bool result = true;
                    if(args.Interaction.Channel.Name.Contains("-closed-")) {
                        deletedWhileClosed = true;
                        await logger.debugLog("Trying to delete alrady closed ticket...");
                        var (channelId, userId) = await deleteActiveTicketEntry(args.Interaction.Channel.Id);
                        await logger.debugLog($"Received {channelId} and {userId} from deleteActiveTicketEntry");
                        if(channelId == null || userId == null)
                        {
                            await logger.debugLog("Setting values to default...");
                            channelId = args.Interaction.Channel.Id;
                            userId = args.Interaction.User.Id;
                            await logger.debugLog($"channelId is now {channelId}");
                            await logger.debugLog($"userId is now {userId}");
                        }
                        await logger.debugLog($"Attempting to grab channel");
                        channel = await args.Guild.GetChannelAsync(channelId.Value);
                        await logger.debugLog($"Grabbed {channel.Id}");
                        await logger.debugLog($"Attempting to grab member");
                        mem = await args.Guild.GetMemberAsync(userId.Value);
                        await logger.debugLog($"Grabbed {mem.Id}");
                        await logger.debugLog($"Auto transcripting ticket...");
                        result = await autoTranscript.ProcessTranscript(sender, args.Interaction, args.Interaction.Channel, "delete");
                        await logger.debugLog($"Received as result: {result}");
                    }
                    if (!result)
                    {
                        var errorEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "This ticket cannot be deleted.",
                            Color = DiscordColor.Red
                        };
                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                        System.Console.WriteLine($"ERROR: {result}");
                    } else
                    {
                        try {                            
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = "Ticket Closed",
                                Description = $"The ticket {channel.Name} has been closed.",
                                Color = DiscordColor.Red
                            };

                            embed.AddField("Closed By:", $"{args.Interaction.User.Mention}");

                            if(!deletedWhileClosed) {
                                await mem.SendMessageAsync(embed);
                            }
                            deletedWhileClosed = false;
                        } catch (NullReferenceException)
                        {
                            return;
                        } catch (Exception e)
                        {
                            await logger.LOGGER(e);
                        }
                    }
                }

                if (args.Id == "bulkdelete")
                {
                    await bulkDeleteTickets(sender, args);
                    return;
                }

                if (args.Id == "closeticket")
                {
                    bool doNotDm = false;
                    await args.Interaction.DeferAsync(true);
                    var member = (DiscordMember)args.Interaction.User;
                    if(!member.PermissionsIn(args.Interaction.Channel).HasPermission(DiscordPermission.ModerateMembers))
                    {
                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You cannot close this ticket."));
                        return;
                    }

                    var autoTranscript = new AutoTranscript();
                    var (channelId, userId) = await deleteActiveTicketEntry(args.Interaction.Channel.Id);
                    if(channelId == null || userId == null)
                    {
                        doNotDm = true;
                    }
                    bool result = await autoTranscript.ProcessTranscript(sender, args.Interaction, args.Interaction.Channel, "close");
                    if (!result)
                    {
                        var errorEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "This ticket cannot be closed.",
                            Color = DiscordColor.Red
                        };
                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                        System.Console.WriteLine($"ERROR: {result}");
                    } else
                    { try {
                            if(!doNotDm) {
                                DiscordMember mem = await args.Guild.GetMemberAsync(userId.Value);
                                DiscordChannel channel = await args.Guild.GetChannelAsync(channelId.Value);
                                
                                var embed = new DiscordEmbedBuilder
                                {
                                    Title = "Ticket Closed",
                                    Description = $"The ticket {channel.Name} has been closed.",
                                    Color = DiscordColor.Red
                                };

                                embed.AddField("Closed By:", $"{args.Interaction.User.Mention}");

                                try {
                                    await mem.SendMessageAsync(embed);
                                } catch(UnauthorizedException e)
                                {
                                    await logger.LOGGER(e);
                                    return;
                                }
                            }
                        } catch(NullReferenceException)
                        {
                            return;
                        } catch(Exception e)
                        {
                            await logger.LOGGER(e);
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