using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public class TransparentPictureBox : PictureBox
    {
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            PictureBox background = Parent == null
                ? null
                : Parent.Controls
                    .OfType<PictureBox>()
                    .FirstOrDefault(control => control.Name == "pictureBox1" && control.Image != null);

            if (background != null)
            {
                Rectangle target = new Rectangle(
                    background.Left - Left,
                    background.Top - Top,
                    background.Width,
                    background.Height);

                pevent.Graphics.DrawImage(background.Image, target);
                return;
            }

            base.OnPaintBackground(pevent);
        }
    }
}
