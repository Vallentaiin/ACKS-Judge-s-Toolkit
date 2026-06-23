using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private readonly Random characterNameRandom = new Random();
        private readonly string characterLibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OSRCGG",
            "characters.xml");

        private readonly CharacterGenerator characterGenerator = new CharacterGenerator();

        private Dictionary<string, NumericUpDown> characterAttributes;
        private List<CharacterRecord> characterLibrary = new List<CharacterRecord>();
        private bool characterUiLoading;
        private bool characterEventsWired;
        private CheckBox chkCharacterGenerateName;
        private ComboBox cmbCharacterNameCulture;
        private Button btnCharacterGenerateName;
        private NameGenerationService characterNameService;
        private Label lblCharacterLibraryFilters;
        private TextBox txtCharacterLibrarySearch;
        private ComboBox cmbCharacterClassFilter;
        private ComboBox cmbCharacterLevelFilter;
        private ComboBox cmbCharacterKindFilter;
        private ComboBox cmbCharacterSexFilter;
        private ComboBox cmbCharacterAlignmentFilter;
        private void InitializeCharacterTab()
        {
            ConfigureCharacterDesignerControls();
            LoadCharacterLibrary();
            RefreshCharacterList();
            NewCharacter(false);
        }

        private void ConfigureCharacterDesignerControls()
        {
            characterAttributes = new Dictionary<string, NumericUpDown>()
            {
                {"STR", nudCharacterStr},
                {"INT", nudCharacterInt},
                {"WIL", nudCharacterWil},
                {"DEX", nudCharacterDex},
                {"CON", nudCharacterCon},
                {"CHA", nudCharacterCha}
            };

            PopulateCharacterCombo(cmbCharacterKind, new[] { "Player", "NPC" });
            PopulateCharacterCombo(cmbCharacterClass, CharacterGenerationCatalog.Classes.Select(c => c.Name).ToArray());
            PopulateCharacterCombo(cmbCharacterOccupation, CharacterGenerationCatalog.NpcOccupationProficiencies.Keys.Concat(new[] { "Classed NPC" }).OrderBy(k => k).ToArray());
            PopulateCharacterCombo(cmbCharacterAlignment, CharacterGenerationCatalog.Alignments);
            PopulateCharacterCombo(cmbCharacterSex, CharacterGenerationCatalog.Sexes);
            EnsureCharacterNameControls();
            EnsureCharacterLibraryFilterControls();

            string[] proficiencies = CharacterGenerationCatalog.GeneralProficiencies.Concat(CharacterGenerationCatalog.ClassProficiencies).Distinct().OrderBy(p => p).ToArray();
            clbCharacterProficiencies.Items.Clear();
            clbCharacterProficiencies.Items.AddRange(proficiencies);
            lstCharacters.ScrollAlwaysVisible = true;

            if (characterEventsWired) return;

            btnCharacterNew.Click += (s, e) => NewCharacter(true);
            btnCharacterSave.Click += (s, e) => SaveCurrentCharacter();
            btnCharacterDelete.Click += (s, e) => DeleteSelectedCharacter();
            btnCharacterImport.Click += (s, e) => ImportCharacter();
            btnCharacterExport.Click += (s, e) => ExportSelectedCharacter();
            btnCharacterRandomPlayer.Click += (s, e) => GenerateRandomPlayer();
            btnCharacterRandomNpc.Click += (s, e) => GenerateRandomNpc();
            btnCharacterRollAttributes.Click += (s, e) => RollCharacterAttributes();
            btnCharacterRandomProficiencies.Click += (s, e) => RandomizeCharacterProficiencies();
            lstCharacters.SelectedIndexChanged += (s, e) =>
            {
                if (!characterUiLoading && lstCharacters.SelectedItem is CharacterRecord record)
                {
                    LoadCharacterToUi(record);
                }
            };
            characterEventsWired = true;
        }

        private void EnsureCharacterLibraryFilterControls()
        {
            if (lblCharacterLibraryFilters != null || tabPageCharacters == null) return;

            lblCharacterLibraryFilters = new Label
            {
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = UiTheme.CreateFont(FontStyle.Bold),
                Location = new Point(8, 512),
                Size = new Size(190, 18)
            };
            txtCharacterLibrarySearch = new TextBox
            {
                Location = new Point(8, 532),
                Size = new Size(190, 23)
            };
            cmbCharacterClassFilter = CreateCharacterFilterCombo(8, 560, 190);
            cmbCharacterLevelFilter = CreateCharacterFilterCombo(8, 588, 90);
            cmbCharacterKindFilter = CreateCharacterFilterCombo(108, 588, 90);
            cmbCharacterSexFilter = CreateCharacterFilterCombo(8, 616, 90);
            cmbCharacterAlignmentFilter = CreateCharacterFilterCombo(108, 616, 90);

            txtCharacterLibrarySearch.TextChanged += (s, e) => RefreshCharacterList();
            cmbCharacterClassFilter.SelectedIndexChanged += (s, e) => RefreshCharacterList();
            cmbCharacterLevelFilter.SelectedIndexChanged += (s, e) => RefreshCharacterList();
            cmbCharacterKindFilter.SelectedIndexChanged += (s, e) => RefreshCharacterList();
            cmbCharacterSexFilter.SelectedIndexChanged += (s, e) => RefreshCharacterList();
            cmbCharacterAlignmentFilter.SelectedIndexChanged += (s, e) => RefreshCharacterList();

            tabPageCharacters.Controls.Add(lblCharacterLibraryFilters);
            tabPageCharacters.Controls.Add(txtCharacterLibrarySearch);
            tabPageCharacters.Controls.Add(cmbCharacterClassFilter);
            tabPageCharacters.Controls.Add(cmbCharacterLevelFilter);
            tabPageCharacters.Controls.Add(cmbCharacterKindFilter);
            tabPageCharacters.Controls.Add(cmbCharacterSexFilter);
            tabPageCharacters.Controls.Add(cmbCharacterAlignmentFilter);
            FillCharacterFilterCombos();
        }

        private ComboBox CreateCharacterFilterCombo(int x, int y, int width)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x, y),
                Size = new Size(width, 23),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White
            };
        }

        private void FillCharacterFilterCombos()
        {
            if (lblCharacterLibraryFilters != null)
            {
                lblCharacterLibraryFilters.Text = isEnglish ? "Search and filters" : "Поиск и фильтры";
            }

            FillFilterCombo(cmbCharacterClassFilter,
                new[] { new FilterItem("", isEnglish ? "Any class" : "Любой класс") }
                    .Concat(CharacterGenerationCatalog.Classes
                        .Select(c => c.Name)
                        .Distinct()
                        .OrderBy(n => n)
                        .Select(n => new FilterItem(n, n))));
            FillFilterCombo(cmbCharacterLevelFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any lvl" : "Люб. ур."),
                    new FilterItem("1", "L1"),
                    new FilterItem("2", "L2"),
                    new FilterItem("3", "L3"),
                    new FilterItem("4", "L4"),
                    new FilterItem("5", "L5"),
                    new FilterItem("6", "L6"),
                    new FilterItem("7", "L7"),
                    new FilterItem("8", "L8"),
                    new FilterItem("9", "L9"),
                    new FilterItem("10+", "L10+")
                });
            FillFilterCombo(cmbCharacterKindFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any type" : "Любой тип"),
                    new FilterItem("Player", "PC"),
                    new FilterItem("NPC", "NPC")
                });
            FillFilterCombo(cmbCharacterSexFilter,
                new[] { new FilterItem("", isEnglish ? "Any sex" : "Любой пол") }
                    .Concat(CharacterGenerationCatalog.Sexes.Select(s => new FilterItem(s, s))));
            FillFilterCombo(cmbCharacterAlignmentFilter,
                new[] { new FilterItem("", isEnglish ? "Any align." : "Люб. мир.") }
                    .Concat(CharacterGenerationCatalog.Alignments.Select(a => new FilterItem(a, a))));
        }

        private void PopulateCharacterCombo(ComboBox combo, string[] items)
        {
            object selected = combo.SelectedItem;
            combo.Items.Clear();
            combo.Items.AddRange(items);
            if (selected != null && combo.Items.Contains(selected))
            {
                combo.SelectedItem = selected;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        private void EnsureCharacterNameControls()
        {
            if (chkCharacterGenerateName != null) return;

            characterNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);

            chkCharacterGenerateName = new CheckBox
            {
                AutoSize = true,
                ForeColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(225, 207)
            };

            cmbCharacterNameCulture = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(300, 205),
                Size = new System.Drawing.Size(130, 23)
            };
            cmbCharacterNameCulture.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            cmbCharacterNameCulture.ForeColor = System.Drawing.Color.White;
            PopulateCharacterNameCultures("english");
            SelectCharacterNameCulture("english");

            btnCharacterGenerateName = new Button
            {
                Location = new System.Drawing.Point(344, 224),
                Size = new System.Drawing.Size(86, 22)
            };
            UiTheme.StyleCommandButton(btnCharacterGenerateName, UiTheme.PositiveButtonColor);
            btnCharacterGenerateName.Click += (s, e) => GenerateCharacterNameIntoField();

            tabCharacterMain.Controls.Add(chkCharacterGenerateName);
            tabCharacterMain.Controls.Add(cmbCharacterNameCulture);
            tabCharacterMain.Controls.Add(btnCharacterGenerateName);
            UpdateCharacterNameControlsLanguage();
        }

        private void UpdateCharacterNameControlsLanguage()
        {
            if (chkCharacterGenerateName == null) return;
            string selectedKey = SelectedCharacterNameCultureKey();
            PopulateCharacterNameCultures(selectedKey);
            chkCharacterGenerateName.Text = isEnglish ? "Auto" : "Ген. имя";
            btnCharacterGenerateName.Text = isEnglish ? "Roll" : "Имя";
        }

        private void PopulateCharacterNameCultures(string selectedKey)
        {
            if (cmbCharacterNameCulture == null) return;
            if (characterNameService == null) characterNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);

            cmbCharacterNameCulture.Items.Clear();
            foreach (NameCultureInfo culture in characterNameService.GetCultures(isEnglish))
            {
                cmbCharacterNameCulture.Items.Add(culture);
            }
            if (cmbCharacterNameCulture.Items.Count == 0)
            {
                cmbCharacterNameCulture.Items.Add(new NameCultureInfo { Key = "english", Label = isEnglish ? "English" : "Английская" });
            }
            SelectCharacterNameCulture(string.IsNullOrWhiteSpace(selectedKey) ? "english" : selectedKey);
        }

        private void SelectCharacterNameCulture(string key)
        {
            if (cmbCharacterNameCulture == null) return;
            for (int i = 0; i < cmbCharacterNameCulture.Items.Count; i++)
            {
                NameCultureInfo culture = cmbCharacterNameCulture.Items[i] as NameCultureInfo;
                if (culture != null && string.Equals(culture.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    cmbCharacterNameCulture.SelectedIndex = i;
                    return;
                }
            }
            if (cmbCharacterNameCulture.Items.Count > 0) cmbCharacterNameCulture.SelectedIndex = 0;
        }

        private string SelectedCharacterNameCultureKey()
        {
            NameCultureInfo culture = cmbCharacterNameCulture == null ? null : cmbCharacterNameCulture.SelectedItem as NameCultureInfo;
            return culture == null ? "english" : culture.Key;
        }

        private void GenerateCharacterNameIntoField()
        {
            if (txtCharacterName == null) return;
            if (characterNameService == null) characterNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            bool female = string.Equals(cmbCharacterSex.Text, "Female", StringComparison.OrdinalIgnoreCase)
                || cmbCharacterSex.Text.IndexOf("жен", StringComparison.OrdinalIgnoreCase) >= 0;
            txtCharacterName.Text = characterNameService.GeneratePersonalName(characterNameRandom, SelectedCharacterNameCultureKey(), female, !isEnglish);
        }

        private void ApplyGeneratedCharacterNameIfEnabled()
        {
            if (chkCharacterGenerateName != null && chkCharacterGenerateName.Checked)
            {
                GenerateCharacterNameIntoField();
            }
        }

        private void UpdateCharacterLanguage()
        {
            if (tabPageCharacters == null) return;

            tabPageCharacters.Text = isEnglish ? "Characters" : "Персонажи";
            tabCharacterMain.Text = isEnglish ? "Basics" : "Основа";
            tabCharacterProficiencies.Text = isEnglish ? "Proficiencies" : "Навыки";
            tabCharacterSheet.Text = isEnglish ? "Sheet" : "Лист";

            UpdateCharacterNameControlsLanguage();

            lblCharacterSummary.Text = isEnglish
                ? "Character library\r\nSave, import, export"
                : "Библиотека персонажей\r\nСохранение, импорт, экспорт";
            lblCharacterName.Text = isEnglish ? "Name" : "Имя";
            lblCharacterKind.Text = isEnglish ? "Kind" : "Тип";
            lblCharacterClass.Text = isEnglish ? "Class" : "Класс";
            lblCharacterOccupation.Text = isEnglish ? "Job" : "Ремесло";
            lblCharacterTemplate.Text = isEnglish ? "Template" : "Шаблон";
            lblCharacterAlignment.Text = isEnglish ? "Align" : "Мировоззр.";
            lblCharacterHome.Text = isEnglish ? "Home" : "Родина";
            lblCharacterSex.Text = isEnglish ? "Sex" : "Пол";
            lblCharacterLevel.Text = isEnglish ? "Level" : "Уровень";
            lblCharacterXp.Text = "XP";
            lblCharacterHp.Text = "HP";
            lblCharacterAc.Text = "AC";
            lblCharacterAge.Text = isEnglish ? "Age" : "Возраст";
            lblCharacterSelectedProfs.Text = isEnglish ? "Selected / custom proficiencies" : "Выбранные / дополнительные навыки";
            lblCharacterLanguages.Text = isEnglish ? "Languages" : "Языки";
            lblCharacterSpells.Text = isEnglish ? "Spells" : "Заклинания";
            lblCharacterEquipment.Text = isEnglish ? "Equipment" : "Снаряжение";
            lblCharacterAppearance.Text = isEnglish ? "Looks" : "Внешность";
            lblCharacterBackground.Text = isEnglish ? "Story" : "История";
            lblCharacterHint.Text = isEnglish
                ? "PC: ACKS character creation steps. NPC: level 0 creates occupation NPC; set Level above 0 before Random NPC to create a leveled NPC."
                : "PC: шаги создания персонажа ACKS. NPC: уровень 0 создаёт НПС с профессией; поставь уровень выше 0 перед «Случ. NPC», чтобы создать уровневого НПС.";

            btnCharacterNew.Text = isEnglish ? "New" : "Новый";
            btnCharacterSave.Text = isEnglish ? "Save" : "Сохранить";
            btnCharacterDelete.Text = isEnglish ? "Delete" : "Удалить";
            btnCharacterImport.Text = isEnglish ? "Import" : "Импорт";
            btnCharacterExport.Text = isEnglish ? "Export" : "Экспорт";
            btnCharacterRandomPlayer.Text = isEnglish ? "Random PC" : "Случ. PC";
            btnCharacterRandomNpc.Text = isEnglish ? "Random NPC" : "Случ. NPC";
            btnCharacterRollAttributes.Text = isEnglish ? "Attributes" : "Атрибуты";
            btnCharacterRandomProficiencies.Text = isEnglish ? "Profic." : "Навыки";
            FillCharacterFilterCombos();
        }

        private void LoadCharacterLibrary()
        {
            characterLibrary = new XmlRecordStore<CharacterRecord>(characterLibraryPath).Load();
        }

        private void SaveCharacterLibrary()
        {
            new XmlRecordStore<CharacterRecord>(characterLibraryPath).Save(characterLibrary);
        }

        private void RefreshCharacterList()
        {
            if (lstCharacters == null) return;
            string selectedId = (lstCharacters.SelectedItem as CharacterRecord)?.Id;
            characterUiLoading = true;
            lstCharacters.DataSource = null;
            lstCharacters.DataSource = characterLibrary
                .Where(CharacterMatchesLibraryFilters)
                .OrderBy(c => c.Name)
                .ThenBy(c => c.ClassName)
                .ToList();
            SelectCharacterInCurrentList(selectedId);
            characterUiLoading = false;
        }

        private bool CharacterMatchesLibraryFilters(CharacterRecord character)
        {
            if (character == null) return false;

            string search = txtCharacterLibrarySearch == null ? "" : txtCharacterLibrarySearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search)
                && !ContainsIgnoreCase(character.Name, search)
                && !ContainsIgnoreCase(character.ClassName, search)
                && !ContainsIgnoreCase(character.Occupation, search)
                && !ContainsIgnoreCase(character.Template, search)
                && !ContainsIgnoreCase(character.Homeland, search))
            {
                return false;
            }

            string className = SelectedFilterValue(cmbCharacterClassFilter);
            if (!string.IsNullOrWhiteSpace(className)
                && !string.Equals(character.ClassName, className, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string level = SelectedFilterValue(cmbCharacterLevelFilter);
            if (!string.IsNullOrWhiteSpace(level))
            {
                if (level == "10+")
                {
                    if (character.Level < 10) return false;
                }
                else if (character.Level.ToString() != level)
                {
                    return false;
                }
            }

            string kind = SelectedFilterValue(cmbCharacterKindFilter);
            if (!string.IsNullOrWhiteSpace(kind)
                && !string.Equals(character.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string sex = SelectedFilterValue(cmbCharacterSexFilter);
            if (!string.IsNullOrWhiteSpace(sex)
                && !string.Equals(character.Sex, sex, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string alignment = SelectedFilterValue(cmbCharacterAlignmentFilter);
            return string.IsNullOrWhiteSpace(alignment)
                || string.Equals(character.Alignment, alignment, StringComparison.OrdinalIgnoreCase);
        }

        private void SelectCharacterInCurrentList(string id)
        {
            if (lstCharacters == null || string.IsNullOrWhiteSpace(id)) return;

            for (int i = 0; i < lstCharacters.Items.Count; i++)
            {
                CharacterRecord item = lstCharacters.Items[i] as CharacterRecord;
                if (item != null && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    lstCharacters.SelectedIndex = i;
                    return;
                }
            }
        }

        private void NewCharacter(bool clearSelection)
        {
            CharacterRecord record = new CharacterRecord();
            record.Level = 1;
            record.HitPoints = 4;
            record.Age = 18;
            record.STR = 10;
            record.INT = 10;
            record.WIL = 10;
            record.DEX = 10;
            record.CON = 10;
            record.CHA = 10;
            record.Proficiencies = "Adventuring";
            record.Languages = "Native language";
            LoadCharacterToUi(record);
            if (clearSelection) lstCharacters.ClearSelected();
        }

        private void LoadCharacterToUi(CharacterRecord record)
        {
            characterUiLoading = true;
            txtCharacterName.Text = record.Name;
            SetCombo(cmbCharacterKind, record.Kind);
            SetCombo(cmbCharacterClass, record.ClassName);
            SetCombo(cmbCharacterOccupation, record.Occupation);
            SetCombo(cmbCharacterAlignment, record.Alignment);
            SetCombo(cmbCharacterSex, NormalizeCharacterSex(record.Sex));
            txtCharacterTemplate.Text = record.Template;
            txtCharacterHomeland.Text = record.Homeland;
            nudCharacterLevel.Value = Clamp(record.Level, nudCharacterLevel);
            nudCharacterXp.Value = Clamp(record.Experience, nudCharacterXp);
            nudCharacterHp.Value = Clamp(record.HitPoints, nudCharacterHp);
            nudCharacterAc.Value = Clamp(record.ArmorClass, nudCharacterAc);
            nudCharacterAge.Value = Clamp(record.Age, nudCharacterAge);
            characterAttributes["STR"].Value = Clamp(record.STR, characterAttributes["STR"]);
            characterAttributes["INT"].Value = Clamp(record.INT, characterAttributes["INT"]);
            characterAttributes["WIL"].Value = Clamp(record.WIL, characterAttributes["WIL"]);
            characterAttributes["DEX"].Value = Clamp(record.DEX, characterAttributes["DEX"]);
            characterAttributes["CON"].Value = Clamp(record.CON, characterAttributes["CON"]);
            characterAttributes["CHA"].Value = Clamp(record.CHA, characterAttributes["CHA"]);
            txtCharacterLanguages.Text = record.Languages;
            txtCharacterSpells.Text = record.Spells;
            txtCharacterEquipment.Text = record.Equipment;
            txtCharacterAppearance.Text = record.Appearance;
            txtCharacterBackground.Text = record.Background;
            txtCharacterNotes.Text = record.Notes;
            SetCheckedProficiencies(record.Proficiencies);
            characterUiLoading = false;
        }

        private CharacterRecord ReadCharacterFromUi()
        {
            CharacterRecord selected = lstCharacters.SelectedItem as CharacterRecord;
            CharacterRecord record = selected == null ? new CharacterRecord() : selected;
            record.Kind = cmbCharacterKind.Text;
            record.Name = txtCharacterName.Text.Trim();
            record.ClassName = cmbCharacterClass.Text;
            record.Occupation = cmbCharacterOccupation.Text;
            record.Template = txtCharacterTemplate.Text.Trim();
            record.Alignment = cmbCharacterAlignment.Text;
            record.Homeland = txtCharacterHomeland.Text.Trim();
            record.Sex = NormalizeCharacterSex(cmbCharacterSex.Text);
            record.Level = (int)nudCharacterLevel.Value;
            record.Experience = (int)nudCharacterXp.Value;
            record.HitPoints = (int)nudCharacterHp.Value;
            record.ArmorClass = (int)nudCharacterAc.Value;
            record.Age = (int)nudCharacterAge.Value;
            record.STR = (int)characterAttributes["STR"].Value;
            record.INT = (int)characterAttributes["INT"].Value;
            record.WIL = (int)characterAttributes["WIL"].Value;
            record.DEX = (int)characterAttributes["DEX"].Value;
            record.CON = (int)characterAttributes["CON"].Value;
            record.CHA = (int)characterAttributes["CHA"].Value;
            record.Proficiencies = GetCheckedProficiencies();
            record.Languages = txtCharacterLanguages.Text.Trim();
            record.Spells = txtCharacterSpells.Text.Trim();
            record.Equipment = txtCharacterEquipment.Text.Trim();
            record.Appearance = txtCharacterAppearance.Text.Trim();
            record.Background = txtCharacterBackground.Text.Trim();
            record.Notes = txtCharacterNotes.Text.Trim();
            record.UpdatedAt = DateTime.Now;
            return record;
        }

        private void SaveCurrentCharacter()
        {
            CharacterRecord record = ReadCharacterFromUi();
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                record.Name = isEnglish ? "Unnamed character" : "Безымянный персонаж";
                txtCharacterName.Text = record.Name;
            }

            CharacterRecord existing = characterLibrary.FirstOrDefault(c => c.Id == record.Id);
            if (existing == null)
            {
                characterLibrary.Add(record);
            }

            SaveCharacterLibrary();
            RefreshCharacterList();
            SelectCharacter(record.Id);
        }

        private void DeleteSelectedCharacter()
        {
            CharacterRecord selected = lstCharacters.SelectedItem as CharacterRecord;
            if (selected == null) return;

            DialogResult result = MessageBox.Show(
                isEnglish ? "Delete selected character?" : "Удалить выбранного персонажа?",
                "ACKS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            characterLibrary.RemoveAll(c => c.Id == selected.Id);
            SaveCharacterLibrary();
            RefreshCharacterList();
            NewCharacter(false);
        }

        private void ImportCharacter()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "ACKS Character XML|*.xml|All files|*.*";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                CharacterRecord record = XmlSerialization.DeserializeFile<CharacterRecord>(dialog.FileName);
                if (record == null) return;

                if (string.IsNullOrWhiteSpace(record.Id) || characterLibrary.Any(c => c.Id == record.Id))
                {
                    record.Id = Guid.NewGuid().ToString("N");
                }
                characterLibrary.Add(record);
                SaveCharacterLibrary();
                RefreshCharacterList();
                SelectCharacter(record.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEnglish ? "Import failed: " : "Не удалось импортировать: ") + ex.Message);
            }
        }

        private void ExportSelectedCharacter()
        {
            CharacterRecord record = lstCharacters.SelectedItem as CharacterRecord;
            if (record == null)
            {
                record = ReadCharacterFromUi();
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "ACKS Character XML|*.xml";
            dialog.FileName = FileNameHelper.MakeSafeFileName(string.IsNullOrWhiteSpace(record.Name) ? "acks-character" : record.Name) + ".xml";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            XmlSerialization.SerializeFile(dialog.FileName, record);
        }

        private void SetCheckedProficiencies(string proficiencies)
        {
            for (int i = 0; i < clbCharacterProficiencies.Items.Count; i++)
            {
                clbCharacterProficiencies.SetItemChecked(i, false);
            }

            if (string.IsNullOrWhiteSpace(proficiencies)) return;
            string[] tokens = proficiencies.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();

            for (int i = 0; i < clbCharacterProficiencies.Items.Count; i++)
            {
                string item = clbCharacterProficiencies.Items[i].ToString();
                if (tokens.Any(t => item.Equals(t, StringComparison.OrdinalIgnoreCase) ||
                                    t.StartsWith(item + " ", StringComparison.OrdinalIgnoreCase) ||
                                    t.StartsWith(item + "(", StringComparison.OrdinalIgnoreCase)))
                {
                    clbCharacterProficiencies.SetItemChecked(i, true);
                }
            }
        }

        private string GetCheckedProficiencies()
        {
            return string.Join(", ", clbCharacterProficiencies.CheckedItems.Cast<object>().Select(i => i.ToString()));
        }

        private int CountProficiencies(string proficiencies)
        {
            if (string.IsNullOrWhiteSpace(proficiencies)) return 0;
            return CharacterRulesService.SplitProficiencies(proficiencies).Length;
        }

        private string NormalizeCharacterSex(string sex)
        {
            if (string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(sex) && sex.IndexOf("жен", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "Female";
            }

            return "Male";
        }

        private void SetCombo(ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                return;
            }

            int index = combo.FindStringExact(value);
            if (index < 0)
            {
                combo.Items.Add(value);
                index = combo.FindStringExact(value);
            }
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        private decimal Clamp(int value, NumericUpDown number)
        {
            return Math.Min(Math.Max(value, (int)number.Minimum), (int)number.Maximum);
        }

        private void SelectCharacter(string id)
        {
            for (int i = 0; i < lstCharacters.Items.Count; i++)
            {
                CharacterRecord item = lstCharacters.Items[i] as CharacterRecord;
                if (item != null && item.Id == id)
                {
                    lstCharacters.SelectedIndex = i;
                    return;
                }
            }
        }

    }
}
