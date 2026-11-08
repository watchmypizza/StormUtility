using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace Stormtility.commands.Anti_Raid_Commands.AntiRaidAutoCompleteHandler
{
    public class AutoComplete : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            var userInput = ctx.UserInput ?? string.Empty;

            var choices = new List<DiscordAutoCompleteChoice>(2) { new("true", true), new("false", false) };
            var results = choices.Where(choice => choice.Name.StartsWith(userInput, StringComparison.OrdinalIgnoreCase)).Take(2);

            return ValueTask.FromResult(results);
        }
    }
}
