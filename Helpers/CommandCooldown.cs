using System.Collections.Concurrent;

namespace Stormtility.Helpers {
    public class CommandCooldown {
        private static readonly ConcurrentDictionary<(string, ulong), DateTimeOffset> cooldownDict = [];

        public static bool CheckCooldown(string command, ulong userId)
        {
            if (cooldownDict.TryGetValue((command, userId), out DateTimeOffset timeToExpiry))
            {
                if (timeToExpiry > DateTimeOffset.UtcNow)
                {
                    return false;
                }

                cooldownDict.TryRemove((command, userId), out _);
            }

            return true;
        }

        public static void RemoveCooldown(string command, ulong userId)
        {
            cooldownDict.TryRemove((command, userId), out _);
            return;
        }

        public static void RemoveCooldown(ulong userId)
        {
            foreach (var entry in cooldownDict)
            {
                if (entry.Key.Item2 == userId)
                {
                    cooldownDict.TryRemove(entry.Key, out _);
                }
            }
            return;
        }

        public static string GetRemainingTime(string command, ulong userId)
        {
            if (cooldownDict.TryGetValue((command, userId), out DateTimeOffset expiryTime))
            {
                TimeSpan remaining = expiryTime - DateTimeOffset.UtcNow;
                return Formatting.FormatTimeSpan(remaining);
            }

            return "Something went wrong processing your request.";
        }

        public static void AddCooldown(string command, ulong userId, int hours) {
            try
            {
                cooldownDict[(command, userId)] = DateTimeOffset.UtcNow.AddHours(hours);
            } catch(Exception e) {
                _ = Logger.LOGGER(e);
            }
            return;
        }
    }
}
