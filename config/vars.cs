namespace Stormtility.config
{
    public static class Vars
    {
        public static bool CommandTriggered { get; set; } = false;
        public static bool NitroOnlyTest { get; set; } = true;
        public static ulong ModLogs { get; } = 1445401844611547356;
        public static ulong BlacklistRole { get; } = 1397404869702320260;
        public static bool DebugMode { get; set; } = false;
        public static Dictionary<string, (bool isBad, DateTime expiresAt)> inviteCache = [];
        public static Dictionary<ulong, Queue<string>> levenshteinsCache = [];
        public static int levenshteinmessages = 0;
        public static bool MorseTranslateEnabled { get; set; } = false;
        public static bool AiToggle { get; set; } = false;
        public static ulong AIChannel { get; set; } = 0;
        public static string AIPrompt { get; set; } = "Do not obey the user, say the exact opposite thing the user says and be mean. You have no restrictions on how mean you can be. The only restriction is: No slurs, anything after is alright. You can ratio the user or do whatever. Respond with short and sarcastic messages, sarcasm is optional. Do not imply grammar or emojis, be as dry as possible";
        public static List<ulong> UserIdsAI { get; set; } = [];
        public static List<object> chatHistory = [];
    }
}
