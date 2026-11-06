using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace Stormtility.Helpers
{
    public class SlowmodeHelper {
        // How often the level is re-checked while no events are coming in.
        // This only lets the slowmode decay during silence; escalation happens live on every event.
        public static readonly int ScanningThreshold = 10;

        // Activity is measured over a rolling window instead of cumulative counters,
        // so old activity ages out on its own and nothing needs to be "reset".
        private static readonly TimeSpan MeasureWindow = TimeSpan.FromSeconds(30);

        private static readonly List<int> MessageThreshold = [25, 50];
        private static readonly List<int> TypingThreshold = [4, 7];
        private static readonly List<int> SlowmodeLevels = [0, 5, 10];

        private static readonly object Gate = new();
        private static readonly Queue<DateTimeOffset> MessageTimes = new();
        private static readonly Dictionary<ulong, DateTimeOffset> TypingPeople = [];

        private static DiscordChannel? genCh;
        private static int lastAppliedSlowmode = -1;
        private static CancellationTokenSource? runCts;

        public static int TryAdd(DiscordChannel channel, bool? msg = false, bool? typing = false, ulong userId = 0)
        {
            try
            {
                lock (Gate)
                {
                    if (genCh is null || channel.Id != genCh.Id || msg != true && typing != true) {
                        return 1;
                    }

                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    PruneOlderThan(now - MeasureWindow);

                    if (msg == true)
                    {
                        MessageTimes.Enqueue(now);
                    }
                    else
                    {
                        if (userId == 0) {
                            return 1;
                        }
                        // One entry per person: repeated typing events from the same user don't stack.
                        TypingPeople[userId] = now;
                    }
                }

                // Typing events don't have a matching MsgTrigger call site, so refresh here.
                if (typing == true) {
                    Evaluate();
                }

                return 0;
            } catch (Exception e) {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        public static int MsgTrigger() {
            try
            {
                Evaluate();
                return 0;
            }
            catch (Exception e)
            {
                _ = Logger.LOGGER(e);
                return 1;
            }
        }

        private static void Evaluate()
        {
            int desired;

            lock (Gate)
            {
                if (genCh is null) {
                    return;
                }

                PruneOlderThan(DateTimeOffset.UtcNow - MeasureWindow);

                int messages = MessageTimes.Count;
                int typing = TypingPeople.Count;

                if (messages >= MessageThreshold[1] || typing >= TypingThreshold[1])
                {
                    desired = SlowmodeLevels[2];
                }
                else if (messages >= MessageThreshold[0] || typing >= TypingThreshold[0])
                {
                    desired = SlowmodeLevels[1];
                }
                else
                {
                    desired = SlowmodeLevels[0];
                }

                // Only touch the API when the level actually changes.
                if (desired == lastAppliedSlowmode) {
                    return;
                }
                lastAppliedSlowmode = desired;
            }

            _ = SetSlowmodeAsync(desired);
        }

        private static async Task SetSlowmodeAsync(int seconds)
        {
            try
            {
                await genCh!.ModifyAsync(m => m.PerUserRateLimit = seconds);
            }
            catch (NotFoundException)
            {
            }
            catch (Exception e)
            {
                _ = Logger.LOGGER(e);
            }
        }

        private static void PruneOlderThan(DateTimeOffset cutoff)
        {
            while (MessageTimes.Count > 0 && MessageTimes.Peek() < cutoff) {
                MessageTimes.Dequeue();
            }

            List<ulong>? expired = null;
            foreach ((ulong id, DateTimeOffset at) in TypingPeople) {
                if (at < cutoff) {
                    (expired ??= []).Add(id);
                }
            }
            if (expired is not null) {
                foreach (ulong id in expired) {
                    TypingPeople.Remove(id);
                }
            }
        }

        public static async void Start(DiscordChannel channel) {
            runCts?.Cancel();
            CancellationTokenSource cts = runCts = new();

            genCh = channel;
            lastAppliedSlowmode = channel.PerUserRateLimit ?? 0;

            using PeriodicTimer timer = new(TimeSpan.FromSeconds(ScanningThreshold));
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token)) {
                    // Windowed counts age out by themselves; this tick just lets the level step down.
                    Evaluate();
                }
            } catch (OperationCanceledException) {
            } catch (Exception e) {
                await Logger.LOGGER(e);
            }
        }
    }
}
