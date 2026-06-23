using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    public static class DungeonGeometry
    {
        public static bool UsesBoxEdges(DungeonRoomRecord room)
        {
            if (room == null) return true;
            string shape = room.Shape ?? "";
            return string.Equals(shape, "Rectangle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(shape, "Narrow", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(shape);
        }

        public static DungeonPathPointRecord OutsideRoomPoint(DungeonRoomRecord room, double targetX, double targetY, double outsideOffset, bool snapToHalfCell)
        {
            double x;
            double y;
            string orientation;
            FindRoomEdgePoint(room, targetX, targetY, out x, out y, out orientation);

            double normalX;
            double normalY;
            RoomOutwardNormal(room, x, y, orientation, out normalX, out normalY);
            x += normalX * outsideOffset;
            y += normalY * outsideOffset;

            if (snapToHalfCell)
            {
                x = Math.Round(x * 2.0) / 2.0;
                y = Math.Round(y * 2.0) / 2.0;
            }

            return new DungeonPathPointRecord { X = x, Y = y };
        }

        public static DungeonPathPointRecord OutsidePassagePoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth, double outsideOffset, bool snapToHalfCell)
        {
            double x;
            double y;
            string orientation;
            FindRoomPassageEdgePoint(room, targetX, targetY, passageWidth, out x, out y, out orientation);

            double normalX;
            double normalY;
            RoomOutwardNormal(room, x, y, orientation, out normalX, out normalY);
            x += normalX * outsideOffset;
            y += normalY * outsideOffset;

            if (snapToHalfCell)
            {
                x = Math.Round(x * 2.0) / 2.0;
                y = Math.Round(y * 2.0) / 2.0;
            }

            return new DungeonPathPointRecord { X = x, Y = y };
        }

        public static void FindRoomEdgePoint(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            x = room == null ? 0 : room.X;
            y = room == null ? 0 : room.Y;
            orientation = "Horizontal";
            if (room == null) return;

            string shape = room.Shape ?? "";
            if (IsEllipseShape(shape))
            {
                FindEllipseEdgePoint(room, targetX, targetY, out x, out y, out orientation);
                return;
            }

            if (string.Equals(shape, "Cavern", StringComparison.OrdinalIgnoreCase))
            {
                if (FindPolygonEdgePoint(CavernPoints(room), RoomCenterX(room), RoomCenterY(room), targetX, targetY, out x, out y, out orientation))
                {
                    return;
                }
            }

            FindRectangleEdgePoint(room, targetX, targetY, out x, out y, out orientation);
        }

        public static void FindRoomPassageEdgePoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth, out double x, out double y, out string orientation)
        {
            FindRoomEdgePoint(room, targetX, targetY, out x, out y, out orientation);
            if (room == null) return;
            if (!UsesBoxEdges(room))
            {
                NudgeEdgePointOutward(room, ref x, ref y, orientation, 0.02);
                return;
            }

            double clearance = PassageCornerClearance(passageWidth);
            double left = room.X;
            double right = room.X + room.Width;
            double top = room.Y;
            double bottom = room.Y + room.Height;
            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase))
            {
                x = ClampToPassageWallSpan(x, left, right, clearance);
                y = y <= RoomCenterY(room) ? top : bottom;
                return;
            }

            if (string.Equals(orientation, "Vertical", StringComparison.OrdinalIgnoreCase))
            {
                x = x <= RoomCenterX(room) ? left : right;
                y = ClampToPassageWallSpan(y, top, bottom, clearance);
            }
        }

        public static bool TryFindAxisAlignedPassageEdgePoint(
            DungeonRoomRecord room,
            bool horizontalPassage,
            bool positiveSide,
            double fixedCoordinate,
            int passageWidth,
            bool useCornerClearance,
            out DungeonPathPointRecord point)
        {
            point = null;
            if (room == null) return false;

            string shape = room.Shape ?? "";
            if (UsesBoxEdges(room))
            {
                double clearance = PassageCornerClearance(passageWidth);
                point = horizontalPassage
                    ? new DungeonPathPointRecord
                    {
                        X = positiveSide ? room.X + room.Width : room.X,
                        Y = useCornerClearance
                            ? ClampToPassageWallSpan(fixedCoordinate, room.Y, room.Y + room.Height, clearance)
                            : Clamp(fixedCoordinate, room.Y, room.Y + room.Height)
                    }
                    : new DungeonPathPointRecord
                    {
                        X = useCornerClearance
                            ? ClampToPassageWallSpan(fixedCoordinate, room.X, room.X + room.Width, clearance)
                            : Clamp(fixedCoordinate, room.X, room.X + room.Width),
                        Y = positiveSide ? room.Y + room.Height : room.Y
                    };
                return true;
            }

            if (IsEllipseShape(shape))
            {
                double centerX = RoomCenterX(room);
                double centerY = RoomCenterY(room);
                double rx = Math.Max(0.05, room.Width / 2.0);
                double ry = Math.Max(0.05, room.Height / 2.0);
                if (horizontalPassage)
                {
                    double normalized = (fixedCoordinate - centerY) / ry;
                    if (Math.Abs(normalized) > 1.0) return false;
                    double x = centerX + (positiveSide ? 1 : -1) * rx * Math.Sqrt(Math.Max(0.0, 1.0 - normalized * normalized));
                    point = new DungeonPathPointRecord { X = x, Y = fixedCoordinate };
                    return true;
                }

                double normalizedX = (fixedCoordinate - centerX) / rx;
                if (Math.Abs(normalizedX) > 1.0) return false;
                double y = centerY + (positiveSide ? 1 : -1) * ry * Math.Sqrt(Math.Max(0.0, 1.0 - normalizedX * normalizedX));
                point = new DungeonPathPointRecord { X = fixedCoordinate, Y = y };
                return true;
            }

            if (string.Equals(shape, "Cavern", StringComparison.OrdinalIgnoreCase))
            {
                return TryFindPolygonAxisPoint(CavernPoints(room), horizontalPassage, positiveSide, fixedCoordinate, out point);
            }

            return false;
        }

        public static bool PathHasTinyStep(IList<DungeonPathPointRecord> points, double maximumMiddleLength)
        {
            if (points == null || points.Count < 4) return false;
            double limit = Math.Max(0.01, maximumMiddleLength);
            for (int i = 1; i < points.Count - 2; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                DungeonPathPointRecord c = points[i + 1];
                DungeonPathPointRecord d = points[i + 2];
                if (a == null || b == null || c == null || d == null) continue;

                double middleLength = Math.Abs(b.X - c.X) + Math.Abs(b.Y - c.Y);
                if (middleLength > limit) continue;

                bool horizontalStep = AlmostSame(a.Y, b.Y)
                    && AlmostSame(c.Y, d.Y)
                    && Math.Sign(b.X - a.X) == Math.Sign(d.X - c.X);
                bool verticalStep = AlmostSame(a.X, b.X)
                    && AlmostSame(c.X, d.X)
                    && Math.Sign(b.Y - a.Y) == Math.Sign(d.Y - c.Y);
                if (horizontalStep || verticalStep) return true;
            }

            return false;
        }

        public static bool PathHasTightSelfParallelRun(IList<DungeonPathPointRecord> points, double minimumSeparation, double minimumSharedRun)
        {
            if (points == null || points.Count < 4) return false;
            double separation = Math.Max(0.01, minimumSeparation);
            double sharedRun = Math.Max(0.01, minimumSharedRun);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                if (a == null || b == null) continue;
                for (int j = i + 2; j < points.Count; j++)
                {
                    DungeonPathPointRecord c = points[j - 1];
                    DungeonPathPointRecord d = points[j];
                    if (c == null || d == null) continue;
                    if (SegmentsRunTooCloseParallel(a, b, c, d, separation, sharedRun)) return true;
                }
            }

            return false;
        }

        public static double PathLength(IList<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count < 2) return double.MaxValue;
            double length = 0;
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord previous = points[i - 1];
                DungeonPathPointRecord current = points[i];
                if (previous == null || current == null) return double.MaxValue;
                length += Math.Abs(current.X - previous.X) + Math.Abs(current.Y - previous.Y);
            }

            return length;
        }

        public static double PathShapePenalty(IList<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count < 2) return 0;
            double penalty = 0;
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord previous = points[i - 1];
                DungeonPathPointRecord current = points[i];
                if (previous == null || current == null) continue;
                double length = Math.Abs(current.X - previous.X) + Math.Abs(current.Y - previous.Y);
                if (length < 1.25) penalty += (1.25 - length) * 10.0;
            }

            for (int i = 1; i < points.Count - 1; i++)
            {
                DungeonPathPointRecord previous = points[i - 1];
                DungeonPathPointRecord current = points[i];
                DungeonPathPointRecord next = points[i + 1];
                if (previous == null || current == null || next == null) continue;
                if (AlmostSame(previous.X, current.X) && AlmostSame(current.X, next.X)) continue;
                if (AlmostSame(previous.Y, current.Y) && AlmostSame(current.Y, next.Y)) continue;
                penalty += 3.0;
            }

            return penalty;
        }

        public static double PathInteractionPenalty(IList<DungeonPathPointRecord> points, IEnumerable<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (points == null || points.Count < 2 || existingPaths == null) return 0;
            double cost = 0;
            foreach (IList<DungeonPathPointRecord> existing in existingPaths)
            {
                if (existing == null || existing.Count < 2) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    DungeonPathPointRecord a = points[i - 1];
                    DungeonPathPointRecord b = points[i];
                    if (a == null || b == null) continue;
                    for (int j = 1; j < existing.Count; j++)
                    {
                        DungeonPathPointRecord c = existing[j - 1];
                        DungeonPathPointRecord d = existing[j];
                        if (SegmentsOverlap(a, b, c, d))
                        {
                            cost += 4;
                        }
                        else if (SegmentsRunTooCloseParallel(a, b, c, d, 1.05, 1.25, true))
                        {
                            cost += 90;
                        }
                        else if (SegmentsCross(a, b, c, d))
                        {
                            cost += 12;
                        }
                    }
                }
            }

            return cost;
        }

        public static bool PathRunsTooCloseToAny(
            IList<DungeonPathPointRecord> points,
            IEnumerable<IList<DungeonPathPointRecord>> existingPaths,
            double minimumSeparation,
            double minimumSharedRun,
            bool ignoreSameLine)
        {
            if (points == null || points.Count < 2 || existingPaths == null) return false;
            foreach (IList<DungeonPathPointRecord> existing in existingPaths)
            {
                if (existing == null || existing.Count < 2) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    DungeonPathPointRecord a = points[i - 1];
                    DungeonPathPointRecord b = points[i];
                    if (a == null || b == null) continue;
                    for (int j = 1; j < existing.Count; j++)
                    {
                        if (SegmentsRunTooCloseParallel(a, b, existing[j - 1], existing[j], minimumSeparation, minimumSharedRun, ignoreSameLine))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool SegmentsRunTooCloseParallel(
            DungeonPathPointRecord a,
            DungeonPathPointRecord b,
            DungeonPathPointRecord c,
            DungeonPathPointRecord d,
            double minimumSeparation,
            double minimumSharedRun)
        {
            return SegmentsRunTooCloseParallel(a, b, c, d, minimumSeparation, minimumSharedRun, false);
        }

        public static bool SegmentsRunTooCloseParallel(
            DungeonPathPointRecord a,
            DungeonPathPointRecord b,
            DungeonPathPointRecord c,
            DungeonPathPointRecord d,
            double minimumSeparation,
            double minimumSharedRun,
            bool ignoreSameLine)
        {
            if (a == null || b == null || c == null || d == null) return false;
            double separation = Math.Max(0.01, minimumSeparation);
            double sharedRun = Math.Max(0.01, minimumSharedRun);
            bool firstVertical = AlmostSame(a.X, b.X);
            bool firstHorizontal = AlmostSame(a.Y, b.Y);
            bool secondVertical = AlmostSame(c.X, d.X);
            bool secondHorizontal = AlmostSame(c.Y, d.Y);
            if (firstVertical && secondVertical)
            {
                if (ignoreSameLine && AlmostSame(a.X, c.X)) return false;
                if (Math.Abs(a.X - c.X) > separation) return false;
                return RangeOverlapLength(a.Y, b.Y, c.Y, d.Y) >= sharedRun;
            }

            if (firstHorizontal && secondHorizontal)
            {
                if (ignoreSameLine && AlmostSame(a.Y, c.Y)) return false;
                if (Math.Abs(a.Y - c.Y) > separation) return false;
                return RangeOverlapLength(a.X, b.X, c.X, d.X) >= sharedRun;
            }

            return false;
        }

        public static bool SegmentsOverlap(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonPathPointRecord c, DungeonPathPointRecord d)
        {
            if (a == null || b == null || c == null || d == null) return false;
            if (AlmostSame(a.X, b.X) && AlmostSame(c.X, d.X) && AlmostSame(a.X, c.X))
            {
                return RangesOverlap(a.Y, b.Y, c.Y, d.Y);
            }

            if (AlmostSame(a.Y, b.Y) && AlmostSame(c.Y, d.Y) && AlmostSame(a.Y, c.Y))
            {
                return RangesOverlap(a.X, b.X, c.X, d.X);
            }

            return false;
        }

        public static bool SegmentsCross(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonPathPointRecord c, DungeonPathPointRecord d)
        {
            if (a == null || b == null || c == null || d == null) return false;
            bool firstVertical = AlmostSame(a.X, b.X);
            bool firstHorizontal = AlmostSame(a.Y, b.Y);
            bool secondVertical = AlmostSame(c.X, d.X);
            bool secondHorizontal = AlmostSame(c.Y, d.Y);
            if (firstVertical && secondHorizontal)
            {
                return Between(c.X, d.X, a.X) && Between(a.Y, b.Y, c.Y);
            }

            if (firstHorizontal && secondVertical)
            {
                return Between(a.X, b.X, c.X) && Between(c.Y, d.Y, a.Y);
            }

            return false;
        }

        private static double RangeOverlapLength(double a, double b, double c, double d)
        {
            double minA = Math.Min(a, b);
            double maxA = Math.Max(a, b);
            double minB = Math.Min(c, d);
            double maxB = Math.Max(c, d);
            return Math.Min(maxA, maxB) - Math.Max(minA, minB);
        }

        private static bool RangesOverlap(double a, double b, double c, double d)
        {
            return RangeOverlapLength(a, b, c, d) > 0.05;
        }

        private static bool Between(double a, double b, double value)
        {
            return value > Math.Min(a, b) + 0.05 && value < Math.Max(a, b) - 0.05;
        }

        private static bool AlmostSame(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        private static void NudgeEdgePointOutward(DungeonRoomRecord room, ref double x, ref double y, string orientation, double distance)
        {
            double normalX;
            double normalY;
            RoomOutwardNormal(room, x, y, orientation, out normalX, out normalY);
            x += normalX * distance;
            y += normalY * distance;
        }

        public static bool SegmentCrossesRoomInterior(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonRoomRecord room)
        {
            if (a == null || b == null || room == null) return false;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            int steps = Math.Max(2, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) * 8.0));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double x = a.X + dx * t;
                double y = a.Y + dy * t;
                if (IsPointInsideRoomInterior(room, x, y, 0.05)) return true;
            }

            return false;
        }

        public static bool SegmentCrossesRoomBuffer(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonRoomRecord room, double margin)
        {
            if (a == null || b == null || room == null) return false;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            int steps = Math.Max(2, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) * 8.0));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double x = a.X + dx * t;
                double y = a.Y + dy * t;
                if (IsPointInsideRoomBuffer(room, x, y, margin)) return true;
            }

            return false;
        }

        public static bool SegmentRunsAlongRoomBoundary(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonRoomRecord room, double tolerance)
        {
            if (a == null || b == null || room == null || !UsesBoxEdges(room)) return false;
            double left = room.X;
            double right = room.X + room.Width;
            double top = room.Y;
            double bottom = room.Y + room.Height;
            double overlapTolerance = Math.Max(0.05, tolerance);

            if (Math.Abs(a.X - b.X) <= tolerance)
            {
                double x = (a.X + b.X) / 2.0;
                if (Math.Abs(x - left) > tolerance && Math.Abs(x - right) > tolerance) return false;
                double minY = Math.Min(a.Y, b.Y);
                double maxY = Math.Max(a.Y, b.Y);
                return Math.Min(maxY, bottom - overlapTolerance) - Math.Max(minY, top + overlapTolerance) > overlapTolerance;
            }

            if (Math.Abs(a.Y - b.Y) <= tolerance)
            {
                double y = (a.Y + b.Y) / 2.0;
                if (Math.Abs(y - top) > tolerance && Math.Abs(y - bottom) > tolerance) return false;
                double minX = Math.Min(a.X, b.X);
                double maxX = Math.Max(a.X, b.X);
                return Math.Min(maxX, right - overlapTolerance) - Math.Max(minX, left + overlapTolerance) > overlapTolerance;
            }

            return false;
        }

        public static bool IsPointInsideRoomBuffer(DungeonRoomRecord room, double x, double y, double margin)
        {
            if (room == null) return false;
            string shape = room.Shape ?? "";
            double left = room.X - margin;
            double right = room.X + room.Width + margin;
            double top = room.Y - margin;
            double bottom = room.Y + room.Height + margin;

            if (IsEllipseShape(shape))
            {
                double cx = RoomCenterX(room);
                double cy = RoomCenterY(room);
                double rx = Math.Max(0.05, room.Width / 2.0 + margin);
                double ry = Math.Max(0.05, room.Height / 2.0 + margin);
                double nx = (x - cx) / rx;
                double ny = (y - cy) / ry;
                return nx * nx + ny * ny < 1.0;
            }

            if (string.Equals(shape, "Cavern", StringComparison.OrdinalIgnoreCase))
            {
                return x > left && x < right && y > top && y < bottom;
            }

            return x > left && x < right && y > top && y < bottom;
        }

        public static double PassageHalfWidthCells(int passageWidth)
        {
            int width = Math.Max(1, Math.Min(4, passageWidth));
            return 0.21 + width * 0.11;
        }

        public static bool IsPointInsideRoomInterior(DungeonRoomRecord room, double x, double y, double margin)
        {
            if (room == null) return false;
            string shape = room.Shape ?? "";
            double left = room.X + margin;
            double right = room.X + room.Width - margin;
            double top = room.Y + margin;
            double bottom = room.Y + room.Height - margin;
            if (right <= left || bottom <= top) return false;

            if (IsEllipseShape(shape))
            {
                double cx = RoomCenterX(room);
                double cy = RoomCenterY(room);
                double rx = Math.Max(0.05, room.Width / 2.0 - margin);
                double ry = Math.Max(0.05, room.Height / 2.0 - margin);
                double nx = (x - cx) / rx;
                double ny = (y - cy) / ry;
                return nx * nx + ny * ny < 1.0;
            }

            if (string.Equals(shape, "Cavern", StringComparison.OrdinalIgnoreCase))
            {
                if (x <= left || x >= right || y <= top || y >= bottom) return false;
                List<DungeonPathPointRecord> cavern = CavernPoints(room);
                if (!PointInPolygon(cavern, x, y)) return false;
                return DistanceToPolygonEdges(cavern, x, y) > Math.Max(0.01, margin);
            }

            return x > left && x < right && y > top && y < bottom;
        }

        private static void FindRectangleEdgePoint(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            double left = room.X;
            double right = room.X + room.Width;
            double top = room.Y;
            double bottom = room.Y + room.Height;

            if (targetX >= left && targetX <= right)
            {
                if (targetY <= top)
                {
                    x = targetX;
                    y = top;
                    orientation = "Horizontal";
                    return;
                }

                if (targetY >= bottom)
                {
                    x = targetX;
                    y = bottom;
                    orientation = "Horizontal";
                    return;
                }
            }

            if (targetY >= top && targetY <= bottom)
            {
                if (targetX <= left)
                {
                    x = left;
                    y = targetY;
                    orientation = "Vertical";
                    return;
                }

                if (targetX >= right)
                {
                    x = right;
                    y = targetY;
                    orientation = "Vertical";
                    return;
                }
            }

            double centerX = RoomCenterX(room);
            double centerY = RoomCenterY(room);
            double dx = targetX - centerX;
            double dy = targetY - centerY;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001)
            {
                x = centerX;
                y = top;
                orientation = "Horizontal";
                return;
            }

            double tx = Math.Abs(dx) < 0.0001
                ? double.MaxValue
                : dx > 0 ? (right - centerX) / dx : (left - centerX) / dx;
            double ty = Math.Abs(dy) < 0.0001
                ? double.MaxValue
                : dy > 0 ? (bottom - centerY) / dy : (top - centerY) / dy;
            double t = Math.Min(Math.Abs(tx), Math.Abs(ty));
            x = Clamp(centerX + dx * t, left, right);
            y = Clamp(centerY + dy * t, top, bottom);
            bool verticalWall = Math.Abs(Math.Abs(tx) - t) <= Math.Abs(Math.Abs(ty) - t);
            orientation = verticalWall ? "Vertical" : "Horizontal";
        }

        private static double PassageCornerClearance(int passageWidth)
        {
            int width = Math.Max(1, Math.Min(4, passageWidth));
            return Math.Min(1.0, 0.45 + width * 0.13);
        }

        private static double ClampToWallSpan(double value, double min, double max, double clearance)
        {
            double low = min + clearance;
            double high = max - clearance;
            if (low > high) return (min + max) / 2.0;
            return Clamp(value, low, high);
        }

        private static double ClampToPassageWallSpan(double value, double min, double max, double clearance)
        {
            // Если стена слишком короткая, обычный отступ от углов сам ломает прямой коридор.
            if (max - min <= clearance * 3.0 + 0.001) return Clamp(value, min, max);
            return ClampToWallSpan(value, min, max, clearance);
        }

        private static void FindEllipseEdgePoint(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            double centerX = RoomCenterX(room);
            double centerY = RoomCenterY(room);
            double dx = targetX - centerX;
            double dy = targetY - centerY;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001)
            {
                dx = 0;
                dy = -1;
            }

            double rx = Math.Max(0.05, room.Width / 2.0);
            double ry = Math.Max(0.05, room.Height / 2.0);
            double scale = 1.0 / Math.Sqrt(dx * dx / (rx * rx) + dy * dy / (ry * ry));
            x = centerX + dx * scale;
            y = centerY + dy * scale;

            double normalX = (x - centerX) / (rx * rx);
            double normalY = (y - centerY) / (ry * ry);
            double tangent = Math.Atan2(normalY, normalX) * 180.0 / Math.PI + 90.0;
            orientation = "Angle:" + NormalizeAngle(tangent).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool FindPolygonEdgePoint(List<DungeonPathPointRecord> polygon, double centerX, double centerY, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            x = centerX;
            y = centerY;
            orientation = "Horizontal";
            if (polygon == null || polygon.Count < 3) return false;

            double rayX = targetX - centerX;
            double rayY = targetY - centerY;
            if (Math.Abs(rayX) < 0.0001 && Math.Abs(rayY) < 0.0001)
            {
                rayX = 0;
                rayY = -1;
            }

            double bestT = double.MaxValue;
            double bestEdgeX = 1;
            double bestEdgeY = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                DungeonPathPointRecord a = polygon[i];
                DungeonPathPointRecord b = polygon[(i + 1) % polygon.Count];
                double segX = b.X - a.X;
                double segY = b.Y - a.Y;
                double denominator = Cross(rayX, rayY, segX, segY);
                if (Math.Abs(denominator) < 0.0001) continue;

                double relX = a.X - centerX;
                double relY = a.Y - centerY;
                double t = Cross(relX, relY, segX, segY) / denominator;
                double u = Cross(relX, relY, rayX, rayY) / denominator;
                if (t <= 0.0001 || u < -0.0001 || u > 1.0001) continue;
                if (t >= bestT) continue;

                bestT = t;
                bestEdgeX = segX;
                bestEdgeY = segY;
            }

            if (bestT == double.MaxValue) return false;
            x = centerX + rayX * bestT;
            y = centerY + rayY * bestT;
            orientation = "Angle:" + NormalizeAngle(Math.Atan2(bestEdgeY, bestEdgeX) * 180.0 / Math.PI).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryFindPolygonAxisPoint(List<DungeonPathPointRecord> polygon, bool horizontalPassage, bool positiveSide, double fixedCoordinate, out DungeonPathPointRecord point)
        {
            point = null;
            if (polygon == null || polygon.Count < 3) return false;

            List<double> intersections = new List<double>();
            for (int i = 0; i < polygon.Count; i++)
            {
                DungeonPathPointRecord a = polygon[i];
                DungeonPathPointRecord b = polygon[(i + 1) % polygon.Count];
                if (horizontalPassage)
                {
                    if ((fixedCoordinate < Math.Min(a.Y, b.Y) || fixedCoordinate > Math.Max(a.Y, b.Y)) || Math.Abs(a.Y - b.Y) < 0.0001) continue;
                    double t = (fixedCoordinate - a.Y) / (b.Y - a.Y);
                    if (t < -0.0001 || t > 1.0001) continue;
                    intersections.Add(a.X + (b.X - a.X) * t);
                }
                else
                {
                    if ((fixedCoordinate < Math.Min(a.X, b.X) || fixedCoordinate > Math.Max(a.X, b.X)) || Math.Abs(a.X - b.X) < 0.0001) continue;
                    double t = (fixedCoordinate - a.X) / (b.X - a.X);
                    if (t < -0.0001 || t > 1.0001) continue;
                    intersections.Add(a.Y + (b.Y - a.Y) * t);
                }
            }

            if (intersections.Count == 0) return false;
            double coordinate = positiveSide ? intersections.Max() : intersections.Min();
            point = horizontalPassage
                ? new DungeonPathPointRecord { X = coordinate, Y = fixedCoordinate }
                : new DungeonPathPointRecord { X = fixedCoordinate, Y = coordinate };
            return true;
        }

        private static void RoomOutwardNormal(DungeonRoomRecord room, double edgeX, double edgeY, string orientation, out double normalX, out double normalY)
        {
            normalX = 0;
            normalY = -1;
            if (room == null) return;

            if (string.Equals(orientation, "Vertical", StringComparison.OrdinalIgnoreCase))
            {
                normalX = edgeX <= RoomCenterX(room) ? -1 : 1;
                normalY = 0;
                return;
            }

            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase))
            {
                normalX = 0;
                normalY = edgeY <= RoomCenterY(room) ? -1 : 1;
                return;
            }

            normalX = edgeX - RoomCenterX(room);
            normalY = edgeY - RoomCenterY(room);
            double length = Math.Sqrt(normalX * normalX + normalY * normalY);
            if (length < 0.0001)
            {
                normalX = 0;
                normalY = -1;
                return;
            }

            normalX /= length;
            normalY /= length;
        }

        private static bool IsEllipseShape(string shape)
        {
            return string.Equals(shape, "Circle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(shape, "Oval", StringComparison.OrdinalIgnoreCase);
        }

        private static List<DungeonPathPointRecord> CavernPoints(DungeonRoomRecord room)
        {
            double wobbleX = Math.Max(0.15, room.Width * 0.08);
            double wobbleY = Math.Max(0.15, room.Height * 0.08);
            return new List<DungeonPathPointRecord>
            {
                new DungeonPathPointRecord { X = room.X + wobbleX, Y = room.Y },
                new DungeonPathPointRecord { X = room.X + room.Width - wobbleX * 0.5, Y = room.Y + wobbleY },
                new DungeonPathPointRecord { X = room.X + room.Width, Y = room.Y + room.Height * 0.42 },
                new DungeonPathPointRecord { X = room.X + room.Width - wobbleX, Y = room.Y + room.Height - wobbleY * 0.4 },
                new DungeonPathPointRecord { X = room.X + room.Width * 0.48, Y = room.Y + room.Height },
                new DungeonPathPointRecord { X = room.X, Y = room.Y + room.Height - wobbleY },
                new DungeonPathPointRecord { X = room.X + wobbleX * 0.4, Y = room.Y + room.Height * 0.45 }
            };
        }

        private static bool PointInPolygon(List<DungeonPathPointRecord> polygon, double x, double y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                DungeonPathPointRecord a = polygon[i];
                DungeonPathPointRecord b = polygon[j];
                bool crosses = (a.Y > y) != (b.Y > y);
                if (crosses)
                {
                    double intersectionX = (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X;
                    if (x < intersectionX) inside = !inside;
                }
            }

            return inside;
        }

        private static double DistanceToPolygonEdges(List<DungeonPathPointRecord> polygon, double x, double y)
        {
            if (polygon == null || polygon.Count == 0) return double.MaxValue;
            double best = double.MaxValue;
            for (int i = 0; i < polygon.Count; i++)
            {
                DungeonPathPointRecord a = polygon[i];
                DungeonPathPointRecord b = polygon[(i + 1) % polygon.Count];
                best = Math.Min(best, DistanceToSegment(x, y, a.X, a.Y, b.X, b.Y));
            }

            return best;
        }

        private static double DistanceToSegment(double x, double y, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 0.000001)
            {
                double px = x - ax;
                double py = y - ay;
                return Math.Sqrt(px * px + py * py);
            }

            double t = ((x - ax) * dx + (y - ay) * dy) / lengthSquared;
            t = Clamp(t, 0, 1);
            double nearestX = ax + dx * t;
            double nearestY = ay + dy * t;
            double ox = x - nearestX;
            double oy = y - nearestY;
            return Math.Sqrt(ox * ox + oy * oy);
        }

        private static double RoomCenterX(DungeonRoomRecord room)
        {
            return room.X + room.Width / 2.0;
        }

        private static double RoomCenterY(DungeonRoomRecord room)
        {
            return room.Y + room.Height / 2.0;
        }

        private static double Cross(double ax, double ay, double bx, double by)
        {
            return ax * by - ay * bx;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static double NormalizeAngle(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return 0.0;
            angle %= 360.0;
            if (angle <= -180.0) angle += 360.0;
            if (angle > 180.0) angle -= 360.0;
            return angle;
        }
    }
}
