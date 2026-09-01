namespace Stormtility.Helpers {
    public class Formatting {
        public static string FormatTimeSpan(TimeSpan time) {
            List<string> parts = [];

            if (time.Days > 0)
            {
                parts.Add($"{time.Days} days");
            }
            if (time.Hours > 0)
            {
                parts.Add($"{time.Hours} hours");
            }
            if (time.Minutes > 0)
            {
                parts.Add($"{time.Minutes} minutes");
            }
            if (time.Minutes <= 0 && time.Seconds > 0)
            {
                parts.Add($"{time.Seconds} seconds");
            }
            else
            {
                parts.Add($"and {time.Seconds} seconds");
            }

            return string.Join(" ", parts);
        }
    }
}
