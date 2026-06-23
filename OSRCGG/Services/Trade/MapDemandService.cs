using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    // Чистая логика торгового спроса: UI передаёт состояние гекса, а сервис возвращает готовые модификаторы.
    internal static class MapDemandService
    {
        public static double[] BuildCellAdjustedDemands(
            double[] baseDemands,
            HexCellRecord cell,
            IEnumerable<HexCellRecord> neighborCells,
            bool hasRiver)
        {
            double[] values = NormalizeDemandArray(baseDemands);
            if (cell == null) return values;

            AddDemandAdjustment(values, GetTerrainAdjustment(cell.Terrain));
            AddDemandAdjustment(values, GetElevationAdjustment(cell.Elevation));
            foreach (string water in GetWaterInfluences(neighborCells, hasRiver))
            {
                AddDemandAdjustment(values, GetWaterAdjustment(water));
            }

            return values;
        }

        public static IEnumerable<string> GetWaterInfluences(IEnumerable<HexCellRecord> neighborCells, bool hasRiver)
        {
            bool hasSea = false;
            bool hasLake = false;

            // Влияние воды берётся не только с текущего гекса: береговые рынки зависят от соседних морей/озёр.
            foreach (HexCellRecord neighbor in neighborCells ?? Enumerable.Empty<HexCellRecord>())
            {
                if (neighbor == null) continue;
                if (neighbor.Water == "Ocean" || neighbor.Water == "Sea") hasSea = true;
                if (neighbor.Water == "Lake") hasLake = true;
            }

            if (hasRiver) yield return "River";
            if (hasSea) yield return "Sea";
            if (hasLake) yield return "Lake";
        }

        public static double[] GetWaterAdjustment(string water)
        {
            if (water == "Sea")
            {
                return new double[29] { 0, -0.5, -0.5, -0.5, 0, -0.5, -0.5, -1, 0, -0.5, 0, 0, 0, 0, -0.5, -0.5, 0, -0.5, -0.5, 0, 0, 0, 0, -0.5, 0, 0, -0.5, 0, 0 };
            }
            if (water == "Lake")
            {
                return new double[29] { 0, -0.5, -0.5, -0.5, 0, -0.5, 0, -0.5, 0, -0.5, 0, 0, 0, 0, -0.5, 0, 0, -0.5, -0.5, 0, 0, 0, 0, -0.5, 0, 0, -0.5, 0, 0 };
            }
            if (water == "River")
            {
                return new double[29] { -1, -0.5, -0.5, -0.5, 0, -0.5, -0.5, -0.5, 0, -0.5, 0, 0, 0, -0.5, -0.5, 0, -1, -0.5, -0.5, 0, 0, 0, 0, -0.5, 0, 0, -0.5, 0, 0 };
            }
            return new double[AcksRules.DemandCount];
        }

        public static double[] GetTerrainAdjustment(string terrain)
        {
            switch (terrain)
            {
                case "Rainforest":
                    return new double[29] { 0, +1, +1, +1, -1, +1, -0.5, 0, +1, +1, -1, -0.5, 0, +1, -0.5, -1, -0.5, +1, +1, -0.5, -1, 0, -1, +1, -0.5, -1, +1, -0.5, -0.5 };
                case "Savanna":
                    return new double[29] { +0.5, 0, +1, +1, 0, +1, 0, +0.5, 0, +1, 0, 0, 0, +0.5, 0, -0.5, 0, +1, +1, -0.5, -1, 0, -0.5, +1, 0, -0.5, +1, 0, 0 };
                case "Desert":
                    return new double[29] { +1, -0.5, +1, -0.5, +1, +0.5, +0.5, +1, +1, -0.5, +1, 0, 0, +1, -0.5, -0.5, +0.5, +1, +1, -0.5, -0.5, 0, 0, -0.5, -0.5, +0.5, +1, -0.5, -0.5 };
                case "Steppe":
                    return new double[29] { +0.5, -0.5, +1, -0.5, +0.5, +1, +0.5, +0.5, -1, 0, +0.5, 0, 0, +0.5, 0, 0, 0, +1, +1, 0, -0.5, 0, 0, -0.5, 0, +0.5, +1, 0, 0 };
                case "Forest":
                case "DeepForest":
                    return new double[29] { -0.5, 0, -0.5, 0, -1, -0.5, 0, -0.5, -1, 0, -1, +0.5, 0, -1, 0, +0.5, -0.5, -0.5, -0.5, -0.5, +0.5, -0.5, +1, 0, 0, -0.5, -0.5, 0, 0 };
                case "Taiga":
                case "DeepTaiga":
                    return new double[29] { +0.5, 0, +1, 0, -1, +0.5, -1, 0, 0, +1, -1, 0, -0.5, -1, +1, +1, +1, +1, +1, 0, +0.5, -0.5, +1, 0, 0, +1, +1, -0.5, -0.5 };
                case "Tundra":
                    return new double[29] { +1, 0, +1, 0, +1, +1, 0, 0, -0.5, +1, +1, -0.5, -0.5, +0.5, +1, +1, +1, +1, +1, 0, 0, -1, +1, 0, 0, +1, +1, 0, 0 };
                case "Scrub":
                    return new double[29] { -0.5, 0, -0.5, -0.5, 0, -1, -1, 0, 0, -0.5, 0, 0, 0, 0, 0, 0, -0.5, -0.5, -0.5, 0, -0.5, 0, 0, -0.5, 0, -0.5, 0, -0.5, -0.5 };
                case "Grasslands":
                    return new double[29] { -1, 0, 0, 0, +0.5, +1, +1, +0.5, -0.5, 0, +0.5, 0, -0.5, -0.5, 0, 0, -0.5, -0.5, -0.5, 0, +0.5, -0.5, +0.5, 0, 0, +1, 0, 0, 0 };
                default:
                    return new double[AcksRules.DemandCount];
            }
        }

        public static double[] GetElevationAdjustment(string elevation)
        {
            switch (elevation)
            {
                case "Hills":
                    return new double[29] { 0, 0, -0.5, -0.5, 0, -0.5, -0.5, +0.5, 0, -0.5, 0, -0.5, 0, -0.5, -0.5, -0.5, -0.5, -0.5, -0.5, 0, +0.5, -0.5, +0.5, -0.5, -0.5, -0.5, -0.5, -0.5, -0.5 };
                case "Mountains":
                    return new double[29] { +0.5, 0, -0.5, 0, +0.5, -0.5, 0, +1, 0, 0, +0.5, -0.5, 0, 0, 0, -0.5, 0, 0, 0, -1, +1, -0.5, +1, 0, -0.5, 0, 0, -0.5, -0.5 };
                default:
                    return new double[29] { -0.5, -0.5, -0.5, 0, -0.5, +0.5, +0.5, 0, -0.5, 0, -0.5, +0.5, -0.5, -0.5, 0, 0, -0.5, -0.5, -0.5, 0, 0, 0, -0.5, 0, 0, 0, 0, 0, 0 };
            }
        }

        public static double[] NormalizeDemandArray(double[] source)
        {
            double[] values = new double[AcksRules.DemandCount];
            if (source == null) return values;
            // Копируем только допустимый размер: старые файлы могли содержать меньше или больше колонок.
            System.Array.Copy(source, values, System.Math.Min(source.Length, values.Length));
            return values;
        }

        public static void AddDemandAdjustment(double[] target, double[] adjustment)
        {
            if (target == null || adjustment == null) return;

            for (int i = 0; i < target.Length && i < adjustment.Length; i++)
            {
                target[i] += adjustment[i];
            }
        }
    }
}
