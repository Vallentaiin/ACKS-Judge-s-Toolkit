using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    public sealed class WildernessHexModifiers
    {
        public string TerrainType { get; set; }
        public string RussianTerrainType { get; set; }
        public string NavigationThrow { get; set; }
        public string SpeedMultiplier { get; set; }
        public string RoadSpeedMultiplier { get; set; }
        public string RoadDriverSpeedMultiplier { get; set; }
        public string[] EvasionThrowsByPartySize { get; set; }

        public WildernessHexModifiers()
        {
            TerrainType = "";
            RussianTerrainType = "";
            NavigationThrow = "";
            SpeedMultiplier = "";
            RoadSpeedMultiplier = "";
            RoadDriverSpeedMultiplier = "";
            EvasionThrowsByPartySize = new string[0];
        }
    }

    public static class WildernessHexRules
    {
        public static readonly string[] PartySizeLabels = { "6-", "7-14", "15-30", "31-60", "61+" };

        public static WildernessHexModifiers GetModifiers(HexCellRecord cell, bool hasRoad)
        {
            if (cell == null || IsWaterCell(cell)) return null;

            string terrain = MapDataNormalizer.TerrainKey(cell.Terrain);
            string elevation = MapDataNormalizer.ElevationKey(cell.Elevation);
            bool forested = IsForestLikeTerrain(terrain);

            if (string.Equals(elevation, "Mountains", StringComparison.OrdinalIgnoreCase))
            {
                return Create(
                    forested ? "Mountains (forested)" : "Mountains (rocky)",
                    forested ? "Горы (лесистые)" : "Горы (каменистые)",
                    "6+",
                    "x1/2",
                    forested ? Evasion(5) : Evasion(12),
                    hasRoad);
            }

            if (string.Equals(elevation, "Hills", StringComparison.OrdinalIgnoreCase))
            {
                return Create(
                    forested ? "Hills (forested)" : "Hills (rocky)",
                    forested ? "Холмы (лесистые)" : "Холмы (каменистые)",
                    "8+",
                    "x2/3",
                    forested ? Evasion(5) : Evasion(12),
                    hasRoad);
            }

            switch (terrain)
            {
                case "Desert":
                    return Create("Desert (sandy)", "Пустыня (песчаная)", "6+", "x2/3", Evasion(12), hasRoad);
                case "Forest":
                case "DeepForest":
                    return Create("Forest (deciduous)", "Лес (лиственный)", "8+", "x2/3", Evasion(2), hasRoad);
                case "Taiga":
                case "DeepTaiga":
                    return Create("Forest (taiga)", "Лес (тайга)", "8+", "x2/3", Evasion(5), hasRoad);
                case "Rainforest":
                    return Create("Jungle (any)", "Джунгли", "14+", "x1/2", Evasion(2), hasRoad);
                case "Steppe":
                    return Create("Grassland (steppe)", "Степь", "6+", "x1", Evasion(16), hasRoad);
                case "Savanna":
                    return Create("Grassland (other)", "Саванна", "6+", "x1", Evasion(9), hasRoad);
                case "Scrub":
                    return Create("Scrubland (sparse)", "Редкий кустарник", "6+", "x1", Evasion(12), hasRoad);
                case "Marsh":
                    return Create("Swamp (marshy)", "Болото (топкое)", "10+", "x1/2", Evasion(9), hasRoad);
                case "Tundra":
                    return Create("Barrens (any)", "Пустошь", "6+", "x2/3", Evasion(12), hasRoad);
                default:
                    return Create("Grassland (other)", "Равнина", "6+", "x1", Evasion(9), hasRoad);
            }
        }

        public static List<string> BuildDisplayLines(HexCellRecord cell, bool hasRoad, bool english)
        {
            return BuildDisplayLines(cell, hasRoad, false, null, english);
        }

        public static List<string> BuildDisplayLines(
            HexCellRecord cell,
            bool hasRoad,
            bool hasRiverOrLake,
            string domainClassification,
            bool english)
        {
            WildernessHexModifiers modifiers = GetModifiers(cell, hasRoad);
            if (modifiers == null) return new List<string>();

            List<string> lines = new List<string>
            {
                english
                    ? "Wilderness terrain: " + modifiers.TerrainType
                    : "Тип местности: " + modifiers.RussianTerrainType,
                (english ? "Navigation: " : "Навигация: ") + modifiers.NavigationThrow,
                (english ? "Speed: " : "Скорость: ") + SpeedText(modifiers, english),
                EvasionText(modifiers, english)
            };
            lines.AddRange(BuildForagingDisplayLines(modifiers, hasRiverOrLake, domainClassification, english));
            return lines;
        }

        private static WildernessHexModifiers Create(
            string terrainType,
            string russianTerrainType,
            string navigationThrow,
            string speedMultiplier,
            string[] evasionThrows,
            bool hasRoad)
        {
            return new WildernessHexModifiers
            {
                TerrainType = terrainType,
                RussianTerrainType = russianTerrainType,
                NavigationThrow = navigationThrow,
                SpeedMultiplier = speedMultiplier,
                RoadSpeedMultiplier = hasRoad ? "x3/2" : "",
                RoadDriverSpeedMultiplier = hasRoad ? "x2" : "",
                EvasionThrowsByPartySize = evasionThrows
            };
        }

        private static string SpeedText(WildernessHexModifiers modifiers, bool english)
        {
            if (modifiers == null) return "";
            if (string.IsNullOrWhiteSpace(modifiers.RoadSpeedMultiplier)) return modifiers.SpeedMultiplier;

            return english
                ? modifiers.SpeedMultiplier + "; road " + modifiers.RoadSpeedMultiplier + " (drivers " + modifiers.RoadDriverSpeedMultiplier + ")"
                : modifiers.SpeedMultiplier + "; дорога " + modifiers.RoadSpeedMultiplier + " (возницы " + modifiers.RoadDriverSpeedMultiplier + ")";
        }

        private static List<string> BuildForagingDisplayLines(
            WildernessHexModifiers modifiers,
            bool hasRiverOrLake,
            string domainClassification,
            bool english)
        {
            bool forest = IsForestWildernessTerrain(modifiers.TerrainType);
            bool barrensOrDesert = IsBarrensOrDesertTerrain(modifiers.TerrainType);
            string survivalBonus = english ? " (+4 with Survival proficiency)" : " (+4 при навыке Выживание)";
            string firewood = (forest ? "3+" : "14+") + survivalBonus;
            string water = hasRiverOrLake
                ? (english ? "automatic (river/lake)" : "авто (река/озеро)")
                : (barrensOrDesert ? "18+" : "14+") + survivalBonus;
            string food = (barrensOrDesert ? "22+" : "18+") + survivalBonus;

            List<string> lines = new List<string>
            {
                english ? "Foraging:" : "Добыча ресурсов:",
                (english ? "Wood: " : "Дрова: ") + firewood,
                (english ? "Water: " : "Вода: ") + water,
                (english ? "Food: " : "Еда: ") + food,
                (english ? "Hunt food: " : "Охота: ") + HuntingFoodText(domainClassification, english)
            };
            string noStealPenalty = FoodNoStealPenaltyText(domainClassification, english);
            if (!string.IsNullOrWhiteSpace(noStealPenalty))
            {
                lines.Add((english ? "Food without stealing: " : "Еда без воровства: ") + noStealPenalty);
            }

            return lines;
        }

        private static string HuntingFoodText(string domainClassification, bool english)
        {
            string territory = NormalizeTerritory(domainClassification);
            string survival = english
                ? "+4 with Survival proficiency; triggers a wilderness encounter check"
                : "+4 при навыке Выживание; вызывает проверку блуждающей встречи";
            if (territory == "Civilized") return english ? "18+ in civilized territory; " + survival : "18+ в цивилизованных землях; " + survival;
            if (territory == "Borderlands") return english ? "14+ in borderlands; " + survival : "14+ в пограничье; " + survival;
            if (territory == "Outlands") return english ? "12+ in outlands; " + survival : "12+ в неосвоенных землях; " + survival;
            if (territory == "Unsettled") return english ? "10+ in unsettled wilderness; " + survival : "10+ в диких землях; " + survival;
            return english ? "10+ in unsettled wilderness; " + survival : "10+ в диких землях; " + survival;
        }

        private static string FoodNoStealPenaltyText(string domainClassification, bool english)
        {
            string territory = NormalizeTerritory(domainClassification);
            if (territory == "Civilized") return english ? "-4 in civilized territory" : "-4 в цивилизованных землях";
            if (territory == "Borderlands") return english ? "-2 in borderlands" : "-2 в пограничье";
            return "";
        }

        private static string EvasionText(WildernessHexModifiers modifiers, bool english)
        {
            List<string> pairs = new List<string>();
            for (int i = 0; i < PartySizeLabels.Length && i < modifiers.EvasionThrowsByPartySize.Length; i++)
            {
                string size = PartySizeLabels[i] == "6-" && !english ? "до 6" : PartySizeLabels[i];
                pairs.Add(size + " -> " + modifiers.EvasionThrowsByPartySize[i]);
            }

            return english
                ? "Evasion throw (party size -> d20 target after modifiers): " + string.Join("; ", pairs)
                : "Уклонение (размер отряда -> цель на d20 после модификаторов): " + string.Join("; ", pairs);
        }

        private static string NormalizeTerritory(string domainClassification)
        {
            if (string.IsNullOrWhiteSpace(domainClassification)) return "Unsettled";
            if (domainClassification.IndexOf("Civilized", StringComparison.OrdinalIgnoreCase) >= 0) return "Civilized";
            if (domainClassification.IndexOf("Border", StringComparison.OrdinalIgnoreCase) >= 0) return "Borderlands";
            if (domainClassification.IndexOf("Outland", StringComparison.OrdinalIgnoreCase) >= 0) return "Outlands";
            if (domainClassification.IndexOf("Unsettled", StringComparison.OrdinalIgnoreCase) >= 0
                || domainClassification.IndexOf("Wild", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Unsettled";
            }

            return "Unsettled";
        }

        private static bool IsForestWildernessTerrain(string terrainType)
        {
            return !string.IsNullOrWhiteSpace(terrainType)
                && terrainType.StartsWith("Forest", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBarrensOrDesertTerrain(string terrainType)
        {
            return !string.IsNullOrWhiteSpace(terrainType)
                && (terrainType.StartsWith("Barrens", StringComparison.OrdinalIgnoreCase)
                    || terrainType.StartsWith("Desert", StringComparison.OrdinalIgnoreCase));
        }

        private static string[] Evasion(int firstColumnThrow)
        {
            return Enumerable.Range(0, PartySizeLabels.Length)
                .Select(i => (firstColumnThrow + i * 2).ToString() + "+")
                .ToArray();
        }

        private static bool IsWaterCell(HexCellRecord cell)
        {
            return cell != null
                && !string.IsNullOrWhiteSpace(cell.Water)
                && !string.Equals(cell.Water, "None", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForestLikeTerrain(string terrain)
        {
            return string.Equals(terrain, "Forest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(terrain, "DeepForest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(terrain, "Taiga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(terrain, "DeepTaiga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(terrain, "Rainforest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
