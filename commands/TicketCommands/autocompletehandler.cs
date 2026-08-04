using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace Stormtility.commands.TicketCommands.autocompletehelper
{
    public class autocompletehandler : IAutoCompleteProvider
    {
        private static readonly object _cacheLock = new();

        private static List<string> _cachedEntries = [];

        private static FileSystemWatcher? _watcher;

        public static void LoadTranscriptCache(string jsonFilePath)
        {
            try
            {
                if (!File.Exists(jsonFilePath))
                    return;

                var jsonString = File.ReadAllText(jsonFilePath);

                using var doc = JsonDocument.Parse(jsonString);

                if (!doc.RootElement.TryGetProperty("transcripts", out var transcriptsElement))
                    return;

                var entries = transcriptsElement
                    .EnumerateObject()
                    .Select(p => p.Name)
                    .ToList();

                lock (_cacheLock)
                {
                    _cachedEntries = entries;
                }
            }
            catch (JsonException)
            {
            }
            catch (IOException)
            {
            }
        }

        public static void FileSysWatcher(string jsonFilePath)
        {
            _watcher?.Dispose();

            var fullPath = Path.GetFullPath(jsonFilePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (directory is null)
                throw new ArgumentException(
                    "Could not determine the directory of the transcript file.",
                    nameof(jsonFilePath)
                );

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(fullPath),
                NotifyFilter =
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size |
                    NotifyFilters.FileName
            };

            _watcher.Changed += (_, _) =>
            {
                Thread.Sleep(50);

                LoadTranscriptCache(fullPath);
            };

            _watcher.Created += (_, _) =>
            {
                LoadTranscriptCache(fullPath);
            };

            _watcher.Renamed += (_, _) =>
            {
                LoadTranscriptCache(fullPath);
            };

            _watcher.EnableRaisingEvents = true;

            LoadTranscriptCache(fullPath);
        }

        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(
            AutoCompleteContext ctx)
        {
            var userInput = ctx.UserInput ?? string.Empty;

            List<string> entries;

            lock (_cacheLock)
            {
                entries = _cachedEntries.ToList();
            }

            var results = entries
                .Where(name =>
                    name.StartsWith(
                        userInput,
                        StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(name =>
                    new DiscordAutoCompleteChoice(name, name))
                .ToList();

            return ValueTask.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(results);
        }
    }
}