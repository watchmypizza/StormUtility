using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Stormtility.commands.TicketCommands.autocompletehelper
{
    public class autocompletehandler : IAutocompleteProvider
    {
        public static List<string> _cachedEntries = [];
        public static void LoadTranscriptCache(string jsonFilePath)
        {
            var jsonString = File.ReadAllText(jsonFilePath);
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("transcripts", out var transcriptsElement))
            {
                _cachedEntries = transcriptsElement
                    .EnumerateObject()
                    .Select(p => p.Name)
                    .ToList();
            }
        }

        public static void fileSysWatcher(string jsonFilePath)
        {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(jsonFilePath))
            {
                Filter = Path.GetFileName(jsonFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += (sender, e) =>
            {
                LoadTranscriptCache(jsonFilePath);
            };

            watcher.EnableRaisingEvents = true;
        }

        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var userInput = ctx.OptionValue?.ToString() ?? string.Empty;

            var results = _cachedEntries
                .Where(name => name.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(name => new DiscordAutoCompleteChoice(name, name));

            return Task.FromResult(results);
        }
    }
}
