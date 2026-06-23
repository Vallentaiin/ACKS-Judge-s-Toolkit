using System;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        // Команды импорта и экспорта карты/поселений отделены от редактора и отрисовки.
        private void ExportSelectedSettlementToExcel()
        {
            MapSettlementRecord record = cmbMapSettlementLibrary.SelectedItem as MapSettlementRecord;
            if (record == null)
            {
                MessageBox.Show(isEnglish ? "Select a settlement first." : "Сначала выберите поселение.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel Workbook|*.xlsx";
            dialog.FileName = FileNameHelper.MakeSafeFileName(record.Name) + ".xlsx";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                new MapWorkbookService(MerchandiseNames).SaveSettlement(dialog.FileName, record);

                MessageBox.Show(isEnglish ? "Settlement exported." : "Поселение экспортировано.");
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "Export failed: " : "Ошибка экспорта: ") + ex.Message);
            }
        }

        private void ImportSettlementFromExcel()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel Workbook|*.xlsx";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                MapSettlementRecord record = new MapWorkbookService(MerchandiseNames).LoadSettlement(dialog.FileName);
                if (string.IsNullOrWhiteSpace(record.Id) || settlementLibrary.Any(s => s.Id == record.Id))
                {
                    record.Id = Guid.NewGuid().ToString("N");
                }
                record.Q = -1;
                record.R = -1;
                settlementLibrary.Add(record);
                SaveSettlementLibrary();
                RefreshSettlementLibraryUi();
                SelectSettlementInLibrary(record.Id);
                LoadDemandGridForCurrentTarget();

                MessageBox.Show(isEnglish ? "Settlement imported." : "Поселение импортировано.");
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "Import failed: " : "Ошибка импорта: ") + ex.Message);
            }
        }

        private void ExportCurrentMapToExcel()
        {
            if (currentMap == null) return;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel Workbook|*.xlsx";
            dialog.FileName = FileNameHelper.MakeSafeFileName(string.IsNullOrWhiteSpace(currentMap.Name) ? "acks-map" : currentMap.Name) + ".xlsx";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                NormalizeMap(currentMap);
                new MapWorkbookService(MerchandiseNames).SaveMap(dialog.FileName, currentMap, MapScaleMiles);
                MessageBox.Show(isEnglish ? "Map exported." : "Карта экспортирована.");
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "Export failed: " : "Ошибка экспорта: ") + ex.Message);
            }
        }

        private void ImportMapFromExcel()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel Workbook|*.xlsx";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                HexMapRecord map = new MapWorkbookService(MerchandiseNames).LoadMap(dialog.FileName);
                NormalizeMap(map);
                currentMap = map;
                LoadMapToEditor(currentMap);
                MessageBox.Show(isEnglish ? "Map imported." : "Карта импортирована.");
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "Import failed: " : "Ошибка импорта: ") + ex.Message);
            }
        }

        private void ExportCurrentMapToPng()
        {
            if (currentMap == null) return;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PNG image|*.png";
            dialog.FileName = FileNameHelper.MakeSafeFileName(string.IsNullOrWhiteSpace(currentMap.Name) ? "acks-map" : currentMap.Name) + ".png";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                NormalizeMap(currentMap);
                RectangleF worldBounds = GetMapWorldBounds();
                float padding = MapHexSize * 2.5f;
                float exportZoom = ChoosePngExportZoom(worldBounds, padding);
                int width = Math.Max(1, (int)Math.Ceiling((worldBounds.Width + padding * 2f) * exportZoom));
                int height = Math.Max(1, (int)Math.Ceiling((worldBounds.Height + padding * 2f) * exportZoom));
                using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.FromArgb(43, 50, 45));
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
                    float oldZoom = mapZoom;
                    try
                    {
                        mapZoom = exportZoom;
                        graphics.Transform = new System.Drawing.Drawing2D.Matrix(
                            exportZoom,
                            0f,
                            0f,
                            exportZoom,
                            (-worldBounds.Left + padding) * exportZoom,
                            (-worldBounds.Top + padding) * exportZoom);

                        RectangleF visibleWorld = RectangleF.FromLTRB(
                            worldBounds.Left - padding,
                            worldBounds.Top - padding,
                            worldBounds.Right + padding,
                            worldBounds.Bottom + padding);
                        DrawCurrentMap(graphics, visibleWorld, true);
                    }
                    finally
                    {
                        mapZoom = oldZoom;
                        graphics.Restore(state);
                    }

                    bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }

                string zoomText = exportZoom.ToString("0.#", CultureInfo.InvariantCulture);
                MessageBox.Show(isEnglish
                    ? "PNG exported at " + zoomText + "x."
                    : "PNG экспортирован в масштабе " + zoomText + "x.");
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "PNG export failed: " : "Ошибка экспорта PNG: ") + ex.Message);
            }
        }

        private float ChoosePngExportZoom(RectangleF worldBounds, float padding)
        {
            // PNG остается растровым форматом: четкость при увеличении дает только больший размер экспортируемого изображения.
            float requestedZoom = Math.Max(MinPngExportZoom, Math.Min(3.5f, mapZoom));
            float bitmapWorldWidth = Math.Max(1f, worldBounds.Width + padding * 2f);
            float bitmapWorldHeight = Math.Max(1f, worldBounds.Height + padding * 2f);

            float maxByWidth = MaxPngExportDimension / bitmapWorldWidth;
            float maxByHeight = MaxPngExportDimension / bitmapWorldHeight;
            float maxByPixels = (float)Math.Sqrt(MaxPngExportPixels / (double)(bitmapWorldWidth * bitmapWorldHeight));
            float cappedZoom = Math.Min(requestedZoom, Math.Min(maxByPixels, Math.Min(maxByWidth, maxByHeight)));

            return Math.Max(1f, cappedZoom);
        }

        private RectangleF GetMapWorldBounds()
        {
            if (!currentMapWorldBounds.IsEmpty)
            {
                return currentMapWorldBounds;
            }

            if (currentMap == null || currentMap.Cells == null || currentMap.Cells.Count == 0)
            {
                return new RectangleF(0, 0, MapHexSize, MapHexSize);
            }

            RebuildMapWorldBounds();
            return currentMapWorldBounds.IsEmpty
                ? new RectangleF(0, 0, MapHexSize, MapHexSize)
                : currentMapWorldBounds;
        }
    }
}
