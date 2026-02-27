namespace Stormtility
{
    public class EnvReader
    {
        public static string GetEnvVariables(string Path)
        {
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
                        return line.Substring("DISCORD_TOKEN=".Length);
                    }
                }
                Console.WriteLine("DISCORD_TOKEN not found in .env file.");
                Environment.Exit(0);
            }
            return string.Empty;
        }
    }
}