using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    internal enum MapHexFeaturePlacementMode
    {
        Natural,
        EmptyDungeon,
        GenerateDungeon,
        LinkDungeon
    }

    internal sealed class MapHexFeaturePlacementOptions
    {
        public MapHexFeaturePlacementMode Mode { get; set; }
        public HexFeatureDefinition NaturalFeature { get; set; }
        public DungeonTypeDefinition DungeonType { get; set; }
        public DungeonRecord LibraryDungeon { get; set; }
        public string Name { get; set; }
        public string DungeonSize { get; set; }
        public int DungeonLevel { get; set; }
    }

    internal sealed class MapHexFeaturePlacementDialog : Form
    {
        private readonly bool isEnglish;
        private readonly TextBox txtName = new TextBox();
        private readonly RadioButton rbNatural = new RadioButton();
        private readonly RadioButton rbEmptyDungeon = new RadioButton();
        private readonly RadioButton rbGenerateDungeon = new RadioButton();
        private readonly RadioButton rbLinkDungeon = new RadioButton();
        private readonly ComboBox cmbNaturalFeature = new ComboBox();
        private readonly ComboBox cmbDungeonType = new ComboBox();
        private readonly ComboBox cmbDungeonSize = new ComboBox();
        private readonly NumericUpDown nudDungeonLevel = new NumericUpDown();
        private readonly ComboBox cmbDungeonLibrary = new ComboBox();

        public MapHexFeaturePlacementOptions Options { get; private set; }

        public MapHexFeaturePlacementDialog(
            bool isEnglish,
            IEnumerable<HexFeatureDefinition> naturalFeatures,
            IEnumerable<DungeonTypeDefinition> dungeonTypes,
            IEnumerable<DungeonRecord> dungeonLibrary)
        {
            this.isEnglish = isEnglish;

            Text = isEnglish ? "Place hex feature" : "Поставить особенность";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 440);
            MinimumSize = new Size(516, 479);

            BuildUi(naturalFeatures, dungeonTypes, dungeonLibrary);
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
        }

        private void BuildUi(
            IEnumerable<HexFeatureDefinition> naturalFeatures,
            IEnumerable<DungeonTypeDefinition> dungeonTypes,
            IEnumerable<DungeonRecord> dungeonLibrary)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnCount = 2;
            layout.RowCount = 12;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            rbNatural.Text = isEnglish ? "Natural feature" : "Природная особенность";
            rbEmptyDungeon.Text = isEnglish ? "Empty dungeon marker" : "Пустой маркер данжа";
            rbGenerateDungeon.Text = isEnglish ? "Generate dungeon" : "Сгенерировать данж";
            rbLinkDungeon.Text = isEnglish ? "Link from library" : "Привязать из библиотеки";
            foreach (RadioButton radio in new[] { rbNatural, rbEmptyDungeon, rbGenerateDungeon, rbLinkDungeon })
            {
                radio.AutoSize = true;
                radio.CheckedChanged += (s, e) => UpdateMode();
            }

            FlowLayoutPanel modePanel = new FlowLayoutPanel();
            modePanel.Dock = DockStyle.Fill;
            modePanel.FlowDirection = FlowDirection.TopDown;
            modePanel.WrapContents = false;
            modePanel.Padding = new Padding(0, 4, 0, 0);
            modePanel.Controls.Add(rbNatural);
            modePanel.Controls.Add(rbEmptyDungeon);
            modePanel.Controls.Add(rbGenerateDungeon);
            modePanel.Controls.Add(rbLinkDungeon);
            layout.Controls.Add(modePanel, 0, 0);
            layout.SetColumnSpan(modePanel, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));

            AddRow(layout, 1, isEnglish ? "Name" : "Название", txtName);
            AddRow(layout, 2, isEnglish ? "Feature" : "Особенность", cmbNaturalFeature);
            AddRow(layout, 3, isEnglish ? "Dungeon type" : "Тип данжа", cmbDungeonType);
            AddRow(layout, 4, isEnglish ? "Dungeon size" : "Размер данжа", cmbDungeonSize);
            AddRow(layout, 5, isEnglish ? "Danger level" : "Уровень опасности", nudDungeonLevel);
            AddRow(layout, 6, isEnglish ? "Library dungeon" : "Данж из библиотеки", cmbDungeonLibrary);

            FillNaturalFeatures(naturalFeatures);
            FillDungeonTypes(dungeonTypes);
            FillDungeonSizes();
            FillDungeonLibrary(dungeonLibrary);

            Label hint = new Label();
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.DimGray;
            hint.Text = isEnglish
                ? "The marker is placed on the clicked hex. Dungeon markers open from the map with double-click."
                : "Метка ставится на выбранный гекс. Данж открывается с карты двойным кликом.";
            layout.Controls.Add(hint, 0, 7);
            layout.SetColumnSpan(hint, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;

            Button ok = new Button();
            ok.Text = isEnglish ? "Place" : "Поставить";
            ok.Width = 100;
            UiTheme.StylePositiveButton(ok);
            ok.Click += (s, e) => Accept();

            Button cancel = new Button();
            cancel.Text = isEnglish ? "Cancel" : "Отмена";
            cancel.Width = 100;
            cancel.DialogResult = DialogResult.Cancel;
            UiTheme.StyleNegativeButton(cancel);

            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons, 0, 8);
            layout.SetColumnSpan(buttons, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(layout);

            rbNatural.Checked = true;
            if (cmbDungeonLibrary.Items.Count == 0)
            {
                rbLinkDungeon.Enabled = false;
                rbLinkDungeon.Text += isEnglish ? " (empty)" : " (пусто)";
            }
            UpdateMode();
        }

        private void FillNaturalFeatures(IEnumerable<HexFeatureDefinition> naturalFeatures)
        {
            cmbNaturalFeature.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (HexFeatureDefinition definition in (naturalFeatures ?? Enumerable.Empty<HexFeatureDefinition>()).OrderBy(f => f.Subtype))
            {
                cmbNaturalFeature.Items.Add(new DialogOption<HexFeatureDefinition>(
                    definition,
                    isEnglish ? definition.Subtype : DungeonCatalog.LocalizeFeatureSubtype(definition.Subtype, true)));
            }
            if (cmbNaturalFeature.Items.Count > 0) cmbNaturalFeature.SelectedIndex = 0;
        }

        private void FillDungeonTypes(IEnumerable<DungeonTypeDefinition> dungeonTypes)
        {
            cmbDungeonType.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (DungeonTypeDefinition definition in dungeonTypes ?? Enumerable.Empty<DungeonTypeDefinition>())
            {
                cmbDungeonType.Items.Add(new DialogOption<DungeonTypeDefinition>(
                    definition,
                    isEnglish ? definition.Name : DungeonCatalog.LocalizeDungeonType(definition.Name, true)));
            }
            if (cmbDungeonType.Items.Count > 0) cmbDungeonType.SelectedIndex = 0;
        }

        private void FillDungeonSizes()
        {
            cmbDungeonSize.DropDownStyle = ComboBoxStyle.DropDownList;
            AddDungeonSize("Lair", isEnglish ? "Lair" : "Логово");
            AddDungeonSize("Small", isEnglish ? "Small" : "Малый");
            AddDungeonSize("Standard", isEnglish ? "Standard" : "Стандартный");
            AddDungeonSize("Large", isEnglish ? "Large" : "Большой");
            AddDungeonSize("Megadungeon", isEnglish ? "Megadungeon" : "Мегаданж");
            cmbDungeonSize.SelectedIndex = 2;

            nudDungeonLevel.Minimum = 1;
            nudDungeonLevel.Maximum = DungeonCatalog.MaxDungeonLevel;
            nudDungeonLevel.Value = 1;
        }

        private void AddDungeonSize(string value, string label)
        {
            cmbDungeonSize.Items.Add(new DialogOption<string>(value, label));
        }

        private void FillDungeonLibrary(IEnumerable<DungeonRecord> dungeonLibrary)
        {
            cmbDungeonLibrary.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (DungeonRecord dungeon in (dungeonLibrary ?? Enumerable.Empty<DungeonRecord>()).Where(d => d != null).OrderBy(d => d.Name))
            {
                cmbDungeonLibrary.Items.Add(new DialogOption<DungeonRecord>(dungeon, dungeon.DisplayName));
            }
            if (cmbDungeonLibrary.Items.Count > 0) cmbDungeonLibrary.SelectedIndex = 0;
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

        private void UpdateMode()
        {
            bool natural = rbNatural.Checked;
            bool generatedDungeon = rbGenerateDungeon.Checked;
            bool linkedDungeon = rbLinkDungeon.Checked;
            bool dungeon = rbEmptyDungeon.Checked || generatedDungeon || linkedDungeon;

            cmbNaturalFeature.Enabled = natural;
            cmbDungeonType.Enabled = dungeon && !linkedDungeon;
            cmbDungeonSize.Enabled = generatedDungeon || rbEmptyDungeon.Checked;
            nudDungeonLevel.Enabled = generatedDungeon || rbEmptyDungeon.Checked;
            cmbDungeonLibrary.Enabled = linkedDungeon;
        }

        private void Accept()
        {
            MapHexFeaturePlacementOptions options = new MapHexFeaturePlacementOptions();
            options.Name = txtName.Text.Trim();
            options.DungeonLevel = (int)nudDungeonLevel.Value;
            options.DungeonSize = SelectedValue(cmbDungeonSize, "Standard");

            if (rbNatural.Checked)
            {
                options.Mode = MapHexFeaturePlacementMode.Natural;
                options.NaturalFeature = SelectedValue<HexFeatureDefinition>(cmbNaturalFeature);
                if (options.NaturalFeature == null)
                {
                    ShowValidation(isEnglish ? "Choose a feature." : "Выберите особенность.");
                    return;
                }
            }
            else if (rbLinkDungeon.Checked)
            {
                options.Mode = MapHexFeaturePlacementMode.LinkDungeon;
                options.LibraryDungeon = SelectedValue<DungeonRecord>(cmbDungeonLibrary);
                if (options.LibraryDungeon == null)
                {
                    ShowValidation(isEnglish ? "Choose a dungeon from the library." : "Выберите данж из библиотеки.");
                    return;
                }
            }
            else
            {
                options.Mode = rbGenerateDungeon.Checked
                    ? MapHexFeaturePlacementMode.GenerateDungeon
                    : MapHexFeaturePlacementMode.EmptyDungeon;
                options.DungeonType = SelectedValue<DungeonTypeDefinition>(cmbDungeonType);
                if (options.DungeonType == null)
                {
                    ShowValidation(isEnglish ? "Choose a dungeon type." : "Выберите тип данжа.");
                    return;
                }
            }

            Options = options;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowValidation(string message)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private T SelectedValue<T>(ComboBox combo)
        {
            DialogOption<T> item = combo == null ? null : combo.SelectedItem as DialogOption<T>;
            return item == null ? default(T) : item.Value;
        }

        private string SelectedValue(ComboBox combo, string fallback)
        {
            DialogOption<string> item = combo == null ? null : combo.SelectedItem as DialogOption<string>;
            return item == null || string.IsNullOrWhiteSpace(item.Value) ? fallback : item.Value;
        }

        private sealed class DialogOption<T>
        {
            public T Value { get; private set; }
            private readonly string label;

            public DialogOption(T value, string label)
            {
                Value = value;
                this.label = label;
            }

            public override string ToString()
            {
                return label ?? "";
            }
        }
    }
}
