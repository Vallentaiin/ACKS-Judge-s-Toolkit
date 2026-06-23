using System;
using System.Drawing;
using System.Windows.Forms;

namespace OSRCGG
{
    internal class ThemedTrackBar : TrackBar
    {
        private const int WmPaint = 0x000F;

        public ThemedTrackBar()
        {
            TickStyle = TickStyle.None;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            TickStyle = TickStyle.None;
        }

        protected override void OnValueChanged(EventArgs e)
        {
            base.OnValueChanged(e);
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WmPaint)
            {
                DrawThemedTicks();
            }
        }

        private void DrawThemedTicks()
        {
            if (!IsHandleCreated || Orientation != Orientation.Horizontal || Maximum <= Minimum)
            {
                return;
            }

            int left = 12;
            int right = Math.Max(left + 1, Width - 13);
            int top = Math.Max(2, Height - 16);
            int bottom = Math.Min(Height - 4, top + 4);
            int frequency = Math.Max(1, TickFrequency);

            using (Graphics graphics = CreateGraphics())
            using (Pen pen = new Pen(UiTheme.TextColor))
            {
                for (int value = Minimum; value <= Maximum; value += frequency)
                {
                    DrawTick(graphics, pen, value, left, right, top, bottom);
                }

                if ((Maximum - Minimum) % frequency != 0)
                {
                    DrawTick(graphics, pen, Maximum, left, right, top, bottom);
                }
            }
        }

        private void DrawTick(Graphics graphics, Pen pen, int value, int left, int right, int top, int bottom)
        {
            double ratio = (value - Minimum) / (double)(Maximum - Minimum);
            int x = left + (int)Math.Round((right - left) * ratio);
            graphics.DrawLine(pen, x, top, x, bottom);
        }
    }
}
