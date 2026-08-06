using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stormtility.Logging
{
    public static class HttpLogger
    {
        public static async Task LogHttpErrorAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            string? requestBody = null,
            Exception? exception = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("===== HTTP ERROR =====");
            sb.AppendLine($"Time: {DateTime.UtcNow:O}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"URL: {request.RequestUri}");
            sb.AppendLine($"StatusCode: {(int)response.StatusCode} ({response.StatusCode})");

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                sb.AppendLine("---- Request Body ----");
                sb.AppendLine(PrettyJson(requestBody));
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                sb.AppendLine("---- Response Body ----");
                sb.AppendLine(PrettyJson(responseBody));
            }

            if (exception != null)
            {
                sb.AppendLine("---- Exception ----");
                sb.AppendLine(exception.ToString());
            }

            sb.AppendLine("======================");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(sb.ToString());
            Console.ResetColor();

            await File.AppendAllTextAsync("http-errors.log", sb + Environment.NewLine);
        }

        private static string PrettyJson(string input)
        {
            try
            {
                var json = JsonDocument.Parse(input);
                return JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return input;
            }
        }
    }
}
