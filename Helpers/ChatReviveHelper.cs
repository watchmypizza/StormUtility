using DSharpPlus.Entities;

namespace Stormtility.Helpers {
    public class ChatReviveHelper {
        private static DiscordRole? ChatReviveRole;
        private static DateTimeOffset LastMessage = DateTime.UtcNow.AddHours(3);
        public static DiscordChannel? genCh;

        private static async Task CheckMessage()
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
            try
            {
                do
                {
                    if (LastMessage.AddHours(3) <= DateTimeOffset.UtcNow)
                    {
                        await SendChatRevivePing();
                        LastMessage = DateTimeOffset.UtcNow.AddHours(3);
                    }
                } while (await timer.WaitForNextTickAsync());
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
            }
        }

        private static async Task SendChatRevivePing()
        {
            try
            {
                await genCh!.SendMessageAsync(new DiscordMessageBuilder().WithContent(ChatReviveRole!.Mention + " ** [[ AUTOMATIC CHAT REVIVE ]]\n-# If you'd like to disable this, visit <id:customize>").WithAllowedMentions(Mentions.All));
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                return;
            }
        }

        public static int ResetTime(DiscordMessage message) {
            try
            {
                DateTimeOffset timeSent = message.CreationTimestamp;
                LastMessage = timeSent;

                return 0;
            } catch (Exception e) {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        public static int Start(DiscordRole role, DiscordChannel channel)
        {
            try
            {
                ChatReviveRole = role;
                genCh = channel;
                _ = CheckMessage();
                return 0;
            } catch(Exception e) {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }
    }
}
