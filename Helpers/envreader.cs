namespace Stormtility
{
    public class EnvReader
    {
        public static List<string> GetEnvVariables(string Path)
        {
            List<string> env = [];
            bool fileExists = File.Exists(Path);
            if (!fileExists)
            {
                Console.WriteLine("No .env file found, exiting...");
                Environment.Exit(0);
            }
            else
            {
                string[] lines = File.ReadAllLines(Path);
                foreach (string line in lines)
                {
                    if (line.StartsWith("DISCORD_TOKEN="))
                    {
                        env.Add(line.Substring("DISCORD_TOKEN=".Length));
                    }
                    if(line.StartsWith("WEBHOOK_TOKEN"))
                    {
                        env.Add(line.Substring("WEBHOOK_TOKEN=".Length));
                    }
                }
            }
            return env;
        }
    }
}