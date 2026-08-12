using DSharpPlus;
using DSharpPlus.Entities;

namespace Stormtility.Helpers {
    public class CheckSuggestion {
        public static async Task IsValidSuggestion(DiscordClient sender, DiscordMessage message) {
            if ((message?.Channel?.Id ?? 0) != 1322350501081124977) return;

            if (!message?.Content.ToLower().Contains("suggestion:") ?? true)
            {
                ulong memberId = message!.Author!.Id;
                DiscordUser user = message.Author;
                DiscordMember member = (DiscordMember) user;
                if (member!.IsBot) return;
                if (member.Roles.Any(r => r.Id == 1370422448763437207)) return;

                await message.DeleteAsync();
            }

            return;
        }
    }
}
