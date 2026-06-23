using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed class DungeonTypeDefinition
    {
        public int Roll { get; set; }
        public string Name { get; set; }
        public string RussianName { get; set; }
        public string IconKey { get; set; }
    }

    internal sealed class HexFeatureDefinition
    {
        public string Kind { get; set; }
        public string Subtype { get; set; }
        public string RussianName { get; set; }
        public string IconKey { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
    }

    internal sealed class DungeonPlacementContext
    {
        public HexCellRecord Cell { get; set; }
        public MapSettlementRecord Settlement { get; set; }
        public int NearestSettlementDistance { get; set; }
        public bool HasRiver { get; set; }
        public bool SeismicRegion { get; set; }
    }

    internal static class DungeonCatalog
    {
        public const int MinDungeonLevel = 1;
        public const int MaxDungeonLevel = 6;

        private static readonly List<DungeonTypeDefinition> dungeonTypes = new List<DungeonTypeDefinition>
        {
            new DungeonTypeDefinition { Roll = 1, Name = "Abandoned mine", RussianName = "Заброшенная шахта", IconKey = "dungeon_abandoned_mine" },
            new DungeonTypeDefinition { Roll = 2, Name = "Barrow mound", RussianName = "Курган", IconKey = "dungeon_barrow_mound" },
            new DungeonTypeDefinition { Roll = 3, Name = "Catacombs", RussianName = "Катакомбы", IconKey = "dungeon_catacombs" },
            new DungeonTypeDefinition { Roll = 4, Name = "Cliff city", RussianName = "Город в скалах", IconKey = "dungeon_cliff_city" },
            new DungeonTypeDefinition { Roll = 5, Name = "Crumbling castle", RussianName = "Рушащийся замок", IconKey = "dungeon_crumbling_castle" },
            new DungeonTypeDefinition { Roll = 6, Name = "Giant burrow", RussianName = "Нора гиганта", IconKey = "dungeon_giant_burrow" },
            new DungeonTypeDefinition { Roll = 7, Name = "Giant insect hive", RussianName = "Улей гигантских насекомых", IconKey = "dungeon_giant_insect_hive" },
            new DungeonTypeDefinition { Roll = 8, Name = "Humanoid warren", RussianName = "Логово гуманоидов", IconKey = "dungeon_humanoid_warren" },
            new DungeonTypeDefinition { Roll = 9, Name = "Lost city", RussianName = "Затерянный город", IconKey = "dungeon_lost_city" },
            new DungeonTypeDefinition { Roll = 10, Name = "Monster lair", RussianName = "Логово монстра", IconKey = "dungeon_monster_lair" },
            new DungeonTypeDefinition { Roll = 11, Name = "Natural caverns", RussianName = "Естественные пещеры", IconKey = "dungeon_natural_caverns" },
            new DungeonTypeDefinition { Roll = 12, Name = "Prison", RussianName = "Тюрьма", IconKey = "dungeon_prison" },
            new DungeonTypeDefinition { Roll = 13, Name = "Ruined villa", RussianName = "Разрушенная вилла", IconKey = "dungeon_ruined_villa" },
            new DungeonTypeDefinition { Roll = 14, Name = "Sewers", RussianName = "Канализация", IconKey = "dungeon_sewers" },
            new DungeonTypeDefinition { Roll = 15, Name = "Temple", RussianName = "Храм", IconKey = "dungeon_temple" },
            new DungeonTypeDefinition { Roll = 16, Name = "Tomb", RussianName = "Гробница", IconKey = "dungeon_tomb" },
            new DungeonTypeDefinition { Roll = 17, Name = "Tower", RussianName = "Башня", IconKey = "dungeon_tower" },
            new DungeonTypeDefinition { Roll = 18, Name = "Treetop settlement", RussianName = "Поселение в кронах", IconKey = "dungeon_treetop_settlement" },
            new DungeonTypeDefinition { Roll = 19, Name = "Underground vault", RussianName = "Подземное хранилище", IconKey = "dungeon_underground_vault" },
            new DungeonTypeDefinition { Roll = 20, Name = "Wizard's dungeon", RussianName = "Подземелье волшебника", IconKey = "dungeon_wizards_dungeon" }
        };

        private static readonly List<HexFeatureDefinition> naturalFeatures = new List<HexFeatureDefinition>
        {
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Volcano", RussianName = "Вулкан", IconKey = "feature_volcano", Severity = "Hazard", Description = "Сейсмоактивная гора с жерлом, лавовыми полями или свежим пеплом." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Hot spring", RussianName = "Горячие источники", IconKey = "feature_hot_spring", Severity = "Wonder", Description = "Горячие минеральные ключи, связанные с глубинным жаром региона." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Geyser field", RussianName = "Поле гейзеров", IconKey = "feature_geyser", Severity = "Hazard", Description = "Периодические выбросы кипящей воды и пара." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Quicksand", RussianName = "Зыбучие пески", IconKey = "feature_quicksand", Severity = "Hazard", Description = "Опасные песчаные ловушки в пустынной местности." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Waterfall", RussianName = "Большой водопад", IconKey = "feature_waterfall", Severity = "Wonder", Description = "Крупный перепад воды на реке, полезный как ориентир и препятствие." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Chasm", RussianName = "Расселина", IconKey = "feature_chasm", Severity = "Hazard", Description = "Глубокая трещина или каньон, разрывающий путь через гекс." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Sinkhole", RussianName = "Карстовый провал", IconKey = "feature_sinkhole", Severity = "Hazard", Description = "Провал грунта, часто ведущий в скрытые полости." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Crystal field", RussianName = "Кристальное поле", IconKey = "feature_crystals", Severity = "Wonder", Description = "Выходы необычных кристаллов среди холмов или гор." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Ancient stones", RussianName = "Древние камни", IconKey = "feature_standing_stones", Severity = "Mystic", Description = "Каменный круг, менгиры или другой древний ориентир." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Sacred spring", RussianName = "Священный родник", IconKey = "feature_sacred_spring", Severity = "Mystic", Description = "Чистый источник с местными легендами и возможной магической репутацией." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Meteor crater", RussianName = "Метеоритный кратер", IconKey = "feature_crater", Severity = "Mystic", Description = "Старое место падения небесного тела." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Tar pit", RussianName = "Смоляные ямы", IconKey = "feature_tar_pit", Severity = "Hazard", Description = "Вязкие битумные ловушки в сухой или кустарниковой местности." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Fey grove", RussianName = "Чарующая роща", IconKey = "feature_fey_grove", Severity = "Mystic", Description = "Глубокая лесная поляна, где местные избегают ночевать." },
            new HexFeatureDefinition { Kind = "Natural", Subtype = "Monster nest", RussianName = "Гнездо чудовищ", IconKey = "feature_monster_nest", Severity = "Hazard", Description = "Следы крупного хищника или выводка монстров." }
        };

        public static IReadOnlyList<DungeonTypeDefinition> DungeonTypes
        {
            get { return dungeonTypes; }
        }

        public static IReadOnlyList<HexFeatureDefinition> NaturalFeatures
        {
            get { return naturalFeatures; }
        }

        public static int ClampDungeonLevel(int level)
        {
            return Math.Max(MinDungeonLevel, Math.Min(MaxDungeonLevel, level));
        }

        public static string NormalizeDungeonType(string dungeonType)
        {
            DungeonTypeDefinition match = dungeonTypes.FirstOrDefault(t => string.Equals(t.Name, dungeonType, StringComparison.OrdinalIgnoreCase));
            return match == null ? "Natural caverns" : match.Name;
        }

        public static string DungeonTypeIconKey(string dungeonType)
        {
            DungeonTypeDefinition match = dungeonTypes.FirstOrDefault(t => string.Equals(t.Name, dungeonType, StringComparison.OrdinalIgnoreCase));
            return match == null ? "dungeon_natural_caverns" : match.IconKey;
        }

        public static string LocalizeDungeonType(string dungeonType, bool russian)
        {
            DungeonTypeDefinition match = dungeonTypes.FirstOrDefault(t => string.Equals(t.Name, dungeonType, StringComparison.OrdinalIgnoreCase));
            if (match == null) return dungeonType ?? "";
            return russian ? match.RussianName : match.Name;
        }

        public static string LocalizeFeatureSubtype(string subtype, bool russian)
        {
            HexFeatureDefinition match = naturalFeatures.FirstOrDefault(f => string.Equals(f.Subtype, subtype, StringComparison.OrdinalIgnoreCase));
            if (match == null) return subtype ?? "";
            return russian ? match.RussianName : match.Subtype;
        }

        public static string LocalizeFeatureDescription(string subtype, bool russian)
        {
            if (russian)
            {
                HexFeatureDefinition match = naturalFeatures.FirstOrDefault(f => string.Equals(f.Subtype, subtype, StringComparison.OrdinalIgnoreCase));
                return match == null ? "" : match.Description;
            }

            switch (subtype)
            {
                case "Volcano": return "A seismic mountain with a crater, lava fields, or fresh ash.";
                case "Hot spring": return "Hot mineral springs tied to deep heat below the region.";
                case "Geyser field": return "Periodic eruptions of boiling water and steam.";
                case "Quicksand": return "Dangerous sand traps in desert terrain.";
                case "Waterfall": return "A major river drop, useful as both landmark and obstacle.";
                case "Chasm": return "A deep crack or canyon cutting through the hex.";
                case "Sinkhole": return "Collapsed ground, often leading to hidden cavities.";
                case "Crystal field": return "Unusual crystal outcrops among hills or mountains.";
                case "Ancient stones": return "A stone circle, menhirs, or another old landmark.";
                case "Sacred spring": return "A clean spring with local legends and possible mystic reputation.";
                case "Meteor crater": return "An old impact site left by a fallen celestial body.";
                case "Tar pit": return "Sticky bitumen traps in dry or scrub terrain.";
                case "Fey grove": return "A deep woodland glade locals avoid after nightfall.";
                case "Monster nest": return "Tracks of a major predator or monster brood.";
                default: return "";
            }
        }

        public static string PickDungeonType(Random random, DungeonPlacementContext context)
        {
            random = random ?? new Random();
            List<DungeonTypeDefinition> candidates = dungeonTypes
                .Where(t => IsDungeonTypeAllowed(t.Name, context))
                .ToList();
            if (candidates.Count == 0) candidates.Add(dungeonTypes.First(t => t.Name == "Natural caverns"));
            return candidates[random.Next(candidates.Count)].Name;
        }

        public static bool IsDungeonTypeAllowed(string dungeonType, DungeonPlacementContext context)
        {
            dungeonType = NormalizeDungeonType(dungeonType);
            HexCellRecord cell = context == null ? null : context.Cell;
            MapSettlementRecord settlement = context == null ? null : context.Settlement;
            bool inSettlement = settlement != null;
            string terrain = cell == null ? "Grasslands" : cell.Terrain;
            string elevation = cell == null ? "Plains" : cell.Elevation;
            bool forest = IsForestTerrain(terrain);
            bool hillsOrMountains = string.Equals(elevation, "Hills", StringComparison.OrdinalIgnoreCase)
                || string.Equals(elevation, "Mountains", StringComparison.OrdinalIgnoreCase);

            if (cell != null && !string.Equals(cell.Water, "None", StringComparison.OrdinalIgnoreCase)) return false;
            if (inSettlement && (dungeonType == "Lost city" || dungeonType == "Treetop settlement")) return false;
            if (dungeonType == "Sewers")
            {
                return inSettlement && settlement.MarketClass >= 1 && settlement.MarketClass <= 4;
            }

            if (dungeonType == "Cliff city") return !inSettlement && hillsOrMountains;
            if (dungeonType == "Treetop settlement") return !inSettlement && forest;
            if (dungeonType == "Lost city") return !inSettlement && (context == null || context.NearestSettlementDistance >= 6);
            if (dungeonType == "Abandoned mine" || dungeonType == "Underground vault" || dungeonType == "Natural caverns")
            {
                return hillsOrMountains || string.Equals(terrain, "DeepForest", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(terrain, "DeepTaiga", StringComparison.OrdinalIgnoreCase);
            }

            if (dungeonType == "Giant insect hive")
            {
                return !string.Equals(terrain, "Tundra", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        public static bool IsFeatureAllowed(string subtype, HexCellRecord cell, bool hasRiver, bool seismicRegion)
        {
            if (cell == null || !string.Equals(cell.Water, "None", StringComparison.OrdinalIgnoreCase)) return false;
            string terrain = cell.Terrain ?? "";
            string elevation = cell.Elevation ?? "";
            bool hillsOrMountains = elevation == "Hills" || elevation == "Mountains";

            if (subtype == "Volcano") return seismicRegion && elevation == "Mountains";
            if (subtype == "Hot spring") return seismicRegion && hillsOrMountains;
            if (subtype == "Geyser field") return seismicRegion && hillsOrMountains;
            if (subtype == "Quicksand") return terrain == "Desert";
            if (subtype == "Waterfall") return hasRiver && hillsOrMountains;
            if (subtype == "Chasm") return hillsOrMountains;
            if (subtype == "Sinkhole") return elevation == "Hills" || terrain == "Grasslands" || terrain == "Scrub";
            if (subtype == "Crystal field") return hillsOrMountains;
            if (subtype == "Ancient stones") return terrain == "Grasslands" || terrain == "Steppe" || elevation == "Hills";
            if (subtype == "Sacred spring") return IsForestTerrain(terrain) || elevation == "Hills";
            if (subtype == "Meteor crater") return true;
            if (subtype == "Tar pit") return terrain == "Desert" || terrain == "Scrub" || terrain == "Steppe";
            if (subtype == "Fey grove") return IsForestTerrain(terrain);
            if (subtype == "Monster nest") return true;
            return true;
        }

        private static bool IsForestTerrain(string terrain)
        {
            return terrain == "Forest" || terrain == "DeepForest" || terrain == "Taiga" || terrain == "DeepTaiga" || terrain == "Rainforest";
        }
    }
}
