using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        // Поселения выбираются отдельно от доменов и дорог: эта фаза решает только где стоят рынки и кланхолды.
        private void GenerateSettlements(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            bool seedSpecialDomains = ShouldSeedSpecialDomainSettlements(options);
            if (options.SettlementDensityPercent <= 0 && !seedSpecialDomains) return;

            // Поселения ставятся только в пригодные гексы; скоринг предпочитает равнины,
            // луга/степи и расстояние от уже созданных городов.
            List<HexCellRecord> candidates = map.Cells.Where(IsSettlementEligible).OrderBy(c => random.Next()).ToList();
            if (candidates.Count == 0) return;

            int area = candidates.Count;
            bool wild = string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            int count = 0;
            if (options.SettlementDensityPercent > 0)
            {
                count = wild
                    ? Math.Max(1, area * options.SettlementDensityPercent / 7000)
                    : Math.Max(1, area * options.SettlementDensityPercent / 500);
                if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) count = Math.Max(count + 2, count * 4 / 3);
                count = Math.Min(count, Math.Max(1, candidates.Count / 4));
            }

            int borderlandsCoreSide = string.Equals(options.CivilizationLevel, "Borderlands", StringComparison.OrdinalIgnoreCase)
                ? random.Next(4)
                : -1;
            List<Point> wildCivilizedCenters = wild
                ? BuildWildCivilizedClusterCenters(candidates, map, random)
                : new List<Point>();
            int wildClusterBudget = WildClusterSettlementBudget(count, wildCivilizedCenters.Count, random);
            Dictionary<string, int> nearestSettlementDistance = new Dictionary<string, int>();
            foreach (HexCellRecord candidate in candidates)
            {
                nearestSettlementDistance[CellKey(candidate.Q, candidate.R)] = int.MaxValue;
            }

            int reservedSpecialSettlements = AddPrioritySpecialDomainSettlements(
                map,
                options,
                random,
                candidates,
                nearestSettlementDistance,
                borderlandsCoreSide);
            count = Math.Max(0, count - reservedSpecialSettlements);

            for (int i = 0; i < count; i++)
            {
                CheckCancellation();
                bool preferWildCluster = wild && i < wildClusterBudget;
                HexCellRecord cell = PickSettlementCell(
                    candidates,
                    map,
                    map.Settlements,
                    nearestSettlementDistance,
                    borderlandsCoreSide,
                    options,
                    random,
                    wildCivilizedCenters,
                    preferWildCluster);
                if (cell == null) break;

                int marketClass = PickMarketClass(random, options, i == 0);
                MapSettlementRecord settlement = new MapSettlementRecord
                {
                    Name = GenerateUniqueSettlementName(random, options.CultureKey),
                    MarketClass = marketClass,
                    Q = cell.Q,
                    R = cell.R,
                    Race = "Human",
                    LandValue = "Generated"
                };
                map.Settlements.Add(settlement);
                candidates.RemoveAll(c => HexDistance(c.Q, c.R, cell.Q, cell.R) < (marketClass <= 3 ? 4 : 2));
                UpdateNearestSettlementDistances(candidates, nearestSettlementDistance, cell);
            }

            AddRemoteClanholdSettlements(map, options, random, borderlandsCoreSide);
            AddRemoteSpecialSettlementsWithoutDomains(map, options, random, borderlandsCoreSide);
        }

        private bool ShouldSeedSpecialDomainSettlements(RegionGenerationOptions options)
        {
            return options != null
                && options.GenerateSettlements
                && options.GenerateDomains
                && options.GenerateSpecialDomains
                && (options.GenerateDwarvenDomains || options.GenerateElvenDomains || options.GenerateClanDomains);
        }

        private int AddPrioritySpecialDomainSettlements(
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            List<HexCellRecord> candidates,
            Dictionary<string, int> nearestSettlementDistance,
            int borderlandsCoreSide)
        {
            if (!ShouldSeedSpecialDomainSettlements(options) || map == null || random == null || candidates == null) return 0;

            int placed = 0;
            // Особые домены резервируются до обычных людей: иначе цивилизованный режим
            // успевает занять все хорошие леса, горы и удалённые фронтирные клетки.
            if (options.GenerateDwarvenDomains)
            {
                placed += AddSeededSpecialDomainSettlements(map, options, random, candidates, nearestSettlementDistance, borderlandsCoreSide, "Dwarf");
            }

            if (options.GenerateElvenDomains)
            {
                placed += AddSeededSpecialDomainSettlements(map, options, random, candidates, nearestSettlementDistance, borderlandsCoreSide, "Elf");
            }

            if (options.GenerateClanDomains)
            {
                placed += AddSeededSpecialDomainSettlements(map, options, random, candidates, nearestSettlementDistance, borderlandsCoreSide, "Clanhold");
            }

            return placed;
        }

        private int AddSeededSpecialDomainSettlements(
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            List<HexCellRecord> candidates,
            Dictionary<string, int> nearestSettlementDistance,
            int borderlandsCoreSide,
            string kind)
        {
            if (candidates.Count == 0) return 0;

            int target = SpecialDomainSeedTarget(map, candidates.Count, options, kind);
            int spacing = SpecialDomainSeedSpacing(map, options, kind);
            List<Point> placed = new List<Point>();
            int placedCount = 0;

            for (int i = 0; i < target && candidates.Count > 0; i++)
            {
                CheckCancellation();
                HexCellRecord cell = PickSpecialDomainSeedCell(candidates, map, options, random, borderlandsCoreSide, kind, placed, spacing);
                if (cell == null) break;

                ShapeSpecialDomainZone(map, cell, kind, random);

                string race = SpecialDomainSeedRace(kind, random);
                string culture = SpecialDomainSeedCulture(kind, race, options);
                MapSettlementRecord settlement = new MapSettlementRecord
                {
                    Name = GenerateUniqueSettlementName(random, culture),
                    MarketClass = SpecialDomainSeedMarketClass(options, kind, random),
                    Q = cell.Q,
                    R = cell.R,
                    Race = NormalizeGeneratedRace(race),
                    LandValue = SpecialDomainSeedMarker(kind)
                };
                map.Settlements.Add(settlement);
                placed.Add(new Point(cell.Q, cell.R));
                placedCount++;

                candidates.RemoveAll(c => HexDistance(c.Q, c.R, cell.Q, cell.R) < spacing);
                UpdateNearestSettlementDistances(candidates, nearestSettlementDistance, cell);
            }

            return placedCount;
        }

        private int SpecialDomainSeedTarget(HexMapRecord map, int candidateCount, RegionGenerationOptions options, string kind)
        {
            if (map == null || options == null || candidateCount <= 0) return 0;

            int area = Math.Max(1, map.Width * map.Height);
            bool wild = string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            bool civilized = string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase);
            int weight = SpecialDomainSeedWeight(options, kind);
            if (weight <= 0) return 0;

            int divisor;
            if (string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                divisor = wild ? 26000 : civilized ? 42000 : 30000;
            }
            else
            {
                divisor = wild ? 36000 : civilized ? 30000 : 28000;
            }

            int target = Math.Max(1, area * weight / divisor);
            if (civilized && area >= 3000 && !string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) target++;
            if (!civilized && area >= 5600 && string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) target++;
            if (wild && !string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) target = Math.Min(target, 2);

            int cap = Math.Max(1, candidateCount / (wild ? 80 : 55));
            cap = Math.Min(cap, civilized ? 6 : wild ? 4 : 5);
            return Math.Max(1, Math.Min(target, cap));
        }

        private int SpecialDomainSeedSpacing(HexMapRecord map, RegionGenerationOptions options, string kind)
        {
            int area = map == null ? 1200 : Math.Max(1, map.Width * map.Height);
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            int spacing = (int)Math.Round(Math.Sqrt(area) / (wild ? 7.0 : 8.5));
            if (string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) spacing += wild ? 3 : 2;
            return Math.Max(wild ? 8 : 6, Math.Min(wild ? 18 : 14, spacing));
        }

        private HexCellRecord PickSpecialDomainSeedCell(
            List<HexCellRecord> candidates,
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            int borderlandsCoreSide,
            string kind,
            List<Point> placed,
            int spacing)
        {
            HexCellRecord best = null;
            double bestScore = double.MinValue;
            foreach (HexCellRecord candidate in candidates)
            {
                double score = SpecialDomainSeedScore(candidate, map, options, random, borderlandsCoreSide, kind, placed, spacing);
                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private double SpecialDomainSeedScore(
            HexCellRecord cell,
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            int borderlandsCoreSide,
            string kind,
            List<Point> placed,
            int spacing)
        {
            double score = random.NextDouble() * 4.0;
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase))
            {
                if (cell.Elevation == "Mountains") score += 18.0;
                if (cell.Elevation == "Hills") score += 10.0;
                if (cell.Terrain == "Tundra" || cell.Terrain == "Taiga" || cell.Terrain == "Steppe") score += 2.0;
            }
            else if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase))
            {
                if (cell.Terrain == "Forest" || cell.Terrain == "Rainforest") score += 18.0;
                if (cell.Terrain == "Taiga") score += 12.0;
                if (cell.Terrain == "Grasslands" || cell.Terrain == "Scrub") score += 3.0;
                if (cell.Elevation == "Mountains") score -= 2.0;
            }
            else
            {
                if (cell.Terrain == "Scrub" || cell.Terrain == "Steppe" || cell.Terrain == "Tundra" || cell.Terrain == "Desert") score += 8.0;
                if (cell.Terrain == "Forest" || cell.Terrain == "Taiga" || cell.Elevation != "Plains") score += 3.0;
                int nearest = map.Settlements
                    .Select(s => HexDistance(cell.Q, cell.R, s.Q, s.R))
                    .DefaultIfEmpty(wild ? 18 : 12)
                    .Min();
                score += Math.Min(wild ? 18 : 12, nearest) * 0.85;
            }

            if (borderlandsCoreSide >= 0)
            {
                int distance = DistanceFromSide(cell, map, borderlandsCoreSide);
                int maxDistance = Math.Max(1, SideLength(map, borderlandsCoreSide));
                double frontier = distance / (double)maxDistance;
                score += string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)
                    ? frontier * 12.0
                    : frontier * 4.0;
            }

            if (placed != null && placed.Count > 0)
            {
                int nearestPlaced = placed
                    .Select(p => HexDistance(cell.Q, cell.R, p.X, p.Y))
                    .DefaultIfEmpty(spacing)
                    .Min();
                if (nearestPlaced < spacing) score -= (spacing - nearestPlaced) * 6.0;
                score += Math.Min(spacing + 4, nearestPlaced) * 0.35;
            }

            return score;
        }

        private void ShapeSpecialDomainZone(HexMapRecord map, HexCellRecord center, string kind, Random random)
        {
            if (map == null || center == null || random == null) return;

            int radius = string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            foreach (HexCellRecord cell in map.Cells.Where(c => c.Water == "None" && HexDistance(c.Q, c.R, center.Q, center.R) <= radius))
            {
                int distance = HexDistance(cell.Q, cell.R, center.Q, center.R);
                if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase))
                {
                    if (distance == 0)
                    {
                        cell.Elevation = "Mountains";
                    }
                    else if (distance <= 1 || random.Next(100) < 55)
                    {
                        cell.Elevation = random.Next(100) < 45 ? "Mountains" : "Hills";
                    }
                }
                else if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase))
                {
                    string forest = string.Equals(cell.Terrain, "Taiga", StringComparison.OrdinalIgnoreCase) ? "Taiga" : "Forest";
                    if (distance == 0 || random.Next(100) < 78)
                    {
                        cell.Terrain = forest;
                    }
                    else if (distance >= 2 && random.Next(100) < 40)
                    {
                        cell.Terrain = "DeepForest";
                    }
                }
                else
                {
                    if (distance == 0) continue;
                    if (random.Next(100) < 45)
                    {
                        cell.Terrain = Pick(random, new[] { "Scrub", "Steppe", "Forest" });
                    }
                }
            }
        }

        private int SpecialDomainSeedWeight(RegionGenerationOptions options, string kind)
        {
            if (options == null) return 0;
            if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase)) return options.DwarvenDomainWeight;
            if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase)) return options.ElvenDomainWeight;
            if (string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) return options.ClanDomainWeight;
            return 0;
        }

        private string SpecialDomainSeedRace(string kind, Random random)
        {
            if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase)) return "Dwarf";
            if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase)) return "Elf";
            return PickClanholdRace(random);
        }

        private string SpecialDomainSeedCulture(string kind, string race, RegionGenerationOptions options)
        {
            string fallback = options == null || string.IsNullOrWhiteSpace(options.CultureKey) ? "english" : options.CultureKey;
            if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase)) return options != null && options.UseDwarvenCultureNames ? "dwarf" : fallback;
            if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase)) return options != null && options.UseElvenCultureNames ? "elf" : fallback;
            return options != null && options.UseClanCultureNames ? CultureForClanhold(race, fallback) : fallback;
        }

        private int SpecialDomainSeedMarketClass(RegionGenerationOptions options, string kind, Random random)
        {
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            bool civilized = options != null && string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(kind, "Clanhold", StringComparison.OrdinalIgnoreCase)) return random.Next(100) < (wild ? 8 : 18) ? 5 : 6;
            if (wild) return random.Next(100) < 35 ? 5 : 6;
            if (civilized) return random.Next(100) < 10 ? 2 : random.Next(100) < 48 ? 3 : 4;
            return random.Next(100) < 18 ? 3 : random.Next(100) < 58 ? 4 : 5;
        }

        private string SpecialDomainSeedMarker(string kind)
        {
            if (string.Equals(kind, "Dwarf", StringComparison.OrdinalIgnoreCase)) return DwarvenDomainSeedMarker;
            if (string.Equals(kind, "Elf", StringComparison.OrdinalIgnoreCase)) return ElvenDomainSeedMarker;
            return ClanholdSeedMarker;
        }

        private List<Point> BuildWildCivilizedClusterCenters(List<HexCellRecord> candidates, HexMapRecord map, Random random)
        {
            List<Point> result = new List<Point>();
            if (candidates == null || map == null || random == null) return result;

            int area = Math.Max(1, map.Width * map.Height);
            if (area < 2400) return result;

            int target = Math.Max(1, area / 5200);
            if (area >= 5600 && random.Next(100) < 45) target++;
            target = Math.Min(3, target);
            int spacing = Math.Max(12, Math.Min(28, (int)Math.Round(Math.Sqrt(area / (double)Math.Max(1, target)) * 0.55)));
            List<HexCellRecord> pool = candidates.ToList();

            for (int i = 0; i < target && pool.Count > 0; i++)
            {
                HexCellRecord best = null;
                double bestScore = double.MinValue;
                foreach (HexCellRecord candidate in pool)
                {
                    double score = WildCivilizedClusterCenterScore(candidate, map, result, spacing, random);
                    if (best == null || score > bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                    }
                }

                if (best == null) break;
                result.Add(new Point(best.Q, best.R));
                pool.RemoveAll(c => HexDistance(c.Q, c.R, best.Q, best.R) < spacing);
            }

            return result;
        }

        private double WildCivilizedClusterCenterScore(
            HexCellRecord cell,
            HexMapRecord map,
            List<Point> centers,
            int spacing,
            Random random)
        {
            double score = random.NextDouble() * 4.0;
            if (cell.Terrain == "Grasslands" || cell.Terrain == "Steppe") score += 4.0;
            if (cell.Terrain == "Forest" || cell.Terrain == "Scrub") score += 2.0;
            if (cell.Elevation == "Plains") score += 2.0;
            score += Math.Min(6, EdgeDistance(cell, map)) * 0.25;

            if (centers != null && centers.Count > 0)
            {
                int nearest = centers.Select(p => HexDistance(cell.Q, cell.R, p.X, p.Y)).Min();
                if (nearest < spacing) score -= (spacing - nearest) * 2.0;
            }

            return score;
        }

        private int WildClusterSettlementBudget(int settlementCount, int clusterCount, Random random)
        {
            if (settlementCount <= 0 || clusterCount <= 0) return 0;

            int desired = 0;
            for (int i = 0; i < clusterCount; i++)
            {
                desired += random.Next(3, 6);
            }

            return Math.Min(settlementCount, desired);
        }

        private void AddRemoteClanholdSettlements(HexMapRecord map, RegionGenerationOptions options, Random random, int borderlandsCoreSide)
        {
            if (map == null || options == null || random == null) return;
            if (!options.GenerateClanDomains) return;
            if (!options.GenerateDomains && !options.GenerateSpecialSettlementsWithoutDomains) return;
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) return;

            HashSet<string> occupied = new HashSet<string>(map.Settlements.Select(s => CellKey(s.Q, s.R)));
            List<HexCellRecord> candidates = map.Cells
                .Where(IsSettlementEligible)
                .Where(c => !occupied.Contains(CellKey(c.Q, c.R)))
                .Where(c => IsRemoteFromSettlements(map, c, string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase) ? 10 : 8))
                .ToList();
            if (candidates.Count == 0) return;

            int target = RemoteClanholdTarget(map, candidates.Count, options);
            int spacing = RemoteClanholdSpacing(map, target);
            List<Point> placed = new List<Point>();

            for (int i = 0; i < target; i++)
            {
                CheckCancellation();
                HexCellRecord cell = null;
                double bestScore = double.MinValue;
                foreach (HexCellRecord candidate in candidates)
                {
                    double score = ClanholdSettlementScore(candidate, map, borderlandsCoreSide, options, random, placed, spacing);
                    if (cell == null || score > bestScore)
                    {
                        cell = candidate;
                        bestScore = score;
                    }
                }

                if (cell == null) break;

                string race = PickClanholdRace(random);
                string culture = options.UseClanCultureNames ? CultureForRace(race, options.CultureKey) : options.CultureKey;
                MapSettlementRecord settlement = new MapSettlementRecord
                {
                    Name = GenerateUniqueSettlementName(random, culture),
                    MarketClass = random.Next(100) < 18 ? 5 : 6,
                    Q = cell.Q,
                    R = cell.R,
                    Race = NormalizeGeneratedRace(race),
                    LandValue = ClanholdSeedMarker
                };
                map.Settlements.Add(settlement);
                placed.Add(new Point(cell.Q, cell.R));
                candidates.RemoveAll(c => HexDistance(c.Q, c.R, cell.Q, cell.R) < spacing);
            }
        }

        private int RemoteClanholdTarget(HexMapRecord map, int candidateCount, RegionGenerationOptions options)
        {
            if (map == null || options == null || candidateCount <= 0) return 0;

            int area = Math.Max(1, map.Width * map.Height);
            int weight = Math.Max(5, options.ClanDomainWeight);
            bool wild = string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);

            // Кланхолды нужны как редкие, но заметные удалённые очаги цивилизации.
            // Старая формула почти всегда давала 1 на больших картах пограничья,
            // потому что считала только от уже отфильтрованных кандидатов.
            int byArea = area * weight / (wild ? 30000 : 32000);
            int byCandidates = candidateCount * weight / (wild ? 6200 : 6800);
            int target = Math.Max(byArea, byCandidates);

            if (area >= 2500) target = Math.Max(target, wild ? 3 : 2);
            if (area >= 5600) target = Math.Max(target, wild ? 7 : 6);
            if (wild) target++;

            int candidateCap = Math.Max(1, candidateCount / 45);
            int areaCap = Math.Max(3, area / 520);
            target = Math.Min(target, Math.Min(candidateCap, areaCap));
            return Math.Min(target, 14);
        }

        private int RemoteClanholdSpacing(HexMapRecord map, int target)
        {
            if (map == null || target <= 1) return 7;

            int area = Math.Max(1, map.Width * map.Height);
            int spacing = (int)Math.Round(Math.Sqrt(area / (double)Math.Max(1, target)) * 0.45);
            return Math.Max(7, Math.Min(18, spacing));
        }

        private void AddRemoteSpecialSettlementsWithoutDomains(HexMapRecord map, RegionGenerationOptions options, Random random, int borderlandsCoreSide)
        {
            if (map == null || options == null || random == null) return;
            if (!options.GenerateSpecialSettlementsWithoutDomains || options.GenerateDomains) return;

            if (options.GenerateDwarvenDomains)
            {
                AddRemoteRaceSettlements(map, options, random, borderlandsCoreSide, "Dwarf");
            }

            if (options.GenerateElvenDomains)
            {
                AddRemoteRaceSettlements(map, options, random, borderlandsCoreSide, "Elf");
            }
        }

        private void AddRemoteRaceSettlements(
            HexMapRecord map,
            RegionGenerationOptions options,
            Random random,
            int borderlandsCoreSide,
            string race)
        {
            HashSet<string> occupied = new HashSet<string>(map.Settlements.Select(s => CellKey(s.Q, s.R)));
            int minDistance = string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase) ? 5 : 7;
            List<HexCellRecord> candidates = map.Cells
                .Where(IsSettlementEligible)
                .Where(c => !occupied.Contains(CellKey(c.Q, c.R)))
                .Where(c => IsRemoteFromSettlements(map, c, minDistance))
                .ToList();
            if (candidates.Count == 0) return;

            int weight = string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase)
                ? options.DwarvenDomainWeight
                : options.ElvenDomainWeight;
            int target = Math.Max(1, candidates.Count * Math.Max(5, weight) / 42000);
            target = Math.Min(target, Math.Max(1, candidates.Count / 110));
            target = Math.Min(target, 4);

            for (int i = 0; i < target; i++)
            {
                CheckCancellation();
                HexCellRecord cell = PickSpecialRaceSettlementCell(candidates, map, borderlandsCoreSide, race, random);
                if (cell == null) break;

                string culture = string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase)
                    ? (options.UseDwarvenCultureNames ? "dwarf" : options.CultureKey)
                    : (options.UseElvenCultureNames ? "elf" : options.CultureKey);
                MapSettlementRecord settlement = new MapSettlementRecord
                {
                    Name = GenerateUniqueSettlementName(random, culture),
                    MarketClass = random.Next(100) < 12 ? 4 : random.Next(100) < 45 ? 5 : 6,
                    Q = cell.Q,
                    R = cell.R,
                    Race = race,
                    LandValue = "Generated " + race
                };
                map.Settlements.Add(settlement);
                candidates.RemoveAll(c => HexDistance(c.Q, c.R, cell.Q, cell.R) < minDistance);
            }
        }

        private HexCellRecord PickSpecialRaceSettlementCell(
            List<HexCellRecord> candidates,
            HexMapRecord map,
            int borderlandsCoreSide,
            string race,
            Random random)
        {
            HexCellRecord best = null;
            double bestScore = double.MinValue;
            foreach (HexCellRecord candidate in candidates)
            {
                double score = random.NextDouble() * 3.0;
                if (string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase))
                {
                    if (candidate.Elevation == "Mountains") score += 8.0;
                    if (candidate.Elevation == "Hills") score += 4.0;
                }
                else
                {
                    if (candidate.Terrain == "Forest" || candidate.Terrain == "Taiga" || candidate.Terrain == "Rainforest") score += 7.0;
                    if (candidate.Terrain == "DeepForest" || candidate.Terrain == "DeepTaiga") score += 4.0;
                }

                if (borderlandsCoreSide >= 0)
                {
                    int distance = DistanceFromSide(candidate, map, borderlandsCoreSide);
                    int maxDistance = Math.Max(1, SideLength(map, borderlandsCoreSide));
                    score += distance / (double)maxDistance * 6.0;
                }

                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private double ClanholdSettlementScore(
            HexCellRecord cell,
            HexMapRecord map,
            int borderlandsCoreSide,
            RegionGenerationOptions options,
            Random random,
            List<Point> placedClanholds,
            int spacing)
        {
            double score = random.NextDouble() * 3.0;
            if (cell.Terrain == "Scrub" || cell.Terrain == "Steppe" || cell.Terrain == "Tundra" || cell.Terrain == "Desert") score += 2.0;
            if (cell.Terrain == "Forest" || cell.Terrain == "Taiga" || cell.Elevation != "Plains") score += 1.2;

            if (borderlandsCoreSide >= 0)
            {
                int distance = DistanceFromSide(cell, map, borderlandsCoreSide);
                int maxDistance = Math.Max(1, SideLength(map, borderlandsCoreSide));
                double frontier = distance / (double)maxDistance;
                score += frontier * 9.0;
                if (frontier < 0.58) score -= 12.0;
            }

            int nearestMajor = map.Settlements
                .Where(s => s.MarketClass <= 4)
                .Select(s => HexDistance(cell.Q, cell.R, s.Q, s.R))
                .DefaultIfEmpty(18)
                .Min();
            score += Math.Min(8, nearestMajor);

            if (placedClanholds != null && placedClanholds.Count > 0)
            {
                int nearestClanhold = placedClanholds
                    .Select(p => HexDistance(cell.Q, cell.R, p.X, p.Y))
                    .DefaultIfEmpty(spacing)
                    .Min();
                score += Math.Min(16, nearestClanhold) * 0.85;
            }

            return score;
        }

        private bool IsRemoteFromMajorSettlements(HexMapRecord map, HexCellRecord cell, int minDistance)
        {
            if (map == null || cell == null) return false;
            return map.Settlements
                .Where(s => s.MarketClass <= 4)
                .All(s => HexDistance(cell.Q, cell.R, s.Q, s.R) >= minDistance);
        }

        private bool IsRemoteFromSettlements(HexMapRecord map, HexCellRecord cell, int minDistance)
        {
            if (map == null || cell == null) return false;
            return map.Settlements
                .All(s => HexDistance(cell.Q, cell.R, s.Q, s.R) >= minDistance);
        }

        private bool IsPreseededClanholdSettlement(MapSettlementRecord settlement)
        {
            return settlement != null
                && string.Equals(settlement.LandValue, ClanholdSeedMarker, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreseededDwarvenDomainSettlement(MapSettlementRecord settlement)
        {
            return settlement != null
                && string.Equals(settlement.LandValue, DwarvenDomainSeedMarker, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreseededElvenDomainSettlement(MapSettlementRecord settlement)
        {
            return settlement != null
                && string.Equals(settlement.LandValue, ElvenDomainSeedMarker, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreseededSpecialDomainSettlement(MapSettlementRecord settlement)
        {
            return IsPreseededClanholdSettlement(settlement)
                || IsPreseededDwarvenDomainSettlement(settlement)
                || IsPreseededElvenDomainSettlement(settlement);
        }


        private HexCellRecord PickSettlementCell(
            List<HexCellRecord> candidates,
            HexMapRecord map,
            List<MapSettlementRecord> settlements,
            Dictionary<string, int> nearestSettlementDistance,
            int borderlandsCoreSide,
            RegionGenerationOptions options,
            Random random,
            List<Point> wildCivilizedCenters,
            bool preferWildCluster)
        {
            HexCellRecord best = null;
            double bestScore = double.MinValue;
            foreach (HexCellRecord candidate in candidates)
            {
                double score = SettlementScore(
                    candidate,
                    map,
                    settlements,
                    nearestSettlementDistance,
                    borderlandsCoreSide,
                    options,
                    random,
                    wildCivilizedCenters,
                    preferWildCluster);
                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private double SettlementScore(
            HexCellRecord cell,
            HexMapRecord map,
            List<MapSettlementRecord> settlements,
            Dictionary<string, int> nearestSettlementDistance,
            int borderlandsCoreSide,
            RegionGenerationOptions options,
            Random random,
            List<Point> wildCivilizedCenters,
            bool preferWildCluster)
        {
            double score = random.NextDouble() * 4;
            if (cell.Elevation == "Plains") score += 2;
            if (cell.Elevation == "Mountains") score -= 4;
            else if (cell.Elevation == "Hills") score -= 0.5;
            if (cell.Terrain == "Grasslands" || cell.Terrain == "Steppe") score += 2;
            if (cell.Terrain == "Forest" || cell.Terrain == "Scrub") score += 1;
            if (options != null && options.GenerateElvenDomains && (cell.Terrain == "Forest" || cell.Terrain == "Taiga" || cell.Terrain == "Rainforest")) score += 2.6;
            if (options != null && options.GenerateDwarvenDomains && (cell.Elevation == "Mountains" || cell.Elevation == "Hills")) score += 2.1;
            if (cell.Water == "None") score += 1;
            if (borderlandsCoreSide >= 0)
            {
                // В пограничье поселения тяготеют к одной освоенной стороне карты:
                // дальше от нее плотность падает, и фронтир остается более пустым.
                int distance = DistanceFromSide(cell, map, borderlandsCoreSide);
                int maxDistance = Math.Max(1, SideLength(map, borderlandsCoreSide));
                double frontier = distance / (double)maxDistance;
                score += Math.Max(0, 8.0 - frontier * 11.0);
                if (frontier > 0.62) score -= 3.0;
            }

            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            if (wild && wildCivilizedCenters != null && wildCivilizedCenters.Count > 0)
            {
                int nearestCluster = wildCivilizedCenters
                    .Select(p => HexDistance(cell.Q, cell.R, p.X, p.Y))
                    .DefaultIfEmpty(99)
                    .Min();
                if (preferWildCluster)
                {
                    score += Math.Max(0.0, 22.0 - nearestCluster * 3.5);
                    if (nearestCluster > 6) score -= 16.0;
                }
                else
                {
                    if (nearestCluster < 7) score -= (7 - nearestCluster) * 2.0;
                    score += Math.Min(8, nearestCluster) * 0.35;
                }
            }

            if (settlements.Count > 0)
            {
                int nearest;
                if (nearestSettlementDistance == null || !nearestSettlementDistance.TryGetValue(CellKey(cell.Q, cell.R), out nearest))
                {
                    nearest = settlements.Min(s => HexDistance(cell.Q, cell.R, s.Q, s.R));
                }

                score += wild && preferWildCluster
                    ? Math.Min(4, nearest) * 0.35
                    : Math.Min(6, nearest);
            }

            return score;
        }

        private void UpdateNearestSettlementDistances(
            List<HexCellRecord> candidates,
            Dictionary<string, int> nearestSettlementDistance,
            HexCellRecord settlementCell)
        {
            if (candidates == null || nearestSettlementDistance == null || settlementCell == null) return;

            // Кеш хранит ближайшее расстояние до уже поставленных поселений.
            // Это та же величина, которую раньше каждый кандидат пересчитывал через Min().
            foreach (HexCellRecord candidate in candidates)
            {
                string key = CellKey(candidate.Q, candidate.R);
                int existing;
                if (!nearestSettlementDistance.TryGetValue(key, out existing))
                {
                    existing = int.MaxValue;
                }

                int distance = HexDistance(candidate.Q, candidate.R, settlementCell.Q, settlementCell.R);
                if (distance < existing) nearestSettlementDistance[key] = distance;
            }
        }

        private bool IsSettlementEligible(HexCellRecord cell)
        {
            if (cell == null || cell.Water != "None") return false;
            return cell.Terrain != "Marsh" && cell.Terrain != "DeepForest" && cell.Terrain != "DeepTaiga";
        }

        private int PickMarketClass(Random random, RegionGenerationOptions options, bool capital)
        {
            if (capital)
            {
                if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) return random.Next(100) < 30 ? 2 : 3;
                if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase)) return random.Next(100) < 30 ? 5 : 6;
                return random.Next(100) < 35 ? 4 : 5;
            }

            int roll = random.Next(100);
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase))
            {
                if (roll < 2) return 1;
                if (roll < 6) return 2;
                if (roll < 16) return 3;
                if (roll < 38) return 4;
                if (roll < 70) return 5;
                return 6;
            }

            if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase))
            {
                if (roll < 4) return 4;
                if (roll < 20) return 5;
                return 6;
            }

            if (roll < 1) return 2;
            if (roll < 4) return 3;
            if (roll < 14) return 4;
            if (roll < 46) return 5;
            return 6;
        }
    }
}
