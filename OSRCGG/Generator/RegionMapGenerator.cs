﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace OSRCGG
{
    internal sealed partial class RegionMapGenerator
    {
        private readonly NameGenerationService names;
        private Dictionary<string, HexCellRecord> cellIndex = new Dictionary<string, HexCellRecord>();
        private readonly List<HexCellRecord> riverDeadEnds = new List<HexCellRecord>();
        private readonly HashSet<string> usedSettlementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> usedDomainNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> usedRealmNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> usedFeatureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const string ClanholdSeedMarker = "Generated Clanhold";
        private const string DwarvenDomainSeedMarker = "Generated Dwarven Domain";
        private const string ElvenDomainSeedMarker = "Generated Elven Domain";
        private bool useRussianNames;
        private CancellationToken generationCancellationToken;
        private IProgress<RegionGenerationProgress> generationProgress;

        private static readonly int[][] OddRowDirections =
        {
            new[] { 1, 0 },
            new[] { -1, 0 },
            new[] { 1, -1 },
            new[] { 0, -1 },
            new[] { 1, 1 },
            new[] { 0, 1 }
        };

        private static readonly int[][] EvenRowDirections =
        {
            new[] { 1, 0 },
            new[] { -1, 0 },
            new[] { 0, -1 },
            new[] { -1, -1 },
            new[] { 0, 1 },
            new[] { -1, 1 }
        };

        private sealed class ZoneCenter
        {
            public int Q { get; set; }
            public int R { get; set; }
            public string Value { get; set; }
            public double Weight { get; set; }
        }

        private sealed class RoadConnectionCandidate
        {
            public MapSettlementRecord From { get; set; }
            public MapSettlementRecord To { get; set; }
            public double Score { get; set; }
        }

        private sealed class WeightedTextOption
        {
            public string Value { get; set; }
            public int Weight { get; set; }
        }

        public RegionMapGenerator(NameGenerationService names)
        {
            this.names = names;
        }

        public GeneratedRegionResult Generate(RegionGenerationOptions rawOptions)
        {
            return Generate(rawOptions, null, CancellationToken.None);
        }

        public GeneratedRegionResult Generate(
            RegionGenerationOptions rawOptions,
            IProgress<RegionGenerationProgress> progress,
            CancellationToken cancellationToken)
        {
            CancellationToken previousToken = generationCancellationToken;
            IProgress<RegionGenerationProgress> previousProgress = generationProgress;
            generationCancellationToken = cancellationToken;
            generationProgress = progress;

            try
            {
                return GenerateCore(rawOptions);
            }
            finally
            {
                generationCancellationToken = previousToken;
                generationProgress = previousProgress;
            }
        }

        public GeneratedRegionResult RegenerateCivilization(
            HexMapRecord source,
            RegionGenerationOptions rawOptions,
            IProgress<RegionGenerationProgress> progress,
            CancellationToken cancellationToken)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            CancellationToken previousToken = generationCancellationToken;
            IProgress<RegionGenerationProgress> previousProgress = generationProgress;
            generationCancellationToken = cancellationToken;
            generationProgress = progress;

            try
            {
                return RegenerateCivilizationCore(source, rawOptions);
            }
            finally
            {
                generationCancellationToken = previousToken;
                generationProgress = previousProgress;
            }
        }

        private GeneratedRegionResult GenerateCore(RegionGenerationOptions rawOptions)
        {
            CheckCancellation();
            RegionGenerationOptions options = NormalizeOptions(rawOptions);
            Random random = new Random(StableSeed(options.Seed));
            usedSettlementNames.Clear();
            usedDomainNames.Clear();
            usedRealmNames.Clear();
            usedFeatureNames.Clear();
            useRussianNames = options.UseRussianNames;
            GeneratedRegionResult result = new GeneratedRegionResult();

            HexMapRecord map = new HexMapRecord
            {
                Name = GenerateUniqueRealmName(random, options.CultureKey, "", "Region"),
                Width = options.Width,
                Height = options.Height,
                UpdatedAt = DateTime.Now
            };

            // Сначала создаем саму сетку: последующие слои только уточняют данные уже существующих гексов.
            ReportProgress(1, "Creating hex grid");
            for (int r = 0; r < options.Height; r++)
            {
                for (int q = 0; q < options.Width; q++)
                {
                    map.Cells.Add(new HexCellRecord { Q = q, R = r });
                }

                if ((r & 7) == 0) CheckCancellation();
            }

            cellIndex = map.Cells.ToDictionary(c => CellKey(c.Q, c.R));
            riverDeadEnds.Clear();

            ReportProgress(5, "Generating elevation");
            GenerateElevation(map, options, random);
            CheckCancellation();
            ReportProgress(15, "Generating seas and oceans");
            GenerateLargeWater(map, options, random);
            NormalizeLargeWater(map, options);
            CheckCancellation();
            ReportProgress(24, "Refining elevation");
            RefineElevationAfterWater(map, options, random);
            CheckCancellation();
            ReportProgress(32, "Generating terrain");
            GenerateTerrain(map, options, random);
            CheckCancellation();
            ReportProgress(42, "Generating rivers");
            if (options.GenerateRivers) GenerateRivers(map, options, random);
            GenerateLakes(map, options, random);
            NormalizeGeneratedHydrology(map, options);
            NormalizeWaterSurfaces(map);
            CheckCancellation();
            ReportProgress(55, "Generating map names");
            if (options.GenerateFeatureNames) GenerateFeatureNamesForMap(map, options.CultureKey, options.UseRussianNames, random);
            CheckCancellation();
            ReportProgress(63, "Generating settlements");
            if (options.GenerateSettlements) GenerateSettlements(map, options, random);
            CheckCancellation();
            ReportProgress(73, "Generating domains");
            if (options.GenerateDomains) GenerateDomains(map, options, random);
            CheckCancellation();
            ReportProgress(84, "Generating roads");
            if (options.GenerateRoads) GenerateRoads(map, options, random);
            CheckCancellation();
            ReportProgress(93, "Generating realms");
            if (options.GenerateRealms) GenerateRealms(map, options, random);
            CheckCancellation();
            ReportProgress(97, "Generating hex features");
            if (options.GenerateHexFeatures) GenerateHexFeatures(map, options, random);
            ReportProgress(100, "Region generated");

            result.Map = map;
            result.Log.Add("Seed: " + options.Seed);
            result.Log.Add("Cells: " + map.Cells.Count);
            result.Log.Add("Settlements: " + map.Settlements.Count);
            result.Log.Add("Domains: " + map.Domains.Count);
            result.Log.Add("Realms: " + map.Realms.Count + ", vassal links: " + map.VassalLinks.Count);
            result.Log.Add("Hex features: " + map.Features.Count + ", dungeons: " + map.Dungeons.Count);
            return result;
        }

        private GeneratedRegionResult RegenerateCivilizationCore(HexMapRecord source, RegionGenerationOptions rawOptions)
        {
            CheckCancellation();
            HexMapRecord map = XmlSerialization.Clone(source);
            if (map == null) map = new HexMapRecord();
            EnsureMapLists(map);

            RegionGenerationOptions options = NormalizeRegenerationOptions(rawOptions, map);
            Random random = new Random(StableSeed(options.Seed));

            cellIndex = map.Cells.ToDictionary(c => CellKey(c.Q, c.R));
            riverDeadEnds.Clear();
            ResetUsedNameSets(map, options);

            ReportProgress(8, "Preparing selected layers");

            // Частичная регенерация работает слоями поверх уже существующих гексов:
            // Terrain/Elevation/Water не пересоздаются, а зависимые слои чистятся только при выбранном rebuild.
            bool rebuildRivers = options.GenerateRivers;
            bool rebuildSettlements = options.GenerateSettlements;
            bool rebuildDomains = rebuildSettlements || options.GenerateDomains;
            bool rebuildStrongholdsOnly = !rebuildDomains && options.GenerateStrongholds;
            bool rebuildRoads = rebuildSettlements || options.GenerateRoads;
            bool rebuildRealms = rebuildDomains || options.GenerateRealms;
            bool rebuildFeatures = options.GenerateHexFeatures || options.GenerateDungeons;

            if (rebuildRivers)
            {
                map.Rivers.Clear();
            }

            if (rebuildSettlements)
            {
                map.Settlements.Clear();
            }

            if (rebuildDomains)
            {
                map.Domains.Clear();
            }

            if (rebuildRoads)
            {
                map.Roads.Clear();
            }

            if (rebuildRealms)
            {
                map.Realms.Clear();
                map.VassalLinks.Clear();
            }

            if (rebuildFeatures)
            {
                map.Features.Clear();
                map.Dungeons.Clear();
            }

            if (rebuildRivers)
            {
                ReportProgress(18, "Regenerating rivers");
                riverDeadEnds.Clear();
                GenerateRivers(map, options, random);
                NormalizeGeneratedHydrology(map, options);
            }

            CheckCancellation();
            if (options.GenerateFeatureNames)
            {
                ReportProgress(24, "Regenerating map names");
                GenerateFeatureNamesForMap(map, options.CultureKey, options.UseRussianNames, random);
            }

            if (options.GenerateSettlements)
            {
                ReportProgress(35, "Regenerating settlements");
                GenerateSettlements(map, options, random);
            }

            CheckCancellation();
            if (options.GenerateDomains)
            {
                ReportProgress(55, "Regenerating domains");
                GenerateDomains(map, options, random);
            }
            else if (rebuildStrongholdsOnly)
            {
                ReportProgress(55, "Regenerating strongholds");
                RegenerateStrongholdsForExistingDomains(map, options, random);
            }

            CheckCancellation();
            if (options.GenerateRulers && !options.GenerateDomains)
            {
                ReportProgress(65, "Regenerating rulers");
                RegenerateRulersForExistingDomains(map, options, random);
            }

            CheckCancellation();
            if (options.GenerateRoads)
            {
                ReportProgress(75, "Regenerating roads");
                GenerateRoads(map, options, random);
            }

            CheckCancellation();
            if (options.GenerateRealms)
            {
                ReportProgress(92, "Regenerating realms");
                GenerateRealms(map, options, random);
            }
            else if (options.GenerateRulers)
            {
                UpdateRealmRulersFromDomains(map);
            }

            CheckCancellation();
            if (rebuildFeatures)
            {
                ReportProgress(96, "Regenerating hex features");
                GenerateHexFeatures(map, options, random);
            }

            map.UpdatedAt = DateTime.Now;
            ReportProgress(100, "Selected layers regenerated");

            GeneratedRegionResult result = new GeneratedRegionResult();
            result.Map = map;
            result.Log.Add("Seed: " + options.Seed);
            result.Log.Add("Landscape preserved: " + map.Cells.Count + " cells");
            result.Log.Add("Rivers: " + map.Rivers.Count);
            result.Log.Add("Settlements: " + map.Settlements.Count);
            result.Log.Add("Domains: " + map.Domains.Count);
            result.Log.Add("Roads: " + map.Roads.Count);
            result.Log.Add("Realms: " + map.Realms.Count + ", vassal links: " + map.VassalLinks.Count);
            result.Log.Add("Hex features: " + map.Features.Count + ", dungeons: " + map.Dungeons.Count);
            return result;
        }

        private RegionGenerationOptions NormalizeRegenerationOptions(RegionGenerationOptions source, HexMapRecord map)
        {
            RegionGenerationOptions options = source ?? new RegionGenerationOptions();
            options.Width = map == null || map.Width <= 0 ? Math.Max(6, options.Width) : map.Width;
            options.Height = map == null || map.Height <= 0 ? Math.Max(6, options.Height) : map.Height;
            if (string.IsNullOrWhiteSpace(options.Seed)) options.Seed = DateTime.Now.ToString("yyyyMMddHHmmss");
            if (string.IsNullOrWhiteSpace(options.ClimateBelt)) options.ClimateBelt = "Temperate";
            if (string.IsNullOrWhiteSpace(options.CivilizationLevel)) options.CivilizationLevel = "Borderlands";
            if (string.IsNullOrWhiteSpace(options.RealmScale)) options.RealmScale = "Balanced";
            NormalizeRealmProfileOptions(options);
            if (string.IsNullOrWhiteSpace(options.CultureKey)) options.CultureKey = "english";
            if (string.IsNullOrWhiteSpace(options.WaterLayout)) options.WaterLayout = "Coast";
            if (string.IsNullOrWhiteSpace(options.Seismicity)) options.Seismicity = "Normal";
            if (string.IsNullOrWhiteSpace(options.LandValueMode)) options.LandValueMode = "Fixed6";
            if (!options.GenerateHexFeatures) options.GenerateDungeons = false;
            if (options.GenerateSpecialDomains
                && !options.GenerateDwarvenDomains
                && !options.GenerateElvenDomains
                && !options.GenerateClanDomains
                && !options.GenerateTransitionalDomains)
            {
                options.GenerateDwarvenDomains = true;
                options.GenerateElvenDomains = true;
                options.GenerateClanDomains = true;
                options.GenerateTransitionalDomains = true;
            }

            options.GenerateSpecialDomains = options.GenerateDwarvenDomains
                || options.GenerateElvenDomains
                || options.GenerateClanDomains
                || options.GenerateTransitionalDomains;
            if (!options.AdvancedMode)
            {
                int specialDomainPercent = options.SpecialDomainPercent;
                int dwarvenDomainWeight = options.DwarvenDomainWeight;
                int elvenDomainWeight = options.ElvenDomainWeight;
                int clanDomainWeight = options.ClanDomainWeight;
                int transitionalDomainWeight = options.TransitionalDomainWeight;
                // Обычная перегенерация должна пользоваться теми же пресетами освоенности,
                // что и полная генерация карты; иначе "Пограничье" превращается в плотную
                // цивилизованную сетку только потому, что числовые поля были скрыты.
                ApplySimplePreset(options);
                if (options.UseSpecialDomainWeights)
                {
                    options.SpecialDomainPercent = specialDomainPercent;
                    options.DwarvenDomainWeight = dwarvenDomainWeight;
                    options.ElvenDomainWeight = elvenDomainWeight;
                    options.ClanDomainWeight = clanDomainWeight;
                    options.TransitionalDomainWeight = transitionalDomainWeight;
                }
            }

            bool hasExistingSettlements = map != null && map.Settlements != null && map.Settlements.Count > 0;
            bool hasExistingStrongholds = map != null && map.Domains != null && map.Domains.Any(d => d != null && d.StrongholdQ >= 0 && d.StrongholdR >= 0);
            bool hasExistingDomains = map != null && map.Domains != null && map.Domains.Count > 0;

            // Здесь повторяется защита диалога: генератор нельзя сломать программным
            // вызовом с опциями, для которых на карте нет необходимых входных данных.
            bool placesAvailable = hasExistingSettlements || hasExistingStrongholds || options.GenerateSettlements || options.GenerateStrongholds;
            if (!placesAvailable)
            {
                options.GenerateDomains = false;
                options.GenerateRoads = false;
            }

            bool domainsAvailable = hasExistingDomains || options.GenerateDomains;
            if (!domainsAvailable)
            {
                options.GenerateRealms = false;
                options.GenerateRulers = false;
            }

            if (!options.GenerateDomains)
            {
                options.GenerateTransitionalDomains = false;
                if (!options.GenerateSpecialSettlementsWithoutDomains)
                {
                    options.GenerateDwarvenDomains = false;
                    options.GenerateElvenDomains = false;
                    options.GenerateClanDomains = false;
                }
            }

            if (!options.GenerateSettlements)
            {
                options.GenerateSpecialSettlementsWithoutDomains = false;
            }

            options.GenerateSpecialDomains = options.GenerateDwarvenDomains
                || options.GenerateElvenDomains
                || options.GenerateClanDomains
                || options.GenerateTransitionalDomains;
            if (!options.GenerateDwarvenDomains) options.UseDwarvenCultureNames = false;
            if (!options.GenerateElvenDomains) options.UseElvenCultureNames = false;
            if (!options.GenerateClanDomains) options.UseClanCultureNames = false;
            if (!options.GenerateTransitionalDomains) options.UseTransitionalCultureNames = false;

            options.RiverPercent = Clamp(options.RiverPercent, 0, 100);
            options.SettlementDensityPercent = Clamp(options.SettlementDensityPercent, 0, 100);
            options.DomainCoveragePercent = Clamp(options.DomainCoveragePercent, 0, 100);
            options.RealmCount = Clamp(options.RealmCount, 1, 20);
            options.StateSizeVariancePercent = Clamp(options.StateSizeVariancePercent, 0, 100);
            options.SpecialDomainPercent = Clamp(options.SpecialDomainPercent, 0, 100);
            options.DwarvenDomainWeight = Clamp(options.DwarvenDomainWeight, 0, 100);
            options.ElvenDomainWeight = Clamp(options.ElvenDomainWeight, 0, 100);
            options.ClanDomainWeight = Clamp(options.ClanDomainWeight, 0, 100);
            options.TransitionalDomainWeight = Clamp(options.TransitionalDomainWeight, 0, 100);
            options.DefaultAgeIndex = Clamp(options.DefaultAgeIndex, -1, 4);
            options.Seismicity = NormalizeSeismicity(options.Seismicity);
            return options;
        }

        private void EnsureMapLists(HexMapRecord map)
        {
            if (map.Cells == null) map.Cells = new List<HexCellRecord>();
            if (map.Settlements == null) map.Settlements = new List<MapSettlementRecord>();
            if (map.Roads == null) map.Roads = new List<MapEdgeRecord>();
            if (map.Rivers == null) map.Rivers = new List<MapEdgeRecord>();
            if (map.Domains == null) map.Domains = new List<DomainRecord>();
            if (map.Realms == null) map.Realms = new List<RealmRecord>();
            if (map.VassalLinks == null) map.VassalLinks = new List<VassalLinkRecord>();
            if (map.Features == null) map.Features = new List<HexFeatureRecord>();
            if (map.Dungeons == null) map.Dungeons = new List<DungeonRecord>();
        }

        private void ResetUsedNameSets(HexMapRecord map, RegionGenerationOptions options)
        {
            usedSettlementNames.Clear();
            usedDomainNames.Clear();
            usedRealmNames.Clear();
            usedFeatureNames.Clear();
            useRussianNames = options != null && options.UseRussianNames;

            if (map == null) return;
            if (map.Settlements != null)
            {
                foreach (MapSettlementRecord settlement in map.Settlements)
                {
                    if (settlement != null && !string.IsNullOrWhiteSpace(settlement.Name)) usedSettlementNames.Add(settlement.Name);
                }
            }

            if (map.Domains != null)
            {
                foreach (DomainRecord domain in map.Domains)
                {
                    if (domain != null && !string.IsNullOrWhiteSpace(domain.Name)) usedDomainNames.Add(domain.Name);
                }
            }

            if (map.Realms != null)
            {
                foreach (RealmRecord realm in map.Realms)
                {
                    if (realm != null && !string.IsNullOrWhiteSpace(realm.Name)) usedRealmNames.Add(realm.Name);
                }
            }

            if (map.Cells == null) return;
            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell == null) continue;
                if (!string.IsNullOrWhiteSpace(cell.WaterFeatureName)) usedFeatureNames.Add(cell.WaterFeatureName);
            }

            foreach (MapEdgeRecord edge in (map.Roads ?? new List<MapEdgeRecord>()).Concat(map.Rivers ?? new List<MapEdgeRecord>()))
            {
                if (edge != null && !string.IsNullOrWhiteSpace(edge.FeatureName)) usedFeatureNames.Add(edge.FeatureName);
            }
        }

        private void ReportProgress(int percent, string message)
        {
            if (generationProgress != null)
            {
                generationProgress.Report(new RegionGenerationProgress(percent, message));
            }
        }

        private void CheckCancellation()
        {
            generationCancellationToken.ThrowIfCancellationRequested();
        }

        private RegionGenerationOptions NormalizeOptions(RegionGenerationOptions source)
        {
            RegionGenerationOptions options = source ?? new RegionGenerationOptions();
            options.Width = Math.Max(6, Math.Min(RegionGenerationOptions.MaxMapSize, options.Width));
            options.Height = Math.Max(6, Math.Min(RegionGenerationOptions.MaxMapSize, options.Height));
            if (string.IsNullOrWhiteSpace(options.Seed)) options.Seed = DateTime.Now.ToString("yyyyMMddHHmmss");
            if (string.IsNullOrWhiteSpace(options.ClimateBelt)) options.ClimateBelt = "Temperate";
            if (string.IsNullOrWhiteSpace(options.CivilizationLevel)) options.CivilizationLevel = "Borderlands";
            if (string.IsNullOrWhiteSpace(options.RealmScale)) options.RealmScale = "Balanced";
            NormalizeRealmProfileOptions(options);
            if (string.IsNullOrWhiteSpace(options.CultureKey)) options.CultureKey = "english";
            if (string.IsNullOrWhiteSpace(options.WaterLayout)) options.WaterLayout = "Coast";
            if (string.IsNullOrWhiteSpace(options.Seismicity)) options.Seismicity = "Normal";
            if (string.IsNullOrWhiteSpace(options.LandValueMode)) options.LandValueMode = "Fixed6";
            if (!options.GenerateHexFeatures) options.GenerateDungeons = false;
            bool hasGeneratedPlaces = options.GenerateSettlements || options.GenerateStrongholds;
            if (!hasGeneratedPlaces)
            {
                options.GenerateRoads = false;
                options.GenerateDomains = false;
            }

            if (!options.GenerateDomains)
            {
                options.GenerateRealms = false;
                options.GenerateRulers = false;
                options.GenerateTransitionalDomains = false;
                if (!options.GenerateSpecialSettlementsWithoutDomains)
                {
                    options.GenerateDwarvenDomains = false;
                    options.GenerateElvenDomains = false;
                    options.GenerateClanDomains = false;
                }
            }

            if (!options.GenerateSettlements)
            {
                options.GenerateSpecialSettlementsWithoutDomains = false;
            }

            if (options.GenerateSpecialDomains
                && !options.GenerateDwarvenDomains
                && !options.GenerateElvenDomains
                && !options.GenerateClanDomains
                && !options.GenerateTransitionalDomains)
            {
                options.GenerateDwarvenDomains = true;
                options.GenerateElvenDomains = true;
                options.GenerateClanDomains = true;
                options.GenerateTransitionalDomains = true;
            }
            options.GenerateSpecialDomains = options.GenerateDwarvenDomains
                || options.GenerateElvenDomains
                || options.GenerateClanDomains
                || options.GenerateTransitionalDomains;
            if (!options.GenerateDwarvenDomains) options.UseDwarvenCultureNames = false;
            if (!options.GenerateElvenDomains) options.UseElvenCultureNames = false;
            if (!options.GenerateClanDomains) options.UseClanCultureNames = false;
            if (!options.GenerateTransitionalDomains) options.UseTransitionalCultureNames = false;

            if (!options.AdvancedMode)
            {
                int specialDomainPercent = options.SpecialDomainPercent;
                int dwarvenDomainWeight = options.DwarvenDomainWeight;
                int elvenDomainWeight = options.ElvenDomainWeight;
                int clanDomainWeight = options.ClanDomainWeight;
                int transitionalDomainWeight = options.TransitionalDomainWeight;
                ApplySimplePreset(options);
                ApplySeismicityToElevationPercents(options);
                if (options.UseSpecialDomainWeights)
                {
                    options.SpecialDomainPercent = specialDomainPercent;
                    options.DwarvenDomainWeight = dwarvenDomainWeight;
                    options.ElvenDomainWeight = elvenDomainWeight;
                    options.ClanDomainWeight = clanDomainWeight;
                    options.TransitionalDomainWeight = transitionalDomainWeight;
                }
            }

            options.TerrainZoneCount = Clamp(options.TerrainZoneCount, 3, 40);
            options.TerrainChaosPercent = Clamp(options.TerrainChaosPercent, 0, 100);
            options.WaterPercent = Clamp(options.WaterPercent, 0, 80);
            options.LakePercent = Clamp(options.LakePercent, 0, 50);
            options.RiverPercent = Clamp(options.RiverPercent, 0, 100);
            options.HillsPercent = Clamp(options.HillsPercent, 0, 80);
            options.MountainsPercent = Clamp(options.MountainsPercent, 0, 60);
            if (options.AdvancedMode) ApplySeismicityToElevationPercents(options);
            options.SettlementDensityPercent = Clamp(options.SettlementDensityPercent, 0, 100);
            options.DomainCoveragePercent = Clamp(options.DomainCoveragePercent, 0, 100);
            options.RealmCount = Clamp(options.RealmCount, 1, 20);
            options.StateSizeVariancePercent = Clamp(options.StateSizeVariancePercent, 0, 100);
            options.SpecialDomainPercent = Clamp(options.SpecialDomainPercent, 0, 100);
            options.DwarvenDomainWeight = Clamp(options.DwarvenDomainWeight, 0, 100);
            options.ElvenDomainWeight = Clamp(options.ElvenDomainWeight, 0, 100);
            options.ClanDomainWeight = Clamp(options.ClanDomainWeight, 0, 100);
            options.TransitionalDomainWeight = Clamp(options.TransitionalDomainWeight, 0, 100);
            options.DefaultAgeIndex = Clamp(options.DefaultAgeIndex, -1, 4);
            options.Seismicity = NormalizeSeismicity(options.Seismicity);
            return options;
        }

        private void ApplySimplePreset(RegionGenerationOptions options)
        {
            int area = options.Width * options.Height;
            options.TerrainZoneCount = Math.Max(4, Math.Min(18, area / 45));
            options.TerrainChaosPercent = 35;
            options.RiverPercent = 35;
            options.HillsPercent = 20;
            options.MountainsPercent = string.Equals(options.ClimateBelt, "Cold", StringComparison.OrdinalIgnoreCase) ? 11 : 8;
            options.WaterPercent = PresetLargeWaterPercent(options.WaterLayout);

            if (string.Equals(options.ClimateBelt, "Arid", StringComparison.OrdinalIgnoreCase))
            {
                options.LakePercent = 1;
                options.RiverPercent = 12;
            }
            else if (string.Equals(options.ClimateBelt, "Tropical", StringComparison.OrdinalIgnoreCase))
            {
                options.LakePercent = 5;
                options.RiverPercent = 48;
            }
            else if (string.Equals(options.ClimateBelt, "Cold", StringComparison.OrdinalIgnoreCase))
            {
                options.LakePercent = 6;
            }
            else
            {
                options.LakePercent = 4;
            }

            if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase))
            {
                options.SettlementDensityPercent = 15;
                options.DomainCoveragePercent = 12;
                options.RealmCount = 1;
            }
            else if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase))
            {
                options.SettlementDensityPercent = 58;
                options.DomainCoveragePercent = 78;
                options.RealmCount = 4;
            }
            else
            {
                options.SettlementDensityPercent = 14;
                options.DomainCoveragePercent = 28;
                options.RealmCount = 3;
            }

            if (string.Equals(options.RealmScale, "FewLarge", StringComparison.OrdinalIgnoreCase))
            {
                options.RealmCount = Math.Max(1, options.RealmCount - 1);
                options.StateSizeVariancePercent = 25;
            }
            else if (string.Equals(options.RealmScale, "ManySmall", StringComparison.OrdinalIgnoreCase))
            {
                options.RealmCount = Math.Min(20, options.RealmCount + 6);
                options.StateSizeVariancePercent = 25;
            }
            else if (string.Equals(options.RealmScale, "OneState", StringComparison.OrdinalIgnoreCase))
            {
                options.RealmCount = 1;
                options.StateSizeVariancePercent = 0;
            }
            else
            {
                options.StateSizeVariancePercent = 65;
            }

            if (options.GenerateSpecialDomains)
            {
                if (string.Equals(options.CivilizationLevel, "Civilized", StringComparison.OrdinalIgnoreCase)) options.SpecialDomainPercent = 8;
                else if (string.Equals(options.CivilizationLevel, "Wild", StringComparison.OrdinalIgnoreCase)) options.SpecialDomainPercent = 30;
                else options.SpecialDomainPercent = 30;
            }
        }

        private string NormalizeSeismicity(string seismicity)
        {
            if (string.Equals(seismicity, "Stable", StringComparison.OrdinalIgnoreCase)) return "Stable";
            if (string.Equals(seismicity, "Seismic", StringComparison.OrdinalIgnoreCase)) return "Seismic";
            return "Normal";
        }

        private void ApplySeismicityToElevationPercents(RegionGenerationOptions options)
        {
            if (options == null) return;
            options.Seismicity = NormalizeSeismicity(options.Seismicity);
            if (options.Seismicity == "Stable")
            {
                options.MountainsPercent = Math.Max(0, options.MountainsPercent / 3);
                options.HillsPercent = Math.Max(6, options.HillsPercent * 2 / 3);
            }
            else if (options.Seismicity == "Seismic")
            {
                options.MountainsPercent = Math.Min(46, Math.Max(options.MountainsPercent + 9, options.MountainsPercent * 9 / 4));
                options.HillsPercent = Math.Min(64, Math.Max(options.HillsPercent + 8, options.HillsPercent * 4 / 3));
            }
        }

        private void NormalizeRealmProfileOptions(RegionGenerationOptions options)
        {
            if (options == null) return;

            if (string.IsNullOrWhiteSpace(options.HumanRealmScale)) options.HumanRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.DwarvenRealmScale)) options.DwarvenRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.ElvenRealmScale)) options.ElvenRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.HumanClanRealmScale)) options.HumanClanRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.OrcRealmScale)) options.OrcRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.BeastmanRealmScale)) options.BeastmanRealmScale = "Default";
            if (string.IsNullOrWhiteSpace(options.TransitionalRealmScale)) options.TransitionalRealmScale = "Default";
        }

    }
}
