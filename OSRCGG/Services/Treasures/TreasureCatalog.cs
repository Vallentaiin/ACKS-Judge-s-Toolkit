using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed class TreasureTypeRow
    {
        public string TreasureType { get; private set; }
        public string Category { get; private set; }
        public int AverageValueGp { get; private set; }
        public int AverageMagicItemValueGp { get; private set; }
        public TreasureCoinColumn[] Coins { get; private set; }
        public TreasureRollColumn Gems { get; private set; }
        public TreasureRollColumn Jewelry { get; private set; }
        public TreasureMagicInstruction[] MagicItems { get; private set; }

        public TreasureTypeRow(
            string treasureType,
            string category,
            int averageValueGp,
            int averageMagicItemValueGp,
            TreasureCoinColumn[] coins,
            TreasureRollColumn gems,
            TreasureRollColumn jewelry,
            TreasureMagicInstruction[] magicItems)
        {
            TreasureType = treasureType;
            Category = category;
            AverageValueGp = averageValueGp;
            AverageMagicItemValueGp = averageMagicItemValueGp;
            Coins = coins ?? new TreasureCoinColumn[0];
            Gems = gems ?? TreasureRollColumn.None;
            Jewelry = jewelry ?? TreasureRollColumn.None;
            MagicItems = magicItems ?? new TreasureMagicInstruction[0];
        }
    }

    internal sealed class TreasureCoinColumn
    {
        public string Currency { get; private set; }
        public int ChancePercent { get; private set; }
        public string ThousandsExpression { get; private set; }
        public int GpPerCoin { get; private set; }
        public int CoinsPerGp { get; private set; }

        public TreasureCoinColumn(string currency, int chancePercent, string thousandsExpression, int gpPerCoin, int coinsPerGp)
        {
            Currency = currency;
            ChancePercent = chancePercent;
            ThousandsExpression = thousandsExpression;
            GpPerCoin = gpPerCoin;
            CoinsPerGp = coinsPerGp;
        }
    }

    internal sealed class TreasureRollColumn
    {
        public static readonly TreasureRollColumn None = new TreasureRollColumn(0, "", "");

        public int ChancePercent { get; private set; }
        public string CountExpression { get; private set; }
        public string Kind { get; private set; }

        public TreasureRollColumn(int chancePercent, string countExpression, string kind)
        {
            ChancePercent = chancePercent;
            CountExpression = countExpression ?? "";
            Kind = kind ?? "";
        }
    }

    internal sealed class TreasureMagicInstruction
    {
        public int ChancePercent { get; private set; }
        public string CountExpression { get; private set; }
        public string Kind { get; private set; }

        public TreasureMagicInstruction(int chancePercent, string countExpression, string kind)
        {
            ChancePercent = chancePercent;
            CountExpression = countExpression ?? "1";
            Kind = kind ?? "Any";
        }
    }

    internal static class TreasureCatalog
    {
        private static readonly TreasureTypeRow[] ClassicRows = CreateClassicRows();
        private static readonly TreasureTypeRow[] HeroicRows = CreateHeroicRows();

        public static IEnumerable<TreasureTypeSummary> Summaries
        {
            get
            {
                return ClassicRows.Select(r => new TreasureTypeSummary(r.TreasureType, r.Category, r.AverageValueGp));
            }
        }

        public static TreasureTypeRow FindRow(TreasureTableMode mode, string treasureType)
        {
            TreasureTypeRow[] rows = mode == TreasureTableMode.Heroic ? HeroicRows : ClassicRows;
            string normalized = NormalizeTreasureType(treasureType);
            return rows.FirstOrDefault(r => string.Equals(r.TreasureType, normalized, StringComparison.OrdinalIgnoreCase))
                ?? rows[0];
        }

        public static TreasureTypeRow FindClosestByAverageValue(TreasureTableMode mode, int targetGp, string preferredCategory)
        {
            TreasureTypeRow[] rows = mode == TreasureTableMode.Heroic ? HeroicRows : ClassicRows;
            IEnumerable<TreasureTypeRow> candidates = rows;
            if (!string.IsNullOrWhiteSpace(preferredCategory))
            {
                TreasureTypeRow[] sameCategory = rows
                    .Where(r => string.Equals(r.Category, preferredCategory, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (sameCategory.Length > 0) candidates = sameCategory;
            }

            targetGp = Math.Max(1, targetGp);
            return candidates
                .OrderBy(r => Math.Abs(r.AverageValueGp - targetGp))
                .ThenBy(r => r.AverageValueGp)
                .First();
        }

        public static string NormalizeTreasureType(string treasureType)
        {
            if (string.IsNullOrWhiteSpace(treasureType)) return "A";
            char c = char.ToUpperInvariant(treasureType.Trim()[0]);
            return c < 'A' || c > 'R' ? "A" : c.ToString();
        }

        private static TreasureCoinColumn Coin(string currency, int chance, string dice)
        {
            int gpPerCoin = 0;
            int coinsPerGp = 1;
            switch (currency)
            {
                case "cp": coinsPerGp = 100; break;
                case "sp": coinsPerGp = 10; break;
                case "ep": coinsPerGp = 2; break;
                case "gp": gpPerCoin = 1; break;
                case "pp": gpPerCoin = 10; break;
            }

            return new TreasureCoinColumn(currency, chance, dice, gpPerCoin, coinsPerGp);
        }

        private static TreasureRollColumn Roll(int chance, string count, string kind)
        {
            return new TreasureRollColumn(chance, count, kind);
        }

        private static TreasureMagicInstruction Magic(int chance, string count, string kind)
        {
            return new TreasureMagicInstruction(chance, count, kind);
        }

        private static TreasureTypeRow Row(
            string type,
            string category,
            int average,
            int averageMagic,
            TreasureCoinColumn[] coins,
            TreasureRollColumn gems,
            TreasureRollColumn jewelry,
            params TreasureMagicInstruction[] magic)
        {
            return new TreasureTypeRow(type, category, average, averageMagic, coins, gems, jewelry, magic);
        }

        private static TreasureCoinColumn[] Coins(
            int cpChance, string cp,
            int spChance, string sp,
            int epChance, string ep,
            int gpChance, string gp,
            int ppChance, string pp)
        {
            return new[]
            {
                Coin("cp", cpChance, cp),
                Coin("sp", spChance, sp),
                Coin("ep", epChance, ep),
                Coin("gp", gpChance, gp),
                Coin("pp", ppChance, pp)
            };
        }

        private static TreasureTypeRow[] CreateClassicRows()
        {
            return new[]
            {
                Row("A", "Incidental", 275, 150, Coins(0, "", 30, "1d4", 0, "", 0, "", 0, ""), Roll(30, "1d4", "ornamentals"), Roll(30, "1d4", "trinkets"), Magic(1, "1", "Any")),
                Row("B", "Hoarder", 500, 1500, Coins(0, "", 80, "1d6", 0, "", 0, "", 0, ""), Roll(70, "1d4", "ornamentals"), Roll(30, "1d4", "trinkets"), Magic(5, "2", "Any")),
                Row("C", "Incidental", 700, 750, Coins(0, "", 0, "", 15, "1d4", 0, "", 0, ""), Roll(40, "1d6", "gems"), Roll(30, "1d6", "trinkets"), Magic(5, "1", "Any")),
                Row("D", "Hoarder", 1000, 4500, Coins(0, "", 80, "1d6", 20, "1d4", 0, "", 0, ""), Roll(80, "1d6", "ornamentals"), Roll(70, "1d4", "trinkets"), Magic(15, "2", "Any")),
                Row("E", "Raider", 1250, 2500, Coins(80, "2d20", 70, "3d6", 0, "", 0, "", 0, ""), Roll(60, "1d4", "ornamentals"), Roll(40, "1d4", "trinkets"), Magic(15, "1", "SwordWeaponArmor"), Magic(15, "1", "Potion"), Magic(5, "1", "Any")),
                Row("F", "Incidental", 1500, 1000, Coins(0, "", 30, "1d4", 0, "", 15, "1d4", 0, ""), Roll(40, "1d6", "gems"), Roll(30, "1d4", "jewelry"), Magic(7, "1", "Any")),
                Row("G", "Raider", 2000, 5500, Coins(70, "2d20", 70, "3d6", 50, "1d4", 0, "", 0, ""), Roll(50, "1d6", "ornamentals"), Roll(50, "1d6", "trinkets"), Magic(25, "1", "SwordWeaponArmor"), Magic(25, "1", "Potion"), Magic(10, "1", "Any")),
                Row("H", "Hoarder", 2500, 19000, Coins(0, "", 25, "1d6", 70, "1d6", 0, "", 0, ""), Roll(80, "1d6", "gems"), Roll(80, "1d6", "trinkets"), Magic(25, "3", "Any"), Magic(100, "1", "Potion"), Magic(100, "1", "Scroll")),
                Row("I", "Incidental", 3250, 3000, Coins(0, "", 25, "1d4", 0, "", 25, "1d6", 0, ""), Roll(50, "2d4", "gems"), Roll(40, "1d8", "jewelry"), Magic(20, "1", "Any")),
                Row("J", "Raider", 4000, 11000, Coins(50, "3d6", 70, "2d20", 70, "1d8", 0, "", 0, ""), Roll(50, "1d6", "gems"), Roll(50, "1d8", "trinkets"), Magic(50, "1", "SwordWeaponArmor"), Magic(45, "1", "Potion"), Magic(20, "1", "Any")),
                Row("K", "Incidental", 5000, 6000, Coins(0, "", 0, "", 30, "1d4", 25, "1d6", 0, ""), Roll(25, "1d4", "brilliants"), Roll(50, "1d4", "jewelry"), Magic(40, "1", "Any")),
                Row("L", "Raider", 6000, 16500, Coins(40, "3d6", 60, "2d10", 75, "3d6", 0, "", 0, ""), Roll(60, "1d6", "gems"), Roll(40, "1d4", "jewelry"), Magic(75, "1", "SwordWeaponArmor"), Magic(75, "1", "Potion"), Magic(30, "1", "Any")),
                Row("M", "Incidental", 8000, 9000, Coins(0, "", 0, "", 25, "1d4", 0, "", 15, "1d4"), Roll(30, "1d6", "brilliants"), Roll(50, "1d6", "jewelry"), Magic(30, "2", "Any")),
                Row("N", "Hoarder", 9000, 38000, Coins(0, "", 60, "1d8", 60, "2d4", 80, "1d6", 0, ""), Roll(80, "1d8", "gems"), Roll(80, "1d8", "jewelry"), Magic(50, "4", "Any"), Magic(100, "1", "Potion"), Magic(100, "1", "Scroll")),
                Row("O", "Raider", 12000, 27000, Coins(30, "3d6", 50, "3d6", 60, "3d6", 60, "2d6", 0, ""), Roll(30, "1d4", "brilliants"), Roll(60, "1d4", "jewelry"), Magic(75, "1", "SwordWeaponArmor"), Magic(75, "2", "Potion"), Magic(50, "2", "Any")),
                Row("P", "Incidental", 17000, 18000, Coins(0, "", 0, "", 0, "", 30, "1d4", 30, "1d4"), Roll(40, "1d4", "brilliants"), Roll(30, "1d4", "regalia"), Magic(40, "3", "Any")),
                Row("Q", "Hoarder", 22000, 65000, Coins(0, "", 0, "", 50, "1d8", 80, "2d6", 40, "1d4"), Roll(60, "1d6", "brilliants"), Roll(80, "1d4", "jewelry"), Magic(100, "1d4", "Potion"), Magic(100, "1d4", "Scroll"), Magic(50, "6", "Any")),
                Row("R", "Hoarder", 45000, 250000, Coins(0, "", 0, "", 50, "1d6", 60, "1d6", 80, "1d8"), Roll(70, "1d4", "brilliants"), Roll(60, "1d4", "regalia"), Magic(100, "2d4", "Potion"), Magic(100, "2d4", "Scroll"), Magic(75, "1d3", "EachClassicMajor"))
            };
        }

        private static TreasureTypeRow[] CreateHeroicRows()
        {
            return new[]
            {
                Row("A", "Incidental", 275, 150, Coins(30, "2d4", 30, "1d3", 0, "", 0, "", 0, ""), Roll(30, "1d4", "ornamentals"), Roll(30, "1d4", "trinkets"), Magic(20, "1", "Common"), Magic(2, "1", "Uncommon")),
                Row("B", "Hoarder", 500, 1500, Coins(80, "4d4", 80, "1d4", 0, "", 0, "", 0, ""), Roll(70, "1d4", "ornamentals"), Roll(30, "1d4", "trinkets"), Magic(50, "1", "Common"), Magic(25, "1", "Uncommon"), Magic(5, "1", "Rare")),
                Row("C", "Incidental", 700, 750, Coins(35, "2d4", 35, "1d3", 10, "1d3", 0, "", 0, ""), Roll(40, "1d6", "gems"), Roll(30, "1d6", "trinkets"), Magic(25, "1", "Common"), Magic(15, "1", "Uncommon"), Magic(2, "1", "Rare")),
                Row("D", "Hoarder", 1000, 4500, Coins(80, "4d4", 80, "1d4", 20, "1d4", 0, "", 0, ""), Roll(80, "1d6", "ornamentals"), Roll(70, "1d4", "trinkets"), Magic(50, "2d2", "Common"), Magic(50, "1", "Uncommon"), Magic(20, "1", "Rare")),
                Row("E", "Raider", 1250, 2500, Coins(70, "2d10", 60, "2d6", 20, "1d4", 10, "1d3", 0, ""), Roll(60, "1d4", "ornamentals"), Roll(40, "1d4", "trinkets"), Magic(50, "2d4", "Common"), Magic(25, "1d3", "Uncommon")),
                Row("F", "Incidental", 1500, 1000, Coins(35, "2d4", 35, "1d4", 15, "1d3", 10, "1d3", 0, ""), Roll(40, "1d6", "gems"), Roll(30, "1d4", "jewelry"), Magic(30, "1", "Common"), Magic(20, "1", "Uncommon"), Magic(3, "1", "Rare")),
                Row("G", "Raider", 2000, 5500, Coins(70, "2d20", 60, "3d6", 30, "1d4", 20, "1d3", 0, ""), Roll(50, "1d6", "ornamentals"), Roll(50, "1d6", "trinkets"), Magic(60, "2d6", "Common"), Magic(35, "1d4", "Uncommon"), Magic(10, "1", "Rare")),
                Row("H", "Hoarder", 2500, 19000, Coins(80, "4d4", 80, "1d8", 50, "1d4", 0, "", 0, ""), Roll(80, "1d6", "gems"), Roll(80, "1d6", "trinkets"), Magic(50, "2d6", "Common"), Magic(30, "1d8", "Uncommon"), Magic(25, "1d2", "Rare"), Magic(15, "1", "Very Rare"), Magic(1, "1", "Legendary")),
                Row("I", "Incidental", 3250, 3000, Coins(35, "2d4", 35, "1d6", 15, "1d6", 15, "1d6", 0, ""), Roll(50, "2d4", "gems"), Roll(40, "1d8", "jewelry"), Magic(35, "2d2", "Common"), Magic(30, "1", "Uncommon"), Magic(4, "1", "Rare"), Magic(2, "1", "Very Rare")),
                Row("J", "Raider", 4000, 11000, Coins(80, "2d20", 60, "3d6", 50, "1d8", 35, "1d6", 0, ""), Roll(50, "1d6", "gems"), Roll(50, "1d8", "trinkets"), Magic(70, "2d8", "Common"), Magic(50, "1d4", "Uncommon"), Magic(15, "1", "Rare"), Magic(5, "1", "Very Rare")),
                Row("K", "Incidental", 5000, 6000, Coins(35, "3d6", 35, "1d8", 20, "1d6", 20, "1d6", 0, ""), Roll(25, "1d4", "brilliants"), Roll(50, "1d4", "jewelry"), Magic(45, "2d4", "Common"), Magic(35, "1", "Uncommon"), Magic(15, "1", "Rare"), Magic(3, "1", "Very Rare")),
                Row("L", "Raider", 6000, 16500, Coins(80, "3d20", 60, "4d6", 60, "1d8", 50, "1d6", 0, ""), Roll(60, "1d6", "gems"), Roll(40, "1d4", "jewelry"), Magic(80, "2d8", "Common"), Magic(60, "1d4", "Uncommon"), Magic(25, "1", "Rare"), Magic(10, "1", "Very Rare"), Magic(1, "1", "Legendary")),
                Row("M", "Incidental", 8000, 9000, Coins(35, "7d6", 35, "1d8", 20, "1d6", 20, "1d6", 10, "1d2"), Roll(30, "1d6", "brilliants"), Roll(50, "1d6", "jewelry"), Magic(50, "2d6", "Common"), Magic(40, "1", "Uncommon"), Magic(20, "1", "Rare"), Magic(5, "1", "Very Rare"), Magic(2, "1", "Legendary")),
                Row("N", "Hoarder", 9000, 38000, Coins(80, "5d6", 80, "2d6", 75, "2d4", 60, "1d6", 0, ""), Roll(80, "1d8", "gems"), Roll(80, "1d8", "jewelry"), Magic(65, "2d8", "Common"), Magic(50, "1d10", "Uncommon"), Magic(35, "1d4", "Rare"), Magic(25, "1", "Very Rare"), Magic(5, "1", "Legendary")),
                Row("O", "Raider", 12000, 27000, Coins(80, "3d20", 75, "5d6", 70, "2d6", 50, "2d6", 0, ""), Roll(30, "1d4", "brilliants"), Roll(60, "1d4", "jewelry"), Magic(90, "3d6", "Common"), Magic(70, "1d6", "Uncommon"), Magic(30, "1d2", "Rare"), Magic(15, "1", "Very Rare"), Magic(5, "1", "Legendary")),
                Row("P", "Incidental", 17000, 18000, Coins(35, "7d6", 35, "1d8", 30, "1d6", 25, "1d6", 25, "1d4"), Roll(40, "1d4", "brilliants"), Roll(30, "1d4", "regalia"), Magic(50, "2d6", "Common"), Magic(50, "1d3", "Uncommon"), Magic(50, "1", "Rare"), Magic(10, "1", "Very Rare"), Magic(5, "1", "Legendary")),
                Row("Q", "Hoarder", 22000, 65000, Coins(80, "6d6", 80, "4d6", 80, "2d6", 75, "2d4", 30, "1d4"), Roll(60, "1d6", "brilliants"), Roll(80, "1d4", "jewelry"), Magic(75, "3d6", "Common"), Magic(75, "2d6", "Uncommon"), Magic(50, "1d6", "Rare"), Magic(25, "1d2", "Very Rare"), Magic(10, "1", "Legendary")),
                Row("R", "Hoarder", 45000, 250000, Coins(80, "7d6", 80, "5d6", 80, "2d6", 80, "2d4", 75, "1d6"), Roll(70, "1d4", "brilliants"), Roll(60, "1d4", "regalia"), Magic(95, "4d6", "Common"), Magic(95, "3d6", "Uncommon"), Magic(80, "2d6", "Rare"), Magic(75, "1d4", "Very Rare"), Magic(50, "1d2", "Legendary"))
            };
        }
    }
}
