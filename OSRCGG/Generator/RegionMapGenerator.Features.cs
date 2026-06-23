using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        private void GenerateHexFeatures(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || options == null || random == null) return;
            EnsureMapLists(map);
            map.Features.Clear();
            map.Dungeons.Clear();

            // Особенности зависят от уже готовой карты: поселения дают дистанции,
            // реки открывают водопады, а список занятых клеток защищает от наложений.
            Dictionary<string, MapSettlementRecord> settlementsByCell = BuildSettlementCellLookup(map);
            Dictionary<string, int> nearestSettlementDistance = BuildNearestSettlementDistances(map);
            HashSet<string> domainCells = BuildDomainCellSet(map);
            HashSet<string> riverCells = BuildRiverCellSet(map);
            HashSet<string> usedCells = new HashSet<string>();

            if (options.GenerateDungeons)
            {
                GenerateDungeonFeatures(map, options, random, settlementsByCell, nearestSettlementDistance, riverCells, usedCells);
            }

            GenerateNaturalHexFeatures(map, options, random, settlementsByCell, domainCells, riverCells, usedCells);
        }

        private void GenerateDungeonFeatures(
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            Dictionary<string, MapSettlementRecord> settlementsByCell,
            Dictionary<string, int> nearestSettlementDistance,
            HashSet<string> riverCells,
            HashSet<string> usedCells)
        {
            int target = TargetDungeonCount(map, options);
            DungeonGenerator dungeonGenerator = new DungeonGenerator();

            for (int index = 0; index < target; index++)
            {
                // На карте хранится только внешняя метка данжа, а подробная структура
                // живет в DungeonRecord и связывается с гексом через DungeonId.
                HexCellRecord cell = PickDungeonFeatureCell(map, random, settlementsByCell, nearestSettlementDistance, usedCells);
                if (cell == null) break;

                string key = CellKey(cell.Q, cell.R);
                MapSettlementRecord settlement;
                settlementsByCell.TryGetValue(key, out settlement);
                int distance = DistanceFromLookup(nearestSettlementDistance, cell);
                DungeonPlacementContext context = new DungeonPlacementContext
                {
                    Cell = cell,
                    Settlement = settlement,
                    NearestSettlementDistance = distance,
                    HasRiver = riverCells.Contains(key),
                    SeismicRegion = string.Equals(options.Seismicity, "Seismic", StringComparison.OrdinalIgnoreCase)
                };

                string dungeonType = DungeonCatalog.PickDungeonType(random, context);
                int level = PickDungeonRecommendedLevel(distance, settlement != null, random);
                string size = PickDungeonSize(random, options);
                DungeonRecord dungeon = dungeonGenerator.Generate(new DungeonGenerationOptions
                {
                    Seed = options.Seed + "|dungeon|" + index + "|" + cell.Q + "," + cell.R,
                    DungeonType = dungeonType,
                    Size = size,
                    RecommendedLevel = level,
                    RussianOutput = options.UseRussianNames
                }, random);

                HexFeatureRecord feature = new HexFeatureRecord
                {
                    Name = dungeon.Name,
                    Kind = "Dungeon",
                    Subtype = dungeonType,
                    Q = cell.Q,
                    R = cell.R,
                    IconKey = DungeonCatalog.DungeonTypeIconKey(dungeonType),
                    Description = BuildDungeonFeatureDescription(dungeon, settlement, distance, options.UseRussianNames),
                    Severity = dungeon.ChallengeTier,
                    DungeonId = dungeon.Id,
                    DungeonType = dungeonType,
                    DungeonLevel = level,
                    DungeonSize = size,
                    UpdatedAt = DateTime.Now
                };

                map.Dungeons.Add(dungeon);
                map.Features.Add(feature);
                usedCells.Add(key);
                usedFeatureNames.Add(feature.Name);
            }
        }

        private void GenerateNaturalHexFeatures(
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            Dictionary<string, MapSettlementRecord> settlementsByCell,
            HashSet<string> domainCells,
            HashSet<string> riverCells,
            HashSet<string> usedCells)
        {
            int target = TargetNaturalFeatureCount(map, options);
            bool seismic = string.Equals(options.Seismicity, "Seismic", StringComparison.OrdinalIgnoreCase);
            bool stable = string.Equals(options.Seismicity, "Stable", StringComparison.OrdinalIgnoreCase);

            for (int index = 0; index < target; index++)
            {
                HexFeatureDefinition definition = PickNaturalFeatureDefinition(map, random, riverCells, usedCells, seismic, stable);
                if (definition == null) break;

                bool preferSettledCell = index % 4 == 1 || random.Next(100) < 18;
                HexCellRecord cell = PickNaturalFeatureCell(
                    map,
                    definition,
                    random,
                    settlementsByCell,
                    domainCells,
                    riverCells,
                    usedCells,
                    seismic,
                    preferSettledCell);
                if (cell == null) continue;

                string name = GenerateUniqueFeatureName(random, options.CultureKey, definition.Subtype);
                HexFeatureRecord feature = new HexFeatureRecord
                {
                    Name = string.IsNullOrWhiteSpace(name)
                        ? DungeonCatalog.LocalizeFeatureSubtype(definition.Subtype, options.UseRussianNames)
                        : name,
                    Kind = definition.Kind,
                    Subtype = definition.Subtype,
                    Q = cell.Q,
                    R = cell.R,
                    IconKey = definition.IconKey,
                    Description = definition.Description,
                    Severity = definition.Severity,
                    UpdatedAt = DateTime.Now
                };

                map.Features.Add(feature);
                usedCells.Add(CellKey(cell.Q, cell.R));
            }
        }

        private int TargetDungeonCount(HexMapRecord map, RegionGenerationOptions options)
        {
            int area = Math.Max(1, map.Width * map.Height);
            int target = Math.Max(1, area / 220);
            if (area < 500) target = Math.Max(1, area / 180);
            if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase)) target = target * 12 / 10 + 1;
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) target = Math.Max(1, target * 8 / 10);
            return Clamp(target, 1, 45);
        }

        private int TargetNaturalFeatureCount(HexMapRecord map, RegionGenerationOptions options)
        {
            int area = Math.Max(1, map.Width * map.Height);
            int target = Math.Max(1, area / 360);
            if (string.Equals(options.Seismicity, "Seismic", StringComparison.OrdinalIgnoreCase)) target += Math.Max(1, area / 1200);
            if (string.Equals(options.Seismicity, "Stable", StringComparison.OrdinalIgnoreCase)) target = Math.Max(1, target * 8 / 10);
            return Clamp(target, 1, 36);
        }

        private HexCellRecord PickDungeonFeatureCell(
            HexMapRecord map,
            Random random,
            Dictionary<string, MapSettlementRecord> settlementsByCell,
            Dictionary<string, int> nearestSettlementDistance,
            HashSet<string> usedCells)
        {
            bool preferSettlement = settlementsByCell.Count > 0 && random.Next(100) < 8;
            List<WeightedHexCell> candidates = new List<WeightedHexCell>();
            List<WeightedHexCell> settlementCandidates = new List<WeightedHexCell>();
            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell == null || IsWater(cell)) continue;
                string key = CellKey(cell.Q, cell.R);
                if (usedCells.Contains(key)) continue;
                if (IsNearUsedFeature(map, cell, usedCells, 2)) continue;

                // Данжи чаще лежат в стороне от цивилизации, но небольшой шанс
                // поселкового подземелья остается для канализаций, катакомб и башен.
                int distance = DistanceFromLookup(nearestSettlementDistance, cell);
                bool settlement = settlementsByCell.ContainsKey(key);
                double weight = 1.0 + Math.Min(18, distance) * 1.45;
                if (settlement) weight *= 0.16;
                if (cell.Terrain == "DeepForest" || cell.Terrain == "DeepTaiga" || cell.Terrain == "Marsh") weight += 5.0;
                if (cell.Elevation == "Hills") weight += 2.0;
                if (cell.Elevation == "Mountains") weight += 4.0;
                WeightedHexCell candidate = new WeightedHexCell(cell, Math.Max(0.05, weight));
                candidates.Add(candidate);
                if (settlement) settlementCandidates.Add(candidate);
            }

            if (preferSettlement && settlementCandidates.Count > 0)
            {
                return PickWeightedHexCell(settlementCandidates, random);
            }

            return PickWeightedHexCell(candidates, random);
        }

        private HexFeatureDefinition PickNaturalFeatureDefinition(
            HexMapRecord map,
            Random random,
            HashSet<string> riverCells,
            HashSet<string> usedCells,
            bool seismic,
            bool stable)
        {
            List<HexFeatureDefinition> candidates = new List<HexFeatureDefinition>();
            foreach (HexFeatureDefinition definition in DungeonCatalog.NaturalFeatures)
            {
                if (stable && (definition.Subtype == "Volcano" || definition.Subtype == "Hot spring" || definition.Subtype == "Geyser field")) continue;
                bool anyCell = map.Cells.Any(c =>
                    c != null
                    && !usedCells.Contains(CellKey(c.Q, c.R))
                    && DungeonCatalog.IsFeatureAllowed(definition.Subtype, c, riverCells.Contains(CellKey(c.Q, c.R)), seismic));
                if (anyCell) candidates.Add(definition);
            }

            if (candidates.Count == 0) return null;
            return candidates[random.Next(candidates.Count)];
        }

        private HexCellRecord PickNaturalFeatureCell(
            HexMapRecord map,
            HexFeatureDefinition definition,
            Random random,
            Dictionary<string, MapSettlementRecord> settlementsByCell,
            HashSet<string> domainCells,
            HashSet<string> riverCells,
            HashSet<string> usedCells,
            bool seismic,
            bool preferSettledCell)
        {
            List<WeightedHexCell> preferred = new List<WeightedHexCell>();
            List<WeightedHexCell> fallback = new List<WeightedHexCell>();
            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell == null || IsWater(cell)) continue;
                string key = CellKey(cell.Q, cell.R);
                if (usedCells.Contains(key)) continue;
                bool hasRiver = riverCells.Contains(key);
                if (!DungeonCatalog.IsFeatureAllowed(definition.Subtype, cell, hasRiver, seismic)) continue;

                bool settlement = settlementsByCell.ContainsKey(key);
                bool domain = domainCells.Contains(key);
                double weight = NaturalFeatureCellWeight(definition.Subtype, cell, hasRiver, settlement, domain);
                WeightedHexCell candidate = new WeightedHexCell(cell, Math.Max(0.05, weight));
                fallback.Add(candidate);
                if (settlement || domain)
                {
                    preferred.Add(candidate);
                }
            }

            if (preferSettledCell && preferred.Count > 0)
            {
                return PickWeightedHexCell(preferred, random);
            }

            return PickWeightedHexCell(fallback, random);
        }

        private double NaturalFeatureCellWeight(string subtype, HexCellRecord cell, bool hasRiver, bool settlement, bool domain)
        {
            double weight = 1.0;
            if (subtype == "Volcano" && cell.Elevation == "Mountains") weight += 12.0;
            if (subtype == "Hot spring" && (cell.Elevation == "Hills" || cell.Elevation == "Mountains")) weight += 8.0;
            if (subtype == "Geyser field" && (cell.Elevation == "Hills" || cell.Elevation == "Mountains")) weight += 7.0;
            if (subtype == "Waterfall" && hasRiver) weight += 10.0;
            if (subtype == "Quicksand" && cell.Terrain == "Desert") weight += 10.0;
            if (subtype == "Fey grove" && (cell.Terrain == "DeepForest" || cell.Terrain == "Forest")) weight += 8.0;
            if (subtype == "Monster nest") weight += cell.Terrain == "DeepForest" || cell.Terrain == "Marsh" ? 5.0 : 1.0;
            if (cell.Elevation == "Mountains") weight += 1.5;
            if (cell.Terrain == "Marsh" || cell.Terrain == "DeepForest" || cell.Terrain == "DeepTaiga") weight += 2.0;
            if (domain) weight *= 2.1;
            if (settlement)
            {
                // Природные и мистические особенности могут быть внутри освоенной земли:
                // источник, древние камни или провал не обязаны избегать городов и доменов.
                weight *= 2.8;
                if (subtype == "Monster nest") weight *= 0.45;
                if (subtype == "Meteor crater" || subtype == "Ancient stones" || subtype == "Sacred spring") weight *= 1.4;
            }
            return weight;
        }

        private Dictionary<string, MapSettlementRecord> BuildSettlementCellLookup(HexMapRecord map)
        {
            Dictionary<string, MapSettlementRecord> result = new Dictionary<string, MapSettlementRecord>();
            if (map == null || map.Settlements == null) return result;
            foreach (MapSettlementRecord settlement in map.Settlements)
            {
                if (settlement == null) continue;
                string key = CellKey(settlement.Q, settlement.R);
                if (!result.ContainsKey(key)) result[key] = settlement;
            }

            return result;
        }

        private Dictionary<string, int> BuildNearestSettlementDistances(HexMapRecord map)
        {
            Dictionary<string, int> distance = new Dictionary<string, int>();
            if (map == null || map.Cells == null) return distance;
            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell != null) distance[CellKey(cell.Q, cell.R)] = int.MaxValue;
            }

            Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
            foreach (MapSettlementRecord settlement in map.Settlements ?? new List<MapSettlementRecord>())
            {
                HexCellRecord cell = GetCell(map, settlement.Q, settlement.R);
                if (cell == null) continue;
                string key = CellKey(cell.Q, cell.R);
                if (distance[key] == 0) continue;
                distance[key] = 0;
                queue.Enqueue(cell);
            }

            foreach (DomainRecord domain in map.Domains ?? new List<DomainRecord>())
            {
                if (domain == null || domain.StrongholdQ < 0 || domain.StrongholdR < 0) continue;
                HexCellRecord cell = GetCell(map, domain.StrongholdQ, domain.StrongholdR);
                if (cell == null) continue;
                string key = CellKey(cell.Q, cell.R);
                if (distance[key] == 0) continue;
                distance[key] = 0;
                queue.Enqueue(cell);
            }

            // Мультистартовый BFS дешевле, чем считать расстояние от каждого кандидата
            // до всех поселений и крепостей отдельно.
            while (queue.Count > 0)
            {
                HexCellRecord current = queue.Dequeue();
                int currentDistance = distance[CellKey(current.Q, current.R)];
                foreach (HexCellRecord neighbor in GetNeighbors(map, current))
                {
                    string key = CellKey(neighbor.Q, neighbor.R);
                    if (distance[key] <= currentDistance + 1) continue;
                    distance[key] = currentDistance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return distance;
        }

        private HashSet<string> BuildDomainCellSet(HexMapRecord map)
        {
            HashSet<string> result = new HashSet<string>();
            if (map == null || map.Domains == null) return result;
            foreach (DomainRecord domain in map.Domains)
            {
                if (domain == null) continue;
                foreach (DomainHexRecord hex in domain.Hexes ?? new List<DomainHexRecord>())
                {
                    if (hex == null) continue;
                    result.Add(CellKey(hex.Q, hex.R));
                }

                if (domain.StrongholdQ >= 0 && domain.StrongholdR >= 0)
                {
                    result.Add(CellKey(domain.StrongholdQ, domain.StrongholdR));
                }
            }

            return result;
        }

        private HashSet<string> BuildRiverCellSet(HexMapRecord map)
        {
            HashSet<string> result = new HashSet<string>();
            if (map == null || map.Rivers == null) return result;
            foreach (MapEdgeRecord river in map.Rivers)
            {
                if (river == null) continue;
                result.Add(CellKey(river.AQ, river.AR));
                result.Add(CellKey(river.BQ, river.BR));
            }

            return result;
        }

        private int DistanceFromLookup(Dictionary<string, int> distance, HexCellRecord cell)
        {
            if (cell == null || distance == null) return 12;
            int value;
            if (!distance.TryGetValue(CellKey(cell.Q, cell.R), out value) || value == int.MaxValue) return 12;
            return value;
        }

        private bool IsNearUsedFeature(HexMapRecord map, HexCellRecord cell, HashSet<string> usedCells, int radius)
        {
            if (radius <= 0) return false;
            foreach (string key in usedCells)
            {
                HexCellRecord used = GetCell(map, key);
                if (used != null && HexDistance(cell.Q, cell.R, used.Q, used.R) <= radius) return true;
            }

            return false;
        }

        private HexCellRecord PickWeightedHexCell(List<WeightedHexCell> candidates, Random random)
        {
            if (candidates == null || candidates.Count == 0) return null;
            double total = candidates.Sum(c => c.Weight);
            if (total <= 0.0) return candidates[random.Next(candidates.Count)].Cell;

            double roll = random.NextDouble() * total;
            foreach (WeightedHexCell candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll <= 0.0) return candidate.Cell;
            }

            return candidates[candidates.Count - 1].Cell;
        }

        private sealed class WeightedHexCell
        {
            public HexCellRecord Cell { get; private set; }
            public double Weight { get; private set; }

            public WeightedHexCell(HexCellRecord cell, double weight)
            {
                Cell = cell;
                Weight = weight;
            }
        }

        private int PickDungeonRecommendedLevel(int nearestSettlementDistance, bool inSettlement, Random random)
        {
            if (inSettlement) return random.Next(1, 4);
            if (nearestSettlementDistance <= 3) return random.Next(1, 5);
            if (nearestSettlementDistance <= 7) return random.Next(2, DungeonCatalog.MaxDungeonLevel + 1);
            int roll = random.Next(100);
            if (roll < 55) return random.Next(3, DungeonCatalog.MaxDungeonLevel + 1);
            if (roll < 88) return random.Next(4, DungeonCatalog.MaxDungeonLevel + 1);
            return DungeonCatalog.MaxDungeonLevel;
        }

        private string PickDungeonSize(Random random, RegionGenerationOptions options)
        {
            int roll = random.Next(100);
            bool civilized = options != null && string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase);
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            if (roll < (wild ? 16 : 10)) return "Lair";
            if (roll < (wild ? 46 : 35)) return "Small";
            if (roll < (civilized ? 88 : 90)) return "Standard";
            return "Large";
        }

        private string BuildDungeonFeatureDescription(DungeonRecord dungeon, MapSettlementRecord settlement, int distance, bool russian)
        {
            if (dungeon == null) return "";
            if (russian)
            {
                string place = settlement == null
                    ? "до ближайшего поселения: " + distance + " гекс."
                    : "находится в поселении " + settlement.Name + ".";
                return "Данж ACKS: " + DungeonCatalog.LocalizeDungeonType(dungeon.DungeonType, true)
                    + ", размер " + dungeon.Size + ", уровень опасности " + dungeon.RecommendedLevel + "; " + place;
            }

            return "ACKS dungeon: " + dungeon.DungeonType + ", " + dungeon.Size
                + ", danger level " + dungeon.RecommendedLevel
                + (settlement == null ? "; nearest settlement " + distance + " hexes." : "; inside " + settlement.Name + ".");
        }
    }
}
