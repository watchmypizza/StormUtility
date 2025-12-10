using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility
{
    public class JobAutoCompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var input = ctx.OptionValue?.ToString() ?? "";

            var matches = JobCache.Jobs.Keys 
                .Where(j => j.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(j => new DiscordAutoCompleteChoice(j, j));

            return matches;
        }
    }
}
