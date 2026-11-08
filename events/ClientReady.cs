using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Stormtility.commands.TicketCommands;
using Stormtility.Helpers;

namespace Stormtility.events
{
    public class ClientReady
    {
        private static readonly ulong genChId = 1177078904863862866;
        private static readonly ulong chRvId = 1405236449938374766;

        public static async Task Client_Ready(DiscordClient sender, SessionCreatedEventArgs args)
        {
            await sender.UpdateStatusAsync(new DiscordActivity("Chasing 2026 EF5", DiscordActivityType.Playing), DiscordUserStatus.DoNotDisturb, DateTimeOffset.UtcNow);
            Console.WriteLine($"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            AutoCompleteHandler.LoadTranscriptFiles();

            _ = Task.Run(async () => await SyncAllOpenTickets(sender));
            _ = Task.Run(async () => await CycleBanners(sender));
            _ = Task.Run(async () => await StartServer());
            _ = Task.Run(async () => await ScanSlowmode(sender));
            _ = Task.Run(async () => await StartChatRevHelper(sender));
            _ = Task.Run(async () => await LinkHelper.StartCacheRefresh());

            return;
        }

        private static async Task StartServer()
        {
            ProcessStartInfo start = new()
            {
                FileName = "python",
                Arguments = "TranscriptServer/Server/app.py",
                WorkingDirectory = $"{Environment.CurrentDirectory}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = start };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Python Output]: {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Python Error]: {e.Data}");
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Console.WriteLine("Transcript Server is starting up in the background...");

            await process.WaitForExitAsync();

            Console.WriteLine($"Python server exited with code: {process.ExitCode}");
        }

        private static async Task ScanSlowmode(DiscordClient client)
        {
            DiscordChannel channel = await client.GetChannelAsync(genChId);

            SlowmodeHelper.Start(channel);
        }

        private static async Task StartChatRevHelper(DiscordClient client)
        {
            DiscordGuild guild = await client.GetGuildAsync(1177078904176001204); // stormy server guild id
            DiscordRole chatRevive = await guild.GetRoleAsync(chRvId);
            DiscordChannel channel = await client.GetChannelAsync(genChId);

            ChatReviveHelper.Start(chatRevive, channel);
        }

        private static async Task CycleBanners(DiscordClient client)
        {
            using PeriodicTimer timer = new(TimeSpan.FromHours(1));
            do {
                string randFile = "";
                try
                {
                    var banners = Directory.GetFiles("Banners/");

                    int randIdx = Random.Shared.Next(banners.Length);
                    randFile = banners[randIdx];
                    using FileStream stream = new FileStream(randFile, FileMode.Open, FileAccess.Read);

                    await client.ModifyCurrentUserAsync(banner: stream);
                }
                catch (DirectoryNotFoundException e)
                {
                    await Logger.LOGGER(e);
                } catch(BadRequestException) {
                    await Logger.ConsoleLog($"File {randFile} got rejected. Discord might be having a stroke");
                } catch(Exception ex)
                {
                    await Logger.LOGGER(ex);
                }
            } while(await timer.WaitForNextTickAsync());
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
                        continue;
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
                        await Logger.LOGGER(ex);
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
