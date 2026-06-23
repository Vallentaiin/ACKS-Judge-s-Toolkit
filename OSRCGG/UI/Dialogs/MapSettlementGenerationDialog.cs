using System;
using System.Drawing;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class MapSettlementGenerationOptions
    {
        public string SettlementName { get; set; }
        public int MarketClass { get; set; }
        public int AgeIndex { get; set; }
        public string Race { get; set; }
        public string LandValue { get; set; }
        public int LandValueGp { get; set; }
        public bool GenerateDemands { get; set; }
        public bool ApplyNeighborInfluence { get; set; }
    }

    public sealed class MapSettlementGenerationDialog : Form
    {
        private readonly bool isEnglish;
        private readonly TextBox txtName = new TextBox();
        private readonly ComboBox cmbClass = new ComboBox();
        private readonly ComboBox cmbAge = new ComboBox();
        private readonly ComboBox cmbRace = new ComboBox();
        private readonly ComboBox cmbLandValue = new ComboBox();
        private readonly CheckBox chkGenerateDemands = new CheckBox();
        private readonly CheckBox chkNeighborInfluence = new CheckBox();

        public MapSettlementGenerationOptions Options { get; private set; }

        public MapSettlementGenerationDialog(bool isEnglish, string suggestedName, int defaultClass)
        {
            this.isEnglish = isEnglish;
            Options = null;

            Text = isEnglish ? "Generate settlement" : "Сгенерировать поселение";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 320);
            FormClosing += MapSettlementGenerationDialog_FormClosing;

            BuildUi(suggestedName, defaultClass);
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
        }

        private void BuildUi(string suggestedName, int defaultClass)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnCount = 2;
            layout.RowCount = 9;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, isEnglish ? "Name" : "Название", txtName);
            AddRow(layout, 1, isEnglish ? "Class" : "Класс", cmbClass);
            AddRow(layout, 2, isEnglish ? "Age" : "Возраст", cmbAge);
            AddRow(layout, 3, isEnglish ? "Race" : "Раса", cmbRace);
            AddRow(layout, 4, isEnglish ? "Land value" : "Ценность земли", cmbLandValue);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            txtName.Text = suggestedName;

            cmbClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClass.Items.AddRange(new object[] { "I", "II", "III", "IV", "V", "VI" });
            cmbClass.SelectedIndex = Math.Max(0, Math.Min(5, defaultClass - 1));
            cmbClass.SelectedIndexChanged += (s, e) => ApplyRaceClassLimit();

            cmbAge.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAge.Items.AddRange(isEnglish
                ? new object[] { "0-20 years", "21-100 years", "101-1,000 years", "1,001-2,000 years", "2,001+ years" }
                : new object[] { "0-20 лет", "21-100 лет", "101-1000 лет", "1001-2000 лет", "2001+ лет" });
            cmbAge.SelectedIndex = 0;

            cmbRace.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRace.Items.AddRange(isEnglish
                ? new object[] { "Human", "Dwarf", "Elf", "Human clanhold", "Orc", "Beastman" }
                : new object[] { "Человек", "Дварф", "Эльф", "Клановые люди", "Орк", "Зверолюд" });
            cmbRace.SelectedIndex = 0;
            cmbRace.SelectedIndexChanged += (s, e) => ApplyRaceClassLimit();

            cmbLandValue.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLandValue.Items.AddRange(new object[] { "3gp", "4gp", "5gp", "6gp", "7gp", "8gp", "9gp" });
            cmbLandValue.SelectedIndex = 3;

            chkGenerateDemands.Text = isEnglish ? "Generate demands from hex" : "Сгенерировать спрос по гексу";
            chkGenerateDemands.Checked = true;
            chkGenerateDemands.AutoSize = true;
            chkGenerateDemands.Dock = DockStyle.Fill;
            layout.Controls.Add(chkGenerateDemands, 0, 5);
            layout.SetColumnSpan(chkGenerateDemands, 2);

            chkNeighborInfluence.Text = isEnglish ? "Apply nearby settlement influence" : "Учесть влияние соседних поселений";
            chkNeighborInfluence.Checked = false;
            chkNeighborInfluence.AutoSize = true;
            chkNeighborInfluence.Dock = DockStyle.Fill;
            layout.Controls.Add(chkNeighborInfluence, 0, 6);
            layout.SetColumnSpan(chkNeighborInfluence, 2);

            Label hint = new Label();
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.DimGray;
            hint.Text = isEnglish
                ? "Human, orc, beastman, and human clanhold types do not change demands."
                : "Человек, орк, зверолюд и клановые люди не меняют спрос.";
            layout.Controls.Add(hint, 0, 7);
            layout.SetColumnSpan(hint, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;

            Button ok = new Button();
            ok.Text = isEnglish ? "Create" : "Создать";
            ok.Width = 90;
            UiTheme.StylePositiveButton(ok);
            ok.Click += (s, e) => Accept();

            Button cancel = new Button();
            cancel.Text = isEnglish ? "Cancel" : "Отмена";
            cancel.Width = 90;
            UiTheme.StyleNegativeButton(cancel);
            cancel.DialogResult = DialogResult.Cancel;

            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons, 0, 8);
            layout.SetColumnSpan(buttons, 2);

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(layout);
        }

        private void ApplyRaceClassLimit()
        {
            if (IsSelectedClanhold())
            {
                cmbClass.SelectedIndex = 5;
            }
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, Control editor)
        {
            Label lbl = new Label();
            lbl.AutoSize = true;
            lbl.Anchor = AnchorStyles.Left;
            lbl.Text = label;

            editor.Dock = DockStyle.Fill;

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(editor, 1, row);
        }

        private void Accept()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this,
                    isEnglish ? "Enter a settlement name." : "Введите название поселения.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Options = new MapSettlementGenerationOptions
            {
                SettlementName = name,
                MarketClass = IsSelectedClanhold() ? 6 : cmbClass.SelectedIndex + 1,
                AgeIndex = Math.Max(0, cmbAge.SelectedIndex),
                Race = SelectedRace(),
                LandValue = IsSelectedClanhold() ? "Clanhold" : "",
                LandValueGp = SelectedLandValueGp(),
                GenerateDemands = chkGenerateDemands.Checked,
                ApplyNeighborInfluence = chkNeighborInfluence.Checked
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private int SelectedLandValueGp()
        {
            string value = cmbLandValue.Text.Replace("gp", "").Trim();
            int gp;
            if (!int.TryParse(value, out gp)) gp = 6;
            return Math.Max(3, Math.Min(9, gp));
        }

        private bool IsSelectedClanhold()
        {
            return cmbRace.SelectedIndex == 3 || cmbRace.SelectedIndex == 4 || cmbRace.SelectedIndex == 5;
        }

        private string SelectedRace()
        {
            if (cmbRace.SelectedIndex == 1) return "Dwarf";
            if (cmbRace.SelectedIndex == 2) return "Elf";
            if (cmbRace.SelectedIndex == 4) return "Orc";
            if (cmbRace.SelectedIndex == 5) return "Beastman";
            return "Human";
        }

        private void MapSettlementGenerationDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK) return;

            Options = null;
            DialogResult = DialogResult.Cancel;
        }
    }
}
