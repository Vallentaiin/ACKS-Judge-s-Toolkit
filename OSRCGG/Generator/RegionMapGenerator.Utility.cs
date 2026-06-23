using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        // Общие утилиты генератора: броски, уникальные имена, цвета, геометрия и стабильный seed.
        private int RollLandValue(Random random, string mode)
        {
            if (string.Equals(mode, "DomainWide", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "PerHex", StringComparison.OrdinalIgnoreCase))
            {
                return Roll3d3(random);
            }

            return 6;
        }

        private int Roll3d3(Random random)
        {
            return random.Next(1, 4) + random.Next(1, 4) + random.Next(1, 4);
        }

        private int Roll3d6(Random random)
        {
            return random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7);
        }

        private string PickAlignment(Random random, RegionGenerationOptions options)
        {
            int roll = random.Next(100);
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase))
            {
                if (roll < 55) return "Lawful";
                if (roll < 90) return "Neutral";
                return "Chaotic";
            }

            if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase))
            {
                if (roll < 25) return "Lawful";
                if (roll < 65) return "Neutral";
                return "Chaotic";
            }

            if (roll < 40) return "Lawful";
            if (roll < 82) return "Neutral";
            return "Chaotic";
        }

        private string GenerateUniqueSettlementName(Random random, string cultureKey)
        {
            return UniqueName(() => names.GenerateSettlementName(random, cultureKey, useRussianNames), usedSettlementNames, useRussianNames ? "Поселение" : "Settlement");
        }

        private string GenerateUniqueDomainName(Random random, string cultureKey, string settlementName)
        {
            return UniqueName(() => names.GenerateDomainName(random, cultureKey, settlementName, useRussianNames), usedDomainNames, useRussianNames ? "Домен" : "Domain");
        }

        private string GenerateUniqueRealmName(Random random, string cultureKey, string capitalName, string tier)
        {
            return UniqueName(() => names.GenerateRealmName(random, cultureKey, capitalName, tier, useRussianNames), usedRealmNames, useRussianNames ? "Государство" : "Realm");
        }

        private string GenerateUniqueFeatureName(Random random, string cultureKey, string featureKind)
        {
            return UniqueName(() => names.GenerateFeatureName(random, cultureKey, featureKind, useRussianNames), usedFeatureNames, useRussianNames ? "Название" : "Feature");
        }

        private string UniqueName(Func<string> generator, HashSet<string> used, string fallback)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                string candidate = generator();
                if (string.IsNullOrWhiteSpace(candidate)) candidate = fallback;
                if (used.Add(candidate)) return candidate;
            }

            string baseName = generator();
            if (string.IsNullOrWhiteSpace(baseName)) baseName = fallback;
            for (int index = 2; index < 999; index++)
            {
                string candidate = baseName + " " + AcksRules.ToRoman(index);
                if (used.Add(candidate)) return candidate;
            }

            string finalName = baseName + " " + Guid.NewGuid().ToString("N").Substring(0, 4);
            used.Add(finalName);
            return finalName;
        }

        private int DomainColor(int index)
        {
            int[] colors =
            {
                unchecked((int)0x6637A86B),
                unchecked((int)0x668E5AB5),
                unchecked((int)0x66C8883D),
                unchecked((int)0x6680A7D9),
                unchecked((int)0x66C06B6B),
                unchecked((int)0x66B8B044),
                unchecked((int)0x6687A35B),
                unchecked((int)0x669E7BB5)
            };
            return colors[index % colors.Length];
        }

        private int SideLength(HexMapRecord map, int side)
        {
            return side <= 1 ? map.Width : map.Height;
        }

        private int DistanceFromSide(HexCellRecord cell, HexMapRecord map, int side)
        {
            if (side == 0) return cell.Q;
            if (side == 1) return map.Width - 1 - cell.Q;
            if (side == 2) return cell.R;
            return map.Height - 1 - cell.R;
        }

        private int EdgeDistance(HexCellRecord cell, HexMapRecord map)
        {
            return Math.Min(
                Math.Min(cell.Q, map.Width - 1 - cell.Q),
                Math.Min(cell.R, map.Height - 1 - cell.R));
        }

        private List<HexCellRecord> GetNeighbors(HexMapRecord map, HexCellRecord cell)
        {
            int[][] dirs = (cell.R & 1) == 1 ? OddRowDirections : EvenRowDirections;
            List<HexCellRecord> result = new List<HexCellRecord>(6);
            foreach (int[] dir in dirs)
            {
                HexCellRecord next = GetCell(map, cell.Q + dir[0], cell.R + dir[1]);
                if (next != null) result.Add(next);
            }

            return result;
        }

        private HexCellRecord GetCell(HexMapRecord map, string key)
        {
            HexCellRecord cell;
            if (cellIndex != null && cellIndex.TryGetValue(key, out cell)) return cell;

            string[] parts = key.Split(',');
            return GetCell(map, int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private HexCellRecord GetCell(HexMapRecord map, int q, int r)
        {
            HexCellRecord cell;
            if (cellIndex != null && cellIndex.TryGetValue(CellKey(q, r), out cell)) return cell;
            return map.Cells.FirstOrDefault(c => c.Q == q && c.R == r);
        }

        private string CellKey(int q, int r)
        {
            return q + "," + r;
        }

        private int HexDistance(int q1, int r1, int q2, int r2)
        {
            int aq1 = q1 - ((r1 - (r1 & 1)) / 2);
            int aq2 = q2 - ((r2 - (r2 & 1)) / 2);
            return (Math.Abs(aq1 - aq2) + Math.Abs(aq1 + r1 - aq2 - r2) + Math.Abs(r1 - r2)) / 2;
        }

        private string Pick(Random random, string[] values)
        {
            return values[random.Next(values.Length)];
        }

        private int StableSeed(string text)
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

        private int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
