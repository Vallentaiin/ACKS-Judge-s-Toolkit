using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OSRCGG
{
    internal static class TreasureDice
    {
        private static readonly Regex DiceRegex = new Regex(@"^\s*(\d*)d(\d+)(?:\s*([+-])\s*(\d+))?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool Chance(Random random, int percent)
        {
            random = random ?? new Random();
            return percent >= 100 || (percent > 0 && random.Next(1, 101) <= percent);
        }

        public static int RollExpression(Random random, string expression)
        {
            random = random ?? new Random();
            if (string.IsNullOrWhiteSpace(expression) || string.Equals(expression, "none", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            string text = expression.Trim().ToLowerInvariant();
            int multiplier = 1;
            int multiplyIndex = text.IndexOf('x');
            if (multiplyIndex < 0) multiplyIndex = text.IndexOf('*');
            if (multiplyIndex >= 0)
            {
                string right = text.Substring(multiplyIndex + 1).Trim();
                int parsedMultiplier;
                if (int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedMultiplier))
                {
                    multiplier = parsedMultiplier;
                    text = text.Substring(0, multiplyIndex).Trim();
                }
            }

            int value;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value * multiplier;
            }

            Match match = DiceRegex.Match(text);
            if (!match.Success) return 0;

            int count = string.IsNullOrEmpty(match.Groups[1].Value)
                ? 1
                : int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            int sides = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += random.Next(1, sides + 1);
            }

            if (match.Groups[3].Success)
            {
                int modifier = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                total += match.Groups[3].Value == "-" ? -modifier : modifier;
            }

            return total * multiplier;
        }

        public static T RollOnTable<T>(Random random, RollRange<T>[] table, int die)
        {
            if (table == null || table.Length == 0) return default(T);
            random = random ?? new Random();
            int roll = random.Next(1, die + 1);
            foreach (RollRange<T> row in table)
            {
                if (row.Contains(roll)) return row.Value;
            }

            return table[table.Length - 1].Value;
        }

        public static int StableSeed(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text ?? "")
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return hash == int.MinValue ? 1 : Math.Abs(hash);
            }
        }
    }
}
