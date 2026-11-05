using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace Stormtility.Helpers
{
    public class SlowmodeHelper {
        public static readonly int ScanningThreshold = 30;

        private static DiscordChannel? genCh;
        private static int MessagesSent;
        private static int PeopleTyping;
        private static readonly List<int> MessageThreshold = [25, 50];
        private static readonly List<int> TypingThreshold = [4, 7];

        public static int TryAdd(bool? msg = false, bool? typing = false)
        {
            if(genCh?.Id != 1177078904863862866) {
                return 1;
            }

            try
            {
                if (msg == true)
                {
                    MessagesSent += 1;
                }
                else if (typing == true)
                {
                    PeopleTyping += 1;
                }
                else
                {
                    return 1;
                }

                return 0;
            } catch (Exception e) {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        private static int PutSlowmode()
        {
            try
            {
                if (MessageThreshold[1] <= MessagesSent || PeopleTyping >= TypingThreshold[1])
                {
                    _ = genCh!.ModifyAsync(m => m.PerUserRateLimit = 10);
                }
                else if (MessageThreshold[0] <= MessagesSent || PeopleTyping >= TypingThreshold[0])
                {
                    _ = genCh!.ModifyAsync(m => m.PerUserRateLimit = 5);
                }
                else
                {
                    _ = genCh!.ModifyAsync(m => m.PerUserRateLimit = 0);
                }

                return 0;
            }
            catch (NotFoundException)
            {
                return 0;
            }
            catch (Exception e)
            {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        private static int Void()
        {
            MessagesSent = 0;
            PeopleTyping = 0;

            return 0;
        }

        public static int MsgTrigger() {
            try
            {
                PutSlowmode();
                return 0;
            }
            catch (Exception e)
            {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        public static async void Start(DiscordChannel channel) {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(ScanningThreshold));
            genCh = channel;
            do
            {
                try {
                    PutSlowmode();
                    Void();
                } catch (Exception e) {
                    await Logger.LOGGER(e);
                    return;
                }
            } while (await timer.WaitForNextTickAsync());
        }
    }
}
