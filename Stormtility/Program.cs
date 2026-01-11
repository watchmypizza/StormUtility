using DSharpPlus;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;

namespace gamebot
{
    public class Program
    {
        private static string getEnv(string envFilePath)
        {
            if(File.Exists(envFilePath))
            {
                var lines = File.ReadAllLines(envFilePath);
                foreach(var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if(parts.Length == 2 && parts[0] == "DISCORD_BOT_TOKEN")
                    {
                        return parts[1];
                    }
                }
            }
            return "";
        }

        string Token = getEnv(".env");
        
        DiscordClient client = new DiscordClient(new DiscordConfiguration()
        {
            Token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        });
    }
}