using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        // Доменный слой отвечает за владения, крепости, население и сгенерированных правителей.
        private void GenerateDomains(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map.Settlements == null) map.Settlements = new List<MapSettlementRecord>();

            // Домены растут от поселений-столиц по соседним пригодным гексам.
            // Один гекс принадлежит только одному домену, как и в ручном редакторе.
            HashSet<string> occupied = new HashSet<string>();
            HashSet<string> assignedSettlementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> settlementCells = new HashSet<string>(map.Settlements.Select(s => CellKey(s.Q, s.R)));
            HashSet<string> protectedSettlementCells = new HashSet<string>(
                map.Settlements.Where(IsPreseededSpecialDomainSettlement).Select(s => CellKey(s.Q, s.R)));
            Dictionary<string, List<MapSettlementRecord>> settlementsByCell = BuildSettlementsByCell(map.Settlements);
            List<MapSettlementRecord> ordered = map.Settlements.OrderBy(s => s.MarketClass).ThenBy(s => random.Next()).ToList();

            foreach (MapSettlementRecord settlement in ordered)
            {
                CheckCancellation();
                if (assignedSettlementIds.Contains(settlement.Id)) continue;

                HexCellRecord capital = GetCell(map, settlement.Q, settlement.R);
                if (!IsSettlementEligible(capital) || occupied.Contains(CellKey(capital.Q, capital.R))) continue;

                DomainRecord domain = new DomainRecord
                {
                    Name = GenerateUniqueDomainName(random, options.CultureKey, settlement.Name),
                    Classification = "Outlands",
                    LandValueMode = options.LandValueMode,
                    FixedLandValueGp = RollLandValue(random, options.LandValueMode),
                    DomainAlignment = PickAlignment(random, options),
                    ColorArgb = DomainColor(map.Domains.Count),
                    CapitalSettlementId = settlement.Id,
                    Notes = "Generated region seed: " + options.Seed
                };

                int targetHexes = TargetDomainHexes(settlement.MarketClass, options, random);
                if (settlement.MarketClass >= 5) targetHexes = Math.Max(2, targetHexes);
                targetHexes = Math.Max(1, (int)Math.Round(targetHexes * DomainSizeMultiplier(options)));
                foreach (HexCellRecord cell in ExpandDomainCells(map, capital, targetHexes, occupied, protectedSettlementCells))
                {
                    domain.Hexes.Add(new DomainHexRecord
                    {
                        Q = cell.Q,
                        R = cell.R,
                        LandValueGp = string.Equals(options.LandValueMode, "PerHex", StringComparison.OrdinalIgnoreCase)
                            ? Roll3d3(random)
                            : domain.FixedLandValueGp
                    });
                    occupied.Add(CellKey(cell.Q, cell.R));
                }

                if (domain.Hexes.Count == 0) continue;

                domain.Classification = ClassifyDomain(map, domain, options);
                domain.DomainType = PickDomainType(map, domain, settlement, options, random);
                ApplySpecialDomainRules(map, domain, settlement, options, random);
                List<MapSettlementRecord> domainSettlements = AssignSettlementsToDomain(map, domain, settlement, assignedSettlementIds, settlementsByCell, options, random);
                int maxFamilies = AcksDomainRules.MaxFamiliesPerHex(domain) * Math.Max(1, domain.Hexes.Count);
                domain.PeasantFamilies = Math.Max(20, maxFamilies * PopulationFillPercent(domain.Classification, random) / 100);
                domain.UrbanFamilies = domainSettlements.Sum(s => UrbanFamiliesForMarketClass(s.MarketClass));
                domain.GarrisonGpPerFamily = domain.Classification == "Civilized" ? 2 : domain.Classification == "Borderlands" ? 3 : 4;
                if (options.GenerateStrongholds)
                {
                    domain.StrongholdValueGp = AcksDomainRules.RequiredStrongholdValue(domain);
                    PlaceDomainStronghold(map, domain, settlement, random, settlementCells);
                }
                else
                {
                    DisableDomainStronghold(domain);
                }

                if (options.GenerateRulers)
                {
                    CharacterRecord ruler = CreateRuler(random, options, settlement.MarketClass, domain.Name, domain.DomainType, CultureForDomain(domain, options));
                    if (IsChaoticClanhold(domain))
                    {
                        ruler.Alignment = "Chaotic";
                    }
                    domain.Ruler = DomainRulerRecord.FromCharacter(ruler, "Generated");
                }

                DomainMoraleSummary morale = AcksDomainRules.CalculateMorale(domain);
                domain.BaseMorale = morale.BaseMorale;
                domain.CurrentMorale = morale.BaseMorale;
                map.Domains.Add(domain);
            }

            GenerateStrongholdOnlyDomains(map, options, random, occupied, settlementCells);
        }

        private List<MapSettlementRecord> AssignSettlementsToDomain(
            HexMapRecord map,
            DomainRecord domain,
            MapSettlementRecord capital,
            HashSet<string> assignedSettlementIds,
            Dictionary<string, List<MapSettlementRecord>> settlementsByCell,
            RegionGenerationOptions options,
            Random random)
        {
            List<MapSettlementRecord> result = new List<MapSettlementRecord>();
            if (map == null || domain == null || map.Settlements == null) return result;
            if (domain.SettlementIds == null) domain.SettlementIds = new List<string>();

            List<MapSettlementRecord> candidates = new List<MapSettlementRecord>();
            foreach (DomainHexRecord hex in domain.Hexes ?? new List<DomainHexRecord>())
            {
                List<MapSettlementRecord> cellSettlements;
                if (settlementsByCell != null && settlementsByCell.TryGetValue(CellKey(hex.Q, hex.R), out cellSettlements))
                {
                    candidates.AddRange(cellSettlements);
                }
            }

            foreach (MapSettlementRecord settlement in candidates
                .Where(s => s != null)
                .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(s => s.MarketClass)
                .ThenBy(s => s.Name))
            {
                if (assignedSettlementIds != null
                    && assignedSettlementIds.Contains(settlement.Id)
                    && (capital == null || !string.Equals(settlement.Id, capital.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(settlement);
            }

            if (capital != null && result.All(s => !string.Equals(s.Id, capital.Id, StringComparison.OrdinalIgnoreCase)))
            {
                result.Insert(0, capital);
            }

            result = result
                .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(s => string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(s => s.MarketClass)
                .ToList();

            string culture = CultureForDomain(domain, options);
            bool useSpecialCulture = options != null
                && !string.IsNullOrWhiteSpace(culture)
                && !string.Equals(culture, options.CultureKey, StringComparison.OrdinalIgnoreCase);

            // Дополнительные поселения наследуют расу домена. Это удерживает эльфийские,
            // дварфийские и клановые владения цельными после объединения нескольких рынков.
            foreach (MapSettlementRecord settlement in result)
            {
                settlement.Race = NormalizeGeneratedRace(domain.Race);
                if (useSpecialCulture
                    && capital != null
                    && !string.Equals(settlement.Id, capital.Id, StringComparison.OrdinalIgnoreCase))
                {
                    settlement.Name = GenerateUniqueSettlementName(random, culture);
                }

                if (assignedSettlementIds != null) assignedSettlementIds.Add(settlement.Id);
            }

            domain.SettlementIds = result.Select(s => s.Id).ToList();
            if (capital != null && !domain.SettlementIds.Contains(capital.Id, StringComparer.OrdinalIgnoreCase))
            {
                domain.SettlementIds.Insert(0, capital.Id);
            }

            return result;
        }

        private Dictionary<string, List<MapSettlementRecord>> BuildSettlementsByCell(IEnumerable<MapSettlementRecord> settlements)
        {
            Dictionary<string, List<MapSettlementRecord>> result = new Dictionary<string, List<MapSettlementRecord>>();
            if (settlements == null) return result;

            foreach (MapSettlementRecord settlement in settlements)
            {
                if (settlement == null) continue;
                string key = CellKey(settlement.Q, settlement.R);
                List<MapSettlementRecord> cellSettlements;
                if (!result.TryGetValue(key, out cellSettlements))
                {
                    cellSettlements = new List<MapSettlementRecord>();
                    result[key] = cellSettlements;
                }

                cellSettlements.Add(settlement);
            }

            return result;
        }

        private void GenerateStrongholdOnlyDomains(HexMapRecord map, RegionGenerationOptions options, Random random, HashSet<string> occupied, HashSet<string> settlementCells)
        {
            if (map == null || options == null || random == null) return;
            if (!options.GenerateDomains) return;
            if (!options.GenerateStrongholds) return;
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) return;

            List<HexCellRecord> candidates = map.Cells
                .Where(IsSettlementEligible)
                .Where(c => occupied == null || !occupied.Contains(CellKey(c.Q, c.R)))
                .Where(c => settlementCells == null || !settlementCells.Contains(CellKey(c.Q, c.R)))
                .Where(c => IsRemoteFromSettlements(map, c, string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase) ? 9 : 8))
                .ToList();
            if (candidates.Count == 0) return;

            int target = string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            target += candidates.Count * Math.Max(10, options.DomainCoveragePercent) / 42000;
            target = Math.Min(target, Math.Max(1, candidates.Count / 120));
            target = Math.Min(target, 5);

            for (int i = 0; i < target; i++)
            {
                CheckCancellation();
                HexCellRecord strongholdCell = null;
                double bestScore = double.MinValue;
                foreach (HexCellRecord candidate in candidates)
                {
                    double score = StrongholdOnlyDomainScore(candidate, map, random);
                    if (strongholdCell == null || score > bestScore)
                    {
                        strongholdCell = candidate;
                        bestScore = score;
                    }
                }

                if (strongholdCell == null) break;

                string baseName = GenerateUniqueSettlementName(random, options.CultureKey);
                DomainRecord domain = new DomainRecord
                {
                    Name = GenerateUniqueDomainName(random, options.CultureKey, baseName),
                    Classification = string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase) ? "Outlands" : "Borderlands",
                    LandValueMode = options.LandValueMode,
                    FixedLandValueGp = RollLandValue(random, options.LandValueMode),
                    DomainAlignment = PickAlignment(random, options),
                    ColorArgb = DomainColor(map.Domains.Count),
                    CapitalSettlementId = "",
                    Notes = "Generated stronghold-only domain seed: " + options.Seed
                };

                int targetHexes = Math.Max(2, (int)Math.Round(random.Next(2, 6) * DomainSizeMultiplier(options)));
                foreach (HexCellRecord cell in ExpandDomainCells(map, strongholdCell, targetHexes, occupied, settlementCells))
                {
                    domain.Hexes.Add(new DomainHexRecord
                    {
                        Q = cell.Q,
                        R = cell.R,
                        LandValueGp = string.Equals(options.LandValueMode, "PerHex", StringComparison.OrdinalIgnoreCase)
                            ? Roll3d3(random)
                            : domain.FixedLandValueGp
                    });
                    if (occupied != null) occupied.Add(CellKey(cell.Q, cell.R));
                }

                if (domain.Hexes.Count == 0) continue;

                domain.Classification = ClassifyDomain(map, domain, options);
                if (domain.Classification == "Civilized") domain.Classification = "Borderlands";
                domain.DomainType = PickDomainType(map, domain, null, options, random);
                ApplySpecialDomainRules(map, domain, null, options, random);
                int maxFamilies = AcksDomainRules.MaxFamiliesPerHex(domain) * Math.Max(1, domain.Hexes.Count);
                domain.PeasantFamilies = Math.Max(10, maxFamilies * PopulationFillPercent(domain.Classification, random) / 100);
                domain.UrbanFamilies = 0;
                domain.GarrisonGpPerFamily = domain.Classification == "Borderlands" ? 3 : 4;
                domain.StrongholdValueGp = AcksDomainRules.RequiredStrongholdValue(domain);
                PlaceDomainStrongholdAt(map, domain, strongholdCell);

                if (options.GenerateRulers)
                {
                    CharacterRecord ruler = CreateRuler(random, options, 6, domain.Name, domain.DomainType, CultureForDomain(domain, options));
                    if (IsChaoticClanhold(domain)) ruler.Alignment = "Chaotic";
                    domain.Ruler = DomainRulerRecord.FromCharacter(ruler, "Generated");
                }

                DomainMoraleSummary morale = AcksDomainRules.CalculateMorale(domain);
                domain.BaseMorale = morale.BaseMorale;
                domain.CurrentMorale = morale.BaseMorale;
                map.Domains.Add(domain);
                candidates.RemoveAll(c => HexDistance(c.Q, c.R, strongholdCell.Q, strongholdCell.R) < 5);
            }
        }

        private double StrongholdOnlyDomainScore(HexCellRecord cell, HexMapRecord map, Random random)
        {
            double score = random.NextDouble() * 3.0;
            if (cell.Elevation == "Hills") score += 2.0;
            if (cell.Elevation == "Mountains") score += 3.0;
            if (cell.Terrain == "Forest" || cell.Terrain == "Taiga" || cell.Terrain == "Scrub") score += 1.0;
            int nearestSettlement = map.Settlements
                .Select(s => HexDistance(cell.Q, cell.R, s.Q, s.R))
                .DefaultIfEmpty(12)
                .Min();
            score += Math.Min(8, nearestSettlement);
            return score;
        }

        private void DisableDomainStronghold(DomainRecord domain)
        {
            if (domain == null) return;

            // Выключенный слой крепостей оставляет домен владением, но убирает
            // отдельный объект крепости, чтобы дороги и отрисовка не видели
            // несуществующую точку интереса.
            domain.StrongholdId = "";
            domain.StrongholdName = "";
            domain.StrongholdValueGp = 0;
            domain.StrongholdQ = -1;
            domain.StrongholdR = -1;
            domain.StrongholdIconKey = "";
            domain.StrongholdInSettlement = false;
            domain.StrongholdSettlementId = "";
            domain.StrongholdActsAsMarketClassVI = false;
            domain.StrongholdSecuresDomain = false;
            domain.StrongholdIsUnderground = false;
            domain.StrongholdNaturalMajesty = false;
        }

        private void RegenerateStrongholdsForExistingDomains(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || map.Domains == null) return;

            HashSet<string> settlementCells = map.Settlements == null
                ? new HashSet<string>()
                : new HashSet<string>(map.Settlements.Select(s => CellKey(s.Q, s.R)), StringComparer.OrdinalIgnoreCase);
            foreach (DomainRecord domain in map.Domains.Where(d => d != null))
            {
                DisableDomainStronghold(domain);
                if (options == null || !options.GenerateStrongholds) continue;

                domain.StrongholdValueGp = AcksDomainRules.RequiredStrongholdValue(domain);
                MapSettlementRecord capital = map.Settlements == null
                    ? null
                    : map.Settlements.FirstOrDefault(s => string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase));
                PlaceDomainStronghold(map, domain, capital, random, settlementCells);
            }
        }

        private void RegenerateRulersForExistingDomains(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || map.Domains == null || options == null || !options.GenerateRulers) return;

            foreach (DomainRecord domain in map.Domains.Where(d => d != null))
            {
                MapSettlementRecord settlement = map.Settlements == null
                    ? null
                    : map.Settlements.FirstOrDefault(s => string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase));
                int marketClass = settlement == null ? 6 : settlement.MarketClass;
                CharacterRecord ruler = CreateRuler(random, options, marketClass, domain.Name, domain.DomainType, CultureForDomain(domain, options));
                if (IsChaoticClanhold(domain))
                {
                    ruler.Alignment = "Chaotic";
                }

                domain.Ruler = DomainRulerRecord.FromCharacter(ruler, "Generated");
            }
        }

        private void PlaceDomainStrongholdAt(HexMapRecord map, DomainRecord domain, HexCellRecord cell)
        {
            if (domain == null || cell == null) return;
            domain.StrongholdId = Guid.NewGuid().ToString("N");
            domain.StrongholdName = MakeStrongholdName(domain);
            domain.StrongholdType = "Fortress";
            domain.StrongholdIconKey = StrongholdIconKey(domain);
            domain.StrongholdSecuresDomain = true;
            domain.StrongholdActsAsMarketClassVI = true;
            domain.StrongholdIsUnderground = string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase);
            domain.StrongholdNaturalMajesty = string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase);
            domain.StrongholdInSettlement = false;
            domain.StrongholdSettlementId = "";
            domain.StrongholdQ = cell.Q;
            domain.StrongholdR = cell.R;
        }

        private void PlaceDomainStronghold(
            HexMapRecord map,
            DomainRecord domain,
            MapSettlementRecord capital,
            Random random,
            HashSet<string> settlementCells = null)
        {
            if (domain == null) return;

            domain.StrongholdId = Guid.NewGuid().ToString("N");
            domain.StrongholdName = MakeStrongholdName(domain);
            domain.StrongholdType = "Fortress";
            domain.StrongholdIconKey = StrongholdIconKey(domain);
            domain.StrongholdSecuresDomain = true;
            domain.StrongholdActsAsMarketClassVI = true;
            domain.StrongholdIsUnderground = string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase);
            domain.StrongholdNaturalMajesty = string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase);

            bool separate = ShouldPlaceSeparateStronghold(domain, random);
            HexCellRecord cell = separate ? PickStrongholdCell(map, domain, capital, random, settlementCells) : null;
            if (cell == null && capital != null)
            {
                cell = GetCell(map, capital.Q, capital.R);
                domain.StrongholdInSettlement = true;
                domain.StrongholdSettlementId = capital.Id;
            }
            else
            {
                domain.StrongholdInSettlement = false;
                domain.StrongholdSettlementId = "";
            }

            if (cell == null && domain.Hexes != null && domain.Hexes.Count > 0)
            {
                DomainHexRecord first = domain.Hexes[0];
                cell = GetCell(map, first.Q, first.R);
            }

            if (cell != null)
            {
                domain.StrongholdQ = cell.Q;
                domain.StrongholdR = cell.R;
            }
        }

        private bool ShouldPlaceSeparateStronghold(DomainRecord domain, Random random)
        {
            int chance = 18;
            if (string.Equals(domain.Classification, "Outlands", StringComparison.OrdinalIgnoreCase)) chance += 22;
            if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)) chance += 22;
            if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase)) chance += 28;
            if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase)) chance += 24;
            if (string.Equals(domain.DomainType, "Transitional", StringComparison.OrdinalIgnoreCase)) chance -= 8;
            return random.Next(100) < Math.Max(5, Math.Min(78, chance));
        }

        private HexCellRecord PickStrongholdCell(
            HexMapRecord map,
            DomainRecord domain,
            MapSettlementRecord capital,
            Random random,
            HashSet<string> settlementCells = null)
        {
            if (map == null || domain == null || domain.Hexes == null) return null;
            if (settlementCells == null)
            {
                settlementCells = map.Settlements == null
                    ? new HashSet<string>()
                    : new HashSet<string>(map.Settlements.Select(s => CellKey(s.Q, s.R)), StringComparer.OrdinalIgnoreCase);
            }

            List<HexCellRecord> cells = DomainCells(map, domain)
                .Where(c => c != null && !IsWater(c) && !settlementCells.Contains(CellKey(c.Q, c.R)))
                .ToList();
            if (cells.Count == 0) return null;

            HexCellRecord best = null;
            double bestScore = double.MinValue;
            foreach (HexCellRecord cell in cells)
            {
                double score = StrongholdSiteScore(cell, domain, capital, random);
                if (best == null || score > bestScore)
                {
                    best = cell;
                    bestScore = score;
                }
            }

            return best;
        }

        private double StrongholdSiteScore(HexCellRecord cell, DomainRecord domain, MapSettlementRecord capital, Random random)
        {
            double score = random.NextDouble() * 2.0;
            if (capital != null) score += Math.Min(4, HexDistance(cell.Q, cell.R, capital.Q, capital.R));
            if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase))
            {
                if (cell.Elevation == "Mountains") score += 8;
                if (cell.Elevation == "Hills") score += 4;
            }
            else if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase))
            {
                if (cell.Terrain == "Forest" || cell.Terrain == "Taiga" || cell.Terrain == "Rainforest") score += 6;
                if (cell.Elevation == "Mountains") score += 3;
            }
            else if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                if (cell.Elevation != "Plains") score += 2;
                if (cell.Terrain == "Scrub" || cell.Terrain == "Steppe" || cell.Terrain == "Tundra") score += 3;
            }

            return score;
        }

        private string MakeStrongholdName(DomainRecord domain)
        {
            if (domain == null || string.IsNullOrWhiteSpace(domain.Name))
            {
                return useRussianNames ? "Крепость" : "Stronghold";
            }

            string baseName = ShortDomainNameForStronghold(domain.Name);
            return useRussianNames ? "Крепость " + baseName : baseName + " Stronghold";
        }

        private string ShortDomainNameForStronghold(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string value = name.Trim();
            string[] prefixes =
            {
                "Domain of ", "Duchy of ", "County of ", "March of ",
                "Домен ", "Марка ", "Графство ", "Долина ", "Земли "
            };

            foreach (string prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(prefix.Length).Trim();
                }
            }

            string[] suffixes = { " March", " County", " Vale", " Land" };
            foreach (string suffix in suffixes)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(0, value.Length - suffix.Length).Trim();
                }
            }

            return value;
        }

        private string StrongholdIconKey(DomainRecord domain)
        {
            if (domain == null) return "fortress";
            string race = NormalizeGeneratedRace(domain.Race);
            if (race == "Dwarf") return "fortressdwarf";
            if (race == "Elf") return "fortresself";
            if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                if (race == "Orc") return "fortressorcs";
                if (race == "Beastman") return "fortressbeastman";
                return "fortressbarbarians";
            }

            return "fortress";
        }

        private IEnumerable<HexCellRecord> ExpandDomainCells(HexMapRecord map, HexCellRecord capital, int targetCount, HashSet<string> occupied, HashSet<string> settlementCells)
        {
            Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(capital);
            visited.Add(CellKey(capital.Q, capital.R));

            while (queue.Count > 0 && targetCount > 0)
            {
                HexCellRecord cell = queue.Dequeue();
                string key = CellKey(cell.Q, cell.R);
                bool anotherSettlement = settlementCells != null
                    && settlementCells.Contains(key)
                    && !(cell.Q == capital.Q && cell.R == capital.R);
                bool claimed = !occupied.Contains(key) && !anotherSettlement && IsDomainEligible(cell);
                if (claimed)
                {
                    yield return cell;
                    targetCount--;
                }

                if (!claimed) continue;

                foreach (HexCellRecord next in GetNeighbors(map, cell).OrderBy(n => HexDistance(n.Q, n.R, capital.Q, capital.R)))
                {
                    string nextKey = CellKey(next.Q, next.R);
                    if (!visited.Add(nextKey)) continue;
                    if (!CanExpandDomainInto(next, capital, occupied, settlementCells)) continue;
                    queue.Enqueue(next);
                }
            }
        }

        private bool CanExpandDomainInto(HexCellRecord cell, HexCellRecord capital, HashSet<string> occupied, HashSet<string> settlementCells)
        {
            if (cell == null || capital == null) return false;
            if (!IsDomainEligible(cell)) return false;

            string key = CellKey(cell.Q, cell.R);
            if (occupied != null && occupied.Contains(key)) return false;

            // Доменные гексы должны образовывать сухопутную связную область.
            // Поэтому расширение не проходит сквозь воду, запретные биомы,
            // чужие поселения и уже занятые гексы.
            bool anotherSettlement = settlementCells != null
                && settlementCells.Contains(key)
                && !(cell.Q == capital.Q && cell.R == capital.R);
            return !anotherSettlement;
        }

        private bool IsDomainEligible(HexCellRecord cell)
        {
            if (cell == null || cell.Water != "None") return false;
            return cell.Terrain != "Marsh" && cell.Terrain != "DeepForest" && cell.Terrain != "DeepTaiga";
        }

        private string ClassifyDomain(HexMapRecord map, DomainRecord domain, RegionGenerationOptions options)
        {
            if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase)) return "Outlands";
            if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) return "Civilized";

            List<MapSettlementRecord> largeMarkets = map.Settlements
                .Where(s => s.MarketClass <= 4)
                .ToList();
            if (largeMarkets.Count == 0 || domain == null || domain.Hexes == null || domain.Hexes.Count == 0) return "Outlands";

            if (string.Equals(options.CivilizationLevel, "Borderlands", StringComparison.OrdinalIgnoreCase))
            {
                // Для пограничья используем малое цивилизованное ядро и более
                // широкий пояс рынков: так карта получает градиент освоенности.
                int coreCount = Math.Max(1, map.Settlements.Count / 35);
                int borderCount = Math.Max(coreCount + 1, map.Settlements.Count / 10);
                List<MapSettlementRecord> coreMarkets = map.Settlements
                    .OrderBy(s => s.MarketClass)
                    .ThenBy(s => s.Q + s.R)
                    .Take(coreCount)
                    .ToList();
                List<MapSettlementRecord> borderMarkets = largeMarkets
                    .OrderBy(s => s.MarketClass)
                    .ThenBy(s => s.Q + s.R)
                    .Take(borderCount)
                    .ToList();

                int farthestNearestCore = domain.Hexes
                    .Select(h => coreMarkets.Select(s => HexDistance(h.Q, h.R, s.Q, s.R)).DefaultIfEmpty(99).Min())
                    .DefaultIfEmpty(99)
                    .Max();
                int farthestNearestBorder = domain.Hexes
                    .Select(h => borderMarkets.Select(s => HexDistance(h.Q, h.R, s.Q, s.R)).DefaultIfEmpty(99).Min())
                    .DefaultIfEmpty(99)
                    .Max();

                if (farthestNearestCore <= 4) return "Civilized";
                if (farthestNearestCore <= 10 || farthestNearestBorder <= 5) return "Borderlands";
                return "Outlands";
            }

            int farthestNearestLarge = domain.Hexes
                .Select(h => largeMarkets.Select(s => HexDistance(h.Q, h.R, s.Q, s.R)).DefaultIfEmpty(99).Min())
                .DefaultIfEmpty(99)
                .Max();

            if (farthestNearestLarge <= 8) return "Civilized";
            if (farthestNearestLarge <= 12) return "Borderlands";
            return "Outlands";
        }

        private string PickDomainType(HexMapRecord map, DomainRecord domain, MapSettlementRecord capital, RegionGenerationOptions options, Random random)
        {
            if (domain == null || options == null || !options.GenerateSpecialDomains) return "Ordinary";
            if (IsPreseededDwarvenDomainSettlement(capital) && options.GenerateDwarvenDomains)
            {
                return "Dwarven Vault";
            }

            if (IsPreseededElvenDomainSettlement(capital) && options.GenerateElvenDomains)
            {
                return "Elven Fastness";
            }

            if (IsPreseededClanholdSettlement(capital) && options.GenerateClanDomains)
            {
                return "Clanhold";
            }

            if (random.Next(100) >= options.SpecialDomainPercent) return "Ordinary";

            List<HexCellRecord> cells = DomainCells(map, domain).ToList();
            if (cells.Count == 0) return "Ordinary";

            bool hasMountains = cells.Any(c => c.Elevation == "Mountains");
            bool hasHighlands = hasMountains || cells.Any(c => c.Elevation == "Hills");
            bool hasElvenForest = cells.Any(c => c.Terrain == "Forest" || c.Terrain == "DeepForest" || c.Terrain == "Taiga" || c.Terrain == "DeepTaiga" || c.Terrain == "Rainforest");
            bool isCivilized = domain.Classification == "Civilized";
            bool isBorderOrWild = domain.Classification == "Borderlands" || domain.Classification == "Outlands";

            List<WeightedTextOption> candidates = new List<WeightedTextOption>();
            bool wild = options != null && string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase);
            bool noHumansNearby = HasNoNearbyRace(map, domain, "Human", wild ? 9 : 6);
            bool noHumansNearElves = HasNoNearbyRace(map, domain, "Human", wild ? 7 : domain.Classification == "Outlands" ? 4 : 5);
            bool noDwarvesNearby = HasNoNearbyRace(map, domain, "Dwarf", 6);
            bool noDwarvesNearElves = HasNoNearbyRace(map, domain, "Dwarf", 5);
            bool noElvesNearby = HasNoNearbyRace(map, domain, "Elf", 6);
            bool remoteFromCivilization = IsRemoteFromCivilization(map, domain, wild ? 10 : domain.Classification == "Outlands" ? 7 : 5);

            if (options.GenerateDwarvenDomains && hasHighlands && isBorderOrWild && noHumansNearby && noElvesNearby && options.DwarvenDomainWeight > 0)
            {
                candidates.Add(new WeightedTextOption
                {
                    Value = "Dwarven Vault",
                    Weight = options.DwarvenDomainWeight * (hasMountains ? 5 : 2) * (domain.Classification == "Outlands" ? 2 : 1)
                });
            }

            if (options.GenerateElvenDomains && hasElvenForest && isBorderOrWild && noHumansNearElves && noDwarvesNearElves && options.ElvenDomainWeight > 0)
            {
                candidates.Add(new WeightedTextOption
                {
                    Value = "Elven Fastness",
                    Weight = options.ElvenDomainWeight * (domain.Classification == "Outlands" ? 6 : 3)
                });
            }

            if (options.GenerateClanDomains && domain.Classification == "Outlands" && remoteFromCivilization && noHumansNearby && options.ClanDomainWeight > 0)
            {
                candidates.Add(new WeightedTextOption
                {
                    Value = "Clanhold",
                    Weight = options.ClanDomainWeight * 4
                });
            }

            if (options.GenerateTransitionalDomains && options.TransitionalDomainWeight > 0)
            {
                candidates.Add(new WeightedTextOption
                {
                    Value = "Transitional",
                    Weight = options.TransitionalDomainWeight * (isCivilized ? 1 : 2)
                });
            }

            string picked = PickWeighted(candidates, random);
            return string.IsNullOrWhiteSpace(picked) ? "Ordinary" : picked;
        }

        private void ApplySpecialDomainRules(HexMapRecord map, DomainRecord domain, MapSettlementRecord capital, RegionGenerationOptions options, Random random)
        {
            if (domain == null) return;

            // ACKS различает особые домены не только названием. Здесь фиксируем
            // расу столицы, классификацию земель и мировоззрение для дальнейших правил.
            domain.Race = "Human";
            if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase))
            {
                domain.Race = "Dwarf";
                domain.DomainAlignment = random.Next(100) < 72 ? "Lawful" : "Neutral";
            }
            else if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase))
            {
                domain.Race = "Elf";
                domain.DomainAlignment = random.Next(100) < 65 ? "Neutral" : "Lawful";
            }
            else if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                string seededRace = IsPreseededClanholdSettlement(capital) ? NormalizeGeneratedRace(capital.Race) : "";
                domain.Race = string.IsNullOrWhiteSpace(seededRace) ? PickClanholdRace(random) : seededRace;
                domain.Classification = "Outlands";
                domain.DomainAlignment = IsChaoticClanhold(domain)
                    ? "Chaotic"
                    : (random.Next(100) < 55 ? "Neutral" : "Chaotic");
            }
            else if (string.Equals(domain.DomainType, "Transitional", StringComparison.OrdinalIgnoreCase))
            {
                domain.Race = "Human";
                domain.Classification = TransitionalClassification(map, domain);
            }

            if (capital != null)
            {
                capital.Race = NormalizeGeneratedRace(domain.Race);
                string culture = CultureForDomain(domain, options);
                bool usesSpecialCulture = !string.Equals(culture, options.CultureKey, StringComparison.OrdinalIgnoreCase);
                if (usesSpecialCulture)
                {
                    capital.Name = GenerateUniqueSettlementName(random, culture);
                    domain.Name = GenerateUniqueDomainName(random, culture, capital.Name);
                }
            }
        }

        private bool HasNoNearbyRace(HexMapRecord map, DomainRecord domain, string race, int radius)
        {
            if (map == null || map.Domains == null || domain == null || domain.Hexes == null) return true;
            string blocked = NormalizeGeneratedRace(race);
            foreach (DomainRecord other in map.Domains)
            {
                string otherRace = NormalizeGeneratedRace(other.Race);
                if (otherRace != blocked) continue;
                if (other.Hexes == null) continue;
                foreach (DomainHexRecord a in domain.Hexes)
                {
                    if (other.Hexes.Any(b => HexDistance(a.Q, a.R, b.Q, b.R) <= radius))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsRemoteFromCivilization(HexMapRecord map, DomainRecord domain, int minDistance)
        {
            if (map == null || domain == null || domain.Hexes == null) return false;
            List<MapSettlementRecord> civilized = map.Settlements
                .Where(s => s.MarketClass <= 4)
                .ToList();
            if (civilized.Count == 0) return true;

            int nearest = domain.Hexes
                .Select(h => civilized.Select(s => HexDistance(h.Q, h.R, s.Q, s.R)).DefaultIfEmpty(99).Min())
                .DefaultIfEmpty(99)
                .Min();
            return nearest >= minDistance;
        }

        private string PickClanholdRace(Random random)
        {
            int roll = random.Next(100);
            if (roll < 45) return "Human";
            if (roll < 75) return "Orc";
            return "Beastman";
        }

        private bool IsChaoticClanhold(DomainRecord domain)
        {
            if (domain == null || !string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)) return false;
            string race = NormalizeGeneratedRace(domain.Race);
            return race == "Orc" || race == "Beastman";
        }

        private string TransitionalClassification(HexMapRecord map, DomainRecord domain)
        {
            MapSettlementRecord capital = map == null || map.Settlements == null
                ? null
                : map.Settlements.FirstOrDefault(s => s.Id == domain.CapitalSettlementId);
            if (capital == null) return "Outlands";

            bool nearCity = map.Settlements.Any(s => s.MarketClass <= 4 && HexDistance(s.Q, s.R, capital.Q, capital.R) <= 4);
            if (nearCity) return "Civilized";

            bool nearCivilized = map.Domains.Any(d => !string.Equals(d.Id, domain.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(d.Classification, "Civilized", StringComparison.OrdinalIgnoreCase)
                && d.Hexes != null
                && d.Hexes.Any(h => HexDistance(h.Q, h.R, capital.Q, capital.R) <= 8));
            return nearCivilized ? "Borderlands" : "Outlands";
        }

        private string NormalizeGeneratedRace(string race)
        {
            if (string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase)) return "Dwarf";
            if (string.Equals(race, "Elf", StringComparison.OrdinalIgnoreCase)) return "Elf";
            if (string.Equals(race, "Orc", StringComparison.OrdinalIgnoreCase)) return "Orc";
            if (string.Equals(race, "Beastman", StringComparison.OrdinalIgnoreCase) || string.Equals(race, "Beastmen", StringComparison.OrdinalIgnoreCase)) return "Beastman";
            return "Human";
        }

        private string CultureForRace(string race, string fallbackCulture)
        {
            string normalized = NormalizeGeneratedRace(race);
            if (normalized == "Dwarf") return "dwarf";
            if (normalized == "Elf") return "elf";
            if (normalized == "Orc") return "orc";
            if (normalized == "Beastman") return "beastman";
            return string.IsNullOrWhiteSpace(fallbackCulture) ? "english" : fallbackCulture;
        }

        private string CultureForDomain(DomainRecord domain, RegionGenerationOptions options)
        {
            string fallback = options == null || string.IsNullOrWhiteSpace(options.CultureKey) ? "english" : options.CultureKey;
            if (domain == null) return fallback;

            if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase))
            {
                return options != null && options.UseDwarvenCultureNames ? "dwarf" : fallback;
            }

            if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase))
            {
                return options != null && options.UseElvenCultureNames ? "elf" : fallback;
            }

            if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                return options != null && options.UseClanCultureNames ? CultureForClanhold(domain.Race, fallback) : fallback;
            }

            if (string.Equals(domain.DomainType, "Transitional", StringComparison.OrdinalIgnoreCase))
            {
                return options != null && options.UseTransitionalCultureNames ? CultureForRace(domain.Race, fallback) : fallback;
            }

            return fallback;
        }

        private string CultureForClanhold(string race, string fallbackCulture)
        {
            string normalized = NormalizeGeneratedRace(race);
            if (normalized == "Human") return "human_clan";
            return CultureForRace(normalized, fallbackCulture);
        }

        private IEnumerable<HexCellRecord> DomainCells(HexMapRecord map, DomainRecord domain)
        {
            if (map == null || domain == null || domain.Hexes == null) yield break;
            foreach (DomainHexRecord hex in domain.Hexes)
            {
                HexCellRecord cell = GetCell(map, hex.Q, hex.R);
                if (cell != null) yield return cell;
            }
        }

        private string PickWeighted(List<WeightedTextOption> candidates, Random random)
        {
            if (candidates == null || candidates.Count == 0) return null;
            int total = candidates.Sum(c => Math.Max(0, c.Weight));
            if (total <= 0) return null;

            int roll = random.Next(total);
            foreach (WeightedTextOption candidate in candidates)
            {
                int weight = Math.Max(0, candidate.Weight);
                if (roll < weight) return candidate.Value;
                roll -= weight;
            }

            return candidates.Last().Value;
        }

        private int TargetDomainHexes(int marketClass, RegionGenerationOptions options, Random random)
        {
            int baseSize;
            switch (marketClass)
            {
                case 1: baseSize = 34; break;
                case 2: baseSize = 24; break;
                case 3: baseSize = 14; break;
                case 4: baseSize = 8; break;
                case 5: baseSize = 5; break;
                default: baseSize = 3; break;
            }

            double variance = 1.0 + ((random.NextDouble() * 2.0 - 1.0) * options.StateSizeVariancePercent / 100.0);
            return Math.Max(1, (int)Math.Round(baseSize * variance));
        }

        private double DomainSizeMultiplier(RegionGenerationOptions options)
        {
            int coverage = options == null ? 45 : Clamp(options.DomainCoveragePercent, 0, 100);
            return 0.85 + coverage / 200.0;
        }

        private int PopulationFillPercent(string classification, Random random)
        {
            if (classification == "Civilized") return random.Next(62, 96);
            if (classification == "Borderlands") return random.Next(32, 70);
            return random.Next(10, 45);
        }

        private int UrbanFamiliesForMarketClass(int marketClass)
        {
            switch (marketClass)
            {
                case 1: return 25000;
                case 2: return 8000;
                case 3: return 3200;
                case 4: return 900;
                case 5: return 320;
                default: return 120;
            }
        }

        private CharacterRecord CreateRuler(Random random, RegionGenerationOptions options, int marketClass, string domainName, string domainType, string cultureKey)
        {
            string className = PickRulerClass(random, domainType);
            int level = marketClass <= 2 ? random.Next(11, 15)
                : marketClass == 3 ? random.Next(9, 13)
                : marketClass == 4 ? random.Next(7, 11)
                : marketClass == 5 ? random.Next(5, 9)
                : random.Next(3, 7);
            bool female = random.Next(100) < 35;
            string resolvedCulture = !string.IsNullOrWhiteSpace(cultureKey)
                ? cultureKey
                : options == null || string.IsNullOrWhiteSpace(options.CultureKey) ? "english" : options.CultureKey;
            bool russianNames = options != null && options.UseRussianNames;

            CharacterRecord ruler = new CharacterRecord
            {
                Kind = "NPC",
                Name = names.GeneratePersonalName(random, resolvedCulture, female, russianNames),
                ClassName = className,
                Occupation = "Ruler",
                Sex = female ? "Female" : "Male",
                Level = level,
                CHA = Roll3d6(random),
                Alignment = PickRulerAlignment(random, options, domainType),
                Proficiencies = random.Next(100) < 45 ? "Leadership" : "",
                Notes = "Generated ruler of " + domainName + "."
            };
            ruler.UpdatedAt = DateTime.Now;
            return ruler;
        }

        private string PickRulerClass(Random random, string domainType)
        {
            if (domainType == "Dwarven Vault") return Pick(random, new[] { "Dwarven Vaultguard", "Dwarven Craftpriest", "Fighter" });
            if (domainType == "Elven Fastness") return Pick(random, new[] { "Elven Spellsword", "Elven Nightblade", "Mage" });
            if (domainType == "Clanhold") return Pick(random, new[] { "Barbarian", "Shaman", "Fighter" });
            if (domainType == "Transitional") return Pick(random, new[] { "Venturer", "Explorer", "Crusader", "Fighter" });
            return Pick(random, new[] { "Fighter", "Venturer", "Crusader", "Explorer", "Mage" });
        }

        private string PickRulerAlignment(Random random, RegionGenerationOptions options, string domainType)
        {
            if (domainType == "Dwarven Vault") return random.Next(100) < 70 ? "Lawful" : "Neutral";
            if (domainType == "Elven Fastness") return random.Next(100) < 65 ? "Neutral" : "Lawful";
            if (domainType == "Clanhold") return random.Next(100) < 60 ? "Neutral" : "Chaotic";
            return PickAlignment(random, options);
        }
    }
}
