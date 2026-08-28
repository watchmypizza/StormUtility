using System.Text.Json;
using Stormtility.config;

namespace Stormtility.Helpers
{
    public class Logger
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<bool> LOGGER(Exception e)
        {
            try
            {
                var url = Program.webhookURL;

                var payload = new
                {
                    content = $"**[ERROR]** {DateTime.Now}\n```\n{e.Message}\n{e.StackTrace?[..Math.Min(e.StackTrace.Length, 1500)]}\n```"
                };

                string json = JsonSerializer.Serialize(payload);

                using StringContent stringContent = new(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, stringContent);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Webhook Error] Discord returned: {response.StatusCode}");
                }
                return true;
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"FAILED TO SEND WEBHOOK: {logEx.Message}");
                return false;
            }
        }

        public static async Task DebugLog(string message)
        {
            if(!Vars.DebugMode)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"/// DEBUG: {message} ///");
            Console.ResetColor();
        }

        public static async Task ConsoleLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"/// CONLOG: {message} ///");
            Console.ResetColor();
        }
    }
}
