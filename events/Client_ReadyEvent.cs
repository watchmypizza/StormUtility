using DSharpPlus;
using DSharpPlus.Entities;
using Stormtility;
using Stormtility.commands;
using System.Text.Json.Nodes;

namespace Stormtility.events
{
    public class Client_ReadyEvent
    {
        public static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Logged in as ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            Console.ResetColor();
            Console.WriteLine();

            var activity = new DiscordActivity()
            {
                Name = "Game Moderation | /help",
                ActivityType = ActivityType.Watching,
            };
            sender.UpdateStatusAsync(activity);
            Console.WriteLine("Status set!");

            var httpService = new HttpClient();
            var response = httpService.GetAsync($"https://apis.roblox.com/universes/v1/places/{gamecommands.BanLogEntry.placeId}/universe").Result;
            var json = JsonObject.Parse(response.Content.ReadAsStringAsync().Result);
            gamecommands.BanLogEntry.universeId = json!["universeId"].GetValue<ulong>();

            return Task.CompletedTask;
        }
    }
}