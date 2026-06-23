﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace OSRCGG
{
    public partial class AcksToolkitForm : Form
    {
        // Глобальные переменные для данных
        double[] A; // Массив модификаторов (29 элементов согласно Image 2)
        double[] PartnerDemands; // Массив модификаторов партнера
        bool isEnglish = false; // Флаг языка

        // Названия товаров (согласно Image 2 и старой версии, объединены) - всего 29 штук
        string[] MerchandiseNames = {
            "Grain & Vegetables", "Salt", "Beer/Ale", "Pottery", "Common Wood",
            "Wine/Spirits", "Oil/Sauce", "Preserved Fish", "Preserved Meats", "Glassware",
            "Rare Wood", "Common Metals", "Common Furs", "Textiles", "Dyes/Pigments",
            "Botanicals", "Clothing", "Tools", "Armor/Weapons", "Monster Parts",
            "Ivory", "Rare Furs", "Spices", "Fine Porcelain", "Precious Metals",
            "Silk", "Rare Books & Art", "Semi-prec. Stones", "Gems"
        };

        // Объект для музыки
        public WMPLib.WindowsMediaPlayer WMP = new WMPLib.WindowsMediaPlayer();
        private const string ProgramVersion = "v2.4.0";
        private const string SettlementKindHuman = "Human";
        private const string SettlementKindDwarf = "Dwarf";
        private const string SettlementKindElf = "Elf";
        private const string SettlementKindHumanClanhold = "HumanClanhold";
        private const string SettlementKindOrcClanhold = "Orc";
        private const string SettlementKindBeastmanClanhold = "Beastman";
        private const string SettlementMetadataClanhold = "Clanhold";
        private readonly Random demandRandom = new Random();
        private readonly List<DemandBreakdownRow> generatorDemandBreakdown = new List<DemandBreakdownRow>();
        private Button btnHelpWindow;
        private HelpForm helpForm;
        private ComboBox cmbGeneratorRace;

        private sealed class DemandBreakdownRow
        {
            public string EnglishLabel { get; set; }
            public string RussianLabel { get; set; }
            public double[] Values { get; set; }
        }

        public AcksToolkitForm()
        {
            InitializeComponent();
            ConfigureTradeGridStyles();
            InitializeData();
            InitializeGeneratorRaceChoice();
            InitializeCharacterTab();
            InitializeMapTab();
            InitializeDungeonTab();
            InitializeTreasureTab();
            InitializeNameGeneratorTab();
            InitializeGeneratorBranding();
            InitializeThemedBackgrounds();
            ApplyUnifiedInterfaceStyle();
            InitializeResponsiveTabs();
            UpdateLanguage();
            InitializeHelpButton();
        }

        private void InitializeHelpButton()
        {
            if (btnHelpWindow != null) return;

            btnHelpWindow = new Button
            {
                Width = 82,
                Height = 24,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = isEnglish ? "Help" : "Справка"
            };
            UiTheme.StyleCommandButton(btnHelpWindow, UiTheme.NeutralButtonColor);
            btnHelpWindow.Click += (s, e) => OpenHelpWindow();
            Controls.Add(btnHelpWindow);
            PositionHelpButton();
            Resize += (s, e) => PositionHelpButton();
            Layout += (s, e) => PositionHelpButton();
            Shown += (s, e) => BeginInvoke(new Action(PositionHelpButton));
            btnHelpWindow.BringToFront();
        }

        private string L(string english, string russian)
        {
            return isEnglish ? english : russian;
        }

        private void PositionHelpButton()
        {
            if (btnHelpWindow == null) return;
            btnHelpWindow.Left = Math.Max(0, ClientSize.Width - btnHelpWindow.Width - 8);
            btnHelpWindow.Top = 2;
            btnHelpWindow.BringToFront();
        }

        private void OpenHelpWindow()
        {
            if (helpForm == null || helpForm.IsDisposed)
            {
                helpForm = new HelpForm(isEnglish);
            }
            else
            {
                helpForm.ApplyLanguage(isEnglish);
            }

            helpForm.Show(this);
            helpForm.BringToFront();
        }

        private void InitializeGeneratorBranding()
        {
            if (lblProgramVersion != null)
            {
                if (string.IsNullOrWhiteSpace(lblProgramVersion.Text))
                {
                    lblProgramVersion.Text = ProgramVersion;
                }

                lblProgramVersion.BackColor = Color.Transparent;
                lblProgramVersion.ForeColor = Color.Black;
                lblProgramVersion.Visible = false;
            }

            if (pictureBox1 != null)
            {
                pictureBox1.Paint -= PictureBox1_PaintProgramVersion;
                pictureBox1.Paint += PictureBox1_PaintProgramVersion;
                pictureBox1.Invalidate();
            }

            if (picProgramSign == null) return;

            string[] paths =
            {
                Path.Combine(Application.StartupPath, "MapAssets", "sign.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapAssets", "sign.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "MapAssets", "sign.png")
            };

            string imagePath = paths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(imagePath)) return;

            try
            {
                using (FileStream stream = File.OpenRead(imagePath))
                using (Image image = Image.FromStream(stream))
                {
                    picProgramSign.Image = new Bitmap(image);
                }
            }
            catch
            {
                picProgramSign.Image = null;
            }
        }

        private void PictureBox1_PaintProgramVersion(object sender, PaintEventArgs e)
        {
            PictureBox background = sender as PictureBox;
            if (background == null || lblProgramVersion == null) return;

            string versionText = string.IsNullOrWhiteSpace(lblProgramVersion.Text)
                ? ProgramVersion
                : lblProgramVersion.Text;
            Font font = lblProgramVersion.Font ?? new Font(UiTheme.FontFamily, 10f, FontStyle.Bold, GraphicsUnit.Point);
            Size textSize = TextRenderer.MeasureText(
                e.Graphics,
                versionText,
                font,
                Size.Empty,
                TextFormatFlags.NoPadding);

            int x = lblProgramVersion.Right - background.Left - textSize.Width;
            int y = lblProgramVersion.Top - background.Top + Math.Max(0, (lblProgramVersion.Height - textSize.Height) / 2);
            x = Math.Max(4, Math.Min(background.Width - textSize.Width - 4, x));
            y = Math.Max(4, Math.Min(background.Height - textSize.Height - 4, y));

            TextRenderer.DrawText(
                e.Graphics,
                versionText,
                font,
                new Point(x, y),
                Color.Black,
                TextFormatFlags.NoPadding);
        }

        private void InitializeThemedBackgrounds()
        {
            SetTabBackground(pictureBox1, "fon.png");
            SetTabBackground(pictureBox2, "fon.png");
            SetTabBackground(pictureBoxCharacters, "fon.png");

            Bitmap nameGeneratorBackground = LoadMapAssetBitmap("fon.png");
            if (nameGeneratorBackground != null && tabPageNameGenerator != null)
            {
                tabPageNameGenerator.BackgroundImage = nameGeneratorBackground;
                tabPageNameGenerator.BackgroundImageLayout = ImageLayout.Stretch;
            }
        }

        private void SetTabBackground(PictureBox pictureBox, string fileName)
        {
            if (pictureBox == null) return;

            Bitmap background = LoadMapAssetBitmap(fileName);
            if (background == null) return;

            pictureBox.Image = background;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.BackColor = Color.Transparent;
        }

        private Bitmap LoadMapAssetBitmap(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            string[] paths =
            {
                Path.Combine(Application.StartupPath, "MapAssets", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapAssets", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "MapAssets", fileName)
            };

            string imagePath = paths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(imagePath)) return null;

            try
            {
                using (FileStream stream = File.OpenRead(imagePath))
                using (Image image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch
            {
                return null;
            }
        }

        private void InitializeData()
        {
            A = new double[AcksRules.DemandCount];
            for (int i = 0; i < A.Length; i++) A[i] = 0;

            // Инициализация массива для Партнера
            PartnerDemands = new double[AcksRules.DemandCount];
            for (int i = 0; i < PartnerDemands.Length; i++) PartnerDemands[i] = 0;
        }

        private void AcksToolkitForm_Load(object sender, EventArgs e)
        {
            try
            {
                string musicPath = System.IO.Path.Combine(Application.StartupPath, "fon.mp3");
                if (System.IO.File.Exists(musicPath))
                {
                    WMP.URL = musicPath;
                    WMP.settings.volume = 100; // Устанавливаем громкость на максимум при старте
                    WMP.settings.setMode("loop", true); // Автоповтор, пока музыка не поставлена на паузу

                    // Добавляем эту строку для автоматического старта:
                    WMP.controls.play();

                    // Обновляем текст кнопки, чтобы пользователь видел, что музыка играет
                    btnMusicToggle.Text = isEnglish ? "⏸ Pause" : "⏸ Пауза";
                }
            }
            catch { }
        }

        // --- ЛОКАЛИЗАЦИЯ ---

        // Словарь для интерфейса
        Dictionary<string, string> UI_Texts = new Dictionary<string, string>()
        {
            {"FormTitle", "ACKS Judge's Toolkit"},
        };

        // Словарь для названий товаров
        Dictionary<string, string> ItemNames = new Dictionary<string, string>()
        {
            {"Grain & Vegetables", "Зерно и овощи"},
            {"Salt", "Соль"},
            {"Beer/Ale", "Пиво/Эль"},
            {"Pottery", "Гончарные изделия"},
            {"Common Wood", "Обычная древесина"},
            {"Wine/Spirits", "Вино/Крепкие напитки"},
            {"Oil/Sauce", "Масло/Соусы"},
            {"Preserved Fish", "Консервированная рыба"},
            {"Preserved Meats", "Консервированное мясо"},
            {"Glassware", "Стеклянные изделия"},
            {"Rare Wood", "Редкая древесина"},
            {"Common Metals", "Обычные металлы"},
            {"Common Furs", "Обычные меха"},
            {"Textiles", "Текстиль"},
            {"Dyes/Pigments", "Красители/Пигменты"},
            {"Botanicals", "Ботанические"},
            {"Clothing", "Одежда"},
            {"Tools", "Инструменты"},
            {"Armor/Weapons", "Доспехи/Оружие"},
            {"Monster Parts", "Части монстров"},
            {"Ivory", "Слоновая кость"},
            {"Rare Furs", "Редкие меха"},
            {"Spices", "Специи"},
            {"Fine Porcelain", "Тонкий фарфор"},
            {"Precious Metals", "Драгоценные металлы"},
            {"Silk", "Шелк"},
            {"Rare Books & Art", "Редкие книги и искусство"},
            {"Semi-prec. Stones", "Полудрагоценные камни"},
            {"Gems", "Драгоценные камни"}
        };

        private void UpdateLanguage()
        {
            // Обновление заголовков и кнопок
            this.Text = UI_Texts["FormTitle"];

            if (tabControl1.TabPages.Count > 0) tabControl1.TabPages[0].Text = isEnglish ? "Generator" : "Генератор";
            if (tabControl1.TabPages.Count > 1) tabControl1.TabPages[1].Text = isEnglish ? "Trade Routes" : "Торговые пути";
            UpdateCharacterLanguage();
            UpdateNameGeneratorLanguage();
            UpdateMapLanguage();
            UpdateDungeonLanguage();
            UpdateTreasureLanguage();
            if (btnHelpWindow != null) btnHelpWindow.Text = isEnglish ? "Help" : "Справка";
            if (helpForm != null && !helpForm.IsDisposed) helpForm.ApplyLanguage(isEnglish);

            // Кнопки
            btnGenerate.Text = isEnglish ? "Generate" : "Генерировать";
            btnApply.Text = isEnglish ? "Apply modifiers" : "Учесть факторы";
            btnExport.Text = isEnglish ? "Export" : "Экспорт";
            btnClose.Text = isEnglish ? "Exit" : "Выход";

            lblPartnerClass.Text = isEnglish ? "Class of market-partner" : "Класс рынка-партнера:";
            lblDistance.Text = isEnglish ? "Distance (miles)" : "Расстояние (миль):";
            btnCalcRoute.Text = isEnglish ? "Calculate" : "Рассчитать";
            btnExportRoute.Text = isEnglish ? "Export A" : "Экспорт A";
            ConfigureTradeCalculationLogGrid();

            // Переключатель языка
            btnLang.Text = isEnglish ? "EN" : "RU";

            // Обновление текстов в панелях (Labels)
            label3Title.Text = isEnglish ? "Age" : "Возраст";
            label4Title.Text = isEnglish ? "Water Src" : "Вода";
            label5Title.Text = isEnglish ? "Biome" : "Климат";
            label6Title.Text = isEnglish ? "Elevation" : "Высота";
            labelRaceTitle.Text = isEnglish ? "Race" : "Раса";

            // Обновление текстов RadioButtons (Age)
            radioButton1.Text = isEnglish ? "0-20 years" : "0-20 лет";
            radioButton2.Text = isEnglish ? "21-100 years" : "21-100 лет";
            radioButton3.Text = isEnglish ? "101-1,000 years" : "101-1,000 лет";
            radioButton4.Text = isEnglish ? "1,001-2000 years" : "1,001-2000 лет";
            radioButton5.Text = isEnglish ? "2,001+ years" : "2,001+ лет";
            
            // Water
            radioSea.Text = isEnglish ? "Sea" : "Море";
            radioLake.Text = isEnglish ? "Lake" : "Озеро";
            radioRiver.Text = isEnglish ? "River" : "Река";

            // Biome
            radioRainforest.Text = isEnglish ? "Rainforest" : "Троп. лес";
            radioSavanna.Text = isEnglish ? "Savanna" : "Саванна";
            radioDesert.Text = isEnglish ? "Desert" : "Пустыня";
            radioSteppe.Text = isEnglish ? "Steppe" : "Степь";
            radioForest.Text = isEnglish ? "Forest" : "Лес";
            radioTaiga.Text = isEnglish ? "Taiga" : "Тайга";
            radioTundra.Text = isEnglish ? "Tundra" : "Тундра";
            radioScrub.Text = isEnglish ? "Scrub" : "Кустарник";
            radioGrasslands.Text = isEnglish ? "Grasslands" : "Луга";

            // Elevation
            radioPlains.Text = isEnglish ? "Plains" : "Равнины";
            radioHills.Text = isEnglish ? "Hills" : "Холмы";
            radioMountains.Text = isEnglish ? "Mountains" : "Горы";

            // Race
            UpdateGeneratorRaceChoiceLanguage();

            // Trade Route Labels
            lblMarketAClass.Text = isEnglish ? "Market A Class:" : "Класс Рынка А:";
            rbtnConnectionRoad.Text = isEnglish ? "Road" : "Дорога";
            rbtnConnectionWater.Text = isEnglish ? "Water" : "Вода";

            // Land Revenue Panel
            labelLandRevTitle.Text = isEnglish ? "Domain Land Revenue:" : "Доход Земли:";
            btnApplyLandMod.Text = isEnglish ? "Apply Modifiers" : "Применить Модификаторы";

            // Buttons in trade routes
            btnImportPartner.Text = isEnglish ? "Import Partner Demands (excel)" : "Импорт спроса Партнера (excel)";
            btnImportMarketA.Text = isEnglish ? "Import Market A Demands (excel)" : "Импорт спроса Рынка А (excel)";

            // Кнопка музыки
            btnMusicToggle.Text = isEnglish ? "▶ Play" : "▶ Играть";

            // Если музыка уже играет, обновим текст на Паузу (опционально, лучше делать в обработчике)
            if (WMP.playState == WMPLib.WMPPlayState.wmppsPlaying)
                btnMusicToggle.Text = isEnglish ? "⏸ Pause" : "⏸ Пауза";

            if (dataGridView1 != null && dataGridView1.ColumnCount > 0)
            {
                UpdateDataGridView(dataGridView1);
            }

            LayoutAllResponsiveTabs();
        }
        
        private void btnLang_Click(object sender, EventArgs e)
        {
            isEnglish = !isEnglish;
            UpdateLanguage();
        }

        // --- ГЕНЕРАЦИЯ И ПРИМЕНЕНИЕ МОДИФИКАТОРОВ ---

        private void button1_Click(object sender, EventArgs e)
        {
            A = RollBaseDemandModifiers();
            ClearGeneratorDemandBreakdown();
            AddGeneratorDemandBreakdown("Base random roll", "Базовый случайный бросок", A);

            UpdateDataGridView(dataGridView1);

            btnApply.Enabled = true;
            btnApplyLandMod.Enabled = true;
        }

        private double[] RollBaseDemandModifiers()
        {
            return RollBaseDemandModifiers(demandRandom);
        }

        private double[] RollBaseDemandModifiers(Random random)
        {
            int length = MerchandiseNames == null || MerchandiseNames.Length == 0 ? 29 : MerchandiseNames.Length;
            double[] values = new double[length];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = random.Next(1, 4) - random.Next(1, 4);
            }
            return values;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // ... (existing logic for ApplyAge, ApplyWater, etc.) ...
            EnsureGeneratorDemandBreakdownStart();

            // 1. Применение Environmental Adjustments (Image 2)
            // Age
            if (radioButton1.Checked) ApplyAge(0);      // 0-20 years
            else if (radioButton2.Checked) ApplyAge(1); // 21-100
            else if (radioButton3.Checked) ApplyAge(2); // 101-1,000
            else if (radioButton4.Checked) ApplyAge(3); // 1,001-2,000
            else if (radioButton5.Checked) ApplyAge(4); // 2,001+

            if (radioSea.Checked) ApplyWater("Sea");
            if (radioLake.Checked) ApplyWater("Lake");
            if (radioRiver.Checked) ApplyWater("River");

            if (radioRainforest.Checked) ApplyBiome("Rainforest");
            else if (radioSavanna.Checked) ApplyBiome("Savanna");
            else if (radioDesert.Checked) ApplyBiome("Desert");
            else if (radioSteppe.Checked) ApplyBiome("Steppe");
            else if (radioForest.Checked) ApplyBiome("Forest");
            else if (radioTaiga.Checked) ApplyBiome("Taiga");
            else if (radioTundra.Checked) ApplyBiome("Tundra");
            else if (radioScrub.Checked) ApplyBiome("Scrub");
            else if (radioGrasslands.Checked) ApplyBiome("Grasslands");

            if (radioPlains.Checked) ApplyElev(0);
            else if (radioHills.Checked) ApplyElev(1);
            else if (radioMountains.Checked) ApplyElev(2);

            // 2. Раса поселения выбирается взаимоисключающе. Для спроса модификаторы
            // есть только у дварфов и эльфов; люди, клановые люди, орки и зверолюды
            // влияют на размещение/иконку карты, но не меняют demand.
            string demandRace = GetGeneratorSettlementRace();
            if (IsDemandModifierRace(demandRace)) ApplyRace(demandRace);

            UpdateDataGridView(dataGridView1);

            // Обновляем и сетку Рынка А на вкладке торговых путей, если она есть
            UpdateTradeGrids();

            btnApply.Enabled = false;
            btnGenerate.Text = isEnglish ? "Regenerate" : "Заново";
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ТАБЛИЦ (Исправлены длины массивов до 29) ---

        private void ApplyAge(int ageIndex)
        {
            double[] adjustment = GetAgeAdjustment(ageIndex);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Age: " + GetAgeLabel(ageIndex, true),
                "Возраст: " + GetAgeLabel(ageIndex, false),
                adjustment);
        }

        private double[] GetAgeAdjustment(int ageIndex)
        {
            switch (ageIndex)
            {
                case 0: // 0-20 Years
                    return new double[29] { -1, -1, +0.5, +1, -1, +0.5, +0.5, +0.5, +0.5, +1, -1.5, -1, -1, -1, +1, -1, -1, +1, +1, -1, -1, -1, +0.5, +1, -1.5, +0.5, +1, -1.5, -1.5 };
                case 1: // 21-100 Years
                    return new double[29] { -1, -0.5, -0.5, +0.5, -0.5, -0.5, -0.5, -0.5, -0.5, +0.5, -0.5, -0.5, -0.5, -0.5, +0.5, -0.5, -0.5, +0.5, +0.5, -0.5, -0.5, -0.5, -0.5, +0.5, -0.5, -0.5, +0.5, -0.5, -0.5 };
                case 2: // 101-1,000 Years (Все значения 0 согласно таблице)
                    return new double[29] { 0, 0, -0.5, 0, 0, -0.5, -0.5, -0.5, -0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.5, 0, 0, -0.5, 0, 0, 0 };
                case 3: // 1,001-2,000 Years
                    return new double[29] { +2, +0.5, -0.5, -0.5, +1, -0.5, -0.5, -0.5, -0.5, -0.5, +1, +0.5, +0.5, +0.5, -0.5, +0.5, +0.5, -0.5, -0.5, +0.5, +1, +1, -0.5, -0.5, +0.5, -0.5, -0.5, +0.5, +0.5 };
                case 4: // 2,001+ Years
                    return new double[29] { +3, +1, 0, -1, +2, 0, 0, +0.5, +0.5, -1, +2, +1, +1, +1, -1, +1, +1, -1, -1, +1, +2, +2, +1, -1, +1.5, +1, -1, +2, +2 };
                default:
                    return new double[AcksRules.DemandCount];
            }
        }

        private void ApplyWater(string water)
        {
            double[] adjustment = GetMapWaterAdjustment(water);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Water: " + GetWaterLabel(water, true),
                "Вода: " + GetWaterLabel(water, false),
                adjustment);
        }

        private void ApplyBiome(string biomeName)
        {
            double[] adjustment = GetMapTerrainAdjustment(biomeName);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Biome: " + GetBiomeLabel(biomeName, true),
                "Климат: " + GetBiomeLabel(biomeName, false),
                adjustment);
        }

        private void ApplyElev(int elevIndex)
        {
            string elevation = elevIndex == 1 ? "Hills" : elevIndex == 2 ? "Mountains" : "Plains";
            double[] adjustment = GetMapElevationAdjustment(elevation);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Elevation: " + GetElevationLabel(elevation, true),
                "Высота: " + GetElevationLabel(elevation, false),
                adjustment);
        }

        private void ApplyRace(string raceName)
        {
            double[] adjustment = GetRaceAdjustment(raceName);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Race: " + GetRaceLabel(raceName, true),
                "Раса: " + GetRaceLabel(raceName, false),
                adjustment);
        }

        private double[] GetRaceAdjustment(string raceName)
        {
            double[] vals = new double[AcksRules.DemandCount];
            if (raceName == "Dwarf")
            {
                vals[2] -= 2; vals[11] -= 2; vals[17] -= 2; vals[18] -= 2; vals[24] -= 2; vals[27] -= 2; vals[28] -= 2;
                vals[0] += 2; vals[4] += 2; vals[6] += 2; vals[10] += 2; vals[12] += 2; vals[20] += 2; vals[21] += 2;
            }
            else if (raceName == "Elf")
            {
                vals[4] -= 2; vals[9] -= 2; vals[13] -= 2; vals[14] -= 2; vals[15] -= 2; vals[16] -= 2; vals[23] -= 2;
                vals[0] += 2; vals[1] += 2; vals[19] += 2; vals[25] += 2; vals[26] += 2; vals[27] += 2; vals[28] += 2;
            }
            return vals;
        }

        private void InitializeGeneratorRaceChoice()
        {
            if (panelRace == null || cmbGeneratorRace != null) return;

            cmbGeneratorRace = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(6, 30),
                Size = new Size(88, 24)
            };
            panelRace.Controls.Add(cmbGeneratorRace);
            cmbGeneratorRace.BringToFront();
            cmbGeneratorRace.SelectedIndexChanged += (s, e) =>
            {
                SyncLegacyRaceChecks();
                if (IsGeneratorClanholdKind())
                {
                    SelectMarketClassCombo(cmbGeneratedSettlementClass, 6);
                    SelectMarketClassCombo(cmbMapSettlementClass, 6);
                }
            };
            if (cmbGeneratedSettlementClass != null)
            {
                cmbGeneratedSettlementClass.SelectedIndexChanged += (s, e) =>
                {
                    if (IsGeneratorClanholdKind()) SelectMarketClassCombo(cmbGeneratedSettlementClass, 6);
                };
            }

            if (cmbMapSettlementClass != null)
            {
                cmbMapSettlementClass.SelectedIndexChanged += (s, e) =>
                {
                    if (IsGeneratorClanholdKind()) SelectMarketClassCombo(cmbMapSettlementClass, 6);
                };
            }

            if (checkDwarf != null) checkDwarf.Visible = false;
            if (checkElf != null) checkElf.Visible = false;
            UpdateGeneratorRaceChoiceLanguage();
            SelectGeneratorRaceKind(SettlementKindHuman);
        }

        private void UpdateGeneratorRaceChoiceLanguage()
        {
            if (checkDwarf != null) checkDwarf.Text = isEnglish ? "Dwarf" : "Дварф";
            if (checkElf != null) checkElf.Text = isEnglish ? "Elf" : "Эльф";
            if (cmbGeneratorRace == null) return;

            string selected = GetGeneratorSettlementKind();
            cmbGeneratorRace.Items.Clear();
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindHuman, isEnglish ? "Human" : "Человек"));
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindDwarf, isEnglish ? "Dwarf" : "Дварф"));
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindElf, isEnglish ? "Elf" : "Эльф"));
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindHumanClanhold, isEnglish ? "Human clanhold" : "Клановые люди"));
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindOrcClanhold, isEnglish ? "Orc" : "Орк"));
            cmbGeneratorRace.Items.Add(new GeneratorRaceItem(SettlementKindBeastmanClanhold, isEnglish ? "Beastman" : "Зверолюд"));
            SelectGeneratorRaceKind(string.IsNullOrWhiteSpace(selected) ? SettlementKindHuman : selected);
        }

        private void SelectGeneratorRaceKind(string kind)
        {
            if (cmbGeneratorRace == null) return;
            for (int i = 0; i < cmbGeneratorRace.Items.Count; i++)
            {
                GeneratorRaceItem item = cmbGeneratorRace.Items[i] as GeneratorRaceItem;
                if (item != null && string.Equals(item.Value, kind, StringComparison.OrdinalIgnoreCase))
                {
                    cmbGeneratorRace.SelectedIndex = i;
                    SyncLegacyRaceChecks();
                    return;
                }
            }

            if (cmbGeneratorRace.Items.Count > 0) cmbGeneratorRace.SelectedIndex = 0;
            SyncLegacyRaceChecks();
        }

        private string GetGeneratorSettlementKind()
        {
            GeneratorRaceItem item = cmbGeneratorRace == null ? null : cmbGeneratorRace.SelectedItem as GeneratorRaceItem;
            if (item != null && !string.IsNullOrWhiteSpace(item.Value)) return item.Value;
            if (checkDwarf != null && checkDwarf.Checked) return SettlementKindDwarf;
            if (checkElf != null && checkElf.Checked) return SettlementKindElf;
            return SettlementKindHuman;
        }

        private string GetGeneratorSettlementRace()
        {
            string kind = GetGeneratorSettlementKind();
            if (string.Equals(kind, SettlementKindDwarf, StringComparison.OrdinalIgnoreCase)) return "Dwarf";
            if (string.Equals(kind, SettlementKindElf, StringComparison.OrdinalIgnoreCase)) return "Elf";
            if (string.Equals(kind, SettlementKindOrcClanhold, StringComparison.OrdinalIgnoreCase)) return "Orc";
            if (string.Equals(kind, SettlementKindBeastmanClanhold, StringComparison.OrdinalIgnoreCase)) return "Beastman";
            return "Human";
        }

        private bool IsGeneratorClanholdKind()
        {
            return IsClanholdSettlementKind(GetGeneratorSettlementKind());
        }

        private bool IsClanholdSettlementKind(string kind)
        {
            return string.Equals(kind, SettlementKindHumanClanhold, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, SettlementKindOrcClanhold, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, SettlementKindBeastmanClanhold, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDemandModifierRace(string race)
        {
            return string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(race, "Elf", StringComparison.OrdinalIgnoreCase);
        }

        private string SettlementKindFromRecord(MapSettlementRecord record)
        {
            if (record == null) return SettlementKindHuman;
            bool clanhold = IsClanholdSettlementRecord(record);
            string race = MapDataNormalizer.SettlementRace(record.Race);
            if (clanhold && race == "Orc") return SettlementKindOrcClanhold;
            if (clanhold && race == "Beastman") return SettlementKindBeastmanClanhold;
            if (clanhold) return SettlementKindHumanClanhold;
            if (race == "Dwarf") return SettlementKindDwarf;
            if (race == "Elf") return SettlementKindElf;
            return SettlementKindHuman;
        }

        private bool IsClanholdSettlementRecord(MapSettlementRecord settlement)
        {
            return settlement != null
                && HasSettlementMetadataToken(settlement.LandValue, SettlementMetadataClanhold);
        }

        private bool HasSettlementMetadataToken(string metadata, string token)
        {
            if (string.IsNullOrWhiteSpace(metadata) || string.IsNullOrWhiteSpace(token)) return false;

            string[] parts = metadata.Split(new[] { ';', ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (string.Equals(part.Trim(), token, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private string BuildSettlementLandValueMetadata(string metadata, int landValueGp)
        {
            string gpToken = AcksDomainRules.ClampLandValue(landValueGp).ToString(CultureInfo.InvariantCulture) + "gp";
            return HasSettlementMetadataToken(metadata, SettlementMetadataClanhold)
                ? SettlementMetadataClanhold + ";" + gpToken
                : gpToken;
        }

        private int ParseSettlementLandValueGp(string metadata, int fallback)
        {
            int value;
            if (TryParseSettlementLandValueGp(metadata, out value)) return value;

            return AcksDomainRules.ClampLandValue(fallback);
        }

        private bool TryParseSettlementLandValueGp(string metadata, out int gp)
        {
            int value;
            gp = 6;
            if (string.IsNullOrWhiteSpace(metadata)) return false;

            string[] parts = metadata.Split(new[] { ';', ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (part.EndsWith("gp", StringComparison.OrdinalIgnoreCase))
                {
                    part = part.Substring(0, part.Length - 2);
                }

                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    gp = AcksDomainRules.ClampLandValue(value);
                    return true;
                }
            }

            return false;
        }

        private void SyncLegacyRaceChecks()
        {
            string race = GetGeneratorSettlementRace();
            if (checkDwarf != null) checkDwarf.Checked = string.Equals(race, "Dwarf", StringComparison.OrdinalIgnoreCase);
            if (checkElf != null) checkElf.Checked = string.Equals(race, "Elf", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class GeneratorRaceItem
        {
            public string Value { get; private set; }
            private readonly string label;

            public GeneratorRaceItem(string value, string label)
            {
                Value = value;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }

        private void ClearGeneratorDemandBreakdown()
        {
            generatorDemandBreakdown.Clear();
        }

        private void EnsureGeneratorDemandBreakdownStart()
        {
            if (generatorDemandBreakdown.Count == 0 && A != null)
            {
                AddGeneratorDemandBreakdown("Initial values", "Исходные значения", A);
            }
        }

        private void AddGeneratorDemandBreakdown(string englishLabel, string russianLabel, double[] values)
        {
            if (values == null) return;

            generatorDemandBreakdown.Add(new DemandBreakdownRow
            {
                EnglishLabel = englishLabel,
                RussianLabel = russianLabel,
                Values = CopyDemandValues(values)
            });
        }

        private double[] CopyDemandValues(double[] values)
        {
            int length = MerchandiseNames == null || MerchandiseNames.Length == 0
                ? AcksRules.DemandCount
                : MerchandiseNames.Length;
            double[] copy = new double[length];
            Array.Copy(values, copy, Math.Min(values.Length, copy.Length));
            return copy;
        }

        private string GetAgeLabel(int ageIndex, bool english)
        {
            switch (ageIndex)
            {
                case 0: return english ? "0-20 years" : "0-20 лет";
                case 1: return english ? "21-100 years" : "21-100 лет";
                case 2: return english ? "101-1,000 years" : "101-1,000 лет";
                case 3: return english ? "1,001-2,000 years" : "1,001-2,000 лет";
                case 4: return english ? "2,001+ years" : "2,001+ лет";
                default: return english ? "Unknown" : "Неизвестно";
            }
        }

        private string GetWaterLabel(string water, bool english)
        {
            if (string.Equals(water, "Sea", StringComparison.OrdinalIgnoreCase)) return english ? "Sea" : "Море";
            if (string.Equals(water, "Lake", StringComparison.OrdinalIgnoreCase)) return english ? "Lake" : "Озеро";
            if (string.Equals(water, "River", StringComparison.OrdinalIgnoreCase)) return english ? "River" : "Река";
            return water;
        }

        private string GetBiomeLabel(string biomeName, bool english)
        {
            switch (biomeName)
            {
                case "Rainforest": return english ? "Rainforest" : "Тропический лес";
                case "Savanna": return english ? "Savanna" : "Саванна";
                case "Desert": return english ? "Desert" : "Пустыня";
                case "Steppe": return english ? "Steppe" : "Степь";
                case "Forest": return english ? "Forest" : "Лес";
                case "Taiga": return english ? "Taiga" : "Тайга";
                case "Tundra": return english ? "Tundra" : "Тундра";
                case "Scrub": return english ? "Scrub" : "Кустарник";
                case "Grasslands": return english ? "Grasslands" : "Луга";
                default: return biomeName;
            }
        }

        private string GetElevationLabel(string elevation, bool english)
        {
            if (string.Equals(elevation, "Hills", StringComparison.OrdinalIgnoreCase)) return english ? "Hills" : "Холмы";
            if (string.Equals(elevation, "Mountains", StringComparison.OrdinalIgnoreCase)) return english ? "Mountains" : "Горы";
            return english ? "Plains" : "Равнины";
        }

        private string GetRaceLabel(string raceName, bool english)
        {
            if (string.Equals(raceName, "Dwarf", StringComparison.OrdinalIgnoreCase)) return english ? "Dwarf" : "Дварф";
            if (string.Equals(raceName, "Elf", StringComparison.OrdinalIgnoreCase)) return english ? "Elf" : "Эльф";
            return raceName;
        }

        // --- ОБНОВЛЕНИЕ DataGridView ---
        private void UpdateDataGridView(DataGridView dgv)
        {
            if (dgv == null || A == null) return;

            // Очищаем и настраиваем для горизонтального вида с двумя строками
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            ApplyDemandGridVisualStyle(dgv);

            dgv.ColumnHeadersVisible = false;
            dgv.RowHeadersVisible = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ScrollBars = ScrollBars.Both;

            bool showBreakdown = dgv == dataGridView1 && generatorDemandBreakdown.Count > 0;
            int demandCount = Math.Min(A.Length, MerchandiseNames.Length);
            int breakdownRows = showBreakdown ? generatorDemandBreakdown.Count : 0;
            SetDemandGridRowHeaderWidth(dgv, showBreakdown);

            // 0 - названия товаров, 1 - итог, ниже - только экранная расшифровка расчёта.
            dgv.RowCount = 2 + breakdownRows;
            dgv.ColumnCount = demandCount;

            dgv.Rows[0].HeaderCell.Value = isEnglish ? "Good" : "Товар";
            dgv.Rows[1].HeaderCell.Value = isEnglish ? "Final" : "Итог";
            dgv.Rows[1].DefaultCellStyle.Font = showBreakdown
                ? new Font(dgv.Font, FontStyle.Bold)
                : dgv.Font;

            for (int i = 0; i < demandCount; i++)
            {
                // Строка 0: Названия товаров
                dgv.Rows[0].Cells[i].Value = LocalizedMerchandiseName(i);

                // Строка 1: Значения модификаторов
                dgv.Rows[1].Cells[i].Value = FormatDemandValue(A[i]);
            }

            for (int rowIndex = 0; rowIndex < breakdownRows; rowIndex++)
            {
                DemandBreakdownRow breakdown = generatorDemandBreakdown[rowIndex];
                DataGridViewRow row = dgv.Rows[rowIndex + 2];
                string label = isEnglish ? breakdown.EnglishLabel : breakdown.RussianLabel;
                row.HeaderCell.Value = label;
                row.HeaderCell.ToolTipText = label;
                row.DefaultCellStyle.BackColor = rowIndex % 2 == 0
                    ? UiTheme.Accent2Color
                    : Color.FromArgb(0xF2, 0xE8, 0xC6);
                row.DefaultCellStyle.ForeColor = UiTheme.TextColor;

                for (int i = 0; i < demandCount && i < breakdown.Values.Length; i++)
                {
                    row.Cells[i].Value = FormatDemandChange(breakdown.Values[i]);
                }
            }
        }
        private void UpdateTradeGrids()
        {
            // Обновляем сетку Рынка А (используем глобальный массив A)
            UpdateDataGridView(dgvMarketA);

            // Обновляем сетку Партнера (используем PartnerDemands)
            dgvPartner.Columns.Clear();
            dgvPartner.Rows.Clear();
            ApplyDemandGridVisualStyle(dgvPartner);
            dgvPartner.ColumnHeadersVisible = false;
            dgvPartner.RowHeadersVisible = true;
            dgvPartner.AllowUserToAddRows = false;
            dgvPartner.AllowUserToDeleteRows = false;
            dgvPartner.ScrollBars = ScrollBars.Both;
            SetDemandGridRowHeaderWidth(dgvPartner, false);
            dgvPartner.RowCount = 2;
            dgvPartner.ColumnCount = A.Length;
            dgvPartner.Rows[0].HeaderCell.Value = isEnglish ? "Good" : "Товар";
            dgvPartner.Rows[1].HeaderCell.Value = isEnglish ? "Final" : "Итог";
            dgvPartner.Rows[1].DefaultCellStyle.Font = dgvPartner.Font;

            for (int i = 0; i < A.Length; i++)
            {
                if (i < MerchandiseNames.Length)
                    dgvPartner.Rows[0].Cells[i].Value = LocalizedMerchandiseName(i);

                dgvPartner.Rows[1].Cells[i].Value = FormatDemandValue(PartnerDemands[i]);
            }
        }

        private void ConfigureTradeGridStyles()
        {
            ApplyDemandGridVisualStyle(dgvMarketA);
            ApplyDemandGridVisualStyle(dgvPartner);
            ConfigureTradeCalculationLogGrid();
        }

        private void ApplyDemandGridVisualStyle(DataGridView dgv)
        {
            if (dgv == null) return;

            dgv.EnableHeadersVisualStyles = false;
            dgv.BackgroundColor = UiTheme.Accent2Color;
            dgv.GridColor = UiTheme.TextColor;

            dgv.DefaultCellStyle.BackColor = UiTheme.Accent2Color;
            dgv.DefaultCellStyle.ForeColor = UiTheme.TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = UiTheme.FieldColor;
            dgv.DefaultCellStyle.SelectionForeColor = UiTheme.TextColor;

            dgv.RowsDefaultCellStyle.BackColor = UiTheme.Accent2Color;
            dgv.RowsDefaultCellStyle.ForeColor = UiTheme.TextColor;
            dgv.RowsDefaultCellStyle.SelectionBackColor = UiTheme.FieldColor;
            dgv.RowsDefaultCellStyle.SelectionForeColor = UiTheme.TextColor;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF1, 0xE8, 0xC8);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = UiTheme.TextColor;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiTheme.FieldColor;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiTheme.TextColor;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Accent1Color;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiTheme.Accent1Color;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiTheme.TextColor;
            dgv.RowHeadersDefaultCellStyle.BackColor = UiTheme.Accent1Color;
            dgv.RowHeadersDefaultCellStyle.ForeColor = UiTheme.TextColor;
            dgv.RowHeadersDefaultCellStyle.SelectionBackColor = UiTheme.Accent1Color;
            dgv.RowHeadersDefaultCellStyle.SelectionForeColor = UiTheme.TextColor;
            dgv.RowHeadersDefaultCellStyle.Font = dgv.Font;
            dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void SetDemandGridRowHeaderWidth(DataGridView dgv, bool showBreakdown)
        {
            if (dgv == null) return;
            dgv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgv.RowHeadersWidth = showBreakdown ? 190 : 96;
        }

        private void SaveGridToData(DataGridView dgv, double[] targetArray)
        {
            int count = Math.Min(targetArray.Length, dgv.ColumnCount);
            for (int i = 0; i < count; i++)
            {
                object cellValue = dgv.Rows[1].Cells[i].Value;
                string text = cellValue == null ? string.Empty : cellValue.ToString().Replace(",", ".");
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    targetArray[i] = val;
                }
            }
        }

        // --- ЭКСПОРТ В EXCEL (Вкладка 1) ---
        private void button2_Click(object sender, EventArgs e)
        {
            ExportDemandArrayToXlsx(A, isEnglish ? "market-demands" : "спрос-рынка");
        }

        // --- ВКЛАДКА ТОРГОВЫХ ПУТЕЙ (Trade Routes) ---

        private void btnCalcRoute_Click(object sender, EventArgs e)
        {
            // 1. Parse Inputs
            int classA = ParseClass(cmbMarketAClass.Text);
            int classP = ParseClass(cmbPartnerClass.Text);

            if (classA == -1 || classP == -1)
            {
                MessageBox.Show(isEnglish ? "Please select valid Market Classes (I-VI)." : "Выберите корректные классы рынков (I-VI).");
                return;
            }

            if (!double.TryParse(txtDistance.Text, out double distance))
            {
                MessageBox.Show(isEnglish ? "Please enter a valid Distance." : "Введите корректное расстояние.");
                return;
            }

            bool isRoad = rbtnConnectionRoad.Checked;
            SaveGridToData(dgvMarketA, A);
            SaveGridToData(dgvPartner, PartnerDemands);
            double[] marketABefore = (double[])A.Clone();
            double[] partnerBefore = (double[])PartnerDemands.Clone();

            // 2. Check Connectivity
            if (!AcksRules.IsTradeRouteInRange(classA, classP, isRoad, distance))
            {
                ShowTradeCalculationMessage(isEnglish
                    ? "Calculation was not applied: the route is outside trade range."
                    : "Расчёт не применён: путь находится вне радиуса торговли.");
                MessageBox.Show(isEnglish
                    ? "Markets are not connected by this route type.\nDistance exceeds one or both market's trade range."
                    : "Рынки не соединены этим типом пути.\nРасстояние превышает радиус торговли одного или обоих рынков.");
                return;
            }

            AcksRules.ApplyTradeInfluence(classA, classP, A, PartnerDemands);

            UpdateTradeGrids();
            UpdateTradeCalculationLog(marketABefore, partnerBefore);
        }

        private void ConfigureTradeCalculationLogGrid()
        {
            if (dgvTradeResult == null) return;

            ApplyDemandGridVisualStyle(dgvTradeResult);
            dgvTradeResult.AllowUserToAddRows = false;
            dgvTradeResult.AllowUserToDeleteRows = false;
            dgvTradeResult.ReadOnly = true;
            dgvTradeResult.RowHeadersVisible = false;
            dgvTradeResult.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTradeResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            string[] headers = isEnglish
                ? new[] { "Good", "A before", "A change", "A after", "Partner before", "Partner change", "Partner after" }
                : new[] { "Товар", "A до", "A изм.", "A после", "Партнёр до", "Партнёр изм.", "Партнёр после" };

            if (dgvTradeResult.Columns.Count != headers.Length)
            {
                dgvTradeResult.Columns.Clear();
                for (int i = 0; i < headers.Length; i++)
                {
                    dgvTradeResult.Columns.Add("tradeLogColumn" + i, headers[i]);
                }
            }
            else
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    dgvTradeResult.Columns[i].HeaderText = headers[i];
                }
            }

            if (dgvTradeResult.Columns.Count >= headers.Length)
            {
                dgvTradeResult.Columns[0].FillWeight = 210;
                dgvTradeResult.Columns[1].FillWeight = 70;
                dgvTradeResult.Columns[2].FillWeight = 75;
                dgvTradeResult.Columns[3].FillWeight = 70;
                dgvTradeResult.Columns[4].FillWeight = 95;
                dgvTradeResult.Columns[5].FillWeight = 100;
                dgvTradeResult.Columns[6].FillWeight = 95;
            }

            foreach (DataGridViewRow row in dgvTradeResult.Rows)
            {
                int? demandIndex = row.Tag as int?;
                if (demandIndex.HasValue && demandIndex.Value >= 0 && demandIndex.Value < MerchandiseNames.Length)
                {
                    row.Cells[0].Value = LocalizedMerchandiseName(demandIndex.Value);
                }
            }
        }

        private void UpdateTradeCalculationLog(double[] marketABefore, double[] partnerBefore)
        {
            if (dgvTradeResult == null || marketABefore == null || partnerBefore == null) return;

            ConfigureTradeCalculationLogGrid();
            dgvTradeResult.Rows.Clear();

            int count = new[] { MerchandiseNames.Length, marketABefore.Length, partnerBefore.Length, A.Length, PartnerDemands.Length }.Min();
            for (int i = 0; i < count; i++)
            {
                double marketAChange = A[i] - marketABefore[i];
                double partnerChange = PartnerDemands[i] - partnerBefore[i];
                int rowIndex = dgvTradeResult.Rows.Add(
                    LocalizedMerchandiseName(i),
                    FormatDemandValue(marketABefore[i]),
                    FormatDemandChange(marketAChange),
                    FormatDemandValue(A[i]),
                    FormatDemandValue(partnerBefore[i]),
                    FormatDemandChange(partnerChange),
                    FormatDemandValue(PartnerDemands[i]));

                DataGridViewRow row = dgvTradeResult.Rows[rowIndex];
                row.Tag = i;
                StyleTradeChangeCell(row.Cells[2], marketAChange);
                StyleTradeChangeCell(row.Cells[5], partnerChange);
            }
        }

        private void ShowTradeCalculationMessage(string message)
        {
            ConfigureTradeCalculationLogGrid();
            dgvTradeResult.Rows.Clear();
            int rowIndex = dgvTradeResult.Rows.Add(message, "", "", "", "", "", "");
            dgvTradeResult.Rows[rowIndex].DefaultCellStyle.ForeColor = UiTheme.TextColor;
        }

        private string LocalizedMerchandiseName(int index)
        {
            string englishName = MerchandiseNames[index];
            return isEnglish || !ItemNames.ContainsKey(englishName) ? englishName : ItemNames[englishName];
        }

        private string FormatDemandValue(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private string FormatDemandChange(double value)
        {
            if (Math.Abs(value) < 0.0001) return "0.0";
            return (value > 0 ? "+" : "") + value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void StyleTradeChangeCell(DataGridViewCell cell, double change)
        {
            if (cell == null) return;

            if (change > 0.0001)
            {
                cell.Style.ForeColor = Color.FromArgb(0x1F, 0x6B, 0x3A);
            }
            else if (change < -0.0001)
            {
                cell.Style.ForeColor = Color.FromArgb(0x8C, 0x2D, 0x2D);
            }
            else
            {
                cell.Style.ForeColor = UiTheme.TextColor;
            }
        }

        // Универсальный метод импорта Excel для обоих рынков
        private void ImportExcelData(double[] targetArray, DataGridView dgv)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Workbook|*.xlsx";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    TradeDemandWorkbookService workbookService = new TradeDemandWorkbookService(MerchandiseNames);
                    double[] imported = workbookService.LoadDemands(ofd.FileName, targetArray.Length);
                    Array.Copy(imported, targetArray, Math.Min(imported.Length, targetArray.Length));

                    UpdateTradeGrids();
                    MessageBox.Show(isEnglish ? "Excel imported." : "Excel импортирован.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(isEnglish ? "Error importing Excel: " + ex.Message : "Ошибка импорта Excel: " + ex.Message);
                }
            }
        }

        private void ExportDemandArrayToXlsx(double[] demands, string defaultName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel Workbook|*.xlsx";
            dialog.FileName = FileNameHelper.MakeSafeFileName(defaultName) + ".xlsx";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                string[] headers = isEnglish ? MerchandiseNames : MerchandiseNames.Select(n => ItemNames[n]).ToArray();
                TradeDemandWorkbookService workbookService = new TradeDemandWorkbookService(MerchandiseNames);
                workbookService.SaveDemands(dialog.FileName, demands, headers);

                MessageBox.Show(isEnglish ? "Excel exported." : "Excel экспортирован.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(isEnglish ? "Error exporting Excel: " + ex.Message : "Ошибка экспорта Excel: " + ex.Message);
            }
        }

        // Обработчик кнопки для Рынка А
        private void btnImportMarketA_Click(object sender, EventArgs e)
        {
            ImportExcelData(A, dgvMarketA);
        }

        // Обработчик кнопки для Партнера
        private void btnImportPartner_Click(object sender, EventArgs e)
        {
            ImportExcelData(PartnerDemands, dgvPartner);
        }
        private int ParseClass(string text)
        {
            return AcksRules.ParseMarketClass(text);
        }

        private void btnExportRoute_Click(object sender, EventArgs e)
        {
            ExportDemandArrayToXlsx(A, isEnglish ? "trade-route-demands" : "спрос-торгового-пути");
        }

        // --- ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ, КОТОРЫЕ ТРЕБУЮТСЯ ДИЗАЙНЕРОМ ---

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSettlementName.Text))
            {
                btnGenerate.Enabled = true;
            }
            else
            {
                btnGenerate.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnMusicToggle_Click(object sender, EventArgs e)
        {
            // Проверяем состояние плеера
            if (WMP.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                WMP.controls.pause();
                btnMusicToggle.Text = isEnglish ? "▶ Play" : "▶ Играть";
            }
            else
            {
                // Если остановлен или на паузе - запускаем
                if (WMP.playState == WMPLib.WMPPlayState.wmppsStopped)
                {
                    WMP.URL = System.IO.Path.Combine(Application.StartupPath, "fon.mp3");
                }
                WMP.settings.setMode("loop", true);
                WMP.controls.play();
                btnMusicToggle.Text = isEnglish ? "⏸ Pause" : "⏸ Пауза";
            }
        }

        private void trkVolume_Scroll(object sender, EventArgs e)
        {
            // Устанавливаем громкость напрямую из значения TrackBar (0-100)
            WMP.settings.volume = trkVolume.Value;
        }
        private void btnApplyLandMod_Click(object sender, EventArgs e)
        {
            if (cmbLandRevenue.SelectedItem == null) return;

            string revenue = cmbLandRevenue.Text; // "3gp", etc.
            // Извлекаем числовую часть, удаляя суффикс "gp"
            int gp = int.Parse(revenue.Replace("gp", ""));

            btnApplyLandMod.Enabled = false;
            EnsureGeneratorDemandBreakdownStart();
            double[] adjustment = CreateLandValueDemandAdjustment(gp, demandRandom);
            AddDemandAdjustment(A, adjustment);
            AddGeneratorDemandBreakdown(
                "Land value: " + gp + "gp random",
                "Ценность земли: " + gp + "gp случайно",
                adjustment);

            UpdateDataGridView(dataGridView1);
        }

        private void AddLandValueDemandAdjustment(double[] target, int gp, Random random)
        {
            if (target == null || random == null) return;

            AddDemandAdjustment(target, CreateLandValueDemandAdjustment(gp, random));
        }

        private double[] CreateLandValueDemandAdjustment(int gp, Random random)
        {
            double[] landVals = new double[AcksRules.DemandCount];
            if (random == null) return landVals;

            // Land value в ACKS меняет случайные товарные категории: бедные земли повышают спрос,
            // богатые земли чаще создают избыток и понижают спрос.
            int posCount = 0;
            int negCount = 0;

            switch (gp)
            {
                case 3: posCount = 6; negCount = 1; break;
                case 4: posCount = 4; negCount = 1; break;
                case 5: posCount = 2; negCount = 1; break;
                case 6: posCount = 1; negCount = 1; break;
                case 7: posCount = 1; negCount = 2; break;
                case 8: posCount = 1; negCount = 4; break;
                case 9: posCount = 1; negCount = 6; break;
            }

            List<int> usedIndices = new List<int>();
            for (int i = 0; i < posCount; i++)
            {
                int idx;
                do { idx = random.Next(AcksRules.DemandCount); } while (usedIndices.Contains(idx));
                usedIndices.Add(idx);
                landVals[idx] += 1;
            }

            for (int i = 0; i < negCount; i++)
            {
                int idx;
                do { idx = random.Next(AcksRules.DemandCount); } while (usedIndices.Contains(idx));
                usedIndices.Add(idx);
                landVals[idx] -= 1;
            }

            return landVals;
        }

        private void lblProgramVersion_Click(object sender, EventArgs e)
        {

        }
    }
}
