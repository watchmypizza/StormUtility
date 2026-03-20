using System.Collections;
using System.Security.Cryptography;
using System.Text;
using DiscordRPC;
using DSharpPlus.Entities;

namespace Stormtility.Helpers
{
    public class verificationCode
    {
        private static string charSet { get; } = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz123456789";
        private static Dictionary<ulong, (string Code, DateTime Expiry)> codeCache { get; set; }

        private static List<char> separateChars()
        {
            return charSet.ToList();
        }

        private static bool tryGetCache(DiscordUser user, out (string Code, DateTime Expiry) entry)
        {
            if (codeCache == null) codeCache = new Dictionary<ulong, (string Code, DateTime Expiry)>();

            if (codeCache.TryGetValue(user.Id, out entry))
            {
                if (entry.Expiry > DateTime.UtcNow)
                    return true;
            }

            entry = default;
            return false;
        }

        private static bool addCache(DiscordUser user, string code)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expiry = now.AddMinutes(1);

            try {
                codeCache[user.Id] = (code, expiry); 
            } catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e);
                Console.WriteLine("---  STACK END  ---");
                Console.ResetColor();
                return false;
            }
            return true;
        }

        public static string GenerateCode()
        {
            var characters = separateChars();
            var sb = new StringBuilder();
            byte[] buffer = new byte[6];
            RandomNumberGenerator.Fill(buffer);

            for (int i = 0; i < 6; i++)
            {
                sb.Append(characters[buffer[i] % characters.Count]);
            }

            return sb.ToString();
        }

        public static string getCodeIfExists(DiscordUser user)
        {
            if (tryGetCache(user, out var entry))
            {
                return entry.Code;
            }

            string code = GenerateCode();
            addCache(user, code);
            return code;
        }
    }
}