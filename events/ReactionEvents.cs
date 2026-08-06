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
            DiscordMessage message = args.Message;
            message ??= await args.Channel.GetMessageAsync(args.Message.Id);

            await MessageEvents.RouteMessages(message, reaction: args, isReaction: true);
        }

        public static async Task Client_ReactionRemovedEvent(DiscordClient sender, MessageReactionRemovedEventArgs args)
        {
            DiscordMessage message = args.Message;
            message ??= await args.Channel.GetMessageAsync(args.Message.Id);
            await MessageEvents.RouteMessages(message, reactionRem: args, isReaction: true);
        }
    }
}
