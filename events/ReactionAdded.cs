using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Stormtility.events
{
    public class ReactionAdded
    {
        public static readonly ConcurrentDictionary<ulong, byte> ids = new();

        private static bool isInit = false;
        private static readonly SemaphoreSlim initLock = new(1, 1);

        public static async Task Client_ReactionAddedEvent(DiscordClient sender, MessageReactionAddedEventArgs args)
        {
            if(args.Message.Id != 1386497117379493980)
            {
                return;
            }

            if(!isInit)
            {
                await initLock.WaitAsync();
                try
                {
                    if(!isInit)
                    {
                        var check = DiscordEmoji.FromUnicode("✅");
                        CancellationToken token = new();
                        var historicUsers = args.Message.GetReactionsAsync(check, token);

                        await foreach(var user in historicUsers)
                        {
                            if(!user.IsBot)
                            {
                                ids.TryAdd(user.Id, 0);
                            }
                        }
                        isInit = true;
                    }
                } finally
                {
                    initLock.Release();
                }
            }

            if(!args.User.IsBot)
            {
                ids.TryAdd(args.User.Id, 0);
            }
        }
    }
}