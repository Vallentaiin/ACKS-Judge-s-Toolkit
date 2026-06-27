using System.Windows.Forms;

namespace OSRCGG
{
    public class DoubleBufferedPanel : Panel
    {
        public event MouseEventHandler MouseWheelWithoutAutoScroll;

        public bool SuppressMouseWheelAutoScroll { get; set; }

        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (SuppressMouseWheelAutoScroll)
            {
                MouseWheelWithoutAutoScroll?.Invoke(this, e);
                return;
            }

            base.OnMouseWheel(e);
        }
    }
}
