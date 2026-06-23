using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace OSRCGG
{
    internal sealed class MapWorkbookService
    {
        private readonly string[] merchandiseNames;

        public MapWorkbookService(IEnumerable<string> merchandiseNames)
        {
            this.merchandiseNames = (merchandiseNames ?? Enumerable.Empty<string>()).ToArray();
        }

        public void SaveSettlement(string fileName, MapSettlementRecord record)
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet sheet = workbook.Worksheets.Add("Settlement");
                WriteSettlementRow(sheet, 1, record, true);
                WriteSettlementRow(sheet, 2, record, false);
                sheet.ColumnsUsed().AdjustToContents();
                workbook.SaveAs(fileName);
            }
        }

        public MapSettlementRecord LoadSettlement(string fileName)
        {
            using (XLWorkbook workbook = new XLWorkbook(fileName))
            {
                IXLWorksheet sheet = ExcelWorksheetShim.FindWorksheet(workbook, "Settlement") ?? workbook.Worksheets.FirstOrDefault();
                if (sheet == null) throw new InvalidOperationException("Workbook has no worksheets.");
                return ReadSettlementRow(sheet, 2);
            }
        }

        public void SaveMap(string fileName, HexMapRecord map, int scaleMiles)
        {
            if (map == null) return;

            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet mapSheet = workbook.Worksheets.Add("Map");
                IXLWorksheet cellsSheet = workbook.Worksheets.Add("Cells");
                IXLWorksheet settlementsSheet = workbook.Worksheets.Add("Settlements");
                IXLWorksheet roadsSheet = workbook.Worksheets.Add("Roads");
                IXLWorksheet riversSheet = workbook.Worksheets.Add("Rivers");
                IXLWorksheet domainsSheet = workbook.Worksheets.Add("Domains");
                IXLWorksheet domainHexesSheet = workbook.Worksheets.Add("DomainHexes");
                IXLWorksheet realmsSheet = workbook.Worksheets.Add("Realms");
                IXLWorksheet vassalLinksSheet = workbook.Worksheets.Add("VassalLinks");
                IXLWorksheet featuresSheet = workbook.Worksheets.Add("HexFeatures");
                IXLWorksheet dungeonsSheet = workbook.Worksheets.Add("Dungeons");
                IXLWorksheet dungeonLevelsSheet = workbook.Worksheets.Add("DungeonLevels");
                IXLWorksheet dungeonRoomsSheet = workbook.Worksheets.Add("DungeonRooms");
                IXLWorksheet dungeonConnectionsSheet = workbook.Worksheets.Add("DungeonConnections");
                IXLWorksheet dungeonDoorsSheet = workbook.Worksheets.Add("DungeonDoors");
                IXLWorksheet dungeonEncountersSheet = workbook.Worksheets.Add("DungeonEncounters");

                // Заголовки листа Map являются частью внешней схемы workbook; не переименовывать без миграции.
                mapSheet.Cell(1, 1).SetValue("Name");
                mapSheet.Cell(1, 2).SetValue("Width");
                mapSheet.Cell(1, 3).SetValue("Height");
                mapSheet.Cell(1, 4).SetValue("ScaleMiles");
                mapSheet.Cell(2, 1).SetValue(map.Name);
                mapSheet.Cell(2, 2).SetValue(map.Width);
                mapSheet.Cell(2, 3).SetValue(map.Height);
                mapSheet.Cell(2, 4).SetValue(scaleMiles);

                WriteCellsSheet(cellsSheet, map.Cells);

                WriteSettlementRow(settlementsSheet, 1, null, true);
                if (map.Settlements != null)
                {
                    for (int i = 0; i < map.Settlements.Count; i++)
                    {
                        WriteSettlementRow(settlementsSheet, i + 2, map.Settlements[i], false);
                    }
                }

                WriteEdgesSheet(roadsSheet, map.Roads);
                WriteEdgesSheet(riversSheet, map.Rivers);
                WriteDomainsSheets(domainsSheet, domainHexesSheet, map.Domains);
                WriteRealmSheets(realmsSheet, vassalLinksSheet, map);
                WriteHexFeaturesSheet(featuresSheet, map.Features);
                WriteDungeonSheets(dungeonsSheet, dungeonLevelsSheet, dungeonRoomsSheet, dungeonConnectionsSheet, dungeonDoorsSheet, dungeonEncountersSheet, map.Dungeons);

                foreach (IXLWorksheet sheet in workbook.Worksheets)
                {
                    sheet.ColumnsUsed().AdjustToContents();
                }

                workbook.SaveAs(fileName);
            }
        }

        public HexMapRecord LoadMap(string fileName)
        {
            using (XLWorkbook workbook = new XLWorkbook(fileName))
            {
                IXLWorksheet mapSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Map");
                IXLWorksheet cellsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Cells");
                IXLWorksheet settlementsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Settlements");
                IXLWorksheet roadsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Roads");
                IXLWorksheet riversSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Rivers");
                IXLWorksheet domainsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Domains");
                IXLWorksheet domainHexesSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DomainHexes");
                IXLWorksheet realmsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Realms");
                IXLWorksheet vassalLinksSheet = ExcelWorksheetShim.FindWorksheet(workbook, "VassalLinks");
                IXLWorksheet featuresSheet = ExcelWorksheetShim.FindWorksheet(workbook, "HexFeatures");
                IXLWorksheet dungeonsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "Dungeons");
                IXLWorksheet dungeonLevelsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DungeonLevels");
                IXLWorksheet dungeonRoomsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DungeonRooms");
                IXLWorksheet dungeonConnectionsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DungeonConnections");
                IXLWorksheet dungeonDoorsSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DungeonDoors");
                IXLWorksheet dungeonEncountersSheet = ExcelWorksheetShim.FindWorksheet(workbook, "DungeonEncounters");

                if (mapSheet == null || cellsSheet == null || settlementsSheet == null)
                {
                    throw new InvalidOperationException("Workbook must contain Map, Cells, and Settlements sheets.");
                }

                HexMapRecord map = new HexMapRecord();
                map.Name = ExcelWorksheetShim.ReadString(mapSheet, 2, 1);
                map.Width = Math.Max(1, ExcelWorksheetShim.ReadInt(mapSheet, 2, 2, 12));
                map.Height = Math.Max(1, ExcelWorksheetShim.ReadInt(mapSheet, 2, 3, 10));
                map.Cells.Clear();
                map.Settlements.Clear();
                map.Roads.Clear();
                map.Rivers.Clear();
                map.Domains.Clear();
                map.Realms.Clear();
                map.VassalLinks.Clear();

                int cellRows = ExcelWorksheetShim.GetUsedRows(cellsSheet);
                int cellColumns = ExcelWorksheetShim.GetUsedColumns(cellsSheet);
                for (int row = 2; row <= cellRows; row++)
                {
                    map.Cells.Add(new HexCellRecord
                    {
                        Q = ExcelWorksheetShim.ReadInt(cellsSheet, row, 1, 0),
                        R = ExcelWorksheetShim.ReadInt(cellsSheet, row, 2, 0),
                        Terrain = MapDataNormalizer.TerrainKey(ExcelWorksheetShim.ReadString(cellsSheet, row, 3)),
                        Elevation = MapDataNormalizer.ElevationKey(ExcelWorksheetShim.ReadString(cellsSheet, row, 4)),
                        Water = MapDataNormalizer.WaterKey(ExcelWorksheetShim.ReadString(cellsSheet, row, 5)),
                        WaterFeatureName = cellColumns >= 6 ? ExcelWorksheetShim.ReadString(cellsSheet, row, 6) : ""
                    });
                }

                int settlementRows = ExcelWorksheetShim.GetUsedRows(settlementsSheet);
                for (int row = 2; row <= settlementRows; row++)
                {
                    MapSettlementRecord settlement = ReadSettlementRow(settlementsSheet, row);
                    if (!string.IsNullOrWhiteSpace(settlement.Name))
                    {
                        if (string.IsNullOrWhiteSpace(settlement.Id)) settlement.Id = Guid.NewGuid().ToString("N");
                        map.Settlements.Add(settlement);
                    }
                }

                if (roadsSheet != null) map.Roads = ReadEdgesSheet(roadsSheet, "Road");
                if (riversSheet != null) map.Rivers = ReadEdgesSheet(riversSheet, "River");
                if (domainsSheet != null) map.Domains = ReadDomainsSheets(domainsSheet, domainHexesSheet);
                if (realmsSheet != null) ReadRealmSheets(realmsSheet, vassalLinksSheet, map);
                if (featuresSheet != null) map.Features = ReadHexFeaturesSheet(featuresSheet);
                if (dungeonsSheet != null) map.Dungeons = ReadDungeonSheets(dungeonsSheet, dungeonLevelsSheet, dungeonRoomsSheet, dungeonConnectionsSheet, dungeonDoorsSheet, dungeonEncountersSheet);

                MapDataNormalizer.NormalizeMapShell(map);
                return map;
            }
        }

        private void WriteHexFeaturesSheet(IXLWorksheet sheet, List<HexFeatureRecord> features)
        {
            string[] headers =
            {
                "Id", "Name", "Kind", "Subtype", "Q", "R", "IconKey", "Description", "Severity",
                "DungeonId", "DungeonType", "DungeonLevel", "DungeonSize", "UpdatedAt"
            };
            for (int i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).SetValue(headers[i]);
            features = features ?? new List<HexFeatureRecord>();
            for (int i = 0; i < features.Count; i++)
            {
                HexFeatureRecord feature = features[i];
                int row = i + 2;
                sheet.Cell(row, 1).SetValue(feature.Id);
                sheet.Cell(row, 2).SetValue(feature.Name);
                sheet.Cell(row, 3).SetValue(feature.Kind);
                sheet.Cell(row, 4).SetValue(feature.Subtype);
                sheet.Cell(row, 5).SetValue(feature.Q);
                sheet.Cell(row, 6).SetValue(feature.R);
                sheet.Cell(row, 7).SetValue(feature.IconKey);
                sheet.Cell(row, 8).SetValue(feature.Description);
                sheet.Cell(row, 9).SetValue(feature.Severity);
                sheet.Cell(row, 10).SetValue(feature.DungeonId);
                sheet.Cell(row, 11).SetValue(feature.DungeonType);
                sheet.Cell(row, 12).SetValue(feature.DungeonLevel);
                sheet.Cell(row, 13).SetValue(feature.DungeonSize);
                sheet.Cell(row, 14).SetValue(feature.UpdatedAt.ToString("o", CultureInfo.InvariantCulture));
            }
        }

        private List<HexFeatureRecord> ReadHexFeaturesSheet(IXLWorksheet sheet)
        {
            List<HexFeatureRecord> features = new List<HexFeatureRecord>();
            int rows = ExcelWorksheetShim.GetUsedRows(sheet);
            for (int row = 2; row <= rows; row++)
            {
                string name = ExcelWorksheetShim.ReadString(sheet, row, 2);
                string kind = ExcelWorksheetShim.ReadString(sheet, row, 3);
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(kind)) continue;
                HexFeatureRecord feature = new HexFeatureRecord
                {
                    Id = ExcelWorksheetShim.ReadString(sheet, row, 1),
                    Name = name,
                    Kind = kind,
                    Subtype = ExcelWorksheetShim.ReadString(sheet, row, 4),
                    Q = ExcelWorksheetShim.ReadInt(sheet, row, 5, 0),
                    R = ExcelWorksheetShim.ReadInt(sheet, row, 6, 0),
                    IconKey = ExcelWorksheetShim.ReadString(sheet, row, 7),
                    Description = ExcelWorksheetShim.ReadString(sheet, row, 8),
                    Severity = ExcelWorksheetShim.ReadString(sheet, row, 9),
                    DungeonId = ExcelWorksheetShim.ReadString(sheet, row, 10),
                    DungeonType = ExcelWorksheetShim.ReadString(sheet, row, 11),
                    DungeonLevel = ExcelWorksheetShim.ReadInt(sheet, row, 12, 0),
                    DungeonSize = ExcelWorksheetShim.ReadString(sheet, row, 13)
                };
                MapDataNormalizer.NormalizeHexFeature(feature);
                features.Add(feature);
            }
            return features;
        }

        private void WriteDungeonSheets(
            IXLWorksheet dungeonsSheet,
            IXLWorksheet levelsSheet,
            IXLWorksheet roomsSheet,
            IXLWorksheet connectionsSheet,
            IXLWorksheet doorsSheet,
            IXLWorksheet encountersSheet,
            List<DungeonRecord> dungeons)
        {
            string[] dungeonHeaders = { "Id", "Name", "DungeonType", "Size", "RecommendedLevel", "ChallengeTier", "Notes", "UpdatedAt" };
            string[] levelHeaders = { "DungeonId", "LevelNumber", "Width", "Height" };
            string[] roomHeaders = { "DungeonId", "LevelNumber", "Id", "X", "Y", "Width", "Height", "Shape", "Kind", "Title", "Details", "Monster", "Treasure", "Trap", "UniqueFeature" };
            string[] connectionHeaders = { "DungeonId", "LevelNumber", "FromRoomId", "ToRoomId", "Kind", "PassageWidth", "DoorKind", "PathPoints" };
            string[] doorHeaders = { "DungeonId", "LevelNumber", "Id", "X", "Y", "Kind", "Orientation", "FromRoomId", "ToRoomId", "Notes" };
            string[] encounterHeaders = { "DungeonId", "DungeonLevel", "Roll", "MonsterLevel", "Monster", "CountExpression", "Notes" };
            ExcelWorksheetShim.WriteHeaderRow(dungeonsSheet, dungeonHeaders);
            ExcelWorksheetShim.WriteHeaderRow(levelsSheet, levelHeaders);
            ExcelWorksheetShim.WriteHeaderRow(roomsSheet, roomHeaders);
            ExcelWorksheetShim.WriteHeaderRow(connectionsSheet, connectionHeaders);
            ExcelWorksheetShim.WriteHeaderRow(doorsSheet, doorHeaders);
            ExcelWorksheetShim.WriteHeaderRow(encountersSheet, encounterHeaders);

            dungeons = dungeons ?? new List<DungeonRecord>();
            int dungeonRow = 2;
            int levelRow = 2;
            int roomRow = 2;
            int connectionRow = 2;
            int doorRow = 2;
            int encounterRow = 2;
            foreach (DungeonRecord dungeon in dungeons.Where(d => d != null))
            {
                dungeonsSheet.Cell(dungeonRow, 1).SetValue(dungeon.Id);
                dungeonsSheet.Cell(dungeonRow, 2).SetValue(dungeon.Name);
                dungeonsSheet.Cell(dungeonRow, 3).SetValue(dungeon.DungeonType);
                dungeonsSheet.Cell(dungeonRow, 4).SetValue(dungeon.Size);
                dungeonsSheet.Cell(dungeonRow, 5).SetValue(dungeon.RecommendedLevel);
                dungeonsSheet.Cell(dungeonRow, 6).SetValue(dungeon.ChallengeTier);
                dungeonsSheet.Cell(dungeonRow, 7).SetValue(dungeon.Notes);
                dungeonsSheet.Cell(dungeonRow, 8).SetValue(dungeon.UpdatedAt.ToString("o", CultureInfo.InvariantCulture));
                dungeonRow++;

                foreach (DungeonLevelRecord level in dungeon.Levels ?? new List<DungeonLevelRecord>())
                {
                    levelsSheet.Cell(levelRow, 1).SetValue(dungeon.Id);
                    levelsSheet.Cell(levelRow, 2).SetValue(level.LevelNumber);
                    levelsSheet.Cell(levelRow, 3).SetValue(level.Width);
                    levelsSheet.Cell(levelRow, 4).SetValue(level.Height);
                    levelRow++;

                    foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                    {
                        roomsSheet.Cell(roomRow, 1).SetValue(dungeon.Id);
                        roomsSheet.Cell(roomRow, 2).SetValue(level.LevelNumber);
                        roomsSheet.Cell(roomRow, 3).SetValue(room.Id);
                        roomsSheet.Cell(roomRow, 4).SetValue(room.X);
                        roomsSheet.Cell(roomRow, 5).SetValue(room.Y);
                        roomsSheet.Cell(roomRow, 6).SetValue(room.Width);
                        roomsSheet.Cell(roomRow, 7).SetValue(room.Height);
                        roomsSheet.Cell(roomRow, 8).SetValue(room.Shape);
                        roomsSheet.Cell(roomRow, 9).SetValue(room.Kind);
                        roomsSheet.Cell(roomRow, 10).SetValue(room.Title);
                        roomsSheet.Cell(roomRow, 11).SetValue(room.Details);
                        roomsSheet.Cell(roomRow, 12).SetValue(room.Monster);
                        roomsSheet.Cell(roomRow, 13).SetValue(room.Treasure);
                        roomsSheet.Cell(roomRow, 14).SetValue(room.Trap);
                        roomsSheet.Cell(roomRow, 15).SetValue(room.UniqueFeature);
                        roomRow++;
                    }

                    foreach (DungeonConnectionRecord connection in level.Connections ?? new List<DungeonConnectionRecord>())
                    {
                        connectionsSheet.Cell(connectionRow, 1).SetValue(dungeon.Id);
                        connectionsSheet.Cell(connectionRow, 2).SetValue(level.LevelNumber);
                        connectionsSheet.Cell(connectionRow, 3).SetValue(connection.FromRoomId);
                        connectionsSheet.Cell(connectionRow, 4).SetValue(connection.ToRoomId);
                        connectionsSheet.Cell(connectionRow, 5).SetValue(connection.Kind);
                        connectionsSheet.Cell(connectionRow, 6).SetValue(connection.PassageWidth);
                        connectionsSheet.Cell(connectionRow, 7).SetValue(connection.DoorKind);
                        connectionsSheet.Cell(connectionRow, 8).SetValue(FormatDungeonPathPoints(connection.PathPoints));
                        connectionRow++;
                    }

                    foreach (DungeonDoorRecord door in level.Doors ?? new List<DungeonDoorRecord>())
                    {
                        doorsSheet.Cell(doorRow, 1).SetValue(dungeon.Id);
                        doorsSheet.Cell(doorRow, 2).SetValue(level.LevelNumber);
                        doorsSheet.Cell(doorRow, 3).SetValue(door.Id);
                        doorsSheet.Cell(doorRow, 4).SetValue(door.X);
                        doorsSheet.Cell(doorRow, 5).SetValue(door.Y);
                        doorsSheet.Cell(doorRow, 6).SetValue(door.Kind);
                        doorsSheet.Cell(doorRow, 7).SetValue(door.Orientation);
                        doorsSheet.Cell(doorRow, 8).SetValue(door.FromRoomId);
                        doorsSheet.Cell(doorRow, 9).SetValue(door.ToRoomId);
                        doorsSheet.Cell(doorRow, 10).SetValue(door.Notes);
                        doorRow++;
                    }
                }

                foreach (DungeonEncounterRecord encounter in dungeon.WanderingEncounters ?? new List<DungeonEncounterRecord>())
                {
                    encountersSheet.Cell(encounterRow, 1).SetValue(dungeon.Id);
                    encountersSheet.Cell(encounterRow, 2).SetValue(encounter.DungeonLevel);
                    encountersSheet.Cell(encounterRow, 3).SetValue(encounter.Roll);
                    encountersSheet.Cell(encounterRow, 4).SetValue(encounter.MonsterLevel);
                    encountersSheet.Cell(encounterRow, 5).SetValue(encounter.Monster);
                    encountersSheet.Cell(encounterRow, 6).SetValue(encounter.CountExpression);
                    encountersSheet.Cell(encounterRow, 7).SetValue(encounter.Notes);
                    encounterRow++;
                }
            }
        }

        private static string FormatDungeonPathPoints(List<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count == 0) return "";
            return string.Join(";", points
                .Where(p => p != null)
                .Select(p => p.X.ToString("0.###", CultureInfo.InvariantCulture) + "," + p.Y.ToString("0.###", CultureInfo.InvariantCulture)));
        }

        private static List<DungeonPathPointRecord> ParseDungeonPathPoints(string value)
        {
            List<DungeonPathPointRecord> points = new List<DungeonPathPointRecord>();
            if (string.IsNullOrWhiteSpace(value)) return points;

            foreach (string token in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = token.Split(',');
                if (parts.Length != 2) continue;
                double x;
                double y;
                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) continue;
                if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) continue;
                points.Add(new DungeonPathPointRecord { X = x, Y = y });
            }

            return points;
        }

        private List<DungeonRecord> ReadDungeonSheets(
            IXLWorksheet dungeonsSheet,
            IXLWorksheet levelsSheet,
            IXLWorksheet roomsSheet,
            IXLWorksheet connectionsSheet,
            IXLWorksheet doorsSheet,
            IXLWorksheet encountersSheet)
        {
            List<DungeonRecord> dungeons = new List<DungeonRecord>();
            Dictionary<string, DungeonRecord> byId = new Dictionary<string, DungeonRecord>(StringComparer.OrdinalIgnoreCase);
            int rows = ExcelWorksheetShim.GetUsedRows(dungeonsSheet);
            for (int row = 2; row <= rows; row++)
            {
                string name = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 2);
                if (string.IsNullOrWhiteSpace(name)) continue;
                DungeonRecord dungeon = new DungeonRecord
                {
                    Id = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 1),
                    Name = name,
                    DungeonType = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 3),
                    Size = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 4),
                    RecommendedLevel = ExcelWorksheetShim.ReadInt(dungeonsSheet, row, 5, 1),
                    ChallengeTier = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 6),
                    Notes = ExcelWorksheetShim.ReadString(dungeonsSheet, row, 7),
                    Levels = new List<DungeonLevelRecord>(),
                    WanderingEncounters = new List<DungeonEncounterRecord>()
                };
                MapDataNormalizer.NormalizeDungeon(dungeon);
                dungeons.Add(dungeon);
                byId[dungeon.Id] = dungeon;
            }

            Dictionary<string, DungeonLevelRecord> levelByKey = new Dictionary<string, DungeonLevelRecord>(StringComparer.OrdinalIgnoreCase);
            if (levelsSheet != null)
            {
                int levelRows = ExcelWorksheetShim.GetUsedRows(levelsSheet);
                for (int row = 2; row <= levelRows; row++)
                {
                    string dungeonId = ExcelWorksheetShim.ReadString(levelsSheet, row, 1);
                    DungeonRecord dungeon;
                    if (!byId.TryGetValue(dungeonId, out dungeon)) continue;
                    DungeonLevelRecord level = new DungeonLevelRecord
                    {
                        LevelNumber = ExcelWorksheetShim.ReadInt(levelsSheet, row, 2, 1),
                        Width = ExcelWorksheetShim.ReadInt(levelsSheet, row, 3, 16),
                        Height = ExcelWorksheetShim.ReadInt(levelsSheet, row, 4, 12),
                        Rooms = new List<DungeonRoomRecord>(),
                        Connections = new List<DungeonConnectionRecord>(),
                        Doors = new List<DungeonDoorRecord>()
                    };
                    dungeon.Levels.Add(level);
                    levelByKey[dungeonId + "|" + level.LevelNumber] = level;
                }
            }

            if (roomsSheet != null)
            {
                int shapeColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Shape", 0);
                int roomKindColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Kind", 8);
                int roomTitleColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Title", 9);
                int roomDetailsColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Details", 10);
                int roomMonsterColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Monster", 11);
                int roomTreasureColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Treasure", 12);
                int roomTrapColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "Trap", 13);
                int roomUniqueColumn = ExcelWorksheetShim.GetHeaderColumn(roomsSheet, "UniqueFeature", 14);
                int roomRows = ExcelWorksheetShim.GetUsedRows(roomsSheet);
                for (int row = 2; row <= roomRows; row++)
                {
                    string dungeonId = ExcelWorksheetShim.ReadString(roomsSheet, row, 1);
                    int levelNumber = ExcelWorksheetShim.ReadInt(roomsSheet, row, 2, 1);
                    DungeonLevelRecord level;
                    if (!levelByKey.TryGetValue(dungeonId + "|" + levelNumber, out level)) continue;
                    level.Rooms.Add(new DungeonRoomRecord
                    {
                        Id = ExcelWorksheetShim.ReadString(roomsSheet, row, 3),
                        LevelNumber = levelNumber,
                        X = ExcelWorksheetShim.ReadInt(roomsSheet, row, 4, 0),
                        Y = ExcelWorksheetShim.ReadInt(roomsSheet, row, 5, 0),
                        Width = ExcelWorksheetShim.ReadInt(roomsSheet, row, 6, 2),
                        Height = ExcelWorksheetShim.ReadInt(roomsSheet, row, 7, 2),
                        Shape = shapeColumn <= 0 ? "Rectangle" : ExcelWorksheetShim.ReadString(roomsSheet, row, shapeColumn),
                        Kind = ExcelWorksheetShim.ReadString(roomsSheet, row, roomKindColumn),
                        Title = ExcelWorksheetShim.ReadString(roomsSheet, row, roomTitleColumn),
                        Details = ExcelWorksheetShim.ReadString(roomsSheet, row, roomDetailsColumn),
                        Monster = ExcelWorksheetShim.ReadString(roomsSheet, row, roomMonsterColumn),
                        Treasure = ExcelWorksheetShim.ReadString(roomsSheet, row, roomTreasureColumn),
                        Trap = ExcelWorksheetShim.ReadString(roomsSheet, row, roomTrapColumn),
                        UniqueFeature = ExcelWorksheetShim.ReadString(roomsSheet, row, roomUniqueColumn)
                    });
                }
            }

            if (connectionsSheet != null)
            {
                int passageWidthColumn = ExcelWorksheetShim.GetHeaderColumn(connectionsSheet, "PassageWidth", 0);
                int doorKindColumn = ExcelWorksheetShim.GetHeaderColumn(connectionsSheet, "DoorKind", 0);
                int pathPointsColumn = ExcelWorksheetShim.GetHeaderColumn(connectionsSheet, "PathPoints", 0);
                int connectionRows = ExcelWorksheetShim.GetUsedRows(connectionsSheet);
                for (int row = 2; row <= connectionRows; row++)
                {
                    string dungeonId = ExcelWorksheetShim.ReadString(connectionsSheet, row, 1);
                    int levelNumber = ExcelWorksheetShim.ReadInt(connectionsSheet, row, 2, 1);
                    DungeonLevelRecord level;
                    if (!levelByKey.TryGetValue(dungeonId + "|" + levelNumber, out level)) continue;
                    level.Connections.Add(new DungeonConnectionRecord
                    {
                        FromRoomId = ExcelWorksheetShim.ReadString(connectionsSheet, row, 3),
                        ToRoomId = ExcelWorksheetShim.ReadString(connectionsSheet, row, 4),
                        Kind = ExcelWorksheetShim.ReadString(connectionsSheet, row, 5),
                        PassageWidth = passageWidthColumn <= 0 ? 1 : ExcelWorksheetShim.ReadInt(connectionsSheet, row, passageWidthColumn, 1),
                        DoorKind = doorKindColumn <= 0 ? "" : ExcelWorksheetShim.ReadString(connectionsSheet, row, doorKindColumn),
                        PathPoints = pathPointsColumn <= 0
                            ? new List<DungeonPathPointRecord>()
                            : ParseDungeonPathPoints(ExcelWorksheetShim.ReadString(connectionsSheet, row, pathPointsColumn))
                    });
                }
            }

            if (doorsSheet != null)
            {
                int idColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "Id", 3);
                int xColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "X", 4);
                int yColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "Y", 5);
                int kindColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "Kind", 6);
                int orientationColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "Orientation", 7);
                int fromColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "FromRoomId", 8);
                int toColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "ToRoomId", 9);
                int notesColumn = ExcelWorksheetShim.GetHeaderColumn(doorsSheet, "Notes", 10);
                int doorRows = ExcelWorksheetShim.GetUsedRows(doorsSheet);
                ExcelWorksheetShim shim = new ExcelWorksheetShim(doorsSheet);
                for (int row = 2; row <= doorRows; row++)
                {
                    string dungeonId = ExcelWorksheetShim.ReadString(doorsSheet, row, 1);
                    int levelNumber = ExcelWorksheetShim.ReadInt(doorsSheet, row, 2, 1);
                    DungeonLevelRecord level;
                    if (!levelByKey.TryGetValue(dungeonId + "|" + levelNumber, out level)) continue;
                    if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();
                    level.Doors.Add(new DungeonDoorRecord
                    {
                        Id = ExcelWorksheetShim.ReadString(doorsSheet, row, idColumn),
                        LevelNumber = levelNumber,
                        X = ExcelWorksheetShim.ReadDouble(shim, row, xColumn, 0),
                        Y = ExcelWorksheetShim.ReadDouble(shim, row, yColumn, 0),
                        Kind = ExcelWorksheetShim.ReadString(doorsSheet, row, kindColumn),
                        Orientation = ExcelWorksheetShim.ReadString(doorsSheet, row, orientationColumn),
                        FromRoomId = ExcelWorksheetShim.ReadString(doorsSheet, row, fromColumn),
                        ToRoomId = ExcelWorksheetShim.ReadString(doorsSheet, row, toColumn),
                        Notes = ExcelWorksheetShim.ReadString(doorsSheet, row, notesColumn)
                    });
                }
            }

            if (encountersSheet != null)
            {
                int monsterLevelColumn = ExcelWorksheetShim.GetHeaderColumn(encountersSheet, "MonsterLevel", 0);
                int monsterColumn = ExcelWorksheetShim.GetHeaderColumn(encountersSheet, "Monster", monsterLevelColumn <= 0 ? 4 : 5);
                int countColumn = ExcelWorksheetShim.GetHeaderColumn(encountersSheet, "CountExpression", monsterLevelColumn <= 0 ? 5 : 6);
                int notesColumn = ExcelWorksheetShim.GetHeaderColumn(encountersSheet, "Notes", monsterLevelColumn <= 0 ? 6 : 7);
                int encounterRows = ExcelWorksheetShim.GetUsedRows(encountersSheet);
                for (int row = 2; row <= encounterRows; row++)
                {
                    string dungeonId = ExcelWorksheetShim.ReadString(encountersSheet, row, 1);
                    DungeonRecord dungeon;
                    if (!byId.TryGetValue(dungeonId, out dungeon)) continue;
                    dungeon.WanderingEncounters.Add(new DungeonEncounterRecord
                    {
                        DungeonLevel = ExcelWorksheetShim.ReadInt(encountersSheet, row, 2, 1),
                        Roll = ExcelWorksheetShim.ReadInt(encountersSheet, row, 3, 1),
                        MonsterLevel = monsterLevelColumn <= 0 ? 1 : ExcelWorksheetShim.ReadInt(encountersSheet, row, monsterLevelColumn, 1),
                        Monster = ExcelWorksheetShim.ReadString(encountersSheet, row, monsterColumn),
                        CountExpression = ExcelWorksheetShim.ReadString(encountersSheet, row, countColumn),
                        Notes = ExcelWorksheetShim.ReadString(encountersSheet, row, notesColumn)
                    });
                }
            }

            foreach (DungeonRecord dungeon in dungeons) MapDataNormalizer.NormalizeDungeon(dungeon);
            return dungeons;
        }

        private void WriteDomainsSheets(ExcelWorksheetShim domainsSheet, ExcelWorksheetShim domainHexesSheet, List<DomainRecord> domains)
        {
            string[] domainHeaders =
            {
                "Id", "Name", "Type", "Classification", "Alignment", "LandValueMode", "FixedLandValueGp",
                "PeasantFamilies", "UrbanFamilies", "StrongholdValueGp", "GarrisonGpPerFamily",
                "TaxGpPerFamily", "LiturgiesGpPerFamily", "TithesGpPerFamily", "MaintenanceGpPerFamily",
                "BaseMorale", "CurrentMorale", "ColorArgb", "RulerSource", "RulerLibraryCharacterId",
                "RulerName", "RulerClass", "RulerLevel", "RulerCHA", "RulerAlignment", "RulerLeadership", "Notes",
                "RealmId", "CapitalSettlementId", "Race", "StrongholdId", "StrongholdName", "StrongholdQ", "StrongholdR",
                "StrongholdType", "StrongholdIconKey", "StrongholdInSettlement", "StrongholdSettlementId",
                "StrongholdActsAsMarketClassVI", "StrongholdSecuresDomain", "StrongholdIsUnderground", "StrongholdNaturalMajesty"
            };

            ExcelWorksheetShim.WriteHeaderRow(domainsSheet, domainHeaders);

            domains = domains ?? new List<DomainRecord>();
            for (int i = 0; i < domains.Count; i++)
            {
                DomainRecord domain = domains[i];
                CharacterRecord ruler = domain.Ruler == null ? null : domain.Ruler.Snapshot;
                int row = i + 2;
                domainsSheet.Cells[row, 1] = domain.Id;
                domainsSheet.Cells[row, 2] = domain.Name;
                domainsSheet.Cells[row, 3] = domain.DomainType;
                domainsSheet.Cells[row, 4] = domain.Classification;
                domainsSheet.Cells[row, 5] = domain.DomainAlignment;
                domainsSheet.Cells[row, 6] = domain.LandValueMode;
                domainsSheet.Cells[row, 7] = domain.FixedLandValueGp;
                domainsSheet.Cells[row, 8] = domain.PeasantFamilies;
                domainsSheet.Cells[row, 9] = domain.UrbanFamilies;
                domainsSheet.Cells[row, 10] = domain.StrongholdValueGp;
                domainsSheet.Cells[row, 11] = domain.GarrisonGpPerFamily;
                domainsSheet.Cells[row, 12] = domain.TaxGpPerFamily;
                domainsSheet.Cells[row, 13] = domain.LiturgiesGpPerFamily;
                domainsSheet.Cells[row, 14] = domain.TithesGpPerFamily;
                domainsSheet.Cells[row, 15] = domain.MaintenanceGpPerFamily;
                domainsSheet.Cells[row, 16] = domain.BaseMorale;
                domainsSheet.Cells[row, 17] = domain.CurrentMorale;
                domainsSheet.Cells[row, 18] = domain.ColorArgb;
                domainsSheet.Cells[row, 19] = domain.Ruler == null ? "" : domain.Ruler.SourceMode;
                domainsSheet.Cells[row, 20] = domain.Ruler == null ? "" : domain.Ruler.LibraryCharacterId;
                domainsSheet.Cells[row, 21] = ruler == null ? "" : ruler.Name;
                domainsSheet.Cells[row, 22] = ruler == null ? "" : ruler.ClassName;
                domainsSheet.Cells[row, 23] = ruler == null ? 0 : ruler.Level;
                domainsSheet.Cells[row, 24] = ruler == null ? 9 : ruler.CHA;
                domainsSheet.Cells[row, 25] = ruler == null ? "" : ruler.Alignment;
                domainsSheet.Cells[row, 26] = domain.Ruler != null && domain.Ruler.HasLeadership() ? "Yes" : "No";
                domainsSheet.Cells[row, 27] = domain.Notes;
                domainsSheet.Cells[row, 28] = domain.RealmId;
                domainsSheet.Cells[row, 29] = domain.CapitalSettlementId;
                domainsSheet.Cells[row, 30] = MapDataNormalizer.SettlementRace(domain.Race);
                domainsSheet.Cells[row, 31] = domain.StrongholdId;
                domainsSheet.Cells[row, 32] = domain.StrongholdName;
                domainsSheet.Cells[row, 33] = domain.StrongholdQ;
                domainsSheet.Cells[row, 34] = domain.StrongholdR;
                domainsSheet.Cells[row, 35] = domain.StrongholdType;
                domainsSheet.Cells[row, 36] = domain.StrongholdIconKey;
                domainsSheet.Cells[row, 37] = domain.StrongholdInSettlement ? "Yes" : "No";
                domainsSheet.Cells[row, 38] = domain.StrongholdSettlementId;
                domainsSheet.Cells[row, 39] = domain.StrongholdActsAsMarketClassVI ? "Yes" : "No";
                domainsSheet.Cells[row, 40] = domain.StrongholdSecuresDomain ? "Yes" : "No";
                domainsSheet.Cells[row, 41] = domain.StrongholdIsUnderground ? "Yes" : "No";
                domainsSheet.Cells[row, 42] = domain.StrongholdNaturalMajesty ? "Yes" : "No";
            }

            domainHexesSheet.Cells[1, 1] = "DomainId";
            domainHexesSheet.Cells[1, 2] = "Q";
            domainHexesSheet.Cells[1, 3] = "R";
            domainHexesSheet.Cells[1, 4] = "LandValueGp";
            int hexRow = 2;
            foreach (DomainRecord domain in domains)
            {
                if (domain.Hexes == null) continue;
                foreach (DomainHexRecord hex in domain.Hexes)
                {
                    domainHexesSheet.Cells[hexRow, 1] = domain.Id;
                    domainHexesSheet.Cells[hexRow, 2] = hex.Q;
                    domainHexesSheet.Cells[hexRow, 3] = hex.R;
                    domainHexesSheet.Cells[hexRow, 4] = hex.LandValueGp;
                    hexRow++;
                }
            }
        }

        private void WriteDomainsSheets(IXLWorksheet domainsSheet, IXLWorksheet domainHexesSheet, List<DomainRecord> domains)
        {
            WriteDomainsSheets(new ExcelWorksheetShim(domainsSheet), new ExcelWorksheetShim(domainHexesSheet), domains);
        }

        private List<DomainRecord> ReadDomainsSheets(IXLWorksheet domainsSheet, IXLWorksheet domainHexesSheet)
        {
            return ReadDomainsSheets(new ExcelWorksheetShim(domainsSheet), new ExcelWorksheetShim(domainHexesSheet));
        }

        private List<DomainRecord> ReadDomainsSheets(ExcelWorksheetShim domainsSheet, ExcelWorksheetShim domainHexesSheet)
        {
            List<DomainRecord> domains = new List<DomainRecord>();
            Dictionary<string, DomainRecord> byId = new Dictionary<string, DomainRecord>();
            int rows = ExcelWorksheetShim.GetUsedRows(domainsSheet);

            for (int row = 2; row <= rows; row++)
            {
                string name = ExcelWorksheetShim.ReadString(domainsSheet, row, 2);
                if (string.IsNullOrWhiteSpace(name)) continue;

                DomainRecord domain = new DomainRecord();
                domain.Id = ExcelWorksheetShim.ReadString(domainsSheet, row, 1);
                if (string.IsNullOrWhiteSpace(domain.Id)) domain.Id = Guid.NewGuid().ToString("N");
                domain.Name = name;
                domain.DomainType = ExcelWorksheetShim.ReadString(domainsSheet, row, 3);
                domain.Classification = ExcelWorksheetShim.ReadString(domainsSheet, row, 4);
                domain.DomainAlignment = ExcelWorksheetShim.ReadString(domainsSheet, row, 5);
                domain.LandValueMode = ExcelWorksheetShim.ReadString(domainsSheet, row, 6);
                domain.FixedLandValueGp = ExcelWorksheetShim.ReadInt(domainsSheet, row, 7, 6);
                domain.PeasantFamilies = ExcelWorksheetShim.ReadInt(domainsSheet, row, 8, 0);
                domain.UrbanFamilies = ExcelWorksheetShim.ReadInt(domainsSheet, row, 9, 0);
                domain.StrongholdValueGp = ExcelWorksheetShim.ReadInt(domainsSheet, row, 10, 0);
                domain.GarrisonGpPerFamily = ExcelWorksheetShim.ReadInt(domainsSheet, row, 11, 2);
                domain.TaxGpPerFamily = ExcelWorksheetShim.ReadInt(domainsSheet, row, 12, 2);
                domain.LiturgiesGpPerFamily = ExcelWorksheetShim.ReadInt(domainsSheet, row, 13, 1);
                domain.TithesGpPerFamily = ExcelWorksheetShim.ReadInt(domainsSheet, row, 14, 1);
                domain.MaintenanceGpPerFamily = ExcelWorksheetShim.ReadInt(domainsSheet, row, 15, 1);
                domain.BaseMorale = ExcelWorksheetShim.ReadInt(domainsSheet, row, 16, 0);
                domain.CurrentMorale = ExcelWorksheetShim.ReadInt(domainsSheet, row, 17, 0);
                domain.ColorArgb = ExcelWorksheetShim.ReadInt(domainsSheet, row, 18, unchecked((int)0x6637A86B));
                domain.Notes = ExcelWorksheetShim.ReadString(domainsSheet, row, 27);
                domain.RealmId = ExcelWorksheetShim.ReadString(domainsSheet, row, 28);
                domain.CapitalSettlementId = ExcelWorksheetShim.ReadString(domainsSheet, row, 29);
                domain.Race = MapDataNormalizer.SettlementRace(ExcelWorksheetShim.ReadString(domainsSheet, row, 30));
                domain.StrongholdId = ExcelWorksheetShim.ReadString(domainsSheet, row, 31);
                domain.StrongholdName = ExcelWorksheetShim.ReadString(domainsSheet, row, 32);
                domain.StrongholdQ = ExcelWorksheetShim.ReadInt(domainsSheet, row, 33, -1);
                domain.StrongholdR = ExcelWorksheetShim.ReadInt(domainsSheet, row, 34, -1);
                domain.StrongholdType = ExcelWorksheetShim.ReadString(domainsSheet, row, 35);
                domain.StrongholdIconKey = ExcelWorksheetShim.ReadString(domainsSheet, row, 36);
                domain.StrongholdInSettlement = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 37));
                domain.StrongholdSettlementId = ExcelWorksheetShim.ReadString(domainsSheet, row, 38);
                domain.StrongholdActsAsMarketClassVI = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 39));
                domain.StrongholdSecuresDomain = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 40));
                domain.StrongholdIsUnderground = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 41));
                domain.StrongholdNaturalMajesty = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 42));

                CharacterRecord ruler = new CharacterRecord
                {
                    Kind = "NPC",
                    Name = ExcelWorksheetShim.ReadString(domainsSheet, row, 21),
                    ClassName = ExcelWorksheetShim.ReadString(domainsSheet, row, 22),
                    Level = ExcelWorksheetShim.ReadInt(domainsSheet, row, 23, 0),
                    CHA = ExcelWorksheetShim.ReadInt(domainsSheet, row, 24, 9),
                    Alignment = ExcelWorksheetShim.ReadString(domainsSheet, row, 25),
                    Proficiencies = IsExcelYes(ExcelWorksheetShim.ReadString(domainsSheet, row, 26)) ? "Leadership" : ""
                };

                domain.Ruler = new DomainRulerRecord
                {
                    SourceMode = ExcelWorksheetShim.ReadString(domainsSheet, row, 19),
                    LibraryCharacterId = ExcelWorksheetShim.ReadString(domainsSheet, row, 20),
                    Snapshot = ruler
                };

                domains.Add(domain);
                byId[domain.Id] = domain;
            }

            if (domainHexesSheet != null)
            {
                int hexRows = ExcelWorksheetShim.GetUsedRows(domainHexesSheet);
                for (int row = 2; row <= hexRows; row++)
                {
                    string domainId = ExcelWorksheetShim.ReadString(domainHexesSheet, row, 1);
                    DomainRecord domain;
                    if (!byId.TryGetValue(domainId, out domain)) continue;
                    if (domain.Hexes == null) domain.Hexes = new List<DomainHexRecord>();
                    domain.Hexes.Add(new DomainHexRecord
                    {
                        Q = ExcelWorksheetShim.ReadInt(domainHexesSheet, row, 2, 0),
                        R = ExcelWorksheetShim.ReadInt(domainHexesSheet, row, 3, 0),
                        LandValueGp = ExcelWorksheetShim.ReadInt(domainHexesSheet, row, 4, domain.FixedLandValueGp <= 0 ? 6 : domain.FixedLandValueGp)
                    });
                }
            }

            return domains;
        }

        private void WriteRealmSheets(ExcelWorksheetShim realmsSheet, ExcelWorksheetShim vassalLinksSheet, HexMapRecord map)
        {
            string[] realmHeaders =
            {
                "Id", "Name", "Tier", "CultureKey", "CapitalSettlementId", "RulerName", "RulerLevel", "ColorArgb", "Notes", "TitleOverride", "FemaleTitleOverride"
            };
            ExcelWorksheetShim.WriteHeaderRow(realmsSheet, realmHeaders);

            List<RealmRecord> realms = map == null || map.Realms == null ? new List<RealmRecord>() : map.Realms;
            for (int i = 0; i < realms.Count; i++)
            {
                RealmRecord realm = realms[i];
                int row = i + 2;
                realmsSheet.Cells[row, 1] = realm.Id;
                realmsSheet.Cells[row, 2] = realm.Name;
                realmsSheet.Cells[row, 3] = realm.Tier;
                realmsSheet.Cells[row, 4] = realm.CultureKey;
                realmsSheet.Cells[row, 5] = realm.CapitalSettlementId;
                realmsSheet.Cells[row, 6] = realm.RulerName;
                realmsSheet.Cells[row, 7] = realm.RulerLevel;
                realmsSheet.Cells[row, 8] = realm.ColorArgb;
                realmsSheet.Cells[row, 9] = realm.Notes;
                realmsSheet.Cells[row, 10] = realm.TitleOverride;
                realmsSheet.Cells[row, 11] = realm.FemaleTitleOverride;
            }

            string[] linkHeaders = { "Id", "LiegeRealmId", "VassalRealmId", "RelationType", "Loyalty", "TributeGp", "Notes" };
            ExcelWorksheetShim.WriteHeaderRow(vassalLinksSheet, linkHeaders);

            List<VassalLinkRecord> links = map == null || map.VassalLinks == null ? new List<VassalLinkRecord>() : map.VassalLinks;
            for (int i = 0; i < links.Count; i++)
            {
                VassalLinkRecord link = links[i];
                int row = i + 2;
                vassalLinksSheet.Cells[row, 1] = link.Id;
                vassalLinksSheet.Cells[row, 2] = link.LiegeRealmId;
                vassalLinksSheet.Cells[row, 3] = link.VassalRealmId;
                vassalLinksSheet.Cells[row, 4] = link.RelationType;
                vassalLinksSheet.Cells[row, 5] = link.Loyalty;
                vassalLinksSheet.Cells[row, 6] = link.TributeGp;
                vassalLinksSheet.Cells[row, 7] = link.Notes;
            }
        }

        private void WriteRealmSheets(IXLWorksheet realmsSheet, IXLWorksheet vassalLinksSheet, HexMapRecord map)
        {
            WriteRealmSheets(new ExcelWorksheetShim(realmsSheet), new ExcelWorksheetShim(vassalLinksSheet), map);
        }

        private void ReadRealmSheets(IXLWorksheet realmsSheet, IXLWorksheet vassalLinksSheet, HexMapRecord map)
        {
            ReadRealmSheets(new ExcelWorksheetShim(realmsSheet), new ExcelWorksheetShim(vassalLinksSheet), map);
        }

        private void ReadRealmSheets(ExcelWorksheetShim realmsSheet, ExcelWorksheetShim vassalLinksSheet, HexMapRecord map)
        {
            if (map == null || realmsSheet == null) return;
            map.Realms = new List<RealmRecord>();
            map.VassalLinks = new List<VassalLinkRecord>();

            int rows = ExcelWorksheetShim.GetUsedRows(realmsSheet);
            for (int row = 2; row <= rows; row++)
            {
                string name = ExcelWorksheetShim.ReadString(realmsSheet, row, 2);
                if (string.IsNullOrWhiteSpace(name)) continue;

                RealmRecord realm = new RealmRecord
                {
                    Id = ExcelWorksheetShim.ReadString(realmsSheet, row, 1),
                    Name = name,
                    Tier = ExcelWorksheetShim.ReadString(realmsSheet, row, 3),
                    CultureKey = ExcelWorksheetShim.ReadString(realmsSheet, row, 4),
                    CapitalSettlementId = ExcelWorksheetShim.ReadString(realmsSheet, row, 5),
                    RulerName = ExcelWorksheetShim.ReadString(realmsSheet, row, 6),
                    RulerLevel = ExcelWorksheetShim.ReadInt(realmsSheet, row, 7, 7),
                    ColorArgb = ExcelWorksheetShim.ReadInt(realmsSheet, row, 8, unchecked((int)0x66547AA5)),
                    Notes = ExcelWorksheetShim.ReadString(realmsSheet, row, 9),
                    TitleOverride = ExcelWorksheetShim.ReadString(realmsSheet, row, 10),
                    FemaleTitleOverride = ExcelWorksheetShim.ReadString(realmsSheet, row, 11)
                };
                if (string.IsNullOrWhiteSpace(realm.Id)) realm.Id = Guid.NewGuid().ToString("N");
                map.Realms.Add(realm);
            }

            if (vassalLinksSheet == null) return;
            int linkRows = ExcelWorksheetShim.GetUsedRows(vassalLinksSheet);
            for (int row = 2; row <= linkRows; row++)
            {
                string liegeId = ExcelWorksheetShim.ReadString(vassalLinksSheet, row, 2);
                string vassalId = ExcelWorksheetShim.ReadString(vassalLinksSheet, row, 3);
                if (string.IsNullOrWhiteSpace(liegeId) || string.IsNullOrWhiteSpace(vassalId)) continue;

                map.VassalLinks.Add(new VassalLinkRecord
                {
                    Id = ExcelWorksheetShim.ReadString(vassalLinksSheet, row, 1),
                    LiegeRealmId = liegeId,
                    VassalRealmId = vassalId,
                    RelationType = ExcelWorksheetShim.ReadString(vassalLinksSheet, row, 4),
                    Loyalty = ExcelWorksheetShim.ReadInt(vassalLinksSheet, row, 5, 0),
                    TributeGp = ExcelWorksheetShim.ReadInt(vassalLinksSheet, row, 6, 0),
                    Notes = ExcelWorksheetShim.ReadString(vassalLinksSheet, row, 7)
                });
            }
        }

        private bool IsExcelYes(string value)
        {
            return string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Да", StringComparison.OrdinalIgnoreCase)
                || value == "1";
        }

        private void WriteSettlementRow(ExcelWorksheetShim sheet, int row, MapSettlementRecord record, bool header)
        {
            string[] fixedHeaders = { "Id", "Name", "Class", "Q", "R", "Race", "LandValue" };
            for (int i = 0; i < fixedHeaders.Length; i++)
            {
                sheet.Cells[row, i + 1] = header ? fixedHeaders[i] : GetSettlementFixedValue(record, i);
            }

            int start = fixedHeaders.Length + 1;
            for (int i = 0; i < merchandiseNames.Length; i++)
            {
                sheet.Cells[row, start + i] = header ? "Base: " + merchandiseNames[i] : record.BaseDemands[i].ToString(CultureInfo.InvariantCulture);
                sheet.Cells[row, start + merchandiseNames.Length + i] = header ? "Current: " + merchandiseNames[i] : record.CurrentDemands[i].ToString(CultureInfo.InvariantCulture);
            }
        }

        private void WriteSettlementRow(IXLWorksheet sheet, int row, MapSettlementRecord record, bool header)
        {
            WriteSettlementRow(new ExcelWorksheetShim(sheet), row, record, header);
        }

        private object GetSettlementFixedValue(MapSettlementRecord record, int index)
        {
            if (record == null) return "";
            switch (index)
            {
                case 0: return record.Id;
                case 1: return record.Name;
                case 2: return record.MarketClass;
                case 3: return record.Q;
                case 4: return record.R;
                case 5: return MapDataNormalizer.SettlementRace(record.Race);
                case 6: return record.LandValue;
                default: return "";
            }
        }

        private MapSettlementRecord ReadSettlementRow(IXLWorksheet sheet, int row)
        {
            return ReadSettlementRow(new ExcelWorksheetShim(sheet), row);
        }

        private MapSettlementRecord ReadSettlementRow(ExcelWorksheetShim sheet, int row)
        {
            bool hasRaceColumn = string.Equals(ExcelWorksheetShim.ReadString(sheet, 1, 6), "Race", StringComparison.OrdinalIgnoreCase);
            int demandStart = hasRaceColumn ? 8 : 6;
            MapSettlementRecord record = new MapSettlementRecord();
            record.Id = ExcelWorksheetShim.ReadString(sheet, row, 1);
            record.Name = ExcelWorksheetShim.ReadString(sheet, row, 2);
            record.MarketClass = Math.Max(1, Math.Min(6, ExcelWorksheetShim.ReadInt(sheet, row, 3, 6)));
            record.Q = ExcelWorksheetShim.ReadInt(sheet, row, 4, -1);
            record.R = ExcelWorksheetShim.ReadInt(sheet, row, 5, -1);
            record.Race = hasRaceColumn ? MapDataNormalizer.SettlementRace(ExcelWorksheetShim.ReadString(sheet, row, 6)) : "Human";
            record.LandValue = hasRaceColumn ? ExcelWorksheetShim.ReadString(sheet, row, 7) : "";

            int start = demandStart;
            record.BaseDemands = new double[merchandiseNames.Length];
            record.CurrentDemands = new double[merchandiseNames.Length];
            for (int i = 0; i < merchandiseNames.Length; i++)
            {
                record.BaseDemands[i] = ExcelWorksheetShim.ReadDouble(sheet, row, start + i, 0);
                record.CurrentDemands[i] = ExcelWorksheetShim.ReadDouble(sheet, row, start + merchandiseNames.Length + i, record.BaseDemands[i]);
            }
            record.UpdatedAt = DateTime.Now;
            MapDataNormalizer.NormalizeSettlementMetadata(record);
            return record;
        }

        private void WriteCellsSheet(ExcelWorksheetShim sheet, List<HexCellRecord> cells)
        {
            cells = cells ?? new List<HexCellRecord>();
            int rows = Math.Max(1, cells.Count + 1);
            object[,] values = new object[rows, 6];
            values[0, 0] = "Q";
            values[0, 1] = "R";
            values[0, 2] = "Terrain";
            values[0, 3] = "Elevation";
            values[0, 4] = "Water";
            values[0, 5] = "WaterFeatureName";

            for (int i = 0; i < cells.Count; i++)
            {
                HexCellRecord cell = cells[i];
                int row = i + 1;
                values[row, 0] = cell.Q;
                values[row, 1] = cell.R;
                values[row, 2] = cell.Terrain;
                values[row, 3] = cell.Elevation;
                values[row, 4] = cell.Water;
                values[row, 5] = cell.WaterFeatureName;
            }

            ExcelWorksheetShim.SetValues(sheet, values);
        }

        private void WriteCellsSheet(IXLWorksheet sheet, List<HexCellRecord> cells)
        {
            WriteCellsSheet(new ExcelWorksheetShim(sheet), cells);
        }

        private void WriteEdgesSheet(ExcelWorksheetShim sheet, List<MapEdgeRecord> edges)
        {
            edges = edges ?? new List<MapEdgeRecord>();
            int rows = Math.Max(1, edges.Count + 1);
            object[,] values = new object[rows, 5];
            values[0, 0] = "AQ";
            values[0, 1] = "AR";
            values[0, 2] = "BQ";
            values[0, 3] = "BR";
            values[0, 4] = "FeatureName";

            for (int i = 0; i < edges.Count; i++)
            {
                MapEdgeRecord edge = edges[i];
                int row = i + 1;
                values[row, 0] = edge.AQ;
                values[row, 1] = edge.AR;
                values[row, 2] = edge.BQ;
                values[row, 3] = edge.BR;
                values[row, 4] = edge.FeatureName;
            }

            ExcelWorksheetShim.SetValues(sheet, values);
        }

        private void WriteEdgesSheet(IXLWorksheet sheet, List<MapEdgeRecord> edges)
        {
            WriteEdgesSheet(new ExcelWorksheetShim(sheet), edges);
        }

        private List<MapEdgeRecord> ReadEdgesSheet(IXLWorksheet sheet, string kind)
        {
            return ReadEdgesSheet(new ExcelWorksheetShim(sheet), kind);
        }

        private List<MapEdgeRecord> ReadEdgesSheet(ExcelWorksheetShim sheet, string kind)
        {
            List<MapEdgeRecord> edges = new List<MapEdgeRecord>();
            object[,] values = ExcelWorksheetShim.GetUsedRangeValues(sheet);
            int rows = ExcelWorksheetShim.GetArrayRows(values);
            int columns = ExcelWorksheetShim.GetArrayColumns(values);
            for (int row = 2; row <= rows; row++)
            {
                edges.Add(new MapEdgeRecord
                {
                    AQ = ExcelWorksheetShim.ReadArrayInt(values, row, 1, 0),
                    AR = ExcelWorksheetShim.ReadArrayInt(values, row, 2, 0),
                    BQ = ExcelWorksheetShim.ReadArrayInt(values, row, 3, 0),
                    BR = ExcelWorksheetShim.ReadArrayInt(values, row, 4, 0),
                    Kind = kind,
                    FeatureName = columns >= 5 ? ExcelWorksheetShim.ReadArrayString(values, row, 5) : ""
                });
            }
            return edges;
        }

    }
}
