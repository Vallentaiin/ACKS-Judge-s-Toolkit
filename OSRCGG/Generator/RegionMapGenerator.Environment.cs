using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        // Природные слои карты: местность, высоты, крупная вода, реки, озёра и имена природных объектов.
        private sealed class CoastFeature
        {
            public int Side { get; set; }
            public int Center { get; set; }
            public int Radius { get; set; }
            public double DepthOffset { get; set; }
        }

        private int PresetLargeWaterPercent(string layout)
        {
            if (string.Equals(layout, "NoLargeWater", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(layout, "Archipelago", StringComparison.OrdinalIgnoreCase)) return 48;
            if (string.Equals(layout, "Coast", StringComparison.OrdinalIgnoreCase)) return 36;
            if (string.Equals(layout, "TwoContinents", StringComparison.OrdinalIgnoreCase)) return 30;
            if (string.Equals(layout, "InlandSea", StringComparison.OrdinalIgnoreCase)) return 18;
            if (string.Equals(layout, "Gulf", StringComparison.OrdinalIgnoreCase)) return 34;
            return 24;
        }

        private void GenerateTerrain(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Местность строится через центры зон: так карта получает крупные природные области,
            // а параметр хаотичности разбивает их на более неровные границы.
            List<ZoneCenter> zones = new List<ZoneCenter>();
            string[] terrainPool = TerrainPool(options.ClimateBelt);
            for (int i = 0; i < options.TerrainZoneCount; i++)
            {
                zones.Add(new ZoneCenter
                {
                    Q = random.Next(map.Width),
                    R = random.Next(map.Height),
                    Value = terrainPool[random.Next(terrainPool.Length)],
                    Weight = 0.75 + random.NextDouble() * 1.4
                });
            }

            foreach (HexCellRecord cell in map.Cells)
            {
                ZoneCenter best = null;
                double bestScore = double.MaxValue;
                foreach (ZoneCenter zone in zones)
                {
                    double score = HexDistance(cell.Q, cell.R, zone.Q, zone.R) / zone.Weight
                        + random.NextDouble() * options.TerrainChaosPercent / 70.0;
                    if (best == null || score < bestScore)
                    {
                        best = zone;
                        bestScore = score;
                    }
                }

                cell.Terrain = best.Value;
            }

            EnsureDeepForestPresence(map, options, random);
            EnsureMarshPresence(map, options, random);
        }

        private void EnsureDeepForestPresence(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || random == null) return;
            if (map.Cells.Any(c => c.Water == "None" && c.Terrain == "DeepForest")) return;

            List<HexCellRecord> candidates = map.Cells
                .Where(c => c.Water == "None")
                .OrderByDescending(c => DeepForestSeedScore(c, options, random))
                .Take(12)
                .ToList();
            HexCellRecord center = candidates.FirstOrDefault();
            if (center == null) return;

            int radius = Math.Max(1, Math.Min(3, Math.Min(map.Width, map.Height) / 18));
            foreach (HexCellRecord cell in map.Cells.Where(c => c.Water == "None" && HexDistance(c.Q, c.R, center.Q, center.R) <= radius))
            {
                int distance = HexDistance(cell.Q, cell.R, center.Q, center.R);
                int chance = distance == 0 ? 100 : distance == 1 ? 82 : 58;
                if (random.Next(100) < chance) cell.Terrain = "DeepForest";
            }
        }

        private double DeepForestSeedScore(HexCellRecord cell, RegionGenerationOptions options, Random random)
        {
            double score = random.NextDouble() * 3.0;
            if (cell.Terrain == "Forest" || cell.Terrain == "Rainforest") score += 8.0;
            if (cell.Terrain == "Taiga") score += 5.0;
            if (cell.Terrain == "Grasslands" || cell.Terrain == "Scrub") score += 2.0;
            if (cell.Elevation == "Plains" || cell.Elevation == "Hills") score += 1.5;
            if (options != null && string.Equals(options.ClimateBelt, "Arid", StringComparison.OrdinalIgnoreCase)) score -= 3.0;
            return score;
        }

        private void EnsureMarshPresence(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || random == null) return;
            if (map.Cells.Any(c => c.Water == "None" && c.Terrain == "Marsh")) return;

            bool arid = options != null && string.Equals(options.ClimateBelt, "Arid", StringComparison.OrdinalIgnoreCase);
            bool civilized = options != null && string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase);
            int area = Math.Max(1, map.Width * map.Height);
            if (arid && random.Next(100) >= (civilized ? 18 : 10)) return;

            List<HexCellRecord> candidates = map.Cells
                .Where(c => c.Water == "None" && c.Elevation != "Mountains")
                .OrderByDescending(c => MarshSeedScore(map, c, options, random))
                .Take(10)
                .ToList();
            HexCellRecord center = candidates.FirstOrDefault();
            if (center == null) return;

            // Болота и глубокие леса остаются неудобьями: домены и поселения их обходят,
            // поэтому даже цивилизованная карта получает естественные незаселённые пятна.
            int radius = Math.Max(1, Math.Min(civilized ? 3 : 2, Math.Min(map.Width, map.Height) / 16));
            foreach (HexCellRecord cell in map.Cells.Where(c => c.Water == "None" && HexDistance(c.Q, c.R, center.Q, center.R) <= radius))
            {
                if (cell.Elevation == "Mountains") continue;
                int distance = HexDistance(cell.Q, cell.R, center.Q, center.R);
                int chance = distance == 0 ? 100 : distance == 1 ? 72 : 42;
                if (area >= 3600 && civilized) chance += 10;
                if (random.Next(100) < Math.Min(92, chance)) cell.Terrain = "Marsh";
            }
        }

        private double MarshSeedScore(HexMapRecord map, HexCellRecord cell, RegionGenerationOptions options, Random random)
        {
            double score = random.NextDouble() * 3.0;
            if (cell.Elevation == "Plains") score += 4.0;
            if (cell.Elevation == "Hills") score += 1.0;
            if (cell.Terrain == "Grasslands" || cell.Terrain == "Forest" || cell.Terrain == "Taiga") score += 2.0;
            if (cell.Terrain == "Rainforest") score += 4.0;
            int nearbyWater = map == null ? 0 : GetNeighbors(map, cell).Count(IsWater);
            if (nearbyWater > 0) score += Math.Min(5, nearbyWater) * 1.4;
            if (options != null && string.Equals(options.ClimateBelt, "Tropical", StringComparison.OrdinalIgnoreCase)) score += 2.0;
            if (options != null && string.Equals(options.ClimateBelt, "Arid", StringComparison.OrdinalIgnoreCase)) score -= 3.0;
            return score;
        }

        private void NormalizeWaterSurfaces(HexMapRecord map)
        {
            if (map == null || map.Cells == null) return;

            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell == null) continue;
                if (IsWater(cell))
                {
                    // Водный гекс сам является местностью; под ним не должно оставаться леса,
                    // тайги или гор, которые затем видны на отдельных слоях карты.
                    cell.Terrain = cell.Water;
                    cell.Elevation = "Water";
                }
                else
                {
                    if (cell.Terrain == "Ocean" || cell.Terrain == "Sea" || cell.Terrain == "Lake") cell.Terrain = "Grasslands";
                    if (cell.Elevation == "Water") cell.Elevation = "Plains";
                }
            }
        }

        private bool IsWater(HexCellRecord cell)
        {
            return cell != null && (cell.Water == "Ocean" || cell.Water == "Sea" || cell.Water == "Lake");
        }

        public void GenerateFeatureNamesForMap(HexMapRecord map, string cultureKey, bool russianOutput, Random random)
        {
            if (map == null || random == null) return;
            if (map.Cells == null) map.Cells = new List<HexCellRecord>();
            if (map.Rivers == null) map.Rivers = new List<MapEdgeRecord>();

            cellIndex = map.Cells.ToDictionary(c => CellKey(c.Q, c.R));
            useRussianNames = russianOutput;
            usedFeatureNames.Clear();

            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell != null) cell.WaterFeatureName = "";
            }

            foreach (MapEdgeRecord river in map.Rivers)
            {
                if (river != null) river.FeatureName = "";
            }

            GenerateWaterFeatureNames(map, cultureKey, random);
            GenerateRiverFeatureNames(map, cultureKey, random);
        }

        private void GenerateWaterFeatureNames(HexMapRecord map, string cultureKey, Random random)
        {
            HashSet<string> visited = new HashSet<string>();
            foreach (HexCellRecord start in map.Cells.Where(IsWater))
            {
                string startKey = CellKey(start.Q, start.R);
                if (!visited.Add(startKey)) continue;

                List<HexCellRecord> cluster = new List<HexCellRecord>();
                Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
                queue.Enqueue(start);
                cluster.Add(start);

                while (queue.Count > 0)
                {
                    HexCellRecord cell = queue.Dequeue();
                    foreach (HexCellRecord neighbor in GetNeighbors(map, cell))
                    {
                        if (!IsSameWaterFeature(start, neighbor)) continue;
                        string key = CellKey(neighbor.Q, neighbor.R);
                        if (!visited.Add(key)) continue;
                        queue.Enqueue(neighbor);
                        cluster.Add(neighbor);
                    }
                }

                string kind = WaterFeatureKind(cluster);
                int threshold = kind == "Lake" ? 3 : kind == "Sea" ? 6 : 10;
                if (cluster.Count < threshold) continue;

                string name = GenerateUniqueFeatureName(random, cultureKey, kind);
                foreach (HexCellRecord cell in cluster)
                {
                    cell.WaterFeatureName = name;
                }
            }
        }

        private bool IsSameWaterFeature(HexCellRecord start, HexCellRecord neighbor)
        {
            if (!IsWater(start) || !IsWater(neighbor)) return false;
            if (start.Water == "Lake" || neighbor.Water == "Lake") return start.Water == neighbor.Water;
            return (start.Water == "Ocean" || start.Water == "Sea")
                && (neighbor.Water == "Ocean" || neighbor.Water == "Sea");
        }

        private string WaterFeatureKind(List<HexCellRecord> cluster)
        {
            if (cluster.Any(c => c.Water == "Ocean")) return "Ocean";
            if (cluster.Any(c => c.Water == "Sea")) return "Sea";
            return "Lake";
        }

        private void GenerateRiverFeatureNames(HexMapRecord map, string cultureKey, Random random)
        {
            Dictionary<string, List<MapEdgeRecord>> byEndpoint = new Dictionary<string, List<MapEdgeRecord>>();
            foreach (MapEdgeRecord river in map.Rivers.Where(e => e != null))
            {
                AddEndpoint(byEndpoint, CellKey(river.AQ, river.AR), river);
                AddEndpoint(byEndpoint, CellKey(river.BQ, river.BR), river);
            }

            HashSet<string> visited = new HashSet<string>();
            foreach (MapEdgeRecord start in map.Rivers.Where(e => e != null))
            {
                string startKey = start.NormalizedKey();
                if (!visited.Add(startKey)) continue;

                List<MapEdgeRecord> river = new List<MapEdgeRecord>();
                Queue<MapEdgeRecord> queue = new Queue<MapEdgeRecord>();
                queue.Enqueue(start);
                river.Add(start);

                while (queue.Count > 0)
                {
                    MapEdgeRecord edge = queue.Dequeue();
                    foreach (string endpoint in new[] { CellKey(edge.AQ, edge.AR), CellKey(edge.BQ, edge.BR) })
                    {
                        List<MapEdgeRecord> connected;
                        if (!byEndpoint.TryGetValue(endpoint, out connected)) continue;
                        foreach (MapEdgeRecord next in connected)
                        {
                            string key = next.NormalizedKey();
                            if (!visited.Add(key)) continue;
                            queue.Enqueue(next);
                            river.Add(next);
                        }
                    }
                }

                if (river.Count < 6) continue;
                string name = GenerateUniqueFeatureName(random, cultureKey, "River");
                foreach (MapEdgeRecord edge in river)
                {
                    edge.FeatureName = name;
                }
            }
        }

        private void AddEndpoint(Dictionary<string, List<MapEdgeRecord>> endpoints, string key, MapEdgeRecord edge)
        {
            List<MapEdgeRecord> list;
            if (!endpoints.TryGetValue(key, out list))
            {
                list = new List<MapEdgeRecord>();
                endpoints[key] = list;
            }

            list.Add(edge);
        }

        private string[] TerrainPool(string climate)
        {
            if (string.Equals(climate, "Tropical", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Rainforest", "Rainforest", "DeepForest", "Savanna", "Savanna", "Scrub", "Grasslands", "Marsh", "Desert" };
            }

            if (string.Equals(climate, "Arid", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Desert", "Desert", "Steppe", "Steppe", "Scrub", "Savanna", "Grasslands", "DeepForest" };
            }

            if (string.Equals(climate, "Cold", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Taiga", "Taiga", "DeepTaiga", "DeepForest", "Tundra", "Forest", "Steppe", "Marsh" };
            }

            if (string.Equals(climate, "Mixed", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "Rainforest", "Savanna", "Desert", "Steppe", "Scrub", "Grasslands", "Forest", "DeepForest", "Taiga", "Tundra", "Marsh" };
            }

            return new[] { "Grasslands", "Grasslands", "Forest", "Forest", "DeepForest", "Scrub", "Steppe", "Marsh", "Taiga" };
        }

        private void GenerateElevation(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Высотность строится не отдельными случайными точками, а хребтами:
            // горы образуют протяжённые цепи, а холмы чаще возникают как предгорья.
            foreach (HexCellRecord cell in map.Cells)
            {
                cell.Elevation = "Plains";
            }

            int area = map.Width * map.Height;
            bool archipelago = string.Equals(options.WaterLayout, "Archipelago", StringComparison.OrdinalIgnoreCase);
            bool seismic = string.Equals(options.Seismicity, "Seismic", StringComparison.OrdinalIgnoreCase);
            int ridgeDivisor = seismic ? (archipelago ? 2800 : 2400) : (archipelago ? 3100 : 2600);
            int ridgeCount = Math.Max(1, area * Math.Max(1, options.MountainsPercent) / ridgeDivisor);
            int maxRidgeLength = Math.Max(5, Math.Min(map.Width, map.Height) / (seismic ? (archipelago ? 5 : 3) : (archipelago ? 6 : 4)));

            for (int ridge = 0; ridge < ridgeCount; ridge++)
            {
                HexCellRecord current = map.Cells[random.Next(map.Cells.Count)];
                int length = random.Next(4, maxRidgeLength + 1);
                int drift = random.Next(6);

                for (int step = 0; step < length && current != null; step++)
                {
                    MarkElevationAround(map, current, "Mountains", 0, 100, random);
                    MarkElevationAround(map, current, "Mountains", 1, seismic ? (archipelago ? 22 : 34) : (archipelago ? 18 : 28), random);
                    MarkElevationAround(map, current, "Hills", 1, seismic ? 66 : 60, random);
                    MarkElevationAround(map, current, "Hills", 2, seismic ? (archipelago ? 24 : 38) : (archipelago ? 20 : 34), random);

                    List<HexCellRecord> neighbors = GetNeighbors(map, current)
                        .OrderBy(n => DirectionalRidgeScore(current, n, drift, random))
                        .ToList();
                    current = neighbors.FirstOrDefault();
                }
            }

            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell.Elevation != "Plains") continue;
                int mountainDistance = NearestElevationDistance(map, cell, "Mountains", 3);
                int hillChance = mountainDistance <= 3 ? options.HillsPercent + 10 : options.HillsPercent / 4;
                if (random.Next(100) < hillChance)
                {
                    cell.Elevation = "Hills";
                }
            }
        }

        private void MarkElevationAround(HexMapRecord map, HexCellRecord center, string elevation, int radius, int chance, Random random)
        {
            foreach (HexCellRecord cell in map.Cells.Where(c => HexDistance(c.Q, c.R, center.Q, center.R) <= radius))
            {
                if (random.Next(100) >= chance) continue;
                if (elevation == "Hills" && cell.Elevation == "Mountains") continue;
                cell.Elevation = elevation;
            }
        }

        private double DirectionalRidgeScore(HexCellRecord current, HexCellRecord next, int drift, Random random)
        {
            int dq = next.Q - current.Q;
            int dr = next.R - current.R;
            int direction = Math.Abs(dq) > Math.Abs(dr) ? (dq > 0 ? 0 : 3) : (dr > 0 ? 2 : 5);
            int turn = Math.Abs(direction - drift);
            turn = Math.Min(turn, 6 - turn);
            return turn + random.NextDouble() * 1.6;
        }

        private int NearestElevationDistance(HexMapRecord map, HexCellRecord origin, string elevation, int maxDistance)
        {
            int best = maxDistance + 1;
            foreach (HexCellRecord cell in map.Cells.Where(c => c.Elevation == elevation))
            {
                int distance = HexDistance(origin.Q, origin.R, cell.Q, cell.R);
                if (distance < best) best = distance;
                if (best <= 1) break;
            }

            return best;
        }

        private void RefineElevationAfterWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || options == null) return;

            // После большой воды смягчаем высоты на маленьких островах и побережьях:
            // так архипелаг не выглядит как случайная россыпь горных гексов в море.
            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell.Water != "None") continue;
                int landNeighbors = GetNeighbors(map, cell).Count(n => n.Water == "None");
                int waterNeighbors = GetNeighbors(map, cell).Count(n => n.Water == "Sea" || n.Water == "Ocean");
                if (landNeighbors <= 2 && cell.Elevation == "Mountains" && random.Next(100) < 70)
                {
                    cell.Elevation = "Hills";
                }
                else if (waterNeighbors >= 3 && cell.Elevation == "Mountains" && random.Next(100) < 45)
                {
                    cell.Elevation = "Hills";
                }
                else if (waterNeighbors >= 4 && cell.Elevation == "Hills" && random.Next(100) < 35)
                {
                    cell.Elevation = "Plains";
                }
            }
        }

        private void GenerateLargeWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Большая вода задает форму суши до рек и озер. Озера создаются отдельным проходом,
            // иначе один процент воды одновременно порождал и побережье, и чрезмерные озерные пятна.
            foreach (HexCellRecord cell in map.Cells)
            {
                cell.Water = "None";
            }

            string layout = options.WaterLayout ?? "Coast";
            if (string.Equals(layout, "NoLargeWater", StringComparison.OrdinalIgnoreCase) || options.WaterPercent <= 0) return;

            if (string.Equals(layout, "Archipelago", StringComparison.OrdinalIgnoreCase))
            {
                GenerateArchipelagoWater(map, options, random);
                return;
            }

            if (string.Equals(layout, "Continent", StringComparison.OrdinalIgnoreCase))
            {
                GenerateContinentWater(map, options, random);
                return;
            }

            if (string.Equals(layout, "TwoContinents", StringComparison.OrdinalIgnoreCase))
            {
                GenerateTwoContinentsWater(map, options, random);
                return;
            }

            if (string.Equals(layout, "InlandSea", StringComparison.OrdinalIgnoreCase))
            {
                GenerateInlandSea(map, options, random);
                return;
            }

            if (string.Equals(layout, "Gulf", StringComparison.OrdinalIgnoreCase))
            {
                GenerateGulfWater(map, options, random);
                return;
            }

            GenerateCoastWater(map, options, random);
        }

        private void GenerateCoastWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            int side = random.Next(4);
            int depth = Math.Max(2, SideLength(map, side) * options.WaterPercent / 100);
            int alongLength = Math.Max(1, side <= 1 ? map.Height : map.Width);
            List<CoastFeature> features = CreateCoastFeatures(side, alongLength, depth, Math.Max(5, alongLength / 12), random, true);
            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;
            foreach (HexCellRecord cell in map.Cells)
            {
                int distance = DistanceFromSide(cell, map, side);
                int along = side <= 1 ? cell.R : cell.Q;
                double coastDepth = IrregularCoastDepth(cell, map, side, depth, phaseA, phaseB)
                    + CoastFeatureOffset(side, along, features);
                if (distance <= coastDepth)
                {
                    cell.Water = distance <= Math.Max(1, coastDepth * 0.36) ? "Ocean" : "Sea";
                }
            }
        }

        private void GenerateContinentWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            int depth = Math.Max(1, Math.Min(map.Width, map.Height) * options.WaterPercent / 210);
            int bayCount = Math.Max(2, Math.Min(5, options.WaterPercent / 10 + 1));
            int[] baySides = new int[bayCount];
            int[] bayCenters = new int[bayCount];
            int[] bayRadii = new int[bayCount];
            int[] bayDepths = new int[bayCount];
            for (int i = 0; i < bayCount; i++)
            {
                int side = random.Next(4);
                int alongLength = Math.Max(1, side <= 1 ? map.Height : map.Width);
                baySides[i] = side;
                bayCenters[i] = random.Next(alongLength);
                bayRadii[i] = Math.Max(3, alongLength / random.Next(6, 11));
                bayDepths[i] = Math.Max(depth + 2, depth + random.Next(Math.Max(2, depth / 2), Math.Max(3, depth * 2 + 1)));
            }

            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;
            foreach (HexCellRecord cell in map.Cells)
            {
                for (int side = 0; side < 4; side++)
                {
                    int distance = DistanceFromSide(cell, map, side);
                    double coastDepth = IrregularCoastDepth(cell, map, side, depth, phaseA, phaseB)
                        + CoastBayBonus(cell, map, side, baySides, bayCenters, bayRadii, bayDepths);
                    if (distance <= coastDepth)
                    {
                        cell.Water = distance <= Math.Max(1, coastDepth * 0.45) ? "Ocean" : "Sea";
                        break;
                    }
                }
            }

            int islandCount = Math.Max(1, Math.Min(6, map.Width * map.Height / 2600));
            AddSmallIslands(map, islandCount, random);
            UpdateSeaOceanByLandDistance(map, 3);
        }

        private void GenerateTwoContinentsWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            foreach (HexCellRecord cell in map.Cells)
            {
                cell.Water = EdgeDistance(cell, map) <= 1 ? "Ocean" : "Sea";
            }

            // В этом режиме сначала создаём большую воду, а затем вырезаем две
            // неровные суши. Так это именно два материковых массива, омываемых
            // водой, а не одна карта суши с прорезанной посередине рекой.
            // Два континента должны быть целыми массивами внутри рамки карты.
            // Если шумный контур пересекает край, защитный водный пояс срезает его
            // прямой линией, поэтому радиусы подгоняются под безопасные половины карты.
            bool vertical = random.Next(2) == 0;
            int edgeBufferCells = 2;
            double marginX = Math.Min(0.12, (edgeBufferCells + 0.75) / Math.Max(1.0, map.Width));
            double marginY = Math.Min(0.12, (edgeBufferCells + 0.75) / Math.Max(1.0, map.Height));
            double waterShare = Math.Max(0.42, Math.Min(0.66, options.WaterPercent / 100.0 + 0.16));
            double desiredLandSharePerContinent = (1.0 - waterShare) / 2.0;
            double straitWidth = 0.055 + random.NextDouble() * 0.095;
            double centerJitter = 0.035;
            double shoreStretch = 1.36;
            double shapePower = 3.15 + random.NextDouble() * 0.65;

            if (vertical)
            {
                double leftMinX = marginX;
                double leftMaxX = 0.5 - straitWidth / 2.0;
                double rightMinX = 0.5 + straitWidth / 2.0;
                double rightMaxX = 1.0 - marginX;
                MarkFittedIrregularContinentLand(map, leftMinX, leftMaxX, marginY, 1.0 - marginY,
                    (leftMinX + leftMaxX) / 2.0 + (random.NextDouble() - 0.5) * centerJitter,
                    0.50 + (random.NextDouble() - 0.5) * centerJitter,
                    desiredLandSharePerContinent,
                    1.75,
                    shoreStretch,
                    edgeBufferCells,
                    shapePower,
                    random);
                MarkFittedIrregularContinentLand(map, rightMinX, rightMaxX, marginY, 1.0 - marginY,
                    (rightMinX + rightMaxX) / 2.0 + (random.NextDouble() - 0.5) * centerJitter,
                    0.50 + (random.NextDouble() - 0.5) * centerJitter,
                    desiredLandSharePerContinent,
                    1.75,
                    shoreStretch,
                    edgeBufferCells,
                    shapePower,
                    random);
            }
            else
            {
                double topMinY = marginY;
                double topMaxY = 0.5 - straitWidth / 2.0;
                double bottomMinY = 0.5 + straitWidth / 2.0;
                double bottomMaxY = 1.0 - marginY;
                MarkFittedIrregularContinentLand(map, marginX, 1.0 - marginX, topMinY, topMaxY,
                    0.50 + (random.NextDouble() - 0.5) * centerJitter,
                    (topMinY + topMaxY) / 2.0 + (random.NextDouble() - 0.5) * centerJitter,
                    desiredLandSharePerContinent,
                    0.58,
                    shoreStretch,
                    edgeBufferCells,
                    shapePower,
                    random);
                MarkFittedIrregularContinentLand(map, marginX, 1.0 - marginX, bottomMinY, bottomMaxY,
                    0.50 + (random.NextDouble() - 0.5) * centerJitter,
                    (bottomMinY + bottomMaxY) / 2.0 + (random.NextDouble() - 0.5) * centerJitter,
                    desiredLandSharePerContinent,
                    0.58,
                    shoreStretch,
                    edgeBufferCells,
                    shapePower,
                    random);
            }

            AddSmallIslands(map, Math.Max(1, Math.Min(5, map.Width * map.Height / 3000)), random);
            UpdateSeaOceanByLandDistance(map, 3);
        }

        private void MarkFittedIrregularContinentLand(
            HexMapRecord map,
            double minX,
            double maxX,
            double minY,
            double maxY,
            double centerX,
            double centerY,
            double desiredAreaShare,
            double radiusYOverRadiusX,
            double shoreStretch,
            int edgeBufferCells,
            double shapePower,
            Random random)
        {
            double availableRadiusX = Math.Min(centerX - minX, maxX - centerX) / Math.Max(1.01, shoreStretch);
            double availableRadiusY = Math.Min(centerY - minY, maxY - centerY) / Math.Max(1.01, shoreStretch);
            if (availableRadiusX <= 0.035 || availableRadiusY <= 0.035) return;

            double aspect = Math.Max(0.25, Math.Min(4.0, radiusYOverRadiusX));
            double radiusX = Math.Sqrt(Math.Max(0.005, desiredAreaShare) / (Math.PI * aspect));
            double radiusY = radiusX * aspect;

            if (radiusX > availableRadiusX)
            {
                radiusX = availableRadiusX;
                radiusY = Math.Min(availableRadiusY, desiredAreaShare / (Math.PI * Math.Max(0.01, radiusX)));
            }

            if (radiusY > availableRadiusY)
            {
                radiusY = availableRadiusY;
                radiusX = Math.Min(availableRadiusX, desiredAreaShare / (Math.PI * Math.Max(0.01, radiusY)));
            }

            radiusX = Math.Max(Math.Min(availableRadiusX, 0.09), Math.Min(availableRadiusX, radiusX));
            radiusY = Math.Max(Math.Min(availableRadiusY, 0.09), Math.Min(availableRadiusY, radiusY));
            MarkIrregularContinentLand(map, centerX, centerY, radiusX, radiusY, random, edgeBufferCells, shapePower);
        }

        private void MarkIrregularContinentLand(
            HexMapRecord map,
            double centerX,
            double centerY,
            double radiusX,
            double radiusY,
            Random random,
            int edgeBufferCells = 1,
            double shapePower = 2.0)
        {
            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;
            double phaseC = random.NextDouble() * Math.PI * 2.0;
            foreach (HexCellRecord cell in map.Cells)
            {
                double x = (cell.Q + 0.5) / Math.Max(1.0, map.Width);
                double y = (cell.R + 0.5) / Math.Max(1.0, map.Height);
                double dx = (x - centerX) / Math.Max(0.01, radiusX);
                double dy = (y - centerY) / Math.Max(0.01, radiusY);
                double angle = Math.Atan2(dy, dx);
                double distance = shapePower <= 2.01
                    ? Math.Sqrt(dx * dx + dy * dy)
                    : Math.Pow(Math.Pow(Math.Abs(dx), shapePower) + Math.Pow(Math.Abs(dy), shapePower), 1.0 / shapePower);
                double shore = 1.0
                    + Math.Sin(angle * 3.0 + phaseA) * 0.14
                    + Math.Sin(angle * 5.0 + phaseB) * 0.09
                    + SmoothCoordinateNoise(cell.Q, cell.R, phaseC) * 0.10;
                if (distance <= shore && EdgeDistance(cell, map) > edgeBufferCells)
                {
                    cell.Water = "None";
                }
            }
        }

        private void GenerateInlandSea(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            double radius = Math.Sqrt(map.Width * map.Height * Math.Max(1, options.WaterPercent) / 100.0) / 2.2;
            int centerQ = random.Next(map.Width / 3, Math.Max(map.Width / 3 + 1, map.Width * 2 / 3));
            int centerR = random.Next(map.Height / 3, Math.Max(map.Height / 3 + 1, map.Height * 2 / 3));
            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;
            double phaseC = random.NextDouble() * Math.PI * 2.0;

            foreach (HexCellRecord cell in map.Cells)
            {
                double distance = HexDistance(cell.Q, cell.R, centerQ, centerR);
                double angle = Math.Atan2(cell.R - centerR, cell.Q - centerQ);
                double shore = radius
                    * (1.0
                        + Math.Sin(angle * 3.0 + phaseA) * 0.20
                        + Math.Sin(angle * 5.0 + phaseB) * 0.12)
                    + SmoothCoordinateNoise(cell.Q, cell.R, phaseC) * Math.Max(1.0, radius * 0.12);
                if (distance <= shore)
                {
                    cell.Water = "Sea";
                }
            }
        }

        private void GenerateGulfWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            int side = random.Next(4);
            int coastDepth = Math.Max(2, SideLength(map, side) * options.WaterPercent / 140);
            int alongLength = Math.Max(1, side <= 1 ? map.Height : map.Width);
            double axisMiddle = alongLength * (0.34 + random.NextDouble() * 0.32);
            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;

            foreach (HexCellRecord cell in map.Cells)
            {
                int distance = DistanceFromSide(cell, map, side);
                int along = side <= 1 ? cell.R : cell.Q;
                int inletWidth = Math.Max(2, alongLength / 7);
                int inletDepth = Math.Max(coastDepth + 2, SideLength(map, side) * options.WaterPercent / 72);
                double coastLine = IrregularCoastDepth(cell, map, side, coastDepth, phaseA, phaseB);
                double crookedCenter = axisMiddle
                    + Math.Sin(distance * 0.19 + phaseA) * inletWidth * 0.9
                    + Math.Sin(distance * 0.47 + phaseB) * inletWidth * 0.35;
                double distanceShare = Math.Min(1.0, distance / (double)Math.Max(1, inletDepth));
                double width = inletWidth * (1.05 - distanceShare * 0.48)
                    + Math.Abs(SmoothCoordinateNoise(cell.Q, cell.R, phaseA)) * Math.Max(1.0, inletWidth * 0.35);
                double depth = inletDepth + SmoothCoordinateNoise(cell.Q, cell.R, phaseB) * Math.Max(1.0, coastDepth * 0.7);
                bool coast = distance <= coastLine;
                bool inlet = Math.Abs(along - crookedCenter) <= width && distance <= depth;
                if (coast || inlet)
                {
                    cell.Water = distance <= Math.Max(1, coastDepth / 3) || EdgeDistance(cell, map) == 0 ? "Ocean" : "Sea";
                }
            }
        }

        private double IrregularCoastDepth(
            HexCellRecord cell,
            HexMapRecord map,
            int side,
            double baseDepth,
            double phaseA,
            double phaseB)
        {
            int along = side <= 1 ? cell.R : cell.Q;
            int length = Math.Max(1, side <= 1 ? map.Height : map.Width);
            double t = along / (double)length;
            double sideShift = side * 0.91;
            double wave = Math.Sin(t * Math.PI * 2.0 + phaseA + sideShift) * baseDepth * 0.35
                + Math.Sin(t * Math.PI * 5.0 + phaseB + sideShift) * baseDepth * 0.20
                + SmoothCoordinateNoise(cell.Q, cell.R, phaseA + sideShift) * baseDepth * 0.24;
            return Math.Max(1.0, baseDepth + wave);
        }

        private double CoastBayBonus(
            HexCellRecord cell,
            HexMapRecord map,
            int side,
            int[] baySides,
            int[] bayCenters,
            int[] bayRadii,
            int[] bayDepths)
        {
            if (baySides == null) return 0.0;

            int along = side <= 1 ? cell.R : cell.Q;
            double bonus = 0.0;
            for (int i = 0; i < baySides.Length; i++)
            {
                if (baySides[i] != side) continue;
                int radius = Math.Max(1, bayRadii[i]);
                int offset = Math.Abs(along - bayCenters[i]);
                if (offset > radius) continue;

                double share = 1.0 - offset / (double)radius;
                bonus = Math.Max(bonus, bayDepths[i] * share * share);
            }

            return bonus;
        }

        private List<CoastFeature> CreateCoastFeatures(
            int side,
            int alongLength,
            double baseDepth,
            int count,
            Random random,
            bool allowCapes)
        {
            List<CoastFeature> result = new List<CoastFeature>();
            for (int i = 0; i < count; i++)
            {
                bool cape = allowCapes && random.Next(100) < 35;
                result.Add(new CoastFeature
                {
                    Side = side,
                    Center = random.Next(Math.Max(1, alongLength)),
                    Radius = Math.Max(3, random.Next(Math.Max(4, alongLength / 14), Math.Max(5, alongLength / 5))),
                    DepthOffset = (cape ? -1.0 : 1.0) * (baseDepth * (0.25 + random.NextDouble() * (cape ? 0.45 : 0.80)))
                });
            }

            return result;
        }

        private double CoastFeatureOffset(int side, int along, List<CoastFeature> features)
        {
            if (features == null || features.Count == 0) return 0.0;

            double offset = 0.0;
            foreach (CoastFeature feature in features)
            {
                if (feature == null || feature.Side != side) continue;
                int distance = Math.Abs(along - feature.Center);
                if (distance > feature.Radius) continue;

                double share = 1.0 - distance / (double)Math.Max(1, feature.Radius);
                offset += feature.DepthOffset * share * share * (3.0 - 2.0 * share);
            }

            return offset;
        }

        private void AddSmallIslands(HexMapRecord map, int count, Random random)
        {
            if (map == null || random == null || count <= 0) return;

            List<HexCellRecord> candidates = map.Cells
                .Where(c => c.Water == "Sea" || c.Water == "Ocean")
                .Where(c => EdgeDistance(c, map) > 2)
                .Where(c => NearestLandDistance(map, c, 5) >= 3)
                .OrderBy(c => random.Next())
                .ToList();
            foreach (HexCellRecord center in candidates)
            {
                if (count <= 0) break;
                if (center.Water == "None") continue;

                int landDistance = NearestLandDistance(map, center, 5);
                int radius = landDistance >= 4 && random.Next(100) < 22 ? 2 : 1;
                if (landDistance <= radius + 1) continue;
                MarkIslandLand(map, center.Q, center.R, radius, random);
                count--;
            }
        }

        private int NearestLandDistance(HexMapRecord map, HexCellRecord origin, int maxDistance)
        {
            if (map == null || origin == null) return maxDistance + 1;
            int best = maxDistance + 1;
            foreach (HexCellRecord cell in map.Cells.Where(c => c.Water == "None"))
            {
                int distance = HexDistance(origin.Q, origin.R, cell.Q, cell.R);
                if (distance < best) best = distance;
                if (best <= 1) break;
            }

            return best;
        }

        private void UpdateSeaOceanByLandDistance(HexMapRecord map, int seaDistance)
        {
            if (map == null) return;

            HashSet<string> nearLandWater = new HashSet<string>();
            Dictionary<string, int> distanceByCell = new Dictionary<string, int>();
            Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
            foreach (HexCellRecord land in map.Cells.Where(c => c.Water == "None"))
            {
                string key = CellKey(land.Q, land.R);
                if (distanceByCell.ContainsKey(key)) continue;
                distanceByCell[key] = 0;
                queue.Enqueue(land);
            }

            while (queue.Count > 0)
            {
                HexCellRecord cell = queue.Dequeue();
                int distance = distanceByCell[CellKey(cell.Q, cell.R)];
                if (distance >= seaDistance) continue;

                foreach (HexCellRecord neighbor in GetNeighbors(map, cell))
                {
                    string key = CellKey(neighbor.Q, neighbor.R);
                    if (distanceByCell.ContainsKey(key)) continue;

                    int nextDistance = distance + 1;
                    distanceByCell[key] = nextDistance;
                    if (IsSeaOrOcean(neighbor)) nearLandWater.Add(key);
                    queue.Enqueue(neighbor);
                }
            }

            foreach (HexCellRecord cell in map.Cells.Where(IsSeaOrOcean))
            {
                cell.Water = nearLandWater.Contains(CellKey(cell.Q, cell.R)) ? "Sea" : "Ocean";
            }
        }

        private double SmoothCoordinateNoise(int q, int r, double phase)
        {
            double a = Math.Sin(q * 0.173 + r * 0.317 + phase);
            double b = Math.Sin(q * 0.071 - r * 0.149 + phase * 1.37);
            double c = Math.Cos(q * 0.227 + r * 0.053 + phase * 0.73);
            return (a * 0.55 + b * 0.30 + c * 0.15);
        }

        private void GenerateArchipelagoWater(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Архипелаг начинается как сплошной океан, а затем получает группы
            // островов; так суша не слипается в скрытый материк.
            foreach (HexCellRecord cell in map.Cells)
            {
                cell.Water = EdgeDistance(cell, map) <= 1 ? "Ocean" : "Sea";
            }

            int area = map.Width * map.Height;
            int clusterCount = Math.Max(3, Math.Min(12, area / 650 + 2));
            for (int cluster = 0; cluster < clusterCount; cluster++)
            {
                int centerQ = random.Next(Math.Max(1, map.Width / 8), Math.Max(2, map.Width - Math.Max(1, map.Width / 8)));
                int centerR = random.Next(Math.Max(1, map.Height / 8), Math.Max(2, map.Height - Math.Max(1, map.Height / 8)));
                int islandsInCluster = random.Next(3, 7);
                int spread = Math.Max(3, Math.Min(map.Width, map.Height) / 10);

                for (int island = 0; island < islandsInCluster; island++)
                {
                    int q = Math.Max(0, Math.Min(map.Width - 1, centerQ + random.Next(-spread, spread + 1)));
                    int r = Math.Max(0, Math.Min(map.Height - 1, centerR + random.Next(-spread, spread + 1)));
                    int radius = random.Next(1, random.Next(100) < 20 ? 5 : 4);
                    int fittedRadius = FitIslandRadiusToEdge(map, q, r, radius);
                    if (fittedRadius < 0) continue;
                    MarkArchipelagoIslandLand(map, q, r, fittedRadius, random);
                }
            }

            UpdateSeaOceanByLandDistance(map, 2);
        }

        private int FitIslandRadiusToEdge(HexMapRecord map, int centerQ, int centerR, int radius)
        {
            HexCellRecord center = GetCell(map, centerQ, centerR);
            if (center == null) return 0;

            // В архипелаге на малых картах большой остров у края выглядит как
            // срезанный защитной рамкой. Лучше уменьшить остров до доступного
            // радиуса, чем получить длинный прямой берег на расстоянии двух гексов.
            int availableRadius = EdgeDistance(center, map) - 3;
            return availableRadius < 0 ? -1 : Math.Min(radius, availableRadius);
        }

        private void MarkArchipelagoIslandLand(HexMapRecord map, int centerQ, int centerR, int radius, Random random)
        {
            HexCellRecord center = GetCell(map, centerQ, centerR);
            if (center == null) return;

            // Архипелаг не должен рисовать идеальные гексовые диски.
            // Переводим odd-r координаты в плоскость, поворачиваем эллипс и
            // размываем берег несколькими волнами и низкочастотным шумом.
            double centerX;
            double centerY;
            IslandPlotCoordinates(centerQ, centerR, out centerX, out centerY);

            double baseRadius = Math.Max(0.85, radius + 0.55);
            double axisA = baseRadius * (0.85 + random.NextDouble() * 0.55);
            double axisB = baseRadius * (0.60 + random.NextDouble() * 0.60);
            double rotation = random.NextDouble() * Math.PI;
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double phaseA = random.NextDouble() * Math.PI * 2.0;
            double phaseB = random.NextDouble() * Math.PI * 2.0;
            double phaseC = random.NextDouble() * Math.PI * 2.0;
            int scanRadius = Math.Max(3, radius + 4);

            for (int r = Math.Max(0, centerR - scanRadius); r <= Math.Min(map.Height - 1, centerR + scanRadius); r++)
            {
                for (int q = Math.Max(0, centerQ - scanRadius); q <= Math.Min(map.Width - 1, centerQ + scanRadius); q++)
                {
                    HexCellRecord cell = GetCell(map, q, r);
                    if (cell == null) continue;

                    int edge = EdgeDistance(cell, map);
                    if (edge <= 2) continue;

                    double x;
                    double y;
                    IslandPlotCoordinates(cell.Q, cell.R, out x, out y);
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double localX = dx * cos + dy * sin;
                    double localY = -dx * sin + dy * cos;
                    double normalizedX = localX / Math.Max(0.1, axisA);
                    double normalizedY = localY / Math.Max(0.1, axisB);
                    double distance = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    double angle = Math.Atan2(normalizedY, normalizedX);
                    double shore = 1.0
                        + Math.Sin(angle * 2.0 + phaseA) * 0.22
                        + Math.Sin(angle * 3.0 + phaseB) * 0.16
                        + SmoothCoordinateNoise(cell.Q, cell.R, phaseC) * 0.24;

                    if (edge <= 4)
                    {
                        shore -= (4 - edge) * 0.20;
                    }

                    if (cell.Q == centerQ && cell.R == centerR)
                    {
                        shore = Math.Max(shore, 0.18);
                    }

                    if (distance <= shore)
                    {
                        cell.Water = "None";
                    }
                }
            }
        }

        private void IslandPlotCoordinates(int q, int r, out double x, out double y)
        {
            x = q + ((r & 1) == 1 ? 0.5 : 0.0);
            y = r * 0.8660254037844386;
        }

        private void MarkIslandLand(HexMapRecord map, int centerQ, int centerR, int radius, Random random)
        {
            foreach (HexCellRecord cell in map.Cells)
            {
                int distance = HexDistance(cell.Q, cell.R, centerQ, centerR);
                int edge = EdgeDistance(cell, map);
                if (edge <= 1) continue;
                if (distance <= radius + random.Next(-1, 2))
                {
                    cell.Water = "None";
                }
            }
        }

        private void NormalizeLargeWater(HexMapRecord map, RegionGenerationOptions options)
        {
            if (map == null || options == null) return;
            if (string.Equals(options.WaterLayout, "InlandSea", StringComparison.OrdinalIgnoreCase)) return;

            // Море и океан должны быть частью большой водной массы. Малые замкнутые
            // пятна внутри суши превращаем в озёра, чтобы не появлялось "море в миске".
            HashSet<string> connectedToEdge = new HashSet<string>();
            Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
            foreach (HexCellRecord edgeCell in map.Cells.Where(c => IsSeaOrOcean(c) && EdgeDistance(c, map) == 0))
            {
                string key = CellKey(edgeCell.Q, edgeCell.R);
                if (!connectedToEdge.Add(key)) continue;
                queue.Enqueue(edgeCell);
            }

            while (queue.Count > 0)
            {
                HexCellRecord cell = queue.Dequeue();
                foreach (HexCellRecord neighbor in GetNeighbors(map, cell).Where(IsSeaOrOcean))
                {
                    string key = CellKey(neighbor.Q, neighbor.R);
                    if (!connectedToEdge.Add(key)) continue;
                    queue.Enqueue(neighbor);
                }
            }

            foreach (HexCellRecord cell in map.Cells.Where(IsSeaOrOcean).ToList())
            {
                if (connectedToEdge.Contains(CellKey(cell.Q, cell.R))) continue;
                cell.Water = "Lake";
            }
        }

        private void NormalizeGeneratedHydrology(HexMapRecord map, RegionGenerationOptions options)
        {
            if (map == null) return;
            NormalizeLargeWater(map, options);
            ConvertCoastalLakesToSea(map);
            RemoveRiverEdgesInsideWater(map);
            RemoveShortRiverComponents(map, 6, 5, 8);
        }

        private void ConvertCoastalLakesToSea(HexMapRecord map)
        {
            if (map == null) return;

            HashSet<string> visited = new HashSet<string>();
            foreach (HexCellRecord start in map.Cells.Where(c => c.Water == "Lake").ToList())
            {
                string startKey = CellKey(start.Q, start.R);
                if (!visited.Add(startKey)) continue;

                List<HexCellRecord> cluster = new List<HexCellRecord>();
                Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
                queue.Enqueue(start);
                cluster.Add(start);

                while (queue.Count > 0)
                {
                    HexCellRecord cell = queue.Dequeue();
                    foreach (HexCellRecord neighbor in GetNeighbors(map, cell).Where(c => c.Water == "Lake"))
                    {
                        string key = CellKey(neighbor.Q, neighbor.R);
                        if (!visited.Add(key)) continue;
                        queue.Enqueue(neighbor);
                        cluster.Add(neighbor);
                    }
                }

                bool touchesLargeWater = cluster.Any(c => GetNeighbors(map, c).Any(IsSeaOrOcean));
                if (!touchesLargeWater) continue;

                foreach (HexCellRecord cell in cluster)
                {
                    cell.Water = "Sea";
                }
            }
        }

        private void RemoveRiverEdgesInsideWater(HexMapRecord map)
        {
            if (map == null || map.Rivers == null) return;

            // Река может входить в водоём, но не должна продолжаться как ребро между
            // двумя водными гексами озера, моря или океана.
            map.Rivers = map.Rivers
                .Where(edge =>
                {
                    HexCellRecord a = GetCell(map, edge.AQ, edge.AR);
                    HexCellRecord b = GetCell(map, edge.BQ, edge.BR);
                    return !(IsWater(a) && IsWater(b));
                })
                .ToList();
        }

        private void RemoveShortRiverComponents(HexMapRecord map, int minCells, int minEdges, int minWaterTouchingCells)
        {
            if (map == null || map.Rivers == null || map.Rivers.Count == 0) return;

            Dictionary<string, List<MapEdgeRecord>> byEndpoint = new Dictionary<string, List<MapEdgeRecord>>();
            foreach (MapEdgeRecord edge in map.Rivers.Where(e => e != null))
            {
                AddEndpoint(byEndpoint, CellKey(edge.AQ, edge.AR), edge);
                AddEndpoint(byEndpoint, CellKey(edge.BQ, edge.BR), edge);
            }

            HashSet<string> visitedEdges = new HashSet<string>();
            HashSet<string> keepEdges = new HashSet<string>();
            foreach (MapEdgeRecord start in map.Rivers.Where(e => e != null))
            {
                string startKey = start.NormalizedKey();
                if (!visitedEdges.Add(startKey)) continue;

                List<MapEdgeRecord> component = new List<MapEdgeRecord>();
                HashSet<string> componentCells = new HashSet<string>();
                Queue<MapEdgeRecord> queue = new Queue<MapEdgeRecord>();
                queue.Enqueue(start);
                component.Add(start);

                while (queue.Count > 0)
                {
                    MapEdgeRecord edge = queue.Dequeue();
                    componentCells.Add(CellKey(edge.AQ, edge.AR));
                    componentCells.Add(CellKey(edge.BQ, edge.BR));
                    foreach (string endpoint in new[] { CellKey(edge.AQ, edge.AR), CellKey(edge.BQ, edge.BR) })
                    {
                        List<MapEdgeRecord> connected;
                        if (!byEndpoint.TryGetValue(endpoint, out connected)) continue;
                        foreach (MapEdgeRecord next in connected)
                        {
                            string key = next.NormalizedKey();
                            if (!visitedEdges.Add(key)) continue;
                            queue.Enqueue(next);
                            component.Add(next);
                        }
                    }
                }

                bool touchesWaterOrMapEdge = false;
                foreach (string cellKey in componentCells)
                {
                    HexCellRecord cell = GetCell(map, cellKey);
                    if (IsWater(cell) || (cell != null && EdgeDistance(cell, map) == 0))
                    {
                        touchesWaterOrMapEdge = true;
                        break;
                    }
                }

                // После создания озер короткая речка может превратиться в обрубок у берега.
                // Поэтому для компонентов, которые уже касаются воды или края карты, фильтр строже.
                int requiredCells = touchesWaterOrMapEdge ? Math.Max(minCells, minWaterTouchingCells) : minCells;
                int requiredEdges = touchesWaterOrMapEdge ? Math.Max(minEdges, minWaterTouchingCells - 1) : minEdges;
                if (componentCells.Count < requiredCells || component.Count < requiredEdges) continue;

                foreach (MapEdgeRecord edge in component)
                {
                    keepEdges.Add(edge.NormalizedKey());
                }
            }

            map.Rivers = map.Rivers.Where(e => e != null && keepEdges.Contains(e.NormalizedKey())).ToList();
        }

        private bool IsSeaOrOcean(HexCellRecord cell)
        {
            return cell != null && (cell.Water == "Sea" || cell.Water == "Ocean");
        }

        private bool IsNearSeaOrOcean(HexMapRecord map, HexCellRecord cell)
        {
            return cell != null && GetNeighbors(map, cell).Any(IsSeaOrOcean);
        }

        private void GenerateRivers(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Реки стартуют с высот и идут по соседним гексам к более низким/водным областям.
            // Это не гидрологическая модель, а быстрый слой для ACKS-карты и торговых путей.
            List<HexCellRecord> starts = map.Cells
                .Where(c => c.Water == "None" && (c.Elevation == "Mountains" || c.Elevation == "Hills"))
                .OrderBy(c => random.Next())
                .ToList();

            int riverCount = Math.Min(starts.Count, Math.Max(0, map.Width * map.Height * options.RiverPercent / 7000));
            HashSet<string> edges = new HashSet<string>();
            HashSet<string> riverCells = new HashSet<string>();
            for (int i = 0; i < riverCount; i++)
            {
                HexCellRecord current = starts[i];
                HashSet<string> localCells = new HashSet<string> { CellKey(current.Q, current.R) };
                HexCellRecord target = PickRiverTarget(map, current, random);
                int length = random.Next(8, Math.Max(10, Math.Min(map.Width, map.Height) / 2 + 8));
                bool reachedWater = false;

                for (int step = 0; step < length; step++)
                {
                    List<HexCellRecord> neighbors = GetNeighbors(map, current)
                        .Where(n => n.Water != "Ocean" && !localCells.Contains(CellKey(n.Q, n.R)))
                        .OrderBy(n => WaterFlowScore(current, n, target, random))
                        .ToList();
                    if (neighbors.Count == 0) break;

                    HexCellRecord next = neighbors[0];
                    bool joinsExistingRiver = riverCells.Contains(CellKey(next.Q, next.R));
                    MapEdgeRecord edge = new MapEdgeRecord { AQ = current.Q, AR = current.R, BQ = next.Q, BR = next.R, Kind = "River" };
                    string key = edge.NormalizedKey();
                    if (!edges.Contains(key))
                    {
                        map.Rivers.Add(edge);
                        edges.Add(key);
                    }

                    riverCells.Add(CellKey(current.Q, current.R));
                    riverCells.Add(CellKey(next.Q, next.R));
                    localCells.Add(CellKey(next.Q, next.R));
                    current = next;
                    if (current.Water == "Sea" || current.Water == "Lake" || EdgeDistance(current, map) == 0)
                    {
                        reachedWater = true;
                        break;
                    }

                    if (step > 2 && joinsExistingRiver)
                    {
                        reachedWater = true;
                        break;
                    }
                }

                if (!reachedWater && localCells.Count >= 4 && current.Water == "None")
                {
                    riverDeadEnds.Add(current);
                }
            }
        }

        private double WaterFlowScore(HexCellRecord current, HexCellRecord cell, HexCellRecord target, Random random)
        {
            int currentElevation = ElevationRank(current);
            int nextElevation = ElevationRank(cell);
            int uphillPenalty = nextElevation > currentElevation ? 6 : 0;
            int waterBonus = cell.Water == "Sea" || cell.Water == "Lake" ? -10 : 0;
            int targetDistance = target == null ? 0 : HexDistance(cell.Q, cell.R, target.Q, target.R);
            return nextElevation * 3 + uphillPenalty + targetDistance * 0.55 + TerrainFlowPenalty(cell.Terrain) + waterBonus + random.NextDouble() * 1.4;
        }

        private HexCellRecord PickRiverTarget(HexMapRecord map, HexCellRecord start, Random random)
        {
            List<HexCellRecord> water = map.Cells
                .Where(c => c.Water == "Sea" || c.Water == "Lake")
                .OrderBy(c => HexDistance(start.Q, start.R, c.Q, c.R))
                .Take(12)
                .ToList();

            if (water.Count > 0) return water[random.Next(Math.Min(3, water.Count))];

            return map.Cells
                .Where(c => EdgeDistance(c, map) == 0)
                .OrderBy(c => HexDistance(start.Q, start.R, c.Q, c.R))
                .FirstOrDefault();
        }

        private int ElevationRank(HexCellRecord cell)
        {
            if (cell == null) return 1;
            if (cell.Water == "Sea" || cell.Water == "Ocean" || cell.Water == "Lake") return 0;
            if (cell.Elevation == "Mountains") return 3;
            if (cell.Elevation == "Hills") return 2;
            return 1;
        }

        private double TerrainFlowPenalty(string terrain)
        {
            if (terrain == "Marsh") return -1.5;
            if (terrain == "Grasslands" || terrain == "Steppe" || terrain == "Savanna") return -0.6;
            if (terrain == "Desert") return 2.2;
            if (terrain == "Tundra") return 0.8;
            return 0;
        }

        private void GenerateLakes(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (options.LakePercent <= 0) return;

            // Озера появляются как редкие водосборы: у тупиков рек, в болотах и во влажных низинах.
            // Отдельный процент не дает морскому побережью автоматически превращать карту в россыпь озер.
            int targetCount = Math.Max(0, map.Width * map.Height * options.LakePercent / 2500);
            List<HexCellRecord> candidates = new List<HexCellRecord>();
            candidates.AddRange(riverDeadEnds);
            candidates.AddRange(map.Cells.Where(c => c.Water == "None" && (c.Terrain == "Marsh" || c.Terrain == "Taiga" || c.Terrain == "Rainforest")));
            candidates.AddRange(map.Cells.Where(c => c.Water == "None" && c.Elevation != "Mountains"));

            HashSet<string> used = new HashSet<string>();
            foreach (HexCellRecord center in candidates.OrderBy(c => random.Next()))
            {
                if (targetCount <= 0) break;
                if (center == null || center.Water != "None") continue;
                if (IsNearSeaOrOcean(map, center)) continue;
                string centerKey = CellKey(center.Q, center.R);
                if (!used.Add(centerKey)) continue;

                int radius = random.Next(100) < 20 ? 2 : 1;
                foreach (HexCellRecord cell in map.Cells.Where(c => c.Water == "None" && HexDistance(c.Q, c.R, center.Q, center.R) <= radius))
                {
                    if (IsNearSeaOrOcean(map, cell)) continue;
                    int chance = radius == 1 ? 78 : 56;
                    if (random.Next(100) < chance)
                    {
                        cell.Water = "Lake";
                    }
                }

                targetCount--;
            }
        }
    }
}
