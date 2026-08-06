using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Stormtility.events
{
    public class ReactionEvents
    {
        public static async Task Client_ReactionAddedEvent(DiscordClient sender, MessageReactionAddedEventArgs args)
        {
            await MessageEvents.RouteMessages(args.Message, reaction: args, isReaction: true);
        }

        public static async Task Client_ReactionRemovedEvent(DiscordClient sender, MessageReactionRemovedEventArgs args)
        {
            await MessageEvents.RouteMessages(args.Message, reactionRem: args, isReaction: true);
        }
    }
}
