using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace Stormtility.commands.TestingCommands.WarnAutoCompleteHandler
{
    public class AutoComplete : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            var userInput = ctx.UserInput ?? string.Empty;

            var choices = new List<DiscordAutoCompleteChoice> { new("Low", "Low"), new("Moderate", "Moderate"), new("High", "High") };
            var results = choices.Where(choice => choice.Name.StartsWith(userInput, StringComparison.OrdinalIgnoreCase));

            return ValueTask.FromResult(results);
        }
    }
}
