using DSharpPlus.Entities;
using Stormtility.config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Stormtility
{
    public class TranslateMorsecode
    {
        private static Dictionary<char, string> morseCodeDict = new Dictionary<char, string>()
        {
            { 'A', ".-" },
            { 'B', "-..." },
            { 'C', "-.-." },
            { 'D', "-.." },
            { 'E', "." },
            { 'F', "..-." },
            { 'G', "--." },
            { 'H', "...." },
            { 'I', ".." },
            { 'J', ".---" },
            { 'K', "-.-" },
            { 'L', ".-.." },
            { 'M', "--" },
            { 'N', "-." },
            { 'O', "---" },
            { 'P', ".--." },
            { 'Q', "--.-" },
            { 'R', ".-." },
            { 'S', "..." },
            { 'T', "-" },
            { 'U', "..-" },
            { 'V', "...-" },
            { 'W', ".--" },
            { 'X', "-..-" },
            { 'Y', "-.--" },
            { 'Z', "--.." },
            { '0', "-----" },
            { '1', ".----" },
            { '2', "..---" },
            { '3', "...--" },
            { '4', "....-" },
            { '5', "....." },
            { '6', "-...." },
            { '7', "--..." },
            { '8', "---.." },
            { '9', "----." },
            { ' ', "/" }
        };

        public async Task<bool> translateMorse(DiscordMessage message)
        {
            if (message.Author!.IsBot)
                return false;

            string msgContent = message.Content.Trim();

            string[] parts = msgContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, char> morseToLetter =
                morseCodeDict.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            StringBuilder sb = new StringBuilder();
            int alerts = 0;
            bool foundMorse = false;

            foreach (string part in parts)
            {
                if (part == "/")
                {
                    sb.Append(' ');
                    continue;
                }

                if (morseToLetter.TryGetValue(part, out char translated))
                {
                    sb.Append(translated);
                    alerts++;
                    foundMorse = true;
                }
            }

            if (foundMorse && alerts >= 3 && Vars.MorseTranslateEnabled)
            {
                await message.RespondAsync("Translated morsecode: " + sb.ToString());
            }

            if(!Vars.MorseTranslateEnabled && foundMorse && alerts >= 3)
            {
                await message.DeleteAsync();
            }

            return false;
        }
    }
}
