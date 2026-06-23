﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class RegionGenerationDialog : Form
    {
        private sealed class OptionItem
        {
            public string Value { get; private set; }
            public string Label { get; private set; }

            public OptionItem(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public override string ToString()
            {
                return Label;
            }
        }

        private readonly bool isEnglish;
        private readonly List<NameCultureInfo> cultures;
        private readonly TextBox txtSeed = new TextBox();
        private readonly Button btnSeed = new Button();
        private readonly NumericUpDown nudWidth = new NumericUpDown();
        private readonly NumericUpDown nudHeight = new NumericUpDown();
        private readonly ComboBox cmbClimate = new ComboBox();
        private readonly ComboBox cmbCivilization = new ComboBox();
        private readonly ComboBox cmbRealmScale = new ComboBox();
        private readonly ComboBox cmbHumanRealmScale = new ComboBox();
        private readonly ComboBox cmbDwarvenRealmScale = new ComboBox();
        private readonly ComboBox cmbElvenRealmScale = new ComboBox();
        private readonly ComboBox cmbHumanClanRealmScale = new ComboBox();
        private readonly ComboBox cmbOrcRealmScale = new ComboBox();
        private readonly ComboBox cmbBeastmanRealmScale = new ComboBox();
        private readonly ComboBox cmbTransitionalRealmScale = new ComboBox();
        private readonly ComboBox cmbCulture = new ComboBox();
        private readonly ComboBox cmbWaterLayout = new ComboBox();
        private readonly ComboBox cmbSeismicity = new ComboBox();
        private readonly CheckBox chkAdvanced = new CheckBox();
        private readonly CheckBox chkSettlements = new CheckBox();
        private readonly CheckBox chkStrongholds = new CheckBox();
        private readonly CheckBox chkDomains = new CheckBox();
        private readonly CheckBox chkRealms = new CheckBox();
        private readonly CheckBox chkRulers = new CheckBox();
        private readonly CheckBox chkRoads = new CheckBox();
        private readonly CheckBox chkRivers = new CheckBox();
        private readonly CheckBox chkFeatureNames = new CheckBox();
        private readonly CheckBox chkHexFeatures = new CheckBox();
        private readonly CheckBox chkDungeons = new CheckBox();
        private readonly CheckBox chkDwarvenDomains = new CheckBox();
        private readonly CheckBox chkElvenDomains = new CheckBox();
        private readonly CheckBox chkClanDomains = new CheckBox();
        private readonly CheckBox chkTransitionalDomains = new CheckBox();
        private readonly CheckBox chkSpecialSettlements = new CheckBox();
        private readonly CheckBox chkDwarvenNames = new CheckBox();
        private readonly CheckBox chkElvenNames = new CheckBox();
        private readonly CheckBox chkClanNames = new CheckBox();
        private readonly CheckBox chkTransitionalNames = new CheckBox();
        private readonly NumericUpDown nudTerrainZones = new NumericUpDown();
        private readonly NumericUpDown nudTerrainChaos = new NumericUpDown();
        private readonly NumericUpDown nudWater = new NumericUpDown();
        private readonly NumericUpDown nudLakes = new NumericUpDown();
        private readonly NumericUpDown nudRivers = new NumericUpDown();
        private readonly NumericUpDown nudHills = new NumericUpDown();
        private readonly NumericUpDown nudMountains = new NumericUpDown();
        private readonly NumericUpDown nudSettlementDensity = new NumericUpDown();
        private readonly NumericUpDown nudDomainCoverage = new NumericUpDown();
        private readonly NumericUpDown nudRealmCount = new NumericUpDown();
        private readonly NumericUpDown nudVariance = new NumericUpDown();
        private readonly NumericUpDown nudSpecialDomainPercent = new NumericUpDown();
        private readonly NumericUpDown nudDwarvenDomains = new NumericUpDown();
        private readonly NumericUpDown nudElvenDomains = new NumericUpDown();
        private readonly NumericUpDown nudClanDomains = new NumericUpDown();
        private readonly NumericUpDown nudTransitionalDomains = new NumericUpDown();
        private readonly ComboBox cmbAge = new ComboBox();
        private readonly ComboBox cmbLandValue = new ComboBox();
        private Panel advancedPanel;
        private bool suppressAdvancedReset;
        private static RegionGenerationOptions lastAcceptedOptions;

        public RegionGenerationOptions Options { get; private set; }

        public RegionGenerationDialog(bool isEnglish, NameGenerationService nameService, int currentWidth, int currentHeight)
        {
            this.isEnglish = isEnglish;
            cultures = nameService == null ? new List<NameCultureInfo>() : nameService.GetCultures(isEnglish);
            Options = null;

            Text = L("Generate region", "Генерация региона");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(680, 700);

            BuildUi(Math.Max(6, currentWidth), Math.Max(6, currentHeight));
            ApplyInitialOptions(lastAcceptedOptions);
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
        }

        private void BuildUi(int currentWidth, int currentHeight)
        {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            TabPage basic = new TabPage(L("Basic", "Основное"));
            TabPage advanced = new TabPage(L("Advanced", "Расширенное"));
            tabs.TabPages.Add(basic);
            tabs.TabPages.Add(advanced);

            basic.Controls.Add(BuildBasicPanel(currentWidth, currentHeight));
            advancedPanel = BuildAdvancedPanel();
            advanced.Controls.Add(advancedPanel);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.Height = 46;
            buttons.Padding = new Padding(8);
            buttons.FlowDirection = FlowDirection.RightToLeft;

            Button cancel = new Button { Text = L("Cancel", "Отмена"), Width = 110, Height = 30, DialogResult = DialogResult.Cancel };
            UiTheme.StyleNegativeButton(cancel);

            Button ok = new Button { Text = L("Generate", "Сгенерировать"), Width = 140, Height = 30 };
            UiTheme.StylePositiveButton(ok);
            ok.Click += (s, e) => Accept();

            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(tabs);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
            UpdateAdvancedEnabled();
        }

        private Control BuildBasicPanel(int currentWidth, int currentHeight)
        {
            TableLayoutPanel layout = CreateTable(2, 17);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles[10] = new RowStyle(SizeType.Absolute, 132);
            layout.RowStyles[11] = new RowStyle(SizeType.Absolute, 74);
            layout.RowStyles[12] = new RowStyle(SizeType.Absolute, 92);

            txtSeed.Text = DateTime.Now.ToString("yyyyMMddHHmmss");
            FlowLayoutPanel seedPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            txtSeed.Width = 260;
            btnSeed.Text = L("New", "Новый");
            btnSeed.Width = 72;
            UiTheme.StyleCommandButton(btnSeed, UiTheme.PositiveButtonColor);
            btnSeed.Click += (s, e) => txtSeed.Text = DateTime.Now.ToString("yyyyMMddHHmmss");
            seedPanel.Controls.Add(txtSeed);
            seedPanel.Controls.Add(btnSeed);
            AddRow(layout, 0, L("Seed", "Seed"), seedPanel);

            ConfigureNumeric(nudWidth, 6, RegionGenerationOptions.MaxMapSize, currentWidth);
            ConfigureNumeric(nudHeight, 6, RegionGenerationOptions.MaxMapSize, currentHeight);
            FlowLayoutPanel sizePanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            sizePanel.Controls.Add(nudWidth);
            sizePanel.Controls.Add(new Label { Text = " x ", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(4, 6, 4, 0) });
            sizePanel.Controls.Add(nudHeight);
            AddRow(layout, 1, L("Map size", "Размер карты"), sizePanel);

            FillCombo(cmbClimate, new[]
            {
                new OptionItem("Temperate", L("Temperate", "Умеренный")),
                new OptionItem("Tropical", L("Tropical", "Тропический")),
                new OptionItem("Arid", L("Arid", "Засушливый")),
                new OptionItem("Cold", L("Cold", "Холодный")),
                new OptionItem("Mixed", L("Mixed", "Смешанный"))
            }, 0);
            AddRow(layout, 2, L("Climate belt", "Климатический пояс"), cmbClimate);

            FillCombo(cmbWaterLayout, new[]
            {
                new OptionItem("NoLargeWater", L("No seas or oceans", "Без морей и океанов")),
                new OptionItem("Continent", L("Continent", "Континент")),
                new OptionItem("Archipelago", L("Archipelago", "Архипелаг")),
                new OptionItem("Coast", L("Sea coast", "Побережье")),
                new OptionItem("TwoContinents", L("Two continents", "Два континента")),
                new OptionItem("InlandSea", L("Inland sea", "Внутреннее море")),
                new OptionItem("Gulf", L("Large gulf", "Большой залив"))
            }, 3);
            AddRow(layout, 3, L("Water layout", "Распределение воды"), cmbWaterLayout);

            FillCombo(cmbSeismicity, new[]
            {
                new OptionItem("Normal", L("Normal", "Обычный")),
                new OptionItem("Stable", L("Non-seismic", "Несейсмоактивный")),
                new OptionItem("Seismic", L("Seismic", "Сейсмоактивный"))
            }, 0);
            AddRow(layout, 4, L("Seismicity", "Сейсмичность"), cmbSeismicity);

            FillCombo(cmbCivilization, new[]
            {
                new OptionItem("Borderlands", L("Borderlands", "Пограничье")),
                new OptionItem("Civilized", L("Civilized", "Цивилизованные земли")),
                new OptionItem("Wild", L("Wild frontier", "Дикие земли"))
            }, 0);
            AddRow(layout, 5, L("Civilization", "Освоенность"), cmbCivilization);

            FillCombo(cmbRealmScale, new[]
            {
                new OptionItem("Balanced", L("Mixed sizes", "Смешанные размеры")),
                new OptionItem("ManySmall", L("Mostly small realms", "Больше малых государств")),
                new OptionItem("FewLarge", L("Mostly large realms", "Больше крупных государств")),
                new OptionItem("OneState", L("One realm", "Одно государство"))
            }, 0);
            AddRow(layout, 6, L("Realm size profile", "Профиль размеров государств"), cmbRealmScale);

            cmbCulture.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (NameCultureInfo culture in cultures)
            {
                cmbCulture.Items.Add(culture);
            }
            if (cmbCulture.Items.Count == 0)
            {
                cmbCulture.Items.Add(new NameCultureInfo { Key = "english", Label = "English" });
            }
            SelectCulture("english");
            AddRow(layout, 7, L("Naming culture", "Культура имён"), cmbCulture);

            chkAdvanced.Text = L("Use advanced settings", "Использовать расширенные настройки");
            chkAdvanced.AutoSize = true;
            chkAdvanced.CheckedChanged += (s, e) =>
            {
                if (!suppressAdvancedReset && !chkAdvanced.Checked)
                {
                    ResetAdvancedControlsToDefaults();
                }

                UpdateAdvancedEnabled();
            };
            layout.Controls.Add(chkAdvanced, 0, 8);
            layout.SetColumnSpan(chkAdvanced, 2);

            Label layers = new Label
            {
                Text = L("Generated layers", "Генерируемые слои"),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };
            layout.Controls.Add(layers, 0, 9);
            layout.SetColumnSpan(layers, 2);

            chkSettlements.Text = L("Settlements", "Поселения");
            chkStrongholds.Text = L("Strongholds", "Крепости");
            chkDomains.Text = L("Domains", "Домены");
            chkRealms.Text = L("Realms and vassals", "Державы и вассалы");
            chkRulers.Text = L("NPC rulers", "Правители NPC");
            chkRoads.Text = L("Roads", "Дороги");
            chkRivers.Text = L("Rivers", "Реки");
            chkFeatureNames.Text = L("Map names", "Названия карты");
            chkHexFeatures.Text = L("Hex features", "Особенности гексов");
            chkDungeons.Text = L("Dungeons", "Данжи");
            ConfigureLayerCheckBox(chkSettlements);
            ConfigureLayerCheckBox(chkStrongholds);
            ConfigureLayerCheckBox(chkDomains);
            ConfigureLayerCheckBox(chkRealms);
            ConfigureLayerCheckBox(chkRulers);
            ConfigureLayerCheckBox(chkRoads);
            ConfigureLayerCheckBox(chkRivers);
            ConfigureLayerCheckBox(chkFeatureNames);
            ConfigureLayerCheckBox(chkHexFeatures);
            ConfigureLayerCheckBox(chkDungeons);
            chkDwarvenDomains.Text = L("Dwarven domains", "Дварфийские домены");
            chkElvenDomains.Text = L("Elven domains", "Эльфийские домены");
            chkClanDomains.Text = L("Clanholds", "Клановые домены");
            chkTransitionalDomains.Text = L("Transitional domains", "Переходные домены");
            chkSpecialSettlements.Text = L("Special settlements without domains", "Особые поселения без доменов");
            ConfigureLayerCheckBox(chkDwarvenDomains);
            ConfigureLayerCheckBox(chkElvenDomains);
            ConfigureLayerCheckBox(chkClanDomains);
            ConfigureLayerCheckBox(chkTransitionalDomains);
            ConfigureLayerCheckBox(chkSpecialSettlements);
            chkSettlements.Checked = chkStrongholds.Checked = chkDomains.Checked = chkRealms.Checked = chkRulers.Checked = chkRoads.Checked = chkRivers.Checked = chkFeatureNames.Checked = chkHexFeatures.Checked = chkDungeons.Checked = true;
            chkDwarvenDomains.Checked = chkElvenDomains.Checked = chkClanDomains.Checked = chkTransitionalDomains.Checked = false;
            chkSpecialSettlements.Checked = false;
            chkSettlements.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkStrongholds.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkDomains.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkRealms.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkRulers.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkRoads.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkHexFeatures.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkSpecialSettlements.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkDwarvenDomains.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkElvenDomains.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkClanDomains.CheckedChanged += (s, e) => UpdateAdvancedEnabled();
            chkTransitionalDomains.CheckedChanged += (s, e) => UpdateAdvancedEnabled();

            FlowLayoutPanel checks = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };
            checks.Controls.Add(chkSettlements);
            checks.Controls.Add(chkStrongholds);
            checks.Controls.Add(chkDomains);
            checks.Controls.Add(chkRealms);
            checks.Controls.Add(chkRulers);
            checks.Controls.Add(chkRoads);
            checks.Controls.Add(chkRivers);
            checks.Controls.Add(chkFeatureNames);
            checks.Controls.Add(chkHexFeatures);
            checks.Controls.Add(chkDungeons);
            checks.Controls.Add(chkDwarvenDomains);
            checks.Controls.Add(chkElvenDomains);
            checks.Controls.Add(chkClanDomains);
            checks.Controls.Add(chkTransitionalDomains);
            checks.Controls.Add(chkSpecialSettlements);
            layout.Controls.Add(checks, 0, 10);
            layout.SetColumnSpan(checks, 2);

            Label names = new Label
            {
                Text = L("Special-domain naming", "Имена особых доменов"),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };
            layout.Controls.Add(names, 0, 11);

            chkDwarvenNames.Text = L("Dwarven names", "Дварфийские имена");
            chkElvenNames.Text = L("Elven names", "Эльфийские имена");
            chkClanNames.Text = L("Clan names", "Клановые имена");
            chkTransitionalNames.Text = L("Transitional names", "Переходные имена");
            ConfigureLayerCheckBox(chkDwarvenNames);
            ConfigureLayerCheckBox(chkElvenNames);
            ConfigureLayerCheckBox(chkClanNames);
            ConfigureLayerCheckBox(chkTransitionalNames);
            chkDwarvenNames.Checked = chkElvenNames.Checked = chkClanNames.Checked = true;
            chkTransitionalNames.Checked = false;

            FlowLayoutPanel nameChecks = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };
            nameChecks.Controls.Add(chkDwarvenNames);
            nameChecks.Controls.Add(chkElvenNames);
            nameChecks.Controls.Add(chkClanNames);
            nameChecks.Controls.Add(chkTransitionalNames);
            layout.Controls.Add(nameChecks, 1, 11);

            Label hint = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Text = L(
                    "Basic mode keeps most densities on presets derived from climate, civilization, and realm scale.",
                    "Обычный режим берёт плотности из пресетов по климату, освоенности и размеру государств.")
            };
            layout.Controls.Add(hint, 0, 12);
            layout.SetColumnSpan(hint, 2);
            return layout;
        }

        private Panel BuildAdvancedPanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel layout = CreateTable(2, 26);
            layout.Dock = DockStyle.Top;
            layout.Height = 940;
            layout.Padding = new Padding(12);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            ConfigureNumeric(nudTerrainZones, 3, 40, 10);
            ConfigureNumeric(nudTerrainChaos, 0, 100, 35);
            ConfigureNumeric(nudWater, 0, 70, 12);
            ConfigureNumeric(nudLakes, 0, 50, 4);
            ConfigureNumeric(nudRivers, 0, 100, 35);
            ConfigureNumeric(nudHills, 0, 80, 22);
            ConfigureNumeric(nudMountains, 0, 60, 8);
            ConfigureNumeric(nudSettlementDensity, 0, 100, 35);
            ConfigureNumeric(nudDomainCoverage, 0, 100, 45);
            ConfigureNumeric(nudRealmCount, 1, 20, 3);
            ConfigureNumeric(nudVariance, 0, 100, 45);
            ConfigureNumeric(nudSpecialDomainPercent, 0, 100, 12);
            ConfigureNumeric(nudDwarvenDomains, 0, 100, 25);
            ConfigureNumeric(nudElvenDomains, 0, 100, 25);
            ConfigureNumeric(nudClanDomains, 0, 100, 30);
            ConfigureNumeric(nudTransitionalDomains, 0, 100, 20);

            AddRow(layout, 0, L("Terrain zones", "Зоны местности"), nudTerrainZones);
            AddRow(layout, 1, L("Zone chaos (%)", "Хаотичность зон (%)"), nudTerrainChaos);
            AddRow(layout, 2, L("Large water coverage (%)", "Большая вода (%)"), nudWater);
            AddRow(layout, 3, L("Lake frequency (%)", "Частота озёр (%)"), nudLakes);
            AddRow(layout, 4, L("River frequency (%)", "Частота рек (%)"), nudRivers);
            AddRow(layout, 5, L("Hills (%)", "Холмы (%)"), nudHills);
            AddRow(layout, 6, L("Mountains (%)", "Горы (%)"), nudMountains);
            AddRow(layout, 7, L("Settlement density (%)", "Плотность поселений (%)"), nudSettlementDensity);
            AddRow(layout, 8, L("Domain coverage (%)", "Покрытие доменами (%)"), nudDomainCoverage);
            AddRow(layout, 9, L("Realm count", "Количество государств"), nudRealmCount);
            AddRow(layout, 10, L("State size variance (%)", "Разброс размеров (%)"), nudVariance);

            FillCombo(cmbAge, new[]
            {
                new OptionItem("-1", L("Random by market class", "Случайно по классу рынка")),
                new OptionItem("0", L("0-20 years", "0-20 лет")),
                new OptionItem("1", L("21-100 years", "21-100 лет")),
                new OptionItem("2", L("101-1,000 years", "101-1,000 лет")),
                new OptionItem("3", L("1,001-2,000 years", "1,001-2,000 лет")),
                new OptionItem("4", L("2,001+ years", "2,001+ лет"))
            }, 0);
            AddRow(layout, 11, L("Settlement age", "Возраст поселений"), cmbAge);

            FillCombo(cmbLandValue, new[]
            {
                new OptionItem("Fixed6", L("Fixed 6 gp", "Фиксированные 6 gp")),
                new OptionItem("DomainWide", L("3d3 per domain", "3d3 на домен")),
                new OptionItem("PerHex", L("3d3 per hex", "3d3 на гекс"))
            }, 0);
            AddRow(layout, 12, L("Land value mode", "Ценность земли"), cmbLandValue);

            AddRow(layout, 13, L("Selected special chance (%)", "Шанс выбранных типов (%)"), nudSpecialDomainPercent);
            AddRow(layout, 14, L("Dwarven weight", "Вес дварфийских"), nudDwarvenDomains);
            AddRow(layout, 15, L("Elven weight", "Вес эльфийских"), nudElvenDomains);
            AddRow(layout, 16, L("Clanhold weight", "Вес клановых"), nudClanDomains);
            AddRow(layout, 17, L("Transitional weight", "Вес переходных"), nudTransitionalDomains);
            AddRealmProfileRow(layout, 18, L("Human realm profile", "Профиль людей"), cmbHumanRealmScale);
            AddRealmProfileRow(layout, 19, L("Dwarven realm profile", "Профиль дварфов"), cmbDwarvenRealmScale);
            AddRealmProfileRow(layout, 20, L("Elven realm profile", "Профиль эльфов"), cmbElvenRealmScale);
            AddRealmProfileRow(layout, 21, L("Human clanhold profile", "Профиль клановых людей"), cmbHumanClanRealmScale);
            AddRealmProfileRow(layout, 22, L("Orc clanhold profile", "Профиль орков"), cmbOrcRealmScale);
            AddRealmProfileRow(layout, 23, L("Beastman clanhold profile", "Профиль зверолюдов"), cmbBeastmanRealmScale);
            AddRealmProfileRow(layout, 24, L("Transitional profile", "Профиль переходных"), cmbTransitionalRealmScale);

            panel.Controls.Add(layout);
            return panel;
        }

        private TableLayoutPanel CreateTable(int columns, int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = columns;
            layout.RowCount = rows;
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            }

            return layout;
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, Control editor)
        {
            Label lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
            editor.Dock = DockStyle.Fill;
            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(editor, 1, row);
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

        private void ConfigureNumeric(NumericUpDown numeric, int min, int max, int value)
        {
            numeric.Minimum = min;
            numeric.Maximum = max;
            numeric.Value = Math.Max(min, Math.Min(max, value));
            numeric.Width = 70;
        }

        private void ConfigureLayerCheckBox(CheckBox checkBox)
        {
            if (checkBox == null) return;

            checkBox.AutoSize = true;
            checkBox.Margin = new Padding(4, 8, 18, 4);
        }

        private void FillCombo(ComboBox combo, IEnumerable<OptionItem> values, int selectedIndex)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.Clear();
            foreach (OptionItem item in values)
            {
                combo.Items.Add(item);
            }

            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = Math.Max(0, Math.Min(combo.Items.Count - 1, selectedIndex));
            }
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

        private void ApplyInitialOptions(RegionGenerationOptions source)
        {
            if (source == null)
            {
                UpdateAdvancedEnabled();
                return;
            }

            suppressAdvancedReset = true;
            try
            {
                SetNumericValue(nudWidth, source.Width);
                SetNumericValue(nudHeight, source.Height);
                SelectOptionValue(cmbClimate, source.ClimateBelt);
                SelectOptionValue(cmbWaterLayout, source.WaterLayout);
                SelectOptionValue(cmbSeismicity, source.Seismicity);
                SelectOptionValue(cmbCivilization, source.CivilizationLevel);
                SelectOptionValue(cmbRealmScale, source.RealmScale);
                SelectOptionValue(cmbHumanRealmScale, source.HumanRealmScale);
                SelectOptionValue(cmbDwarvenRealmScale, source.DwarvenRealmScale);
                SelectOptionValue(cmbElvenRealmScale, source.ElvenRealmScale);
                SelectOptionValue(cmbHumanClanRealmScale, source.HumanClanRealmScale);
                SelectOptionValue(cmbOrcRealmScale, source.OrcRealmScale);
                SelectOptionValue(cmbBeastmanRealmScale, source.BeastmanRealmScale);
                SelectOptionValue(cmbTransitionalRealmScale, source.TransitionalRealmScale);
                SelectCulture(source.CultureKey);

                chkSettlements.Checked = source.GenerateSettlements;
                chkStrongholds.Checked = source.GenerateStrongholds;
                chkDomains.Checked = source.GenerateDomains;
                chkRealms.Checked = source.GenerateRealms;
                chkRulers.Checked = source.GenerateRulers;
                chkRoads.Checked = source.GenerateRoads;
                chkRivers.Checked = source.GenerateRivers;
                chkFeatureNames.Checked = source.GenerateFeatureNames;
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

                if (source.AdvancedMode)
                {
                    ApplyAdvancedControls(source);
                }
                else
                {
                    ResetAdvancedControlsToDefaults();
                }

                chkAdvanced.Checked = source.AdvancedMode;
            }
            finally
            {
                suppressAdvancedReset = false;
            }

            UpdateAdvancedEnabled();
        }

        private void ApplyAdvancedControls(RegionGenerationOptions source)
        {
            if (source == null) return;

            SetNumericValue(nudTerrainZones, source.TerrainZoneCount);
            SetNumericValue(nudTerrainChaos, source.TerrainChaosPercent);
            SetNumericValue(nudWater, source.WaterPercent);
            SetNumericValue(nudLakes, source.LakePercent);
            SetNumericValue(nudRivers, source.RiverPercent);
            SetNumericValue(nudHills, source.HillsPercent);
            SetNumericValue(nudMountains, source.MountainsPercent);
            SetNumericValue(nudSettlementDensity, source.SettlementDensityPercent);
            SetNumericValue(nudDomainCoverage, source.DomainCoveragePercent);
            SetNumericValue(nudRealmCount, source.RealmCount);
            SetNumericValue(nudVariance, source.StateSizeVariancePercent);
            SetNumericValue(nudSpecialDomainPercent, source.SpecialDomainPercent);
            SetNumericValue(nudDwarvenDomains, source.DwarvenDomainWeight);
            SetNumericValue(nudElvenDomains, source.ElvenDomainWeight);
            SetNumericValue(nudClanDomains, source.ClanDomainWeight);
            SetNumericValue(nudTransitionalDomains, source.TransitionalDomainWeight);
            SelectOptionValue(cmbAge, source.DefaultAgeIndex.ToString());
            SelectOptionValue(cmbLandValue, source.LandValueMode);
            SelectOptionValue(cmbHumanRealmScale, source.HumanRealmScale);
            SelectOptionValue(cmbDwarvenRealmScale, source.DwarvenRealmScale);
            SelectOptionValue(cmbElvenRealmScale, source.ElvenRealmScale);
            SelectOptionValue(cmbHumanClanRealmScale, source.HumanClanRealmScale);
            SelectOptionValue(cmbOrcRealmScale, source.OrcRealmScale);
            SelectOptionValue(cmbBeastmanRealmScale, source.BeastmanRealmScale);
            SelectOptionValue(cmbTransitionalRealmScale, source.TransitionalRealmScale);
        }

        private void ResetAdvancedControlsToDefaults()
        {
            ApplyAdvancedControls(new RegionGenerationOptions());
        }

        private void SetNumericValue(NumericUpDown numeric, int value)
        {
            if (numeric == null) return;

            decimal clamped = Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, value));
            numeric.Value = clamped;
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

        private void UpdateAdvancedEnabled()
        {
            if (advancedPanel != null) advancedPanel.Enabled = chkAdvanced.Checked;

            // Слои цивилизации зависят друг от друга: дорогам и доменам нужны
            // точки интереса, а державам и вассалам нужны уже созданные домены.
            bool hasPlaces = chkSettlements.Checked || chkStrongholds.Checked;
            SetDependentLayerEnabled(chkRoads, hasPlaces);
            SetDependentLayerEnabled(chkDomains, hasPlaces);

            bool hasDomains = hasPlaces && chkDomains.Checked;
            SetDependentLayerEnabled(chkRealms, hasDomains);
            SetDependentLayerEnabled(chkRulers, hasDomains);

            bool specialSettlementsAllowed = chkSettlements.Checked && !chkDomains.Checked;
            chkSpecialSettlements.Enabled = specialSettlementsAllowed;
            if (!specialSettlementsAllowed) chkSpecialSettlements.Checked = false;

            bool specialDomainTypesAllowed = hasDomains || chkSpecialSettlements.Checked;
            SetDependentLayerEnabled(chkDwarvenDomains, specialDomainTypesAllowed);
            SetDependentLayerEnabled(chkElvenDomains, specialDomainTypesAllowed);
            SetDependentLayerEnabled(chkClanDomains, specialDomainTypesAllowed);
            SetDependentLayerEnabled(chkTransitionalDomains, hasDomains);

            chkDwarvenNames.Enabled = chkDwarvenDomains.Checked && chkDwarvenDomains.Enabled;
            chkElvenNames.Enabled = chkElvenDomains.Checked && chkElvenDomains.Enabled;
            chkClanNames.Enabled = chkClanDomains.Checked && chkClanDomains.Enabled;
            chkTransitionalNames.Enabled = chkTransitionalDomains.Checked && chkTransitionalDomains.Enabled;

            chkDungeons.Enabled = chkHexFeatures.Checked;
            if (!chkDungeons.Enabled) chkDungeons.Checked = false;
        }

        private void SetDependentLayerEnabled(CheckBox checkBox, bool enabled)
        {
            if (checkBox == null) return;
            checkBox.Enabled = enabled;
            if (!enabled) checkBox.Checked = false;
        }

        private void Accept()
        {
            Options = new RegionGenerationOptions
            {
                Seed = txtSeed.Text.Trim(),
                Width = (int)nudWidth.Value,
                Height = (int)nudHeight.Value,
                ClimateBelt = SelectedValue(cmbClimate),
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
                WaterLayout = SelectedValue(cmbWaterLayout),
                Seismicity = SelectedValue(cmbSeismicity),
                AdvancedMode = chkAdvanced.Checked,
                GenerateSettlements = chkSettlements.Checked,
                GenerateStrongholds = chkStrongholds.Checked,
                GenerateDomains = chkDomains.Checked,
                GenerateRealms = chkRealms.Checked,
                GenerateRulers = chkRulers.Checked,
                GenerateRoads = chkRoads.Checked,
                GenerateRivers = chkRivers.Checked,
                GenerateFeatureNames = chkFeatureNames.Checked,
                GenerateHexFeatures = chkHexFeatures.Checked,
                GenerateDungeons = chkDungeons.Checked,
                GenerateSpecialDomains = chkDwarvenDomains.Checked || chkElvenDomains.Checked || chkClanDomains.Checked || chkTransitionalDomains.Checked,
                GenerateDwarvenDomains = chkDwarvenDomains.Checked,
                GenerateElvenDomains = chkElvenDomains.Checked,
                GenerateClanDomains = chkClanDomains.Checked,
                GenerateTransitionalDomains = chkTransitionalDomains.Checked,
                GenerateSpecialSettlementsWithoutDomains = chkSpecialSettlements.Checked,
                UseRussianNames = !isEnglish,
                UseDwarvenCultureNames = chkDwarvenNames.Checked,
                UseElvenCultureNames = chkElvenNames.Checked,
                UseClanCultureNames = chkClanNames.Checked,
                UseTransitionalCultureNames = chkTransitionalNames.Checked,
                UseSpecialDomainWeights = chkAdvanced.Checked,
                TerrainZoneCount = (int)nudTerrainZones.Value,
                TerrainChaosPercent = (int)nudTerrainChaos.Value,
                WaterPercent = (int)nudWater.Value,
                LakePercent = (int)nudLakes.Value,
                RiverPercent = (int)nudRivers.Value,
                HillsPercent = (int)nudHills.Value,
                MountainsPercent = (int)nudMountains.Value,
                SettlementDensityPercent = (int)nudSettlementDensity.Value,
                DomainCoveragePercent = (int)nudDomainCoverage.Value,
                RealmCount = (int)nudRealmCount.Value,
                StateSizeVariancePercent = (int)nudVariance.Value,
                SpecialDomainPercent = (int)nudSpecialDomainPercent.Value,
                DwarvenDomainWeight = (int)nudDwarvenDomains.Value,
                ElvenDomainWeight = (int)nudElvenDomains.Value,
                ClanDomainWeight = (int)nudClanDomains.Value,
                TransitionalDomainWeight = (int)nudTransitionalDomains.Value,
                DefaultAgeIndex = int.Parse(SelectedValue(cmbAge)),
                LandValueMode = SelectedValue(cmbLandValue)
            };

            EnforceLayerDependencies(Options);

            if (string.IsNullOrWhiteSpace(Options.Seed))
            {
                Options.Seed = DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            lastAcceptedOptions = CreateRememberedOptions(Options);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static RegionGenerationOptions CreateRememberedOptions(RegionGenerationOptions source)
        {
            RegionGenerationOptions remembered = CloneOptions(source);
            remembered.Seed = "";

            if (!remembered.AdvancedMode)
            {
                RegionGenerationOptions defaults = new RegionGenerationOptions();
                remembered.TerrainZoneCount = defaults.TerrainZoneCount;
                remembered.TerrainChaosPercent = defaults.TerrainChaosPercent;
                remembered.WaterPercent = defaults.WaterPercent;
                remembered.LakePercent = defaults.LakePercent;
                remembered.RiverPercent = defaults.RiverPercent;
                remembered.HillsPercent = defaults.HillsPercent;
                remembered.MountainsPercent = defaults.MountainsPercent;
                remembered.SettlementDensityPercent = defaults.SettlementDensityPercent;
                remembered.DomainCoveragePercent = defaults.DomainCoveragePercent;
                remembered.RealmCount = defaults.RealmCount;
                remembered.StateSizeVariancePercent = defaults.StateSizeVariancePercent;
                remembered.SpecialDomainPercent = defaults.SpecialDomainPercent;
                remembered.DwarvenDomainWeight = defaults.DwarvenDomainWeight;
                remembered.ElvenDomainWeight = defaults.ElvenDomainWeight;
                remembered.ClanDomainWeight = defaults.ClanDomainWeight;
                remembered.TransitionalDomainWeight = defaults.TransitionalDomainWeight;
                remembered.DefaultAgeIndex = defaults.DefaultAgeIndex;
                remembered.LandValueMode = defaults.LandValueMode;
                remembered.HumanRealmScale = defaults.HumanRealmScale;
                remembered.DwarvenRealmScale = defaults.DwarvenRealmScale;
                remembered.ElvenRealmScale = defaults.ElvenRealmScale;
                remembered.HumanClanRealmScale = defaults.HumanClanRealmScale;
                remembered.OrcRealmScale = defaults.OrcRealmScale;
                remembered.BeastmanRealmScale = defaults.BeastmanRealmScale;
                remembered.TransitionalRealmScale = defaults.TransitionalRealmScale;
            }

            return remembered;
        }

        private static void EnforceLayerDependencies(RegionGenerationOptions options)
        {
            if (options == null) return;

            bool hasPlaces = options.GenerateSettlements || options.GenerateStrongholds;
            if (!hasPlaces)
            {
                options.GenerateRoads = false;
                options.GenerateDomains = false;
            }

            if (!options.GenerateDomains)
            {
                options.GenerateRealms = false;
                options.GenerateRulers = false;
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
                Seismicity = source.Seismicity,
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
                TerrainZoneCount = source.TerrainZoneCount,
                TerrainChaosPercent = source.TerrainChaosPercent,
                WaterPercent = source.WaterPercent,
                LakePercent = source.LakePercent,
                RiverPercent = source.RiverPercent,
                HillsPercent = source.HillsPercent,
                MountainsPercent = source.MountainsPercent,
                SettlementDensityPercent = source.SettlementDensityPercent,
                DomainCoveragePercent = source.DomainCoveragePercent,
                RealmCount = source.RealmCount,
                StateSizeVariancePercent = source.StateSizeVariancePercent,
                SpecialDomainPercent = source.SpecialDomainPercent,
                DwarvenDomainWeight = source.DwarvenDomainWeight,
                ElvenDomainWeight = source.ElvenDomainWeight,
                ClanDomainWeight = source.ClanDomainWeight,
                TransitionalDomainWeight = source.TransitionalDomainWeight,
                DefaultAgeIndex = source.DefaultAgeIndex,
                LandValueMode = source.LandValueMode
            };
        }

        private string SelectedValue(ComboBox combo)
        {
            OptionItem item = combo.SelectedItem as OptionItem;
            return item == null ? "" : item.Value;
        }

        private string SelectedCultureKey()
        {
            NameCultureInfo culture = cmbCulture.SelectedItem as NameCultureInfo;
            return culture == null ? "english" : culture.Key;
        }

        private string L(string english, string russian)
        {
            return isEnglish ? english : russian;
        }
    }
}
