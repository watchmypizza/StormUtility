using DiscordRPC;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;

namespace Stormtility.commands
{
    public class refBan
    {
        public static string banFile = "./banFile.txt";
        public static string banFileTemp = "./banFile.txt.tmp";
        private const string Delimiter = ",";

        public static async Task<bool> refreshBans(bool? manualRefresh = false)
        {
            try
            {
                if (!File.Exists(banFile))
                {
                    string lowerPath = banFile.ToLower();
                    if (File.Exists(lowerPath))
                    {
                        Console.WriteLine($"[Linux Boot] Notice: Path case mismatch. Redirecting to: {lowerPath}");
                        banFile = lowerPath;
                    }
                    else
                    {
                        Console.WriteLine($"[Linux Error] File absolutely not found at: {banFile}");
                        return false;
                    }
                }

                string[] allLines = await File.ReadAllLinesAsync(banFile);
                List<string> updatedLines = new List<string>();
                bool dataChanged = false;

                foreach (string line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(",", StringSplitOptions.TrimEntries);
                    
                    var (userId, time) = parts switch
                    {
                        [var id, var t] => (id, t),
                        _ => ("0", "Unknown")
                    };

                    bool isValidDate = DateTime.TryParseExact(
                        time, 
                        "O",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out DateTime banDateTime
                    );

                    if (isValidDate)
                    {
                        DateTime expirationTime = banDateTime.AddDays(1);

                        if (DateTime.UtcNow >= expirationTime || manualRefresh == true)
                        {
                            Console.WriteLine($"[Linux Ban] Target matching refresh rule: {userId}");
                            
                            try
                            {
                                await Stormtility.commands.gamecommands.sendRequest("ban", userId, "Exploits");
                                
                                string newTimeStr = DateTime.UtcNow.ToString("O");
                                updatedLines.Add($"{userId},{newTimeStr}");
                                dataChanged = true;
                            }
                            catch (Exception apiEx)
                            {
                                Console.WriteLine($"[Linux API Error] {userId} skipped: {apiEx.Message}");
                                updatedLines.Add(line); 
                            }
                        }
                        else
                        {
                            updatedLines.Add(line);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Linux Warning] Line skip. Invalid timestamp format structural issue: '{time}'");
                        updatedLines.Add(line);
                    }
                }

                if (dataChanged)
                {
                    await File.WriteAllLinesAsync(banFile, updatedLines);
                    Console.WriteLine("[Linux Sync] Ban file changes flushed to disk safely.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Linux Exception] Fatal breakdown: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> writeToFile(long userId)
        {
            if (!File.Exists(banFile))
            {
                await File.WriteAllTextAsync(banFile, "");
            }

            string stringToAppend = $"{userId},{DateTime.UtcNow.ToString("O")}" + Environment.NewLine;
            
            await File.AppendAllTextAsync(banFile, stringToAppend);

            return true;
        }

        public static async Task<bool> removeFromFile(long userId)
        {
            if(!File.Exists(banFile))
            {
                return false;
            }

            string targetPrefix = $"{userId}{Delimiter}";

            try
            {
                using (StreamReader reader = new StreamReader(banFile))
                using (StreamWriter writer = new StreamWriter(banFileTemp))
                {
                    string line;
                    while((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith(targetPrefix))
                        {
                            continue;
                        } else
                        {
                            writer.WriteLine(line);
                        }
                    }
                }

                File.Delete(banFile);
                File.Move(banFileTemp, banFile);

                return true;
            } catch (IOException e)
            {
                Console.WriteLine("! A critical error has occured writing the file !");
                Console.WriteLine($"{e}");
                return false;
            }
        }
    }
}