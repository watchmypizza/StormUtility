using System.Collections.Concurrent;

namespace Stormtility.Helpers {
    public class CommandCooldown {
        private static readonly ConcurrentDictionary<(string, ulong), DateTimeOffset> cooldownDict = [];

        public static bool CheckCooldown(string command, ulong userId)
        {
            if (cooldownDict.TryGetValue((command, userId), out DateTimeOffset timeToExpiry))
            {
                if (timeToExpiry <= DateTimeOffset.UtcNow)
                {
                    cooldownDict[(command, userId)] = DateTimeOffset.UtcNow.AddHours(12);
                    return false;
                }

                return true;
            }
            return false;
        }

        public static void RemoveCooldown(string command, ulong userId)
        {
            cooldownDict[(command, userId)] = DateTimeOffset.UtcNow;
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
