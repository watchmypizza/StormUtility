using System.Globalization;

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
                List<string> updatedLines = new();

                bool dataChanged = false;

                foreach (string line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split(Delimiter, StringSplitOptions.TrimEntries);

                    var (userId, time) = parts switch
                    {
                        [var id, var t] => (id, t),
                        _ => ("0", "Unknown")
                    };

                    bool validDate = DateTime.TryParseExact(
                        time,
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTime banDateTime
                    );

                    if (!validDate)
                    {
                        Console.WriteLine($"[Linux Warning] Invalid timestamp: '{time}'");
                        updatedLines.Add(line);
                        continue;
                    }

                    DateTime expirationTime = banDateTime.AddDays(1);

                    if (DateTime.UtcNow >= expirationTime || manualRefresh.GetValueOrDefault())
                    {
                        Console.WriteLine($"[Linux Ban] Refreshing ban: {userId}");

                        try
                        {
                            await Stormtility.commands.Gamecommands.SendRequest(
                                "ban",
                                userId,
                                "Exploits"
                            );

                            updatedLines.Add($"{userId}{Delimiter}{DateTime.UtcNow:O}");
                            dataChanged = true;
                        }
                        catch (Exception apiEx)
                        {
                            Console.WriteLine($"[Linux API Error] {userId}: {apiEx.Message}");
                            updatedLines.Add(line);
                        }
                    }
                    else
                    {
                        updatedLines.Add(line);
                    }
                }

                if (dataChanged)
                {
                    await File.WriteAllLinesAsync(banFileTemp, updatedLines);

                    if (File.Exists(banFile))
                        File.Delete(banFile);

                    File.Move(banFileTemp, banFile);

                    Console.WriteLine("[Linux Sync] Ban file safely updated.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Linux Exception] {ex.Message}");
                return false;
            }
        }


        public static async Task<bool> writeToFile(long userId)
        {
            try
            {
                if (!File.Exists(banFile))
                    await File.WriteAllTextAsync(banFile, "");

                string prefix = $"{userId}{Delimiter}";

                bool exists = File.ReadLines(banFile)
                    .Any(line => line.StartsWith(prefix));

                if (exists)
                {
                    Console.WriteLine($"[Linux Ban] User already exists: {userId}");
                    return false;
                }

                string entry = $"{userId}{Delimiter}{DateTime.UtcNow:O}{Environment.NewLine}";

                await File.AppendAllTextAsync(banFile, entry);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Linux Write Error] {ex.Message}");
                return false;
            }
        }


        public static async Task<bool> removeFromFile(long userId)
        {
            if (!File.Exists(banFile))
                return false;

            string targetPrefix = $"{userId}{Delimiter}";

            try
            {
                using StreamReader reader = new(banFile);
                using StreamWriter writer = new(banFileTemp);

                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith(targetPrefix))
                    {
                        await writer.WriteLineAsync(line);
                    }
                }

                reader.Close();
                writer.Close();

                File.Delete(banFile);
                File.Move(banFileTemp, banFile);

                return true;
            }
            catch (IOException ex)
            {
                Console.WriteLine("[Linux File Error]");
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
