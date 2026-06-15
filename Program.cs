using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Stormtility.commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gamebot
{
    public class Program
    {
        public static string apiKey { get; set; }
        public static ulong logChannel { get; } = 1477086071266218094;
        public static ulong staffId { get; } = 1474165198947356793;
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private static Dictionary<string, string> ReadEnv(string envFilePath)
        {
            var env = new Dictionary<string, string>();

            if (!File.Exists(envFilePath))
                return env;

            foreach (var line in File.ReadAllLines(envFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                env[parts[0].Trim()] = parts[1].Trim();
            }

            return env;
        }

        static async Task Main(string[] args)
        {
            var env = ReadEnv(".env");
            string token = env["DISCORD_BOT_TOKEN"];
            apiKey = env["API_KEY"];
            string RBLXCKY = env["RBLX_CKY"];

            _ = StartBanRefreshLoop(TimeSpan.FromMinutes(5), _cts.Token);

            DiscordClient client = new DiscordClient(new DiscordConfiguration()
            {
                Token = token,
                AutoReconnect = true,
                LogUnknownEvents = false,
                Intents = DiscordIntents.All,
                ReconnectIndefinitely = true,
            });

            var prefix = new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
            };

            client.UseCommandsNext(prefix);

            var sc = client.UseSlashCommands();
            sc.RegisterCommands<helpcommand>();
            sc.RegisterCommands<gamecommands>();

            client.Ready += Stormtility.events.Client_ReadyEvent.Client_Ready;

            await client.ConnectAsync();
            await Task.Delay(-1, _cts.Token);
        }

        private static async Task StartBanRefreshLoop(TimeSpan interval, CancellationToken token)
        {
            using var timer = new PeriodicTimer(interval);
            Console.WriteLine($"[System] Background task initialized: Checking bans every {interval.TotalMinutes} minutes.");

            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    Console.WriteLine("[System] 5-minute interval reached. Scanning ban file...");
                    
                    bool success = await refBan.refreshBans();
                    
                    if (success)
                    {
                        Console.WriteLine("[System] Ban file check completed successfully.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[System] Ban loop terminated safely.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Error in ban refresh thread: {ex.Message}");
            }
        }
    }
}
