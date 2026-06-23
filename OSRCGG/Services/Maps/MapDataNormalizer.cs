using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    // Единая точка нормализации данных карты перед сохранением, импортом и расчётами.
    // Это защищает UI от знания всех старых синонимов и допустимых значений.
    internal static class MapDataNormalizer
    {
        public static string CellKey(int q, int r)
        {
            return q + "," + r;
        }

        public static string SettlementRace(string race)
        {
            if (string.IsNullOrWhiteSpace(race)) return "Human";

            string value = race.Trim();
            // Старые файлы могли хранить прилагательные или множественное число вместо ключа расы.
            if (string.Equals(value, "Dwarven", StringComparison.OrdinalIgnoreCase)) return "Dwarf";
            if (string.Equals(value, "Elven", StringComparison.OrdinalIgnoreCase)) return "Elf";
            if (string.Equals(value, "Beastmen", StringComparison.OrdinalIgnoreCase)) return "Beastman";
            if (string.Equals(value, "Beastman", StringComparison.OrdinalIgnoreCase)) return "Beastman";
            if (string.Equals(value, "Orc", StringComparison.OrdinalIgnoreCase)) return "Orc";
            if (string.Equals(value, "Dwarf", StringComparison.OrdinalIgnoreCase)) return "Dwarf";
            if (string.Equals(value, "Elf", StringComparison.OrdinalIgnoreCase)) return "Elf";
            return "Human";
        }

        public static string TerrainKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Grasslands";
            string normalized = value.Trim().Replace(" ", "");
            if (string.Equals(normalized, "DeepForest", StringComparison.OrdinalIgnoreCase)) return "DeepForest";
            if (string.Equals(normalized, "DeepTaiga", StringComparison.OrdinalIgnoreCase)) return "DeepTaiga";

            string[] terrainKeys =
            {
                "Rainforest", "Savanna", "Desert", "Steppe", "Scrub", "Grasslands",
                "Forest", "Taiga", "Tundra", "Marsh"
            };
            return terrainKeys.FirstOrDefault(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase)) ?? "Grasslands";
        }

        public static string ElevationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Plains";
            string normalized = value.Trim();
            if (string.Equals(normalized, "Hills", StringComparison.OrdinalIgnoreCase)) return "Hills";
            if (string.Equals(normalized, "Mountains", StringComparison.OrdinalIgnoreCase)) return "Mountains";
            return "Plains";
        }

        public static string WaterKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "None";
            string normalized = value.Trim();
            if (string.Equals(normalized, "Ocean", StringComparison.OrdinalIgnoreCase)) return "Ocean";
            if (string.Equals(normalized, "Sea", StringComparison.OrdinalIgnoreCase)) return "Sea";
            if (string.Equals(normalized, "Lake", StringComparison.OrdinalIgnoreCase)) return "Lake";
            return "None";
        }

        public static void NormalizeSettlementMetadata(MapSettlementRecord settlement)
        {
            if (settlement == null) return;
            if (string.IsNullOrWhiteSpace(settlement.Id)) settlement.Id = Guid.NewGuid().ToString("N");
            settlement.Race = SettlementRace(settlement.Race);
            if (settlement.MarketClass < 1 || settlement.MarketClass > 6) settlement.MarketClass = 6;
            // Массивы спроса всегда приводятся к длине ACKS, чтобы расчёты не зависели от источника данных.
            settlement.BaseDemands = MapDemandService.NormalizeDemandArray(settlement.BaseDemands);
            settlement.CurrentDemands = MapDemandService.NormalizeDemandArray(settlement.CurrentDemands);
        }

        public static void NormalizeMapShell(HexMapRecord map)
        {
            if (map == null) return;

            if (map.Cells == null) map.Cells = new List<HexCellRecord>();
            if (map.Settlements == null) map.Settlements = new List<MapSettlementRecord>();
            if (map.Roads == null) map.Roads = new List<MapEdgeRecord>();
            if (map.Rivers == null) map.Rivers = new List<MapEdgeRecord>();
            if (map.Domains == null) map.Domains = new List<DomainRecord>();
            if (map.Realms == null) map.Realms = new List<RealmRecord>();
            if (map.VassalLinks == null) map.VassalLinks = new List<VassalLinkRecord>();
            if (map.Features == null) map.Features = new List<HexFeatureRecord>();
            if (map.Dungeons == null) map.Dungeons = new List<DungeonRecord>();

            foreach (HexCellRecord cell in map.Cells)
            {
                if (cell == null) continue;
                cell.Terrain = TerrainKey(cell.Terrain);
                cell.Elevation = ElevationKey(cell.Elevation);
                cell.Water = WaterKey(cell.Water);
                if (cell.WaterFeatureName == null) cell.WaterFeatureName = "";
            }

            foreach (MapSettlementRecord settlement in map.Settlements)
            {
                NormalizeSettlementMetadata(settlement);
            }

            foreach (HexFeatureRecord feature in map.Features)
            {
                NormalizeHexFeature(feature);
            }

            foreach (DungeonRecord dungeon in map.Dungeons)
            {
                NormalizeDungeon(dungeon);
            }
        }

        public static void NormalizeHexFeature(HexFeatureRecord feature)
        {
            if (feature == null) return;
            if (string.IsNullOrWhiteSpace(feature.Id)) feature.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(feature.Kind)) feature.Kind = "Natural";
            if (feature.Subtype == null) feature.Subtype = "";
            if (feature.IconKey == null) feature.IconKey = "";
            if (feature.Description == null) feature.Description = "";
            if (feature.Severity == null) feature.Severity = "";
            if (feature.DungeonId == null) feature.DungeonId = "";
            if (feature.DungeonType == null) feature.DungeonType = "";
            if (feature.DungeonSize == null) feature.DungeonSize = "Standard";
            feature.DungeonLevel = DungeonCatalog.ClampDungeonLevel(feature.DungeonLevel);
        }

        public static void NormalizeDungeon(DungeonRecord dungeon)
        {
            if (dungeon == null) return;
            if (string.IsNullOrWhiteSpace(dungeon.Id)) dungeon.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(dungeon.DungeonType)) dungeon.DungeonType = "Natural caverns";
            if (string.IsNullOrWhiteSpace(dungeon.Size)) dungeon.Size = "Standard";
            dungeon.RecommendedLevel = DungeonCatalog.ClampDungeonLevel(dungeon.RecommendedLevel);
            if (dungeon.ChallengeTier == null) dungeon.ChallengeTier = "";
            if (dungeon.Notes == null) dungeon.Notes = "";
            if (dungeon.Levels == null) dungeon.Levels = new List<DungeonLevelRecord>();
            if (dungeon.WanderingEncounters == null) dungeon.WanderingEncounters = new List<DungeonEncounterRecord>();
            RenumberDungeonLevels(dungeon);
            foreach (DungeonLevelRecord level in dungeon.Levels)
            {
                if (level == null) continue;
                if (level.LevelNumber < 1) level.LevelNumber = 1;
                if (level.Width < 1) level.Width = 16;
                if (level.Height < 1) level.Height = 12;
                if (level.Rooms == null) level.Rooms = new List<DungeonRoomRecord>();
                if (level.Connections == null) level.Connections = new List<DungeonConnectionRecord>();
                if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();
                foreach (DungeonRoomRecord room in level.Rooms)
                {
                    if (room == null) continue;
                    if (string.IsNullOrWhiteSpace(room.Id)) room.Id = Guid.NewGuid().ToString("N");
                    if (room.Kind == null) room.Kind = "Empty";
                    if (string.IsNullOrWhiteSpace(room.Shape)) room.Shape = "Rectangle";
                    if (room.Title == null) room.Title = "";
                    if (room.Details == null) room.Details = "";
                    if (room.Monster == null) room.Monster = "";
                    if (room.Treasure == null) room.Treasure = "";
                    if (room.Trap == null) room.Trap = "";
                    if (room.UniqueFeature == null) room.UniqueFeature = "";
                    if (room.Width < 1) room.Width = 2;
                    if (room.Height < 1) room.Height = 2;
                }

                foreach (DungeonConnectionRecord connection in level.Connections)
                {
                    if (connection == null) continue;
                    if (connection.FromRoomId == null) connection.FromRoomId = "";
                    if (connection.ToRoomId == null) connection.ToRoomId = "";
                    if (string.IsNullOrWhiteSpace(connection.Kind)) connection.Kind = "Corridor";
                    if (connection.PassageWidth < 1) connection.PassageWidth = 1;
                    if (connection.PassageWidth > 4) connection.PassageWidth = 4;
                    if (connection.DoorKind == null) connection.DoorKind = "";
                    if (connection.PathPoints == null) connection.PathPoints = new List<DungeonPathPointRecord>();
                    connection.PathPoints.RemoveAll(p => p == null);
                    foreach (DungeonPathPointRecord point in connection.PathPoints)
                    {
                        if (double.IsNaN(point.X) || double.IsInfinity(point.X)) point.X = 0;
                        if (double.IsNaN(point.Y) || double.IsInfinity(point.Y)) point.Y = 0;
                    }
                    NormalizeDungeonConnectionPath(level, connection);

                    string legacyDoorKind = IsDungeonDoorKind(connection.Kind)
                        ? connection.Kind
                        : connection.DoorKind;
                    if (!string.IsNullOrWhiteSpace(legacyDoorKind))
                    {
                        AddDoorFromLegacyConnection(level, connection, legacyDoorKind);
                        connection.DoorKind = "";
                    }

                    if (IsDungeonDoorKind(connection.Kind))
                    {
                        connection.Kind = "Passage";
                    }
                }

                foreach (DungeonDoorRecord door in level.Doors)
                {
                    NormalizeDungeonDoor(level, door);
                    SnapDungeonDoorAwayFromRoomInterior(level, door);
                }
            }
        }

        private static void NormalizeDungeonConnectionPath(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || connection.PathPoints == null || connection.PathPoints.Count == 0) return;
            if (IsDungeonOverpass(connection))
            {
                connection.PathPoints = CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
                return;
            }

            DungeonRoomRecord from = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r != null && r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r != null && r.Id == connection.ToRoomId);
            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            if (from == null || to == null)
            {
                connection.PathPoints = CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
                return;
            }
            if (connection.PathPoints.Count <= 2)
            {
                connection.PathPoints = CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
                return;
            }

            List<DungeonPathPointRecord> directPath;
            if (TryBuildNearStraightDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            List<DungeonPathPointRecord> normalized = new List<DungeonPathPointRecord>();
            DungeonPathPointRecord first = connection.PathPoints.First();
            DungeonPathPointRecord last = connection.PathPoints.Last();
            DungeonPathPointRecord firstTarget = connection.PathPoints.Count > 1 ? connection.PathPoints[1] : first;
            DungeonPathPointRecord lastTarget = connection.PathPoints.Count > 1 ? connection.PathPoints[connection.PathPoints.Count - 2] : last;
            normalized.Add(RoomEdgePathPoint(from, firstTarget.X, firstTarget.Y, passageWidth));
            for (int i = 1; i < connection.PathPoints.Count - 1; i++)
            {
                DungeonPathPointRecord point = connection.PathPoints[i];
                if (point == null) continue;
                if (IsDungeonPointInsideAnyRoomInterior(level, point.X, point.Y)) continue;
                normalized.Add(CloneDungeonPathPoint(point));
            }

            normalized.Add(RoomEdgePathPoint(to, lastTarget.X, lastTarget.Y, passageWidth));
            connection.PathPoints = NormalizeDungeonPathEntrances(normalized, from, to, passageWidth);
        }

        private static List<DungeonPathPointRecord> NormalizeDungeonPathEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            points = CleanDungeonPathPoints((points ?? new List<DungeonPathPointRecord>())
                .Where(point => point != null)
                .Select(CloneDungeonPathPoint)
                .ToList());
            if (points.Count < 2 || from == null || to == null) return points;

            DungeonPathPointRecord startTarget = points.Count > 1 ? points[1] : new DungeonPathPointRecord { X = RoomCenterX(to), Y = RoomCenterY(to) };
            points[0] = RoomEdgePathPoint(from, startTarget.X, startTarget.Y, passageWidth);
            if (!DungeonRoomEntranceIsValid(from, points[0], points[1], passageWidth))
            {
                points.Insert(1, RoomOutsidePathPoint(from, startTarget.X, startTarget.Y, passageWidth));
            }

            int lastIndex = points.Count - 1;
            DungeonPathPointRecord endTarget = points.Count > 1 ? points[lastIndex - 1] : new DungeonPathPointRecord { X = RoomCenterX(from), Y = RoomCenterY(from) };
            points[lastIndex] = RoomEdgePathPoint(to, endTarget.X, endTarget.Y, passageWidth);
            if (!DungeonRoomEntranceIsValid(to, points[lastIndex], points[lastIndex - 1], passageWidth))
            {
                points.Insert(lastIndex, RoomOutsidePathPoint(to, endTarget.X, endTarget.Y, passageWidth));
            }

            return CleanDungeonPathPoints(points);
        }

        private static bool TryBuildNearStraightDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;
            DungeonPathPointRecord start = RoomEdgePathPoint(from, RoomCenterX(to), RoomCenterY(to), passageWidth);
            DungeonPathPointRecord end = RoomEdgePathPoint(to, RoomCenterX(from), RoomCenterY(from), passageWidth);
            if (!ShouldPreferStraightDungeonPath(start, end)) return false;

            List<DungeonPathPointRecord> candidate = CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
            if (candidate.Count != 2) return false;
            if (!DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            path = candidate;
            return true;
        }

        private static bool ShouldPreferStraightDungeonPath(DungeonPathPointRecord start, DungeonPathPointRecord end)
        {
            if (start == null || end == null) return false;
            const double alignmentTolerance = 0.45;
            return Math.Abs(start.X - end.X) <= alignmentTolerance
                || Math.Abs(start.Y - end.Y) <= alignmentTolerance;
        }

        private static bool DungeonPathHasValidRoomEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            if (points == null || points.Count < 2) return false;
            return DungeonRoomEntranceIsValid(from, points[0], points[1], passageWidth)
                && DungeonRoomEntranceIsValid(to, points[points.Count - 1], points[points.Count - 2], passageWidth);
        }

        private static bool DungeonRoomEntranceIsValid(DungeonRoomRecord room, DungeonPathPointRecord edge, DungeonPathPointRecord outside, int passageWidth)
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

        private static bool DungeonPathHitsRoom(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == fromRoomId || room.Id == toRoomId) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(points[i - 1], points[i], room, margin)) return true;
                }
            }

            return false;
        }

        private static bool DungeonPathHitsLinkedRoomInterior(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r != null && r.Id == fromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r != null && r.Id == toRoomId);
            for (int i = 1; i < points.Count; i++)
            {
                if (from != null && DungeonGeometry.SegmentCrossesRoomInterior(points[i - 1], points[i], from)) return true;
                if (to != null && DungeonGeometry.SegmentCrossesRoomInterior(points[i - 1], points[i], to)) return true;
            }

            return false;
        }

        private static bool IsDungeonOverpass(DungeonConnectionRecord connection)
        {
            return connection != null && string.Equals(connection.Kind, "Overpass", StringComparison.OrdinalIgnoreCase);
        }

        private static DungeonPathPointRecord RoomEdgePathPoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth)
        {
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, targetX, targetY, passageWidth, out x, out y, out orientation);
            return new DungeonPathPointRecord { X = x, Y = y };
        }

        private static DungeonPathPointRecord RoomOutsidePathPoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth)
        {
            return DungeonGeometry.OutsidePassagePoint(room, targetX, targetY, passageWidth, 0.85, false);
        }

        private static bool IsDungeonPointInsideAnyRoomInterior(DungeonLevelRecord level, double x, double y)
        {
            if (level == null || level.Rooms == null) return false;
            return level.Rooms.Any(room => room != null && DungeonGeometry.IsPointInsideRoomInterior(room, x, y, 0.06));
        }

        private static DungeonPathPointRecord CloneDungeonPathPoint(DungeonPathPointRecord point)
        {
            return point == null
                ? new DungeonPathPointRecord()
                : new DungeonPathPointRecord { X = point.X, Y = point.Y };
        }

        private static List<DungeonPathPointRecord> CleanDungeonPathPoints(List<DungeonPathPointRecord> points)
        {
            List<DungeonPathPointRecord> cleaned = new List<DungeonPathPointRecord>();
            foreach (DungeonPathPointRecord point in points ?? new List<DungeonPathPointRecord>())
            {
                if (point == null) continue;
                if (cleaned.Count > 0 && AlmostSame(cleaned[cleaned.Count - 1].X, point.X) && AlmostSame(cleaned[cleaned.Count - 1].Y, point.Y)) continue;
                cleaned.Add(point);
            }

            for (int i = cleaned.Count - 2; i >= 1; i--)
            {
                DungeonPathPointRecord previous = cleaned[i - 1];
                DungeonPathPointRecord current = cleaned[i];
                DungeonPathPointRecord next = cleaned[i + 1];
                bool sameX = AlmostSame(previous.X, current.X) && AlmostSame(current.X, next.X);
                bool sameY = AlmostSame(previous.Y, current.Y) && AlmostSame(current.Y, next.Y);
                if (sameX || sameY) cleaned.RemoveAt(i);
            }

            return cleaned;
        }

        private static bool AlmostSame(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        private static void RenumberDungeonLevels(DungeonRecord dungeon)
        {
            if (dungeon == null || dungeon.Levels == null) return;

            var orderedLevels = dungeon.Levels
                .Where(level => level != null)
                .Select((level, index) => new
                {
                    Level = level,
                    OriginalIndex = index,
                    OldNumber = level.LevelNumber < 1 ? index + 1 : level.LevelNumber
                })
                .OrderBy(item => item.OldNumber)
                .ThenBy(item => item.OriginalIndex)
                .ToList();

            Dictionary<int, int> oldToNew = new Dictionary<int, int>();
            for (int i = 0; i < orderedLevels.Count; i++)
            {
                int newNumber = i + 1;
                if (!oldToNew.ContainsKey(orderedLevels[i].OldNumber))
                {
                    oldToNew[orderedLevels[i].OldNumber] = newNumber;
                }

                DungeonLevelRecord level = orderedLevels[i].Level;
                level.LevelNumber = newNumber;
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room != null) room.LevelNumber = newNumber;
                }

                foreach (DungeonDoorRecord door in level.Doors ?? new List<DungeonDoorRecord>())
                {
                    if (door != null) door.LevelNumber = newNumber;
                }
            }

            dungeon.Levels = orderedLevels.Select(item => item.Level).ToList();
            foreach (DungeonEncounterRecord encounter in dungeon.WanderingEncounters ?? new List<DungeonEncounterRecord>())
            {
                if (encounter == null) continue;
                int oldNumber = encounter.DungeonLevel < 1 ? 1 : encounter.DungeonLevel;
                int newNumber;
                if (oldToNew.TryGetValue(oldNumber, out newNumber))
                {
                    encounter.DungeonLevel = newNumber;
                }
                else
                {
                    encounter.DungeonLevel = Math.Max(1, Math.Min(Math.Max(1, dungeon.Levels.Count), oldNumber));
                }

                if (encounter.MonsterLevel < 1) encounter.MonsterLevel = 1;
                if (encounter.MonsterLevel > 6) encounter.MonsterLevel = 6;
                if (encounter.Monster == null) encounter.Monster = "";
                if (encounter.CountExpression == null) encounter.CountExpression = "";
                if (encounter.Notes == null) encounter.Notes = "";
            }
        }

        private static bool IsDungeonDoorKind(string value)
        {
            return string.Equals(value, "Door", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "SecretDoor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "SecretPassage", StringComparison.OrdinalIgnoreCase);
        }

        private static void NormalizeDungeonDoor(DungeonLevelRecord level, DungeonDoorRecord door)
        {
            if (door == null) return;
            if (string.IsNullOrWhiteSpace(door.Id)) door.Id = Guid.NewGuid().ToString("N");
            door.LevelNumber = level == null || level.LevelNumber < 1 ? 1 : level.LevelNumber;
            if (!IsDungeonDoorKind(door.Kind)) door.Kind = "Door";
            if (!string.Equals(door.Orientation, "Horizontal", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(door.Orientation, "Vertical", StringComparison.OrdinalIgnoreCase)
                && !IsDungeonDoorAngle(door.Orientation))
            {
                door.Orientation = "Vertical";
            }

            if (door.FromRoomId == null) door.FromRoomId = "";
            if (door.ToRoomId == null) door.ToRoomId = "";
            if (door.Notes == null) door.Notes = "";
        }

        private static bool IsDungeonDoorAngle(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Trim().StartsWith("Angle:", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddDoorFromLegacyConnection(DungeonLevelRecord level, DungeonConnectionRecord connection, string kind)
        {
            if (level == null || connection == null || !IsDungeonDoorKind(kind)) return;
            if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();
            if (level.Doors.Any(d => d != null
                && string.Equals(d.FromRoomId, connection.FromRoomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(d.ToRoomId, connection.ToRoomId, StringComparison.OrdinalIgnoreCase)
                && IsDungeonDoorKind(d.Kind)))
            {
                return;
            }

            Dictionary<string, DungeonRoomRecord> rooms = (level.Rooms ?? new List<DungeonRoomRecord>())
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
                .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            DungeonRoomRecord from;
            DungeonRoomRecord to;
            double x = Math.Max(0.5, level.Width / 2.0);
            double y = Math.Max(0.5, level.Height / 2.0);
            string orientation = "Vertical";
            if (rooms.TryGetValue(connection.FromRoomId ?? "", out from)
                && rooms.TryGetValue(connection.ToRoomId ?? "", out to))
            {
                DungeonPathPointRecord firstPathPoint = connection.PathPoints == null ? null : connection.PathPoints.FirstOrDefault();
                if (firstPathPoint != null)
                {
                    FindDungeonDoorPointOnRoomEdge(from, firstPathPoint.X, firstPathPoint.Y, out x, out y, out orientation);
                }
                else if (!TryFindSharedDungeonDoorPoint(from, to, out x, out y, out orientation))
                {
                    FindDungeonDoorPointOnRoomEdge(from, RoomCenterX(to), RoomCenterY(to), out x, out y, out orientation);
                }
            }

            level.Doors.Add(new DungeonDoorRecord
            {
                LevelNumber = level.LevelNumber,
                X = x,
                Y = y,
                Kind = kind,
                Orientation = orientation,
                FromRoomId = connection.FromRoomId,
                ToRoomId = connection.ToRoomId
            });
        }

        private static void SnapDungeonDoorAwayFromRoomInterior(DungeonLevelRecord level, DungeonDoorRecord door)
        {
            if (level == null || door == null || level.Rooms == null || level.Rooms.Count == 0) return;
            Dictionary<string, DungeonRoomRecord> rooms = level.Rooms
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
                .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            DungeonRoomRecord room;
            DungeonRoomRecord other;

            if (!string.IsNullOrWhiteSpace(door.FromRoomId)
                && rooms.TryGetValue(door.FromRoomId, out room)
                && IsDungeonDoorInsideRoomInterior(door, room))
            {
                if (rooms.TryGetValue(door.ToRoomId ?? "", out other))
                {
                    MoveDungeonDoorToRoomEdge(door, room, other);
                    return;
                }

                MoveDungeonDoorToNearestRoomWall(door, room);
                return;
            }

            if (!string.IsNullOrWhiteSpace(door.ToRoomId)
                && rooms.TryGetValue(door.ToRoomId, out room)
                && IsDungeonDoorInsideRoomInterior(door, room))
            {
                if (rooms.TryGetValue(door.FromRoomId ?? "", out other))
                {
                    MoveDungeonDoorToRoomEdge(door, room, other);
                    return;
                }

                MoveDungeonDoorToNearestRoomWall(door, room);
            }
        }

        private static bool IsDungeonDoorInsideRoomInterior(DungeonDoorRecord door, DungeonRoomRecord room)
        {
            return DungeonGeometry.IsPointInsideRoomInterior(room, door.X, door.Y, 0.001);
        }

        private static void MoveDungeonDoorToRoomEdge(DungeonDoorRecord door, DungeonRoomRecord room, DungeonRoomRecord target)
        {
            double x;
            double y;
            string orientation;
            FindDungeonDoorPointOnRoomEdge(room, RoomCenterX(target), RoomCenterY(target), out x, out y, out orientation);
            door.X = x;
            door.Y = y;
            door.Orientation = orientation;
        }

        private static void MoveDungeonDoorToNearestRoomWall(DungeonDoorRecord door, DungeonRoomRecord room)
        {
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomEdgePoint(room, door.X, door.Y, out x, out y, out orientation);
            door.X = x;
            door.Y = y;
            door.Orientation = orientation;
        }

        private static double RoomCenterX(DungeonRoomRecord room)
        {
            return room.X + room.Width / 2.0;
        }

        private static double RoomCenterY(DungeonRoomRecord room)
        {
            return room.Y + room.Height / 2.0;
        }

        private static bool TryFindSharedDungeonDoorPoint(DungeonRoomRecord from, DungeonRoomRecord to, out double x, out double y, out string orientation)
        {
            x = 0;
            y = 0;
            orientation = "Vertical";
            if (from == null || to == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(from) || !DungeonGeometry.UsesBoxEdges(to)) return false;

            if (from.X + from.Width == to.X || to.X + to.Width == from.X)
            {
                double top = Math.Max(from.Y, to.Y);
                double bottom = Math.Min(from.Y + from.Height, to.Y + to.Height);
                if (bottom <= top) return false;
                x = from.X + from.Width == to.X ? from.X + from.Width : from.X;
                y = (top + bottom) / 2.0;
                orientation = "Vertical";
                return true;
            }

            if (from.Y + from.Height == to.Y || to.Y + to.Height == from.Y)
            {
                double left = Math.Max(from.X, to.X);
                double right = Math.Min(from.X + from.Width, to.X + to.Width);
                if (right <= left) return false;
                x = (left + right) / 2.0;
                y = from.Y + from.Height == to.Y ? from.Y + from.Height : from.Y;
                orientation = "Horizontal";
                return true;
            }

            return false;
        }

        private static void FindDungeonDoorPointOnRoomEdge(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            DungeonGeometry.FindRoomEdgePoint(room, targetX, targetY, out x, out y, out orientation);
        }
    }
}
