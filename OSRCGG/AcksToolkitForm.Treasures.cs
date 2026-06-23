using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private TabPage tabPageTreasures;
        private ComboBox cmbTreasureTable;
        private ComboBox cmbTreasureType;
        private CheckBox chkTreasureUseXpTarget;
        private NumericUpDown nudTreasureXp;
        private NumericUpDown nudTreasureSeed;
        private NumericUpDown nudTreasureCount;
        private ComboBox cmbTreasureSubtype;
        private ComboBox cmbTreasureSubKind;
        private ComboBox cmbTreasureMagicKind;
        private TextBox txtTreasureOutput;
        private Button btnTreasureNewSeed;
        private Button btnTreasureGenerate;
        private Button btnTreasureGenerateSubtype;
        private Button btnTreasureGenerateMagic;
        private Button btnTreasureClear;

        private void InitializeTreasureTab()
        {
            if (tabControl1 == null || tabPageTreasures != null) return;

            tabPageTreasures = new TabPage(isEnglish ? "Treasures" : "Сокровища");
            tabControl1.TabPages.Add(tabPageTreasures);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildTreasureControlPanel(), 0, 0);

            txtTreasureOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9f, FontStyle.Regular, GraphicsUnit.Point)
            };
            root.Controls.Add(txtTreasureOutput, 1, 0);

            tabPageTreasures.Controls.Add(root);
            UpdateTreasureLanguage();
        }

        private Control BuildTreasureControlPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 1,
                RowCount = 0,
                Padding = new Padding(0, 0, 8, 0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddTreasureHeader(panel, "lblTreasureHoardHeader");
            AddTreasureLabel(panel, "lblTreasureTableMode");
            cmbTreasureTable = AddTreasureCombo(panel);
            RefreshTreasureTableCombo();

            AddTreasureLabel(panel, "lblTreasureType");
            cmbTreasureType = AddTreasureCombo(panel);
            RefreshTreasureTypeCombo();

            chkTreasureUseXpTarget = new CheckBox
            {
                Dock = DockStyle.Top,
                Height = 24
            };
            panel.Controls.Add(chkTreasureUseXpTarget);

            AddTreasureLabel(panel, "lblTreasureXp");
            nudTreasureXp = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = 1,
                Maximum = 1000000,
                Value = 250,
                ThousandsSeparator = true
            };
            panel.Controls.Add(nudTreasureXp);

            AddTreasureLabel(panel, "lblTreasureSeed");
            nudTreasureSeed = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = 0,
                Maximum = 999999999,
                Value = NextDungeonSeedValue()
            };
            panel.Controls.Add(nudTreasureSeed);

            btnTreasureNewSeed = AddTreasureButton(panel, UiTheme.NeutralButtonColor);
            btnTreasureNewSeed.Click += (s, e) => nudTreasureSeed.Value = NextDungeonSeedValue();

            btnTreasureGenerate = AddTreasureButton(panel, UiTheme.PositiveButtonColor);
            btnTreasureGenerate.Click += (s, e) => GenerateTreasureHoardFromUi();

            AddTreasureSpacer(panel);
            AddTreasureHeader(panel, "lblTreasureSubtypeHeader");
            AddTreasureLabel(panel, "lblTreasureSubtype");
            cmbTreasureSubtype = AddTreasureCombo(panel);
            cmbTreasureSubtype.SelectedIndexChanged += (s, e) => RefreshTreasureSubKindCombo();

            AddTreasureLabel(panel, "lblTreasureSubKind");
            cmbTreasureSubKind = AddTreasureCombo(panel);
            AddTreasureLabel(panel, "lblTreasureCount");
            nudTreasureCount = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = 1,
                Maximum = 100,
                Value = 1
            };
            panel.Controls.Add(nudTreasureCount);

            btnTreasureGenerateSubtype = AddTreasureButton(panel, UiTheme.PositiveButtonColor);
            btnTreasureGenerateSubtype.Click += (s, e) => GenerateTreasureSubtypeFromUi();

            AddTreasureSpacer(panel);
            AddTreasureHeader(panel, "lblTreasureMagicHeader");
            AddTreasureLabel(panel, "lblTreasureMagicKind");
            cmbTreasureMagicKind = AddTreasureCombo(panel);
            btnTreasureGenerateMagic = AddTreasureButton(panel, UiTheme.PositiveButtonColor);
            btnTreasureGenerateMagic.Click += (s, e) => GenerateTreasureMagicFromUi();

            AddTreasureSpacer(panel);
            btnTreasureClear = AddTreasureButton(panel, UiTheme.NeutralButtonColor);
            btnTreasureClear.Click += (s, e) => txtTreasureOutput.Clear();

            return panel;
        }

        private void AddTreasureHeader(TableLayoutPanel panel, string name)
        {
            Label label = new Label
            {
                Name = name,
                Dock = DockStyle.Top,
                Height = 24,
                Font = UiTheme.CreateFont(FontStyle.Bold)
            };
            panel.Controls.Add(label);
        }

        private void AddTreasureLabel(TableLayoutPanel panel, string name)
        {
            Label label = new Label
            {
                Name = name,
                Dock = DockStyle.Top,
                Height = 18,
                Margin = new Padding(0, 2, 0, 1)
            };
            panel.Controls.Add(label);
        }

        private ComboBox AddTreasureCombo(TableLayoutPanel panel)
        {
            ComboBox combo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 24,
                Margin = new Padding(0, 0, 0, 6)
            };
            panel.Controls.Add(combo);
            return combo;
        }

        private Button AddTreasureButton(TableLayoutPanel panel, Color color)
        {
            Button button = new Button
            {
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 4, 0, 4)
            };
            UiTheme.StyleCommandButton(button, color);
            panel.Controls.Add(button);
            return button;
        }

        private void AddTreasureSpacer(TableLayoutPanel panel)
        {
            Panel spacer = new Panel { Dock = DockStyle.Top, Height = 12 };
            panel.Controls.Add(spacer);
        }

        private void UpdateTreasureLanguage()
        {
            if (tabPageTreasures == null) return;
            tabPageTreasures.Text = isEnglish ? "Treasures" : "Сокровища";
            SetNamedText(tabPageTreasures, "lblTreasureHoardHeader", isEnglish ? "Treasure type tables" : "Таблицы сокровищ");
            SetNamedText(tabPageTreasures, "lblTreasureSubtypeHeader", isEnglish ? "Treasure sub-types" : "Подтипы сокровищ");
            SetNamedText(tabPageTreasures, "lblTreasureMagicHeader", isEnglish ? "Magic items" : "Магические предметы");
            SetNamedText(tabPageTreasures, "lblTreasureTableMode", isEnglish ? "Table" : "Таблица");
            SetNamedText(tabPageTreasures, "lblTreasureType", "Treasure Type");
            SetNamedText(tabPageTreasures, "lblTreasureXp", isEnglish ? "Monster XP" : "XP монстров");
            SetNamedText(tabPageTreasures, "lblTreasureSeed", "Seed");
            SetNamedText(tabPageTreasures, "lblTreasureSubtype", isEnglish ? "Sub-type" : "Подтип");
            SetNamedText(tabPageTreasures, "lblTreasureSubKind", isEnglish ? "Kind" : "Вид");
            SetNamedText(tabPageTreasures, "lblTreasureCount", isEnglish ? "Count" : "Количество");
            SetNamedText(tabPageTreasures, "lblTreasureMagicKind", isEnglish ? "Magic table" : "Таблица магии");

            if (chkTreasureUseXpTarget != null) chkTreasureUseXpTarget.Text = isEnglish ? "Pick TT by monster XP x4" : "Подобрать TT по XP монстров x4";
            if (btnTreasureNewSeed != null) btnTreasureNewSeed.Text = isEnglish ? "New seed" : "Новый seed";
            if (btnTreasureGenerate != null) btnTreasureGenerate.Text = isEnglish ? "Generate hoard" : "Сгенерировать набор";
            if (btnTreasureGenerateSubtype != null) btnTreasureGenerateSubtype.Text = isEnglish ? "Generate sub-type" : "Сгенерировать подтип";
            if (btnTreasureGenerateMagic != null) btnTreasureGenerateMagic.Text = isEnglish ? "Generate magic item" : "Сгенерировать магический предмет";
            if (btnTreasureClear != null) btnTreasureClear.Text = isEnglish ? "Clear output" : "Очистить вывод";

            RefreshTreasureTableCombo();
            RefreshTreasureTypeCombo();
            RefreshTreasureSubtypeCombo();
            RefreshTreasureMagicKindCombo();
        }

        private void RefreshTreasureTableCombo()
        {
            if (cmbTreasureTable == null) return;
            string selected = SelectedTreasureValue(cmbTreasureTable);
            cmbTreasureTable.Items.Clear();
            cmbTreasureTable.Items.Add(new TreasureOptionItem("Classic", TreasureGenerator.FormatTableMode(TreasureTableMode.Classic, !isEnglish)));
            cmbTreasureTable.Items.Add(new TreasureOptionItem("Heroic", TreasureGenerator.FormatTableMode(TreasureTableMode.Heroic, !isEnglish)));
            SelectTreasureValue(cmbTreasureTable, string.IsNullOrWhiteSpace(selected) ? "Classic" : selected);
        }

        private void RefreshTreasureTypeCombo()
        {
            if (cmbTreasureType == null) return;
            string selected = SelectedTreasureValue(cmbTreasureType);
            cmbTreasureType.Items.Clear();
            foreach (TreasureTypeSummary summary in TreasureCatalog.Summaries)
            {
                cmbTreasureType.Items.Add(new TreasureOptionItem(
                    summary.TreasureType,
                    "TT " + summary.TreasureType
                    + " - " + TreasureGenerator.FormatTreasureKind(summary.Category, !isEnglish)
                    + (isEnglish ? " avg " : " средн. ")
                    + TreasureGenerator.FormatGp(summary.AverageValueGp, !isEnglish)));
            }

            SelectTreasureValue(cmbTreasureType, string.IsNullOrWhiteSpace(selected) ? "A" : selected);
        }

        private void RefreshTreasureSubtypeCombo()
        {
            if (cmbTreasureSubtype == null) return;
            string selected = SelectedTreasureValue(cmbTreasureSubtype);
            cmbTreasureSubtype.Items.Clear();
            cmbTreasureSubtype.Items.Add(new TreasureOptionItem("Gems", isEnglish ? "Gems" : "Самоцветы"));
            cmbTreasureSubtype.Items.Add(new TreasureOptionItem("Jewelry", isEnglish ? "Jewelry" : "Украшения"));
            cmbTreasureSubtype.Items.Add(new TreasureOptionItem("Special", isEnglish ? "Special Treasures" : "Особые сокровища"));
            SelectTreasureValue(cmbTreasureSubtype, string.IsNullOrWhiteSpace(selected) ? "Gems" : selected);
            RefreshTreasureSubKindCombo();
        }

        private void RefreshTreasureSubKindCombo()
        {
            if (cmbTreasureSubKind == null) return;
            string subtype = SelectedTreasureValue(cmbTreasureSubtype);
            string selected = SelectedTreasureValue(cmbTreasureSubKind);
            cmbTreasureSubKind.Items.Clear();
            if (subtype == "Jewelry")
            {
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Trinket", isEnglish ? "Trinkets" : "Безделушки"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Jewelry", isEnglish ? "Jewelry" : "Украшения"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Regalia", isEnglish ? "Regalia" : "Регалии"));
            }
            else if (subtype == "Special")
            {
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("cp", isEnglish ? "per 1,000cp (10gp)" : "на 1,000 мм (10 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("sp", isEnglish ? "per 1,000sp (100gp)" : "на 1,000 см (100 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("ep", isEnglish ? "per 1,000ep (500gp)" : "на 1,000 эм (500 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("gp", isEnglish ? "per 1,000gp" : "на 1,000 зм"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("pp", isEnglish ? "per 1,000pp (10,000gp)" : "на 1,000 пм (10,000 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("ornamental", isEnglish ? "per ornamental (30gp)" : "на орнаментальный самоцвет (30 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("gem", isEnglish ? "per gem (200gp)" : "на самоцвет (200 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("brilliant", isEnglish ? "per brilliant (4,000gp)" : "на бриллиант (4,000 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("trinket", isEnglish ? "per trinket (225gp)" : "на безделушку (225 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("jewelry", isEnglish ? "per jewelry (1,000gp)" : "на украшение (1,000 зм)"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("regalia", isEnglish ? "per regalia (11,000gp)" : "на регалию (11,000 зм)"));
            }
            else
            {
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Ornamental", isEnglish ? "Ornamentals" : "Орнаментальные"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Gem", isEnglish ? "Gems" : "Самоцветы"));
                cmbTreasureSubKind.Items.Add(new TreasureOptionItem("Brilliant", isEnglish ? "Brilliants" : "Бриллианты"));
            }

            SelectTreasureValue(cmbTreasureSubKind, selected);
            if (cmbTreasureSubKind.SelectedIndex < 0 && cmbTreasureSubKind.Items.Count > 0) cmbTreasureSubKind.SelectedIndex = 0;
        }

        private void RefreshTreasureMagicKindCombo()
        {
            if (cmbTreasureMagicKind == null) return;
            string selected = SelectedTreasureValue(cmbTreasureMagicKind);
            cmbTreasureMagicKind.Items.Clear();
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Any", isEnglish ? "Any (by current table)" : "Любой (по выбранной таблице)"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Common", isEnglish ? "Common" : "Обычный"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Uncommon", isEnglish ? "Uncommon" : "Необычный"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Rare", isEnglish ? "Rare" : "Редкий"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Very Rare", isEnglish ? "Very Rare" : "Очень редкий"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Legendary", isEnglish ? "Legendary" : "Легендарный"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Potion", isEnglish ? "Potion" : "Зелье"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Ring", isEnglish ? "Ring" : "Кольцо"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Scroll", isEnglish ? "Scroll" : "Свиток"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Implement", isEnglish ? "Implement" : "Жезл/посох/палочка"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Miscellaneous Item", isEnglish ? "Miscellaneous item" : "Прочий предмет"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Sword", isEnglish ? "Sword" : "Меч"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Weapon", isEnglish ? "Miscellaneous weapon" : "Прочее оружие"));
            cmbTreasureMagicKind.Items.Add(new TreasureOptionItem("Armor", isEnglish ? "Armor" : "Броня"));
            SelectTreasureValue(cmbTreasureMagicKind, string.IsNullOrWhiteSpace(selected) ? "Any" : selected);
        }

        private void GenerateTreasureHoardFromUi()
        {
            int seed;
            Random random = CurrentTreasureRandom(out seed);
            TreasureTableMode mode = SelectedTreasureMode();
            TreasureHoardResult result;
            TreasureGenerator generator = new TreasureGenerator();
            if (chkTreasureUseXpTarget != null && chkTreasureUseXpTarget.Checked)
            {
                int targetGp = (int)nudTreasureXp.Value * 4;
                result = generator.GenerateForTargetValue(targetGp, mode, "", random);
            }
            else
            {
                result = generator.Generate(new TreasureGenerationOptions
                {
                    TableMode = mode,
                    TreasureType = SelectedTreasureValue(cmbTreasureType),
                    RussianOutput = !isEnglish
                }, random);
            }

            AppendTreasureOutput(generator.Format(result, !isEnglish, seed));
            AdvanceTreasureSeed();
        }

        private void GenerateTreasureSubtypeFromUi()
        {
            TreasureGenerator generator = new TreasureGenerator();
            int seed;
            Random random = CurrentTreasureRandom(out seed);
            string subtype = SelectedTreasureValue(cmbTreasureSubtype);
            string kind = SelectedTreasureValue(cmbTreasureSubKind);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(isEnglish ? "Treasure sub-type:" : "Подтип сокровищ:");
            builder.AppendLine("Seed: " + seed.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < (int)nudTreasureCount.Value; i++)
            {
                TreasureEntry entry;
                if (subtype == "Jewelry")
                {
                    entry = generator.RollJewelry(ParseJewelryKind(kind), random);
                }
                else if (subtype == "Special")
                {
                    entry = generator.RollSpecialTreasure(kind, random);
                }
                else
                {
                    entry = generator.RollGem(ParseGemKind(kind), random);
                }

                builder.AppendLine("- " + generator.FormatEntry(entry, !isEnglish));
            }

            AppendTreasureOutput(builder.ToString());
            AdvanceTreasureSeed();
        }

        private void GenerateTreasureMagicFromUi()
        {
            TreasureGenerator generator = new TreasureGenerator();
            int seed;
            Random random = CurrentTreasureRandom(out seed);
            TreasureTableMode mode = SelectedTreasureMode();
            string kind = SelectedTreasureValue(cmbTreasureMagicKind);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(isEnglish ? "Magic items:" : "Магические предметы:");
            builder.AppendLine("Seed: " + seed.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < (int)nudTreasureCount.Value; i++)
            {
                string item = generator.RollMagicItem(mode, kind, random);
                builder.AppendLine("- " + generator.FormatEntry(new TreasureEntry
                {
                    Category = "Magic Items",
                    Description = item
                }, !isEnglish));
            }

            AppendTreasureOutput(builder.ToString());
            AdvanceTreasureSeed();
        }

        private Random CurrentTreasureRandom(out int seed)
        {
            seed = nudTreasureSeed == null ? Environment.TickCount : (int)nudTreasureSeed.Value;
            return seed == 0 ? new Random() : new Random(seed);
        }

        private void AdvanceTreasureSeed()
        {
            if (nudTreasureSeed == null) return;
            nudTreasureSeed.Value = NextDungeonSeedValue();
        }

        private TreasureTableMode SelectedTreasureMode()
        {
            return string.Equals(SelectedTreasureValue(cmbTreasureTable), "Heroic", StringComparison.OrdinalIgnoreCase)
                ? TreasureTableMode.Heroic
                : TreasureTableMode.Classic;
        }

        private static TreasureGemKind ParseGemKind(string kind)
        {
            if (string.Equals(kind, "Ornamental", StringComparison.OrdinalIgnoreCase)) return TreasureGemKind.Ornamental;
            if (string.Equals(kind, "Brilliant", StringComparison.OrdinalIgnoreCase)) return TreasureGemKind.Brilliant;
            return TreasureGemKind.Gem;
        }

        private static TreasureJewelryKind ParseJewelryKind(string kind)
        {
            if (string.Equals(kind, "Trinket", StringComparison.OrdinalIgnoreCase)) return TreasureJewelryKind.Trinket;
            if (string.Equals(kind, "Regalia", StringComparison.OrdinalIgnoreCase)) return TreasureJewelryKind.Regalia;
            return TreasureJewelryKind.Jewelry;
        }

        private void AppendTreasureOutput(string text)
        {
            if (txtTreasureOutput == null) return;
            if (!string.IsNullOrWhiteSpace(txtTreasureOutput.Text)) txtTreasureOutput.AppendText(Environment.NewLine + Environment.NewLine);
            txtTreasureOutput.AppendText(text.TrimEnd() + Environment.NewLine);
        }

        private static string SelectedTreasureValue(ComboBox combo)
        {
            TreasureOptionItem item = combo == null ? null : combo.SelectedItem as TreasureOptionItem;
            return item == null ? "" : item.Value;
        }

        private static void SelectTreasureValue(ComboBox combo, string value)
        {
            if (combo == null) return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                TreasureOptionItem item = combo.Items[i] as TreasureOptionItem;
                if (item != null && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private sealed class TreasureOptionItem
        {
            public string Value { get; private set; }
            public string Label { get; private set; }

            public TreasureOptionItem(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
