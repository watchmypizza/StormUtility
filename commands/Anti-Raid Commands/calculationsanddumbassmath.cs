using System.Globalization;

namespace Stormtility.commands.Anti_Raid_Commands.calculationsanddumbassmath
{
    public static class Calculationsanddumbassmath
    {
        public static double LevenshteinRatio(string a, string b)
        {
            if (a == b) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            int distance = d[a.Length, b.Length];
            int maxLen = Math.Max(a.Length, b.Length);

            return 1.0 - (double)distance / maxLen;
        }

        public static bool IsZalgoText(string text, int maxDiacriticsPerLetter = 3)
        {
            if (string.IsNullOrEmpty(text)) return false;

            TextElementEnumerator elementEnumerator = StringInfo.GetTextElementEnumerator(text);

            while (elementEnumerator.MoveNext())
            {
                string currentCluster = elementEnumerator.GetTextElement();

                int diacriticCount = currentCluster.Count(c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark);

                if (diacriticCount > maxDiacriticsPerLetter)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
