using System;
using System.Threading.Tasks;
using GTranslate.Translators;

namespace Stormtility.Helpers.translateMessage
{
    public class translateMessage
    {
        private readonly AggregateTranslator _webTranslator;

        public translateMessage()
        {
            _webTranslator = new AggregateTranslator();
        }

        public async Task<string> ProcessTranslationAsync(string text, string targetLanguage = "en")
        {
            try
            {
                var webResult = await _webTranslator.TranslateAsync(text, targetLanguage, "auto");
                return webResult.Translation;
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GTranslate failed: {ex.Message}");
                return "Translation Error";
            }
        }
    }
}
