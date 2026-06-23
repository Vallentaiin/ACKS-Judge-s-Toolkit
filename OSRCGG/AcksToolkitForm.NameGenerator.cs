using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private NameGenerationService standaloneNameService;
        private readonly Random standaloneNameRandom = new Random();
        private ComboBox cmbNameGeneratorCulture;
        private ComboBox cmbNameGeneratorKind;
        private ComboBox cmbNameGeneratorSex;
        private ComboBox cmbNameGeneratorRealmTier;
        private NumericUpDown nudNameGeneratorCount;
        private TextBox txtNameGeneratorBase;
        private TextBox txtNameGeneratorOutput;
        private Button btnNameGeneratorGenerate;
        private Button btnNameGeneratorCopy;
        private Label lblNameGeneratorCulture;
        private Label lblNameGeneratorKind;
        private Label lblNameGeneratorSex;
        private Label lblNameGeneratorRealmTier;
        private Label lblNameGeneratorCount;
        private Label lblNameGeneratorBase;
        private Label lblNameGeneratorHint;

        private sealed class NameGeneratorOption
        {
            public string Key { get; private set; }
            public string English { get; private set; }
            public string Russian { get; private set; }

            public NameGeneratorOption(string key, string english, string russian)
            {
                Key = key;
                English = english;
                Russian = russian;
            }

            public string Label(bool english)
            {
                return english ? English : Russian;
            }

            public override string ToString()
            {
                return English;
            }
        }

        private void InitializeNameGeneratorTab()
        {
            if (tabPageNameGenerator == null)
            {
                tabPageNameGenerator = new TabPage();
                tabControl1.TabPages.Insert(Math.Max(0, tabControl1.TabPages.Count - 1), tabPageNameGenerator);
            }

            standaloneNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            tabPageNameGenerator.Controls.Clear();
            tabPageNameGenerator.BackColor = Color.Transparent;

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Tag = UiTheme.TransparentSurfaceTag,
                Padding = new Padding(14),
                ColumnCount = 2,
                RowCount = 2
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tabPageNameGenerator.Controls.Add(root);

            Panel optionsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Accent2Color,
                Padding = new Padding(16)
            };
            root.Controls.Add(optionsPanel, 0, 0);

            TableLayoutPanel leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            optionsPanel.Controls.Add(leftLayout);

            TableLayoutPanel options = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 7
            };
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            leftLayout.Controls.Add(options, 0, 0);

            lblNameGeneratorCulture = CreateNameGeneratorLabel();
            cmbNameGeneratorCulture = CreateNameGeneratorCombo();
            options.Controls.Add(lblNameGeneratorCulture, 0, 0);
            options.Controls.Add(cmbNameGeneratorCulture, 1, 0);

            lblNameGeneratorKind = CreateNameGeneratorLabel();
            cmbNameGeneratorKind = CreateNameGeneratorCombo();
            cmbNameGeneratorKind.SelectedIndexChanged += (s, e) => UpdateNameGeneratorOptionVisibility();
            options.Controls.Add(lblNameGeneratorKind, 0, 1);
            options.Controls.Add(cmbNameGeneratorKind, 1, 1);

            lblNameGeneratorSex = CreateNameGeneratorLabel();
            cmbNameGeneratorSex = CreateNameGeneratorCombo();
            options.Controls.Add(lblNameGeneratorSex, 0, 2);
            options.Controls.Add(cmbNameGeneratorSex, 1, 2);

            lblNameGeneratorRealmTier = CreateNameGeneratorLabel();
            cmbNameGeneratorRealmTier = CreateNameGeneratorCombo();
            options.Controls.Add(lblNameGeneratorRealmTier, 0, 3);
            options.Controls.Add(cmbNameGeneratorRealmTier, 1, 3);

            lblNameGeneratorBase = CreateNameGeneratorLabel();
            txtNameGeneratorBase = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.FieldColor,
                ForeColor = UiTheme.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            options.Controls.Add(lblNameGeneratorBase, 0, 4);
            options.Controls.Add(txtNameGeneratorBase, 1, 4);

            lblNameGeneratorCount = CreateNameGeneratorLabel();
            nudNameGeneratorCount = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 90,
                Minimum = 1,
                Maximum = 200,
                Value = 20,
                BackColor = Color.White,
                ForeColor = UiTheme.TextColor,
                Margin = new Padding(0, 7, 0, 1),
                Tag = UiTheme.WhiteFieldTag
            };
            options.Controls.Add(lblNameGeneratorCount, 0, 5);
            options.Controls.Add(nudNameGeneratorCount, 1, 5);

            lblNameGeneratorHint = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.TextColor,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 18, 0, 0)
            };
            leftLayout.Controls.Add(lblNameGeneratorHint, 0, 1);

            txtNameGeneratorOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = UiTheme.FieldColor,
                ForeColor = UiTheme.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(UiTheme.FontFamily, 10f, FontStyle.Regular)
            };
            root.Controls.Add(txtNameGeneratorOutput, 1, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Tag = UiTheme.TransparentSurfaceTag,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };
            root.Controls.Add(buttons, 1, 1);

            btnNameGeneratorGenerate = new Button { Width = 170, Height = 32 };
            UiTheme.StylePositiveButton(btnNameGeneratorGenerate);
            btnNameGeneratorGenerate.Click += (s, e) => GenerateStandaloneNames();
            buttons.Controls.Add(btnNameGeneratorGenerate);

            btnNameGeneratorCopy = new Button { Width = 130, Height = 32 };
            UiTheme.StyleCommandButton(btnNameGeneratorCopy, UiTheme.NeutralButtonColor);
            btnNameGeneratorCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtNameGeneratorOutput.Text))
                {
                    Clipboard.SetText(txtNameGeneratorOutput.Text);
                }
            };
            buttons.Controls.Add(btnNameGeneratorCopy);

            PopulateNameGeneratorCultures("english");
            PopulateNameGeneratorOptions();
            UpdateNameGeneratorLanguage();
        }

        private Label CreateNameGeneratorLabel()
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.TextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 8, 4)
            };
        }

        private ComboBox CreateNameGeneratorCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UiTheme.FieldColor,
                ForeColor = UiTheme.TextColor,
                FormattingEnabled = true,
                Margin = new Padding(0, 7, 0, 1)
            };
        }

        private void PopulateNameGeneratorCultures(string selectedKey)
        {
            if (cmbNameGeneratorCulture == null) return;
            if (standaloneNameService == null) standaloneNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);

            cmbNameGeneratorCulture.Items.Clear();
            foreach (NameCultureInfo culture in standaloneNameService.GetCultures(isEnglish))
            {
                cmbNameGeneratorCulture.Items.Add(culture);
            }

            if (cmbNameGeneratorCulture.Items.Count == 0)
            {
                cmbNameGeneratorCulture.Items.Add(new NameCultureInfo { Key = "english", Label = L("English", "Английская") });
            }

            SelectNameGeneratorCulture(string.IsNullOrWhiteSpace(selectedKey) ? "english" : selectedKey);
        }

        private void SelectNameGeneratorCulture(string key)
        {
            if (cmbNameGeneratorCulture == null) return;
            for (int i = 0; i < cmbNameGeneratorCulture.Items.Count; i++)
            {
                NameCultureInfo culture = cmbNameGeneratorCulture.Items[i] as NameCultureInfo;
                if (culture != null && string.Equals(culture.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    cmbNameGeneratorCulture.SelectedIndex = i;
                    return;
                }
            }

            if (cmbNameGeneratorCulture.Items.Count > 0) cmbNameGeneratorCulture.SelectedIndex = 0;
        }

        private string SelectedNameGeneratorCultureKey()
        {
            NameCultureInfo culture = cmbNameGeneratorCulture == null ? null : cmbNameGeneratorCulture.SelectedItem as NameCultureInfo;
            return culture == null || string.IsNullOrWhiteSpace(culture.Key) ? "english" : culture.Key;
        }

        private void PopulateNameGeneratorOptions()
        {
            PopulateNameGeneratorCombo(cmbNameGeneratorKind, new[]
            {
                new NameGeneratorOption("Personal", "Character name", "Имя персонажа"),
                new NameGeneratorOption("Dynasty", "Surname / dynasty", "Фамилия / династия"),
                new NameGeneratorOption("Settlement", "Settlement", "Поселение"),
                new NameGeneratorOption("Domain", "Domain", "Домен"),
                new NameGeneratorOption("Realm", "Realm / state", "Realm / держава"),
                new NameGeneratorOption("River", "River", "Река"),
                new NameGeneratorOption("Lake", "Lake", "Озеро"),
                new NameGeneratorOption("Sea", "Sea", "Море"),
                new NameGeneratorOption("Ocean", "Ocean", "Океан")
            }, SelectedNameGeneratorOptionKey(cmbNameGeneratorKind));

            PopulateNameGeneratorCombo(cmbNameGeneratorSex, new[]
            {
                new NameGeneratorOption("Random", "Random", "Случайно"),
                new NameGeneratorOption("Male", "Male", "Мужское"),
                new NameGeneratorOption("Female", "Female", "Женское")
            }, SelectedNameGeneratorOptionKey(cmbNameGeneratorSex));

            PopulateNameGeneratorCombo(cmbNameGeneratorRealmTier, new[]
            {
                new NameGeneratorOption("Barony", "Barony", "Баронство"),
                new NameGeneratorOption("County", "County", "Графство"),
                new NameGeneratorOption("Duchy", "Duchy", "Герцогство"),
                new NameGeneratorOption("Principality", "Principality", "Княжество"),
                new NameGeneratorOption("Kingdom", "Kingdom", "Королевство"),
                new NameGeneratorOption("Empire", "Empire", "Империя")
            }, SelectedNameGeneratorOptionKey(cmbNameGeneratorRealmTier));
        }

        private void PopulateNameGeneratorCombo(ComboBox combo, IEnumerable<NameGeneratorOption> options, string selectedKey)
        {
            if (combo == null) return;
            combo.BeginUpdate();
            combo.Items.Clear();
            foreach (NameGeneratorOption option in options)
            {
                combo.Items.Add(new NameGeneratorOption(option.Key, option.English, option.Russian));
            }
            combo.EndUpdate();
            SelectNameGeneratorOption(combo, selectedKey);
        }

        private void SelectNameGeneratorOption(ComboBox combo, string key)
        {
            if (combo == null) return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                NameGeneratorOption option = combo.Items[i] as NameGeneratorOption;
                if (option != null && string.Equals(option.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private string SelectedNameGeneratorOptionKey(ComboBox combo)
        {
            NameGeneratorOption option = combo == null ? null : combo.SelectedItem as NameGeneratorOption;
            return option == null ? "" : option.Key;
        }

        private void UpdateNameGeneratorLanguage()
        {
            if (tabPageNameGenerator == null) return;

            string selectedCulture = SelectedNameGeneratorCultureKey();
            PopulateNameGeneratorCultures(selectedCulture);
            PopulateNameGeneratorOptions();

            tabPageNameGenerator.Text = L("Names", "Названия");
            lblNameGeneratorCulture.Text = L("Culture", "Культура");
            lblNameGeneratorKind.Text = L("Name type", "Тип названия");
            lblNameGeneratorSex.Text = L("Gender", "Пол");
            lblNameGeneratorRealmTier.Text = L("Realm tier", "Уровень державы");
            lblNameGeneratorBase.Text = L("Base name", "Основа");
            lblNameGeneratorCount.Text = L("Count", "Количество");
            lblNameGeneratorHint.Text = L(
                "The base name is optional. It is useful for domain and realm names when you want a title built from an existing settlement or capital.",
                "Основа необязательна. Она полезна для доменов и держав, если нужно построить название от уже существующего поселения или столицы.");
            if (btnNameGeneratorGenerate != null) btnNameGeneratorGenerate.Text = L("Generate", "Сгенерировать");
            if (btnNameGeneratorCopy != null) btnNameGeneratorCopy.Text = L("Copy", "Копировать");

            RefreshNameGeneratorOptionLabels(cmbNameGeneratorKind);
            RefreshNameGeneratorOptionLabels(cmbNameGeneratorSex);
            RefreshNameGeneratorOptionLabels(cmbNameGeneratorRealmTier);
            UpdateNameGeneratorOptionVisibility();
        }

        private void RefreshNameGeneratorOptionLabels(ComboBox combo)
        {
            if (combo == null) return;
            foreach (object item in combo.Items)
            {
                NameGeneratorOption option = item as NameGeneratorOption;
                if (option != null)
                {
                    // ComboBox вызывает ToString; пересоздавать объект не нужно.
                }
            }
            combo.Format -= NameGeneratorComboFormat;
            combo.Format += NameGeneratorComboFormat;
            combo.Refresh();
        }

        private void NameGeneratorComboFormat(object sender, ListControlConvertEventArgs e)
        {
            NameGeneratorOption option = e.ListItem as NameGeneratorOption;
            if (option != null) e.Value = option.Label(isEnglish);
        }

        private void UpdateNameGeneratorOptionVisibility()
        {
            string kind = SelectedNameGeneratorOptionKey(cmbNameGeneratorKind);
            bool personal = string.Equals(kind, "Personal", StringComparison.OrdinalIgnoreCase);
            bool realm = string.Equals(kind, "Realm", StringComparison.OrdinalIgnoreCase);
            bool baseName = string.Equals(kind, "Domain", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "Realm", StringComparison.OrdinalIgnoreCase);

            if (lblNameGeneratorSex != null) lblNameGeneratorSex.Visible = personal;
            if (cmbNameGeneratorSex != null) cmbNameGeneratorSex.Visible = personal;
            if (lblNameGeneratorRealmTier != null) lblNameGeneratorRealmTier.Visible = realm;
            if (cmbNameGeneratorRealmTier != null) cmbNameGeneratorRealmTier.Visible = realm;
            if (lblNameGeneratorBase != null) lblNameGeneratorBase.Visible = baseName;
            if (txtNameGeneratorBase != null) txtNameGeneratorBase.Visible = baseName;
        }

        private void GenerateStandaloneNames()
        {
            if (standaloneNameService == null)
            {
                standaloneNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            }

            string kind = SelectedNameGeneratorOptionKey(cmbNameGeneratorKind);
            string culture = SelectedNameGeneratorCultureKey();
            string baseName = txtNameGeneratorBase == null ? "" : txtNameGeneratorBase.Text.Trim();
            int count = nudNameGeneratorCount == null ? 20 : (int)nudNameGeneratorCount.Value;
            bool russianOutput = !isEnglish;
            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> results = new List<string>();

            // Сначала стараемся набрать уникальные варианты, но не жертвуем выбранным
            // пользователем количеством: при малом словаре допускаем редкий повтор.
            while (results.Count < count)
            {
                string fallback = "";
                bool added = false;
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    string value = GenerateStandaloneName(kind, culture, baseName, russianOutput);
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    if (string.IsNullOrWhiteSpace(fallback)) fallback = value;
                    if (!unique.Add(value)) continue;

                    results.Add(value);
                    added = true;
                    break;
                }

                if (added) continue;

                if (string.IsNullOrWhiteSpace(fallback))
                {
                    fallback = GenerateStandaloneName(kind, culture, baseName, russianOutput);
                }

                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    results.Add(fallback);
                }
                else
                {
                    break;
                }
            }

            txtNameGeneratorOutput.Text = string.Join(Environment.NewLine, results);
        }

        private string GenerateStandaloneName(string kind, string culture, string baseName, bool russianOutput)
        {
            if (string.Equals(kind, "Personal", StringComparison.OrdinalIgnoreCase))
            {
                string sex = SelectedNameGeneratorOptionKey(cmbNameGeneratorSex);
                bool female = string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
                    || (!string.Equals(sex, "Male", StringComparison.OrdinalIgnoreCase) && standaloneNameRandom.Next(2) == 0);
                return standaloneNameService.GeneratePersonalName(standaloneNameRandom, culture, female, russianOutput);
            }

            if (string.Equals(kind, "Dynasty", StringComparison.OrdinalIgnoreCase))
            {
                return standaloneNameService.GenerateDynastyName(standaloneNameRandom, culture, russianOutput);
            }

            if (string.Equals(kind, "Settlement", StringComparison.OrdinalIgnoreCase))
            {
                return standaloneNameService.GenerateSettlementName(standaloneNameRandom, culture, russianOutput);
            }

            if (string.Equals(kind, "Domain", StringComparison.OrdinalIgnoreCase))
            {
                return standaloneNameService.GenerateDomainName(standaloneNameRandom, culture, baseName, russianOutput);
            }

            if (string.Equals(kind, "Realm", StringComparison.OrdinalIgnoreCase))
            {
                return standaloneNameService.GenerateRealmName(standaloneNameRandom, culture, baseName, SelectedNameGeneratorOptionKey(cmbNameGeneratorRealmTier), russianOutput);
            }

            if (string.Equals(kind, "River", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "Lake", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "Sea", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "Ocean", StringComparison.OrdinalIgnoreCase))
            {
                return standaloneNameService.GenerateFeatureName(standaloneNameRandom, culture, kind, russianOutput);
            }

            return standaloneNameService.GenerateSettlementName(standaloneNameRandom, culture, russianOutput);
        }
    }
}
