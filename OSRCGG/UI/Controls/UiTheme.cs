using System;
using System.Drawing;
using System.Windows.Forms;

namespace OSRCGG
{
    internal static class UiTheme
    {
        public const string FontFamily = "Microsoft Sans Serif";
        public const float FontSize = 9f;
        public const string TransparentSurfaceTag = "TransparentThemeSurface";
        public const string WhiteFieldTag = "WhiteThemeField";

        public static readonly Color TextColor = Color.FromArgb(0x3F, 0x3F, 0x3F);
        public static readonly Color FieldColor = Color.FromArgb(0xDA, 0xA4, 0x64);
        public static readonly Color Accent1Color = Color.FromArgb(0xDE, 0xC3, 0x84);
        public static readonly Color Accent2Color = Color.FromArgb(0xE8, 0xDD, 0xB4);
        public static readonly Color PositiveButtonColor = Accent1Color;
        public static readonly Color NegativeButtonColor = FieldColor;
        public static readonly Color NeutralButtonColor = Accent2Color;

        public static Font CreateFont(FontStyle style)
        {
            return new Font(FontFamily, FontSize, style, GraphicsUnit.Point);
        }

        public static void StylePositiveButton(Button button)
        {
            StyleCommandButton(button, PositiveButtonColor);
        }

        public static void StyleNegativeButton(Button button)
        {
            StyleCommandButton(button, NegativeButtonColor);
        }

        public static void StyleCommandButton(Button button, Color color)
        {
            if (button == null) return;

            button.BackColor = color;
            button.ForeColor = TextColor;
            button.Font = CreateFont(FontStyle.Bold);
            button.FlatStyle = FlatStyle.Standard;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleInput(Control control)
        {
            if (control == null) return;
            control.BackColor = FieldColor;
            control.ForeColor = TextColor;
        }

        public static void ApplyThemeColors(Control root)
        {
            if (root == null) return;

            ApplyControlTheme(root);

            foreach (Control child in root.Controls)
            {
                ApplyThemeColors(child);
            }
        }

        public static void ApplyUniformFonts(Control root)
        {
            if (root == null) return;

            ApplyControlFont(root);

            DataGridView grid = root as DataGridView;
            if (grid != null)
            {
                ApplyGridFonts(grid);
            }

            foreach (Control child in root.Controls)
            {
                ApplyUniformFonts(child);
            }
        }

        private static void ApplyControlFont(Control control)
        {
            if (control == null || IsCustomDrawnButton(control as Button)) return;

            FontStyle style = control.Font == null ? FontStyle.Regular : control.Font.Style;
            control.Font = CreateFont(style);
        }

        private static void ApplyControlTheme(Control control)
        {
            if (control == null || IsCustomDrawnButton(control as Button)) return;

            if (string.Equals(control.Tag as string, TransparentSurfaceTag, StringComparison.OrdinalIgnoreCase))
            {
                control.BackColor = Color.Transparent;
                control.ForeColor = TextColor;
                return;
            }

            if (string.Equals(control.Tag as string, WhiteFieldTag, StringComparison.OrdinalIgnoreCase))
            {
                control.BackColor = Color.White;
                control.ForeColor = TextColor;
                return;
            }

            Form form = control as Form;
            if (form != null)
            {
                form.BackColor = Accent2Color;
                form.ForeColor = TextColor;
                return;
            }

            TabPage tabPage = control as TabPage;
            if (tabPage != null)
            {
                tabPage.BackColor = Accent2Color;
                tabPage.ForeColor = TextColor;
                return;
            }

            Label label = control as Label;
            if (label != null)
            {
                label.ForeColor = TextColor;
                if (label.BackColor != Color.Transparent)
                {
                    label.BackColor = Color.Transparent;
                }
                return;
            }

            Button button = control as Button;
            if (button != null)
            {
                if (button.BackColor == SystemColors.Control
                    || button.BackColor == Color.Empty
                    || button.BackColor == Color.Transparent)
                {
                    button.BackColor = NeutralButtonColor;
                }
                button.ForeColor = TextColor;
                button.UseVisualStyleBackColor = false;
                return;
            }

            TextBoxBase textBox = control as TextBoxBase;
            if (textBox != null)
            {
                StyleInput(textBox);
                textBox.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            ComboBox comboBox = control as ComboBox;
            if (comboBox != null)
            {
                StyleInput(comboBox);
                return;
            }

            ListBox listBox = control as ListBox;
            if (listBox != null)
            {
                StyleInput(listBox);
                return;
            }

            NumericUpDown numeric = control as NumericUpDown;
            if (numeric != null)
            {
                StyleInput(numeric);
                return;
            }

            TrackBar trackBar = control as TrackBar;
            if (trackBar != null)
            {
                trackBar.BackColor = Accent2Color;
                trackBar.ForeColor = TextColor;
                return;
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                ApplyGridColors(grid);
                return;
            }

            CheckBox checkBox = control as CheckBox;
            if (checkBox != null)
            {
                checkBox.ForeColor = TextColor;
                checkBox.BackColor = Color.Transparent;
                checkBox.UseVisualStyleBackColor = false;
                return;
            }

            RadioButton radioButton = control as RadioButton;
            if (radioButton != null)
            {
                radioButton.ForeColor = TextColor;
                radioButton.BackColor = Color.Transparent;
                radioButton.UseVisualStyleBackColor = false;
                return;
            }

            if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel || control is GroupBox)
            {
                if (!string.Equals(control.Name, "pnlHexMap", StringComparison.OrdinalIgnoreCase))
                {
                    control.BackColor = Accent2Color;
                }
                control.ForeColor = TextColor;
            }
        }

        private static bool IsCustomDrawnButton(Button button)
        {
            // Квадратные инструменты карты рисуют иконки, подписи и рамку самостоятельно.
            return button != null && button.FlatStyle == FlatStyle.Flat && button.Tag != null;
        }

        private static void ApplyGridFonts(DataGridView grid)
        {
            Font regular = CreateFont(FontStyle.Regular);
            Font bold = CreateFont(FontStyle.Bold);
            grid.Font = regular;
            grid.DefaultCellStyle.Font = regular;
            grid.RowsDefaultCellStyle.Font = regular;
            grid.AlternatingRowsDefaultCellStyle.Font = regular;
            grid.ColumnHeadersDefaultCellStyle.Font = bold;
            grid.RowHeadersDefaultCellStyle.Font = regular;
        }

        public static void ApplyGridColors(DataGridView grid)
        {
            if (grid == null) return;

            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Accent2Color;
            grid.GridColor = TextColor;
            grid.DefaultCellStyle.BackColor = Accent2Color;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = FieldColor;
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.RowsDefaultCellStyle.BackColor = Accent2Color;
            grid.RowsDefaultCellStyle.ForeColor = TextColor;
            grid.RowsDefaultCellStyle.SelectionBackColor = FieldColor;
            grid.RowsDefaultCellStyle.SelectionForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF1, 0xE8, 0xC8);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = FieldColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextColor;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Accent1Color;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Accent1Color;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;
            grid.RowHeadersDefaultCellStyle.BackColor = Accent1Color;
            grid.RowHeadersDefaultCellStyle.ForeColor = TextColor;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Accent1Color;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = TextColor;
        }
    }
}
