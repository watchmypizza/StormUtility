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
using DSharpPlus.SlashCommands;
using Stormtility.commands.Anti_Raid_Commands.calculationsanddumbassmath;

namespace Stormtility.commands.TicketCommands
{
    public class AutoCompleteHandler : IAutoCompleteProvider
    {
        public static List<string> LoadTranscriptFiles()
        {
            var files = Directory.GetFiles("transcripts/").ToList();
            List<string> cleansedFiles = [];

            foreach(var file in files)
            {
                var nF = file.Split(".tsk", StringSplitOptions.RemoveEmptyEntries);
                var bF = nF[0].Split("/", StringSplitOptions.RemoveEmptyEntries);
                cleansedFiles.Add(bF[1]);
            }
            return cleansedFiles;
        }

        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            var files = LoadTranscriptFiles();
            var userInput = ctx.Options?.ToString() ?? string.Empty;

            var result = files.OrderBy(x => Calculationsanddumbassmath.LevenshteinRatio(x, userInput)).Take(3).Select(x => new DiscordAutoCompleteChoice(x, x));

            return ValueTask.FromResult(result);
        }
    }
}
