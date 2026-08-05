using DSharpPlus;

using Stormtility.commands;
using Stormtility.commands.Anti_Raid_Commands;
using Stormtility.commands.TicketCommands;
using Stormtility.commands.UtilityCommands;
using Stormtility.commands.TestingCommands;
using Stormtility.Helpers;
using Stormtility.events;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using CommunityToolkit.HighPerformance;

namespace Stormtility
{
    internal class Program
    {
        private static DiscordClient? Client { get; set; }
        public static string? webhookURL { get; set; }
        public static string? wbhurl2 { get; set; }
        public static string? nonce { get; set; }
        public static string? botToken { get; set; }
        static async Task Main(string[] args)
        {
            foreach(string arg in args)
            {
                if(arg == "-v" || arg == "--version")
                {
                    await logger.consoleLog("v1.1.2 Overwrites!");
                    return;
                }
            }

            List<string>? envLines = EnvReader.GetEnvVariables(".config");
            webhookURL = envLines[1];
            wbhurl2 = envLines[2];
            nonce = envLines[3];
            botToken = envLines[0];

            await autostart.createTable();

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(envLines[0], DiscordIntents.All);

            builder.ConfigureEventHandlers(eventBuilder =>
            {
                eventBuilder.HandleSessionCreated(events.ClientReady.Client_Ready);
                eventBuilder.HandleGuildMemberUpdated(events.MemberUpdated.Client_GuildMemberUpdated);
                eventBuilder.HandleMessageCreated(events.MessageEvents.Client_MessageCreated);
                eventBuilder.HandleMessageUpdated(events.MessageEvents.Client_MessageUpdated);
                eventBuilder.HandleMessageDeleted(events.MessageEvents.Client_MessageDeleted);
                eventBuilder.HandleModalSubmitted(events.ModalSubmitted.Client_ModalSubmittedEvent);
                eventBuilder.HandleGuildMemberAdded(events.MemberJoined.Client_GuildMemberAdded);
                eventBuilder.HandleComponentInteractionCreated( events.InteractionCreated.Client_ComponentInteractionCreated);
                eventBuilder.HandleMessageReactionAdded(events.ReactionEvents.Client_ReactionAddedEvent);
                eventBuilder.HandleMessageReactionRemoved(events.ReactionEvents.Client_ReactionRemovedEvent);
            });

            builder.UseCommands(async (serviceProvider, extension) =>
            {
                extension.AddCommands([
                    typeof(slashcommands),
                    typeof(ticketcommands.ticketingCommands),
                    typeof(antiraidcommands.antiraidcmds),
                    typeof(utilitycommands.UtilityCommandsGroup),
                    typeof(HelpCommand),
                    typeof(mutecounter.MCounter)
                ]);

                extension.AddCommands([typeof(testcommands)], 1349792051134468156);
            });

            Client = builder.Build();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
