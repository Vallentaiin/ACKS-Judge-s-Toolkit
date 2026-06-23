using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        private Dictionary<string, MapSettlementRecord> realmSettlementById = new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<DomainRecord, Point?> realmDomainAnchorCache = new Dictionary<DomainRecord, Point?>();

        // Политический слой отделен от рельефа, поселений и дорог: здесь домены
        // только группируются в державы и связываются вассальными отношениями.
        private void GenerateRealms(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map.Domains.Count == 0) return;

            map.Realms.Clear();
            map.VassalLinks.Clear();
            usedRealmNames.Clear();

            BuildRealmGenerationCaches(map);
            try
            {
                foreach (RealmDomainGroup domainGroup in PartitionDomainsForRealms(map, options, random))
                {
                    RealmBuildNode node = BuildRealmTitlePyramid(map, domainGroup.Domains, options, random, domainGroup.Scale);
                    if (node == null) continue;
                }

                EnsureGeneratedRealmTierOrder(map, options, random);
                UpdateRealmRulersFromDomains(map);
            }
            finally
            {
                ClearRealmGenerationCaches();
            }
        }

        private void BuildRealmGenerationCaches(HexMapRecord map)
        {
            // Кэш живёт только в одном проходе генерации: он не меняет модель, а убирает
            // повторные поиски столиц и якорей доменов при разбиении больших карт на державы.
            realmSettlementById = map == null || map.Settlements == null
                ? new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase)
                : map.Settlements
                    .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id))
                    .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            realmDomainAnchorCache = new Dictionary<DomainRecord, Point?>();
        }

        private void ClearRealmGenerationCaches()
        {
            realmSettlementById = new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase);
            realmDomainAnchorCache = new Dictionary<DomainRecord, Point?>();
        }

        private sealed class RealmBuildNode
        {
            public RealmRecord Realm { get; set; }
            public List<DomainRecord> Domains { get; set; }
        }

        private sealed class RealmDomainGroup
        {
            public string PolityKey { get; set; }
            public string Scale { get; set; }
            public List<DomainRecord> Domains { get; set; }
        }

        private List<RealmDomainGroup> PartitionDomainsForRealms(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            List<RealmDomainGroup> result = new List<RealmDomainGroup>();
            List<DomainRecord> domains = map.Domains
                .Where(d => d != null && d.Hexes != null && d.Hexes.Count > 0)
                .ToList();
            if (domains.Count == 0) return result;

            foreach (IGrouping<string, DomainRecord> polityGroup in domains.GroupBy(DomainPolityKey, StringComparer.OrdinalIgnoreCase))
            {
                string scale = RealmScaleForPolity(options, polityGroup.Key);
                List<DomainRecord> polityDomains = polityGroup.OrderByDescending(DomainPower).ToList();
                foreach (List<DomainRecord> group in SplitDomainsByRealmScale(map, polityDomains, scale, options, random))
                {
                    result.Add(new RealmDomainGroup
                    {
                        PolityKey = polityGroup.Key,
                        Scale = scale,
                        Domains = group
                    });
                }
            }

            return result;
        }

        private IEnumerable<List<DomainRecord>> SplitDomainsByRealmScale(
            HexMapRecord map,
            List<DomainRecord> domains,
            string scale,
            RegionGenerationOptions options,
            Random random)
        {
            if (domains == null || domains.Count == 0) yield break;

            if (string.Equals(scale, "Independent", StringComparison.OrdinalIgnoreCase))
            {
                foreach (DomainRecord domain in OrderDomainsSpatially(map, domains))
                {
                    yield return new List<DomainRecord> { domain };
                }

                yield break;
            }

            if (string.Equals(scale, "OneState", StringComparison.OrdinalIgnoreCase))
            {
                yield return OrderDomainsSpatially(map, domains).ToList();
                yield break;
            }

            List<DomainRecord> remaining = new List<DomainRecord>();
            foreach (DomainRecord domain in domains)
            {
                if (ShouldPeelIndependentDomain(domain, domains, scale, random))
                {
                    yield return new List<DomainRecord> { domain };
                }
                else
                {
                    remaining.Add(domain);
                }
            }

            if (remaining.Count == 0) yield break;

            int targetCount = TargetRealmGroupCount(scale, remaining.Count, options);
            List<DomainRecord> anchors = remaining
                .OrderByDescending(DomainPower)
                .Take(targetCount)
                .ToList();
            Dictionary<DomainRecord, List<DomainRecord>> groups = anchors.ToDictionary(a => a, a => new List<DomainRecord>());
            foreach (DomainRecord domain in remaining)
            {
                DomainRecord anchor = anchors
                    .OrderBy(a => DistanceBetweenDomains(map, domain, a))
                    .ThenByDescending(DomainPower)
                    .First();
                groups[anchor].Add(domain);
            }

            foreach (List<DomainRecord> group in groups.Values.Where(g => g.Count > 0))
            {
                yield return OrderDomainsSpatially(map, group).ToList();
            }
        }

        private bool ShouldPeelIndependentDomain(DomainRecord domain, List<DomainRecord> allDomains, string scale, Random random)
        {
            if (domain == null || allDomains == null || allDomains.Count <= 1) return false;

            int chance;
            if (string.Equals(scale, "ManySmall", StringComparison.OrdinalIgnoreCase)) chance = 42;
            else if (string.Equals(scale, "FewLarge", StringComparison.OrdinalIgnoreCase)) chance = 3;
            else chance = 14;

            int topPower = Math.Max(1, allDomains.Max(DomainPower));
            if (DomainPower(domain) >= topPower * 0.55) chance /= 3;
            return random.Next(100) < chance;
        }

        private int TargetRealmGroupCount(string scale, int domainCount, RegionGenerationOptions options)
        {
            if (domainCount <= 1) return domainCount;
            int baseCount = Math.Max(1, options == null ? 3 : options.RealmCount);

            if (string.Equals(scale, "ManySmall", StringComparison.OrdinalIgnoreCase))
            {
                return Clamp((int)Math.Ceiling(domainCount / 2.4), 1, domainCount);
            }

            if (string.Equals(scale, "FewLarge", StringComparison.OrdinalIgnoreCase))
            {
                return Clamp(Math.Max(1, Math.Min(baseCount, (int)Math.Ceiling(domainCount / 14.0))), 1, domainCount);
            }

            return Clamp(Math.Max(baseCount, (int)Math.Ceiling(domainCount / 7.0)), 1, domainCount);
        }

        private IEnumerable<DomainRecord> OrderDomainsSpatially(HexMapRecord map, IEnumerable<DomainRecord> domains)
        {
            return domains
                .OrderBy(d => DomainAnchorSortY(map, d))
                .ThenBy(d => DomainAnchorSortX(map, d))
                .ThenByDescending(DomainPower);
        }

        private RealmBuildNode BuildRealmTitlePyramid(HexMapRecord map, List<DomainRecord> domains, RegionGenerationOptions options, Random random, string scale)
        {
            List<RealmBuildNode> current = domains
                .Select(d => CreateBaronyRealmNode(map, d, options, random))
                .ToList();

            while (current.Count > 1 && RealmTierRank(current[0].Realm.Tier) < 6)
            {
                string parentTier = NextHierarchyTier(current[0].Realm.Tier);
                int groupSize = Math.Max(1, ParentRealmGroupSize(parentTier, scale));
                List<RealmBuildNode> parents = new List<RealmBuildNode>();
                for (int i = 0; i < current.Count; i += groupSize)
                {
                    int take = Math.Min(groupSize, current.Count - i);
                    parents.Add(CreateParentRealmNode(map, current.GetRange(i, take), parentTier, options, random, "Generated realm hierarchy."));
                }

                current = parents;
            }

            return current.FirstOrDefault();
        }

        private RealmBuildNode CreateBaronyRealmNode(HexMapRecord map, DomainRecord domain, RegionGenerationOptions options, Random random)
        {
            MapSettlementRecord capital = SettlementById(map, domain.CapitalSettlementId);
            RealmRecord realm = CreateRealmForDomain(domain, capital, options, random);
            realm.Tier = "Barony";
            usedRealmNames.Remove(realm.Name);
            realm.Name = GenerateUniqueRealmName(random, realm.CultureKey, capital == null ? domain.Name : capital.Name, realm.Tier);
            map.Realms.Add(realm);
            domain.RealmId = realm.Id;
            return new RealmBuildNode { Realm = realm, Domains = new List<DomainRecord> { domain } };
        }

        private RealmBuildNode CreateParentRealmNode(HexMapRecord map, List<RealmBuildNode> children, string tier, RegionGenerationOptions options, Random random, string note)
        {
            List<DomainRecord> childDomains = children.SelectMany(c => c.Domains).ToList();
            DomainRecord anchor = BestDomainByPower(childDomains);
            MapSettlementRecord capital = anchor == null ? null : SettlementById(map, anchor.CapitalSettlementId);
            string culture = anchor == null ? options.CultureKey : CultureForDomain(anchor, options);
            RealmRecord parent = new RealmRecord
            {
                Tier = tier,
                CultureKey = culture,
                CapitalSettlementId = anchor == null ? "" : anchor.CapitalSettlementId,
                ColorArgb = anchor == null ? unchecked((int)0x66547AA5) : anchor.ColorArgb,
                Notes = note
            };

            parent.Name = GenerateUniqueRealmName(random, culture, capital == null ? (anchor == null ? "" : anchor.Name) : capital.Name, tier);
            if (anchor != null && anchor.Ruler != null && anchor.Ruler.Snapshot != null)
            {
                parent.RulerName = anchor.Ruler.Snapshot.Name;
                parent.RulerLevel = anchor.Ruler.Snapshot.Level;
            }

            map.Realms.Add(parent);
            foreach (RealmBuildNode child in children)
            {
                AddGeneratedVassalLink(map, parent, child.Realm, child.Domains, random);
            }

            return new RealmBuildNode { Realm = parent, Domains = childDomains };
        }

        private DomainRecord BestDomainByPower(IEnumerable<DomainRecord> domains)
        {
            DomainRecord best = null;
            int bestPower = int.MinValue;
            foreach (DomainRecord domain in domains)
            {
                if (domain == null) continue;
                int power = DomainPower(domain);
                if (best != null && power <= bestPower) continue;
                best = domain;
                bestPower = power;
            }

            return best;
        }

        private void AddGeneratedVassalLink(HexMapRecord map, RealmRecord liege, RealmRecord vassal, List<DomainRecord> vassalDomains, Random random)
        {
            int families = vassalDomains.Sum(d => d.PeasantFamilies + d.UrbanFamilies);
            map.VassalLinks.Add(new VassalLinkRecord
            {
                LiegeRealmId = liege.Id,
                VassalRealmId = vassal.Id,
                Loyalty = random.Next(-2, 3),
                TributeGp = (int)Math.Round(18.0 * Math.Pow(Math.Max(1, families), 0.6)),
                Notes = "Generated realm hierarchy."
            });
        }

        private string NextHierarchyTier(string tier)
        {
            if (string.Equals(tier, "Barony", StringComparison.OrdinalIgnoreCase)) return "County";
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return "County";
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return "Duchy";
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return "Kingdom";
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return "Kingdom";
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return "Empire";
            return "Empire";
        }

        private int ParentRealmGroupSize(string parentTier, string scale)
        {
            bool manySmall = string.Equals(scale, "ManySmall", StringComparison.OrdinalIgnoreCase);
            bool fewLarge = string.Equals(scale, "FewLarge", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scale, "OneState", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(parentTier, "County", StringComparison.OrdinalIgnoreCase)) return manySmall ? 2 : fewLarge ? 4 : 3;
            if (string.Equals(parentTier, "Duchy", StringComparison.OrdinalIgnoreCase)) return manySmall ? 3 : fewLarge ? 5 : 4;
            if (string.Equals(parentTier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return manySmall ? 4 : fewLarge ? 5 : 3;
            return manySmall ? 5 : 4;
        }

        private string DomainPolityKey(DomainRecord domain)
        {
            if (domain == null) return "Human";

            string type = domain.DomainType ?? "";
            string race = DomainRace(domain);
            if (string.Equals(type, "Dwarven Vault", StringComparison.OrdinalIgnoreCase) || race == "Dwarf") return "Dwarf";
            if (string.Equals(type, "Elven Fastness", StringComparison.OrdinalIgnoreCase) || race == "Elf") return "Elf";
            if (string.Equals(type, "Transitional", StringComparison.OrdinalIgnoreCase)) return "Transitional";
            if (string.Equals(type, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                if (race == "Orc") return "Orc";
                if (race == "Beastman") return "Beastman";
                return "HumanClan";
            }

            return "Human";
        }

        private string RealmScaleForPolity(RegionGenerationOptions options, string polityKey)
        {
            string specific = SpecificRealmScaleForPolity(options, polityKey);
            if (!string.IsNullOrWhiteSpace(specific) && !string.Equals(specific, "Default", StringComparison.OrdinalIgnoreCase))
            {
                return specific;
            }

            if (string.Equals(polityKey, "HumanClan", StringComparison.OrdinalIgnoreCase)
                || string.Equals(polityKey, "Orc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(polityKey, "Beastman", StringComparison.OrdinalIgnoreCase))
            {
                return "Independent";
            }

            return string.IsNullOrWhiteSpace(options.RealmScale) ? "Balanced" : options.RealmScale;
        }

        private string SpecificRealmScaleForPolity(RegionGenerationOptions options, string polityKey)
        {
            if (options == null) return "Default";
            if (string.Equals(polityKey, "Dwarf", StringComparison.OrdinalIgnoreCase)) return options.DwarvenRealmScale;
            if (string.Equals(polityKey, "Elf", StringComparison.OrdinalIgnoreCase)) return options.ElvenRealmScale;
            if (string.Equals(polityKey, "HumanClan", StringComparison.OrdinalIgnoreCase)) return options.HumanClanRealmScale;
            if (string.Equals(polityKey, "Orc", StringComparison.OrdinalIgnoreCase)) return options.OrcRealmScale;
            if (string.Equals(polityKey, "Beastman", StringComparison.OrdinalIgnoreCase)) return options.BeastmanRealmScale;
            if (string.Equals(polityKey, "Transitional", StringComparison.OrdinalIgnoreCase)) return options.TransitionalRealmScale;
            return options.HumanRealmScale;
        }

        private int DistanceBetweenDomains(HexMapRecord map, DomainRecord a, DomainRecord b)
        {
            Point? pa = DomainAnchorPoint(map, a);
            Point? pb = DomainAnchorPoint(map, b);
            if (!pa.HasValue || !pb.HasValue) return 999;
            return HexDistance(pa.Value.X, pa.Value.Y, pb.Value.X, pb.Value.Y);
        }

        private int DomainAnchorSortX(HexMapRecord map, DomainRecord domain)
        {
            Point? point = DomainAnchorPoint(map, domain);
            return point.HasValue ? point.Value.X : 999;
        }

        private int DomainAnchorSortY(HexMapRecord map, DomainRecord domain)
        {
            Point? point = DomainAnchorPoint(map, domain);
            return point.HasValue ? point.Value.Y : 999;
        }

        private int DomainPower(DomainRecord domain)
        {
            return domain == null ? 0 : domain.PeasantFamilies + domain.UrbanFamilies * 3;
        }

        private void EnsureGeneratedRealmTierOrder(HexMapRecord map, RegionGenerationOptions options, Random random)
        {
            if (map == null || map.Realms == null || map.VassalLinks == null || map.VassalLinks.Count == 0) return;

            Dictionary<string, RealmRecord> byId = map.Realms
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
                .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> directVassalCounts = map.VassalLinks
                .Where(l => l != null && !string.IsNullOrWhiteSpace(l.LiegeRealmId))
                .GroupBy(l => l.LiegeRealmId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            bool changed = true;
            int guard = 0;
            while (changed && guard++ < 12)
            {
                changed = false;
                foreach (VassalLinkRecord link in map.VassalLinks.Where(l => l != null))
                {
                    RealmRecord liege;
                    RealmRecord vassal;
                    if (!byId.TryGetValue(link.LiegeRealmId, out liege) || !byId.TryGetValue(link.VassalRealmId, out vassal)) continue;

                    int directVassals;
                    directVassalCounts.TryGetValue(liege.Id, out directVassals);
                    int directVassalRank = directVassals >= 6 ? 3 : 1;
                    int requiredRank = Math.Max(RealmTierRank(vassal.Tier) + 1, directVassalRank);
                    string requiredTier = RealmTierFromRank(requiredRank);
                    if (RealmTierRank(liege.Tier) < RealmTierRank(requiredTier))
                    {
                        liege.Tier = requiredTier;
                        changed = true;
                    }
                }
            }

            usedRealmNames.Clear();
            foreach (RealmRecord realm in map.Realms.Where(r => r != null))
            {
                string capitalName = SettlementNameById(map, realm.CapitalSettlementId);
                realm.Name = GenerateUniqueRealmName(random, string.IsNullOrWhiteSpace(realm.CultureKey) ? options.CultureKey : realm.CultureKey, capitalName, realm.Tier);
            }
        }

        private void UpdateRealmRulersFromDomains(HexMapRecord map)
        {
            if (map == null || map.Realms == null || map.Domains == null) return;

            Dictionary<string, List<VassalLinkRecord>> vassalsByLiege = map.VassalLinks == null
                ? new Dictionary<string, List<VassalLinkRecord>>(StringComparer.OrdinalIgnoreCase)
                : map.VassalLinks
                    .Where(l => l != null && !string.IsNullOrWhiteSpace(l.LiegeRealmId))
                    .GroupBy(l => l.LiegeRealmId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<DomainRecord>> domainsByRealm = map.Domains
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.RealmId))
                .GroupBy(d => d.RealmId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (RealmRecord realm in map.Realms.Where(r => r != null))
            {
                DomainRecord rulerDomain = DomainsForRealmAndVassals(map, realm, vassalsByLiege, domainsByRealm)
                    .OrderByDescending(d => d.UrbanFamilies * 3 + d.PeasantFamilies)
                    .FirstOrDefault();
                if (rulerDomain == null || rulerDomain.Ruler == null || rulerDomain.Ruler.Snapshot == null) continue;

                realm.RulerName = rulerDomain.Ruler.Snapshot.Name;
                realm.RulerLevel = rulerDomain.Ruler.Snapshot.Level;
            }
        }

        private List<DomainRecord> DomainsForRealmAndVassals(
            HexMapRecord map,
            RealmRecord realm,
            Dictionary<string, List<VassalLinkRecord>> vassalsByLiege,
            Dictionary<string, List<DomainRecord>> domainsByRealm)
        {
            if (map == null || realm == null || map.Domains == null) return new List<DomainRecord>();
            HashSet<string> realmIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Queue<string> queue = new Queue<string>();
            realmIds.Add(realm.Id);
            queue.Enqueue(realm.Id);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                List<VassalLinkRecord> links;
                if (vassalsByLiege == null || !vassalsByLiege.TryGetValue(current, out links)) continue;
                foreach (VassalLinkRecord link in links)
                {
                    if (realmIds.Add(link.VassalRealmId)) queue.Enqueue(link.VassalRealmId);
                }
            }

            List<DomainRecord> result = new List<DomainRecord>();
            foreach (string realmId in realmIds)
            {
                List<DomainRecord> domains;
                if (domainsByRealm != null && domainsByRealm.TryGetValue(realmId, out domains))
                {
                    result.AddRange(domains);
                }
            }

            return result;
        }

        private int RealmTierRank(string tier)
        {
            if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private string RealmTierFromRank(int rank)
        {
            if (rank >= 6) return "Empire";
            if (rank == 5) return "Kingdom";
            if (rank == 4) return "Principality";
            if (rank == 3) return "Duchy";
            if (rank == 2) return "County";
            if (rank == 1) return "Viscounty";
            return "Barony";
        }

        private Point? DomainAnchorPoint(HexMapRecord map, DomainRecord domain)
        {
            if (map == null || domain == null) return null;
            Point? cached;
            if (realmDomainAnchorCache != null && realmDomainAnchorCache.TryGetValue(domain, out cached)) return cached;

            Point? result = null;
            MapSettlementRecord settlement = SettlementById(map, domain.CapitalSettlementId);
            if (settlement != null) result = new Point(settlement.Q, settlement.R);
            else if (domain.StrongholdQ >= 0 && domain.StrongholdR >= 0) result = new Point(domain.StrongholdQ, domain.StrongholdR);
            else
            {
                DomainHexRecord hex = domain.Hexes == null ? null : domain.Hexes.FirstOrDefault();
                result = hex == null ? (Point?)null : new Point(hex.Q, hex.R);
            }

            if (realmDomainAnchorCache != null) realmDomainAnchorCache[domain] = result;
            return result;
        }

        private MapSettlementRecord SettlementById(HexMapRecord map, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            MapSettlementRecord settlement;
            if (realmSettlementById != null && realmSettlementById.TryGetValue(id, out settlement)) return settlement;
            if (map == null || map.Settlements == null) return null;
            return map.Settlements.FirstOrDefault(s => s != null && string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private RealmRecord CreateRealmForDomain(DomainRecord domain, MapSettlementRecord capital, RegionGenerationOptions options, Random random)
        {
            string race = DomainRace(domain);
            string culture = CultureForDomain(domain, options);
            return new RealmRecord
            {
                Name = GenerateUniqueRealmName(random, culture, capital == null ? domain.Name : capital.Name, RealmTierForDomainCount(1)),
                Tier = "County",
                CultureKey = culture,
                CapitalSettlementId = domain.CapitalSettlementId,
                RulerName = domain.Ruler == null || domain.Ruler.Snapshot == null ? "" : domain.Ruler.Snapshot.Name,
                RulerLevel = domain.Ruler == null || domain.Ruler.Snapshot == null ? 7 : domain.Ruler.Snapshot.Level,
                ColorArgb = domain.ColorArgb,
                Notes = "Generated from " + race + " domain " + domain.Name + "."
            };
        }

        private string DomainRace(DomainRecord domain)
        {
            return domain == null ? "Human" : NormalizeGeneratedRace(domain.Race);
        }

        private string SettlementNameById(HexMapRecord map, string id)
        {
            MapSettlementRecord settlement = SettlementById(map, id);
            return settlement == null ? "" : settlement.Name;
        }

        private string RealmTierForDomainCount(int domainCount)
        {
            if (domainCount >= 72) return "Empire";
            if (domainCount >= 48) return "Kingdom";
            if (domainCount >= 24) return "Principality";
            if (domainCount >= 9) return "Duchy";
            if (domainCount >= 3) return "County";
            if (domainCount == 2) return "Viscounty";
            return "Barony";
        }
    }
}
