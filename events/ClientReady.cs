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

namespace Stormtility.events
{
    public class ClientReady
    {
        public static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            await sender.UpdateStatusAsync(new DiscordActivity("Chasing 2026 EF5", ActivityType.Playing), UserStatus.DoNotDisturb, DateTimeOffset.UtcNow);
            Console.WriteLine($"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            autocompletehandler.LoadTranscriptCache("transcripts.json");
            autocompletehandler.fileSysWatcher("./transcripts.json");
            return;
        }
    }
}