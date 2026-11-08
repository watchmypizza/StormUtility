using System.Collections.Concurrent;

namespace Stormtility.Helpers {
    public class FileHelper {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> locks = [];

        public async Task Insert(string path, string line)
        {
            var gate = locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                await using var file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await using var writer = new StreamWriter(file);

                await writer.WriteLineAsync(line);
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task Remove(string path, string line)
        {
            if (!File.Exists(path)) return;

            var gate = locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                string tempPath = path + ".tmp";

                using var reader = new StreamReader(path);
                {
                    await using StreamWriter kept = new(tempPath);

                    string? current;
                    while ((current = await reader.ReadLineAsync()) is not null)
                    {
                        if (current == line)
                        {
                            continue;
                        }
                        await kept.WriteLineAsync(current);
                    }
                }

                File.Move(tempPath, path, overwrite: true);
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<List<string>> Read(string path)
        {
            List<string> lines = [];

            if (!File.Exists(path)) return lines;

            var gate = locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                using var reader = new StreamReader(path);

                string? line;
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    lines.Add(line);
                }

                return lines;
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                return [];
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
