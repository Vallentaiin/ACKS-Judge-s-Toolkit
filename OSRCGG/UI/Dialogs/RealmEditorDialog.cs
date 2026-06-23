using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class RealmEditorDialog : Form
    {
        private readonly bool isEnglish;
        private readonly RealmRecord source;
        private readonly TextBox txtName = new TextBox();
        private readonly ComboBox cmbTier = new ComboBox();
        private readonly TextBox txtTitleOverride = new TextBox();
        private readonly TextBox txtFemaleTitleOverride = new TextBox();
        private readonly ComboBox cmbCapital = new ComboBox();
        private readonly TextBox txtCulture = new TextBox();
        private readonly TextBox txtRuler = new TextBox();
        private readonly NumericUpDown nudRulerLevel = new NumericUpDown();
        private readonly TextBox txtNotes = new TextBox();

        public RealmRecord Realm { get; private set; }

        public RealmEditorDialog(bool isEnglish, RealmRecord realm, IEnumerable<MapSettlementRecord> settlements)
        {
            this.isEnglish = isEnglish;
            source = CloneRealm(realm ?? new RealmRecord());

            Text = isEnglish ? "Realm" : "Держава";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 430);

            BuildUi(settlements ?? Enumerable.Empty<MapSettlementRecord>());
            LoadRealm();
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
        }

        private void BuildUi(IEnumerable<MapSettlementRecord> settlements)
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 10
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, isEnglish ? "Name" : "Название", txtName);
            AddRow(layout, 1, isEnglish ? "Tier" : "Ранг", cmbTier);
            AddRow(layout, 2, isEnglish ? "Custom title" : "Свой титул", txtTitleOverride);
            AddRow(layout, 3, isEnglish ? "Female title" : "Женский титул", txtFemaleTitleOverride);
            AddRow(layout, 4, isEnglish ? "Capital" : "Столица", cmbCapital);
            AddRow(layout, 5, isEnglish ? "Culture key" : "Культура", txtCulture);
            AddRow(layout, 6, isEnglish ? "Ruler" : "Правитель", txtRuler);
            AddRow(layout, 7, isEnglish ? "Ruler level" : "Уровень правителя", nudRulerLevel);
            AddRow(layout, 8, isEnglish ? "Notes" : "Заметки", txtNotes);

            cmbTier.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTier.Items.AddRange(new object[] { "Empire", "Kingdom", "Principality", "Duchy", "County", "Viscounty", "Barony" });

            cmbCapital.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCapital.Items.Add(new SettlementItem("", isEnglish ? "(none)" : "(нет)"));
            foreach (MapSettlementRecord settlement in settlements.Where(s => s != null).OrderBy(s => s.MarketClass).ThenBy(s => s.Name))
            {
                cmbCapital.Items.Add(new SettlementItem(settlement.Id, settlement.DisplayName));
            }

            nudRulerLevel.Minimum = 0;
            nudRulerLevel.Maximum = 30;
            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            Button ok = new Button { Text = isEnglish ? "Save" : "Сохранить", Width = 96 };
            Button cancel = new Button { Text = isEnglish ? "Cancel" : "Отмена", Width = 96, DialogResult = DialogResult.Cancel };
            UiTheme.StylePositiveButton(ok);
            UiTheme.StyleNegativeButton(cancel);
            ok.Click += (s, e) => Accept();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.Controls.Add(buttons, 0, 9);
            layout.SetColumnSpan(buttons, 2);

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(layout);
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, Control editor)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 8 ? 94 : 34));
            Label lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            editor.Dock = DockStyle.Fill;
            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(editor, 1, row);
        }

        private void LoadRealm()
        {
            txtName.Text = source.Name;
            SelectComboText(cmbTier, string.IsNullOrWhiteSpace(source.Tier) ? "County" : source.Tier);
            txtTitleOverride.Text = source.TitleOverride;
            txtFemaleTitleOverride.Text = source.FemaleTitleOverride;
            SelectSettlement(source.CapitalSettlementId);
            txtCulture.Text = source.CultureKey;
            txtRuler.Text = source.RulerName;
            nudRulerLevel.Value = Math.Max(nudRulerLevel.Minimum, Math.Min(nudRulerLevel.Maximum, source.RulerLevel));
            txtNotes.Text = source.Notes;
        }

        private void Accept()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, isEnglish ? "Enter a realm name." : "Введите название державы.", Text);
                return;
            }

            RealmRecord edited = CloneRealm(source);
            edited.Name = name;
            edited.Tier = cmbTier.Text;
            edited.TitleOverride = txtTitleOverride.Text.Trim();
            edited.FemaleTitleOverride = txtFemaleTitleOverride.Text.Trim();
            edited.CapitalSettlementId = SelectedCapitalId();
            edited.CultureKey = txtCulture.Text.Trim();
            edited.RulerName = txtRuler.Text.Trim();
            edited.RulerLevel = (int)nudRulerLevel.Value;
            edited.Notes = txtNotes.Text;
            edited.UpdatedAt = DateTime.Now;
            Realm = edited;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static RealmRecord CloneRealm(RealmRecord source)
        {
            if (source == null) return new RealmRecord();
            return new RealmRecord
            {
                Id = string.IsNullOrWhiteSpace(source.Id) ? Guid.NewGuid().ToString("N") : source.Id,
                Name = source.Name,
                Tier = source.Tier,
                TitleOverride = source.TitleOverride,
                FemaleTitleOverride = source.FemaleTitleOverride,
                CultureKey = source.CultureKey,
                CapitalSettlementId = source.CapitalSettlementId,
                RulerName = source.RulerName,
                RulerLevel = source.RulerLevel,
                ColorArgb = source.ColorArgb,
                Notes = source.Notes,
                UpdatedAt = source.UpdatedAt
            };
        }

        private void SelectComboText(ComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (string.Equals(combo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void SelectSettlement(string settlementId)
        {
            for (int i = 0; i < cmbCapital.Items.Count; i++)
            {
                SettlementItem item = cmbCapital.Items[i] as SettlementItem;
                if (item != null && string.Equals(item.Id, settlementId, StringComparison.OrdinalIgnoreCase))
                {
                    cmbCapital.SelectedIndex = i;
                    return;
                }
            }

            if (cmbCapital.Items.Count > 0) cmbCapital.SelectedIndex = 0;
        }

        private string SelectedCapitalId()
        {
            SettlementItem item = cmbCapital.SelectedItem as SettlementItem;
            return item == null ? "" : item.Id;
        }

        private sealed class SettlementItem
        {
            public string Id { get; private set; }
            private readonly string label;

            public SettlementItem(string id, string label)
            {
                Id = id ?? "";
                this.label = label ?? "";
            }

            public override string ToString()
            {
                return label;
            }
        }
    }
}
