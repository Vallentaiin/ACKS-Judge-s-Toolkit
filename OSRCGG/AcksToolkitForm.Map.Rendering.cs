using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        // Отрисовка карты отделена от редактирования данных: этот файл отвечает за видимый слой карты.
        private void DrawCurrentMap(Graphics graphics, RectangleF visibleWorld, bool exportMode)
        {
            if (graphics == null || currentMap == null) return;

            List<HexCellRecord> visibleCells = BuildVisibleMapCellList(visibleWorld);
            currentPaintVisibleWorld = visibleWorld;
            currentPaintVisibleCells = visibleCells;
            try
            {
                bool showRoads = chkMapShowRoads == null || chkMapShowRoads.Checked;
                bool showRivers = chkMapShowRivers == null || chkMapShowRivers.Checked;
                List<MapEdgeRecord> visibleRoads = showRoads ? GetVisibleMapEdges(currentMap.Roads, visibleWorld) : new List<MapEdgeRecord>();
                List<MapEdgeRecord> visibleRivers = showRivers ? GetVisibleMapEdges(currentMap.Rivers, visibleWorld) : new List<MapEdgeRecord>();
                int layer = cmbMapLayer.SelectedIndex < 0 ? 0 : cmbMapLayer.SelectedIndex;
                using (Pen cellBorderPen = new Pen(Color.FromArgb(80, 32, 32, 32), 1f))
                using (Pen selectedCellPen = new Pen(Color.Gold, 3f))
                {
                    foreach (HexCellRecord cell in visibleCells)
                    {
                        DrawMapCell(graphics, cell, layer, !exportMode, cellBorderPen, selectedCellPen);
                    }
                }

                DrawMapLargeHexGrid(graphics, visibleWorld);
                DrawMapDomains(graphics, visibleWorld);
                if (showRoads) DrawMapEdges(graphics, visibleRoads, Color.FromArgb(151, 96, 48), Color.FromArgb(85, 50, 24), 7f);
                if (showRivers) DrawMapEdges(graphics, visibleRivers, Color.FromArgb(87, 184, 230), Color.FromArgb(36, 119, 172), 7f);
                if (showRivers) DrawRiverMouths(graphics, visibleRivers);
                if (showRoads && showRivers) DrawRoadRiverCrossings(graphics, visibleRoads, visibleRivers);
                DrawMapSettlements(graphics, layer, visibleWorld);
                DrawMapStrongholds(graphics, visibleWorld);
                DrawMapHexFeatures(graphics, visibleWorld);
                DrawMapHexCoordinates(graphics, visibleWorld);
                DrawMapFeatureLabels(graphics, visibleWorld);
                DrawMapRealmLabels(graphics, visibleWorld);
                DrawMapPlaceLabels(graphics, visibleWorld);
                DrawHoveredMapPlaceLabel(graphics, visibleWorld);
            }
            finally
            {
                currentPaintVisibleCells = null;
            }
        }

        private IEnumerable<HexCellRecord> GetVisibleMapCells(RectangleF visibleWorld)
        {
            if (currentPaintVisibleCells != null && currentPaintVisibleWorld.Equals(visibleWorld))
            {
                foreach (HexCellRecord cell in currentPaintVisibleCells)
                {
                    yield return cell;
                }

                yield break;
            }

            foreach (HexCellRecord cell in BuildVisibleMapCellList(visibleWorld))
            {
                yield return cell;
            }
        }

        private List<HexCellRecord> BuildVisibleMapCellList(RectangleF visibleWorld)
        {
            List<HexCellRecord> result = new List<HexCellRecord>();
            if (currentMap == null || currentMapCellIndex == null || currentMapCellIndex.Count == 0) return result;

            // На больших картах нельзя на каждый Paint проходить все 20k+ гексов.
            // Диапазон q/r даёт тот же порядок отрисовки, но перебирает только область viewport.
            int minR = Math.Max(currentMapMinR, (int)Math.Floor((visibleWorld.Top - 38f - MapHexSize) / MapHexRowHeight));
            int maxR = Math.Min(currentMapMaxR, (int)Math.Ceiling((visibleWorld.Bottom - 38f + MapHexSize) / MapHexRowHeight));
            int minQ = Math.Max(currentMapMinQ, (int)Math.Floor((visibleWorld.Left - 38f - MapHexHorizontalRadius) / MapHexWidth) - 1);
            int maxQ = Math.Min(currentMapMaxQ, (int)Math.Ceiling((visibleWorld.Right - 38f + MapHexHorizontalRadius) / MapHexWidth) + 1);

            for (int r = minR; r <= maxR; r++)
            {
                for (int q = minQ; q <= maxQ; q++)
                {
                    HexCellRecord cell;
                    if (!currentMapCellIndex.TryGetValue(CellKey(q, r), out cell)) continue;
                    if (!IsCellVisible(cell, visibleWorld)) continue;
                    result.Add(cell);
                }
            }

            return result;
        }

        private RectangleF GetVisibleWorldBounds(Rectangle clip)
        {
            if (mapZoom <= 0.001f) return RectangleF.Empty;
            Point scroll = GetMapScroll();
            float left = (clip.Left + scroll.X) / mapZoom;
            float top = (clip.Top + scroll.Y) / mapZoom;
            float right = (clip.Right + scroll.X) / mapZoom;
            float bottom = (clip.Bottom + scroll.Y) / mapZoom;
            const float margin = MapHexSize * 2.5f;
            return RectangleF.FromLTRB(
                Math.Min(left, right) - margin,
                Math.Min(top, bottom) - margin,
                Math.Max(left, right) + margin,
                Math.Max(top, bottom) + margin);
        }

        private bool IsCellVisible(HexCellRecord cell, RectangleF visibleWorld)
        {
            if (cell == null) return false;
            PointF center = GetHexCenter(cell.Q, cell.R);
            return visibleWorld.Contains(center);
        }

        private List<MapEdgeRecord> GetVisibleMapEdges(List<MapEdgeRecord> edges, RectangleF visibleWorld)
        {
            if (edges == null || edges.Count == 0) return new List<MapEdgeRecord>();

            Dictionary<string, List<MapEdgeRecord>> edgeIndex = null;
            if (currentMap != null && object.ReferenceEquals(edges, currentMap.Roads)) edgeIndex = currentMapRoadsByCell;
            else if (currentMap != null && object.ReferenceEquals(edges, currentMap.Rivers)) edgeIndex = currentMapRiversByCell;
            if (edgeIndex != null && edgeIndex.Count > 0)
            {
                List<MapEdgeRecord> indexedResult = new List<MapEdgeRecord>();
                HashSet<string> seen = new HashSet<string>();
                foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
                {
                    List<MapEdgeRecord> cellEdges;
                    if (!edgeIndex.TryGetValue(CellKey(cell.Q, cell.R), out cellEdges)) continue;

                    foreach (MapEdgeRecord edge in cellEdges)
                    {
                        if (edge == null || !seen.Add(edge.NormalizedKey())) continue;
                        PointF a = GetHexCenter(edge.AQ, edge.AR);
                        PointF b = GetHexCenter(edge.BQ, edge.BR);
                        RectangleF bounds = RectangleF.FromLTRB(
                            Math.Min(a.X, b.X) - MapHexSize,
                            Math.Min(a.Y, b.Y) - MapHexSize,
                            Math.Max(a.X, b.X) + MapHexSize,
                            Math.Max(a.Y, b.Y) + MapHexSize);
                        if (visibleWorld.IntersectsWith(bounds)) indexedResult.Add(edge);
                    }
                }

                return indexedResult;
            }

            List<MapEdgeRecord> result = new List<MapEdgeRecord>();
            foreach (MapEdgeRecord edge in edges)
            {
                PointF a = GetHexCenter(edge.AQ, edge.AR);
                PointF b = GetHexCenter(edge.BQ, edge.BR);
                RectangleF bounds = RectangleF.FromLTRB(
                    Math.Min(a.X, b.X) - MapHexSize,
                    Math.Min(a.Y, b.Y) - MapHexSize,
                    Math.Max(a.X, b.X) + MapHexSize,
                    Math.Max(a.Y, b.Y) + MapHexSize);
                if (visibleWorld.IntersectsWith(bounds))
                {
                    result.Add(edge);
                }
            }

            return result;
        }

        private IEnumerable<MapSettlementRecord> GetVisibleMapSettlements(RectangleF visibleWorld)
        {
            if (currentMapSettlementsByCell == null || currentMapSettlementsByCell.Count == 0) yield break;

            foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
            {
                List<MapSettlementRecord> settlements;
                if (!currentMapSettlementsByCell.TryGetValue(CellKey(cell.Q, cell.R), out settlements)) continue;
                foreach (MapSettlementRecord settlement in settlements)
                {
                    if (settlement != null) yield return settlement;
                }
            }
        }

        private IEnumerable<DomainRecord> GetVisibleMapStrongholds(RectangleF visibleWorld)
        {
            if (currentMapStrongholdsByCell == null || currentMapStrongholdsByCell.Count == 0) yield break;

            foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
            {
                List<DomainRecord> domains;
                if (!currentMapStrongholdsByCell.TryGetValue(CellKey(cell.Q, cell.R), out domains)) continue;
                foreach (DomainRecord domain in domains)
                {
                    if (domain != null && HasVisibleStronghold(domain)) yield return domain;
                }
            }
        }

        private IEnumerable<HexFeatureRecord> GetVisibleMapFeatures(RectangleF visibleWorld)
        {
            if (currentMapFeaturesByCell == null || currentMapFeaturesByCell.Count == 0) yield break;

            foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
            {
                List<HexFeatureRecord> features;
                if (!currentMapFeaturesByCell.TryGetValue(CellKey(cell.Q, cell.R), out features)) continue;
                foreach (HexFeatureRecord feature in features)
                {
                    if (feature != null) yield return feature;
                }
            }
        }

        private Dictionary<DomainRecord, List<HexCellRecord>> GetVisibleDomainCellsByDomain(RectangleF visibleWorld)
        {
            Dictionary<DomainRecord, List<HexCellRecord>> result = new Dictionary<DomainRecord, List<HexCellRecord>>();
            if (currentMapDomainByHex == null || currentMapDomainByHex.Count == 0) return result;

            foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
            {
                DomainRecord domain;
                if (!currentMapDomainByHex.TryGetValue(CellKey(cell.Q, cell.R), out domain) || domain == null) continue;

                List<HexCellRecord> cells;
                if (!result.TryGetValue(domain, out cells))
                {
                    cells = new List<HexCellRecord>();
                    result[domain] = cells;
                }

                cells.Add(cell);
            }

            return result;
        }

        private void DrawMapCell(Graphics graphics, HexCellRecord cell, int layer, bool drawSelection, Pen borderPen, Pen selectedPen)
        {
            PointF center = GetHexCenter(cell.Q, cell.R);
            PointF[] hex = GetHexPolygon(center);
            Color fill = GetCellBaseColor(cell, layer);

            using (SolidBrush brush = new SolidBrush(fill))
            {
                graphics.FillPolygon(brush, hex);
            }

            if (borderPen != null)
            {
                graphics.DrawPolygon(borderPen, hex);
            }

            if (drawSelection && selectedPen != null && selectedMapCell != null && selectedMapCell.Q == cell.Q && selectedMapCell.R == cell.R)
            {
                graphics.DrawPolygon(selectedPen, hex);
            }

            if (!chkMapShowIcons.Checked) return;
            if ((layer == 1 || layer == 2 || layer == 3) && mapZoom < 0.55f) return;

            if (layer == 3)
            {
                DrawLandscapeIcons(graphics, cell, center);
                return;
            }

            string iconKey = null;
            if (IsWaterCell(cell))
            {
                iconKey = cell.Water == "Lake" ? "lake" : null;
            }
            else if (layer == 1)
            {
                iconKey = GetTerrainIconKey(cell.Terrain);
            }
            else if (layer == 2)
            {
                iconKey = string.IsNullOrWhiteSpace(cell.Elevation) || cell.Elevation == "Plains" ? null : cell.Elevation.ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(iconKey))
            {
                DrawIconCentered(graphics, iconKey, center, 26);
            }
        }

        private void DrawLandscapeIcons(Graphics graphics, HexCellRecord cell, PointF center)
        {
            if (IsWaterCell(cell))
            {
                if (cell.Water == "Lake")
                {
                    DrawIconCentered(graphics, "lake", center, 26);
                }
                return;
            }

            string terrainIconKey = GetTerrainIconKey(cell.Terrain);
            string elevationIconKey = string.IsNullOrWhiteSpace(cell.Elevation) || cell.Elevation == "Plains" ? null : cell.Elevation.ToLowerInvariant();
            bool hasTerrain = !string.IsNullOrWhiteSpace(terrainIconKey);
            bool hasElevation = !string.IsNullOrWhiteSpace(elevationIconKey);

            if (hasTerrain && hasElevation)
            {
                DrawIconCentered(graphics, terrainIconKey, new PointF(center.X - 8f, center.Y - 6f), 18);
                DrawIconCentered(graphics, elevationIconKey, new PointF(center.X + 8f, center.Y + 7f), 18);
                return;
            }

            if (hasTerrain)
            {
                DrawIconCentered(graphics, terrainIconKey, center, 26);
            }
            else if (hasElevation)
            {
                DrawIconCentered(graphics, elevationIconKey, center, 26);
            }
        }

        private void DrawMapEdges(Graphics graphics, List<MapEdgeRecord> edges, Color color, Color outline, float width)
        {
            if (edges == null) return;

            using (Pen outlinePen = new Pen(outline, width + 4f))
            using (Pen pen = new Pen(color, width))
            {
                outlinePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                outlinePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                outlinePen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                foreach (MapEdgeRecord edge in edges)
                {
                    PointF a;
                    PointF b;
                    GetEdgeDrawPoints(edge, out a, out b);
                    graphics.DrawLine(outlinePen, a, b);
                }

                foreach (MapEdgeRecord edge in edges)
                {
                    PointF a;
                    PointF b;
                    GetEdgeDrawPoints(edge, out a, out b);
                    graphics.DrawLine(pen, a, b);
                }
            }

            if (!chkMapShowIcons.Checked || edges.Count == 0 || !string.Equals(edges[0].Kind, "River", StringComparison.OrdinalIgnoreCase)) return;

            foreach (MapEdgeRecord edge in edges)
            {
                PointF a;
                PointF b;
                GetEdgeDrawPoints(edge, out a, out b);
                PointF center = new PointF((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
                float angle = (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI);
                DrawIconCenteredRotated(graphics, "river", center, 22, angle);
            }
        }

        private void GetEdgeDrawPoints(MapEdgeRecord edge, out PointF a, out PointF b)
        {
            a = GetHexCenter(edge.AQ, edge.AR);
            b = GetHexCenter(edge.BQ, edge.BR);
            if (edge == null || !string.Equals(edge.Kind, "River", StringComparison.OrdinalIgnoreCase)) return;

            HexCellRecord aCell = GetCell(edge.AQ, edge.AR);
            HexCellRecord bCell = GetCell(edge.BQ, edge.BR);
            bool aWater = IsRiverMouthWater(aCell);
            bool bWater = IsRiverMouthWater(bCell);
            if (aWater == bWater) return;

            PointF land = aWater ? b : a;
            PointF water = aWater ? a : b;
            PointF clipped = new PointF(
                land.X + (water.X - land.X) * 0.82f,
                land.Y + (water.Y - land.Y) * 0.82f);

            if (aWater) a = clipped;
            else b = clipped;
        }

        private void DrawRoadRiverCrossings(Graphics graphics, List<MapEdgeRecord> roads, List<MapEdgeRecord> rivers)
        {
            if (roads == null || rivers == null) return;
            if (roads.Count == 0 || rivers.Count == 0) return;

            using (Pen dashedOutline = new Pen(Color.FromArgb(55, 30, 12), 7f))
            using (Pen dashedRoad = new Pen(Color.FromArgb(232, 158, 72), 4f))
            {
                dashedOutline.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                dashedOutline.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                dashedOutline.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                dashedRoad.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                dashedRoad.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                dashedRoad.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                foreach (MapEdgeRecord road in roads)
                {
                    PointF roadA = GetHexCenter(road.AQ, road.AR);
                    PointF roadB = GetHexCenter(road.BQ, road.BR);

                    foreach (MapEdgeRecord river in rivers)
                    {
                        PointF riverA = GetHexCenter(river.AQ, river.AR);
                        PointF riverB = GetHexCenter(river.BQ, river.BR);

                        if (SameMapSegment(road, river))
                        {
                            graphics.DrawLine(dashedOutline, roadA, roadB);
                            graphics.DrawLine(dashedRoad, roadA, roadB);
                            break;
                        }

                        PointF intersection;
                        if (TryGetSegmentIntersection(roadA, roadB, riverA, riverB, out intersection))
                        {
                            DrawShortDashedSegment(graphics, dashedOutline, roadA, roadB, intersection, 18f);
                            DrawShortDashedSegment(graphics, dashedRoad, roadA, roadB, intersection, 14f);
                        }
                    }
                }
            }
        }

        private void DrawRiverMouths(Graphics graphics, List<MapEdgeRecord> rivers)
        {
            if (graphics == null || rivers == null || rivers.Count == 0) return;

            // Устье рисуется программно: оно подстраивается под любое из шести направлений
            // гекса и не требует отдельных повернутых PNG для озера, моря или океана.
            foreach (MapEdgeRecord river in rivers)
            {
                HexCellRecord aCell = GetCell(river.AQ, river.AR);
                HexCellRecord bCell = GetCell(river.BQ, river.BR);
                if (aCell == null || bCell == null) continue;

                bool aWater = IsRiverMouthWater(aCell);
                bool bWater = IsRiverMouthWater(bCell);
                if (aWater == bWater) continue;

                PointF land = GetHexCenter(aWater ? river.BQ : river.AQ, aWater ? river.BR : river.AR);
                PointF water = GetHexCenter(aWater ? river.AQ : river.BQ, aWater ? river.AR : river.BR);
                DrawRiverMouth(graphics, land, water);
            }
        }

        private bool IsRiverMouthWater(HexCellRecord cell)
        {
            return cell != null && (cell.Water == "Lake" || cell.Water == "Sea" || cell.Water == "Ocean");
        }

        private void DrawRiverMouth(Graphics graphics, PointF land, PointF water)
        {
            float dx = water.X - land.X;
            float dy = water.Y - land.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.001f) return;

            float ux = dx / length;
            float uy = dy / length;
            float px = -uy;
            float py = ux;

            PointF mouth = new PointF(land.X + ux * length * 0.55f, land.Y + uy * length * 0.55f);
            PointF end = new PointF(land.X + ux * length * 0.88f, land.Y + uy * length * 0.88f);
            PointF[] fan =
            {
                new PointF(mouth.X + px * 4f, mouth.Y + py * 4f),
                new PointF(mouth.X - px * 4f, mouth.Y - py * 4f),
                new PointF(end.X - px * 15f, end.Y - py * 15f),
                new PointF(end.X + px * 15f, end.Y + py * 15f)
            };

            using (SolidBrush fill = new SolidBrush(Color.FromArgb(125, 87, 184, 230)))
            using (Pen outline = new Pen(Color.FromArgb(120, 36, 119, 172), 2f))
            using (Pen branch = new Pen(Color.FromArgb(205, 95, 202, 238), 3.2f))
            {
                branch.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                branch.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                graphics.FillPolygon(fill, fan);
                graphics.DrawPolygon(outline, fan);
                graphics.DrawLine(branch, mouth, end);
                graphics.DrawLine(branch, mouth, new PointF(end.X + px * 12f, end.Y + py * 12f));
                graphics.DrawLine(branch, mouth, new PointF(end.X - px * 12f, end.Y - py * 12f));
            }
        }

        private void DrawMapFeatureLabels(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null) return;
            if (chkMapShowFeatureLabels != null && !chkMapShowFeatureLabels.Checked) return;
            if (mapZoom < 0.5f) return;

            if (currentMapWaterFeatureLabels == null || currentMapRiverFeatureLabels == null)
            {
                RebuildMapFeatureLabelIndex();
            }

            foreach (MapFeatureLabelRecord label in currentMapWaterFeatureLabels)
            {
                if (label == null || !visibleWorld.Contains(label.Center)) continue;

                using (Font font = CreateMapFont(label.FontSize, FontStyle.Italic))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.FormatFlags |= StringFormatFlags.NoWrap;
                    RectangleF rect = CenteredSingleLineTextBounds(graphics, label.DisplayText, font, label.Center, label.MinWidth, label.HorizontalPadding, label.Height);
                    DrawOutlinedText(graphics, label.DisplayText, font, rect, format, Color.FromArgb(225, 230, 244, 255), Color.FromArgb(120, 8, 28, 52), label.OutlineWidth);
                }
            }

            foreach (MapFeatureLabelRecord label in currentMapRiverFeatureLabels)
            {
                if (label == null || !visibleWorld.Contains(label.Center)) continue;

                using (Font font = CreateMapFont(label.FontSize, FontStyle.Italic))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.FormatFlags |= StringFormatFlags.NoWrap;
                    RectangleF rect = CenteredSingleLineTextBounds(graphics, label.DisplayText, font, label.Center, label.MinWidth, label.HorizontalPadding, label.Height);
                    DrawOutlinedText(graphics, label.DisplayText, font, rect, format, Color.FromArgb(235, 172, 226, 255), Color.FromArgb(150, 8, 54, 84), label.OutlineWidth);
                }
            }
        }

        private void RebuildMapFeatureLabelIndex()
        {
            List<MapFeatureLabelRecord> waterLabels = new List<MapFeatureLabelRecord>();
            List<MapFeatureLabelRecord> riverLabels = new List<MapFeatureLabelRecord>();

            if (currentMap != null)
            {
                // Названия природных объектов хранятся на гексах/речных ребрах. Группируем
                // их только при изменении карты, чтобы pan/zoom не проходил всю модель.
                foreach (var group in (currentMap.Cells ?? new List<HexCellRecord>())
                    .Where(c => c != null && IsWaterCell(c) && !string.IsNullOrWhiteSpace(c.WaterFeatureName))
                    .GroupBy(c => c.WaterFeatureName))
                {
                    List<HexCellRecord> cells = group.ToList();
                    if (cells.Count < 3) continue;

                    string water = WaterFeatureKind(cells);
                    string text = cells[0].WaterFeatureName;
                    bool largeWater = water == "Ocean" || water == "Sea";
                    waterLabels.Add(new MapFeatureLabelRecord
                    {
                        Text = text,
                        DisplayText = largeWater ? AddLetterSpacing(text) : text,
                        Kind = water,
                        Center = AverageCellCenter(cells),
                        FontSize = water == "Ocean" ? 16.5f : water == "Sea" ? 14f : 10.5f,
                        MinWidth = largeWater ? 290f : 190f,
                        HorizontalPadding = 22f,
                        Height = 48f,
                        OutlineWidth = largeWater ? 3f : 2.2f
                    });
                }

                foreach (var group in (currentMap.Rivers ?? new List<MapEdgeRecord>())
                    .Where(e => e != null && !string.IsNullOrWhiteSpace(e.FeatureName))
                    .GroupBy(e => e.FeatureName))
                {
                    List<MapEdgeRecord> river = group.ToList();
                    if (river.Count < 6) continue;

                    string text = group.Key;
                    riverLabels.Add(new MapFeatureLabelRecord
                    {
                        Text = text,
                        DisplayText = text,
                        Kind = "River",
                        Center = AverageEdgeCenter(river),
                        FontSize = 9.8f,
                        MinWidth = 180f,
                        HorizontalPadding = 18f,
                        Height = 32f,
                        OutlineWidth = 2.3f
                    });
                }
            }

            currentMapWaterFeatureLabels = waterLabels;
            currentMapRiverFeatureLabels = riverLabels;
        }

        private string WaterFeatureKind(List<HexCellRecord> cells)
        {
            if (cells != null && cells.Any(c => c.Water == "Ocean")) return "Ocean";
            if (cells != null && cells.Any(c => c.Water == "Sea")) return "Sea";
            return "Lake";
        }

        private PointF AverageCellCenter(List<HexCellRecord> cells)
        {
            if (cells == null || cells.Count == 0) return PointF.Empty;
            float x = 0f;
            float y = 0f;
            foreach (HexCellRecord cell in cells)
            {
                PointF center = GetHexCenter(cell.Q, cell.R);
                x += center.X;
                y += center.Y;
            }

            return new PointF(x / cells.Count, y / cells.Count);
        }

        private PointF AverageEdgeCenter(List<MapEdgeRecord> edges)
        {
            if (edges == null || edges.Count == 0) return PointF.Empty;
            float x = 0f;
            float y = 0f;
            foreach (MapEdgeRecord edge in edges)
            {
                PointF a = GetHexCenter(edge.AQ, edge.AR);
                PointF b = GetHexCenter(edge.BQ, edge.BR);
                x += (a.X + b.X) / 2f;
                y += (a.Y + b.Y) / 2f;
            }

            return new PointF(x / edges.Count, y / edges.Count);
        }

        private string AddLetterSpacing(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            List<string> words = new List<string>();
            foreach (string word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length <= 2) words.Add(word);
                else words.Add(string.Join(" ", word.ToCharArray()));
            }

            return string.Join("   ", words);
        }

        private Font CreateMapFont(float size, FontStyle style)
        {
            EnsureMapFontsLoaded();
            if (mapFontCollection != null && mapFontCollection.Families.Length > 0)
            {
                try
                {
                    return new Font(mapFontCollection.Families[0], size, style, GraphicsUnit.Point);
                }
                catch
                {
                    // Если Windows не принимает конкретное начертание variable-font, падаем на системный serif.
                }
            }

            string family = InstalledFontCollectionHas("Alegreya") ? "Alegreya" : "Georgia";
            return new Font(family, size, style, GraphicsUnit.Point);
        }

        private void EnsureMapFontsLoaded()
        {
            if (mapFontsLoaded) return;
            mapFontsLoaded = true;
            mapFontCollection = new PrivateFontCollection();

            foreach (string fileName in new[] { "Alegreya.ttf", "Alegreya-Italic.ttf" })
            {
                string path = FindMapAssetPath(Path.Combine("Fonts", fileName));
                if (string.IsNullOrWhiteSpace(path)) continue;
                try
                {
                    mapFontCollection.AddFontFile(path);
                }
                catch
                {
                    // Подписи карты используют fallback, если локальный TTF повреждён или недоступен.
                }
            }
        }

        private bool InstalledFontCollectionHas(string familyName)
        {
            using (InstalledFontCollection installed = new InstalledFontCollection())
            {
                return installed.Families.Any(f => string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private string FindMapAssetPath(string relativePath)
        {
            string[] roots =
            {
                Path.Combine(Application.StartupPath, "MapAssets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapAssets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "MapAssets")
            };

            foreach (string root in roots)
            {
                string path = Path.GetFullPath(Path.Combine(root, relativePath));
                if (File.Exists(path)) return path;
            }

            return "";
        }

        private void DrawShortDashedSegment(Graphics graphics, Pen pen, PointF a, PointF b, PointF center, float halfLength)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.001f) return;

            float ux = dx / length;
            float uy = dy / length;
            PointF start = new PointF(center.X - ux * halfLength, center.Y - uy * halfLength);
            PointF end = new PointF(center.X + ux * halfLength, center.Y + uy * halfLength);
            graphics.DrawLine(pen, start, end);
        }

        private bool SameMapSegment(MapEdgeRecord a, MapEdgeRecord b)
        {
            return (a.AQ == b.AQ && a.AR == b.AR && a.BQ == b.BQ && a.BR == b.BR)
                || (a.AQ == b.BQ && a.AR == b.BR && a.BQ == b.AQ && a.BR == b.AR);
        }

        private bool TryGetSegmentIntersection(PointF p, PointF p2, PointF q, PointF q2, out PointF intersection)
        {
            intersection = PointF.Empty;
            float rX = p2.X - p.X;
            float rY = p2.Y - p.Y;
            float sX = q2.X - q.X;
            float sY = q2.Y - q.Y;
            float denominator = Cross(rX, rY, sX, sY);
            if (Math.Abs(denominator) < 0.001f) return false;

            float qmpX = q.X - p.X;
            float qmpY = q.Y - p.Y;
            float t = Cross(qmpX, qmpY, sX, sY) / denominator;
            float u = Cross(qmpX, qmpY, rX, rY) / denominator;
            const float endpointEpsilon = 0.03f;
            if (t <= endpointEpsilon || t >= 1f - endpointEpsilon || u <= endpointEpsilon || u >= 1f - endpointEpsilon) return false;

            intersection = new PointF(p.X + t * rX, p.Y + t * rY);
            return true;
        }

        private float Cross(float ax, float ay, float bx, float by)
        {
            return ax * by - ay * bx;
        }

        private void DrawMapStrongholds(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null || currentMap.Domains == null) return;
            if (chkMapShowStrongholds != null && !chkMapShowStrongholds.Checked) return;
            bool smallIconMode = IsSmallMapIconMode();

            foreach (DomainRecord domain in GetVisibleMapStrongholds(visibleWorld))
            {
                if (domain == null) continue;
                if (!HasVisibleStronghold(domain)) continue;
                PointF center = GetHexCenter(domain.StrongholdQ, domain.StrongholdR);
                if (!visibleWorld.Contains(center)) continue;

                string iconKey = GetDomainStrongholdIconKey(domain);
                MapSettlementRecord settlementAtStronghold = GetSettlementAtStronghold(domain);
                if (domain.StrongholdInSettlement || settlementAtStronghold != null)
                {
                    if (smallIconMode) continue;

                    MapSettlementRecord settlement = settlementAtStronghold;
                    if (settlement == null) continue;

                    PointF settlementCenter = GetHexCenter(settlement.Q, settlement.R);
                    RectangleF badge = new RectangleF(settlementCenter.X + 9f, settlementCenter.Y - 22f, 18f, 18f);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(220, 246, 238, 184)))
                    using (Pen pen = new Pen(Color.FromArgb(230, 20, 20, 20), 1.5f))
                    {
                        graphics.FillEllipse(brush, badge);
                        graphics.DrawEllipse(pen, badge);
                    }
                    DrawIconCentered(graphics, iconKey, new PointF(badge.X + badge.Width / 2f, badge.Y + badge.Height / 2f), 14);
                    continue;
                }

                if (smallIconMode)
                {
                    DrawSmallHexCornerIcon(graphics, iconKey, center);
                    continue;
                }

                DrawSettlementIconBackdrop(graphics, center);
                DrawIconCentered(graphics, iconKey, center, 30);
            }
        }

        private void DrawMapSettlements(Graphics graphics, int layer, RectangleF visibleWorld)
        {
            if (chkMapShowSettlements != null && !chkMapShowSettlements.Checked) return;

            foreach (MapSettlementRecord settlement in GetVisibleMapSettlements(visibleWorld))
            {
                PointF center = GetHexCenter(settlement.Q, settlement.R);
                if (!visibleWorld.Contains(center)) continue;
                string iconKey = GetSettlementIconKey(settlement);
                bool smallIconMode = IsSmallMapIconMode();

                if (chkMapShowSettlementIcons.Checked && mapImages.ContainsKey(iconKey))
                {
                    if (smallIconMode)
                    {
                        DrawSmallHexCornerIcon(graphics, iconKey, center);
                    }
                    else
                    {
                        DrawSettlementIconBackdrop(graphics, center);
                        DrawIconCentered(graphics, iconKey, center, 34);
                    }
                }
                else
                {
                    float markerSize = GetSettlementMarkerSize(settlement.MarketClass);
                    float markerRadius = markerSize / 2f;
                    using (SolidBrush brush = new SolidBrush(GetSettlementMarkerColor(settlement.MarketClass)))
                    {
                        graphics.FillEllipse(brush, center.X - markerRadius, center.Y - markerRadius, markerSize, markerSize);
                    }
                    using (Pen pen = new Pen(Color.Black, 2.2f))
                    {
                        graphics.DrawEllipse(pen, center.X - markerRadius, center.Y - markerRadius, markerSize, markerSize);
                    }
                }

            }
        }

        private void DrawMapHexCoordinates(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null) return;
            if (chkMapShowHexCoordinates == null || !chkMapShowHexCoordinates.Checked) return;

            using (Font font = new Font("Microsoft Sans Serif", 5.8f, FontStyle.Bold))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                foreach (HexCellRecord cell in GetVisibleMapCells(visibleWorld))
                {
                    if (cell == null) continue;
                    PointF center = GetHexCenter(cell.Q, cell.R);
                    if (!visibleWorld.Contains(center)) continue;

                    string coordinate = cell.Q + "," + cell.R;
                    // Координата намеренно сидит в нижней части гекса: так она не спорит
                    // с основными значками местности, поселений и крепостей.
                    PointF labelCenter = new PointF(center.X, center.Y + MapHexSize * 0.62f);
                    RectangleF rect = CenteredSingleLineTextBounds(graphics, coordinate, font, labelCenter, 28f, 3f, 11f);
                    DrawOutlinedText(
                        graphics,
                        coordinate,
                        font,
                        rect,
                        format,
                        Color.FromArgb(235, 250, 245, 218),
                        Color.FromArgb(190, 20, 24, 20),
                        1.7f);
                }
            }
        }

        private void DrawMapHexFeatures(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null) return;
            if (chkMapShowHexFeatures != null && !chkMapShowHexFeatures.Checked) return;
            if (currentMap.Features == null || currentMap.Features.Count == 0) return;

            // Значок особенности занимает отдельный угол гекса, поэтому не спорит
            // с иконками местности, поселений, крепостей и координатами.
            foreach (HexFeatureRecord feature in GetVisibleMapFeatures(visibleWorld))
            {
                PointF center = GetHexCenter(feature.Q, feature.R);
                if (!visibleWorld.Contains(center)) continue;
                DrawHexFeatureBadge(graphics, feature, center);
            }
        }

        private void DrawHexFeatureBadge(Graphics graphics, HexFeatureRecord feature, PointF center)
        {
            RectangleF badge = new RectangleF(center.X - 25f, center.Y - 23f, 18f, 18f);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(232, 238, 226, 174)))
            using (Pen outline = new Pen(Color.FromArgb(230, 32, 24, 12), 1.4f))
            {
                graphics.FillRectangle(fill, badge);
                graphics.DrawRectangle(outline, badge.X, badge.Y, badge.Width, badge.Height);
            }

            string iconKey = string.IsNullOrWhiteSpace(feature.IconKey) ? "feature_standing_stones" : feature.IconKey;
            Image icon = GetScaledMapImage(iconKey, 14);
            if (icon != null)
            {
                graphics.DrawImage(icon, new RectangleF(badge.X + 2f, badge.Y + 2f, 14f, 14f));
                return;
            }

            using (SolidBrush marker = new SolidBrush(Color.FromArgb(230, 30, 24, 16)))
            {
                graphics.FillEllipse(marker, badge.X + 5f, badge.Y + 5f, 8f, 8f);
            }
        }

        private void DrawMapPlaceLabels(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null || mapZoom < 0.75f) return;

            if ((chkMapShowSettlementLabels == null || chkMapShowSettlementLabels.Checked)
                && (chkMapShowSettlements == null || chkMapShowSettlements.Checked)
                && currentMap.Settlements != null)
            {
                foreach (MapSettlementRecord settlement in GetVisibleMapSettlements(visibleWorld))
                {
                    if (settlement == null) continue;
                    PointF center = GetHexCenter(settlement.Q, settlement.R);
                    if (!visibleWorld.Contains(center)) continue;
                    if (ShouldHideNearbyMapLabel("settlement:" + settlement.Id, settlement.Q, settlement.R)) continue;

                    string label = settlement.Name + " " + MapSettlementRecord.ToRoman(settlement.MarketClass);
                    DrawMapPlaceLabel(graphics, label, new PointF(center.X, center.Y + 24f), false);
                }
            }

            if ((chkMapShowStrongholdLabels == null || chkMapShowStrongholdLabels.Checked)
                && (chkMapShowStrongholds == null || chkMapShowStrongholds.Checked)
                && currentMap.Domains != null)
            {
                foreach (DomainRecord domain in GetVisibleMapStrongholds(visibleWorld))
                {
                    if (domain == null) continue;
                    if (!HasVisibleStronghold(domain)) continue;
                    PointF center = GetHexCenter(domain.StrongholdQ, domain.StrongholdR);
                    if (!visibleWorld.Contains(center)) continue;
                    if (ShouldHideNearbyMapLabel(StrongholdMapLabelId(domain), domain.StrongholdQ, domain.StrongholdR)) continue;

                    MapSettlementRecord settlementAtStronghold = GetSettlementAtStronghold(domain);
                    PointF labelCenter = settlementAtStronghold == null
                        ? new PointF(center.X, center.Y + 28f)
                        : new PointF(center.X + 18f, center.Y + 9f);
                    DrawMapPlaceLabel(graphics, GetStrongholdMapLabel(domain), labelCenter, false);
                }
            }
        }

        private void DrawHoveredMapPlaceLabel(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || hoveredMapLabelTarget == null || currentMap == null) return;

            PointF center = GetHexCenter(hoveredMapLabelTarget.Q, hoveredMapLabelTarget.R);
            center = new PointF(center.X + hoveredMapLabelTarget.LabelOffsetX, center.Y + hoveredMapLabelTarget.LabelOffsetY);
            if (!visibleWorld.Contains(center)) return;

            DrawMapPlaceLabel(graphics, hoveredMapLabelTarget.Name, center, true);
        }

        private void DrawMapPlaceLabel(Graphics graphics, string text, PointF center, bool hovered)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string display = text.Replace("\r", " ").Replace("\n", " ");

            using (Font font = new Font("Microsoft Sans Serif", hovered ? 8.4f : 7f, FontStyle.Bold))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                RectangleF rect = CenteredSingleLineTextBounds(graphics, display, font, center, hovered ? 134f : 112f, hovered ? 14f : 8f, hovered ? 26f : 22f);
                if (hovered)
                {
                    using (GraphicsPath path = RoundedRect(rect, 5f))
                    using (SolidBrush fill = new SolidBrush(Color.FromArgb(230, 248, 239, 196)))
                    using (Pen pen = new Pen(Color.FromArgb(230, 35, 25, 15), 1.4f))
                    {
                        graphics.FillPath(fill, path);
                        graphics.DrawPath(pen, path);
                    }
                }

                DrawOutlinedText(
                    graphics,
                    display,
                    font,
                    rect,
                    format,
                    hovered ? Color.FromArgb(42, 28, 18) : Color.White,
                    hovered ? Color.FromArgb(245, 248, 239, 196) : Color.Black,
                    hovered ? 1.8f : 2f);
            }
        }

        private GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            float diameter = radius * 2f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private bool IsSmallMapIconMode()
        {
            return chkMapUseSmallMapIcons != null && chkMapUseSmallMapIcons.Checked;
        }

        private void DrawSmallHexCornerIcon(Graphics graphics, string iconKey, PointF center)
        {
            RectangleF badge = new RectangleF(center.X + 8f, center.Y - 22f, 18f, 18f);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(220, 246, 238, 184)))
            using (Pen pen = new Pen(Color.FromArgb(230, 20, 20, 20), 1.5f))
            {
                graphics.FillEllipse(brush, badge);
                graphics.DrawEllipse(pen, badge);
            }

            DrawIconCentered(graphics, iconKey, new PointF(badge.X + badge.Width / 2f, badge.Y + badge.Height / 2f), 14);
        }

        private string GetStrongholdMapLabel(DomainRecord domain)
        {
            if (domain == null) return isEnglish ? "Stronghold VI" : "Крепость VI";
            string baseName = ExtractShortDomainName(domain.Name);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = isEnglish ? "Stronghold" : "Крепость";
            }

            baseName = TrimMapLabelPart(baseName, 18);
            return isEnglish ? baseName + "\nstronghold" : "Крепость\n" + baseName;
        }

        private MapSettlementRecord GetSettlementAtStronghold(DomainRecord domain)
        {
            if (currentMap == null || currentMap.Settlements == null || domain == null) return null;
            List<MapSettlementRecord> settlements;
            if (currentMapSettlementsByCell != null
                && currentMapSettlementsByCell.TryGetValue(CellKey(domain.StrongholdQ, domain.StrongholdR), out settlements))
            {
                MapSettlementRecord indexed = settlements.FirstOrDefault(s =>
                    (domain.StrongholdInSettlement
                        && (string.Equals(s.Id, domain.StrongholdSettlementId, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase)))
                    || (s.Q == domain.StrongholdQ && s.R == domain.StrongholdR));
                if (indexed != null) return indexed;
            }

            return currentMap.Settlements.FirstOrDefault(s =>
                (domain.StrongholdInSettlement
                    && (string.Equals(s.Id, domain.StrongholdSettlementId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase)))
                || (s.Q == domain.StrongholdQ && s.R == domain.StrongholdR));
        }

        private MapSettlementRecord GetSettlementAtCell(int q, int r)
        {
            if (currentMap == null || currentMap.Settlements == null) return null;

            List<MapSettlementRecord> settlements;
            if (currentMapSettlementsByCell != null
                && currentMapSettlementsByCell.TryGetValue(CellKey(q, r), out settlements))
            {
                return settlements.FirstOrDefault(s => s != null && s.Q == q && s.R == r);
            }

            return currentMap.Settlements.FirstOrDefault(s => s != null && s.Q == q && s.R == r);
        }

        private string GetStrongholdDisplayName(DomainRecord domain)
        {
            if (domain == null) return isEnglish ? "Stronghold" : "Крепость";
            string name = string.IsNullOrWhiteSpace(domain.StrongholdName)
                ? ""
                : domain.StrongholdName.Trim();

            if (isEnglish)
            {
                return string.IsNullOrWhiteSpace(name) ? "Stronghold" : name;
            }

            int oldSuffix = name.IndexOf(": крепость", StringComparison.OrdinalIgnoreCase);
            if (oldSuffix >= 0)
            {
                string domainPart = name.Substring(0, oldSuffix).Trim();
                string baseName = ExtractShortDomainName(domainPart);
                return string.IsNullOrWhiteSpace(baseName) ? "Крепость" : "Крепость " + baseName;
            }

            if (name.StartsWith("Крепость ", StringComparison.OrdinalIgnoreCase)) return name;
            if (!string.IsNullOrWhiteSpace(name)) return "Крепость " + ExtractShortDomainName(name);

            string fallback = ExtractShortDomainName(domain.Name);
            return string.IsNullOrWhiteSpace(fallback) ? "Крепость" : "Крепость " + fallback;
        }

        private string ExtractShortDomainName(string name)
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

        private string TrimMapLabelPart(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim();
            if (value.Length <= maxChars) return value;
            return value.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
        }

        private string GetSettlementIconKey(MapSettlementRecord settlement)
        {
            if (settlement == null) return "class6";

            // Иконка поселения зависит от расы и особого типа домена, но подпись и
            // рыночный класс остаются общими для торговли и библиотеки городов.
            int marketClass = Math.Max(1, Math.Min(6, settlement.MarketClass));
            string race = NormalizeSettlementRace(settlement.Race);
            DomainRecord domain = GetDomainForSettlement(settlement);

            if (IsClanholdSettlementRecord(settlement)
                || (domain != null && string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)))
            {
                string clanRace = domain == null
                    ? NormalizeSettlementRace(settlement.Race)
                    : NormalizeSettlementRace(domain.Race);
                if (clanRace == "Orc") return "orcclanold";
                if (clanRace == "Beastman") return "beastmanclanold";
                return "humanclanold";
            }

            if (race == "Dwarf") return "dwarfclass" + marketClass;
            if (race == "Elf") return "elvenclass" + marketClass;
            return "class" + marketClass;
        }

        private DomainRecord GetDomainForSettlement(MapSettlementRecord settlement)
        {
            if (currentMap == null || currentMap.Domains == null || settlement == null) return null;
            DomainRecord indexed;
            if (!string.IsNullOrWhiteSpace(settlement.Id)
                && currentMapDomainBySettlementId != null
                && currentMapDomainBySettlementId.TryGetValue(settlement.Id, out indexed))
            {
                return indexed;
            }

            if (currentMapDomainByHex != null && currentMapDomainByHex.TryGetValue(CellKey(settlement.Q, settlement.R), out indexed))
            {
                return indexed;
            }

            return currentMap.Domains.FirstOrDefault(d => string.Equals(d.CapitalSettlementId, settlement.Id, StringComparison.OrdinalIgnoreCase))
                ?? currentMap.Domains.FirstOrDefault(d => d.SettlementIds != null && d.SettlementIds.Contains(settlement.Id, StringComparer.OrdinalIgnoreCase))
                ?? currentMap.Domains.FirstOrDefault(d => d.Hexes != null && d.Hexes.Any(h => h.Q == settlement.Q && h.R == settlement.R));
        }

        private string GetDomainStrongholdIconKey(DomainRecord domain)
        {
            if (domain == null) return "fortress";
            if (!string.IsNullOrWhiteSpace(domain.StrongholdIconKey) && mapImages.ContainsKey(domain.StrongholdIconKey.ToLowerInvariant()))
            {
                return domain.StrongholdIconKey.ToLowerInvariant();
            }

            string race = NormalizeSettlementRace(domain.Race);
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

        private void DrawSettlementIconBackdrop(Graphics graphics, PointF center)
        {
            RectangleF back = new RectangleF(center.X - 21f, center.Y - 21f, 42f, 42f);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(210, 246, 238, 184)))
            using (Pen outline = new Pen(Color.FromArgb(210, 20, 20, 20), 2.2f))
            {
                graphics.FillEllipse(brush, back);
                graphics.DrawEllipse(outline, back);
            }
        }

        private float GetSettlementMarkerSize(int marketClass)
        {
            switch (Math.Max(1, Math.Min(6, marketClass)))
            {
                case 1: return 32f;
                case 2: return 29f;
                case 3: return 26f;
                case 4: return 23f;
                case 5: return 20f;
                default: return 17f;
            }
        }

        private Color GetSettlementMarkerColor(int marketClass)
        {
            switch (Math.Max(1, Math.Min(6, marketClass)))
            {
                case 1: return ColorTranslator.FromHtml("#4A2413");
                case 2: return ColorTranslator.FromHtml("#7A4625");
                case 3: return ColorTranslator.FromHtml("#A66A34");
                case 4: return ColorTranslator.FromHtml("#C89B52");
                case 5: return ColorTranslator.FromHtml("#D9C18E");
                default: return ColorTranslator.FromHtml("#E8DCC2");
            }
        }

        private void DrawOutlinedText(Graphics graphics, string text, Font font, RectangleF bounds, StringFormat format, Color fillColor, Color outlineColor, float outlineWidth)
        {
            if (graphics == null || string.IsNullOrWhiteSpace(text) || font == null) return;

            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            using (Pen outline = new Pen(outlineColor, outlineWidth) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
            using (SolidBrush fill = new SolidBrush(fillColor))
            {
                float emSize = graphics.DpiY * font.Size / 72f;
                path.AddString(text, font.FontFamily, (int)font.Style, emSize, bounds, format);
                graphics.DrawPath(outline, path);
                graphics.FillPath(fill, path);
            }
        }

        private RectangleF CenteredSingleLineTextBounds(Graphics graphics, string text, Font font, PointF center, float minWidth, float horizontalPadding, float height)
        {
            if (graphics == null || string.IsNullOrWhiteSpace(text) || font == null)
            {
                return new RectangleF(center.X - minWidth / 2f, center.Y - height / 2f, minWidth, height);
            }

            using (StringFormat measureFormat = new StringFormat(StringFormat.GenericTypographic))
            {
                measureFormat.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
                SizeF measured = graphics.MeasureString(text, font, int.MaxValue, measureFormat);
                float width = Math.Max(minWidth, measured.Width + horizontalPadding * 2f);
                return new RectangleF(center.X - width / 2f, center.Y - height / 2f, width, height);
            }
        }

        private void DrawIconCentered(Graphics graphics, string iconKey, PointF center, int maxSize)
        {
            Image image = GetScaledMapImage(iconKey, maxSize);
            if (image == null) return;
            RectangleF rect = new RectangleF(center.X - maxSize / 2f, center.Y - maxSize / 2f, maxSize, maxSize);
            graphics.DrawImage(image, rect);
        }

        private void DrawIconCenteredRotated(Graphics graphics, string iconKey, PointF center, int maxSize, float angleDegrees)
        {
            Image image = GetScaledMapImage(iconKey, maxSize);
            if (image == null) return;

            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
            graphics.TranslateTransform(center.X, center.Y);
            graphics.RotateTransform(angleDegrees);
            RectangleF rect = new RectangleF(-maxSize / 2f, -maxSize / 2f, maxSize, maxSize);
            graphics.DrawImage(image, rect);
            graphics.Restore(state);
        }

        private Image GetScaledMapImage(string iconKey, int maxSize)
        {
            if (string.IsNullOrWhiteSpace(iconKey) || maxSize <= 0) return null;

            string normalized = iconKey.ToLowerInvariant();
            Image source;
            if (!mapImages.TryGetValue(normalized, out source) || source == null) return null;

            // Значки хранятся крупными PNG, а на карте рисуются в мировых координатах.
            // Кэшируем их с учетом текущего зума, чтобы при приближении не растягивать
            // уже уменьшенную картинку и не терять резкость линий.
            int pixelSize = Math.Max(maxSize, (int)Math.Round(maxSize * Math.Max(1f, mapZoom) / 4f) * 4);
            pixelSize = Math.Max(maxSize, Math.Min(192, pixelSize));
            string cacheKey = normalized + "|" + maxSize + "|" + pixelSize;
            Image cached;
            if (scaledMapImages.TryGetValue(cacheKey, out cached)) return cached;

            Bitmap scaled = new Bitmap(pixelSize, pixelSize);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (IsSettlementIconKey(normalized))
                {
                    Rectangle sourceBounds = GetSettlementIconSourceBounds(normalized, source);
                    g.DrawImage(source, new Rectangle(0, 0, pixelSize, pixelSize), sourceBounds, GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(source, new Rectangle(0, 0, pixelSize, pixelSize));
                }
            }

            if (IsSettlementIconKey(normalized))
            {
                SharpenSettlementIcon(scaled);
            }

            scaledMapImages[cacheKey] = scaled;
            return scaled;
        }

        private bool IsSettlementIconKey(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey)) return false;
            return iconKey.StartsWith("class", StringComparison.OrdinalIgnoreCase)
                || iconKey.StartsWith("dwarfclass", StringComparison.OrdinalIgnoreCase)
                || iconKey.StartsWith("elvenclass", StringComparison.OrdinalIgnoreCase)
                || iconKey.StartsWith("fortress", StringComparison.OrdinalIgnoreCase)
                || iconKey.EndsWith("clanold", StringComparison.OrdinalIgnoreCase);
        }

        private Rectangle GetSettlementIconSourceBounds(string iconKey, Image source)
        {
            Rectangle cached;
            if (mapImageContentBounds.TryGetValue(iconKey, out cached)) return cached;

            Rectangle result = new Rectangle(0, 0, source.Width, source.Height);
            using (Bitmap bitmap = new Bitmap(source))
            {
                int minX = bitmap.Width;
                int minY = bitmap.Height;
                int maxX = -1;
                int maxY = -1;

                // Отсекаем пустой прозрачный/почти белый край PNG, чтобы разные наборы поселений
                // занимали одинаковую визуальную площадь внутри круглой подложки.
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        if (pixel.A < 8) continue;
                        int luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                        if (luminance >= 245) continue;

                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX >= minX && maxY >= minY)
                {
                    int contentWidth = maxX - minX + 1;
                    int contentHeight = maxY - minY + 1;
                    int padding = Math.Max(12, Math.Max(contentWidth, contentHeight) / 18);
                    int side = Math.Min(Math.Max(contentWidth, contentHeight) + padding * 2, Math.Min(bitmap.Width, bitmap.Height));
                    int centerX = (minX + maxX) / 2;
                    int centerY = (minY + maxY) / 2;
                    int left = Math.Max(0, Math.Min(bitmap.Width - side, centerX - side / 2));
                    int top = Math.Max(0, Math.Min(bitmap.Height - side, centerY - side / 2));
                    result = new Rectangle(left, top, side, side);
                }
            }

            mapImageContentBounds[iconKey] = result;
            return result;
        }

        private void SharpenSettlementIcon(Bitmap bitmap)
        {
            if (bitmap == null) return;

            // Малые settlement-иконки являются в основном черной линейной графикой.
            // Усиливаем темные пиксели один раз в кэше, не меняя исходные PNG и не
            // добавляя постоянной нагрузки в Paint.
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    if (pixel.A < 8) continue;

                    int luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                    if (luminance > 232) continue;

                    double ink = (232 - luminance) / 232.0;
                    int alpha = Math.Min(255, (int)Math.Round(pixel.A * (1.0 + ink * 0.9)));
                    int red = Math.Max(0, (int)Math.Round(pixel.R * (1.0 - ink * 0.45)));
                    int green = Math.Max(0, (int)Math.Round(pixel.G * (1.0 - ink * 0.45)));
                    int blue = Math.Max(0, (int)Math.Round(pixel.B * (1.0 - ink * 0.45)));
                    bitmap.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                }
            }
        }

        private Color GetCellBaseColor(HexCellRecord cell, int layer)
        {
            if (IsWaterCell(cell)) return WaterColor(cell.Water);

            if (layer == 2) return ElevationColor(cell.Elevation);
            if (layer == 3) return TerrainColor(cell.Terrain);
            if (layer == 1) return TerrainColor(cell.Terrain);
            return Color.FromArgb(178, 189, 145);
        }

        private Color WaterColor(string water)
        {
            switch (water)
            {
                case "Ocean": return Color.FromArgb(17, 57, 112);
                case "Sea": return Color.FromArgb(31, 101, 181);
                case "Lake": return Color.FromArgb(83, 179, 219);
                default: return Color.FromArgb(86, 86, 86);
            }
        }

        private Color TerrainColor(string terrain)
        {
            switch (terrain)
            {
                case "Rainforest": return Color.FromArgb(24, 84, 44);
                case "Savanna": return Color.FromArgb(218, 166, 93);
                case "Desert": return Color.FromArgb(218, 194, 126);
                case "Steppe": return Color.FromArgb(143, 137, 77);
                case "Scrub": return Color.FromArgb(128, 180, 87);
                case "Grasslands": return Color.FromArgb(132, 194, 92);
                case "Forest": return Color.FromArgb(54, 130, 62);
                case "Taiga": return Color.FromArgb(37, 97, 58);
                case "Tundra": return Color.FromArgb(124, 128, 99);
                case "Marsh": return Color.FromArgb(25, 91, 61);
                case "DeepForest": return Color.FromArgb(18, 75, 41);
                case "DeepTaiga": return Color.FromArgb(18, 70, 48);
                default: return Color.FromArgb(132, 194, 92);
            }
        }

        private Color ElevationColor(string elevation)
        {
            switch (elevation)
            {
                case "Hills": return Color.FromArgb(150, 202, 80);
                case "Mountains": return Color.FromArgb(130, 130, 130);
                default: return Color.FromArgb(161, 204, 104);
            }
        }

        private string GetTerrainIconKey(string terrain)
        {
            switch (terrain)
            {
                case "DeepForest": return "deepforest";
                case "DeepTaiga": return "deeotaiga";
                default: return string.IsNullOrWhiteSpace(terrain) ? null : terrain.ToLowerInvariant();
            }
        }

        private PointF GetHexCenter(int q, int r)
        {
            float x = 38f + MapHexWidth * (q + ((r & 1) == 1 ? 0.5f : 0f));
            float y = 38f + MapHexRowHeight * r;
            return new PointF(x, y);
        }

        private PointF[] GetHexPolygon(PointF center)
        {
            PointF[] points = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                points[i] = new PointF(
                    center.X + MapHexPointOffsets[i].X,
                    center.Y + MapHexPointOffsets[i].Y);
            }
            return points;
        }

        private PointF[] GetHexPolygon(PointF center, float size)
        {
            PointF[] points = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 180d * (30 + 60 * i);
                points[i] = new PointF(
                    center.X + size * (float)Math.Cos(angle),
                    center.Y + size * (float)Math.Sin(angle));
            }

            return points;
        }

        private void DrawMapLargeHexGrid(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null) return;
            if (chkMapShowLargeHexGrid == null || !chkMapShowLargeHexGrid.Checked) return;

            const int scale = 4; // 4 x 6-mile hexes = 24 miles.
            float largeSize = MapHexSize * scale;
            float margin = largeSize * 2f;
            int minR = (int)Math.Floor((visibleWorld.Top - 38f - margin) / MapHexRowHeight);
            int maxR = (int)Math.Ceiling((visibleWorld.Bottom - 38f + margin) / MapHexRowHeight);

            using (Pen pen = new Pen(Color.FromArgb(165, 18, 24, 28), 2.4f))
            {
                pen.LineJoin = LineJoin.Round;
                for (int r = FloorToMultiple(minR, scale); r <= maxR; r += scale)
                {
                    double rowShift = ((r - (r & 1)) / 2.0) + ((r & 1) == 1 ? 0.5 : 0.0);
                    int minAxialQ = (int)Math.Floor((visibleWorld.Left - 38f - margin) / MapHexWidth - rowShift);
                    int maxAxialQ = (int)Math.Ceiling((visibleWorld.Right - 38f + margin) / MapHexWidth - rowShift);

                    for (int axialQ = FloorToMultiple(minAxialQ, scale); axialQ <= maxAxialQ; axialQ += scale)
                    {
                        int offsetQ = axialQ + ((r - (r & 1)) / 2);
                        PointF center = GetHexCenter(offsetQ, r);
                        PointF[] hex = GetHexPolygon(center, largeSize);
                        if (!PolygonIntersectsBounds(hex, visibleWorld)) continue;
                        graphics.DrawPolygon(pen, hex);
                    }
                }
            }
        }

        private int FloorToMultiple(int value, int divisor)
        {
            int remainder = value % divisor;
            if (remainder < 0) remainder += divisor;
            return value - remainder;
        }

        private bool PolygonIntersectsBounds(PointF[] polygon, RectangleF bounds)
        {
            if (polygon == null || polygon.Length == 0) return false;
            float left = polygon.Min(p => p.X);
            float right = polygon.Max(p => p.X);
            float top = polygon.Min(p => p.Y);
            float bottom = polygon.Max(p => p.Y);
            return RectangleF.FromLTRB(left, top, right, bottom).IntersectsWith(bounds);
        }

        private void UpdateMapScrollSize()
        {
            if (currentMap == null || pnlHexMap == null) return;
            RectangleF worldBounds = GetMapWorldBounds();
            float padding = MapHexSize * 3f;
            int width = (int)Math.Ceiling((worldBounds.Right + padding) * mapZoom);
            int height = (int)Math.Ceiling((worldBounds.Bottom + padding) * mapZoom);
            width = Math.Max(width, pnlHexMap.ClientSize.Width + 1);
            height = Math.Max(height, pnlHexMap.ClientSize.Height + 1);
            Size newSize = new Size(width, height);
            if (pnlHexMap.AutoScrollMinSize != newSize)
            {
                pnlHexMap.AutoScrollMinSize = newSize;
            }

            ClampMapScrollToContent();
        }

        private void pnlHexMap_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Clicks > 1) return;
            if (e.Button != MouseButtons.Left) return;
            if (currentMap == null) return;

            HexCellRecord cell = FindCellAt(e.Location);
            int tool = cmbMapTool.SelectedIndex < 0 ? 0 : cmbMapTool.SelectedIndex;
            if (cell == null)
            {
                if (tool == 0)
                {
                    selectedMapCell = null;
                    SelectMapDomainInList(null);
                    SelectMapRealmInList(null);
                    UpdateMapInfoForSelection();
                    LoadDemandGridForCurrentTarget();
                }

                return;
            }

            selectedMapCell = cell;

            if (tool == 1) PlaceSettlementOnCell(cell);
            else if (tool == 2) AddMapEdgeFromClick(cell, "Road");
            else if (tool == 3) AddMapEdgeFromClick(cell, "River");
            else if (tool == 4 && !IsWaterCell(cell)) cell.Terrain = GetSelectedTerrainKey();
            else if (tool == 5 && !IsWaterCell(cell)) cell.Elevation = GetSelectedElevationKey();
            else if (tool == 6)
            {
                bool rebuiltIndexes = false;
                cell.Water = GetSelectedWaterKey();
                NormalizeWaterSurface(cell);
                if (cell.Water != "None")
                {
                    bool removedSettlements = currentMap.Settlements.RemoveAll(s => s.Q == cell.Q && s.R == cell.R) > 0;
                    bool changedDomains = RemoveCellFromAllDomains(cell, true);
                    if (changedDomains)
                    {
                        RefreshMapDomainList();
                    }

                    if (removedSettlements || changedDomains)
                    {
                        RebuildCurrentMapIndex();
                        rebuiltIndexes = true;
                    }
                }

                if (!rebuiltIndexes)
                {
                    RebuildMapFeatureLabelIndex();
                }
            }
            else if (tool == 7) EraseMapCell(cell);
            else if (tool == 8) PlaceStrongholdOnCell(cell);
            else if (tool == 9) PlaceHexFeatureOnCell(cell);

            SelectMapDomainAtCell(selectedMapCell);
            if (tool == 0) SelectMapRealmAtCell(selectedMapCell);
            UpdateMapInfoForSelection();
            UpdateMapDomainSummary();
            LoadDemandGridForCurrentTarget();
            pnlHexMap.Invalidate();
        }

        private void pnlHexMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (currentMap == null) return;

            HexCellRecord cell = FindCellAt(e.Location);
            if (cell == null) return;

            selectedMapCell = cell;
            MapSettlementRecord settlement = GetSettlementAtCell(cell.Q, cell.R);
            HexFeatureRecord feature = GetHexFeatureAtMapPoint(cell, e.Location);
            DomainRecord stronghold = GetStrongholdAtMapPoint(cell, e.Location);
            SelectMapDomainAtCell(selectedMapCell);
            SelectMapRealmAtCell(selectedMapCell);
            UpdateMapInfoForSelection();
            LoadDemandGridForCurrentTarget();
            pnlHexMap.Invalidate();

            if (feature != null)
            {
                ShowHexFeatureInfo(feature);
            }
            else if (stronghold != null)
            {
                ShowMapStrongholdInfo(stronghold);
            }
            else if (settlement != null)
            {
                ShowMapSettlementInfo(settlement);
            }
        }

        private HexCellRecord FindCellAt(Point point)
        {
            HexCellRecord best = null;
            double bestDistanceSquared = double.MaxValue;
            PointF worldPoint = ViewToWorld(point);

            int approxR = (int)Math.Round((worldPoint.Y - 38f) / MapHexRowHeight);
            int approxQ = (int)Math.Round((worldPoint.X - 38f) / MapHexWidth - (((approxR & 1) == 1) ? 0.5f : 0f));

            for (int r = approxR - 2; r <= approxR + 2; r++)
            {
                for (int q = approxQ - 2; q <= approxQ + 2; q++)
                {
                    HexCellRecord cell = GetCell(q, r);
                    if (cell == null) continue;
                    PointF center = GetHexCenter(cell.Q, cell.R);
                    double dx = worldPoint.X - center.X;
                    double dy = worldPoint.Y - center.Y;
                    double distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        best = cell;
                    }
                }
            }

            double maxDistance = MapHexSize + 4;
            return bestDistanceSquared <= maxDistance * maxDistance ? best : null;
        }

        private PointF ViewToWorld(Point point)
        {
            Point scroll = GetMapScroll();
            return new PointF(
                (point.X + scroll.X) / mapZoom,
                (point.Y + scroll.Y) / mapZoom);
        }

        private DomainRecord GetStrongholdAtMapPoint(HexCellRecord cell, Point viewPoint)
        {
            if (cell == null || currentMap == null || currentMap.Domains == null) return null;

            // Крепость может быть отдельной большой иконкой или маленькой меткой в
            // поселении; проверяем попадание именно в эту визуальную область.
            PointF worldPoint = ViewToWorld(viewPoint);
            List<DomainRecord> candidates;
            if (currentMapStrongholdsByCell == null
                || !currentMapStrongholdsByCell.TryGetValue(CellKey(cell.Q, cell.R), out candidates))
            {
                candidates = currentMap.Domains
                    .Where(d => d != null && HasVisibleStronghold(d) && d.StrongholdQ == cell.Q && d.StrongholdR == cell.R)
                    .ToList();
            }

            foreach (DomainRecord domain in candidates)
            {
                PointF center = GetHexCenter(domain.StrongholdQ, domain.StrongholdR);
                PointF clickableCenter = center;
                float radius = 26f;
                bool inSettlement = GetSettlementAtStronghold(domain) != null;

                if (IsSmallMapIconMode())
                {
                    if (inSettlement) continue;
                    clickableCenter = new PointF(center.X + 17f, center.Y - 13f);
                    radius = 12f;
                }
                else if (inSettlement)
                {
                    clickableCenter = new PointF(center.X + 18f, center.Y - 13f);
                    radius = 13f;
                }

                float dx = worldPoint.X - clickableCenter.X;
                float dy = worldPoint.Y - clickableCenter.Y;
                if (Math.Sqrt(dx * dx + dy * dy) <= radius)
                {
                    return domain;
                }
            }

            return candidates.FirstOrDefault(d => GetSettlementAtStronghold(d) == null);
        }

        private HexFeatureRecord GetHexFeatureAtMapPoint(HexCellRecord cell, Point viewPoint)
        {
            if (cell == null || currentMap == null) return null;
            if (chkMapShowHexFeatures != null && !chkMapShowHexFeatures.Checked) return null;

            List<HexFeatureRecord> features = GetHexFeaturesAtCell(cell);
            if (features.Count == 0) return null;

            PointF worldPoint = ViewToWorld(viewPoint);
            PointF center = GetHexCenter(cell.Q, cell.R);
            RectangleF bounds = new RectangleF(center.X - 25f, center.Y - 23f, 18f, 18f);
            bounds.Inflate(4f, 4f);
            if (!bounds.Contains(worldPoint)) return null;

            // Двойной клик должен открывать именно видимую метку. Если в гексе
            // несколько особенностей, данж приоритетнее обычной природной точки.
            return features
                .OrderByDescending(f => string.Equals(f.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        private void UpdateHoveredMapLabel(Point viewPoint)
        {
            MapLabelHoverTarget target = FindHoveredMapLabelTarget(viewPoint);
            bool changed = (target == null && hoveredMapLabelTarget != null)
                || (target != null && hoveredMapLabelTarget == null)
                || (target != null && hoveredMapLabelTarget != null && target.Id != hoveredMapLabelTarget.Id);

            if (!changed) return;

            hoveredMapLabelTarget = target;
            if (mapHoverToolTip != null)
            {
                mapHoverToolTip.SetToolTip(pnlHexMap, target == null ? "" : target.Name);
            }

            pnlHexMap.Invalidate();
        }

        private MapLabelHoverTarget FindHoveredMapLabelTarget(Point viewPoint)
        {
            if (currentMap == null) return null;
            HexCellRecord cell = FindCellAt(viewPoint);
            if (cell == null) return null;

            DomainRecord stronghold = GetStrongholdAtMapPoint(cell, viewPoint);
            if (stronghold != null && (chkMapShowStrongholds == null || chkMapShowStrongholds.Checked))
            {
                string name = GetStrongholdDisplayName(stronghold);
                return new MapLabelHoverTarget
                {
                    Id = StrongholdMapLabelId(stronghold),
                    Name = name,
                    Q = stronghold.StrongholdQ,
                    R = stronghold.StrongholdR,
                    LabelOffsetX = GetSettlementAtStronghold(stronghold) == null ? 0f : 18f,
                    LabelOffsetY = GetSettlementAtStronghold(stronghold) == null ? 28f : 9f,
                    SuppressNeighbors = mapZoom >= 0.75f && !IsSmallMapIconMode() && GetSettlementAtStronghold(stronghold) == null
                };
            }

            MapSettlementRecord settlement = GetSettlementAtCell(cell.Q, cell.R);
            if (settlement == null || (chkMapShowSettlements != null && !chkMapShowSettlements.Checked)) return null;

            return new MapLabelHoverTarget
            {
                Id = "settlement:" + settlement.Id,
                Name = settlement.DisplayName,
                Q = settlement.Q,
                R = settlement.R,
                LabelOffsetX = 0f,
                LabelOffsetY = 24f,
                SuppressNeighbors = true
            };
        }

        private void ClearHoveredMapLabel()
        {
            if (hoveredMapLabelTarget == null) return;
            hoveredMapLabelTarget = null;
            if (mapHoverToolTip != null) mapHoverToolTip.SetToolTip(pnlHexMap, "");
            pnlHexMap.Invalidate();
        }

        private bool ShouldHideNearbyMapLabel(string id, int q, int r)
        {
            if (hoveredMapLabelTarget == null) return false;
            if (!hoveredMapLabelTarget.SuppressNeighbors) return false;
            if (string.Equals(hoveredMapLabelTarget.Id, id, StringComparison.OrdinalIgnoreCase)) return false;
            return HexDistance(hoveredMapLabelTarget.Q, hoveredMapLabelTarget.R, q, r) <= 3;
        }

        private string StrongholdMapLabelId(DomainRecord domain)
        {
            if (domain == null) return "stronghold:";
            string id = string.IsNullOrWhiteSpace(domain.StrongholdId) ? domain.Id : domain.StrongholdId;
            return "stronghold:" + id;
        }

        private void pnlHexMap_MouseDown(object sender, MouseEventArgs e)
        {
            pnlHexMap.Focus();
            if (e.Button != MouseButtons.Right) return;

            ClearHoveredMapLabel();
            mapPanning = true;
            mapPanMouseStart = e.Location;
            mapPanScrollStart = GetMapScroll();
            pnlHexMap.Cursor = Cursors.SizeAll;
            pnlHexMap.Capture = true;
        }

        private void pnlHexMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mapPanning)
            {
                UpdateHoveredMapLabel(e.Location);
                return;
            }

            int dx = e.X - mapPanMouseStart.X;
            int dy = e.Y - mapPanMouseStart.Y;
            SetMapScroll(new Point(mapPanScrollStart.X - dx, mapPanScrollStart.Y - dy));
        }

        private void pnlHexMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            mapPanning = false;
            pnlHexMap.Capture = false;
            pnlHexMap.Cursor = Cursors.Default;
        }

        private void pnlHexMap_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }

            float oldZoom = mapZoom;
            Point scrollBefore = GetMapScroll();
            PointF worldBefore = new PointF(
                (e.Location.X + scrollBefore.X) / oldZoom,
                (e.Location.Y + scrollBefore.Y) / oldZoom);
            if (e.Delta > 0) mapZoom *= 1.15f;
            else mapZoom /= 1.15f;
            mapZoom = Math.Max(0.35f, Math.Min(3.5f, mapZoom));
            if (Math.Abs(oldZoom - mapZoom) < 0.001f) return;

            UpdateMapScrollSize();
            int desiredX = (int)Math.Round(worldBefore.X * mapZoom - e.Location.X);
            int desiredY = (int)Math.Round(worldBefore.Y * mapZoom - e.Location.Y);
            SetMapScroll(new Point(desiredX, desiredY));
            pnlHexMap.Invalidate();
        }

        private Point GetMapScroll()
        {
            return new Point(-pnlHexMap.AutoScrollPosition.X, -pnlHexMap.AutoScrollPosition.Y);
        }

        private void SetMapScroll(Point desired)
        {
            int maxX = Math.Max(0, pnlHexMap.AutoScrollMinSize.Width - pnlHexMap.ClientSize.Width);
            int maxY = Math.Max(0, pnlHexMap.AutoScrollMinSize.Height - pnlHexMap.ClientSize.Height);
            int x = Math.Max(0, Math.Min(maxX, desired.X));
            int y = Math.Max(0, Math.Min(maxY, desired.Y));
            Point current = GetMapScroll();
            if (current.X == x && current.Y == y) return;
            pnlHexMap.AutoScrollPosition = new Point(x, y);
            pnlHexMap.Invalidate();
        }

        private void ClampMapScrollToContent()
        {
            if (pnlHexMap == null) return;

            Point current = GetMapScroll();
            int currentX = current.X;
            int currentY = current.Y;
            int maxX = Math.Max(0, pnlHexMap.AutoScrollMinSize.Width - pnlHexMap.ClientSize.Width);
            int maxY = Math.Max(0, pnlHexMap.AutoScrollMinSize.Height - pnlHexMap.ClientSize.Height);
            int clampedX = Math.Max(0, Math.Min(maxX, currentX));
            int clampedY = Math.Max(0, Math.Min(maxY, currentY));
            if (clampedX != currentX || clampedY != currentY)
            {
                // После загрузки меньшей карты WinForms может сохранить старый AutoScrollPosition.
                // Принудительная обрезка не дает уехать в пустую область за реальными гексами.
                pnlHexMap.AutoScrollPosition = new Point(clampedX, clampedY);
            }
        }

        private void PlaceSettlementOnCell(HexCellRecord cell)
        {
            if (!CanPlaceSettlement(cell))
            {
                MessageBox.Show(isEnglish
                    ? "Settlements cannot be placed on water, marsh, deep forest, or deep taiga."
                    : "Поселения нельзя ставить на воде, болоте, глубоком лесу или глубокой тайге.");
                return;
            }

            MapSettlementRecord source = cmbMapSettlementLibrary.SelectedItem as MapSettlementRecord;
            if (IsNewGeneratedSettlementOption(source))
            {
                GenerateAndPlaceSettlementOnCell(cell);
                return;
            }

            if (source == null)
            {
                MessageBox.Show(isEnglish
                    ? "Save or import a settlement first."
                    : "Сначала сохраните или импортируйте поселение.");
                return;
            }

            currentMap.Settlements.RemoveAll(s => s.Q == cell.Q && s.R == cell.R);
            MapSettlementRecord placed = CloneSettlementForMap(source, cell.Q, cell.R);
            currentMap.Settlements.Add(placed);
            RebuildCurrentMapIndex();
        }
    }
}
