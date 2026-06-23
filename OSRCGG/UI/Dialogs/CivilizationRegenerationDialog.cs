using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class CivilizationRegenerationDialog : Form
    {
        private readonly bool isEnglish;
        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly bool hasExistingSettlements;
        private readonly bool hasExistingStrongholds;
        private readonly bool hasExistingDomains;
        private readonly List<NameCultureInfo> cultures;

        private readonly TextBox txtSeed = new TextBox();
        private readonly Button btnSeed = new Button();
        private readonly ComboBox cmbCulture = new ComboBox();
        private readonly ComboBox cmbCivilization = new ComboBox();
        private readonly ComboBox cmbRealmScale = new ComboBox();
        private readonly ComboBox cmbLandValue = new ComboBox();
        private readonly ComboBox cmbHumanRealmScale = new ComboBox();
        private readonly ComboBox cmbDwarvenRealmScale = new ComboBox();
        private readonly ComboBox cmbElvenRealmScale = new ComboBox();
        private readonly ComboBox cmbHumanClanRealmScale = new ComboBox();
        private readonly ComboBox cmbOrcRealmScale = new ComboBox();
        private readonly ComboBox cmbBeastmanRealmScale = new ComboBox();
        private readonly ComboBox cmbTransitionalRealmScale = new ComboBox();

        private readonly CheckBox chkUseLayerSettings = new CheckBox();
        private readonly CheckBox chkRivers = new CheckBox();
        private readonly CheckBox chkFeatureNames = new CheckBox();
        private readonly CheckBox chkSettlements = new CheckBox();
        private readonly CheckBox chkStrongholds = new CheckBox();
        private readonly CheckBox chkDomains = new CheckBox();
        private readonly CheckBox chkRoads = new CheckBox();
        private readonly CheckBox chkRealms = new CheckBox();
        private readonly CheckBox chkRulers = new CheckBox();
        private readonly CheckBox chkHexFeatures = new CheckBox();
        private readonly CheckBox chkDungeons = new CheckBox();
        private readonly CheckBox chkSpecialSettlements = new CheckBox();
        private readonly CheckBox chkDwarvenDomains = new CheckBox();
        private readonly CheckBox chkElvenDomains = new CheckBox();
        private readonly CheckBox chkClanDomains = new CheckBox();
        private readonly CheckBox chkTransitionalDomains = new CheckBox();
        private readonly CheckBox chkDwarvenNames = new CheckBox();
        private readonly CheckBox chkElvenNames = new CheckBox();
        private readonly CheckBox chkClanNames = new CheckBox();
        private readonly CheckBox chkTransitionalNames = new CheckBox();
        private readonly CheckBox chkUseSpecialWeights = new CheckBox();

        private readonly GroupBox grpLayerSettings = new GroupBox();
        private readonly GroupBox grpSpecialWeights = new GroupBox();
        private readonly Label lblRiverFrequency = new Label();
        private readonly Label lblSettlementDensity = new Label();
        private readonly Label lblDomainCoverage = new Label();
        private readonly Label lblRealmCount = new Label();
        private readonly Label lblLandValue = new Label();
        private readonly Label lblSpecialDomainPercent = new Label();
        private readonly Label lblDwarvenWeight = new Label();
        private readonly Label lblElvenWeight = new Label();
        private readonly Label lblClanWeight = new Label();
        private readonly Label lblTransitionalWeight = new Label();

        private readonly NumericUpDown nudRiverFrequency = new NumericUpDown();
        private readonly NumericUpDown nudSettlementDensity = new NumericUpDown();
        private readonly NumericUpDown nudDomainCoverage = new NumericUpDown();
        private readonly NumericUpDown nudRealmCount = new NumericUpDown();
        private readonly NumericUpDown nudSpecialDomainPercent = new NumericUpDown();
        private readonly NumericUpDown nudDwarvenDomains = new NumericUpDown();
        private readonly NumericUpDown nudElvenDomains = new NumericUpDown();
        private readonly NumericUpDown nudClanDomains = new NumericUpDown();
        private readonly NumericUpDown nudTransitionalDomains = new NumericUpDown();

        private bool applyingOptions;
        private static RegionGenerationOptions lastAcceptedOptions;

        public RegionGenerationOptions Options { get; private set; }

        public CivilizationRegenerationDialog(
            bool isEnglish,
            NameGenerationService nameService,
            HexMapRecord map)
        {
            this.isEnglish = isEnglish;
            mapWidth = map == null || map.Width <= 0 ? 24 : map.Width;
            mapHeight = map == null || map.Height <= 0 ? 18 : map.Height;
            hasExistingSettlements = map != null && map.Settlements != null && map.Settlements.Any();
            hasExistingStrongholds = map != null && map.Domains != null && map.Domains.Any(HasVisibleStronghold);
            hasExistingDomains = map != null && map.Domains != null && map.Domains.Any();
            cultures = nameService == null ? new List<NameCultureInfo>() : nameService.GetCultures(isEnglish);

            Text = L("Regenerate map layers", "Перегенерация слоев карты");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(700, 720);

            BuildUi();
            ApplyInitialOptions(lastAcceptedOptions);
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
        }

        private void BuildUi()
        {
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage layers = new TabPage(L("Layers", "Слои"));
            TabPage specialDomains = new TabPage(L("Special domains", "Особые домены"));
            layers.Controls.Add(BuildLayersPage());
            specialDomains.Controls.Add(BuildSpecialDomainsPage());
            tabs.TabPages.Add(layers);
            tabs.TabPages.Add(specialDomains);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };
            Button cancel = new Button { Text = L("Cancel", "Отмена"), Width = 110, Height = 30, DialogResult = DialogResult.Cancel };
            Button ok = new Button { Text = L("Regenerate", "Перегенерировать"), Width = 170, Height = 30 };
            UiTheme.StyleNegativeButton(cancel);
            UiTheme.StylePositiveButton(ok);
            ok.Click += (s, e) => Accept();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(tabs);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private Control BuildLayersPage()
        {
            // Перегенерация намеренно не показывает размер, климат и распределение воды:
            // эти параметры принадлежат базовому ландшафту, который здесь остается как есть.
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 164));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            GroupBox main = new GroupBox { Text = L("Common", "Общее"), Dock = DockStyle.Fill };
            TableLayoutPanel mainLayout = CreateGrid(1, 4);
            main.Controls.Add(mainLayout);
            AddRow(mainLayout, 0, L("Seed", "Seed"), BuildSeedPanel());
            AddRow(mainLayout, 1, L("Culture", "Культура"), cmbCulture);
            AddRow(mainLayout, 2, L("Civilization", "Освоенность"), cmbCivilization);
            AddRow(mainLayout, 3, L("Realm size profile", "Профиль размеров держав"), cmbRealmScale);

            FillCultures();
            Fill(cmbCivilization,
                new[] { "Borderlands", "Civilized", "Wild" },
                new[] { "Пограничье", "Цивилизованные земли", "Дикие земли" },
                0);
            Fill(cmbRealmScale,
                new[] { "Balanced", "ManySmall", "FewLarge", "OneState" },
                new[] { "Смешанные размеры", "Больше малых государств", "Больше крупных государств", "Одно государство" },
                0);

            grpLayerSettings.Text = L("Layer parameters", "Параметры слоев");
            grpLayerSettings.Dock = DockStyle.Fill;
            TableLayoutPanel parameterLayout = CreateGrid(1, 6);
            grpLayerSettings.Controls.Add(parameterLayout);
            chkUseLayerSettings.Text = L("Use layer parameters", "Использовать параметры слоев");
            chkUseLayerSettings.AutoSize = true;
            chkUseLayerSettings.Dock = DockStyle.Fill;
            chkUseLayerSettings.CheckedChanged += (s, e) => UpdateDependencies();
            parameterLayout.Controls.Add(chkUseLayerSettings, 0, 0);
            parameterLayout.SetColumnSpan(chkUseLayerSettings, 2);
            ConfigureNumber(nudRiverFrequency, 0, 100, 35);
            ConfigureNumber(nudSettlementDensity, 0, 100, 35);
            ConfigureNumber(nudDomainCoverage, 0, 100, 45);
            ConfigureNumber(nudRealmCount, 1, 20, 3);
            Fill(cmbLandValue,
                new[] { "Fixed6", "DomainWide", "PerHex" },
                new[] { "Фиксированные 6 gp", "3d3 на домен", "3d3 на гекс" },
                0);
            AddRow(parameterLayout, 1, L("Rivers %", "Реки %"), nudRiverFrequency, lblRiverFrequency);
            AddRow(parameterLayout, 2, L("Settlements %", "Поселения %"), nudSettlementDensity, lblSettlementDensity);
            AddRow(parameterLayout, 3, L("Domain coverage %", "Домены %"), nudDomainCoverage, lblDomainCoverage);
            AddRow(parameterLayout, 4, L("Realm count", "Держав"), nudRealmCount, lblRealmCount);
            AddRow(parameterLayout, 5, L("Land value", "Ценность земли"), cmbLandValue, lblLandValue);

            GroupBox layers = new GroupBox { Text = L("Regenerate", "Что перегенерировать"), Dock = DockStyle.Fill };
            TableLayoutPanel layerLayout = CreateGrid(2, 6);
            layers.Controls.Add(layerLayout);
            ConfigureCheck(chkRivers, L("Rivers", "Реки"), false);
            ConfigureCheck(chkFeatureNames, L("Feature names", "Названия объектов"), false);
            ConfigureCheck(chkSettlements, L("Settlements", "Поселения"), false);
            ConfigureCheck(chkStrongholds, L("Strongholds", "Крепости"), false);
            ConfigureCheck(chkDomains, L("Domains", "Домены"), false);
            ConfigureCheck(chkRoads, L("Roads", "Дороги"), false);
            ConfigureCheck(chkRealms, L("Realms", "Державы"), false);
            ConfigureCheck(chkRulers, L("Rulers", "Правители"), false);
            ConfigureCheck(chkHexFeatures, L("Hex features", "Особенности гексов"), false);
            ConfigureCheck(chkDungeons, L("Dungeons", "Данжи"), false);
            ConfigureCheck(chkSpecialSettlements, L("Special settlements without domains", "Особые поселения без доменов"), false);
            layerLayout.Controls.Add(chkRivers, 0, 0);
            layerLayout.Controls.Add(chkFeatureNames, 2, 0);
            layerLayout.Controls.Add(chkSettlements, 0, 1);
            layerLayout.Controls.Add(chkStrongholds, 2, 1);
            layerLayout.Controls.Add(chkDomains, 0, 2);
            layerLayout.Controls.Add(chkRoads, 2, 2);
            layerLayout.Controls.Add(chkRealms, 0, 3);
            layerLayout.Controls.Add(chkRulers, 2, 3);
            layerLayout.Controls.Add(chkHexFeatures, 0, 4);
            layerLayout.Controls.Add(chkDungeons, 2, 4);
            layerLayout.Controls.Add(chkSpecialSettlements, 0, 5);
            layerLayout.SetColumnSpan(chkSpecialSettlements, 4);

            foreach (CheckBox checkBox in new[] { chkRivers, chkFeatureNames, chkSettlements, chkStrongholds, chkDomains, chkRoads, chkRealms, chkRulers, chkHexFeatures, chkDungeons, chkSpecialSettlements })
            {
                checkBox.CheckedChanged += (s, e) => UpdateDependencies();
            }

            root.Controls.Add(main, 0, 0);
            root.Controls.Add(grpLayerSettings, 0, 1);
            root.Controls.Add(layers, 0, 2);
            return root;
        }

        private Control BuildSpecialDomainsPage()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Top;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Height = 650;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 258));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));

            GroupBox domainTypes = new GroupBox { Text = L("Generated special domains", "Генерируемые особые домены"), Dock = DockStyle.Fill };
            FlowLayoutPanel typeChecks = CreateCheckPanel();
            ConfigureCheck(chkDwarvenDomains, L("Dwarven domains", "Дварфийские домены"), false);
            ConfigureCheck(chkElvenDomains, L("Elven domains", "Эльфийские домены"), false);
            ConfigureCheck(chkClanDomains, L("Clanholds", "Клановые домены"), false);
            ConfigureCheck(chkTransitionalDomains, L("Transitional domains", "Переходные домены"), false);
            typeChecks.Controls.Add(chkDwarvenDomains);
            typeChecks.Controls.Add(chkElvenDomains);
            typeChecks.Controls.Add(chkClanDomains);
            typeChecks.Controls.Add(chkTransitionalDomains);
            domainTypes.Controls.Add(typeChecks);

            GroupBox names = new GroupBox { Text = L("Special-domain naming", "Имена особых доменов"), Dock = DockStyle.Fill };
            FlowLayoutPanel nameChecks = CreateCheckPanel();
            ConfigureCheck(chkDwarvenNames, L("Dwarven names", "Дварфийские имена"), true);
            ConfigureCheck(chkElvenNames, L("Elven names", "Эльфийские имена"), true);
            ConfigureCheck(chkClanNames, L("Clan names", "Клановые имена"), true);
            ConfigureCheck(chkTransitionalNames, L("Transitional names", "Переходные имена"), false);
            nameChecks.Controls.Add(chkDwarvenNames);
            nameChecks.Controls.Add(chkElvenNames);
            nameChecks.Controls.Add(chkClanNames);
            nameChecks.Controls.Add(chkTransitionalNames);
            names.Controls.Add(nameChecks);

            GroupBox profiles = new GroupBox { Text = L("Realm profiles by domain type", "Профили держав по типам доменов"), Dock = DockStyle.Fill };
            TableLayoutPanel profileLayout = CreateGrid(1, 7);
            profiles.Controls.Add(profileLayout);
            AddRealmProfileRow(profileLayout, 0, L("Human realms", "Люди"), cmbHumanRealmScale);
            AddRealmProfileRow(profileLayout, 1, L("Dwarven realms", "Дварфы"), cmbDwarvenRealmScale);
            AddRealmProfileRow(profileLayout, 2, L("Elven realms", "Эльфы"), cmbElvenRealmScale);
            AddRealmProfileRow(profileLayout, 3, L("Human clanholds", "Клановые люди"), cmbHumanClanRealmScale);
            AddRealmProfileRow(profileLayout, 4, L("Orc clanholds", "Орки"), cmbOrcRealmScale);
            AddRealmProfileRow(profileLayout, 5, L("Beastman clanholds", "Зверолюды"), cmbBeastmanRealmScale);
            AddRealmProfileRow(profileLayout, 6, L("Transitional domains", "Переходные"), cmbTransitionalRealmScale);

            grpSpecialWeights.Text = L("Special-domain weights", "Вес особых доменов");
            grpSpecialWeights.Dock = DockStyle.Fill;
            TableLayoutPanel weightLayout = CreateGrid(2, 4);
            grpSpecialWeights.Controls.Add(weightLayout);
            chkUseSpecialWeights.Text = L("Use special weights", "Использовать веса особых доменов");
            chkUseSpecialWeights.AutoSize = true;
            chkUseSpecialWeights.Dock = DockStyle.Fill;
            chkUseSpecialWeights.CheckedChanged += (s, e) => UpdateDependencies();
            weightLayout.Controls.Add(chkUseSpecialWeights, 0, 0);
            weightLayout.SetColumnSpan(chkUseSpecialWeights, 4);
            ConfigureNumber(nudSpecialDomainPercent, 0, 100, 12);
            ConfigureNumber(nudDwarvenDomains, 0, 100, 25);
            ConfigureNumber(nudElvenDomains, 0, 100, 25);
            ConfigureNumber(nudClanDomains, 0, 100, 30);
            ConfigureNumber(nudTransitionalDomains, 0, 100, 20);
            AddWeightRow(weightLayout, 1, L("Special chance %", "Шанс особых %"), nudSpecialDomainPercent, lblSpecialDomainPercent);
            AddWeightRow(weightLayout, 2, L("Dwarven weight", "Вес дварфов"), nudDwarvenDomains, lblDwarvenWeight);
            AddWeightRow(weightLayout, 3, L("Elven weight", "Вес эльфов"), nudElvenDomains, lblElvenWeight);
            AddWeightRow(weightLayout, 5, L("Clanhold weight", "Вес клановых"), nudClanDomains, lblClanWeight);
            AddWeightRow(weightLayout, 6, L("Transitional weight", "Вес переходных"), nudTransitionalDomains, lblTransitionalWeight);

            foreach (CheckBox checkBox in new[] { chkDwarvenDomains, chkElvenDomains, chkClanDomains, chkTransitionalDomains, chkDwarvenNames, chkElvenNames, chkClanNames, chkTransitionalNames })
            {
                checkBox.CheckedChanged += (s, e) => UpdateDependencies();
            }

            root.Controls.Add(domainTypes, 0, 0);
            root.Controls.Add(names, 0, 1);
            root.Controls.Add(profiles, 0, 2);
            root.Controls.Add(grpSpecialWeights, 0, 3);
            panel.Controls.Add(root);
            return panel;
        }

        private Control BuildSeedPanel()
        {
            txtSeed.Text = DateTime.Now.ToString("yyyyMMddHHmmss");
            txtSeed.Width = 280;
            btnSeed.Text = L("New", "Новый");
            btnSeed.Width = 76;
            UiTheme.StyleCommandButton(btnSeed, UiTheme.PositiveButtonColor);
            btnSeed.Click += (s, e) => txtSeed.Text = DateTime.Now.ToString("yyyyMMddHHmmss");

            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            panel.Controls.Add(txtSeed);
            panel.Controls.Add(btnSeed);
            return panel;
        }

        private FlowLayoutPanel CreateCheckPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };
        }

        private TableLayoutPanel CreateGrid(int columns, int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(8);
            layout.ColumnCount = columns * 2;
            layout.RowCount = rows;
            for (int i = 0; i < columns; i++)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            }

            for (int i = 0; i < rows; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            return layout;
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, Control editor)
        {
            AddRow(layout, row, label, editor, null);
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, Control editor, Label reusableLabel)
        {
            int rows = Math.Max(1, layout.RowCount);
            int pair = Math.Min(layout.ColumnCount / 2 - 1, row / rows);
            int targetRow = row % rows;
            int column = pair * 2;
            Label lbl = reusableLabel ?? new Label();
            lbl.Text = label;
            lbl.AutoSize = false;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            editor.Dock = DockStyle.Fill;
            layout.Controls.Add(lbl, column, targetRow);
            layout.Controls.Add(editor, column + 1, targetRow);
        }

        private void AddWeightRow(TableLayoutPanel layout, int row, string label, Control editor, Label reusableLabel)
        {
            AddRow(layout, row, label, editor, reusableLabel);
            if (reusableLabel != null)
            {
                reusableLabel.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private void AddRealmProfileRow(TableLayoutPanel layout, int row, string label, ComboBox combo)
        {
            FillRealmProfileCombo(combo);
            AddRow(layout, row, label, combo);
        }

        private void FillRealmProfileCombo(ComboBox combo)
        {
            FillCombo(combo, new[]
            {
                new OptionItem("Default", L("Default", "По умолчанию")),
                new OptionItem("Independent", L("Independent holdings", "Независимые владения")),
                new OptionItem("ManySmall", L("Mostly small realms", "Больше малых государств")),
                new OptionItem("Balanced", L("Mixed sizes", "Смешанные размеры")),
                new OptionItem("FewLarge", L("Mostly large realms", "Больше крупных государств")),
                new OptionItem("OneState", L("One realm", "Одно государство"))
            }, 0);
        }

        private void ConfigureNumber(NumericUpDown number, int min, int max, int value)
        {
            number.Minimum = min;
            number.Maximum = max;
            number.Value = Math.Max(min, Math.Min(max, value));
            number.Dock = DockStyle.Fill;
        }

        private void ConfigureCheck(CheckBox checkBox, string text, bool value)
        {
            checkBox.Text = text;
            checkBox.Checked = value;
            checkBox.AutoSize = true;
            checkBox.Margin = new Padding(4, 8, 18, 4);
        }

        private void FillCultures()
        {
            cmbCulture.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCulture.Items.Clear();
            foreach (NameCultureInfo culture in cultures)
            {
                cmbCulture.Items.Add(culture);
            }

            if (cmbCulture.Items.Count == 0)
            {
                cmbCulture.Items.Add(new NameCultureInfo { Key = "english", Label = "English" });
            }

            SelectCulture("english");
        }

        private void Fill(ComboBox combo, string[] values, string[] russianLabels, int selectedIndex)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                combo.Items.Add(new OptionItem(values[i], isEnglish ? values[i] : russianLabels[i]));
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = Math.Max(0, Math.Min(combo.Items.Count - 1, selectedIndex));
        }

        private void FillCombo(ComboBox combo, IEnumerable<OptionItem> values, int selectedIndex)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.Clear();
            foreach (OptionItem item in values)
            {
                combo.Items.Add(item);
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = Math.Max(0, Math.Min(combo.Items.Count - 1, selectedIndex));
        }

        private void ApplyInitialOptions(RegionGenerationOptions source)
        {
            applyingOptions = true;
            try
            {
                if (source != null)
                {
                    txtSeed.Text = string.IsNullOrWhiteSpace(source.Seed) ? DateTime.Now.ToString("yyyyMMddHHmmss") : source.Seed;
                    SelectCulture(source.CultureKey);
                    SelectOptionValue(cmbCivilization, source.CivilizationLevel);
                    SelectOptionValue(cmbRealmScale, source.RealmScale);
                    SelectOptionValue(cmbHumanRealmScale, source.HumanRealmScale);
                    SelectOptionValue(cmbDwarvenRealmScale, source.DwarvenRealmScale);
                    SelectOptionValue(cmbElvenRealmScale, source.ElvenRealmScale);
                    SelectOptionValue(cmbHumanClanRealmScale, source.HumanClanRealmScale);
                    SelectOptionValue(cmbOrcRealmScale, source.OrcRealmScale);
                    SelectOptionValue(cmbBeastmanRealmScale, source.BeastmanRealmScale);
                    SelectOptionValue(cmbTransitionalRealmScale, source.TransitionalRealmScale);

                    chkUseLayerSettings.Checked = source.AdvancedMode;
                    chkRivers.Checked = source.GenerateRivers;
                    chkFeatureNames.Checked = source.GenerateFeatureNames;
                    chkSettlements.Checked = source.GenerateSettlements;
                    chkStrongholds.Checked = source.GenerateStrongholds;
                    chkDomains.Checked = source.GenerateDomains;
                    chkRoads.Checked = source.GenerateRoads;
                    chkRealms.Checked = source.GenerateRealms;
                    chkRulers.Checked = source.GenerateRulers;
                    chkHexFeatures.Checked = source.GenerateHexFeatures;
                    chkDungeons.Checked = source.GenerateDungeons;
                    chkSpecialSettlements.Checked = source.GenerateSpecialSettlementsWithoutDomains;
                    chkDwarvenDomains.Checked = source.GenerateDwarvenDomains;
                    chkElvenDomains.Checked = source.GenerateElvenDomains;
                    chkClanDomains.Checked = source.GenerateClanDomains;
                    chkTransitionalDomains.Checked = source.GenerateTransitionalDomains;
                    chkDwarvenNames.Checked = source.UseDwarvenCultureNames;
                    chkElvenNames.Checked = source.UseElvenCultureNames;
                    chkClanNames.Checked = source.UseClanCultureNames;
                    chkTransitionalNames.Checked = source.UseTransitionalCultureNames;
                    chkUseSpecialWeights.Checked = source.UseSpecialDomainWeights;

                    SetNumericValue(nudRiverFrequency, source.RiverPercent);
                    SetNumericValue(nudSettlementDensity, source.SettlementDensityPercent);
                    SetNumericValue(nudDomainCoverage, source.DomainCoveragePercent);
                    SetNumericValue(nudRealmCount, source.RealmCount);
                    SetNumericValue(nudSpecialDomainPercent, source.SpecialDomainPercent);
                    SetNumericValue(nudDwarvenDomains, source.DwarvenDomainWeight);
                    SetNumericValue(nudElvenDomains, source.ElvenDomainWeight);
                    SetNumericValue(nudClanDomains, source.ClanDomainWeight);
                    SetNumericValue(nudTransitionalDomains, source.TransitionalDomainWeight);
                    SelectOptionValue(cmbLandValue, source.LandValueMode);
                }
            }
            finally
            {
                applyingOptions = false;
            }

            UpdateDependencies();
        }

        private void UpdateDependencies()
        {
            // Эти зависимости повторяют защиту генератора: UI показывает пользователю,
            // какие слои являются входными данными для следующих слоев цивилизации.
            bool domainsWillBeCleared = chkSettlements.Checked || chkDomains.Checked;
            bool existingDomainsAfterSelection = hasExistingDomains && !domainsWillBeCleared;
            bool settlementsAfterSelection = hasExistingSettlements || chkSettlements.Checked;
            bool strongholdsAfterSelection = (existingDomainsAfterSelection && chkStrongholds.Checked)
                || (chkDomains.Checked && chkStrongholds.Checked);

            bool domainBasesAvailable = settlementsAfterSelection || chkStrongholds.Checked;
            SetEnabled(chkDomains, domainBasesAvailable);

            bool placesAvailableForRoads = settlementsAfterSelection || strongholdsAfterSelection;
            SetEnabled(chkRoads, placesAvailableForRoads);

            bool domainsAvailable = existingDomainsAfterSelection || (chkDomains.Enabled && chkDomains.Checked);
            SetEnabled(chkRealms, domainsAvailable);
            SetEnabled(chkRulers, domainsAvailable);

            bool specialSettlementsAllowed = chkSettlements.Checked && (!chkDomains.Enabled || !chkDomains.Checked);
            SetEnabled(chkSpecialSettlements, specialSettlementsAllowed);

            SetEnabled(chkDungeons, chkHexFeatures.Checked);

            bool specialTypesAllowed = (chkDomains.Enabled && chkDomains.Checked) || chkSpecialSettlements.Checked;
            SetEnabled(chkDwarvenDomains, specialTypesAllowed);
            SetEnabled(chkElvenDomains, specialTypesAllowed);
            SetEnabled(chkClanDomains, specialTypesAllowed);
            SetEnabled(chkTransitionalDomains, chkDomains.Enabled && chkDomains.Checked);

            chkDwarvenNames.Enabled = chkDwarvenDomains.Enabled && chkDwarvenDomains.Checked;
            chkElvenNames.Enabled = chkElvenDomains.Enabled && chkElvenDomains.Checked;
            chkClanNames.Enabled = chkClanDomains.Enabled && chkClanDomains.Checked;
            chkTransitionalNames.Enabled = chkTransitionalDomains.Enabled && chkTransitionalDomains.Checked;

            bool advanced = chkUseLayerSettings.Checked;
            bool customSpecialWeights = chkUseSpecialWeights.Checked;
            grpLayerSettings.Enabled = true;
            grpSpecialWeights.Enabled = true;
            SetParameterEnabled(lblRiverFrequency, nudRiverFrequency, advanced && chkRivers.Checked);
            SetParameterEnabled(lblSettlementDensity, nudSettlementDensity, advanced && chkSettlements.Checked);
            SetParameterEnabled(lblDomainCoverage, nudDomainCoverage, advanced && chkDomains.Enabled && chkDomains.Checked);
            SetParameterEnabled(lblRealmCount, nudRealmCount, advanced && chkRealms.Enabled && chkRealms.Checked);
            SetParameterEnabled(lblLandValue, cmbLandValue, advanced && chkDomains.Enabled && chkDomains.Checked);
            chkUseSpecialWeights.Enabled = specialTypesAllowed;
            SetParameterEnabled(lblSpecialDomainPercent, nudSpecialDomainPercent, customSpecialWeights && specialTypesAllowed);
            SetParameterEnabled(lblDwarvenWeight, nudDwarvenDomains, customSpecialWeights && chkDwarvenDomains.Enabled && chkDwarvenDomains.Checked);
            SetParameterEnabled(lblElvenWeight, nudElvenDomains, customSpecialWeights && chkElvenDomains.Enabled && chkElvenDomains.Checked);
            SetParameterEnabled(lblClanWeight, nudClanDomains, customSpecialWeights && chkClanDomains.Enabled && chkClanDomains.Checked);
            SetParameterEnabled(lblTransitionalWeight, nudTransitionalDomains, customSpecialWeights && chkTransitionalDomains.Enabled && chkTransitionalDomains.Checked);

            bool realmProfilesAvailable = domainsAvailable && (chkRealms.Checked || chkDomains.Checked);
            cmbHumanRealmScale.Enabled = realmProfilesAvailable;
            cmbDwarvenRealmScale.Enabled = realmProfilesAvailable;
            cmbElvenRealmScale.Enabled = realmProfilesAvailable;
            cmbHumanClanRealmScale.Enabled = realmProfilesAvailable;
            cmbOrcRealmScale.Enabled = realmProfilesAvailable;
            cmbBeastmanRealmScale.Enabled = realmProfilesAvailable;
            cmbTransitionalRealmScale.Enabled = realmProfilesAvailable;
        }

        private void SetEnabled(CheckBox checkBox, bool enabled)
        {
            checkBox.Enabled = enabled;
            if (!enabled && !applyingOptions) checkBox.Checked = false;
        }

        private void SetParameterEnabled(Label label, Control editor, bool enabled)
        {
            if (label != null) label.Enabled = enabled;
            if (editor != null) editor.Enabled = enabled;
        }

        private void Accept()
        {
            if (!AnyLayerSelected())
            {
                MessageBox.Show(this, L("Select at least one layer.", "Выберите хотя бы один слой."), Text);
                return;
            }

            // На выходе диалог отдает общий RegionGenerationOptions, но только для слоев,
            // которые можно безопасно пересобрать поверх уже существующего ландшафта.
            Options = new RegionGenerationOptions
            {
                Seed = txtSeed.Text.Trim(),
                Width = mapWidth,
                Height = mapHeight,
                ClimateBelt = "Temperate",
                CivilizationLevel = SelectedValue(cmbCivilization),
                RealmScale = SelectedValue(cmbRealmScale),
                HumanRealmScale = SelectedValue(cmbHumanRealmScale),
                DwarvenRealmScale = SelectedValue(cmbDwarvenRealmScale),
                ElvenRealmScale = SelectedValue(cmbElvenRealmScale),
                HumanClanRealmScale = SelectedValue(cmbHumanClanRealmScale),
                OrcRealmScale = SelectedValue(cmbOrcRealmScale),
                BeastmanRealmScale = SelectedValue(cmbBeastmanRealmScale),
                TransitionalRealmScale = SelectedValue(cmbTransitionalRealmScale),
                CultureKey = SelectedCultureKey(),
                WaterLayout = "Coast",
                AdvancedMode = chkUseLayerSettings.Checked,
                GenerateRivers = chkRivers.Checked,
                GenerateFeatureNames = chkFeatureNames.Checked,
                GenerateSettlements = chkSettlements.Checked,
                GenerateStrongholds = chkStrongholds.Checked,
                GenerateDomains = chkDomains.Checked,
                GenerateRoads = chkRoads.Checked,
                GenerateRealms = chkRealms.Checked,
                GenerateRulers = chkRulers.Checked,
                GenerateHexFeatures = chkHexFeatures.Checked,
                GenerateDungeons = chkDungeons.Checked,
                GenerateDwarvenDomains = chkDwarvenDomains.Checked,
                GenerateElvenDomains = chkElvenDomains.Checked,
                GenerateClanDomains = chkClanDomains.Checked,
                GenerateTransitionalDomains = chkTransitionalDomains.Checked,
                GenerateSpecialSettlementsWithoutDomains = chkSpecialSettlements.Checked,
                UseDwarvenCultureNames = chkDwarvenNames.Checked,
                UseElvenCultureNames = chkElvenNames.Checked,
                UseClanCultureNames = chkClanNames.Checked,
                UseTransitionalCultureNames = chkTransitionalNames.Checked,
                UseSpecialDomainWeights = chkUseSpecialWeights.Checked,
                RiverPercent = (int)nudRiverFrequency.Value,
                SettlementDensityPercent = (int)nudSettlementDensity.Value,
                DomainCoveragePercent = (int)nudDomainCoverage.Value,
                RealmCount = (int)nudRealmCount.Value,
                SpecialDomainPercent = (int)nudSpecialDomainPercent.Value,
                DwarvenDomainWeight = (int)nudDwarvenDomains.Value,
                ElvenDomainWeight = (int)nudElvenDomains.Value,
                ClanDomainWeight = (int)nudClanDomains.Value,
                TransitionalDomainWeight = (int)nudTransitionalDomains.Value,
                LandValueMode = SelectedValue(cmbLandValue),
                UseRussianNames = !isEnglish,
                DefaultAgeIndex = -1
            };

            Options.GenerateSpecialDomains = Options.GenerateDwarvenDomains
                || Options.GenerateElvenDomains
                || Options.GenerateClanDomains
                || Options.GenerateTransitionalDomains;
            EnforceLayerDependencies(Options, hasExistingSettlements, hasExistingStrongholds, hasExistingDomains);

            if (string.IsNullOrWhiteSpace(Options.Seed)) Options.Seed = DateTime.Now.ToString("yyyyMMddHHmmss");
            lastAcceptedOptions = CreateRememberedOptions(Options);
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool AnyLayerSelected()
        {
            return chkRivers.Checked || chkFeatureNames.Checked || chkSettlements.Checked || chkStrongholds.Checked
                || chkDomains.Checked || chkRoads.Checked || chkRealms.Checked || chkRulers.Checked
                || chkHexFeatures.Checked || chkDungeons.Checked;
        }

        private static void EnforceLayerDependencies(
            RegionGenerationOptions options,
            bool existingSettlements,
            bool existingStrongholds,
            bool existingDomains)
        {
            if (options == null) return;

            bool domainsWillBeCleared = options.GenerateSettlements || options.GenerateDomains;
            bool existingDomainsAfterSelection = existingDomains && !domainsWillBeCleared;
            bool settlementsAfterSelection = existingSettlements || options.GenerateSettlements;
            bool canBaseNewDomains = settlementsAfterSelection || options.GenerateStrongholds;

            if (!canBaseNewDomains)
            {
                options.GenerateDomains = false;
            }

            bool hasRoadNodes = settlementsAfterSelection
                || (existingDomainsAfterSelection && options.GenerateStrongholds)
                || (options.GenerateDomains && options.GenerateStrongholds);
            if (!hasRoadNodes)
            {
                options.GenerateRoads = false;
            }

            bool hasDomains = existingDomainsAfterSelection || options.GenerateDomains;
            if (!hasDomains)
            {
                options.GenerateRealms = false;
                options.GenerateRulers = false;
            }

            if (!options.GenerateDomains)
            {
                options.GenerateTransitionalDomains = false;
                if (!options.GenerateSpecialSettlementsWithoutDomains)
                {
                    options.GenerateDwarvenDomains = false;
                    options.GenerateElvenDomains = false;
                    options.GenerateClanDomains = false;
                }
            }

            if (!options.GenerateSettlements)
            {
                options.GenerateSpecialSettlementsWithoutDomains = false;
            }

            if (!options.GenerateHexFeatures)
            {
                options.GenerateDungeons = false;
            }

            options.GenerateSpecialDomains = options.GenerateDwarvenDomains
                || options.GenerateElvenDomains
                || options.GenerateClanDomains
                || options.GenerateTransitionalDomains;
        }

        private static RegionGenerationOptions CreateRememberedOptions(RegionGenerationOptions source)
        {
            RegionGenerationOptions remembered = CloneOptions(source);
            if (remembered == null) return null;
            remembered.Seed = "";

            if (!remembered.AdvancedMode)
            {
                RegionGenerationOptions defaults = new RegionGenerationOptions();
                remembered.RiverPercent = defaults.RiverPercent;
                remembered.SettlementDensityPercent = defaults.SettlementDensityPercent;
                remembered.DomainCoveragePercent = defaults.DomainCoveragePercent;
                remembered.RealmCount = defaults.RealmCount;
                remembered.LandValueMode = defaults.LandValueMode;
                if (!remembered.UseSpecialDomainWeights)
                {
                    remembered.SpecialDomainPercent = defaults.SpecialDomainPercent;
                    remembered.DwarvenDomainWeight = defaults.DwarvenDomainWeight;
                    remembered.ElvenDomainWeight = defaults.ElvenDomainWeight;
                    remembered.ClanDomainWeight = defaults.ClanDomainWeight;
                    remembered.TransitionalDomainWeight = defaults.TransitionalDomainWeight;
                }
            }

            return remembered;
        }

        private static RegionGenerationOptions CloneOptions(RegionGenerationOptions source)
        {
            if (source == null) return null;

            return new RegionGenerationOptions
            {
                Seed = source.Seed,
                Width = source.Width,
                Height = source.Height,
                ClimateBelt = source.ClimateBelt,
                CivilizationLevel = source.CivilizationLevel,
                RealmScale = source.RealmScale,
                HumanRealmScale = source.HumanRealmScale,
                DwarvenRealmScale = source.DwarvenRealmScale,
                ElvenRealmScale = source.ElvenRealmScale,
                HumanClanRealmScale = source.HumanClanRealmScale,
                OrcRealmScale = source.OrcRealmScale,
                BeastmanRealmScale = source.BeastmanRealmScale,
                TransitionalRealmScale = source.TransitionalRealmScale,
                CultureKey = source.CultureKey,
                WaterLayout = source.WaterLayout,
                AdvancedMode = source.AdvancedMode,
                GenerateSettlements = source.GenerateSettlements,
                GenerateStrongholds = source.GenerateStrongholds,
                GenerateDomains = source.GenerateDomains,
                GenerateRealms = source.GenerateRealms,
                GenerateRulers = source.GenerateRulers,
                GenerateRoads = source.GenerateRoads,
                GenerateRivers = source.GenerateRivers,
                GenerateFeatureNames = source.GenerateFeatureNames,
                GenerateHexFeatures = source.GenerateHexFeatures,
                GenerateDungeons = source.GenerateDungeons,
                GenerateSpecialDomains = source.GenerateSpecialDomains,
                GenerateDwarvenDomains = source.GenerateDwarvenDomains,
                GenerateElvenDomains = source.GenerateElvenDomains,
                GenerateClanDomains = source.GenerateClanDomains,
                GenerateTransitionalDomains = source.GenerateTransitionalDomains,
                GenerateSpecialSettlementsWithoutDomains = source.GenerateSpecialSettlementsWithoutDomains,
                UseRussianNames = source.UseRussianNames,
                UseDwarvenCultureNames = source.UseDwarvenCultureNames,
                UseElvenCultureNames = source.UseElvenCultureNames,
                UseClanCultureNames = source.UseClanCultureNames,
                UseTransitionalCultureNames = source.UseTransitionalCultureNames,
                UseSpecialDomainWeights = source.UseSpecialDomainWeights,
                RiverPercent = source.RiverPercent,
                SettlementDensityPercent = source.SettlementDensityPercent,
                DomainCoveragePercent = source.DomainCoveragePercent,
                RealmCount = source.RealmCount,
                SpecialDomainPercent = source.SpecialDomainPercent,
                DwarvenDomainWeight = source.DwarvenDomainWeight,
                ElvenDomainWeight = source.ElvenDomainWeight,
                ClanDomainWeight = source.ClanDomainWeight,
                TransitionalDomainWeight = source.TransitionalDomainWeight,
                DefaultAgeIndex = source.DefaultAgeIndex,
                LandValueMode = source.LandValueMode
            };
        }

        private void SelectCulture(string key)
        {
            for (int i = 0; i < cmbCulture.Items.Count; i++)
            {
                NameCultureInfo culture = cmbCulture.Items[i] as NameCultureInfo;
                if (culture != null && string.Equals(culture.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    cmbCulture.SelectedIndex = i;
                    return;
                }
            }

            if (cmbCulture.Items.Count > 0) cmbCulture.SelectedIndex = 0;
        }

        private string SelectedCultureKey()
        {
            NameCultureInfo culture = cmbCulture.SelectedItem as NameCultureInfo;
            return culture == null || string.IsNullOrWhiteSpace(culture.Key) ? "english" : culture.Key;
        }

        private string SelectedValue(ComboBox combo)
        {
            OptionItem item = combo.SelectedItem as OptionItem;
            return item == null ? "" : item.Value;
        }

        private void SelectOptionValue(ComboBox combo, string value)
        {
            if (combo == null || string.IsNullOrWhiteSpace(value)) return;

            for (int i = 0; i < combo.Items.Count; i++)
            {
                OptionItem item = combo.Items[i] as OptionItem;
                if (item != null && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SetNumericValue(NumericUpDown numeric, int value)
        {
            if (numeric == null) return;
            numeric.Value = Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, value));
        }

        private static bool HasVisibleStronghold(DomainRecord domain)
        {
            return domain != null && domain.StrongholdQ >= 0 && domain.StrongholdR >= 0;
        }

        private string L(string english, string russian)
        {
            return isEnglish ? english : russian;
        }

        private sealed class OptionItem
        {
            public string Value { get; private set; }
            private readonly string label;

            public OptionItem(string value, string label)
            {
                Value = value;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }
    }
}
