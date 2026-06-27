using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        // Генерация названий карты работает поверх модели и не должна смешиваться с Paint-слоем.
        private void GenerateNamesForCurrentMap()
        {
            if (currentMap == null) return;
            if (regionNameService == null)
            {
                regionNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            }

            string seed = (currentMap.Name ?? "") + "|" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            Random random = new Random(seed.GetHashCode());
            RegionMapGenerator generator = new RegionMapGenerator(regionNameService);
            generator.GenerateFeatureNamesForMap(currentMap, GetSelectedMapCultureKey(), !isEnglish, random);
            GenerateMissingRealmNames(random);
            NormalizeMap(currentMap);
            RebuildMapFeatureLabelIndex();
            pnlHexMap.Invalidate();

            MessageBox.Show(isEnglish
                ? "Map feature names generated."
                : "Названия природных объектов сгенерированы.");
        }

        private string GetSelectedMapCultureKey()
        {
            if (currentMap != null && currentMap.Realms != null)
            {
                RealmRecord realm = currentMap.Realms.FirstOrDefault(r => r != null && !string.IsNullOrWhiteSpace(r.CultureKey));
                if (realm != null) return realm.CultureKey;
            }

            return "english";
        }

        private void GenerateMissingRealmNames(Random random)
        {
            if (currentMap == null || currentMap.Realms == null || regionNameService == null) return;
            HashSet<string> used = new HashSet<string>(currentMap.Realms
                .Where(r => r != null && !IsPlaceholderRealmName(r.Name))
                .Select(r => r.Name), StringComparer.OrdinalIgnoreCase);

            foreach (RealmRecord realm in currentMap.Realms.Where(r => r != null && IsPlaceholderRealmName(r.Name)))
            {
                string capitalName = "";
                MapSettlementRecord capital = currentMap.Settlements.FirstOrDefault(s => s.Id == realm.CapitalSettlementId);
                if (capital != null) capitalName = capital.Name;
                string culture = string.IsNullOrWhiteSpace(realm.CultureKey) ? GetSelectedMapCultureKey() : realm.CultureKey;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    string name = regionNameService.GenerateRealmName(random, culture, capitalName, realm.Tier, !isEnglish);
                    if (used.Add(name))
                    {
                        realm.Name = name;
                        break;
                    }
                }
            }
        }

        private bool IsPlaceholderRealmName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            return name.IndexOf("unnamed", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("безымян", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void pnlHexMap_Paint(object sender, PaintEventArgs e)
        {
            if (currentMap == null) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            RectangleF visibleWorld = GetVisibleWorldBounds(e.ClipRectangle);

            System.Drawing.Drawing2D.GraphicsState state = e.Graphics.Save();
            e.Graphics.Transform = new System.Drawing.Drawing2D.Matrix(
                mapZoom,
                0f,
                0f,
                mapZoom,
                pnlHexMap.AutoScrollPosition.X,
                pnlHexMap.AutoScrollPosition.Y);

            DrawCurrentMap(e.Graphics, visibleWorld, false);
            e.Graphics.Restore(state);
        }
    }
}
