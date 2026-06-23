using System;

namespace OSRCGG
{
    internal static class AcksRules
    {
        public const int DemandCount = 29;

        public static readonly int[] RoadTradeRanges = { 0, 168, 144, 96, 72, 48, 24 };
        public static readonly int[] WaterTradeRanges = { 0, 480, 360, 240, 120, 96, 48 };

        public static bool IsValidMarketClass(int marketClass)
        {
            return marketClass >= 1 && marketClass <= 6;
        }

        public static string ToRoman(int value)
        {
            switch (value)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                case 6: return "VI";
                default: return value.ToString();
            }
        }

        public static int ParseMarketClass(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;

            string cleanText = text.Trim();
            int parenIndex = cleanText.IndexOf('(');
            if (parenIndex > 0)
            {
                cleanText = cleanText.Substring(0, parenIndex).Trim();
            }

            switch (cleanText.ToUpperInvariant())
            {
                case "I": return 1;
                case "II": return 2;
                case "III": return 3;
                case "IV": return 4;
                case "V": return 5;
                case "VI": return 6;
            }

            int value;
            return int.TryParse(cleanText, out value) && IsValidMarketClass(value) ? value : -1;
        }

        public static bool IsTradeRouteInRange(int marketClassA, int marketClassB, bool isRoad, double distance)
        {
            if (!IsValidMarketClass(marketClassA) || !IsValidMarketClass(marketClassB)) return false;

            int[] ranges = isRoad ? RoadTradeRanges : WaterTradeRanges;
            return distance <= ranges[marketClassA] && distance <= ranges[marketClassB];
        }

        public static void ApplyTradeInfluence(int marketClassA, int marketClassB, double[] demandsA, double[] demandsB)
        {
            if (demandsA == null || demandsB == null) return;

            int count = Math.Min(demandsA.Length, demandsB.Length);
            double[] originalA = (double[])demandsA.Clone();
            double[] originalB = (double[])demandsB.Clone();

            if (marketClassA == marketClassB)
            {
                for (int i = 0; i < count; i++)
                {
                    double diff = originalB[i] - originalA[i];
                    if (diff > 0)
                    {
                        demandsA[i] += 1;
                        demandsB[i] -= 1;
                    }
                    else if (diff < 0)
                    {
                        demandsA[i] -= 1;
                        demandsB[i] += 1;
                    }
                }
                return;
            }

            bool aIsSmaller = marketClassA > marketClassB;
            for (int i = 0; i < count; i++)
            {
                double demandDiff = Math.Abs(originalA[i] - originalB[i]);
                if (demandDiff < 2)
                {
                    if (aIsSmaller) demandsA[i] = originalB[i];
                    else demandsB[i] = originalA[i];
                }
                else
                {
                    double direction = originalB[i] - originalA[i];
                    if (aIsSmaller)
                    {
                        if (direction > 0) demandsA[i] += 2;
                        else if (direction < 0) demandsA[i] -= 2;
                    }
                    else
                    {
                        if (direction > 0) demandsB[i] -= 2;
                        else if (direction < 0) demandsB[i] += 2;
                    }
                }
            }
        }
    }
}
