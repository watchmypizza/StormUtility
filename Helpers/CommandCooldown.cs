namespace Stormtility.Helpers {
    public class CommandCooldown {
        private static Dictionary<ulong, DateTimeOffset> cooldownDict = [];

        public static bool CheckCooldown(ulong userId)
        {
            if (cooldownDict.TryGetValue(userId, out DateTimeOffset timeToExpiry))
            {
                if (timeToExpiry <= DateTime.UtcNow)
                {
                    cooldownDict[userId] = DateTimeOffset.UtcNow.AddHours(12);
                    return false;
                }

                return true;
            }

            cooldownDict[userId] = DateTimeOffset.UtcNow.AddHours(12);
            return false;
        }

        public static void RemoveCooldown(ulong userId)
        {
            cooldownDict[userId] = DateTimeOffset.UtcNow;
            return;
        }

        public static string GetRemainingTime(ulong userId)
        {
            if (cooldownDict.TryGetValue(userId, out DateTimeOffset expiryTime))
            {
                TimeSpan remaining = expiryTime - DateTimeOffset.UtcNow;
                return Formatting.FormatTimeSpan(remaining);
            }

            return "Something went wrong processing your request.";
        }

        public static void AddCooldown(ulong userId) {
            try
            {
                cooldownDict.TryAdd(userId, DateTimeOffset.UtcNow);
            } catch(Exception e) {
                _ = Logger.LOGGER(e);
            }
            return;
        }
    }
}
