using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        private const string StrongholdRoadNodePrefix = "__stronghold_road_node__:";

        // Дорожный слой строит связи между уже созданными поселениями и доменами.
        private void GenerateRoads(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            // Дороги соединяют рынки кратчайшим сухопутным путём и далее используют уже существующий
            // механизм карты для расчёта trade routes.
            List<MapSettlementRecord> roadNodes = BuildRoadNodes(map, options);
            if (roadNodes.Count < 2) return;
            HashSet<string> roadNodeCells = BuildRoadNodeCellIndex(roadNodes);

            // Строим минимальную связную сеть поселений: каждый город получает
            // один лучший путь к уже подключенной части, без дублей и раздвоений.
            HashSet<string> added = new HashSet<string>();
            HashSet<string> roadCells = BuildRoadCellIndex(map);
            Dictionary<string, DomainRecord> domainBySettlementId = BuildDomainBySettlementId(map);
            Dictionary<string, int> components = BuildLandComponentIndex(map);
            foreach (IGrouping<int, MapSettlementRecord> group in roadNodes
                .Where(s => components.ContainsKey(CellKey(s.Q, s.R)))
                .GroupBy(s => components[CellKey(s.Q, s.R)]))
            {
                GenerateRoadComponent(map, group.ToList(), options, random, added, roadCells, domainBySettlementId);
            }

            RemoveRoadCycles(map);
            RemoveDanglingRoadStubs(map, roadNodeCells);
        }

        private List<MapSettlementRecord> BuildRoadNodes(HexMapRecord map, RegionGenerationOptions options)
        {
            List<MapSettlementRecord> result = new List<MapSettlementRecord>();
            if (map == null) return result;

            HashSet<string> occupiedCells = new HashSet<string>();
            if (map.Settlements != null)
            {
                foreach (MapSettlementRecord settlement in map.Settlements.Where(s => s != null))
                {
                    result.Add(settlement);
                    occupiedCells.Add(CellKey(settlement.Q, settlement.R));
                }
            }

            if (options != null && !options.GenerateStrongholds) return result;
            if (map.Domains == null) return result;
            HashSet<string> settlementIds = new HashSet<string>(
                map.Settlements == null
                    ? Enumerable.Empty<string>()
                    : map.Settlements.Where(s => s != null).Select(s => s.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (DomainRecord domain in map.Domains.Where(HasRoadStronghold))
            {
                // Если у домена уже есть столица-поселение, дорожная сеть строится к ней.
                // Отдельная крепость становится узлом только для карт без поселения в этом домене.
                if (!string.IsNullOrWhiteSpace(domain.CapitalSettlementId)
                    && settlementIds.Contains(domain.CapitalSettlementId))
                {
                    continue;
                }

                string cellKey = CellKey(domain.StrongholdQ, domain.StrongholdR);
                if (occupiedCells.Contains(cellKey)) continue;

                result.Add(new MapSettlementRecord
                {
                    Id = StrongholdRoadNodeId(domain),
                    Name = string.IsNullOrWhiteSpace(domain.StrongholdName) ? domain.Name : domain.StrongholdName,
                    MarketClass = 6,
                    Q = domain.StrongholdQ,
                    R = domain.StrongholdR,
                    Race = NormalizeGeneratedRace(domain.Race),
                    LandValue = "Stronghold road node"
                });
                occupiedCells.Add(cellKey);
            }

            return result;
        }

        private bool HasRoadStronghold(DomainRecord domain)
        {
            if (domain == null) return false;
            if (domain.StrongholdQ < 0 || domain.StrongholdR < 0) return false;
            return domain.StrongholdSecuresDomain
                || domain.StrongholdActsAsMarketClassVI
                || !string.IsNullOrWhiteSpace(domain.StrongholdId)
                || !string.IsNullOrWhiteSpace(domain.StrongholdName);
        }

        private string StrongholdRoadNodeId(DomainRecord domain)
        {
            if (domain == null) return StrongholdRoadNodePrefix;
            string id = string.IsNullOrWhiteSpace(domain.StrongholdId) ? domain.Id : domain.StrongholdId;
            return StrongholdRoadNodePrefix + id;
        }

        private HashSet<string> BuildRoadNodeCellIndex(IEnumerable<MapSettlementRecord> roadNodes)
        {
            HashSet<string> result = new HashSet<string>();
            if (roadNodes == null) return result;

            foreach (MapSettlementRecord node in roadNodes.Where(n => n != null))
            {
                result.Add(CellKey(node.Q, node.R));
            }

            return result;
        }

        private Dictionary<string, int> BuildLandComponentIndex(HexMapRecord map)
        {
            Dictionary<string, int> components = new Dictionary<string, int>();
            int componentId = 0;
            foreach (HexCellRecord start in map.Cells.Where(c => c.Water == "None"))
            {
                string startKey = CellKey(start.Q, start.R);
                if (components.ContainsKey(startKey)) continue;

                componentId++;
                Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
                queue.Enqueue(start);
                components[startKey] = componentId;

            while (queue.Count > 0)
            {
                CheckCancellation();
                HexCellRecord cell = queue.Dequeue();
                foreach (HexCellRecord next in GetNeighbors(map, cell))
                    {
                        if (next.Water != "None") continue;
                        string nextKey = CellKey(next.Q, next.R);
                        if (components.ContainsKey(nextKey)) continue;

                        components[nextKey] = componentId;
                        queue.Enqueue(next);
                    }
                }
            }

            return components;
        }

        private void GenerateRoadComponent(
            HexMapRecord map,
            List<MapSettlementRecord> settlements,
            RegionGenerationOptions options,
            Random random,
            HashSet<string> added,
            HashSet<string> roadCells,
            Dictionary<string, DomainRecord> domainBySettlementId)
        {
            if (settlements == null || settlements.Count < 2) return;

            List<MapSettlementRecord> connected = new List<MapSettlementRecord>();
            List<MapSettlementRecord> remaining = settlements
                .OrderBy(s => s.MarketClass)
                .ThenBy(s => random.Next())
                .ToList();

            connected.Add(remaining[0]);
            remaining.RemoveAt(0);

            HashSet<string> rejectedParallelPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int targetSearchLimit = RoadTargetSearchLimit(options);
            while (remaining.Count > 0)
            {
                CheckCancellation();
                RoadConnectionCandidate best = null;
                foreach (MapSettlementRecord source in remaining)
                {
                    IEnumerable<MapSettlementRecord> targetCandidates = connected;
                    if (connected.Count > targetSearchLimit)
                    {
                        targetCandidates = connected
                            .OrderBy(t => HexDistance(source.Q, source.R, t.Q, t.R))
                            .Take(targetSearchLimit);
                    }

                    foreach (MapSettlementRecord target in targetCandidates)
                    {
                        if (rejectedParallelPairs.Contains(RoadPairKey(source, target))) continue;
                        if (!ShouldGenerateRoadBetween(map, source, target, options, random, domainBySettlementId)) continue;
                        double score = HexDistance(source.Q, source.R, target.Q, target.R)
                            + source.MarketClass * 0.35
                            + target.MarketClass * 0.15
                            + random.NextDouble() * 0.25;
                        if (best == null || score < best.Score)
                        {
                            best = new RoadConnectionCandidate
                            {
                                From = source,
                                To = target,
                                Score = score
                            };
                        }
                    }
                }

                if (best == null)
                {
                    connected.Add(remaining[0]);
                    remaining.RemoveAt(0);
                    continue;
                }

                List<HexCellRecord> path = FindLandPath(map, best.From.Q, best.From.R, best.To.Q, best.To.R, roadCells);
                if (IsParallelRoadDuplicate(map, path, roadCells))
                {
                    rejectedParallelPairs.Add(RoadPairKey(best.From, best.To));
                    continue;
                }

                if (path.Count >= 2)
                {
                    AddRoadPath(map, path, added, roadCells);
                }
                connected.Add(best.From);
                remaining.Remove(best.From);
            }
        }

        private int RoadTargetSearchLimit(RegionGenerationOptions options)
        {
            if (options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase)) return 12;
            if (options != null && string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) return 32;
            return 22;
        }

        private string RoadPairKey(MapSettlementRecord a, MapSettlementRecord b)
        {
            string left = RoadNodeKey(a);
            string right = RoadNodeKey(b);
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0
                ? left + "|" + right
                : right + "|" + left;
        }

        private string RoadNodeKey(MapSettlementRecord settlement)
        {
            if (settlement == null) return "";
            if (!string.IsNullOrWhiteSpace(settlement.Id)) return settlement.Id;
            return CellKey(settlement.Q, settlement.R);
        }

        private bool ShouldGenerateRoadBetween(
            HexMapRecord map,
            MapSettlementRecord a,
            MapSettlementRecord b,
            RegionGenerationOptions options,
            Random random,
            Dictionary<string, DomainRecord> domainBySettlementId)
        {
            // Особые домены не строят дороги так же охотно, как человеческие рынки:
            // дварфы торгуют чаще, эльфы реже, кланхолды почти изолированы.
            DomainRecord aDomain = GetCapitalDomain(map, a, domainBySettlementId);
            DomainRecord bDomain = GetCapitalDomain(map, b, domainBySettlementId);
            int distance = HexDistance(a.Q, a.R, b.Q, b.R);
            int limit = RoadDistanceLimit(aDomain, bDomain, options, a.MarketClass, b.MarketClass);
            if (distance > limit) return false;

            int chance = RoadPairChance(aDomain, bDomain, options, distance);
            chance = ApplyRoadDistanceFalloff(chance, distance, limit);
            return random.Next(100) < chance;
        }

        private int RoadDistanceLimit(DomainRecord a, DomainRecord b, RegionGenerationOptions options, int aMarketClass, int bMarketClass)
        {
            // Дорога между доменами должна выглядеть как локальная инфраструктура.
            // В диких и пограничных землях генератор лучше оставит отдельные дорожные
            // острова, чем протянет магистраль через десятки пустых гексов.
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            if (IsClanhold(a) || IsClanhold(b)) return wild ? 6 : 10;

            bool aElf = IsDomainType(a, "Elven Fastness");
            bool bElf = IsDomainType(b, "Elven Fastness");
            if (aElf || bElf) return aElf && bElf ? (wild ? 9 : 12) : (wild ? 5 : 8);

            bool aDwarf = IsDomainType(a, "Dwarven Vault");
            bool bDwarf = IsDomainType(b, "Dwarven Vault");
            if (aDwarf || bDwarf) return aDwarf && bDwarf ? (wild ? 12 : 18) : (wild ? 6 : 14);

            int limit;
            if (options != null && string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) limit = 18;
            else if (wild) limit = 6;
            else limit = 12;

            int bestMarket = Math.Min(aMarketClass <= 0 ? 6 : aMarketClass, bMarketClass <= 0 ? 6 : bMarketClass);
            if (bestMarket <= 2) limit += wild ? 3 : 6;
            else if (bestMarket <= 3) limit += wild ? 1 : 3;

            if (IsOutlands(a) || IsOutlands(b)) limit -= 3;
            return Math.Max(wild ? 4 : 5, limit);
        }

        private int ApplyRoadDistanceFalloff(int chance, int distanceHexes, int limit)
        {
            int softLimit = Math.Max(1, limit * 2 / 3);
            if (distanceHexes <= softLimit) return chance;

            int over = distanceHexes - softLimit;
            int penaltyPercent = Math.Min(75, over * 12);
            return Math.Max(1, chance * (100 - penaltyPercent) / 100);
        }

        private int RoadPairChance(DomainRecord a, DomainRecord b, RegionGenerationOptions options, int distanceHexes)
        {
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            if (IsClanhold(a) || IsClanhold(b))
            {
                if (distanceHexes > (wild ? 6 : 10)) return 0;

                if (IsClanhold(a) && IsClanhold(b))
                {
                    string aRace = NormalizeGeneratedRace(a.Race);
                    string bRace = NormalizeGeneratedRace(b.Race);
                    if (aRace == "Orc" || aRace == "Beastman" || bRace == "Orc" || bRace == "Beastman")
                    {
                        return aRace == bRace ? (wild ? 4 : 7) : 0;
                    }

                    return aRace == bRace ? (wild ? 5 : 10) : (wild ? 0 : 3);
                }

                DomainRecord clan = IsClanhold(a) ? a : b;
                DomainRecord other = IsClanhold(a) ? b : a;
                string clanRace = NormalizeGeneratedRace(clan.Race);
                if (wild) return 0;
                if (clanRace == "Orc" || clanRace == "Beastman") return 0;

                return other != null && string.Equals(other.Classification, "Civilized", StringComparison.OrdinalIgnoreCase) ? 8 : 4;
            }

            bool aElf = IsDomainType(a, "Elven Fastness");
            bool bElf = IsDomainType(b, "Elven Fastness");
            if (aElf || bElf)
            {
                return aElf && bElf ? (wild ? 28 : 42) : (wild ? 0 : 9);
            }

            bool aDwarf = IsDomainType(a, "Dwarven Vault");
            bool bDwarf = IsDomainType(b, "Dwarven Vault");
            if (aDwarf || bDwarf)
            {
                return aDwarf && bDwarf ? (wild ? 38 : 76) : (wild ? 3 : 58);
            }

            int chance = Math.Min(RoadAccessChance(a), RoadAccessChance(b));
            if (wild)
            {
                chance = Math.Min(chance, 18);
            }

            if (IsOutlands(a) || IsOutlands(b))
            {
                chance = Math.Min(chance, wild ? 12 : 48);
            }

            return chance;
        }

        private int RoadAccessChance(DomainRecord domain)
        {
            if (domain == null) return 100;
            if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase)) return 72;
            if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase)) return 24;
            if (string.Equals(domain.DomainType, "Transitional", StringComparison.OrdinalIgnoreCase)) return 86;
            if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                string race = NormalizeGeneratedRace(domain.Race);
                if (race == "Human") return 8;
                return 3;
            }

            return 100;
        }

        private bool IsDomainType(DomainRecord domain, string domainType)
        {
            return domain != null && string.Equals(domain.DomainType, domainType, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsClanhold(DomainRecord domain)
        {
            return IsDomainType(domain, "Clanhold");
        }

        private bool IsOutlands(DomainRecord domain)
        {
            return domain != null && string.Equals(domain.Classification, "Outlands", StringComparison.OrdinalIgnoreCase);
        }

        private Dictionary<string, DomainRecord> BuildDomainBySettlementId(HexMapRecord map)
        {
            Dictionary<string, DomainRecord> result = new Dictionary<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
            if (map == null || map.Domains == null) return result;

            foreach (DomainRecord domain in map.Domains)
            {
                if (domain == null) continue;
                AddDomainSettlementIndex(result, domain.CapitalSettlementId, domain);
                if (HasRoadStronghold(domain)) AddDomainSettlementIndex(result, StrongholdRoadNodeId(domain), domain);
                if (domain.SettlementIds == null) continue;
                foreach (string settlementId in domain.SettlementIds)
                {
                    AddDomainSettlementIndex(result, settlementId, domain);
                }
            }

            return result;
        }

        private void AddDomainSettlementIndex(Dictionary<string, DomainRecord> index, string settlementId, DomainRecord domain)
        {
            if (index == null || domain == null || string.IsNullOrWhiteSpace(settlementId)) return;
            if (!index.ContainsKey(settlementId)) index[settlementId] = domain;
        }

        private DomainRecord GetCapitalDomain(
            HexMapRecord map,
            MapSettlementRecord settlement,
            Dictionary<string, DomainRecord> domainBySettlementId)
        {
            if (map == null || settlement == null || map.Domains == null) return null;
            DomainRecord cached;
            if (domainBySettlementId != null && domainBySettlementId.TryGetValue(settlement.Id, out cached)) return cached;

            return map.Domains.FirstOrDefault(d => string.Equals(d.CapitalSettlementId, settlement.Id, StringComparison.OrdinalIgnoreCase))
                ?? map.Domains.FirstOrDefault(d => d.SettlementIds != null && d.SettlementIds.Contains(settlement.Id, StringComparer.OrdinalIgnoreCase));
        }

        private HashSet<string> BuildRoadCellIndex(HexMapRecord map)
        {
            HashSet<string> result = new HashSet<string>();
            if (map == null || map.Roads == null) return result;

            foreach (MapEdgeRecord existing in map.Roads.Where(e => e != null))
            {
                result.Add(CellKey(existing.AQ, existing.AR));
                result.Add(CellKey(existing.BQ, existing.BR));
            }

            return result;
        }

        private void AddRoadPath(HexMapRecord map, List<HexCellRecord> path, HashSet<string> added, HashSet<string> roadCells)
        {
            HashSet<string> existingRoadCells = roadCells == null ? BuildRoadCellIndex(map) : new HashSet<string>(roadCells);

            for (int i = 1; i < path.Count; i++)
            {
                MapEdgeRecord edge = new MapEdgeRecord
                {
                    AQ = path[i - 1].Q,
                    AR = path[i - 1].R,
                    BQ = path[i].Q,
                    BR = path[i].R,
                    Kind = "Road"
                };

                string key = edge.NormalizedKey();
                if (added.Add(key))
                {
                    map.Roads.Add(edge);
                    if (roadCells != null)
                    {
                        roadCells.Add(CellKey(edge.AQ, edge.AR));
                        roadCells.Add(CellKey(edge.BQ, edge.BR));
                    }
                }

                // Если новый путь уже вошёл в существующую сеть дорог, дальше до
                // исходной цели его продолжать не нужно: так меньше параллельных обходов.
                if (i < path.Count - 1 && existingRoadCells.Contains(CellKey(path[i].Q, path[i].R)))
                {
                    break;
                }
            }
        }

        private bool IsParallelRoadDuplicate(HexMapRecord map, List<HexCellRecord> path, HashSet<string> roadCells)
        {
            if (map == null || path == null || path.Count < 5 || roadCells == null || roadCells.Count == 0) return false;

            int checkedCells = 0;
            int adjacentCells = 0;
            int nearCells = 0;
            int existingOverlap = 0;
            int currentRun = 0;
            int longestRun = 0;
            int currentNearRun = 0;
            int longestNearRun = 0;

            // Точные совпадения рёбер уже отсеиваются ключом дороги, а эта проверка ловит
            // другой случай: новая трасса долго идёт по соседним гексам рядом со старой.
            for (int i = 1; i < path.Count - 1; i++)
            {
                HexCellRecord cell = path[i];
                string key = CellKey(cell.Q, cell.R);
                if (roadCells.Contains(key))
                {
                    existingOverlap++;
                    currentRun = 0;
                    currentNearRun = 0;
                    continue;
                }

                checkedCells++;
                int proximity = RoadProximityRank(map, cell, roadCells);
                if (proximity == 1)
                {
                    adjacentCells++;
                    nearCells++;
                    currentRun++;
                    currentNearRun++;
                    longestRun = Math.Max(longestRun, currentRun);
                    longestNearRun = Math.Max(longestNearRun, currentNearRun);
                }
                else if (proximity == 2)
                {
                    nearCells++;
                    currentRun = 0;
                    currentNearRun++;
                    longestNearRun = Math.Max(longestNearRun, currentNearRun);
                }
                else
                {
                    currentRun = 0;
                    currentNearRun = 0;
                }
            }

            if (checkedCells == 0) return false;
            if (existingOverlap > 1) return false;
            if (longestRun >= 3 && (adjacentCells >= 4 || adjacentCells * 100 >= checkedCells * 18)) return true;
            return longestNearRun >= 5 && nearCells * 100 >= checkedCells * 35;
        }

        private int RoadProximityRank(HexMapRecord map, HexCellRecord cell, HashSet<string> roadCells)
        {
            if (map == null || cell == null || roadCells == null || roadCells.Count == 0) return 99;
            if (roadCells.Contains(CellKey(cell.Q, cell.R))) return 0;

            List<HexCellRecord> neighbors = GetNeighbors(map, cell);
            if (neighbors.Any(n => roadCells.Contains(CellKey(n.Q, n.R)))) return 1;

            foreach (HexCellRecord neighbor in neighbors)
            {
                foreach (HexCellRecord next in GetNeighbors(map, neighbor))
                {
                    if (next.Q == cell.Q && next.R == cell.R) continue;
                    if (roadCells.Contains(CellKey(next.Q, next.R))) return 2;
                }
            }

            return 99;
        }

        private void RemoveRoadCycles(HexMapRecord map)
        {
            if (map == null || map.Roads == null || map.Roads.Count == 0) return;

            // Сгенерированные дороги должны быть сетью связей, а не набором параллельных
            // дублей. Удаляем рёбра, которые создают циклы внутри дорожного графа.
            Dictionary<string, string> parent = new Dictionary<string, string>();
            List<MapEdgeRecord> result = new List<MapEdgeRecord>();

            foreach (MapEdgeRecord edge in map.Roads.Where(e => e != null))
            {
                string a = CellKey(edge.AQ, edge.AR);
                string b = CellKey(edge.BQ, edge.BR);
                EnsureDisjointNode(parent, a);
                EnsureDisjointNode(parent, b);
                string rootA = FindDisjointRoot(parent, a);
                string rootB = FindDisjointRoot(parent, b);
                if (rootA == rootB) continue;

                parent[rootA] = rootB;
                result.Add(edge);
            }

            map.Roads = result;
        }

        private void RemoveDanglingRoadStubs(HexMapRecord map, HashSet<string> roadNodeCells)
        {
            if (map == null || map.Roads == null || map.Roads.Count == 0) return;
            if (roadNodeCells == null) roadNodeCells = new HashSet<string>();

            bool changed;
            do
            {
                Dictionary<string, int> degree = new Dictionary<string, int>();
                foreach (MapEdgeRecord edge in map.Roads.Where(e => e != null))
                {
                    IncrementRoadDegree(degree, CellKey(edge.AQ, edge.AR));
                    IncrementRoadDegree(degree, CellKey(edge.BQ, edge.BR));
                }

                HashSet<string> deadEnds = new HashSet<string>(
                    degree
                        .Where(pair => pair.Value <= 1 && !roadNodeCells.Contains(pair.Key))
                        .Select(pair => pair.Key));
                if (deadEnds.Count == 0) break;

                int before = map.Roads.Count;
                map.Roads = map.Roads
                    .Where(edge => edge != null
                        && !deadEnds.Contains(CellKey(edge.AQ, edge.AR))
                        && !deadEnds.Contains(CellKey(edge.BQ, edge.BR)))
                    .ToList();
                changed = map.Roads.Count != before;
            }
            while (changed && map.Roads.Count > 0);
        }

        private void IncrementRoadDegree(Dictionary<string, int> degree, string key)
        {
            int value;
            degree.TryGetValue(key, out value);
            degree[key] = value + 1;
        }

        private void EnsureDisjointNode(Dictionary<string, string> parent, string key)
        {
            if (!parent.ContainsKey(key)) parent[key] = key;
        }

        private string FindDisjointRoot(Dictionary<string, string> parent, string key)
        {
            string root = parent[key];
            if (root == key) return key;
            root = FindDisjointRoot(parent, root);
            parent[key] = root;
            return root;
        }

        private List<HexCellRecord> FindLandPath(HexMapRecord map, int aq, int ar, int bq, int br, HashSet<string> roadCells = null)
        {
            string startKey = CellKey(aq, ar);
            string targetKey = CellKey(bq, br);
            Dictionary<string, string> previous = new Dictionary<string, string>();
            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();
            Dictionary<string, int> roadProximityCache = roadCells == null || roadCells.Count == 0
                ? null
                : new Dictionary<string, int>();
            queue.Enqueue(startKey);
            visited.Add(startKey);

            while (queue.Count > 0)
            {
                string key = queue.Dequeue();
                if (key == targetKey) break;
                HexCellRecord cell = GetCell(map, key);
                foreach (HexCellRecord next in GetNeighbors(map, cell).OrderBy(n => LandPathRoutingScore(map, n, bq, br, roadCells, roadProximityCache)))
                {
                    if (next.Water != "None") continue;
                    string nextKey = CellKey(next.Q, next.R);
                    if (!visited.Add(nextKey)) continue;
                    previous[nextKey] = key;
                    queue.Enqueue(nextKey);
                }
            }

            if (!visited.Contains(targetKey)) return new List<HexCellRecord>();

            List<HexCellRecord> path = new List<HexCellRecord>();
            string current = targetKey;
            while (true)
            {
                path.Add(GetCell(map, current));
                if (current == startKey) break;
                current = previous[current];
            }

            path.Reverse();
            return path;
        }

        private double LandPathRoutingScore(
            HexMapRecord map,
            HexCellRecord cell,
            int targetQ,
            int targetR,
            HashSet<string> roadCells,
            Dictionary<string, int> roadProximityCache)
        {
            double score = HexDistance(cell.Q, cell.R, targetQ, targetR);
            if (roadCells == null || roadCells.Count == 0) return score;

            string key = CellKey(cell.Q, cell.R);
            if (roadCells.Contains(key)) return score - 5.0;

            int proximity;
            if (roadProximityCache != null && roadProximityCache.TryGetValue(key, out proximity))
            {
                return ApplyRoadProximityScore(score, proximity);
            }

            // В одном BFS один и тот же гекс попадает в сортировку соседей много раз; близость к уже построенным
            // дорогам в пределах этого поиска неизменна, поэтому кэш безопасен и не меняет маршрутные правила.
            proximity = RoadProximityRank(map, cell, roadCells);
            if (roadProximityCache != null) roadProximityCache[key] = proximity;
            return ApplyRoadProximityScore(score, proximity);
        }

        private double ApplyRoadProximityScore(double score, int proximity)
        {
            if (proximity == 1) score += 8.0;
            else if (proximity == 2) score += 2.5;
            return score;
        }
    }
}
