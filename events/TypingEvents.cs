using DSharpPlus;
using DSharpPlus.EventArgs;
using Stormtility.Helpers;

namespace Stormtility.events {
    public class TypingEvents {
        public static async Task Client_HandleTypingStarted(DiscordClient sender, TypingStartedEventArgs args) {
            if (args.User.IsBot) return;

            SlowmodeHelper.TryAdd(args.Channel, typing: true, userId: args.User.Id);
        }
    }
}
