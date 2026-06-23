using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private sealed class ResponsiveTabLayout
        {
            public TabPage Tab { get; set; }
            public PictureBox Background { get; set; }
            public Rectangle BaseContentBounds { get; set; }
            public List<Control> TopControls { get; set; }
        }

        private readonly List<ResponsiveTabLayout> responsiveTabLayouts = new List<ResponsiveTabLayout>();
        private readonly Dictionary<Control, Rectangle> responsiveBaseBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> responsiveBaseFonts = new Dictionary<Control, float>();
        private bool responsiveTabsReady;

        private void InitializeResponsiveTabs()
        {
            if (responsiveTabsReady) return;
            responsiveTabsReady = true;

            DoubleBuffered = true;
            RegisterResponsiveTab(tabPageGenerator, pictureBox1);
            RegisterResponsiveTab(tabPageTrade, pictureBox2);
            RegisterResponsiveTab(tabPageCharacters, pictureBoxCharacters);

            foreach (ResponsiveTabLayout layout in responsiveTabLayouts)
            {
                layout.Tab.Resize += (s, e) => LayoutResponsiveTab(layout);
            }

            if (tabControl1 != null)
            {
                tabControl1.SelectedIndexChanged += (s, e) => LayoutAllResponsiveTabs();
            }

            LayoutAllResponsiveTabs();
        }

        private void RegisterResponsiveTab(TabPage tab, PictureBox background)
        {
            if (tab == null) return;

            if (background != null)
            {
                background.Dock = DockStyle.Fill;
                background.SizeMode = PictureBoxSizeMode.StretchImage;
                background.SendToBack();
            }

            List<Control> topControls = tab.Controls
                .Cast<Control>()
                .Where(c => c != background)
                .ToList();

            if (topControls.Count == 0) return;

            foreach (Control control in topControls)
            {
                CaptureResponsiveTree(control);
            }

            Rectangle contentBounds = topControls
                .Select(c => c.Bounds)
                .Aggregate(Rectangle.Union);

            responsiveTabLayouts.Add(new ResponsiveTabLayout
            {
                Tab = tab,
                Background = background,
                BaseContentBounds = contentBounds,
                TopControls = topControls
            });
        }

        private void CaptureResponsiveTree(Control control)
        {
            if (control == null || responsiveBaseBounds.ContainsKey(control)) return;

            responsiveBaseBounds[control] = control.Bounds;
            responsiveBaseFonts[control] = control.Font.SizeInPoints;

            foreach (Control child in control.Controls)
            {
                CaptureResponsiveTree(child);
            }
        }

        private void LayoutAllResponsiveTabs()
        {
            if (!responsiveTabsReady) return;

            foreach (ResponsiveTabLayout layout in responsiveTabLayouts)
            {
                LayoutResponsiveTab(layout);
            }
        }

        private void LayoutResponsiveTab(ResponsiveTabLayout layout)
        {
            if (layout == null || layout.Tab == null || layout.BaseContentBounds.Width <= 0 || layout.BaseContentBounds.Height <= 0)
            {
                return;
            }

            if (layout.Background != null)
            {
                layout.Background.SendToBack();
            }

            Size clientSize = layout.Tab.ClientSize;
            if (clientSize.Width <= 0 || clientSize.Height <= 0) return;

            float availableWidth = Math.Max(1, clientSize.Width - 24);
            float availableHeight = Math.Max(1, clientSize.Height - 24);
            float scale = Math.Min(availableWidth / layout.BaseContentBounds.Width, availableHeight / layout.BaseContentBounds.Height);
            scale = Math.Max(0.72f, Math.Min(1.55f, scale));

            int scaledWidth = (int)Math.Round(layout.BaseContentBounds.Width * scale);
            int scaledHeight = (int)Math.Round(layout.BaseContentBounds.Height * scale);
            int offsetX = (clientSize.Width - scaledWidth) / 2 - (int)Math.Round(layout.BaseContentBounds.Left * scale);
            int offsetY = (clientSize.Height - scaledHeight) / 2 - (int)Math.Round(layout.BaseContentBounds.Top * scale);

            layout.Tab.SuspendLayout();
            try
            {
                foreach (Control control in layout.TopControls)
                {
                    LayoutResponsiveControl(control, scale, offsetX, offsetY, true);
                }
            }
            finally
            {
                layout.Tab.ResumeLayout(false);
            }

            if (layout.Background != null)
            {
                layout.Background.Invalidate();
            }
        }

        private void LayoutResponsiveControl(Control control, float scale, int offsetX, int offsetY, bool topLevel)
        {
            if (control == null || !responsiveBaseBounds.ContainsKey(control)) return;

            Rectangle baseBounds = responsiveBaseBounds[control];
            bool tabPageManagedByTabControl = control is TabPage && control.Parent is TabControl;
            if (!tabPageManagedByTabControl)
            {
                int x = (int)Math.Round(baseBounds.X * scale) + (topLevel ? offsetX : 0);
                int y = (int)Math.Round(baseBounds.Y * scale) + (topLevel ? offsetY : 0);
                int width = Math.Max(1, (int)Math.Round(baseBounds.Width * scale));
                int height = Math.Max(1, (int)Math.Round(baseBounds.Height * scale));
                control.Bounds = new Rectangle(x, y, width, height);
            }

            ApplyResponsiveFont(control, scale);

            foreach (Control child in control.Controls)
            {
                LayoutResponsiveControl(child, scale, 0, 0, false);
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                grid.RowTemplate.Height = Math.Max(18, (int)Math.Round(22 * scale));
            }
        }

        private void ApplyResponsiveFont(Control control, float scale)
        {
            if (control == null || !responsiveBaseFonts.ContainsKey(control)) return;

            float baseSize = responsiveBaseFonts[control];
            float targetSize = Math.Max(7f, Math.Min(18f, baseSize * scale));
            if (Math.Abs(control.Font.SizeInPoints - targetSize) < 0.1f) return;

            control.Font = new Font(control.Font.FontFamily, targetSize, control.Font.Style, GraphicsUnit.Point);
        }
    }
}
