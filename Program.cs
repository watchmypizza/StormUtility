using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.commands;
using Stormtility.commands.Anti_Raid_Commands;
using Stormtility.commands.EconomyCommands;
using Stormtility.commands.TicketCommands.autocompletehelper;
using Stormtility.commands.TicketCommands;
using Stormtility.commands.UtilityCommands;
using Stormtility.config;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stormtility.commands.Anti_Raid_Commands.calculationsanddumbassmath;
using Stormtility.commands.TestingCommands;
using Stormtility.Helpers;

namespace Stormtility
{
    internal class Program
    {
        private static DiscordClient? Client { get; set; }
        private static CommandsNextExtension? CommandBuilder { get; set; }
        private static SlashCommandsExtension? Slash { get; set; }
        public static string? webhookURL { get; set; }
        public static string? wbhurl2 { get; set; }
        static async Task Main(string[] args)
        {
            foreach(string arg in args)
            {
                if(arg == "-v" || arg == "--version")
                {
                    await logger.consoleLog("v0.8.5 DM FIX");
                    return;
                }
            }

            List<string>? envLines = null;
            envLines = EnvReader.GetEnvVariables(".config");
            webhookURL = envLines[1];
            wbhurl2 = envLines[2];

            await autostart.createTable();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = envLines[0],
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogUnknownEvents = false
            };

            Client = new DiscordClient(discordConfig);
            Client.Ready += events.ClientReady.Client_Ready;
            Client.GuildMemberUpdated += events.MemberUpdated.Client_GuildMemberUpdated;
            Client.MessageCreated += events.MessageEvents.Client_MessageCreated;
            Client.ComponentInteractionCreated += events.InteractionCreated.Client_ComponentInteractionCreated;
            Client.GuildMemberAdded += events.MemberJoined.Client_GuildMemberAdded;
            Client.ModalSubmitted += events.ModalSubmitted.Client_ModalSubmittedEvent;
            Client.MessageUpdated += events.MessageEvents.Client_MessageUpdated;
            Client.MessageDeleted += events.MessageEvents.Client_MessageDeleted;
            // Client.MessageReactionAdded += events.ReactionAdded.Client_ReactionAddedEvent; Not required, different solution has been found.

            JobCache.Load();
            _ = JobPayout.RunDailyJobAsyncPayout();

            Slash = Client.UseSlashCommands();
            Slash.RegisterCommands<slashcommands>();
            Slash.RegisterCommands<EconomyCommands>();
            Slash.RegisterCommands<ticketcommands>();
            Slash.RegisterCommands<antiraidcommands>();
            Slash.RegisterCommands<utilitycommands>();
            Slash.RegisterCommands<HelpCommand>();
            Slash.RegisterCommands<mutecounter>();
            Slash.RegisterCommands<testcommands>(1349792051134468156);

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
