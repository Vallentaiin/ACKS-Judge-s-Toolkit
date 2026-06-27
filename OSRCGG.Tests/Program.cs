using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OSRCGG;

namespace OSRCGG.Tests
{
    internal static class Program
    {
        private static int assertionCount;

        private sealed class TestCase
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public Action Action { get; set; }
            public string[] Tags { get; set; }

            public bool Matches(string token)
            {
                if (string.IsNullOrWhiteSpace(token)) return false;
                string normalized = token.Trim();
                if (string.Equals(Key, normalized, StringComparison.OrdinalIgnoreCase)) return true;
                if (Name != null && Name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return Tags != null && Tags.Any(tag => string.Equals(tag, normalized, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static void Main(string[] args)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "OSRCGG.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                List<TestCase> tests = BuildTestCases(tempRoot);
                if (args != null && args.Any(arg => string.Equals(arg, "--list", StringComparison.OrdinalIgnoreCase)))
                {
                    PrintTestList(tests);
                    return;
                }

                List<TestCase> selected = SelectTests(tests, args);
                if (selected.Count == 0)
                {
                    Console.Error.WriteLine("No tests matched. Use --list to see keys and tags.");
                    Environment.ExitCode = 2;
                    return;
                }

                foreach (TestCase test in selected)
                {
                    RunTest(test.Name, test.Action);
                }

                Console.WriteLine("OK: " + assertionCount + " assertions");
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        private static void RunTest(string name, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Console.WriteLine("RUN " + name);
            action();
            stopwatch.Stop();
            Console.WriteLine("DONE " + name + " (" + stopwatch.ElapsedMilliseconds + " ms)");
        }

        private static List<TestCase> BuildTestCases(string tempRoot)
        {
            return new List<TestCase>
            {
                NewTest("acks", "ACKS rules", TestAcksRules, "rules", "core"),
                NewTest("character-rules", "Character rules", TestCharacterRulesService, "character", "rules"),
                NewTest("character-generator", "Character generator", TestCharacterGenerator, "character"),
                NewTest("map-normalizer", "Map normalizer", TestMapDataNormalizer, "map"),
                NewTest("wilderness-hex", "Wilderness hex modifiers", TestWildernessHexModifiers, "map", "wilderness"),
                NewTest("demand", "Demand service", TestMapDemandService, "trade", "map"),
                NewTest("realms", "Realm titles and clan names", TestRealmTitlesAndClanNames, "realm", "names"),
                NewTest("region", "Region generator", TestRegionMapGeneratorCivilizationLayers, "region", "map"),
                NewTest("treasure", "Treasure generation", TestTreasureGeneration, "treasure", "dungeons"),
                NewTest("dungeon", "Dungeon generation", TestDungeonGeneration, "dungeons"),
                NewTest("hex-features", "Hex features", TestHexFeatures, "hex", "features", "map", "dungeons"),
                NewTest("xml", "XML record store", delegate { TestXmlRecordStore(tempRoot); }, "storage"),
                NewTest("trade-workbook", "Trade demand workbook", delegate { TestTradeDemandWorkbookService(tempRoot); }, "excel", "trade"),
                NewTest("map-workbook", "Map workbook", delegate { TestMapWorkbookService(tempRoot); }, "excel", "map", "dungeons")
            };
        }

        private static TestCase NewTest(string key, string name, Action action, params string[] tags)
        {
            return new TestCase { Key = key, Name = name, Action = action, Tags = tags ?? new string[0] };
        }

        private static List<TestCase> SelectTests(List<TestCase> tests, string[] args)
        {
            if (args == null || args.Length == 0) return tests;
            string[] tokens = args
                .Where(arg => !string.IsNullOrWhiteSpace(arg) && !arg.StartsWith("--", StringComparison.Ordinal))
                .SelectMany(arg => arg.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(arg => arg.Trim())
                .Where(arg => arg.Length > 0)
                .ToArray();
            if (tokens.Length == 0 || tokens.Any(token => string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))) return tests;
            return tests.Where(test => tokens.Any(test.Matches)).ToList();
        }

        private static void PrintTestList(List<TestCase> tests)
        {
            foreach (TestCase test in tests)
            {
                Console.WriteLine(test.Key + " - " + test.Name + " [" + string.Join(", ", test.Tags ?? new string[0]) + "]");
            }
        }

        private static void TestAcksRules()
        {
            AssertEqual(4, AcksRules.ParseMarketClass("IV (large city)"), "Roman market class is parsed");
            AssertEqual(6, AcksRules.ParseMarketClass("6"), "Numeric market class is parsed");
            AssertEqual(-1, AcksRules.ParseMarketClass("VII"), "Invalid market class is rejected");
            AssertTrue(AcksRules.IsTradeRouteInRange(6, 6, true, 24), "Class VI road range includes 24 miles");
            AssertFalse(AcksRules.IsTradeRouteInRange(6, 6, true, 25), "Class VI road range excludes 25 miles");
        }

        private static void TestCharacterRulesService()
        {
            AssertEqual(-3, CharacterRulesService.AttributeBonus(3), "Low attribute penalty is calculated");
            AssertEqual(0, CharacterRulesService.AttributeBonus(12), "Average attribute has no modifier");
            AssertEqual(3, CharacterRulesService.AttributeBonus(18), "High attribute bonus is calculated");

            string[] proficiencies = CharacterRulesService.SplitProficiencies("Adventuring, Theology; Mapping\r\nRiding");
            AssertEqual(4, proficiencies.Length, "Proficiency text is split across separators");
            AssertEqual("Theology", CharacterRulesService.NormalizeProficiencyName("Theology 2"), "Rank suffix is removed from proficiency name");
            AssertEqual("Labor", CharacterRulesService.NormalizeProficiencyName("Labor (construction)"), "Parenthetical specialization is removed from proficiency name");

            AssertEqual(3, CharacterRulesService.GeneralProficiencyCountForLevel(9, 0), "General proficiency level thresholds are applied");
            AssertEqual(4, CharacterRulesService.GeneralProficiencyCountForLevel(9, 1), "INT bonus adds general proficiencies");
            AssertEqual(3, CharacterRulesService.ClassProficiencyCountForLevel("Mage", 12), "Mage-like class proficiency thresholds are slow");
            AssertEqual(3, CharacterRulesService.ClassProficiencyCountForLevel("Thief", 8), "Thief-like class proficiency thresholds are medium");
            AssertEqual(5, CharacterRulesService.ClassProficiencyCountForLevel("Fighter", 12), "Fighter-like class proficiency thresholds are fast");
        }

        private static void TestCharacterGenerator()
        {
            AssertEqual(2, CharacterGenerationCatalog.Sexes.Length, "Character generator exposes only binary sex choices");
            AssertTrue(!CharacterGenerationCatalog.Sexes.Any(s => s.IndexOf("Unknown", StringComparison.OrdinalIgnoreCase) >= 0),
                "Character generator does not expose Other/Unknown sex");

            CharacterGenerator playerGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationResult player = playerGenerator.GeneratePlayer(CreateCharacterGenerationRequest("Player", "Fighter", 1));

            AssertEqual("Player", player.Kind, "Player generation returns player kind");
            AssertEqual(1, player.Level, "Player generation creates first-level characters");
            AssertTrue(!string.IsNullOrWhiteSpace(player.ClassName), "Player generation chooses a class");
            AssertTrue(!string.IsNullOrWhiteSpace(player.Template), "Player generation chooses a template");
            AssertTrue(player.HitPoints >= 1, "Player generation creates positive hit points");
            AssertTrue(player.GenerateName, "Player generation asks UI adapter to generate a name");
            AssertEqual(6, player.Attributes.Count, "Player generation returns all six attributes");
            AssertTrue(CharacterRulesService.SplitProficiencies(player.Proficiencies).Contains("Adventuring"), "Player generation includes Adventuring");

            CharacterGenerator npcGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationResult npc = npcGenerator.GenerateNpc(CreateCharacterGenerationRequest("NPC", "Fighter", 0));

            AssertEqual("NPC", npc.Kind, "Zero-level NPC generation returns NPC kind");
            AssertEqual(0, npc.Level, "Zero-level NPC keeps level zero");
            AssertTrue(!string.IsNullOrWhiteSpace(npc.Occupation), "Zero-level NPC generation chooses an occupation");
            AssertTrue(!string.IsNullOrWhiteSpace(npc.Appearance), "NPC generation creates appearance");
            AssertTrue(npc.HitPoints >= 1, "NPC generation creates positive hit points");

            CharacterGenerator selectedOccupationGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationRequest selectedOccupationRequest = CreateCharacterGenerationRequest("NPC", "Fighter", 0);
            selectedOccupationRequest.RequestedOccupation = "Vintner";
            CharacterGenerationResult selectedOccupationNpc = selectedOccupationGenerator.GenerateNpc(selectedOccupationRequest);

            AssertEqual("Vintner", selectedOccupationNpc.Occupation, "NPC generation respects selected occupation");
            AssertEqual(0, selectedOccupationNpc.Level, "Selected occupation creates a zero-level NPC when level is zero");

            CharacterGenerator ignoredOccupationGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationRequest ignoredOccupationRequest = CreateCharacterGenerationRequest("NPC", "Fighter", 5);
            ignoredOccupationRequest.RequestedOccupation = "Vintner";
            CharacterGenerationResult ignoredOccupationNpc = ignoredOccupationGenerator.GenerateNpc(ignoredOccupationRequest);

            AssertEqual(5, ignoredOccupationNpc.Level, "Leveled NPC generation ignores selected occupation");
            AssertTrue(!string.Equals("Vintner", ignoredOccupationNpc.Occupation, StringComparison.OrdinalIgnoreCase), "Leveled NPC occupation is not the ignored zero-level job");

            CharacterGenerator npcButtonGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationResult npcFromPlayerUiState = npcButtonGenerator.GenerateNpc(CreateCharacterGenerationRequest("Player", "Fighter", 5));

            AssertEqual(5, npcFromPlayerUiState.Level, "NPC generation respects requested level even when current UI kind is Player");

            string appearance = selectedOccupationGenerator.GenerateAppearance(CreateCharacterGenerationRequest("NPC", "Fighter", 0));
            AssertTrue(!string.IsNullOrWhiteSpace(appearance), "Appearance reroll returns text");
            AssertTrue(!string.IsNullOrWhiteSpace(selectedOccupationGenerator.GenerateAppearance(null)), "Appearance reroll tolerates a null request");

            CharacterGenerator leveledNpcGenerator = new CharacterGenerator(new Random(12345));
            CharacterGenerationResult leveledNpc = leveledNpcGenerator.GenerateNpc(CreateCharacterGenerationRequest("NPC", "Fighter", 5));

            AssertEqual("NPC", leveledNpc.Kind, "Leveled NPC generation returns NPC kind");
            AssertEqual(5, leveledNpc.Level, "Leveled NPC generation respects requested level");
            AssertTrue(!string.IsNullOrWhiteSpace(leveledNpc.ClassName), "Leveled NPC generation chooses a class");
            AssertTrue(CharacterRulesService.SplitProficiencies(leveledNpc.Proficiencies).Contains("Adventuring"), "Leveled NPC generation includes Adventuring");

            CharacterGenerator proficiencyGenerator = new CharacterGenerator(new Random(12345));
            string randomizedProficiencies = proficiencyGenerator.RandomizeProficiencies(CreateCharacterGenerationRequest("Player", "Fighter", 5));
            string nullRequestProficiencies = proficiencyGenerator.RandomizeProficiencies(null);
            string[] splitProficiencies = CharacterRulesService.SplitProficiencies(randomizedProficiencies);

            AssertTrue(splitProficiencies.Contains("Adventuring"), "Randomized proficiencies include Adventuring");
            AssertTrue(CharacterRulesService.SplitProficiencies(nullRequestProficiencies).Contains("Adventuring"), "Randomized proficiencies tolerate a null request");
            AssertEqual(
                splitProficiencies.Length,
                splitProficiencies.Select(CharacterRulesService.NormalizeProficiencyName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                "Randomized proficiencies do not duplicate normalized names");
        }

        private static void TestRealmTitlesAndClanNames()
        {
            NameGenerationService names = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);

            AssertEqual("Tarkun", RealmTitleCatalog.RulerTitle("auran", "Empire", false, false, "", ""), "Auran empire title follows ACKS table");
            AssertEqual("Tiarna", RealmTitleCatalog.RulerTitle("elf", "Barony", false, false, "", ""), "Elven/Argollean barony title follows ACKS table");
            AssertEqual("Императрица", RealmTitleCatalog.RulerTitle("english", "Empire", true, true, "", ""), "Russian common female emperor title is feminine");
            AssertEqual("Вождь", RealmTitleCatalog.RulerTitle("english", "Kingdom", false, true, "Вождь", ""), "Custom ruler title overrides catalog");
            AssertEqual("Вождьша", RealmTitleCatalog.RulerTitle("english", "Kingdom", true, true, "Вождь", "Вождьша"), "Custom female ruler title overrides catalog");

            List<string> clanNames = Enumerable.Range(1, 20)
                .Select(i => names.GenerateRealmName(new Random(i), "human_clan", "Wolf", "Barony", false))
                .ToList();
            AssertTrue(clanNames.Any(n => n.IndexOf("Clan", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Tribe", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Oathhold", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("War-Camp", StringComparison.OrdinalIgnoreCase) >= 0),
                "Human clan realm names use clan-specific patterns");
        }

        private static CharacterGenerationRequest CreateCharacterGenerationRequest(string kind, string className, int level)
        {
            return new CharacterGenerationRequest
            {
                IsEnglish = false,
                CurrentKind = kind,
                CurrentClassName = className,
                CurrentSex = "Female",
                RequestedOccupation = "",
                ForceZeroLevelOccupation = false,
                RequestedLevel = level,
                MaximumLevel = 14,
                Attributes = new Dictionary<string, int>
                {
                    { "STR", 10 },
                    { "INT", 14 },
                    { "WIL", 10 },
                    { "DEX", 10 },
                    { "CON", 10 },
                    { "CHA", 10 }
                }
            };
        }

        private static void TestMapDataNormalizer()
        {
            AssertEqual("Dwarf", MapDataNormalizer.SettlementRace("Dwarven"), "Dwarven race alias is normalized");
            AssertEqual("Elf", MapDataNormalizer.SettlementRace("Elven"), "Elven race alias is normalized");
            AssertEqual("DeepForest", MapDataNormalizer.TerrainKey("Deep Forest"), "Terrain names ignore spaces");
            AssertEqual("Sea", MapDataNormalizer.WaterKey("sea"), "Water keys are case-insensitive");

            HexMapRecord map = new HexMapRecord
            {
                Cells = new List<HexCellRecord>
                {
                    new HexCellRecord { Q = 1, R = 2, Terrain = "Deep Taiga", Elevation = "mountains", Water = "lake" }
                },
                Settlements = new List<MapSettlementRecord>
                {
                    new MapSettlementRecord
                    {
                        Id = "",
                        Name = "Stonebridge",
                        MarketClass = 99,
                        Race = "Beastmen",
                        BaseDemands = new[] { 1.0 },
                        CurrentDemands = new[] { 2.0, 3.0 }
                    }
                }
            };

            MapDataNormalizer.NormalizeMapShell(map);

            AssertEqual("DeepTaiga", map.Cells[0].Terrain, "Map terrain is normalized in place");
            AssertEqual("Mountains", map.Cells[0].Elevation, "Map elevation is normalized in place");
            AssertEqual("Lake", map.Cells[0].Water, "Map water is normalized in place");
            AssertEqual("Beastman", map.Settlements[0].Race, "Settlement race is normalized in place");
            AssertEqual(6, map.Settlements[0].MarketClass, "Invalid market class falls back to VI");
            AssertEqual(AcksRules.DemandCount, map.Settlements[0].BaseDemands.Length, "Base demands get canonical size");
            AssertTrue(!string.IsNullOrWhiteSpace(map.Settlements[0].Id), "Missing settlement id is created");

            DungeonRecord dungeon = new DungeonRecord();
            dungeon.Levels.Clear();
            dungeon.Levels.Add(new DungeonLevelRecord
            {
                LevelNumber = 42,
                Rooms = new List<DungeonRoomRecord>
                {
                    new DungeonRoomRecord { LevelNumber = 42, X = 1, Y = 1, Width = 2, Height = 2 }
                },
                Doors = new List<DungeonDoorRecord>
                {
                    new DungeonDoorRecord { LevelNumber = 42, X = 1, Y = 2, FromRoomId = "" }
                }
            });
            dungeon.WanderingEncounters.Add(new DungeonEncounterRecord { DungeonLevel = 42, Roll = 1, Monster = "goblins" });

            MapDataNormalizer.NormalizeDungeon(dungeon);

            AssertEqual(1, dungeon.Levels[0].LevelNumber, "Single imported dungeon level is renumbered to one");
            AssertEqual(1, dungeon.Levels[0].Rooms[0].LevelNumber, "Dungeon room level follows renumbered level");
            AssertEqual(1, dungeon.Levels[0].Doors[0].LevelNumber, "Dungeon door level follows renumbered level");
            AssertEqual(1, dungeon.WanderingEncounters[0].DungeonLevel, "Dungeon encounter table follows renumbered level");

            DungeonRecord dirtyDungeon = new DungeonRecord();
            dirtyDungeon.Levels.Clear();
            dirtyDungeon.Levels.Add(new DungeonLevelRecord
            {
                LevelNumber = 1,
                Rooms = new List<DungeonRoomRecord>
                {
                    new DungeonRoomRecord { Id = "a", LevelNumber = 1, X = 0, Y = 0, Width = 4, Height = 4, Shape = "Rectangle" },
                    new DungeonRoomRecord { Id = "b", LevelNumber = 1, X = 8, Y = 0, Width = 4, Height = 4, Shape = "Rectangle" }
                },
                Connections = new List<DungeonConnectionRecord>
                {
                    new DungeonConnectionRecord
                    {
                        FromRoomId = "a",
                        ToRoomId = "b",
                        PathPoints = new List<DungeonPathPointRecord>
                        {
                            new DungeonPathPointRecord { X = 3.5, Y = 2 },
                            new DungeonPathPointRecord { X = 2, Y = 2 },
                            new DungeonPathPointRecord { X = 5.5, Y = 2 },
                            new DungeonPathPointRecord { X = 8.5, Y = 2 }
                        }
                    }
                }
            });

            MapDataNormalizer.NormalizeDungeon(dirtyDungeon);
            DungeonLevelRecord dirtyLevel = dirtyDungeon.Levels[0];
            DungeonConnectionRecord dirtyConnection = dirtyLevel.Connections[0];
            AssertEqual(2, dirtyConnection.PathPoints.Count, "Dungeon path normalization removes interior and redundant straight-line points");
            AssertTrue(DungeonPointIsOnRoomBoundary(dirtyConnection.PathPoints[0], dirtyLevel.Rooms[0]),
                "Dungeon path normalization anchors first point on source room boundary");
            AssertTrue(DungeonPointIsOnRoomBoundary(dirtyConnection.PathPoints[dirtyConnection.PathPoints.Count - 1], dirtyLevel.Rooms[1]),
                "Dungeon path normalization anchors last point on target room boundary");
            AssertTrue(dirtyConnection.PathPoints.All(p => !DungeonGeometry.IsPointInsideRoomInterior(dirtyLevel.Rooms[0], p.X, p.Y, 0.01)
                    && !DungeonGeometry.IsPointInsideRoomInterior(dirtyLevel.Rooms[1], p.X, p.Y, 0.01)),
                "Dungeon path normalization keeps path points out of linked room interiors");
        }

        private static void TestWildernessHexModifiers()
        {
            WildernessHexModifiers grassland = WildernessHexRules.GetModifiers(
                new HexCellRecord { Terrain = "Grasslands", Elevation = "Plains", Water = "None" },
                false);
            AssertEqual("Grassland (other)", grassland.TerrainType, "Grasslands use the grassland evasion row");
            AssertEqual("6+", grassland.NavigationThrow, "Grasslands navigation throw follows wilderness table");
            AssertEqual("x1", grassland.SpeedMultiplier, "Grasslands speed multiplier follows wilderness table");
            AssertEqual("9+ / 11+ / 13+ / 15+ / 17+", string.Join(" / ", grassland.EvasionThrowsByPartySize), "Grasslands evasion throws follow party-size columns");

            WildernessHexModifiers jungle = WildernessHexRules.GetModifiers(
                new HexCellRecord { Terrain = "Rainforest", Elevation = "Plains", Water = "None" },
                false);
            AssertEqual("Jungle (any)", jungle.TerrainType, "Rainforest maps to jungle wilderness terrain");
            AssertEqual("14+", jungle.NavigationThrow, "Jungle navigation throw is hard");
            AssertEqual("x1/2", jungle.SpeedMultiplier, "Jungle speed multiplier follows wilderness table");
            AssertEqual("2+ / 4+ / 6+ / 8+ / 10+", string.Join(" / ", jungle.EvasionThrowsByPartySize), "Jungle evasion throws follow party-size columns");

            WildernessHexModifiers forestedMountains = WildernessHexRules.GetModifiers(
                new HexCellRecord { Terrain = "Forest", Elevation = "Mountains", Water = "None" },
                false);
            AssertEqual("Mountains (forested)", forestedMountains.TerrainType, "Forested mountain hexes use the forested mountain evasion row");
            AssertEqual("6+", forestedMountains.NavigationThrow, "Mountains navigation throw overrides forest terrain");
            AssertEqual("x1/2", forestedMountains.SpeedMultiplier, "Mountain speed multiplier overrides forest terrain");
            AssertEqual("5+ / 7+ / 9+ / 11+ / 13+", string.Join(" / ", forestedMountains.EvasionThrowsByPartySize), "Forested mountain evasion throws follow party-size columns");

            List<string> roadLines = WildernessHexRules.BuildDisplayLines(
                new HexCellRecord { Terrain = "Scrub", Elevation = "Plains", Water = "None" },
                true,
                true);
            AssertTrue(roadLines.Any(line => line.Contains("road x3/2") && line.Contains("drivers x2")), "Road hex info includes road speed modifiers");

            List<string> riverForagingLines = WildernessHexRules.BuildDisplayLines(
                new HexCellRecord { Terrain = "Scrub", Elevation = "Plains", Water = "None" },
                false,
                true,
                "Outlands",
                true);
            AssertTrue(riverForagingLines.Any(line => line.IndexOf("Water: automatic", StringComparison.OrdinalIgnoreCase) >= 0),
                "River hex foraging info marks water as automatic");
            AssertTrue(riverForagingLines.Any(line => line.IndexOf("Hunt food: 12+ in outlands", StringComparison.OrdinalIgnoreCase) >= 0),
                "Outlands domain classification selects the outlands hunting throw");
            AssertFalse(riverForagingLines.Any(line => line.IndexOf("Food without stealing:", StringComparison.OrdinalIgnoreCase) >= 0),
                "Outlands hex info omits no-stealing penalties because none apply");
            AssertFalse(riverForagingLines.Any(line => line.IndexOf("Weather:", StringComparison.OrdinalIgnoreCase) >= 0),
                "Hex info does not show weather penalties before weather exists in the program");
            AssertTrue(riverForagingLines.Any(line => line.IndexOf("triggers a wilderness encounter check", StringComparison.OrdinalIgnoreCase) >= 0),
                "Hunting text explains that hunting triggers a wilderness encounter check");
            AssertTrue(riverForagingLines.Any(line => line.IndexOf("with Survival proficiency", StringComparison.OrdinalIgnoreCase) >= 0),
                "Foraging text explains the Survival bonus");

            List<string> desertForagingLines = WildernessHexRules.BuildDisplayLines(
                new HexCellRecord { Terrain = "Desert", Elevation = "Plains", Water = "None" },
                false,
                false,
                "Civilized",
                true);
            AssertTrue(desertForagingLines.Any(line => line.IndexOf("Wood: 14+", StringComparison.OrdinalIgnoreCase) >= 0),
                "Non-forest terrain uses the hard firewood foraging throw");
            AssertTrue(desertForagingLines.Any(line => line.IndexOf("Water: 18+", StringComparison.OrdinalIgnoreCase) >= 0),
                "Barrens and desert terrain use the hard water foraging throw");
            AssertTrue(desertForagingLines.Any(line => line.IndexOf("Food: 22+", StringComparison.OrdinalIgnoreCase) >= 0),
                "Barrens and desert terrain use the hard food foraging throw");
            AssertTrue(desertForagingLines.Any(line => line.IndexOf("Food without stealing: -4", StringComparison.OrdinalIgnoreCase) >= 0),
                "Civilized territory applies the no-stealing food foraging penalty");

            List<string> forestForagingLines = WildernessHexRules.BuildDisplayLines(
                new HexCellRecord { Terrain = "Forest", Elevation = "Plains", Water = "None" },
                false,
                false,
                null,
                true);
            AssertTrue(forestForagingLines.Any(line => line.IndexOf("Wood: 3+", StringComparison.OrdinalIgnoreCase) >= 0),
                "Forest terrain uses the easy firewood foraging throw");
            AssertTrue(forestForagingLines.Any(line => line.IndexOf("Hunt food: 10+ in unsettled wilderness", StringComparison.OrdinalIgnoreCase) >= 0),
                "Hexes outside domains use the unsettled hunting throw");
            AssertFalse(forestForagingLines.Any(line => line.IndexOf("Food without stealing:", StringComparison.OrdinalIgnoreCase) >= 0),
                "Unsettled hex info omits no-stealing penalties because none apply");

            List<string> russianLines = WildernessHexRules.BuildDisplayLines(
                new HexCellRecord { Terrain = "Marsh", Elevation = "Plains", Water = "None" },
                false,
                false);
            AssertTrue(russianLines.Any(line => line.Contains("Навигация: 10+")), "Russian hex info includes localized navigation");
            AssertTrue(russianLines.Any(line => line.Contains("Уклонение")
                && line.Contains("размер отряда")
                && line.Contains("цель на d20")), "Russian hex info explains evasion columns");

            AssertEqual(
                0,
                WildernessHexRules.BuildDisplayLines(new HexCellRecord { Terrain = "Grasslands", Elevation = "Plains", Water = "Lake" }, false, true).Count,
                "Water hexes do not show wilderness expedition terrain modifiers");
        }

        private static void TestMapDemandService()
        {
            double[] demands = new double[AcksRules.DemandCount];
            demands[0] = 2;
            HexCellRecord cell = new HexCellRecord { Terrain = "Grasslands", Elevation = "Plains", Water = "None" };

            double[] adjusted = MapDemandService.BuildCellAdjustedDemands(demands, cell, Enumerable.Empty<HexCellRecord>(), false);

            AssertNear(0.5, adjusted[0], "Grasslands and plains adjustments are applied");
            AssertNear(2, demands[0], "Source demand array is not mutated");

            HexCellRecord seaNeighbor = new HexCellRecord { Water = "Sea" };
            string[] influences = MapDemandService.GetWaterInfluences(new[] { seaNeighbor }, true).ToArray();
            AssertTrue(influences.Contains("River"), "River influence is detected");
            AssertTrue(influences.Contains("Sea"), "Sea influence is detected from neighbors");
        }

        private static void TestRegionMapGeneratorCivilizationLayers()
        {
            NameGenerationService names = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            RegionMapGenerator generator = new RegionMapGenerator(names);

            RegionGenerationOptions strongholdsOnly = CreateRegionOptions("strongholds-only");
            strongholdsOnly.GenerateSettlements = false;
            strongholdsOnly.GenerateStrongholds = true;
            strongholdsOnly.GenerateDomains = true;
            strongholdsOnly.GenerateRoads = true;
            strongholdsOnly.GenerateRealms = true;

            HexMapRecord strongholdMap = generator.Generate(strongholdsOnly).Map;
            AssertEqual(0, strongholdMap.Settlements.Count, "Stronghold-only generation can skip settlements");
            AssertTrue(strongholdMap.Domains.Count > 0, "Stronghold-only generation still creates domains");
            AssertTrue(strongholdMap.Domains.All(d => d.StrongholdQ >= 0 && d.StrongholdR >= 0), "Stronghold-only domains get map positions");

            RegionGenerationOptions noStrongholds = CreateRegionOptions("no-strongholds");
            noStrongholds.GenerateSettlements = true;
            noStrongholds.GenerateStrongholds = false;
            noStrongholds.GenerateDomains = true;
            noStrongholds.GenerateRoads = false;
            noStrongholds.GenerateRealms = false;

            HexMapRecord noStrongholdMap = generator.Generate(noStrongholds).Map;
            AssertTrue(noStrongholdMap.Domains.Count > 0, "Domain generation still works without strongholds");
            AssertTrue(noStrongholdMap.Domains.All(d => d.StrongholdQ < 0 && d.StrongholdR < 0 && !d.StrongholdSecuresDomain), "Disabled stronghold layer leaves no stronghold coordinates");

            RegionGenerationOptions landValues = CreateRegionOptions("per-hex-land-values");
            landValues.LandValueMode = "PerHex";
            HexMapRecord landValueMap = generator.Generate(landValues).Map;
            List<DomainHexRecord> generatedDomainHexes = landValueMap.Domains.SelectMany(d => d.Hexes).ToList();
            AssertTrue(generatedDomainHexes.Count > 0, "Per-hex land value generation creates domain hexes");
            AssertTrue(generatedDomainHexes.All(h => h.LandValueGp >= 3 && h.LandValueGp <= 9), "Per-hex land values stay in ACKS 3-9 gp range");
            AssertTrue(landValueMap.Domains.All(d => d.FixedLandValueGp >= 3 && d.FixedLandValueGp <= 9), "Domain land value roll stays in ACKS 3-9 gp range");

            RegionGenerationOptions full = CreateRegionOptions("partial-base");
            HexMapRecord baseMap = generator.Generate(full).Map;
            string beforeLandscape = LandscapeSignature(baseMap);

            RegionGenerationOptions riversOnly = CreateRegionOptions("partial-rivers");
            riversOnly.GenerateRivers = true;
            riversOnly.RiverPercent = 100;
            HexMapRecord riverRegenerated = generator.RegenerateCivilization(baseMap, riversOnly, null, default(System.Threading.CancellationToken)).Map;

            AssertEqual(beforeLandscape, LandscapeSignature(riverRegenerated), "River-only regeneration preserves landscape cells");
            AssertTrue(riverRegenerated.Rivers.Count > 0, "River-only regeneration can rebuild rivers without full region generation");

            RegionGenerationOptions civilization = CreateRegionOptions("partial-civ");
            civilization.GenerateSettlements = false;
            civilization.GenerateStrongholds = true;
            civilization.GenerateDomains = true;
            civilization.GenerateRoads = true;
            civilization.GenerateRealms = true;
            HexMapRecord regenerated = generator.RegenerateCivilization(baseMap, civilization, null, default(System.Threading.CancellationToken)).Map;

            AssertEqual(beforeLandscape, LandscapeSignature(regenerated), "Civilization regeneration preserves landscape cells");
            AssertTrue(regenerated.Domains.Count > 0, "Civilization regeneration can rebuild domains");

            RegionGenerationOptions blockedDependencies = CreateRegionOptions("partial-empty");
            blockedDependencies.GenerateSettlements = false;
            blockedDependencies.GenerateStrongholds = false;
            blockedDependencies.GenerateDomains = true;
            blockedDependencies.GenerateRoads = true;
            blockedDependencies.GenerateRealms = true;
            blockedDependencies.GenerateRulers = true;
            HexMapRecord emptyMap = new HexMapRecord { Width = 8, Height = 8, Cells = new List<HexCellRecord>() };
            for (int r = 0; r < emptyMap.Height; r++)
            {
                for (int q = 0; q < emptyMap.Width; q++)
                {
                    emptyMap.Cells.Add(new HexCellRecord { Q = q, R = r, Terrain = "Grasslands", Elevation = "Plains", Water = "None" });
                }
            }

            HexMapRecord dependencyResult = generator.RegenerateCivilization(emptyMap, blockedDependencies, null, default(System.Threading.CancellationToken)).Map;
            AssertEqual(0, dependencyResult.Domains.Count, "Civilization regeneration disables domains without settlements or strongholds");
            AssertEqual(0, dependencyResult.Roads.Count, "Civilization regeneration disables roads without settlements or strongholds");
            AssertEqual(0, dependencyResult.Realms.Count, "Civilization regeneration disables realms without domains");

            AssertRegenerationDensityMatchesSimplePreset(generator, "regen-density-borderlands", "Borderlands");
            AssertRegenerationDensityMatchesSimplePreset(generator, "regen-density-wild", "Wild");
            AssertLongWildRoadsAreRejected(generator);
            AssertBorderlandsClanholdsAreVisible(generator);
            AssertParallelRoadPathIsRejected(generator);
            AssertDanglingRoadStubsAreRemoved(generator);
            AssertLargeWaterLayoutsAreIrregular(generator);
            AssertSmallArchipelagoAvoidsStraightProtectedEdge(generator);
            AssertDeepForestAppearsInEveryClimate(generator);
            AssertSeismicRegionHasMoreMountains(generator);
            AssertCivilizedNaturalPocketsRemainUnsettled(generator);
            AssertCivilizedArchipelagoReservesSelectedSpecialDomains(generator);
            AssertWildRoadRulesRespectIsolation(generator);
            AssertWildRoadNetworkStaysSparse(generator);

            RegionGenerationOptions realmOptions = CreateRegionOptions("realm-tier-order");
            realmOptions.CivilizationLevel = "Civilized";
            realmOptions.SettlementDensityPercent = 80;
            realmOptions.DomainCoveragePercent = 100;
            realmOptions.GenerateSettlements = true;
            realmOptions.GenerateStrongholds = true;
            realmOptions.GenerateDomains = true;
            realmOptions.GenerateRealms = true;
            realmOptions.GenerateRulers = true;
            realmOptions.RealmCount = 5;
            realmOptions.UseRussianNames = true;
            HexMapRecord realmMap = generator.Generate(realmOptions).Map;

            AssertTrue(realmMap.VassalLinks.Count > 0, "Realm generation creates vassal links on dense civilized maps");
            AssertTrue(realmMap.Realms.Count > realmOptions.RealmCount, "Dense realm generation creates a title hierarchy beyond top-level realms");
            AssertTrue(realmMap.Realms.Any(r => string.Equals(r.Tier, "Barony", StringComparison.OrdinalIgnoreCase)), "Dense realm generation creates baron-level titles");
            AssertTrue(realmMap.Realms.Any(r => string.Equals(r.Tier, "County", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.Tier, "Duchy", StringComparison.OrdinalIgnoreCase)), "Dense realm generation creates intermediate titles");
            AssertTrue(MaxRealmHierarchyDepth(realmMap) >= 3, "Dense realm generation creates more than a shallow sovereign-vassal chain");
            AssertRealmHierarchyRanks(realmMap);
            List<CharacterRecord> generatedRulers = realmMap.Domains
                .Select(d => d.Ruler == null ? null : d.Ruler.Snapshot)
                .Where(r => r != null)
                .ToList();
            AssertTrue(generatedRulers.Count > 0, "Region generation creates domain ruler snapshots");
            AssertTrue(generatedRulers.All(r => string.Equals(r.Sex, "Male", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Sex, "Female", StringComparison.OrdinalIgnoreCase)),
                "Region-generated rulers store sex for gendered realm titles");

            RegionGenerationOptions manySmallRealms = CreateRegionOptions("realm-profile-compare");
            manySmallRealms.CivilizationLevel = "Civilized";
            manySmallRealms.SettlementDensityPercent = 90;
            manySmallRealms.DomainCoveragePercent = 100;
            manySmallRealms.GenerateSettlements = true;
            manySmallRealms.GenerateStrongholds = true;
            manySmallRealms.GenerateDomains = true;
            manySmallRealms.GenerateRealms = true;
            manySmallRealms.RealmScale = "ManySmall";
            manySmallRealms.RealmCount = 5;

            RegionGenerationOptions fewLargeRealms = CreateRegionOptions("realm-profile-compare");
            fewLargeRealms.CivilizationLevel = "Civilized";
            fewLargeRealms.SettlementDensityPercent = 90;
            fewLargeRealms.DomainCoveragePercent = 100;
            fewLargeRealms.GenerateSettlements = true;
            fewLargeRealms.GenerateStrongholds = true;
            fewLargeRealms.GenerateDomains = true;
            fewLargeRealms.GenerateRealms = true;
            fewLargeRealms.RealmScale = "FewLarge";
            fewLargeRealms.RealmCount = 5;

            HexMapRecord manySmallMap = generator.Generate(manySmallRealms).Map;
            HexMapRecord fewLargeMap = generator.Generate(fewLargeRealms).Map;
            AssertTrue(
                CountIndependentRealms(manySmallMap) > CountIndependentRealms(fewLargeMap),
                "Many-small realm profile creates more independent rulers than few-large profile");
            AssertTrue(
                CountIndependentBaronies(manySmallMap) > 0,
                "Many-small realm profile can leave independent baronies");

            RegionGenerationOptions oneStateRealms = CreateRegionOptions("realm-one-state");
            oneStateRealms.CivilizationLevel = "Civilized";
            oneStateRealms.SettlementDensityPercent = 75;
            oneStateRealms.DomainCoveragePercent = 100;
            oneStateRealms.GenerateSettlements = true;
            oneStateRealms.GenerateStrongholds = true;
            oneStateRealms.GenerateDomains = true;
            oneStateRealms.GenerateRealms = true;
            oneStateRealms.RealmScale = "OneState";
            HexMapRecord oneStateMap = generator.Generate(oneStateRealms).Map;
            AssertEqual(1, CountIndependentRealms(oneStateMap), "One-state profile creates one sovereign human realm when only human domains are generated");

            for (int i = 0; i < 50; i++)
            {
                string baronyName = names.GenerateRealmName(new Random(i), "english", "Test", "Barony", true);
                AssertFalse(baronyName.StartsWith("Корона ", StringComparison.Ordinal), "Russian barony names do not use crown wording");
            }
            AssertNameGenerationDiversity(names);
        }

        private static void AssertNameGenerationDiversity(NameGenerationService names)
        {
            AssertTrue(HasUnexpectedInternalCapital("BlackWater"), "Mid-word capital detector catches joined compounds");
            AssertFalse(HasUnexpectedInternalCapital("O'Connor"), "Mid-word capital detector allows apostrophe names");

            Random russianEnglishRandom = new Random(9100);
            List<string> russianEnglishNames = Enumerable.Range(0, 40)
                .Select(i => names.GeneratePersonalName(russianEnglishRandom, "english", i % 2 == 0, true))
                .ToList();
            string[] roughRussianFragments = { " те ", " оф ", "доттир", "Бакер", "Арморер", "Навигатор" };
            AssertTrue(
                russianEnglishNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 34,
                "Russian English-name generation keeps variety");
            AssertFalse(
                russianEnglishNames.Any(ContainsLatinLetter),
                "Russian English-name generation transliterates all Latin letters");
            AssertFalse(
                russianEnglishNames.Any(name => roughRussianFragments.Any(fragment => name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)),
                "Russian English-name generation avoids raw English byname fragments");

            string[] diversityCultures = { "english", "russian", "arabic", "persian", "japanese", "old_norse", "dwarf", "elf" };
            foreach (string culture in diversityCultures)
            {
                Random personalRandom = new Random(7100 + culture.Length);
                List<string> personalNames = Enumerable.Range(0, 40)
                    .Select(i => names.GeneratePersonalName(personalRandom, culture, i % 2 == 0, false))
                    .ToList();
                AssertTrue(
                    personalNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 36,
                    "Name generator keeps personal-name variety for " + culture);
                AssertFalse(
                    personalNames.Any(HasUnexpectedInternalCapital),
                    "Name generator avoids mid-word capitals in personal names for " + culture);

                Random dynastyRandom = new Random(8100 + culture.Length);
                List<string> dynastyNames = Enumerable.Range(0, 40)
                    .Select(i => names.GenerateDynastyName(dynastyRandom, culture, false))
                    .ToList();
                AssertTrue(
                    dynastyNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 28,
                    "Name generator keeps dynasty-name variety for " + culture);
                AssertFalse(
                    dynastyNames.Any(HasUnexpectedInternalCapital),
                    "Name generator avoids mid-word capitals in dynasty names for " + culture);
            }
        }

        private static bool HasUnexpectedInternalCapital(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsUpper(name[i])) continue;

                char previous = name[i - 1];
                if (char.IsWhiteSpace(previous) || previous == '\'' || previous == '’' || previous == '-') continue;
                if (char.IsLower(previous)) return true;
            }

            return false;
        }

        private static bool ContainsLatinLetter(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return value.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
        }

        private static void AssertRegenerationDensityMatchesSimplePreset(RegionMapGenerator generator, string seed, string civilizationLevel)
        {
            RegionGenerationOptions full = CreateSimplePresetRegionOptions(seed + "-full", civilizationLevel);
            HexMapRecord fullMap = generator.Generate(full).Map;

            RegionGenerationOptions regenerate = CreateSimplePresetRegionOptions(seed + "-regen", civilizationLevel);
            regenerate.Width = 1;
            regenerate.Height = 1;
            regenerate.SettlementDensityPercent = 35;
            regenerate.DomainCoveragePercent = 45;
            regenerate.RealmCount = 3;

            HexMapRecord regenerated = generator.RegenerateCivilization(fullMap, regenerate, null, default(System.Threading.CancellationToken)).Map;
            int fullCount = fullMap.Settlements.Count;
            int regeneratedCount = regenerated.Settlements.Count;
            int tolerance = Math.Max(2, Math.Max(fullCount, regeneratedCount) / 3);

            AssertTrue(
                Math.Abs(fullCount - regeneratedCount) <= tolerance,
                "Civilization regeneration uses the same simple settlement-density preset as full " + civilizationLevel + " generation");
        }

        private static RegionGenerationOptions CreateSimplePresetRegionOptions(string seed, string civilizationLevel)
        {
            return new RegionGenerationOptions
            {
                Seed = seed,
                Width = 36,
                Height = 30,
                AdvancedMode = false,
                ClimateBelt = "Temperate",
                CivilizationLevel = civilizationLevel,
                RealmScale = "Balanced",
                CultureKey = "english",
                WaterLayout = "Coast",
                GenerateRivers = false,
                GenerateFeatureNames = false,
                GenerateSettlements = true,
                GenerateStrongholds = true,
                GenerateDomains = true,
                GenerateRoads = true,
                GenerateRealms = true,
                GenerateRulers = false
            };
        }

        private static void AssertLongWildRoadsAreRejected(RegionMapGenerator generator)
        {
            HexMapRecord map = CreateFlatMap(42, 8);
            MapSettlementRecord west = new MapSettlementRecord
            {
                Id = "west-town",
                Name = "West Town",
                MarketClass = 6,
                Q = 2,
                R = 3,
                Race = "Human"
            };
            MapSettlementRecord east = new MapSettlementRecord
            {
                Id = "east-town",
                Name = "East Town",
                MarketClass = 6,
                Q = 36,
                R = 3,
                Race = "Human"
            };
            map.Settlements.Add(west);
            map.Settlements.Add(east);
            map.Domains.Add(CreateTinyDomain("west-domain", west.Id, 2, 3));
            map.Domains.Add(CreateTinyDomain("east-domain", east.Id, 36, 3));

            RegionGenerationOptions roadsOnly = new RegionGenerationOptions
            {
                Seed = "long-wild-road",
                Width = map.Width,
                Height = map.Height,
                AdvancedMode = true,
                CivilizationLevel = "Wild",
                CultureKey = "english",
                GenerateSettlements = false,
                GenerateStrongholds = false,
                GenerateDomains = false,
                GenerateRoads = true,
                GenerateRealms = false,
                GenerateRulers = false,
                GenerateRivers = false,
                GenerateFeatureNames = false
            };

            HexMapRecord roadMap = generator.RegenerateCivilization(map, roadsOnly, null, default(System.Threading.CancellationToken)).Map;
            AssertEqual(0, roadMap.Roads.Count, "Wild road regeneration does not connect distant ordinary outlands domains");
        }

        private static void AssertBorderlandsClanholdsAreVisible(RegionMapGenerator generator)
        {
            RegionGenerationOptions options = CreateRegionOptions("borderlands-clans-visible");
            options.Width = 80;
            options.Height = 80;
            options.CivilizationLevel = "Borderlands";
            options.WaterLayout = "Inland";
            options.WaterPercent = 0;
            options.LakePercent = 0;
            options.SettlementDensityPercent = 35;
            options.DomainCoveragePercent = 85;
            options.GenerateSettlements = true;
            options.GenerateStrongholds = true;
            options.GenerateDomains = true;
            options.GenerateRoads = false;
            options.GenerateRealms = false;
            options.GenerateSpecialDomains = true;
            options.GenerateClanDomains = true;
            options.GenerateDwarvenDomains = false;
            options.GenerateElvenDomains = false;
            options.GenerateTransitionalDomains = false;
            options.UseSpecialDomainWeights = true;
            options.SpecialDomainPercent = 30;
            options.ClanDomainWeight = 30;

            HexMapRecord map = generator.Generate(options).Map;
            List<MapSettlementRecord> seededClanholds = map.Settlements
                .Where(s => string.Equals(s.LandValue, "Generated Clanhold", StringComparison.OrdinalIgnoreCase))
                .ToList();
            int clanholds = map.Domains.Count(d => string.Equals(d.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase));
            AssertTrue(clanholds >= 6, "Large borderlands maps with clan domains enabled create several clanholds");
            AssertTrue(seededClanholds.Count >= 6, "Large borderlands maps seed several remote clanhold settlements");
            AssertTrue(MinSettlementDistance(seededClanholds) >= 7, "Remote clanhold settlements are scattered instead of clustered");
        }

        private static void AssertParallelRoadPathIsRejected(RegionMapGenerator generator)
        {
            HexMapRecord map = CreateFlatMap(8, 10);
            HashSet<string> roadCells = new HashSet<string>();
            for (int r = 0; r <= 8; r++)
            {
                roadCells.Add("2," + r);
            }

            List<HexCellRecord> parallelPath = map.Cells
                .Where(c => c.Q == 3 && c.R <= 8)
                .OrderBy(c => c.R)
                .ToList();

            System.Reflection.MethodInfo method = typeof(RegionMapGenerator).GetMethod(
                "IsParallelRoadDuplicate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bool rejected = (bool)method.Invoke(generator, new object[] { map, parallelPath, roadCells });
            AssertTrue(rejected, "Road generation rejects long paths parallel to an existing road");
        }

        private static void AssertDanglingRoadStubsAreRemoved(RegionMapGenerator generator)
        {
            HexMapRecord map = CreateFlatMap(5, 3);
            map.Roads.Add(new MapEdgeRecord { AQ = 0, AR = 1, BQ = 1, BR = 1, Kind = "Road" });
            map.Roads.Add(new MapEdgeRecord { AQ = 1, AR = 1, BQ = 2, BR = 1, Kind = "Road" });

            HashSet<string> roadNodes = new HashSet<string> { "0,1" };
            System.Reflection.MethodInfo method = typeof(RegionMapGenerator).GetMethod(
                "RemoveDanglingRoadStubs",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.Invoke(generator, new object[] { map, roadNodes });
            AssertEqual(0, map.Roads.Count, "Road generation removes stubs ending in empty cells");

            HexMapRecord kept = CreateFlatMap(5, 3);
            kept.Roads.Add(new MapEdgeRecord { AQ = 0, AR = 1, BQ = 1, BR = 1, Kind = "Road" });
            kept.Roads.Add(new MapEdgeRecord { AQ = 1, AR = 1, BQ = 2, BR = 1, Kind = "Road" });
            method.Invoke(generator, new object[] { kept, new HashSet<string> { "0,1", "2,1" } });
            AssertEqual(2, kept.Roads.Count, "Road generation keeps roads between real road nodes");
        }

        private static void AssertLargeWaterLayoutsAreIrregular(RegionMapGenerator generator)
        {
            foreach (string layout in new[] { "Coast", "Continent", "TwoContinents", "InlandSea", "Gulf" })
            {
                RegionGenerationOptions options = CreateRegionOptions("water-shape-" + layout);
                options.Width = 70;
                options.Height = 54;
                options.WaterLayout = layout;
                options.WaterPercent = layout == "InlandSea" ? 22 : 34;
                options.LakePercent = 0;
                options.GenerateRivers = false;
                options.GenerateFeatureNames = false;
                options.GenerateSettlements = false;
                options.GenerateStrongholds = false;
                options.GenerateDomains = false;
                options.GenerateRoads = false;
                options.GenerateRealms = false;

                HexMapRecord map = generator.Generate(options).Map;
                AssertTrue(map.Cells.Any(IsWaterCell), layout + " layout creates large water");
                AssertTrue(WaterSpanVariation(map) >= (layout == "Coast" ? 8 : 6), layout + " layout has an irregular shoreline span");

                if (layout == "Continent")
                {
                    List<int> landSizes = LandComponentSizes(map);
                    AssertTrue(landSizes.Count >= 2, "Continent layout can create small islands around the main landmass");
                }

                if (layout == "TwoContinents")
                {
                    List<List<HexCellRecord>> components = LandComponents(map)
                        .OrderByDescending(c => c.Count)
                        .ToList();
                    int largeThreshold = Math.Max(40, map.Cells.Count / 16);
                    List<List<HexCellRecord>> largeLand = components.Where(c => c.Count >= largeThreshold).Take(2).ToList();
                    AssertEqual(2, largeLand.Count, "Two-continents layout creates two large landmasses");
                    AssertTrue(largeLand.All(c => c.All(cell => MapEdgeDistance(cell, map) > 0)), "Two-continents landmasses are surrounded by water");
                    AssertTrue(largeLand.All(c => c.All(cell => MapEdgeDistance(cell, map) > 2)), "Two-continents landmasses are not clipped into straight map-edge coastlines");
                }
            }
        }

        private static void AssertSmallArchipelagoAvoidsStraightProtectedEdge(RegionMapGenerator generator)
        {
            RegionGenerationOptions options = CreateRegionOptions("small-archipelago-edge");
            options.Width = 24;
            options.Height = 18;
            options.WaterLayout = "Archipelago";
            options.WaterPercent = 48;
            options.LakePercent = 0;
            options.GenerateRivers = false;
            options.GenerateFeatureNames = false;
            options.GenerateSettlements = false;
            options.GenerateStrongholds = false;
            options.GenerateDomains = false;
            options.GenerateRoads = false;
            options.GenerateRealms = false;

            HexMapRecord map = generator.Generate(options).Map;
            AssertTrue(map.Cells.Any(c => !IsWaterCell(c)), "Small archipelago creates island land");
            AssertTrue(
                MaxProtectedEdgeLandRun(map, 2) <= 4,
                "Small archipelago avoids long straight shores along the protected map edge");
        }

        private static void AssertDeepForestAppearsInEveryClimate(RegionMapGenerator generator)
        {
            foreach (string climate in new[] { "Temperate", "Tropical", "Cold", "Mixed", "Arid" })
            {
                RegionGenerationOptions options = CreateRegionOptions("deep-forest-" + climate);
                options.Width = 36;
                options.Height = 30;
                options.ClimateBelt = climate;
                options.CivilizationLevel = "Civilized";
                options.WaterLayout = "NoLargeWater";
                options.WaterPercent = 0;
                options.LakePercent = 0;
                options.GenerateRivers = false;
                options.GenerateSettlements = false;
                options.GenerateStrongholds = false;
                options.GenerateDomains = false;
                options.GenerateRoads = false;
                options.GenerateRealms = false;

                HexMapRecord map = generator.Generate(options).Map;
                AssertTrue(
                    map.Cells.Any(c => c.Water == "None" && c.Terrain == "DeepForest"),
                    "Deep forest appears for climate " + climate);
            }
        }

        private static void AssertSeismicRegionHasMoreMountains(RegionMapGenerator generator)
        {
            RegionGenerationOptions normal = CreateRegionOptions("seismic-mountain-contrast");
            normal.Width = 60;
            normal.Height = 50;
            normal.WaterLayout = "NoLargeWater";
            normal.WaterPercent = 0;
            normal.LakePercent = 0;
            normal.GenerateRivers = false;
            normal.GenerateFeatureNames = false;
            normal.GenerateSettlements = false;
            normal.GenerateStrongholds = false;
            normal.GenerateDomains = false;
            normal.GenerateRoads = false;
            normal.GenerateRealms = false;
            normal.AdvancedMode = false;
            normal.Seismicity = "Normal";

            RegionGenerationOptions stable = CreateRegionOptions("seismic-mountain-contrast");
            stable.Width = normal.Width;
            stable.Height = normal.Height;
            stable.WaterLayout = normal.WaterLayout;
            stable.WaterPercent = normal.WaterPercent;
            stable.LakePercent = normal.LakePercent;
            stable.GenerateRivers = normal.GenerateRivers;
            stable.GenerateFeatureNames = normal.GenerateFeatureNames;
            stable.GenerateSettlements = normal.GenerateSettlements;
            stable.GenerateStrongholds = normal.GenerateStrongholds;
            stable.GenerateDomains = normal.GenerateDomains;
            stable.GenerateRoads = normal.GenerateRoads;
            stable.GenerateRealms = normal.GenerateRealms;
            stable.AdvancedMode = false;
            stable.Seismicity = "Stable";

            RegionGenerationOptions seismic = CreateRegionOptions("seismic-mountain-contrast");
            seismic.Width = normal.Width;
            seismic.Height = normal.Height;
            seismic.WaterLayout = normal.WaterLayout;
            seismic.WaterPercent = normal.WaterPercent;
            seismic.LakePercent = normal.LakePercent;
            seismic.GenerateRivers = normal.GenerateRivers;
            seismic.GenerateFeatureNames = normal.GenerateFeatureNames;
            seismic.GenerateSettlements = normal.GenerateSettlements;
            seismic.GenerateStrongholds = normal.GenerateStrongholds;
            seismic.GenerateDomains = normal.GenerateDomains;
            seismic.GenerateRoads = normal.GenerateRoads;
            seismic.GenerateRealms = normal.GenerateRealms;
            seismic.AdvancedMode = false;
            seismic.Seismicity = "Seismic";

            HexMapRecord stableMap = generator.Generate(stable).Map;
            HexMapRecord normalMap = generator.Generate(normal).Map;
            HexMapRecord seismicMap = generator.Generate(seismic).Map;
            int stableMountains = stableMap.Cells.Count(c => c.Elevation == "Mountains");
            int normalMountains = normalMap.Cells.Count(c => c.Elevation == "Mountains");
            int seismicMountains = seismicMap.Cells.Count(c => c.Elevation == "Mountains");

            AssertTrue(normalMountains >= stableMountains + Math.Max(8, stableMountains / 2), "Normal seismicity creates a medium mountain profile above stable regions");
            AssertTrue(
                seismicMountains >= normalMountains + Math.Max(12, normalMountains / 2),
                "Seismic regions create visibly more mountains than normal regions");
        }

        private static void AssertCivilizedNaturalPocketsRemainUnsettled(RegionMapGenerator generator)
        {
            RegionGenerationOptions options = CreateRegionOptions("civilized-natural-pockets");
            options.Width = 44;
            options.Height = 36;
            options.CivilizationLevel = "Civilized";
            options.ClimateBelt = "Temperate";
            options.WaterLayout = "Coast";
            options.WaterPercent = 24;
            options.LakePercent = 0;
            options.SettlementDensityPercent = 70;
            options.DomainCoveragePercent = 95;
            options.GenerateSettlements = true;
            options.GenerateStrongholds = true;
            options.GenerateDomains = true;
            options.GenerateRoads = false;
            options.GenerateRealms = false;

            HexMapRecord map = generator.Generate(options).Map;
            HashSet<string> domainHexes = new HashSet<string>(
                map.Domains.SelectMany(d => d.Hexes).Select(h => TestCellKey(h.Q, h.R)));
            List<HexCellRecord> naturalBarriers = map.Cells
                .Where(c => c.Water == "None" && (c.Terrain == "Marsh" || c.Terrain == "DeepForest"))
                .ToList();

            AssertTrue(naturalBarriers.Any(c => c.Terrain == "Marsh"), "Civilized generation can still create marsh pockets");
            AssertTrue(naturalBarriers.Any(c => c.Terrain == "DeepForest"), "Civilized generation can still create deep forest pockets");
            AssertTrue(
                naturalBarriers.All(c => !domainHexes.Contains(TestCellKey(c.Q, c.R))),
                "Natural barrier pockets stay outside generated domains");
        }

        private static void AssertCivilizedArchipelagoReservesSelectedSpecialDomains(RegionMapGenerator generator)
        {
            RegionGenerationOptions options = CreateRegionOptions("civilized-archipelago-specials");
            options.Width = 56;
            options.Height = 44;
            options.CivilizationLevel = "Civilized";
            options.ClimateBelt = "Temperate";
            options.WaterLayout = "Archipelago";
            options.WaterPercent = 48;
            options.LakePercent = 0;
            options.TerrainZoneCount = 18;
            options.MountainsPercent = 14;
            options.SettlementDensityPercent = 62;
            options.DomainCoveragePercent = 95;
            options.GenerateSettlements = true;
            options.GenerateStrongholds = true;
            options.GenerateDomains = true;
            options.GenerateRoads = false;
            options.GenerateRealms = false;
            options.GenerateSpecialDomains = true;
            options.GenerateDwarvenDomains = true;
            options.GenerateElvenDomains = true;
            options.GenerateClanDomains = true;
            options.GenerateTransitionalDomains = false;
            options.UseSpecialDomainWeights = true;
            options.SpecialDomainPercent = 35;
            options.DwarvenDomainWeight = 35;
            options.ElvenDomainWeight = 35;
            options.ClanDomainWeight = 30;

            HexMapRecord map = generator.Generate(options).Map;
            AssertTrue(map.Domains.Any(d => string.Equals(d.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase)), "Selected dwarven domains reserve mountain sites before human settlement");
            AssertTrue(map.Domains.Any(d => string.Equals(d.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase)), "Selected elven domains reserve forest sites before human settlement");
            AssertTrue(map.Domains.Any(d => string.Equals(d.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)), "Selected clan domains reserve remote sites before human settlement");
        }

        private static void AssertWildRoadRulesRespectIsolation(RegionMapGenerator generator)
        {
            System.Reflection.MethodInfo method = typeof(RegionMapGenerator).GetMethod(
                "RoadPairChance",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            RegionGenerationOptions wild = new RegionGenerationOptions { CivilizationLevel = "Wild" };
            DomainRecord orcClan = new DomainRecord { DomainType = "Clanhold", Race = "Orc", Classification = "Outlands" };
            DomainRecord human = new DomainRecord { DomainType = "Ordinary", Race = "Human", Classification = "Outlands" };
            DomainRecord dwarf = new DomainRecord { DomainType = "Dwarven Vault", Race = "Dwarf", Classification = "Outlands" };

            int orcHumanChance = (int)method.Invoke(generator, new object[] { orcClan, human, wild, 3 });
            int orcDwarfChance = (int)method.Invoke(generator, new object[] { orcClan, dwarf, wild, 3 });
            AssertEqual(0, orcHumanChance, "Wild orc clanholds do not connect roads to human outlands domains");
            AssertEqual(0, orcDwarfChance, "Wild orc clanholds do not connect roads to dwarven vaults");
        }

        private static void AssertWildRoadNetworkStaysSparse(RegionMapGenerator generator)
        {
            RegionGenerationOptions options = CreateRegionOptions("wild-road-sparse");
            options.Width = 80;
            options.Height = 80;
            options.AdvancedMode = false;
            options.CivilizationLevel = "Wild";
            options.WaterLayout = "Coast";
            options.GenerateSettlements = true;
            options.GenerateStrongholds = true;
            options.GenerateDomains = true;
            options.GenerateRoads = true;
            options.GenerateRealms = false;
            options.GenerateSpecialDomains = true;
            options.GenerateClanDomains = true;
            options.GenerateDwarvenDomains = true;
            options.GenerateElvenDomains = true;
            options.GenerateTransitionalDomains = false;

            HexMapRecord map = generator.Generate(options).Map;
            AssertTrue(map.Settlements.Count < 40, "Wild generation keeps large empty spaces between settlements");
            AssertTrue(map.Roads.Count <= Math.Max(12, map.Settlements.Count * 5), "Wild road generation keeps the road network sparse");
        }

        private static bool IsWaterCell(HexCellRecord cell)
        {
            return cell != null && (cell.Water == "Sea" || cell.Water == "Ocean" || cell.Water == "Lake");
        }

        private static int WaterSpanVariation(HexMapRecord map)
        {
            if (map == null || map.Cells == null) return 0;

            int rowVariation = map.Cells
                .GroupBy(c => c.R)
                .Select(g => g.Count(IsWaterCell))
                .Where(count => count > 0 && count < map.Width)
                .Distinct()
                .Count();
            int columnVariation = map.Cells
                .GroupBy(c => c.Q)
                .Select(g => g.Count(IsWaterCell))
                .Where(count => count > 0 && count < map.Height)
                .Distinct()
                .Count();
            return Math.Max(rowVariation, columnVariation);
        }

        private static List<int> LandComponentSizes(HexMapRecord map)
        {
            return LandComponents(map).Select(c => c.Count).OrderByDescending(c => c).ToList();
        }

        private static List<List<HexCellRecord>> LandComponents(HexMapRecord map)
        {
            List<List<HexCellRecord>> result = new List<List<HexCellRecord>>();
            if (map == null || map.Cells == null) return result;

            Dictionary<string, HexCellRecord> cells = map.Cells.ToDictionary(c => TestCellKey(c.Q, c.R));
            HashSet<string> visited = new HashSet<string>();
            foreach (HexCellRecord start in map.Cells.Where(c => !IsWaterCell(c)))
            {
                string startKey = TestCellKey(start.Q, start.R);
                if (!visited.Add(startKey)) continue;

                List<HexCellRecord> component = new List<HexCellRecord>();
                Queue<HexCellRecord> queue = new Queue<HexCellRecord>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    HexCellRecord cell = queue.Dequeue();
                    component.Add(cell);
                    foreach (HexCellRecord neighbor in TestNeighbors(map, cell, cells))
                    {
                        if (IsWaterCell(neighbor)) continue;
                        string key = TestCellKey(neighbor.Q, neighbor.R);
                        if (!visited.Add(key)) continue;
                        queue.Enqueue(neighbor);
                    }
                }

                result.Add(component);
            }

            return result;
        }

        private static IEnumerable<HexCellRecord> TestNeighbors(HexMapRecord map, HexCellRecord cell, Dictionary<string, HexCellRecord> cells)
        {
            int[][] directions = (cell.R & 1) == 1
                ? new[]
                {
                    new[] { 1, 0 },
                    new[] { -1, 0 },
                    new[] { 1, -1 },
                    new[] { 0, -1 },
                    new[] { 1, 1 },
                    new[] { 0, 1 }
                }
                : new[]
                {
                    new[] { 1, 0 },
                    new[] { -1, 0 },
                    new[] { 0, -1 },
                    new[] { -1, -1 },
                    new[] { 0, 1 },
                    new[] { -1, 1 }
                };

            foreach (int[] direction in directions)
            {
                HexCellRecord neighbor;
                if (cells.TryGetValue(TestCellKey(cell.Q + direction[0], cell.R + direction[1]), out neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        private static string TestCellKey(int q, int r)
        {
            return q + "," + r;
        }

        private static int MapEdgeDistance(HexCellRecord cell, HexMapRecord map)
        {
            return Math.Min(
                Math.Min(cell.Q, map.Width - 1 - cell.Q),
                Math.Min(cell.R, map.Height - 1 - cell.R));
        }

        private static int MaxProtectedEdgeLandRun(HexMapRecord map, int edgeDistance)
        {
            int max = 0;
            max = Math.Max(max, CountLandRun(map, edgeDistance, 0, 0, 1));
            max = Math.Max(max, CountLandRun(map, map.Width - 1 - edgeDistance, 0, 0, 1));
            max = Math.Max(max, CountLandRun(map, 0, edgeDistance, 1, 0));
            max = Math.Max(max, CountLandRun(map, 0, map.Height - 1 - edgeDistance, 1, 0));

            return max;
        }

        private static int CountLandRun(HexMapRecord map, int q, int r, int dq, int dr)
        {
            int run = 0;
            int max = 0;
            while (q >= 0 && q < map.Width && r >= 0 && r < map.Height)
            {
                HexCellRecord cell = map.Cells.FirstOrDefault(c => c.Q == q && c.R == r);
                if (cell != null && !IsWaterCell(cell))
                {
                    run++;
                    max = Math.Max(max, run);
                }
                else
                {
                    run = 0;
                }

                q += dq;
                r += dr;
            }

            return max;
        }

        private static int MinSettlementDistance(List<MapSettlementRecord> settlements)
        {
            if (settlements == null || settlements.Count < 2) return 999;

            int min = int.MaxValue;
            for (int i = 0; i < settlements.Count; i++)
            {
                for (int j = i + 1; j < settlements.Count; j++)
                {
                    min = Math.Min(min, TestHexDistance(settlements[i].Q, settlements[i].R, settlements[j].Q, settlements[j].R));
                }
            }

            return min;
        }

        private static int TestHexDistance(int q1, int r1, int q2, int r2)
        {
            int aq1 = q1 - ((r1 - (r1 & 1)) / 2);
            int aq2 = q2 - ((r2 - (r2 & 1)) / 2);
            return (Math.Abs(aq1 - aq2) + Math.Abs(aq1 + r1 - aq2 - r2) + Math.Abs(r1 - r2)) / 2;
        }

        private static HexMapRecord CreateFlatMap(int width, int height)
        {
            HexMapRecord map = new HexMapRecord { Width = width, Height = height };
            map.Cells.Clear();
            for (int r = 0; r < height; r++)
            {
                for (int q = 0; q < width; q++)
                {
                    map.Cells.Add(new HexCellRecord { Q = q, R = r, Terrain = "Grasslands", Elevation = "Plains", Water = "None" });
                }
            }

            return map;
        }

        private static DomainRecord CreateTinyDomain(string id, string settlementId, int q, int r)
        {
            DomainRecord domain = new DomainRecord
            {
                Id = id,
                Name = id,
                CapitalSettlementId = settlementId,
                Classification = "Outlands",
                DomainType = "Ordinary",
                Race = "Human"
            };
            domain.SettlementIds.Add(settlementId);
            domain.Hexes.Add(new DomainHexRecord { Q = q, R = r, LandValueGp = 6 });
            return domain;
        }

        private static RegionGenerationOptions CreateRegionOptions(string seed)
        {
            return new RegionGenerationOptions
            {
                Seed = seed,
                Width = 24,
                Height = 18,
                AdvancedMode = true,
                CivilizationLevel = "Wild",
                RealmScale = "Balanced",
                CultureKey = "english",
                WaterLayout = "Inland",
                WaterPercent = 0,
                LakePercent = 0,
                RiverPercent = 0,
                GenerateRivers = false,
                GenerateFeatureNames = false,
                TerrainZoneCount = 8,
                TerrainChaosPercent = 20,
                HillsPercent = 12,
                MountainsPercent = 6,
                SettlementDensityPercent = 45,
                DomainCoveragePercent = 85,
                RealmCount = 2,
                StateSizeVariancePercent = 20,
                LandValueMode = "Fixed6",
                GenerateRulers = false
            };
        }

        private static string LandscapeSignature(HexMapRecord map)
        {
            return string.Join(";",
                map.Cells
                    .OrderBy(c => c.R)
                    .ThenBy(c => c.Q)
                    .Select(c => c.Q + "," + c.R + ":" + c.Terrain + "/" + c.Elevation + "/" + c.Water + "/" + c.WaterFeatureName));
        }

        private static void AssertRealmHierarchyRanks(HexMapRecord map)
        {
            Dictionary<string, RealmRecord> realms = map.Realms
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
                .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

            foreach (VassalLinkRecord link in map.VassalLinks.Where(l => l != null))
            {
                RealmRecord liege;
                RealmRecord vassal;
                if (!realms.TryGetValue(link.LiegeRealmId, out liege) || !realms.TryGetValue(link.VassalRealmId, out vassal)) continue;
                AssertTrue(RealmTierRank(liege.Tier) > RealmTierRank(vassal.Tier), "Generated liege realm has a higher tier than its vassal");
            }
        }

        private static int MaxRealmHierarchyDepth(HexMapRecord map)
        {
            if (map == null || map.Realms == null || map.VassalLinks == null) return 0;

            HashSet<string> vassals = new HashSet<string>(
                map.VassalLinks.Where(l => l != null).Select(l => l.VassalRealmId).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            List<string> roots = map.Realms
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id) && !vassals.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();
            if (roots.Count == 0)
            {
                roots = map.Realms.Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id)).Select(r => r.Id).ToList();
            }

            Dictionary<string, List<string>> children = map.VassalLinks
                .Where(l => l != null)
                .GroupBy(l => l.LiegeRealmId ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(l => l.VassalRealmId).Where(id => !string.IsNullOrWhiteSpace(id)).ToList(), StringComparer.OrdinalIgnoreCase);

            int best = 0;
            foreach (string root in roots)
            {
                best = Math.Max(best, RealmHierarchyDepth(root, children, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            }

            return best;
        }

        private static int CountIndependentRealms(HexMapRecord map)
        {
            if (map == null || map.Realms == null) return 0;
            HashSet<string> vassals = new HashSet<string>(
                (map.VassalLinks ?? new List<VassalLinkRecord>())
                    .Where(l => l != null)
                    .Select(l => l.VassalRealmId)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            return map.Realms.Count(r => r != null && !string.IsNullOrWhiteSpace(r.Id) && !vassals.Contains(r.Id));
        }

        private static int CountIndependentBaronies(HexMapRecord map)
        {
            if (map == null || map.Realms == null) return 0;
            HashSet<string> vassals = new HashSet<string>(
                (map.VassalLinks ?? new List<VassalLinkRecord>())
                    .Where(l => l != null)
                    .Select(l => l.VassalRealmId)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            return map.Realms.Count(r =>
                r != null
                && string.Equals(r.Tier, "Barony", StringComparison.OrdinalIgnoreCase)
                && !vassals.Contains(r.Id));
        }

        private static int RealmHierarchyDepth(string realmId, Dictionary<string, List<string>> children, HashSet<string> visited)
        {
            if (string.IsNullOrWhiteSpace(realmId) || !visited.Add(realmId)) return 0;

            List<string> direct;
            if (!children.TryGetValue(realmId, out direct) || direct.Count == 0) return 1;

            int bestChild = 0;
            foreach (string child in direct)
            {
                bestChild = Math.Max(bestChild, RealmHierarchyDepth(child, children, visited));
            }

            return 1 + bestChild;
        }

        private static int RealmTierRank(string tier)
        {
            if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private static void TestTreasureGeneration()
        {
            TreasureGenerator treasureGenerator = new TreasureGenerator();
            TreasureHoardResult planned = treasureGenerator.GenerateForTargetValue(
                2888,
                TreasureTableMode.Classic,
                "Incidental",
                new Random(1234));

            AssertEqual("I", planned.TreasureType, "Treasure target value chooses the closest ACKS treasure type");
            AssertEqual(TreasureTableMode.Classic, planned.TableMode, "Planned dungeon treasure uses the requested table mode");
            AssertTrue(planned.Entries.Count > 0, "Treasure type generation creates entries");

            TreasureHoardResult heroic = treasureGenerator.Generate(new TreasureGenerationOptions
            {
                TableMode = TreasureTableMode.Heroic,
                TreasureType = "Q"
            }, new Random(5678));
            AssertEqual(TreasureTableMode.Heroic, heroic.TableMode, "Heroic treasure table is selectable");
            AssertTrue(heroic.Entries.Any(e => e.Category == "Magic Items"), "Heroic treasure can generate magic items by rarity");
            string russianTreasure = treasureGenerator.Format(heroic, true, 12345);
            AssertTrue(russianTreasure.IndexOf("Seed: 12345", StringComparison.OrdinalIgnoreCase) >= 0,
                "Treasure output includes the seed used for repeatability");
            AssertTrue(russianTreasure.IndexOf("Героическая", StringComparison.OrdinalIgnoreCase) >= 0,
                "Russian treasure output localizes table mode");
            AssertTrue(russianTreasure.IndexOf("Средняя стоимость магии по таблице", StringComparison.OrdinalIgnoreCase) >= 0,
                "Russian treasure output explains tabular magic item value");
            AssertEqual("1,000 зм", treasureGenerator.FormatEntry(new TreasureEntry { Category = "Coins", Description = "1,000gp" }, true),
                "Russian treasure entries localize gp abbreviations");
            AssertEqual("1 безделушка", treasureGenerator.FormatEntry(new TreasureEntry { Category = "Special", Description = "1 trinket" }, true),
                "Russian treasure entries localize special treasure fallbacks");
            string localizedSpecialTreasure = treasureGenerator.FormatEntry(new TreasureEntry
            {
                Category = "Special",
                Description = "1d3 bundles of large uncommon fur pelts, worth 2d4x50gp each"
            }, true);
            AssertTrue(localizedSpecialTreasure.IndexOf("связок больших необычных меховых шкур", StringComparison.OrdinalIgnoreCase) >= 0,
                "Russian treasure entries localize special treasure lots");
            AssertFalse(localizedSpecialTreasure.IndexOf("bundles", StringComparison.OrdinalIgnoreCase) >= 0,
                "Russian special treasure output does not leave English lot nouns");
            AssertTrue(treasureGenerator.FormatEntry(new TreasureEntry { Category = "Magic Items", Description = "Spell Scroll (1 level)" }, true)
                    .IndexOf("Свиток заклинания", StringComparison.OrdinalIgnoreCase) >= 0,
                "Direct Russian treasure formatting localizes magic item names");

            TreasureEntry gem = treasureGenerator.RollGem(TreasureGemKind.Brilliant, new Random(1));
            TreasureEntry jewelry = treasureGenerator.RollJewelry(TreasureJewelryKind.Regalia, new Random(2));
            TreasureEntry special = treasureGenerator.RollSpecialTreasure("gp", new Random(3));
            AssertTrue(gem.ValueGp >= 500, "Brilliant gem generation uses the high-value gem table");
            AssertTrue(jewelry.ValueGp >= 1000, "Regalia generation uses the high-value jewelry table");
            AssertTrue(!string.IsNullOrWhiteSpace(special.Description), "Special treasure generation returns a lot result");
            AssertTrue(!string.IsNullOrWhiteSpace(treasureGenerator.RollMagicItem(TreasureTableMode.Classic, "Potion", new Random(4))),
                "Classic magic item generation can roll by item type");
            AssertTrue(!string.IsNullOrWhiteSpace(treasureGenerator.RollMagicItem(TreasureTableMode.Heroic, "Rare", new Random(5))),
                "Heroic magic item generation can roll by rarity");
            System.Reflection.MethodInfo unprotectedTreasureMethod = typeof(DungeonGenerator).GetMethod(
                "PickUnprotectedTreasure",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            string unprotectedTreasure = (string)unprotectedTreasureMethod.Invoke(
                new DungeonGenerator(),
                new object[] { 1, new Random(6), false });
            AssertTrue(unprotectedTreasure.IndexOf("Unprotected treasure d6=", StringComparison.OrdinalIgnoreCase) >= 0,
                "Empty and trap room treasure uses the STEP 9 unprotected treasure roll");
            AssertTrue(unprotectedTreasure.IndexOf("TT ", StringComparison.OrdinalIgnoreCase) >= 0,
                "Unprotected treasure is generated through a Treasure Type row");

            DungeonRecord lair = new DungeonGenerator().Generate(new DungeonGenerationOptions
            {
                Seed = "treasure-lair",
                DungeonType = "Monster lair",
                Size = "Lair",
                RecommendedLevel = 4,
                RussianOutput = false
            });
            List<DungeonRoomRecord> lairMonsterRooms = lair.Levels.SelectMany(l => l.Rooms)
                .Where(r => string.Equals(r.Kind, "Monster", StringComparison.OrdinalIgnoreCase))
                .ToList();
            AssertTrue(lairMonsterRooms.Count > 0, "Generated monster lair includes a resident monster room");
            DungeonRoomRecord lairMonsterRoom = lairMonsterRooms[0];
            AssertTrue(lairMonsterRoom.MonsterCount > 0 && lairMonsterRoom.Monster.IndexOf("(", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dungeon monster rooms roll exact quantity and keep the dice expression in parentheses");
            AssertEqual(lairMonsterRoom.MonsterCount * lairMonsterRoom.MonsterXpEach, lairMonsterRoom.MonsterXpTotal,
                "Dungeon monster rooms store total monster XP from exact count");
            AssertTrue(lairMonsterRoom.MonsterLair, "Size Lair monster rooms are marked as lairs");
            if (!DungeonTreasureTypeIsNone(lairMonsterRoom.MonsterTreasureType))
            {
                AssertTrue(!string.IsNullOrWhiteSpace(lairMonsterRoom.Treasure), "Monster lairs with a treasure type receive lair treasure");
                AssertTrue(lairMonsterRoom.Treasure.IndexOf("TT ", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Dungeon treasure is generated through the treasure type process");
                AssertTrue(lairMonsterRoom.Treasure.IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Dungeon lair treasure records monster XP context");
                AssertFalse(lairMonsterRoom.Treasure.IndexOf(" gp in ", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Dungeon treasure no longer uses the old simplified gp-in-kind string");
            }
        }

        private static void TestDungeonGeneration()
        {
            DungeonGenerator dungeonGenerator = new DungeonGenerator();
            DungeonRecord dungeon = dungeonGenerator.Generate(new DungeonGenerationOptions
            {
                Seed = "4242",
                DungeonType = "Natural caverns",
                Size = "Small",
                RecommendedLevel = 3,
                RussianOutput = true
            });

            AssertTrue(dungeon.Levels.Count >= 1, "Dungeon generation creates at least one level");
            AssertTrue(dungeon.Levels.Sum(l => l.Rooms.Count) >= 2, "Dungeon generation creates rooms");
            AssertTrue(dungeon.WanderingEncounters.Count > 0, "Dungeon generation creates wandering encounter table");
            foreach (DungeonLevelRecord level in dungeon.Levels)
            {
                AssertEqual(12, dungeon.WanderingEncounters.Count(e => e.DungeonLevel == level.LevelNumber),
                    "Dungeon generation creates a d12 wandering table for every dungeon level");
            }
            AssertTrue(dungeon.WanderingEncounters.All(e => e.Roll >= 1 && e.Roll <= 12),
                "Dungeon wandering encounter rolls use d12 rows");
            AssertTrue(dungeon.WanderingEncounters.All(e => e.MonsterLevel >= 1 && e.MonsterLevel <= 6),
                "Dungeon wandering encounters store ACKS monster level");
            AssertTrue(dungeon.Levels.Any(l => l.Connections.Count > 0), "Dungeon generation connects rooms");
            foreach (DungeonLevelRecord level in dungeon.Levels)
            {
                for (int i = 0; i < level.Rooms.Count; i++)
                {
                    for (int j = i + 1; j < level.Rooms.Count; j++)
                    {
                        AssertFalse(DungeonRoomsOverlap(level.Rooms[i], level.Rooms[j]),
                            "Dungeon generation does not place rooms on top of each other");
                    }
                }
            }
            AssertTrue(dungeon.Levels.SelectMany(l => l.Rooms).All(r => !string.IsNullOrWhiteSpace(r.Shape)),
                "Dungeon rooms have editable shapes");
            DungeonRoomRecord roundRoom = new DungeonRoomRecord { X = 10, Y = 10, Width = 4, Height = 4, Shape = "Circle" };
            double roundEdgeX;
            double roundEdgeY;
            string roundEdgeOrientation;
            DungeonGeometry.FindRoomEdgePoint(roundRoom, 14, 14, out roundEdgeX, out roundEdgeY, out roundEdgeOrientation);
            AssertTrue(roundEdgeX < 14 && roundEdgeY < 14,
                "Dungeon geometry anchors circular room corridors to the visible room boundary, not the bounding box corner");
            AssertTrue(dungeon.Levels.SelectMany(l => l.Connections).All(c => c.PassageWidth >= 1),
                "Dungeon connections have corridor width metadata");
            AssertTrue(dungeon.Levels.SelectMany(l => l.Connections)
                    .All(c => !string.Equals(c.Kind, "Door", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(c.Kind, "SecretDoor", StringComparison.OrdinalIgnoreCase)),
                "Dungeon connection kind stays separate from door metadata");
            AssertTrue(dungeon.Levels.SelectMany(l => l.Connections)
                    .All(c => string.IsNullOrWhiteSpace(c.DoorKind)),
                "Dungeon connections do not store generated doors inline");
            AssertTrue(dungeon.Levels.SelectMany(l => l.Rooms)
                    .Where(r => string.Equals(r.Kind, "Monster", StringComparison.OrdinalIgnoreCase))
                    .All(r => !string.IsNullOrWhiteSpace(r.Monster) && r.Monster.Any(char.IsDigit)),
                "Dungeon monster rooms include monster quantity");
            System.Reflection.MethodInfo pickTrapMethod = typeof(DungeonGenerator).GetMethod(
                "PickTrap",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            string englishTrap = (string)pickTrapMethod.Invoke(dungeonGenerator, new object[] { 1, new Random(21), false });
            AssertTrue(englishTrap.IndexOf("trap level 1", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dungeon traps use dungeon trap level rather than only room stocking text");
            AssertTrue(englishTrap.IndexOf("trigger:", StringComparison.OrdinalIgnoreCase) >= 0
                    && englishTrap.IndexOf("activates on 1-2 on 1d6", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dungeon traps include a STEP 7 trigger mechanism and activation roll");
            AssertTrue(englishTrap.IndexOf("effect:", StringComparison.OrdinalIgnoreCase) >= 0
                    && englishTrap.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dungeon traps include level-based effect and damage details");
            string russianTrap = (string)pickTrapMethod.Invoke(dungeonGenerator, new object[] { 1, new Random(21), true });
            AssertTrue(russianTrap.IndexOf("уровень ловушки 1", StringComparison.OrdinalIgnoreCase) >= 0
                    && russianTrap.IndexOf("механизм:", StringComparison.OrdinalIgnoreCase) >= 0
                    && russianTrap.IndexOf("эффект:", StringComparison.OrdinalIgnoreCase) >= 0,
                "Russian dungeon trap output includes level, trigger, and effect fields");
            string highLevelTrap = (string)pickTrapMethod.Invoke(dungeonGenerator, new object[] { 6, new Random(22), false });
            AssertTrue(highLevelTrap.IndexOf("trap level 6", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dungeon traps can use the highest ACKS trap level");
            DungeonRecord singleRoomLair = new DungeonRecord
            {
                DungeonType = "Monster lair",
                Size = "Lair",
                RecommendedLevel = 4
            };
            singleRoomLair.Levels.Add(new DungeonLevelRecord
            {
                LevelNumber = 1,
                Rooms = new List<DungeonRoomRecord>
                {
                    new DungeonRoomRecord
                    {
                        Id = "single-lair-room",
                        LevelNumber = 1,
                        X = 2,
                        Y = 2,
                        Width = 3,
                        Height = 3,
                        Shape = "Rectangle",
                        Kind = "Empty"
                    }
                }
            });

            System.Reflection.MethodInfo ensureSingleLairMethod = typeof(DungeonGenerator).GetMethod(
                "EnsureSingleRoomLairIsOccupied",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            ensureSingleLairMethod.Invoke(
                dungeonGenerator,
                new object[] { singleRoomLair, 4, "Lair", "Monster lair", new Random(17), true });

            AssertEqual(1, singleRoomLair.Levels.Sum(l => l.Rooms.Count),
                "Dungeon test fixture contains a single-room monster lair");
            DungeonRoomRecord singleLairRoom = singleRoomLair.Levels.SelectMany(l => l.Rooms).Single();
            AssertEqual("Monster", singleLairRoom.Kind, "One-room monster lairs are occupied by the resident monster");
            AssertTrue(!string.IsNullOrWhiteSpace(singleLairRoom.Monster) && singleLairRoom.Monster.Any(char.IsDigit),
                "One-room monster lairs include monster quantity");
            AssertTrue(singleLairRoom.MonsterCount > 0 && singleLairRoom.MonsterXpTotal == singleLairRoom.MonsterCount * singleLairRoom.MonsterXpEach,
                "One-room monster lairs store exact count and total XP");
            if (!DungeonTreasureTypeIsNone(singleLairRoom.MonsterTreasureType))
            {
                AssertTrue(!string.IsNullOrWhiteSpace(singleLairRoom.Treasure),
                    "One-room monster lairs with a treasure type include lair treasure");
            }
            for (int seed = 6200; seed < 6210; seed++)
            {
                DungeonRecord generatedLair = dungeonGenerator.Generate(new DungeonGenerationOptions
                {
                    Seed = seed.ToString(CultureInfo.InvariantCulture),
                    DungeonType = seed % 2 == 0 ? "Monster lair" : "Natural caverns",
                    Size = "Lair",
                    RecommendedLevel = 3,
                    RussianOutput = true
                });
                List<DungeonRoomRecord> lairRooms = generatedLair.Levels.SelectMany(l => l.Rooms).ToList();
                AssertTrue(lairRooms.All(r => !string.Equals(r.Kind, "Entrance", StringComparison.OrdinalIgnoreCase)),
                    "Size Lair dungeons do not replace a stocked room with an empty entrance");
                AssertTrue(lairRooms.All(r => !string.Equals(r.Kind, "Trap", StringComparison.OrdinalIgnoreCase)),
                    "Size Lair dungeons do not generate trap rooms");
                if (lairRooms.Count == 1)
                {
                    AssertEqual("Monster", lairRooms[0].Kind,
                        "Single-room size Lair dungeons are occupied rooms instead of entrances");
                    AssertTrue(!string.IsNullOrWhiteSpace(lairRooms[0].Monster) && lairRooms[0].Monster.Any(char.IsDigit),
                        "Single-room size Lair dungeons include a monster quantity");
                    AssertTrue(lairRooms[0].MonsterCount > 0 && lairRooms[0].Monster.IndexOf("(", StringComparison.OrdinalIgnoreCase) >= 0,
                        "Single-room size Lair dungeons roll exact monster quantity");
                    if (!DungeonTreasureTypeIsNone(lairRooms[0].MonsterTreasureType))
                    {
                        AssertTrue(!string.IsNullOrWhiteSpace(lairRooms[0].Treasure),
                            "Single-room size Lair dungeons with a treasure type include treasure");
                    }
                }
            }
            AssertTrue(dungeon.Levels.SelectMany(l => l.Doors)
                    .All(d => d.Kind == "Door" || d.Kind == "SecretDoor" || d.Kind == "SecretPassage"),
                "Dungeon doors are separate editable entities");
            foreach (DungeonLevelRecord level in dungeon.Levels)
            {
                AssertTrue(DungeonLevelRoomsAreConnected(level),
                    "Dungeon generation keeps every room reachable through local level connections");
                AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsUnlinkedRoomInterior(level, c)),
                    "Dungeon generation routes corridors around unrelated rooms");
                AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsUnlinkedRoomBuffer(level, c)),
                    "Dungeon generation keeps full corridor width out of unrelated rooms");
                AssertTrue(level.Connections.All(c => DungeonConnectionEndpointsStayOnLinkedRoomBoundary(level, c)),
                    "Dungeon generation anchors corridor path endpoints on linked room boundaries");
                AssertTrue(level.Connections.All(c => DungeonConnectionEntrancesAreFullWidth(level, c)),
                    "Dungeon generation connects corridors to room walls with full-width entrances");
                AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsLinkedRoomInterior(level, c)),
                    "Dungeon generation does not leave corridor path points inside linked rooms");
                AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsLinkedRoomBuffer(level, c)),
                    "Dungeon generation does not route corridor width under linked rooms after the entrance segment");
                AssertTrue(level.Connections.All(c => DungeonConnectionUsesTwoPointPathWhenDirectRouteIsClear(level, c)),
                    "Dungeon generation does not add bends to clear near-straight corridor routes");
                AssertTrue(level.Connections.All(c => DungeonConnectionUsesDirectBoundaryPathWhenClear(level, c)),
                    "Dungeon generation keeps clean boundary-to-boundary corridor routes as direct two-point paths");
                AssertTrue(DungeonBoxRoomConnectionsAreOrthogonal(level),
                    "Dungeon generation keeps rectangular-room corridor segments orthogonal");
                AssertTrue(DungeonConnectionsAvoidCloseParallelRuns(level),
                    "Dungeon generation does not create side-by-side duplicate corridor runs");
                AssertTrue(DungeonConnectionsAvoidSelfParallelRuns(level),
                    "Dungeon generation does not fold a corridor into a tight parallel loop");
                AssertTrue(DungeonConnectionsAvoidTinySteps(level),
                    "Dungeon generation does not leave tiny decorative steps in corridor routes");
                AssertTrue(level.Doors.All(d => DungeonDoorIsOnLinkedRoomBoundary(level, d)),
                    "Generated dungeon doors are anchored on linked room boundaries");
                AssertTrue(level.Doors.All(d => DungeonDoorAvoidsLinkedRoomInterior(level, d)),
                    "Generated dungeon doors do not drift into linked room interiors");
            }

            DungeonGenerationOptions seededLowLevelOptions = new DungeonGenerationOptions
            {
                Seed = "same-visible-seed",
                DungeonType = "Natural caverns",
                Size = "Large",
                RecommendedLevel = 2
            };
            DungeonGenerationOptions seededHighLevelOptions = new DungeonGenerationOptions
            {
                Seed = "same-visible-seed",
                DungeonType = "Natural caverns",
                Size = "Large",
                RecommendedLevel = 8
            };
            DungeonRecord seededLowLevel = dungeonGenerator.Generate(seededLowLevelOptions);
            DungeonRecord seededLowLevelRepeat = dungeonGenerator.Generate(seededLowLevelOptions);
            DungeonRecord seededHighLevel = dungeonGenerator.Generate(seededHighLevelOptions);
            AssertEqual(DungeonCatalog.MaxDungeonLevel, seededHighLevel.RecommendedLevel,
                "Dungeon recommended danger level is capped at the ACKS maximum");
            AssertEqual(DungeonPlanSignature(seededLowLevel), DungeonPlanSignature(seededLowLevelRepeat),
                "Same dungeon seed and same settings repeat the plan");
            AssertFalse(DungeonPlanSignature(seededLowLevel) == DungeonPlanSignature(seededHighLevel),
                "Dungeon recommended level participates in seeded plan generation");

            DungeonRecord manyFloorDungeon = new DungeonRecord
            {
                RecommendedLevel = 99,
                DungeonType = "Natural caverns",
                Size = "Megadungeon"
            };
            for (int floor = 1; floor <= DungeonCatalog.MaxDungeonLevel + 2; floor++)
            {
                manyFloorDungeon.Levels.Add(new DungeonLevelRecord { LevelNumber = floor });
            }
            MapDataNormalizer.NormalizeDungeon(manyFloorDungeon);
            AssertEqual(DungeonCatalog.MaxDungeonLevel, manyFloorDungeon.RecommendedLevel,
                "Imported dungeon danger level is capped at the ACKS maximum");
            AssertEqual(DungeonCatalog.MaxDungeonLevel + 2, manyFloorDungeon.Levels.Count,
                "Dungeon floor geometry is not capped by ACKS danger-level range");

            for (int seed = 5100; seed < 5110; seed++)
            {
                DungeonRecord generated = dungeonGenerator.Generate(new DungeonGenerationOptions
                {
                    Seed = seed.ToString(CultureInfo.InvariantCulture),
                    DungeonType = seed % 2 == 0 ? "Natural caverns" : "Temple",
                    Size = seed % 3 == 0 ? "Large" : "Small",
                    RecommendedLevel = 1 + seed % 6,
                    RussianOutput = true
                });

                foreach (DungeonLevelRecord level in generated.Levels)
                {
                    AssertTrue(level.Connections.All(c => DungeonConnectionEndpointsStayOnLinkedRoomBoundary(level, c)),
                        "Dungeon generation keeps varied-seed corridor endpoints on linked room boundaries: seed " + seed + ", level " + level.LevelNumber);
                    DungeonConnectionRecord badLinkedInteriorConnection = level.Connections
                        .FirstOrDefault(c => !DungeonConnectionAvoidsLinkedRoomInterior(level, c));
                    AssertTrue(badLinkedInteriorConnection == null,
                        "Dungeon generation keeps varied-seed corridor routes out of linked room interiors: seed "
                        + seed + ", level " + level.LevelNumber + ", " + DungeonConnectionDebugText(level, badLinkedInteriorConnection));
                    AssertTrue(level.Doors.All(d => DungeonDoorIsOnLinkedRoomBoundary(level, d)),
                        "Dungeon generation keeps varied-seed doors on linked room boundaries: seed " + seed + ", level " + level.LevelNumber);
                    AssertTrue(level.Doors.All(d => DungeonDoorAvoidsLinkedRoomInterior(level, d)),
                        "Dungeon generation keeps varied-seed doors out of linked room interiors: seed " + seed + ", level " + level.LevelNumber);
                    DungeonConnectionRecord badOrthogonalConnection = DungeonFirstNonOrthogonalBoxConnection(level);
                    AssertTrue(badOrthogonalConnection == null,
                        "Dungeon generation keeps varied-seed rectangular-room corridor segments orthogonal: seed "
                        + seed + ", level " + level.LevelNumber + ", " + DungeonConnectionDebugText(level, badOrthogonalConnection));
                    string closeParallelText = DungeonFirstCloseParallelRunDebugText(level);
                    AssertTrue(string.IsNullOrEmpty(closeParallelText),
                        "Dungeon generation keeps varied-seed corridors from running side by side: seed "
                        + seed + ", level " + level.LevelNumber + ", " + closeParallelText);
                    AssertTrue(DungeonConnectionsAvoidTinySteps(level),
                        "Dungeon generation keeps varied-seed corridors free of tiny decorative steps: seed " + seed + ", level " + level.LevelNumber);
                    AssertTrue(DungeonConnectionsAvoidSelfParallelRuns(level),
                        "Dungeon generation keeps varied-seed corridors free of tight self-parallel loops: seed " + seed + ", level " + level.LevelNumber);
                    AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsLinkedRoomBuffer(level, c)),
                        "Dungeon generation keeps varied-seed corridor width out from under linked rooms: seed " + seed + ", level " + level.LevelNumber);
                    DungeonConnectionRecord bentDirectConnection = level.Connections
                        .FirstOrDefault(c => !DungeonConnectionUsesDirectBoundaryPathWhenClear(level, c));
                    AssertTrue(bentDirectConnection == null,
                        "Dungeon generation keeps varied-seed clear boundary-to-boundary routes direct: seed "
                        + seed + ", level " + level.LevelNumber + ", " + DungeonConnectionDebugText(level, bentDirectConnection));
                }
            }

            string[] corridorStressTypes = { "Temple", "Natural caverns", "Wizard's dungeon" };
            for (int seed = 5000; seed < 5006; seed++)
            {
                foreach (string dungeonType in corridorStressTypes)
                {
                    DungeonRecord generated = dungeonGenerator.Generate(new DungeonGenerationOptions
                    {
                        Seed = seed.ToString(CultureInfo.InvariantCulture),
                        DungeonType = dungeonType,
                        Size = "Large",
                        RecommendedLevel = 4,
                        RussianOutput = true
                    });

                    foreach (DungeonLevelRecord level in generated.Levels)
                    {
                        DungeonConnectionRecord tinyStepConnection = DungeonFirstTinyStepConnection(level);
                        AssertTrue(tinyStepConnection == null,
                            "Dungeon generation stress seeds avoid short Z-shaped corridor steps: seed "
                            + seed + ", type " + dungeonType + ", level " + level.LevelNumber + ", " + DungeonConnectionDebugText(level, tinyStepConnection));
                        AssertTrue(DungeonConnectionsAvoidSelfParallelRuns(level),
                            "Dungeon generation stress seeds avoid tight self-parallel corridor loops: seed " + seed + ", type " + dungeonType + ", level " + level.LevelNumber);
                        AssertTrue(level.Connections.All(c => DungeonConnectionAvoidsLinkedRoomBuffer(level, c)),
                            "Dungeon generation stress seeds keep corridor width out from under linked rooms: seed " + seed + ", type " + dungeonType + ", level " + level.LevelNumber);
                        AssertTrue(level.Doors.All(d => DungeonDoorIsOnLinkedRoomBoundary(level, d) && DungeonDoorAvoidsLinkedRoomInterior(level, d)),
                            "Dungeon generation stress seeds keep doors on room boundaries: seed " + seed + ", type " + dungeonType + ", level " + level.LevelNumber);
                    }
                }
            }
        }

        private static void TestHexFeatures()
        {
            HexCellRecord plains = new HexCellRecord { Terrain = "Grasslands", Elevation = "Plains", Water = "None" };
            HexCellRecord forest = new HexCellRecord { Terrain = "Forest", Elevation = "Plains", Water = "None" };
            HexCellRecord mountain = new HexCellRecord { Terrain = "Grasslands", Elevation = "Mountains", Water = "None" };

            AssertFalse(DungeonCatalog.IsDungeonTypeAllowed("Sewers", new DungeonPlacementContext { Cell = plains, Settlement = new MapSettlementRecord { MarketClass = 6 } }),
                "Class VI settlements cannot host sewers");
            AssertTrue(DungeonCatalog.IsDungeonTypeAllowed("Sewers", new DungeonPlacementContext { Cell = plains, Settlement = new MapSettlementRecord { MarketClass = 4 } }),
                "Large settlements can host sewers");
            AssertFalse(DungeonCatalog.IsDungeonTypeAllowed("Lost city", new DungeonPlacementContext { Cell = plains, Settlement = new MapSettlementRecord { MarketClass = 2 } }),
                "Settlements cannot host lost cities");
            AssertTrue(DungeonCatalog.IsDungeonTypeAllowed("Treetop settlement", new DungeonPlacementContext { Cell = forest, NearestSettlementDistance = 8 }),
                "Treetop settlements require forest-like terrain");
            AssertFalse(DungeonCatalog.IsDungeonTypeAllowed("Cliff city", new DungeonPlacementContext { Cell = plains, NearestSettlementDistance = 8 }),
                "Cliff cities are rejected on plains");

            AssertFalse(DungeonCatalog.IsFeatureAllowed("Volcano", mountain, false, false), "Volcanoes require seismic regions");
            AssertTrue(DungeonCatalog.IsFeatureAllowed("Volcano", mountain, false, true), "Volcanoes can appear in seismic mountains");
            AssertFalse(DungeonCatalog.IsFeatureAllowed("Waterfall", mountain, false, true), "Waterfalls require a river");
            AssertTrue(DungeonCatalog.IsFeatureAllowed("Waterfall", mountain, true, true), "Waterfalls can appear on rivers with elevation drop");
            AssertFalse(DungeonCatalog.IsFeatureAllowed("Quicksand", plains, false, true), "Quicksand is not allowed outside deserts");

            NameGenerationService names = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            RegionMapGenerator generator = new RegionMapGenerator(names);
            RegionGenerationOptions options = CreateRegionOptions("hex-features");
            options.Width = 30;
            options.Height = 30;
            options.GenerateHexFeatures = true;
            options.GenerateDungeons = true;
            options.Seismicity = "Seismic";

            HexMapRecord map = generator.Generate(options).Map;
            AssertTrue(map.Features.Count > 0, "Region generation creates hex features");
            AssertTrue(map.Dungeons.Count > 0, "Region generation creates map dungeons when enabled");
            AssertTrue(map.Features.Where(f => f.Kind == "Dungeon").All(f => !string.IsNullOrWhiteSpace(f.DungeonId)),
                "Dungeon features link to dungeon records");
            AssertTrue(map.Features.Where(f => f.Kind == "Dungeon").All(f => f.DungeonLevel >= DungeonCatalog.MinDungeonLevel && f.DungeonLevel <= DungeonCatalog.MaxDungeonLevel),
                "Map dungeon feature danger levels stay within ACKS dungeon level range");
            AssertTrue(map.Dungeons.All(d => d.RecommendedLevel >= DungeonCatalog.MinDungeonLevel && d.RecommendedLevel <= DungeonCatalog.MaxDungeonLevel),
                "Generated map dungeon records stay within ACKS dungeon level range");

            HexMapRecord domainMap = CreateFlatMap(30, 24);
            foreach (HexCellRecord cell in domainMap.Cells)
            {
                cell.Elevation = "Hills";
            }

            MapSettlementRecord settlement = new MapSettlementRecord
            {
                Id = "settlement-feature-test",
                Name = "Feature Test",
                Q = 14,
                R = 10,
                MarketClass = 3,
                Race = "Human"
            };
            DomainRecord domain = new DomainRecord
            {
                Id = "domain-feature-test",
                Name = "Feature Domain",
                CapitalSettlementId = settlement.Id,
                Classification = "Civilized",
                DomainType = "Ordinary",
                Race = "Human"
            };
            domain.SettlementIds.Add(settlement.Id);
            for (int r = 6; r <= 15; r++)
            {
                for (int q = 10; q <= 19; q++)
                {
                    domain.Hexes.Add(new DomainHexRecord { Q = q, R = r, LandValueGp = 6 });
                }
            }

            domainMap.Settlements.Add(settlement);
            domainMap.Domains.Add(domain);
            RegionGenerationOptions featureOptions = CreateRegionOptions("hex-features-in-domains");
            featureOptions.GenerateDungeons = false;
            featureOptions.Seismicity = "Normal";
            System.Reflection.MethodInfo featureMethod = typeof(RegionMapGenerator).GetMethod(
                "GenerateHexFeatures",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.FieldInfo cellIndexField = typeof(RegionMapGenerator).GetField(
                "cellIndex",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            cellIndexField.SetValue(generator, domainMap.Cells.ToDictionary(c => TestCellKey(c.Q, c.R)));
            featureMethod.Invoke(generator, new object[] { domainMap, featureOptions, new Random(19) });
            HashSet<string> featureDomainCells = new HashSet<string>(domain.Hexes.Select(h => TestCellKey(h.Q, h.R)));
            AssertTrue(
                domainMap.Features.Any(f => featureDomainCells.Contains(TestCellKey(f.Q, f.R))),
                "Natural hex features can appear inside domains and settlements");
        }

        private static void TestXmlRecordStore(string tempRoot)
        {
            string path = Path.Combine(tempRoot, "characters.xml");
            XmlRecordStore<CharacterRecord> store = new XmlRecordStore<CharacterRecord>(path);
            store.Save(new[]
            {
                new CharacterRecord { Name = "Ariadne", Kind = "NPC", ClassName = "Mage", Level = 3 }
            });

            List<CharacterRecord> records = store.Load();

            AssertEqual(1, records.Count, "XML store round-trips record count");
            AssertEqual("Ariadne", records[0].Name, "XML store round-trips record data");
        }

        private static void TestTradeDemandWorkbookService(string tempRoot)
        {
            string path = Path.Combine(tempRoot, "demands.xlsx");
            string[] headers = Enumerable.Range(1, AcksRules.DemandCount).Select(i => "Item " + i).ToArray();
            double[] demands = new double[AcksRules.DemandCount];
            demands[0] = -1.5;
            demands[1] = 2.25;
            demands[28] = 4;

            TradeDemandWorkbookService service = new TradeDemandWorkbookService(headers);
            service.SaveDemands(path, demands, headers);
            double[] loaded = service.LoadDemands(path, AcksRules.DemandCount);

            AssertEqual(AcksRules.DemandCount, loaded.Length, "Demand workbook has canonical size");
            AssertNear(-1.5, loaded[0], "Demand workbook preserves first value");
            AssertNear(2.25, loaded[1], "Demand workbook preserves decimal value");
            AssertNear(4, loaded[28], "Demand workbook preserves last value");
        }

        private static void TestMapWorkbookService(string tempRoot)
        {
            string path = Path.Combine(tempRoot, "map.xlsx");
            string settlementId = "settlement-1";
            string realmId = "realm-1";
            DungeonRecord dungeon = new DungeonGenerator().Generate(new DungeonGenerationOptions
            {
                Seed = "777",
                Name = "Mirror Catacombs",
                DungeonType = "Catacombs",
                Size = "Small",
                RecommendedLevel = 2
            });
            dungeon.Id = "dungeon-1";
            DungeonLevelRecord workbookLevel = dungeon.Levels.First();
            workbookLevel.Rooms[0].Shape = "Circle";
            if (workbookLevel.Connections.Count == 0 && workbookLevel.Rooms.Count >= 2)
            {
                workbookLevel.Connections.Add(new DungeonConnectionRecord
                {
                    FromRoomId = workbookLevel.Rooms[0].Id,
                    ToRoomId = workbookLevel.Rooms[1].Id
                });
            }
            workbookLevel.Connections[0].Kind = "Passage";
            workbookLevel.Connections[0].PassageWidth = 3;
            workbookLevel.Connections[0].DoorKind = "";
            workbookLevel.Connections[0].PathPoints.Clear();
            workbookLevel.Connections[0].PathPoints.Add(new DungeonPathPointRecord { X = 6.5, Y = 4 });
            workbookLevel.Connections[0].PathPoints.Add(new DungeonPathPointRecord { X = 7.5, Y = 6 });
            workbookLevel.Doors.Clear();
            workbookLevel.Doors.Add(new DungeonDoorRecord
            {
                Id = "door-1",
                LevelNumber = workbookLevel.LevelNumber,
                X = 4.5,
                Y = 5.25,
                Kind = "SecretPassage",
                Orientation = "Horizontal",
                FromRoomId = workbookLevel.Connections[0].FromRoomId,
                ToRoomId = workbookLevel.Connections[0].ToRoomId,
                Notes = "Test hidden passage"
            });
            HexMapRecord map = new HexMapRecord
            {
                Name = "Test March",
                Width = 2,
                Height = 1,
                Cells = new List<HexCellRecord>
                {
                    new HexCellRecord { Q = 0, R = 0, Terrain = "Grasslands", Elevation = "Plains", Water = "None" },
                    new HexCellRecord { Q = 1, R = 0, Terrain = "Forest", Elevation = "Hills", Water = "Lake", WaterFeatureName = "Mirror Lake" }
                },
                Settlements = new List<MapSettlementRecord>
                {
                    new MapSettlementRecord
                    {
                        Id = settlementId,
                        Name = "Northford",
                        MarketClass = 4,
                        Q = 0,
                        R = 0,
                        Race = "Human",
                        BaseDemands = Enumerable.Repeat(1.0, AcksRules.DemandCount).ToArray(),
                        CurrentDemands = Enumerable.Repeat(2.0, AcksRules.DemandCount).ToArray()
                    }
                },
                Roads = new List<MapEdgeRecord>
                {
                    new MapEdgeRecord { AQ = 0, AR = 0, BQ = 1, BR = 0, Kind = "Road", FeatureName = "Old Road" }
                },
                Rivers = new List<MapEdgeRecord>
                {
                    new MapEdgeRecord { AQ = 0, AR = 0, BQ = 1, BR = 0, Kind = "River", FeatureName = "Bluewater" }
                },
                Domains = new List<DomainRecord>
                {
                    new DomainRecord
                    {
                        Id = "domain-1",
                        Name = "Northford Domain",
                        RealmId = realmId,
                        CapitalSettlementId = settlementId,
                        Race = "Human",
                        Hexes = new List<DomainHexRecord>
                        {
                            new DomainHexRecord { Q = 0, R = 0, LandValueGp = 6 }
                        }
                    }
                },
                Realms = new List<RealmRecord>
                {
                    new RealmRecord
                    {
                        Id = realmId,
                        Name = "Northford County",
                        Tier = "County",
                        TitleOverride = "Warden",
                        FemaleTitleOverride = "Wardena",
                        CapitalSettlementId = settlementId,
                        RulerName = "Ariadne",
                        RulerLevel = 5
                    }
                },
                VassalLinks = new List<VassalLinkRecord>(),
                Features = new List<HexFeatureRecord>
                {
                    new HexFeatureRecord
                    {
                        Id = "feature-1",
                        Name = "Mirror Catacombs",
                        Kind = "Dungeon",
                        Subtype = "Catacombs",
                        Q = 1,
                        R = 0,
                        IconKey = "dungeon_catacombs",
                        Description = "A test dungeon feature",
                        DungeonId = dungeon.Id,
                        DungeonType = dungeon.DungeonType,
                        DungeonLevel = dungeon.RecommendedLevel,
                        DungeonSize = dungeon.Size
                    }
                },
                Dungeons = new List<DungeonRecord> { dungeon }
            };

            MapWorkbookService service = new MapWorkbookService(Enumerable.Range(1, AcksRules.DemandCount).Select(i => "Item " + i));
            service.SaveMap(path, map, 6);
            HexMapRecord loaded = service.LoadMap(path);

            AssertEqual("Test March", loaded.Name, "Map workbook preserves map name");
            AssertEqual(2, loaded.Cells.Count, "Map workbook preserves cells");
            AssertEqual(1, loaded.Settlements.Count, "Map workbook preserves settlements");
            AssertEqual(1, loaded.Roads.Count, "Map workbook preserves roads");
            AssertEqual(1, loaded.Rivers.Count, "Map workbook preserves rivers");
            AssertEqual(1, loaded.Domains.Count, "Map workbook preserves domains");
            AssertEqual(1, loaded.Realms.Count, "Map workbook preserves realms");
            AssertEqual(1, loaded.Features.Count, "Map workbook preserves hex features");
            AssertEqual(1, loaded.Dungeons.Count, "Map workbook preserves dungeons");
            AssertEqual("Warden", loaded.Realms[0].TitleOverride, "Map workbook preserves custom realm title");
            AssertEqual("Wardena", loaded.Realms[0].FemaleTitleOverride, "Map workbook preserves custom female realm title");
            AssertEqual("Mirror Lake", loaded.Cells[1].WaterFeatureName, "Map workbook preserves feature names");
            AssertEqual(AcksRules.DemandCount, loaded.Settlements[0].BaseDemands.Length, "Map workbook preserves demand array shape");
            AssertEqual("dungeon-1", loaded.Features[0].DungeonId, "Map workbook preserves feature dungeon link");
            AssertTrue(loaded.Dungeons[0].Levels.Sum(l => l.Rooms.Count) > 0, "Map workbook preserves dungeon rooms");
            AssertTrue(loaded.Dungeons[0].WanderingEncounters.Count > 0, "Map workbook preserves dungeon encounters");
            AssertEqual("Circle", loaded.Dungeons[0].Levels[0].Rooms[0].Shape, "Map workbook preserves dungeon room shape");
            AssertEqual("Passage", loaded.Dungeons[0].Levels[0].Connections[0].Kind, "Map workbook preserves dungeon passage kind");
            AssertEqual(3, loaded.Dungeons[0].Levels[0].Connections[0].PassageWidth, "Map workbook preserves dungeon passage width");
            AssertEqual("", loaded.Dungeons[0].Levels[0].Connections[0].DoorKind, "Map workbook keeps passages free of door metadata");
            AssertEqual(2, loaded.Dungeons[0].Levels[0].Connections[0].PathPoints.Count, "Map workbook preserves manual dungeon corridor path points");
            AssertNear(6.5, loaded.Dungeons[0].Levels[0].Connections[0].PathPoints[0].X, "Map workbook preserves first manual corridor X");
            AssertEqual(1, loaded.Dungeons[0].Levels[0].Doors.Count, "Map workbook preserves separate dungeon doors");
            AssertEqual("SecretPassage", loaded.Dungeons[0].Levels[0].Doors[0].Kind, "Map workbook preserves separate dungeon door kind");
            AssertEqual("Horizontal", loaded.Dungeons[0].Levels[0].Doors[0].Orientation, "Map workbook preserves separate dungeon door orientation");
            AssertTrue(loaded.Dungeons[0].WanderingEncounters.All(e => e.MonsterLevel >= 1 && e.MonsterLevel <= 6),
                "Map workbook preserves dungeon encounter monster levels");
        }

        private static bool DungeonRoomsOverlap(DungeonRoomRecord a, DungeonRoomRecord b)
        {
            if (a == null || b == null) return false;
            return a.X < b.X + b.Width
                && a.X + a.Width > b.X
                && a.Y < b.Y + b.Height
                && a.Y + a.Height > b.Y;
        }

        private static bool DungeonTreasureTypeIsNone(string treasureType)
        {
            return string.IsNullOrWhiteSpace(treasureType)
                || string.Equals(treasureType.Trim(), "none", StringComparison.OrdinalIgnoreCase);
        }

        private static string DungeonPlanSignature(DungeonRecord dungeon)
        {
            if (dungeon == null || dungeon.Levels == null) return "";
            return string.Join("|L|", dungeon.Levels
                .Where(level => level != null)
                .OrderBy(level => level.LevelNumber)
                .Select(DungeonLevelSignature)
                .ToArray());
        }

        private static string DungeonLevelSignature(DungeonLevelRecord level)
        {
            List<DungeonRoomRecord> rooms = level.Rooms ?? new List<DungeonRoomRecord>();
            Dictionary<string, DungeonRoomRecord> roomsById = rooms
                .Where(room => room != null && !string.IsNullOrWhiteSpace(room.Id))
                .ToDictionary(room => room.Id, room => room);
            string roomText = string.Join(";", rooms
                .Where(room => room != null)
                .OrderBy(room => room.Y)
                .ThenBy(room => room.X)
                .Select(DungeonRoomSignature)
                .ToArray());
            string connectionText = string.Join(";", (level.Connections ?? new List<DungeonConnectionRecord>())
                .Where(connection => connection != null)
                .Select(connection => DungeonConnectionSignature(connection, roomsById))
                .OrderBy(text => text)
                .ToArray());
            string doorText = string.Join(";", (level.Doors ?? new List<DungeonDoorRecord>())
                .Where(door => door != null)
                .OrderBy(door => door.Y)
                .ThenBy(door => door.X)
                .Select(door => door.Kind + "@" + DungeonNumber(door.X) + "," + DungeonNumber(door.Y) + "/" + door.Orientation)
                .ToArray());
            return level.LevelNumber + ":" + roomText + ":" + connectionText + ":" + doorText;
        }

        private static string DungeonConnectionSignature(DungeonConnectionRecord connection, Dictionary<string, DungeonRoomRecord> roomsById)
        {
            DungeonRoomRecord from;
            DungeonRoomRecord to;
            string fromText = roomsById.TryGetValue(connection.FromRoomId ?? "", out from) ? DungeonRoomSignature(from) : "";
            string toText = roomsById.TryGetValue(connection.ToRoomId ?? "", out to) ? DungeonRoomSignature(to) : "";
            if (string.CompareOrdinal(fromText, toText) > 0)
            {
                string swap = fromText;
                fromText = toText;
                toText = swap;
            }

            string pathText = string.Join(",", (connection.PathPoints ?? new List<DungeonPathPointRecord>())
                .Select(point => DungeonNumber(point.X) + "/" + DungeonNumber(point.Y))
                .ToArray());
            return fromText + ">" + toText + "/" + connection.Kind + "/" + connection.PassageWidth + "/" + pathText;
        }

        private static string DungeonConnectionDebugText(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null) return "connection=<null>";
            DungeonRoomRecord from = (level.Rooms ?? new List<DungeonRoomRecord>())
                .FirstOrDefault(room => room != null && room.Id == connection.FromRoomId);
            DungeonRoomRecord to = (level.Rooms ?? new List<DungeonRoomRecord>())
                .FirstOrDefault(room => room != null && room.Id == connection.ToRoomId);
            string pathText = string.Join(" -> ", (connection.PathPoints ?? new List<DungeonPathPointRecord>())
                .Select(point => "(" + DungeonNumber(point.X) + "," + DungeonNumber(point.Y) + ")")
                .ToArray());
            return "from=" + DungeonRoomDebugText(from)
                + ", to=" + DungeonRoomDebugText(to)
                + ", width=" + connection.PassageWidth
                + ", path=" + pathText;
        }

        private static string DungeonRoomDebugText(DungeonRoomRecord room)
        {
            if (room == null) return "<null>";
            return room.Title + "[" + room.Shape + "]@"
                + room.X + "," + room.Y + " "
                + room.Width + "x" + room.Height;
        }

        private static string DungeonRoomSignature(DungeonRoomRecord room)
        {
            if (room == null) return "";
            return room.X + "," + room.Y + "," + room.Width + "," + room.Height + "," + room.Shape + "," + room.Kind;
        }

        private static string DungeonNumber(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool DungeonLevelRoomsAreConnected(DungeonLevelRecord level)
        {
            if (level == null || level.Rooms == null || level.Rooms.Count <= 1) return true;
            HashSet<string> roomIds = new HashSet<string>(level.Rooms.Where(r => r != null).Select(r => r.Id));
            Dictionary<string, List<string>> graph = roomIds.ToDictionary(id => id, id => new List<string>());
            foreach (DungeonConnectionRecord connection in level.Connections ?? new List<DungeonConnectionRecord>())
            {
                if (connection == null) continue;
                if (!roomIds.Contains(connection.FromRoomId) || !roomIds.Contains(connection.ToRoomId)) continue;
                graph[connection.FromRoomId].Add(connection.ToRoomId);
                graph[connection.ToRoomId].Add(connection.FromRoomId);
            }

            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();
            string start = roomIds.First();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (string next in graph[current])
                {
                    if (!visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }

            return visited.Count == roomIds.Count;
        }

        private static bool DungeonConnectionAvoidsUnlinkedRoomInterior(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count == 0) return true;

            List<DungeonPathPointRecord> points = new List<DungeonPathPointRecord>();
            points.Add(DungeonRoomEdgePoint(from, pathPoints[0]));
            points.AddRange(pathPoints);
            points.Add(DungeonRoomEdgePoint(to, pathPoints[pathPoints.Count - 1]));

            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == from.Id || room.Id == to.Id) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonSegmentCrossesRoomInterior(points[i - 1], points[i], room)) return false;
                }
            }

            return true;
        }

        private static bool DungeonConnectionAvoidsUnlinkedRoomBuffer(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;
            List<DungeonPathPointRecord> points = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (points.Count == 0) return true;

            double margin = DungeonGeometry.PassageHalfWidthCells(connection.PassageWidth);
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == from.Id || room.Id == to.Id) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(points[i - 1], points[i], room, margin)) return false;
                }
            }

            return true;
        }

        private static bool DungeonConnectionEndpointsStayOnLinkedRoomBoundary(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count == 0) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;
            return DungeonPointIsOnRoomBoundary(pathPoints[0], from)
                && DungeonPointIsOnRoomBoundary(pathPoints[pathPoints.Count - 1], to);
        }

        private static bool DungeonConnectionAvoidsLinkedRoomInterior(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count == 0) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            foreach (DungeonPathPointRecord point in pathPoints)
            {
                if (from != null && DungeonGeometry.IsPointInsideRoomInterior(from, point.X, point.Y, 0.01)) return false;
                if (to != null && DungeonGeometry.IsPointInsideRoomInterior(to, point.X, point.Y, 0.01)) return false;
            }

            for (int i = 1; i < pathPoints.Count; i++)
            {
                if (from != null && DungeonGeometry.SegmentCrossesRoomInterior(pathPoints[i - 1], pathPoints[i], from)) return false;
                if (to != null && DungeonGeometry.SegmentCrossesRoomInterior(pathPoints[i - 1], pathPoints[i], to)) return false;
                if (from != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(pathPoints[i - 1], pathPoints[i], from, 0.08)) return false;
                if (to != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(pathPoints[i - 1], pathPoints[i], to, 0.08)) return false;
            }

            return true;
        }

        private static bool DungeonConnectionAvoidsLinkedRoomBuffer(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count < 2) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            double margin = DungeonGeometry.PassageHalfWidthCells(connection.PassageWidth);
            for (int i = 1; i < pathPoints.Count; i++)
            {
                bool entranceFrom = i == 1;
                bool entranceTo = i == pathPoints.Count - 1;
                if (!entranceFrom && from != null && DungeonGeometry.SegmentCrossesRoomBuffer(pathPoints[i - 1], pathPoints[i], from, margin)) return false;
                if (!entranceTo && to != null && DungeonGeometry.SegmentCrossesRoomBuffer(pathPoints[i - 1], pathPoints[i], to, margin)) return false;
            }

            return true;
        }

        private static bool DungeonConnectionEntrancesAreFullWidth(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count == 0) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;
            return DungeonRoomEntranceIsFullWidth(from, pathPoints[0], pathPoints.Count > 1 ? pathPoints[1] : null, connection.PassageWidth)
                && DungeonRoomEntranceIsFullWidth(to, pathPoints[pathPoints.Count - 1], pathPoints.Count > 1 ? pathPoints[pathPoints.Count - 2] : null, connection.PassageWidth);
        }

        private static bool DungeonRoomEntranceIsFullWidth(DungeonRoomRecord room, DungeonPathPointRecord edge, DungeonPathPointRecord outside, int passageWidth)
        {
            if (room == null || edge == null || outside == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(room))
            {
                return !DungeonGeometry.IsPointInsideRoomInterior(room, outside.X, outside.Y, 0.02);
            }

            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, edge.X, edge.Y, passageWidth, out x, out y, out orientation);
            const double entranceTolerance = 0.12;
            if (Math.Abs(edge.X - x) > entranceTolerance || Math.Abs(edge.Y - y) > entranceTolerance) return false;

            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase))
            {
                if (Math.Abs(edge.X - outside.X) > entranceTolerance) return false;
                return edge.Y <= RoomCenterY(room)
                    ? outside.Y <= edge.Y + 0.02
                    : outside.Y >= edge.Y - 0.02;
            }

            if (Math.Abs(edge.Y - outside.Y) > entranceTolerance) return false;
            return edge.X <= RoomCenterX(room)
                ? outside.X <= edge.X + 0.02
                : outside.X >= edge.X - 0.02;
        }

        private static bool DungeonConnectionUsesTwoPointPathWhenDirectRouteIsClear(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count <= 2) return true;

            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;

            DungeonPathPointRecord start = DungeonRoomEdgePoint(from, new DungeonPathPointRecord { X = RoomCenterX(to), Y = RoomCenterY(to) }, connection.PassageWidth);
            DungeonPathPointRecord end = DungeonRoomEdgePoint(to, new DungeonPathPointRecord { X = RoomCenterX(from), Y = RoomCenterY(from) }, connection.PassageWidth);
            if (!DungeonPathPointsAreNearlyAligned(start, end)) return true;

            DungeonConnectionRecord direct = new DungeonConnectionRecord
            {
                FromRoomId = connection.FromRoomId,
                ToRoomId = connection.ToRoomId,
                PassageWidth = connection.PassageWidth,
                PathPoints = new List<DungeonPathPointRecord> { start, end }
            };
            bool directIsClear = DungeonConnectionAvoidsUnlinkedRoomInterior(level, direct)
                && DungeonConnectionAvoidsUnlinkedRoomBuffer(level, direct)
                && DungeonConnectionEntrancesAreFullWidth(level, direct)
                && DungeonConnectionAvoidsLinkedRoomInterior(level, direct);
            return !directIsClear;
        }

        private static bool DungeonConnectionUsesDirectBoundaryPathWhenClear(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return true;
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            if (pathPoints.Count == 0) return true;

            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return true;

            DungeonPathPointRecord start = DungeonRoomEdgePoint(from, new DungeonPathPointRecord { X = RoomCenterX(to), Y = RoomCenterY(to) }, connection.PassageWidth);
            DungeonPathPointRecord end = DungeonRoomEdgePoint(to, new DungeonPathPointRecord { X = RoomCenterX(from), Y = RoomCenterY(from) }, connection.PassageWidth);
            if (DungeonGeometry.UsesBoxEdges(from)
                && DungeonGeometry.UsesBoxEdges(to)
                && !NearlyEqual(start.X, end.X)
                && !NearlyEqual(start.Y, end.Y))
            {
                return true;
            }

            DungeonConnectionRecord direct = new DungeonConnectionRecord
            {
                FromRoomId = connection.FromRoomId,
                ToRoomId = connection.ToRoomId,
                PassageWidth = connection.PassageWidth,
                PathPoints = new List<DungeonPathPointRecord> { start, end }
            };
            bool directIsClear = DungeonConnectionAvoidsUnlinkedRoomInterior(level, direct)
                && DungeonConnectionAvoidsUnlinkedRoomBuffer(level, direct)
                && DungeonConnectionEntrancesAreFullWidth(level, direct)
                && DungeonConnectionAvoidsLinkedRoomInterior(level, direct)
                && !DungeonConnectionRunsTooCloseToExisting(level, direct, connection);
            if (!directIsClear) return true;

            return pathPoints.Count == 2
                && DungeonConnectionEndpointsStayOnLinkedRoomBoundary(level, connection)
                && DungeonConnectionEntrancesAreFullWidth(level, connection)
                && DungeonConnectionAvoidsLinkedRoomInterior(level, connection);
        }

        private static bool DungeonConnectionRunsTooCloseToExisting(DungeonLevelRecord level, DungeonConnectionRecord candidate, DungeonConnectionRecord ignore)
        {
            if (level == null || candidate == null || candidate.PathPoints == null || candidate.PathPoints.Count < 2) return false;
            foreach (DungeonConnectionRecord existing in level.Connections ?? new List<DungeonConnectionRecord>())
            {
                if (existing == null || ReferenceEquals(existing, ignore)) continue;
                if (existing.PathPoints == null || existing.PathPoints.Count < 2) continue;
                for (int i = 1; i < candidate.PathPoints.Count; i++)
                {
                    for (int j = 1; j < existing.PathPoints.Count; j++)
                    {
                        if (DungeonSegmentsRunTooCloseParallel(candidate.PathPoints[i - 1], candidate.PathPoints[i], existing.PathPoints[j - 1], existing.PathPoints[j])) return true;
                    }
                }
            }

            return false;
        }

        private static bool DungeonConnectionsAvoidCloseParallelRuns(DungeonLevelRecord level)
        {
            return string.IsNullOrEmpty(DungeonFirstCloseParallelRunDebugText(level));
        }

        private static string DungeonFirstCloseParallelRunDebugText(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null) return "";
            for (int i = 0; i < level.Connections.Count; i++)
            {
                DungeonConnectionRecord a = level.Connections[i];
                if (a == null || a.PathPoints == null || a.PathPoints.Count < 2) continue;
                for (int j = i + 1; j < level.Connections.Count; j++)
                {
                    DungeonConnectionRecord b = level.Connections[j];
                    if (b == null || b.PathPoints == null || b.PathPoints.Count < 2) continue;
                    for (int ai = 1; ai < a.PathPoints.Count; ai++)
                    {
                        for (int bi = 1; bi < b.PathPoints.Count; bi++)
                        {
                            if (DungeonSegmentsRunTooCloseParallel(a.PathPoints[ai - 1], a.PathPoints[ai], b.PathPoints[bi - 1], b.PathPoints[bi]))
                            {
                                return "a={" + DungeonConnectionDebugText(level, a)
                                    + "}, b={" + DungeonConnectionDebugText(level, b)
                                    + "}, segments=("
                                    + DungeonNumber(a.PathPoints[ai - 1].X) + "," + DungeonNumber(a.PathPoints[ai - 1].Y)
                                    + ")->(" + DungeonNumber(a.PathPoints[ai].X) + "," + DungeonNumber(a.PathPoints[ai].Y)
                                    + ") vs ("
                                    + DungeonNumber(b.PathPoints[bi - 1].X) + "," + DungeonNumber(b.PathPoints[bi - 1].Y)
                                    + ")->(" + DungeonNumber(b.PathPoints[bi].X) + "," + DungeonNumber(b.PathPoints[bi].Y)
                                    + ")";
                            }
                        }
                    }
                }
            }

            return "";
        }

        private static bool DungeonConnectionsAvoidTinySteps(DungeonLevelRecord level)
        {
            return DungeonFirstTinyStepConnection(level) == null;
        }

        private static DungeonConnectionRecord DungeonFirstTinyStepConnection(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null) return null;
            foreach (DungeonConnectionRecord connection in level.Connections)
            {
                List<DungeonPathPointRecord> points = connection == null
                    ? new List<DungeonPathPointRecord>()
                    : connection.PathPoints ?? new List<DungeonPathPointRecord>();
                if (DungeonGeometry.PathHasTinyStep(points, 1.05)) return connection;
            }

            return null;
        }

        private static bool DungeonConnectionsAvoidSelfParallelRuns(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null) return true;
            foreach (DungeonConnectionRecord connection in level.Connections)
            {
                List<DungeonPathPointRecord> points = connection == null
                    ? new List<DungeonPathPointRecord>()
                    : connection.PathPoints ?? new List<DungeonPathPointRecord>();
                if (DungeonGeometry.PathHasTightSelfParallelRun(points, 1.05, 0.75)) return false;
            }

            return true;
        }

        private static bool DungeonBoxRoomConnectionsAreOrthogonal(DungeonLevelRecord level)
        {
            return DungeonFirstNonOrthogonalBoxConnection(level) == null;
        }

        private static DungeonConnectionRecord DungeonFirstNonOrthogonalBoxConnection(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null || level.Rooms == null) return null;
            foreach (DungeonConnectionRecord connection in level.Connections)
            {
                DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => connection != null && r.Id == connection.FromRoomId);
                DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => connection != null && r.Id == connection.ToRoomId);
                if (from == null || to == null) continue;
                if (!DungeonGeometry.UsesBoxEdges(from) || !DungeonGeometry.UsesBoxEdges(to)) continue;

                List<DungeonPathPointRecord> points = connection == null
                    ? new List<DungeonPathPointRecord>()
                    : connection.PathPoints ?? new List<DungeonPathPointRecord>();
                for (int i = 1; i < points.Count; i++)
                {
                    if (!NearlyEqual(points[i - 1].X, points[i].X)
                        && !NearlyEqual(points[i - 1].Y, points[i].Y))
                    {
                        return connection;
                    }
                }
            }

            return null;
        }

        private static bool DungeonSegmentsRunTooCloseParallel(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonPathPointRecord c, DungeonPathPointRecord d)
        {
            if (a == null || b == null || c == null || d == null) return false;
            const double minimumSeparation = 1.05;
            const double minimumSharedRun = 1.25;
            bool firstVertical = NearlyEqual(a.X, b.X);
            bool firstHorizontal = NearlyEqual(a.Y, b.Y);
            bool secondVertical = NearlyEqual(c.X, d.X);
            bool secondHorizontal = NearlyEqual(c.Y, d.Y);
            if (firstVertical && secondVertical)
            {
                if (NearlyEqual(a.X, c.X)) return false;
                if (Math.Abs(a.X - c.X) > minimumSeparation) return false;
                return DungeonRangeOverlapLength(a.Y, b.Y, c.Y, d.Y) >= minimumSharedRun;
            }

            if (firstHorizontal && secondHorizontal)
            {
                if (NearlyEqual(a.Y, c.Y)) return false;
                if (Math.Abs(a.Y - c.Y) > minimumSeparation) return false;
                return DungeonRangeOverlapLength(a.X, b.X, c.X, d.X) >= minimumSharedRun;
            }

            return false;
        }

        private static double DungeonRangeOverlapLength(double a, double b, double c, double d)
        {
            double minA = Math.Min(a, b);
            double maxA = Math.Max(a, b);
            double minB = Math.Min(c, d);
            double maxB = Math.Max(c, d);
            return Math.Min(maxA, maxB) - Math.Max(minA, minB);
        }

        private static bool NearlyEqual(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        private static bool DungeonPathPointsAreNearlyAligned(DungeonPathPointRecord a, DungeonPathPointRecord b)
        {
            if (a == null || b == null) return false;
            return Math.Abs(a.X - b.X) <= 0.45 || Math.Abs(a.Y - b.Y) <= 0.45;
        }

        private static double RoomCenterX(DungeonRoomRecord room)
        {
            return room == null ? 0 : room.X + room.Width / 2.0;
        }

        private static double RoomCenterY(DungeonRoomRecord room)
        {
            return room == null ? 0 : room.Y + room.Height / 2.0;
        }

        private static DungeonPathPointRecord DungeonRoomEdgePoint(DungeonRoomRecord room, DungeonPathPointRecord target)
        {
            return DungeonRoomEdgePoint(room, target, 1);
        }

        private static DungeonPathPointRecord DungeonRoomEdgePoint(DungeonRoomRecord room, DungeonPathPointRecord target, int passageWidth)
        {
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, target.X, target.Y, passageWidth, out x, out y, out orientation);
            return new DungeonPathPointRecord
            {
                X = x,
                Y = y
            };
        }

        private static bool DungeonSegmentCrossesRoomInterior(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonRoomRecord room)
        {
            return DungeonGeometry.SegmentCrossesRoomInterior(a, b, room);
        }

        private static bool DungeonDoorIsOnLinkedRoomBoundary(DungeonLevelRecord level, DungeonDoorRecord door)
        {
            if (level == null || door == null || string.IsNullOrWhiteSpace(door.ToRoomId)) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == door.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == door.ToRoomId);
            if (from == null || to == null) return true;
            return DungeonDoorIsOnRoomBoundary(door, from) || DungeonDoorIsOnRoomBoundary(door, to);
        }

        private static bool DungeonDoorAvoidsLinkedRoomInterior(DungeonLevelRecord level, DungeonDoorRecord door)
        {
            if (level == null || door == null || string.IsNullOrWhiteSpace(door.ToRoomId)) return true;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == door.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == door.ToRoomId);
            return (from == null || !DungeonGeometry.IsPointInsideRoomInterior(from, door.X, door.Y, 0.01))
                && (to == null || !DungeonGeometry.IsPointInsideRoomInterior(to, door.X, door.Y, 0.01));
        }

        private static bool DungeonDoorIsOnRoomBoundary(DungeonDoorRecord door, DungeonRoomRecord room)
        {
            return DungeonPointIsOnRoomBoundary(new DungeonPathPointRecord { X = door.X, Y = door.Y }, room);
        }

        private static bool DungeonPointIsOnRoomBoundary(DungeonPathPointRecord point, DungeonRoomRecord room)
        {
            if (point == null || room == null) return false;
            double edgeX;
            double edgeY;
            string orientation;
            DungeonGeometry.FindRoomEdgePoint(room, point.X, point.Y, out edgeX, out edgeY, out orientation);
            double dx = point.X - edgeX;
            double dy = point.Y - edgeY;
            return Math.Sqrt(dx * dx + dy * dy) <= 0.05;
        }

        private static void AssertTrue(bool condition, string message)
        {
            assertionCount++;
            if (!condition) throw new InvalidOperationException("Assertion failed: " + message);
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            assertionCount++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Assertion failed: " + message + ". Expected <" + expected + ">, actual <" + actual + ">.");
            }
        }

        private static void AssertNear(double expected, double actual, string message)
        {
            assertionCount++;
            if (Math.Abs(expected - actual) > 0.000001)
            {
                throw new InvalidOperationException(
                    "Assertion failed: " + message + ". Expected <" + expected + ">, actual <" + actual + ">.");
            }
        }
    }
}
