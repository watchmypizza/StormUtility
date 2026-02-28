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

namespace Stormtility
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension CommandBuilder { get; set; }
        private static SlashCommandsExtension Slash { get; set; }
        static async Task Main(string[] args)
        {
            string envLines = null;
            envLines = EnvReader.GetEnvVariables(".config");

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = envLines,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogUnknownEvents = false
            };

            Client = new DiscordClient(discordConfig);
            Client.Ready += events.ClientReady.Client_Ready;
            Client.GuildMemberUpdated += events.MemberUpdated.Client_GuildMemberUpdated;
            Client.MessageCreated += events.MessageCreated.Client_MessageCreated;
            Client.ComponentInteractionCreated += events.InteractionCreated.Client_ComponentInteractionCreated;

            JobCache.Load();
            _ = JobPayout.RunDailyJobAsyncPayout();

            Slash = Client.UseSlashCommands();
            Slash.RegisterCommands<slashcommands>();
            Slash.RegisterCommands<EconomyCommands>();
            Slash.RegisterCommands<ticketcommands>();
            Slash.RegisterCommands<antiraidcommands>();
            Slash.RegisterCommands<utilitycommands>();
            Slash.RegisterCommands<HelpCommand>();
            Slash.RegisterCommands<testcommands>(1349792051134468156);

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
