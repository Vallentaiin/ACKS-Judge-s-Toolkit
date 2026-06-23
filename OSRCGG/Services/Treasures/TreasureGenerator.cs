using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OSRCGG
{
    internal sealed class TreasureGenerator
    {
        public TreasureHoardResult Generate(TreasureGenerationOptions options)
        {
            options = options ?? new TreasureGenerationOptions();
            string seedKey = options.TableMode + "|" + options.TreasureType;
            return Generate(options, new Random(TreasureDice.StableSeed(seedKey + "|" + Guid.NewGuid().ToString("N"))));
        }

        public TreasureHoardResult Generate(TreasureGenerationOptions options, Random random)
        {
            options = options ?? new TreasureGenerationOptions();
            random = random ?? new Random();
            TreasureTypeRow row = TreasureCatalog.FindRow(options.TableMode, options.TreasureType);
            TreasureHoardResult result = new TreasureHoardResult
            {
                TreasureType = row.TreasureType,
                TreasureCategory = row.Category,
                TableMode = options.TableMode,
                AverageValueGp = row.AverageValueGp,
                AverageMagicItemValueGp = row.AverageMagicItemValueGp
            };

            foreach (TreasureCoinColumn coin in row.Coins)
            {
                if (!TreasureDice.Chance(random, coin.ChancePercent)) continue;
                int thousands = TreasureDice.RollExpression(random, coin.ThousandsExpression);
                if (thousands <= 0) continue;
                int amount = thousands * 1000;
                int valueGp = CoinValueGp(coin, amount);
                result.EstimatedValueGp += valueGp;
                result.Entries.Add(new TreasureEntry
                {
                    Category = "Coins",
                    Description = amount.ToString("N0", CultureInfo.InvariantCulture) + coin.Currency,
                    ValueGp = valueGp
                });
            }

            AddGemEntries(result, row.Gems, random);
            AddJewelryEntries(result, row.Jewelry, random);
            AddMagicEntries(result, row.MagicItems, options.TableMode, random);
            return result;
        }

        public TreasureHoardResult GenerateForTargetValue(int targetGp, TreasureTableMode mode, string preferredCategory, Random random)
        {
            TreasureTypeRow row = TreasureCatalog.FindClosestByAverageValue(mode, targetGp, preferredCategory);
            return Generate(new TreasureGenerationOptions
            {
                TableMode = mode,
                TreasureType = row.TreasureType
            }, random);
        }

        public string GenerateCompactForDungeonTreasure(int monsterXp, string preferredCategory, Random random, bool russian)
        {
            int targetGp = Math.Max(25, monsterXp * 4);
            TreasureHoardResult hoard = GenerateForTargetValue(targetGp, TreasureTableMode.Classic, preferredCategory, random);
            return FormatCompact(hoard, russian);
        }

        public string GenerateCompactForDungeonTreasureType(string treasureType, int monsterXp, Random random, bool russian)
        {
            List<string> types = ExtractTreasureTypes(treasureType);
            if (types.Count == 0)
            {
                return GenerateCompactForDungeonTreasure(monsterXp, "Hoarder", random, russian);
            }

            List<string> results = new List<string>();
            foreach (string type in types)
            {
                TreasureHoardResult hoard = Generate(new TreasureGenerationOptions
                {
                    TableMode = TreasureTableMode.Classic,
                    TreasureType = type
                }, random);
                results.Add(FormatCompact(hoard, russian));
            }

            return string.Join(" | ", results.ToArray());
        }

        public TreasureEntry RollGem(TreasureGemKind kind, Random random)
        {
            random = random ?? new Random();
            int roll = kind == TreasureGemKind.Ornamental
                ? TreasureDice.RollExpression(random, "2d20")
                : kind == TreasureGemKind.Brilliant
                    ? random.Next(1, 101) + 80
                    : random.Next(1, 101);

            GemRow row = GemRows.First(r => roll >= r.Min && roll <= r.Max);
            return new TreasureEntry
            {
                Category = "Gems",
                Description = row.Description + " (" + row.ValueGp.ToString("N0", CultureInfo.InvariantCulture) + "gp)",
                ValueGp = row.ValueGp
            };
        }

        public TreasureEntry RollJewelry(TreasureJewelryKind kind, Random random)
        {
            random = random ?? new Random();
            int roll = kind == TreasureJewelryKind.Trinket
                ? TreasureDice.RollExpression(random, "2d20")
                : kind == TreasureJewelryKind.Regalia
                    ? random.Next(1, 101) + 80
                    : random.Next(1, 101);

            JewelryRow row = JewelryRows.First(r => roll >= r.Min && roll <= r.Max);
            int value = TreasureDice.RollExpression(random, row.ValueExpression);
            return new TreasureEntry
            {
                Category = "Jewelry",
                Description = row.Description + " (" + value.ToString("N0", CultureInfo.InvariantCulture) + "gp)",
                ValueGp = value
            };
        }

        public TreasureEntry RollSpecialTreasure(string lotKind, Random random)
        {
            random = random ?? new Random();
            RollRange<string>[] table = SpecialTable(lotKind);
            string description = TreasureDice.RollOnTable(random, table, SpecialDie(lotKind));
            return new TreasureEntry
            {
                Category = "Special",
                Description = description
            };
        }

        public string RollMagicItem(TreasureTableMode mode, string kind, Random random)
        {
            return MagicItemCatalog.Roll(mode, kind, random);
        }

        public string Format(TreasureHoardResult hoard, bool russian, int? seed = null)
        {
            if (hoard == null) return "";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine((russian ? "Сокровища" : "Treasure")
                + " TT " + hoard.TreasureType
                + " (" + FormatTableMode(hoard.TableMode, russian) + ", " + FormatTreasureKind(hoard.TreasureCategory, russian) + ")");
            if (seed.HasValue) builder.AppendLine("Seed: " + seed.Value.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine((russian ? "Среднее по таблице без магии: " : "Table average without magic: ")
                + FormatGp(hoard.AverageValueGp, russian));
            if (hoard.AverageMagicItemValueGp > 0)
            {
                builder.AppendLine((russian ? "Средняя стоимость магии по таблице: " : "Table average magic item value: ")
                    + FormatGp(hoard.AverageMagicItemValueGp, russian));
            }

            builder.AppendLine((russian ? "Оценка выпавших монет/самоцветов/украшений без магии: " : "Rolled coin/gem/jewelry estimate without magic: ")
                + FormatGp(hoard.EstimatedValueGp, russian));
            if (hoard.AverageMagicItemValueGp > 0 && hoard.Entries.Any(e => string.Equals(e.Category, "Magic Items", StringComparison.OrdinalIgnoreCase)))
            {
                builder.AppendLine(russian
                    ? "Магические предметы коррелируют со строкой TT через шансы и среднюю стоимость; индивидуальная gp-стоимость предметов не суммируется."
                    : "Magic items correlate through TT chances and average value; individual item gp values are not totaled.");
            }
            builder.AppendLine();

            if (hoard.Entries.Count == 0)
            {
                builder.AppendLine(russian ? "Ничего не выпало." : "No treasure rolled.");
                return builder.ToString();
            }

            foreach (IGrouping<string, TreasureEntry> group in hoard.Entries.GroupBy(e => e.Category))
            {
                builder.AppendLine(FormatEntryCategory(group.Key, russian) + ":");
                foreach (TreasureEntry entry in group)
                {
                    builder.AppendLine("  - " + FormatEntry(entry, russian));
                }
            }

            return builder.ToString();
        }

        public string FormatCompact(TreasureHoardResult hoard, bool russian)
        {
            if (hoard == null) return "";
            if (hoard.Entries.Count == 0)
            {
                return "TT " + hoard.TreasureType + " " + FormatTableMode(hoard.TableMode, russian) + ": " + (russian ? "нет сокровищ" : "no treasure");
            }

            return "TT " + hoard.TreasureType + " " + FormatTableMode(hoard.TableMode, russian) + ": "
                + string.Join("; ", hoard.Entries.Select(e => FormatEntry(e, russian)).ToArray());
        }

        public string FormatEntry(TreasureEntry entry, bool russian)
        {
            if (entry == null) return "";
            if (!russian) return entry.Description ?? "";
            return LocalizeTreasureText(entry.Description ?? "");
        }

        public static string FormatTableMode(TreasureTableMode mode, bool russian)
        {
            if (!russian) return mode.ToString();
            return mode == TreasureTableMode.Heroic ? "Героическая" : "Классическая";
        }

        public static string FormatTreasureKind(string category, bool russian)
        {
            if (!russian) return category ?? "";
            if (string.Equals(category, "Incidental", StringComparison.OrdinalIgnoreCase)) return "Случайные";
            if (string.Equals(category, "Hoarder", StringComparison.OrdinalIgnoreCase)) return "Кладовые";
            if (string.Equals(category, "Raider", StringComparison.OrdinalIgnoreCase)) return "Добыча рейдеров";
            return category ?? "";
        }

        public static string FormatGp(int valueGp, bool russian)
        {
            return valueGp.ToString("N0", CultureInfo.InvariantCulture) + (russian ? " зм" : "gp");
        }

        private static string FormatEntryCategory(string category, bool russian)
        {
            if (!russian) return category ?? "";
            if (string.Equals(category, "Coins", StringComparison.OrdinalIgnoreCase)) return "Монеты";
            if (string.Equals(category, "Gems", StringComparison.OrdinalIgnoreCase)) return "Самоцветы";
            if (string.Equals(category, "Jewelry", StringComparison.OrdinalIgnoreCase)) return "Украшения";
            if (string.Equals(category, "Special", StringComparison.OrdinalIgnoreCase)) return "Особые сокровища";
            if (string.Equals(category, "Magic Items", StringComparison.OrdinalIgnoreCase)) return "Магические предметы";
            return category ?? "";
        }

        private static List<string> ExtractTreasureTypes(string treasureType)
        {
            List<string> result = new List<string>();
            string text = treasureType ?? "";
            MatchCollection matches = TreasureTypeLetterRegex.Matches(text);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                string type = match.Value.ToUpperInvariant();
                int nextIndex = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                string segment = text.Substring(match.Index, Math.Max(0, nextIndex - match.Index));
                int copies = TreasureTypeMultiplierRegex.IsMatch(segment) ? 2 : 1;
                for (int copy = 0; copy < copies; copy++) result.Add(type);
            }

            return result;
        }

        private static string LocalizeTreasureText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string localized = LocalizeKnownTreasureDescription(text)
                .Replace("copper pieces", "медных монет")
                .Replace("silver pieces", "серебряных монет")
                .Replace("electrum pieces", "электровых монет")
                .Replace("gold pieces", "золотых монет")
                .Replace("platinum pieces", "платиновых монет")
                .Replace("worth", "цена")
                .Replace("each", "за шт.");

            localized = CurrencySuffixRegex.Replace(localized, m => " " + FormatCurrencyAbbreviation(m.Value));
            localized = LocalizeKnownTreasureDescription(localized);
            return LocalizeMagicItemText(localized);
        }

        private static string FormatCurrencyAbbreviation(string currency)
        {
            switch ((currency ?? "").ToLowerInvariant())
            {
                case "cp": return "мм";
                case "sp": return "см";
                case "ep": return "эм";
                case "pp": return "пм";
                default: return "зм";
            }
        }

        private static string LocalizeKnownTreasureDescription(string text)
        {
            foreach (KeyValuePair<string, string> pair in KnownTreasureDescriptionTranslations)
            {
                if (text.StartsWith(pair.Key, StringComparison.Ordinal))
                {
                    return pair.Value + text.Substring(pair.Key.Length);
                }
            }

            return text;
        }

        private static string LocalizeMagicItemText(string text)
        {
            string localized = SpellScrollRegex.Replace(text, m => "Свиток заклинания (" + m.Groups[1].Value + " ур.)");
            localized = localized
                .Replace("Oil of ", "Масло: ")
                .Replace("Potion of ", "Зелье: ")
                .Replace("Ring of ", "Кольцо: ")
                .Replace("Scroll of ", "Свиток: ")
                .Replace("Wand of ", "Палочка: ")
                .Replace("Staff of ", "Посох: ")
                .Replace("Rod of ", "Жезл: ")
                .Replace("Sword of ", "Меч: ")
                .Replace("Treasure Maps", "Карты сокровищ")
                .Replace("Treasure Map", "Карта сокровищ")
                .Replace("Ammunition", "Боеприпасы")
                .Replace("Armor", "Броня")
                .Replace("Shield", "Щит")
                .Replace("Sword ", "Меч ")
                .Replace("Weapon ", "Оружие ");
            return localized;
        }

        private static int CoinValueGp(TreasureCoinColumn coin, int amount)
        {
            if (coin.GpPerCoin > 0) return amount * coin.GpPerCoin;
            return Math.Max(0, amount / Math.Max(1, coin.CoinsPerGp));
        }

        private void AddGemEntries(TreasureHoardResult result, TreasureRollColumn column, Random random)
        {
            if (column == null || !TreasureDice.Chance(random, column.ChancePercent)) return;
            int count = TreasureDice.RollExpression(random, column.CountExpression);
            TreasureGemKind kind = GemKind(column.Kind);
            for (int i = 0; i < count; i++)
            {
                TreasureEntry entry = RollGem(kind, random);
                result.EstimatedValueGp += entry.ValueGp;
                result.Entries.Add(entry);
            }
        }

        private void AddJewelryEntries(TreasureHoardResult result, TreasureRollColumn column, Random random)
        {
            if (column == null || !TreasureDice.Chance(random, column.ChancePercent)) return;
            int count = TreasureDice.RollExpression(random, column.CountExpression);
            TreasureJewelryKind kind = JewelryKind(column.Kind);
            for (int i = 0; i < count; i++)
            {
                TreasureEntry entry = RollJewelry(kind, random);
                result.EstimatedValueGp += entry.ValueGp;
                result.Entries.Add(entry);
            }
        }

        private void AddMagicEntries(TreasureHoardResult result, TreasureMagicInstruction[] instructions, TreasureTableMode mode, Random random)
        {
            foreach (TreasureMagicInstruction instruction in instructions ?? new TreasureMagicInstruction[0])
            {
                if (!TreasureDice.Chance(random, instruction.ChancePercent)) continue;
                int count = Math.Max(0, TreasureDice.RollExpression(random, instruction.CountExpression));
                if (string.Equals(instruction.Kind, "EachClassicMajor", StringComparison.OrdinalIgnoreCase))
                {
                    string[] kinds = { "Sword", "Armor", "Weapon", "Implement", "Miscellaneous Item", "Ring" };
                    foreach (string kind in kinds)
                    {
                        for (int i = 0; i < count; i++) AddMagicEntry(result, mode, kind, random);
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++) AddMagicEntry(result, mode, instruction.Kind, random);
                }
            }
        }

        private static void AddMagicEntry(TreasureHoardResult result, TreasureTableMode mode, string kind, Random random)
        {
            result.Entries.Add(new TreasureEntry
            {
                Category = "Magic Items",
                Description = MagicItemCatalog.Roll(mode, kind, random)
            });
        }

        private static TreasureGemKind GemKind(string kind)
        {
            if (string.Equals(kind, "ornamentals", StringComparison.OrdinalIgnoreCase)) return TreasureGemKind.Ornamental;
            if (string.Equals(kind, "brilliants", StringComparison.OrdinalIgnoreCase)) return TreasureGemKind.Brilliant;
            return TreasureGemKind.Gem;
        }

        private static TreasureJewelryKind JewelryKind(string kind)
        {
            if (string.Equals(kind, "trinkets", StringComparison.OrdinalIgnoreCase)) return TreasureJewelryKind.Trinket;
            if (string.Equals(kind, "regalia", StringComparison.OrdinalIgnoreCase)) return TreasureJewelryKind.Regalia;
            return TreasureJewelryKind.Jewelry;
        }

        private static int SpecialDie(string lotKind)
        {
            string key = NormalizeSpecialLotKind(lotKind);
            if (key == "ornamental" || key == "regalia") return 12;
            if (key == "gem" || key == "trinket" || key == "jewelry") return 10;
            if (key == "brilliant") return 8;
            return 20;
        }

        private static RollRange<string>[] SpecialTable(string lotKind)
        {
            switch (NormalizeSpecialLotKind(lotKind))
            {
                case "sp": return SpecialSp;
                case "ep": return SpecialEp;
                case "gp": return SpecialGp;
                case "pp": return SpecialPp;
                case "ornamental": return SpecialOrnamental;
                case "gem": return SpecialGem;
                case "brilliant": return SpecialBrilliant;
                case "trinket": return SpecialTrinket;
                case "jewelry": return SpecialJewelry;
                case "regalia": return SpecialRegalia;
                default: return SpecialCp;
            }
        }

        private static string NormalizeSpecialLotKind(string lotKind)
        {
            string key = (lotKind ?? "").Trim().ToLowerInvariant();
            if (key.Contains("silver") || key == "sp") return "sp";
            if (key.Contains("electrum") || key == "ep") return "ep";
            if (key.Contains("gold") || key == "gp") return "gp";
            if (key.Contains("platinum") || key == "pp") return "pp";
            if (key.Contains("ornamental")) return "ornamental";
            if (key.Contains("brilliant")) return "brilliant";
            if (key.Contains("trinket")) return "trinket";
            if (key.Contains("jewelry")) return "jewelry";
            if (key.Contains("regalia")) return "regalia";
            if (key.Contains("gem")) return "gem";
            return "cp";
        }

        private static RollRange<string> R(int min, int max, string value)
        {
            return new RollRange<string>(min, max, value);
        }

        private static readonly Regex CurrencySuffixRegex = new Regex(@"(?<=\d)(cp|sp|ep|gp|pp)\b", RegexOptions.IgnoreCase);
        private static readonly Regex SpellScrollRegex = new Regex(@"Spell Scroll \((\d+) level\)", RegexOptions.IgnoreCase);
        private static readonly Regex TreasureTypeLetterRegex = new Regex(@"(?<![A-Za-z])[A-Ra-r](?![A-Za-z])", RegexOptions.Compiled);
        private static readonly Regex TreasureTypeMultiplierRegex = new Regex(@"(?:x|×)\s*2", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Dictionary<string, string> KnownTreasureDescriptionTranslations = new Dictionary<string, string>
        {
            { "Azurite, hematite, malachite, obsidian, or quartz", "Азурит, гематит, малахит, обсидиан или кварц" },
            { "Agate, lapis lazuli, tiger eye, or turquoise", "Агат, лазурит, тигровый глаз или бирюза" },
            { "Bloodstone, crystal, citrine, jasper, moonstone, or onyx", "Кровавик, хрусталь, цитрин, яшма, лунный камень или оникс" },
            { "Carnelian, chalcedony, sardonyx, or zircon", "Сердолик, халцедон, сардоникс или циркон" },
            { "Amber, amethyst, coral, jade, jet, or tourmaline", "Янтарь, аметист, коралл, нефрит, гагат или турмалин" },
            { "Garnet, pearl, or spinel", "Гранат, жемчуг или шпинель" },
            { "Aquamarine, alexandrite, or topaz", "Аквамарин, александрит или топаз" },
            { "Opal, star ruby, star sapphire, sunset amethyst, or imperial topaz", "Опал, звездчатый рубин, звездчатый сапфир, закатный аметист или имперский топаз" },
            { "Black sapphire, diamond, emerald, jacinth, or ruby", "Черный сапфир, алмаз, изумруд, гиацинт или рубин" },
            { "Amber with preserved extinct creatures or whorled nephrite jade", "Янтарь с сохранившимися вымершими существами или узорчатый нефрит" },
            { "Black pearl, baroque pearl, or crystal geode", "Черная жемчужина, барочная жемчужина или хрустальная жеода" },
            { "Facet cut imperial topaz or flawless diamond", "Ограненный имперский топаз или безупречный алмаз" },
            { "Facet cut star sapphire or star ruby", "Ограненный звездчатый сапфир или звездчатый рубин" },
            { "Flawless facet cut diamond, emerald, jacinth, or ruby", "Безупречно ограненный алмаз, изумруд, гиацинт или рубин" },
            { "Flawless facet cut black sapphire or blue diamond", "Безупречно ограненный черный сапфир или голубой алмаз" },
            { "Bone, scrimshaw, or beast parts", "Кость, резная кость или части зверей" },
            { "Glass, shells, or wrought copper, brass, or bronze", "Стекло, раковины или кованая медь, латунь либо бронза" },
            { "Fine wood, porcelain, or wrought silver", "Ценное дерево, фарфор или кованое серебро" },
            { "Alabaster, chryselephantine, ivory, or wrought gold", "Алебастр, хрисоэлефантин, слоновая кость или кованое золото" },
            { "Carved jade or wrought platinum", "Резной нефрит или кованая платина" },
            { "Orichalcum or silver studded with turquoise, moonstone, or opal", "Орихалк или серебро с бирюзой, лунным камнем либо опалом" },
            { "Silver studded with jet, amber, or pearl", "Серебро с гагатом, янтарем или жемчугом" },
            { "Gold studded with topaz, jacinth, or ruby", "Золото с топазом, гиацинтом или рубином" },
            { "Platinum studded with diamond, sapphire, or emerald", "Платина с алмазом, сапфиром или изумрудом" },
            { "Electrum or silver pendant with pearls and star rubies", "Электровый или серебряный кулон с жемчугом и звездчатыми рубинами" },
            { "Gold or platinum with diamonds and sapphires", "Золото или платина с алмазами и сапфирами" },
            { "Gold encrusted with flawless facet cut diamonds", "Золото, инкрустированное безупречно ограненными алмазами" },
            { "Platinum encrusted with flawless black sapphires or blue diamonds", "Платина, инкрустированная безупречными черными сапфирами или голубыми алмазами" },
            { "2d20 bags of grain or vegetables, worth 5sp each", "2d20 мешков зерна или овощей, цена 5 см за шт." },
            { "4d6x10 bricks of salt, worth 7cp each", "4d6x10 соляных брусков, цена 7 мм за шт." },
            { "2d10 amphorae of beer, worth 1gp each", "2d10 амфор пива, цена 1 зм за шт." },
            { "6d6 crates of terra-cotta pottery, worth 0.5gp each", "6d6 ящиков терракотовой посуды, цена 0.5 зм за шт." },
            { "2d10 bundles of hardwood logs, worth 1gp each", "2d10 вязанок твердой древесины, цена 1 зм за шт." },
            { "2d10 amphorae of wine and spirits, worth 1gp each", "2d10 амфор вина и крепких напитков, цена 1 зм за шт." },
            { "4d20 wheels of cheese, worth 25cp each", "4d20 головок сыра, цена 25 мм за шт." },
            { "2d6 amphorae of oil or sauce, worth 1.5gp each", "2d6 амфор масла или соуса, цена 1.5 зм за шт." },
            { "1d3 amphorae of preserved fish, worth 4.5gp each", "1d3 амфор соленой или сушеной рыбы, цена 4.5 зм за шт." },
            { "1d3 small amphorae of preserved meat, worth 5gp each", "1d3 малых амфор вяленого мяса, цена 5 зм за шт." },
            { "1d2 crates of glassware, worth 7.5gp each", "1d2 ящиков стеклянной посуды, цена 7.5 зм за шт." },
            { "3d6 ingots of common metals, worth 1gp each", "3d6 слитков обычных металлов, цена 1 зм за шт." },
            { "2d4 bundles of rare wood, worth 2gp each", "2d4 вязанок редкой древесины, цена 2 зм за шт." },
            { "1,000 copper pieces", "1,000 медных монет" },
            { "100 silver pieces", "100 серебряных монет" },
            { "10,000 copper pieces", "10,000 медных монет" },
            { "2d6 bundles of common fur pelts, worth 15gp each", "2d6 связок обычных меховых шкур, цена 15 зм за шт." },
            { "1d6 rolls of woven textiles, worth 30gp each", "1d6 рулонов тканого полотна, цена 30 зм за шт." },
            { "1d3 jars of dyes and pigments, worth 50gp each", "1d3 кувшинов красителей и пигментов, цена 50 зм за шт." },
            { "1d2 bags of loose herbs, worth 75gp each", "1d2 мешков рассыпных трав, цена 75 зм за шт." },
            { "1d2 bags of clothing, worth 75gp each", "1d2 мешков одежды, цена 75 зм за шт." },
            { "1d2 crates of tools, worth 75gp each", "1d2 ящиков инструментов, цена 75 зм за шт." },
            { "1 crate of armor and weapons, worth 110gp", "1 ящик брони и оружия, цена 110 зм" },
            { "4d8 common animal antlers, horns, and tusks, worth 1d10gp each", "4d8 обычных рогов, оленьих рогов и бивней, цена 1d10 зм за шт." },
            { "1d4 captured or enslaved laborers, worth 40gp each", "1d4 пленных или порабощенных работников, цена 40 зм за шт." },
            { "1 captured or enslaved domestic servant, worth 100gp", "1 пленный или порабощенный домашний слуга, цена 100 зм" },
            { "1,000 silver pieces", "1,000 серебряных монет" },
            { "100 gold pieces", "100 золотых монет" },
            { "5,000 silver pieces", "5,000 серебряных монет" },
            { "2d100 bottles of fine wine, worth 5gp each", "2d100 бутылок хорошего вина, цена 5 зм за шт." },
            { "3d12 rugs of common fur pelts, worth 2d4x5gp each", "3d12 ковров из обычных меховых шкур, цена 2d4x5 зм за шт." },
            { "2d4x500 common bird feathers, worth 1d3sp each", "2d4x500 обычных птичьих перьев, цена 1d3 см за шт." },
            { "3d4 bundles of large common fur pelts, worth 1d8x15gp each", "3d4 связок больших обычных меховых шкур, цена 1d8x15 зм за шт." },
            { "1d12 uncommon animal antlers, horns, and tusks, worth 3d4x10gp each", "1d12 необычных рогов, оленьих рогов и бивней, цена 3d4x10 зм за шт." },
            { "1d4 collections of common books, worth 1d3x100gp each", "1d4 собраний обычных книг, цена 1d3x100 зм за шт." },
            { "1d3 bundles of large uncommon fur pelts, worth 2d4x50gp each", "1d3 связок больших необычных меховых шкур, цена 2d4x50 зм за шт." },
            { "1d3 captured or enslaved craftsmen or merchants, worth 1d4x100gp each", "1d3 пленных или порабощенных ремесленников либо купцов, цена 1d4x100 зм за шт." },
            { "1,000 electrum pieces", "1,000 электровых монет" },
            { "500 gold pieces", "500 золотых монет" },
            { "10,000 silver pieces", "10,000 серебряных монет" },
            { "1 metamphora of preserved special components, worth 5d6x60gp", "1 метамфора сохраненных особых компонентов, цена 5d6x60 зм" },
            { "1d6 fresh monster carcasses with special components, worth 1d10x50gp each", "1d6 свежих туш чудовищ с особыми компонентами, цена 1d10x50 зм за шт." },
            { "1d12x12 monster feathers, worth 3d6gp each", "1d12x12 перьев чудовищ, цена 3d6 зм за шт." },
            { "1d8 monster horns and tusks, worth 1d8x50gp each", "1d8 рогов и бивней чудовищ, цена 1d8x50 зм за шт." },
            { "1d3 bundles of rare fur pelts, worth 2d4x100gp each", "1d3 связок редких меховых шкур, цена 2d4x100 зм за шт." },
            { "2d20 pieces of elephant ivory, worth 4d4x10gp each", "2d20 кусков слоновой кости, цена 4d4x10 зм за шт." },
            { "4d4 amphorae of spices, worth 100gp each", "4d4 амфор пряностей, цена 100 зм за шт." },
            { "1d3 crates of fine porcelain, worth 500gp each", "1d3 ящиков тонкого фарфора, цена 500 зм за шт." },
            { "4d10 ingots of precious metals, worth 50gp each", "4d10 слитков драгоценных металлов, цена 50 зм за шт." },
            { "4d6 rugs of large common fur, worth 1d4x30gp each", "4d6 ковров из большого обычного меха, цена 1d4x30 зм за шт." },
            { "1 captured equerry/lady-in-waiting or enslaved hetaera/odalisque, worth 2d4x200gp", "1 пленный конюший или фрейлина, либо порабощенная гетера/одалиска, цена 2d4x200 зм" },
            { "1,000 gold pieces", "1,000 золотых монет" },
            { "200 platinum pieces", "200 платиновых монет" },
            { "5,000 gold pieces", "5,000 золотых монет" },
            { "4d6+1 rolls of silk, worth 333gp each", "4d6+1 рулонов шелка, цена 333 зм за шт." },
            { "6d10 rare books, worth 150gp each", "6d10 редких книг, цена 150 зм за шт." },
            { "5d10 capes of common fur, worth 1d6x50gp each", "5d10 плащей из обычного меха, цена 1d6x50 зм за шт." },
            { "2d6+1 rugs of large uncommon fur, worth 1d4x250gp each", "2d6+1 ковров из большого необычного меха, цена 1d4x250 зм за шт." },
            { "2d12 pieces of rare horn or tusk, worth 1d4x150gp each", "2d12 кусков редкого рога или бивня, цена 1d4x150 зм за шт." },
            { "2d8 coats of common fur, worth 1d6x150gp each", "2d8 шуб из обычного меха, цена 1d6x150 зм за шт." },
            { "4d4 pieces of unicorn or narwhale ivory, worth 2d4x100gp each", "4d4 кусков рога единорога или бивня нарвала, цена 2d4x100 зм за шт." },
            { "1 captured squire/damsel or enslaved gladiator/concubine, worth 2d4x1,000gp", "1 пленный оруженосец или девица, либо порабощенный гладиатор/наложница, цена 2d4x1,000 зм" },
            { "1,000 platinum pieces", "1,000 платиновых монет" },
            { "1d12 silver arrows, each worth 5gp", "1d12 серебряных стрел, цена 5 зм за шт." },
            { "1d12 pouches of lungwort or willowbark, worth 5gp each", "1d12 мешочков медуницы или ивовой коры, цена 5 зм за шт." },
            { "1d6 pouches of birthwort, comfrey, goldenrod, or woundwort, worth 10gp each", "1d6 мешочков кирказона, окопника, золотарника или ранозаживляющей травы, цена 10 зм за шт." },
            { "1d6 pouches of aloe, belladonna, bitterwood, blessed thistle, or wolfsbane, worth 10gp each", "1d6 мешочков алоэ, белладонны, горького дерева, благословенного чертополоха или аконита, цена 10 зм за шт." },
            { "1d4 pouches of horsetail, worth 15gp each", "1d4 мешочков хвоща, цена 15 зм за шт." },
            { "1d2 vials of holy water, worth 25gp each", "1d2 фиалов святой воды, цена 25 зм за шт." },
            { "1 ornamental", "1 орнаментальный самоцвет" },
            { "1 set of superior thieves' tools, worth 200gp", "1 набор превосходных воровских инструментов, цена 200 зм" },
            { "1d4 sets of engraved teeth, worth 2d6x10gp each", "1d4 наборов гравированных зубов, цена 2d6x10 зм за шт." },
            { "1d3 vials of rare perfume, worth 1d6x25gp each", "1d3 фиалов редких духов, цена 1d6x25 зм за шт." },
            { "2d10 sticks of rare incense, worth 5d6gp each", "2d10 палочек редких благовоний, цена 5d6 зм за шт." },
            { "1 gem or 2d6 ornamentals", "1 самоцвет или 2d6 орнаментальных самоцветов" },
            { "2d20 jade carvings of heroes, monsters, and gods, worth 200gp each", "2d20 нефритовых резных фигур героев, чудовищ и богов, цена 200 зм за шт." },
            { "1d4 sets of masterwork thieves' tools, worth 1,600gp each", "1d4 наборов мастерских воровских инструментов, цена 1,600 зм за шт." },
            { "2d4 opal cameo portraits, worth 800gp each", "2d4 опаловых камейных портрета, цена 800 зм за шт." },
            { "1d6 amethyst cylinder seals, worth 1,200gp each", "1d6 аметистовых цилиндрических печатей, цена 1,200 зм за шт." },
            { "1 brilliant or 4d8 gems", "1 бриллиант или 4d8 самоцветов" },
            { "3d6 bone fetishes and figurines, each worth 2d20gp", "3d6 костяных фетишей и фигурок, цена 2d20 зм за шт." },
            { "2d6 glass eyes, lenses, or prisms, each worth 1d6x10gp", "2d6 стеклянных глаз, линз или призм, цена 1d6x10 зм за шт." },
            { "1d4 masterwork items, worth 70+5d6gp each", "1d4 мастерских изделий, цена 70+5d6 зм за шт." },
            { "1d4 silver holy/unholy symbols, worth 2d8x10gp each", "1d4 серебряных священных/нечестивых символов, цена 2d8x10 зм за шт." },
            { "1 trinket", "1 безделушка" },
            { "1d8 trinkets", "1d8 безделушек" },
            { "1 cape of large animal fur, worth 2d4x200gp", "1 плащ из меха крупного животного, цена 2d4x200 зм" },
            { "1d10 vials of common poison, worth 2d6x25gp each", "1d10 фиалов обычного яда, цена 2d6x25 зм за шт." },
            { "1d3 statuettes, worth 1d10x100gp each", "1d3 статуэток, цена 1d10x100 зм за шт." },
            { "1d2 masterwork items, worth 2d6x100gp each", "1d2 мастерских изделий, цена 2d6x100 зм за шт." },
            { "1 piece of jewelry", "1 украшение" },
            { "4d8 pieces of jewelry", "4d8 украшений" },
            { "1d6 capes of rare animal or monster fur, worth 1d6x1000gp each", "1d6 плащей из редкого звериного меха или меха чудовищ, цена 1d6x1000 зм за шт." },
            { "1d4 coats of large common or uncommon animal fur, worth (1d6+1)x1000gp each", "1d4 шуб из большого обычного или необычного звериного меха, цена (1d6+1)x1000 зм за шт." },
            { "2d10 vials of rare poison, worth 4d4x100gp each", "2d10 фиалов редкого яда, цена 4d4x100 зм за шт." },
            { "2d10 alabaster and jet game pieces with jeweled eyes, worth 3d6x100gp each", "2d10 алебастровых и гагатовых игровых фигур с самоцветными глазами, цена 3d6x100 зм за шт." },
            { "1 coat of rare animal or monster fur, worth 2d10x1000gp", "1 шуба из редкого звериного меха или меха чудовища, цена 2d10x1000 зм" },
            { "1d8 carved ivory figurines, worth 1d4x1000gp each", "1d8 резных фигурок из слоновой кости, цена 1d4x1000 зм за шт." },
            { "1d4 platinum reliquaries with crystal panes, worth 1d8x1000gp each", "1d4 платиновых реликвариев с хрустальными створками, цена 1d8x1000 зм за шт." },
            { "1 regalia", "1 регалия" }
        };

        private sealed class GemRow
        {
            public int Min { get; private set; }
            public int Max { get; private set; }
            public int ValueGp { get; private set; }
            public string Description { get; private set; }

            public GemRow(int min, int max, int valueGp, string description)
            {
                Min = min;
                Max = max;
                ValueGp = valueGp;
                Description = description;
            }
        }

        private sealed class JewelryRow
        {
            public int Min { get; private set; }
            public int Max { get; private set; }
            public string ValueExpression { get; private set; }
            public string Description { get; private set; }

            public JewelryRow(int min, int max, string valueExpression, string description)
            {
                Min = min;
                Max = max;
                ValueExpression = valueExpression;
                Description = description;
            }
        }

        private static readonly GemRow[] GemRows =
        {
            new GemRow(1,10,10,"Azurite, hematite, malachite, obsidian, or quartz"),
            new GemRow(11,25,25,"Agate, lapis lazuli, tiger eye, or turquoise"),
            new GemRow(26,40,50,"Bloodstone, crystal, citrine, jasper, moonstone, or onyx"),
            new GemRow(41,55,75,"Carnelian, chalcedony, sardonyx, or zircon"),
            new GemRow(56,70,100,"Amber, amethyst, coral, jade, jet, or tourmaline"),
            new GemRow(71,80,250,"Garnet, pearl, or spinel"),
            new GemRow(81,90,500,"Aquamarine, alexandrite, or topaz"),
            new GemRow(91,95,750,"Opal, star ruby, star sapphire, sunset amethyst, or imperial topaz"),
            new GemRow(96,100,1000,"Black sapphire, diamond, emerald, jacinth, or ruby"),
            new GemRow(101,110,1500,"Amber with preserved extinct creatures or whorled nephrite jade"),
            new GemRow(111,125,2000,"Black pearl, baroque pearl, or crystal geode"),
            new GemRow(126,145,4000,"Facet cut imperial topaz or flawless diamond"),
            new GemRow(146,165,6000,"Facet cut star sapphire or star ruby"),
            new GemRow(166,175,8000,"Flawless facet cut diamond, emerald, jacinth, or ruby"),
            new GemRow(176,180,10000,"Flawless facet cut black sapphire or blue diamond")
        };

        private static readonly JewelryRow[] JewelryRows =
        {
            new JewelryRow(1,10,"2d20","Bone, scrimshaw, or beast parts"),
            new JewelryRow(11,25,"2d10x10","Glass, shells, or wrought copper, brass, or bronze"),
            new JewelryRow(26,40,"2d4x100","Fine wood, porcelain, or wrought silver"),
            new JewelryRow(41,70,"2d6x100","Alabaster, chryselephantine, ivory, or wrought gold"),
            new JewelryRow(71,80,"3d6x100","Carved jade or wrought platinum"),
            new JewelryRow(81,95,"1d4x1000","Orichalcum or silver studded with turquoise, moonstone, or opal"),
            new JewelryRow(96,100,"2d4x1000","Silver studded with jet, amber, or pearl"),
            new JewelryRow(101,125,"3d4x1000","Gold studded with topaz, jacinth, or ruby"),
            new JewelryRow(126,145,"2d8x1000","Platinum studded with diamond, sapphire, or emerald"),
            new JewelryRow(146,155,"3d6x1000","Electrum or silver pendant with pearls and star rubies"),
            new JewelryRow(156,165,"2d20x1000","Gold or platinum with diamonds and sapphires"),
            new JewelryRow(166,175,"1d4x10000","Gold encrusted with flawless facet cut diamonds"),
            new JewelryRow(176,180,"1d8x10000","Platinum encrusted with flawless black sapphires or blue diamonds")
        };

        private static readonly RollRange<string>[] SpecialCp =
        {
            R(1,1,"2d20 bags of grain or vegetables, worth 5sp each"),
            R(2,2,"4d6x10 bricks of salt, worth 7cp each"),
            R(3,3,"2d10 amphorae of beer, worth 1gp each"),
            R(4,4,"6d6 crates of terra-cotta pottery, worth 0.5gp each"),
            R(5,5,"2d10 bundles of hardwood logs, worth 1gp each"),
            R(6,6,"2d10 amphorae of wine and spirits, worth 1gp each"),
            R(7,7,"4d20 wheels of cheese, worth 25cp each"),
            R(8,8,"2d6 amphorae of oil or sauce, worth 1.5gp each"),
            R(9,9,"1d3 amphorae of preserved fish, worth 4.5gp each"),
            R(10,10,"1d3 small amphorae of preserved meat, worth 5gp each"),
            R(11,11,"1d2 crates of glassware, worth 7.5gp each"),
            R(12,12,"3d6 ingots of common metals, worth 1gp each"),
            R(13,13,"2d4 bundles of rare wood, worth 2gp each"),
            R(14,19,"1,000 copper pieces"),
            R(20,20,"100 silver pieces")
        };

        private static readonly RollRange<string>[] SpecialSp =
        {
            R(1,1,"10,000 copper pieces"), R(2,2,"2d6 bundles of common fur pelts, worth 15gp each"),
            R(3,3,"1d6 rolls of woven textiles, worth 30gp each"), R(4,4,"1d3 jars of dyes and pigments, worth 50gp each"),
            R(5,5,"1d2 bags of loose herbs, worth 75gp each"), R(6,6,"1d2 bags of clothing, worth 75gp each"),
            R(7,7,"1d2 crates of tools, worth 75gp each"), R(8,8,"1 crate of armor and weapons, worth 110gp"),
            R(9,9,"4d8 common animal antlers, horns, and tusks, worth 1d10gp each"),
            R(10,10,"1d4 captured or enslaved laborers, worth 40gp each"),
            R(11,11,"1 captured or enslaved domestic servant, worth 100gp"),
            R(12,19,"1,000 silver pieces"), R(20,20,"100 gold pieces")
        };

        private static readonly RollRange<string>[] SpecialEp =
        {
            R(1,1,"5,000 silver pieces"), R(2,2,"2d100 bottles of fine wine, worth 5gp each"),
            R(3,3,"3d12 rugs of common fur pelts, worth 2d4x5gp each"),
            R(4,4,"2d4x500 common bird feathers, worth 1d3sp each"),
            R(5,5,"3d4 bundles of large common fur pelts, worth 1d8x15gp each"),
            R(6,6,"1d12 uncommon animal antlers, horns, and tusks, worth 3d4x10gp each"),
            R(7,7,"1d4 collections of common books, worth 1d3x100gp each"),
            R(8,8,"1d3 bundles of large uncommon fur pelts, worth 2d4x50gp each"),
            R(9,9,"1d3 captured or enslaved craftsmen or merchants, worth 1d4x100gp each"),
            R(10,19,"1,000 electrum pieces"), R(20,20,"500 gold pieces")
        };

        private static readonly RollRange<string>[] SpecialGp =
        {
            R(1,1,"10,000 silver pieces"), R(2,2,"1 metamphora of preserved special components, worth 5d6x60gp"),
            R(3,3,"1d6 fresh monster carcasses with special components, worth 1d10x50gp each"),
            R(4,4,"1d12x12 monster feathers, worth 3d6gp each"),
            R(5,5,"1d8 monster horns and tusks, worth 1d8x50gp each"),
            R(6,6,"1d3 bundles of rare fur pelts, worth 2d4x100gp each"),
            R(7,7,"2d20 pieces of elephant ivory, worth 4d4x10gp each"),
            R(8,8,"1d3 bundles of rare fur pelts, worth 2d4x100gp each"),
            R(9,9,"4d4 amphorae of spices, worth 100gp each"),
            R(10,10,"1d3 crates of fine porcelain, worth 500gp each"),
            R(11,11,"4d10 ingots of precious metals, worth 50gp each"),
            R(12,12,"4d6 rugs of large common fur, worth 1d4x30gp each"),
            R(13,13,"1 captured equerry/lady-in-waiting or enslaved hetaera/odalisque, worth 2d4x200gp"),
            R(14,19,"1,000 gold pieces"), R(20,20,"200 platinum pieces")
        };

        private static readonly RollRange<string>[] SpecialPp =
        {
            R(1,1,"5,000 gold pieces"), R(2,2,"4d6+1 rolls of silk, worth 333gp each"),
            R(3,3,"6d10 rare books, worth 150gp each"), R(4,4,"5d10 capes of common fur, worth 1d6x50gp each"),
            R(5,5,"2d6+1 rugs of large uncommon fur, worth 1d4x250gp each"),
            R(6,6,"2d12 pieces of rare horn or tusk, worth 1d4x150gp each"),
            R(7,7,"2d8 coats of common fur, worth 1d6x150gp each"),
            R(8,8,"4d4 pieces of unicorn or narwhale ivory, worth 2d4x100gp each"),
            R(9,9,"1 captured squire/damsel or enslaved gladiator/concubine, worth 2d4x1,000gp"),
            R(10,20,"1,000 platinum pieces")
        };

        private static readonly RollRange<string>[] SpecialOrnamental =
        {
            R(1,1,"1d12 silver arrows, each worth 5gp"), R(2,2,"1d12 pouches of lungwort or willowbark, worth 5gp each"),
            R(3,3,"1d6 pouches of birthwort, comfrey, goldenrod, or woundwort, worth 10gp each"),
            R(4,4,"1d6 pouches of aloe, belladonna, bitterwood, blessed thistle, or wolfsbane, worth 10gp each"),
            R(5,5,"1d4 pouches of horsetail, worth 15gp each"), R(6,6,"1d2 vials of holy water, worth 25gp each"),
            R(7,12,"1 ornamental")
        };

        private static readonly RollRange<string>[] SpecialGem =
        {
            R(1,1,"1 set of superior thieves' tools, worth 200gp"), R(2,2,"1d4 sets of engraved teeth, worth 2d6x10gp each"),
            R(3,3,"1d3 vials of rare perfume, worth 1d6x25gp each"), R(4,4,"2d10 sticks of rare incense, worth 5d6gp each"),
            R(5,10,"1 gem or 2d6 ornamentals")
        };

        private static readonly RollRange<string>[] SpecialBrilliant =
        {
            R(1,1,"2d20 jade carvings of heroes, monsters, and gods, worth 200gp each"),
            R(2,2,"1d4 sets of masterwork thieves' tools, worth 1,600gp each"),
            R(3,3,"2d4 opal cameo portraits, worth 800gp each"),
            R(4,4,"1d6 amethyst cylinder seals, worth 1,200gp each"),
            R(5,8,"1 brilliant or 4d8 gems")
        };

        private static readonly RollRange<string>[] SpecialTrinket =
        {
            R(1,1,"3d6 bone fetishes and figurines, each worth 2d20gp"),
            R(2,2,"2d6 glass eyes, lenses, or prisms, each worth 1d6x10gp"),
            R(3,3,"1d4 masterwork items, worth 70+5d6gp each"),
            R(4,4,"1d4 silver holy/unholy symbols, worth 2d8x10gp each"),
            R(5,10,"1 trinket")
        };

        private static readonly RollRange<string>[] SpecialJewelry =
        {
            R(1,1,"1d8 trinkets"), R(2,2,"1 cape of large animal fur, worth 2d4x200gp"),
            R(3,3,"1d10 vials of common poison, worth 2d6x25gp each"),
            R(4,4,"1d3 statuettes, worth 1d10x100gp each"),
            R(5,5,"1d2 masterwork items, worth 2d6x100gp each"),
            R(6,10,"1 piece of jewelry")
        };

        private static readonly RollRange<string>[] SpecialRegalia =
        {
            R(1,1,"4d8 pieces of jewelry"), R(2,2,"1d6 capes of rare animal or monster fur, worth 1d6x1000gp each"),
            R(3,3,"1d4 coats of large common or uncommon animal fur, worth (1d6+1)x1000gp each"),
            R(4,4,"2d10 vials of rare poison, worth 4d4x100gp each"),
            R(5,5,"2d10 alabaster and jet game pieces with jeweled eyes, worth 3d6x100gp each"),
            R(6,6,"1 coat of rare animal or monster fur, worth 2d10x1000gp"),
            R(7,7,"1d8 carved ivory figurines, worth 1d4x1000gp each"),
            R(8,8,"1d4 platinum reliquaries with crystal panes, worth 1d8x1000gp each"),
            R(9,12,"1 regalia")
        };
    }
}
