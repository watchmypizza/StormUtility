using Stormtility.config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormtility.commands.TicketCommands.autocompletehelper;
using System.Text.Json;
using Stormtility.Helpers;

namespace Stormtility.events
{
    public class ClientReady
    {
        public static async Task Client_Ready(DiscordClient sender, SessionCreatedEventArgs args)
        {
            await sender.UpdateStatusAsync(new DiscordActivity("Chasing 2026 EF5", DiscordActivityType.Playing), DiscordUserStatus.DoNotDisturb, DateTimeOffset.UtcNow);
            Console.WriteLine($"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            autocompletehandler.LoadTranscriptCache("transcripts.json");
            autocompletehandler.fileSysWatcher("./transcripts.json");

            _ = Task.Run(async () => await SyncAllOpenTickets(sender));

            return;
        }

        private static async Task SyncAllOpenTickets(DiscordClient client)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            await Task.Delay(3000);

            ulong openTicketCategoryId = 1429871426386526310;
            var transcriptProcessor = new AutoTranscript();

            while(await timer.WaitForNextTickAsync())
            {
                try 
                {
                    var guild = await client.GetGuildAsync(1177078904176001204);

                    var openTickets = guild.Channels.Values
                        .Where(c => c.ParentId == openTicketCategoryId)
                        .ToList();

                    if (!openTickets.Any())
                    {
                        Console.WriteLine("[Startup Sync] No tickets found in the category (or cache is empty).");
                        return;
                    }

                    foreach (var channel in openTickets)
                    {
                        if (channel.Name.Contains("closed-")) continue;

                        Console.WriteLine($"[Startup Sync] Archiving {channel.Name}...");
                        await transcriptProcessor.ProcessTranscript(client, null, channel, "auto");
                    }

                    Console.WriteLine("[Startup Sync] All open tickets have been synchronized.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Startup Sync Error] {ex.Message}");
                    try 
                    {
                        await logger.LOGGER(ex);
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"FAILED TO SEND WEBHOOK: {logEx.Message}");
                    }
                }
            }
        }
    }
}