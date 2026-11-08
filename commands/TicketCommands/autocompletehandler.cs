using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
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
            var userInput = ctx.UserInput ?? string.Empty;

            var result = files.Where(file => file.StartsWith(userInput, StringComparison.OrdinalIgnoreCase)).Select(file => new DiscordAutoCompleteChoice(file, file));

            return ValueTask.FromResult(result);
        }
    }
}
