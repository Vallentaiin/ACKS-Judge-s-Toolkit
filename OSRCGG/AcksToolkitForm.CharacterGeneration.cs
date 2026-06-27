using System.Collections.Generic;

using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        // UI-адаптер генерации персонажей: форма собирает состояние вкладки,
        // вызывает CharacterGenerator и переносит результат обратно в контролы.
        private void GenerateRandomPlayer()
        {
            ClearCharacterLibrarySelectionForNewCharacter();
            ApplyCharacterGenerationResult(characterGenerator.GeneratePlayer(BuildCharacterGenerationRequest()));
        }

        private void GenerateRandomNpc()
        {
            bool keepRandomOccupation = IsCharacterRandomOccupationSelected();
            bool ignoreOccupation = nudCharacterLevel != null && nudCharacterLevel.Value > 0;
            string previousOccupation = cmbCharacterOccupation == null ? "" : cmbCharacterOccupation.Text;
            ClearCharacterLibrarySelectionForNewCharacter();
            ApplyCharacterGenerationResult(characterGenerator.GenerateNpc(BuildCharacterGenerationRequest()));
            if (keepRandomOccupation) SelectRandomCharacterOccupation();
            else if (ignoreOccupation) RestoreCharacterOccupationSelection(previousOccupation);
        }

        private void RegenerateCharacterAppearance()
        {
            if (txtCharacterAppearance == null) return;
            txtCharacterAppearance.Text = characterGenerator.GenerateAppearance(BuildCharacterGenerationRequest());
        }

        private void GenerateNpcBatch()
        {
            int count = nudCharacterBatchCount == null ? 1 : (int)nudCharacterBatchCount.Value;
            StringBuilder text = new StringBuilder();
            text.AppendLine(isEnglish ? "NPC batch" : "Пачка NPC");
            text.AppendLine(new string('-', 72));

            for (int i = 1; i <= count; i++)
            {
                CharacterGenerationResult npc = characterGenerator.GenerateNpc(BuildCharacterGenerationRequest());
                text.AppendLine(FormatBatchNpc(i, npc));
            }

            ShowCharacterBatchOutput(text.ToString());
        }

        private void ClearCharacterLibrarySelectionForNewCharacter()
        {
            if (lstCharacters == null) return;

            // Случайная генерация начинает новую запись, а не редактирование выбранной
            // в библиотеке. Сбрасываем выбор до заполнения UI, чтобы Save создал копию.
            characterUiLoading = true;
            try
            {
                lstCharacters.ClearSelected();
            }
            finally
            {
                characterUiLoading = false;
            }
        }

        private void RandomizeCharacterProficiencies()
        {
            SetCheckedProficiencies(characterGenerator.RandomizeProficiencies(BuildCharacterGenerationRequest()));
        }

        private void RollCharacterAttributes()
        {
            ApplyCharacterAttributes(characterGenerator.RollPlayerAttributes(BuildCharacterGenerationRequest()));
        }

        private CharacterGenerationRequest BuildCharacterGenerationRequest()
        {
            return new CharacterGenerationRequest
            {
                IsEnglish = isEnglish,
                CurrentKind = cmbCharacterKind.Text,
                CurrentClassName = cmbCharacterClass.Text,
                CurrentSex = cmbCharacterSex.Text,
                RequestedOccupation = RequestedCharacterOccupation(),
                ForceZeroLevelOccupation = ShouldGenerateZeroLevelOccupationNpc(),
                RequestedLevel = (int)nudCharacterLevel.Value,
                MaximumLevel = (int)nudCharacterLevel.Maximum,
                Attributes = ReadCharacterAttributes()
            };
        }

        private string FormatBatchNpc(int index, CharacterGenerationResult npc)
        {
            string name = GenerateBatchCharacterName(npc);
            string role = npc.Level <= 0
                ? npc.Occupation
                : npc.ClassName + " L" + npc.Level;
            string attributes = string.Join(" ",
                npc.Attributes.OrderBy(a => AttributeSortIndex(a.Key)).Select(a => a.Key + " " + a.Value));
            StringBuilder result = new StringBuilder();
            result.AppendLine(string.Format(
                "{0}. {1}; {2}; {3}; {4}; HP {5}; AC {6}; {7}",
                index,
                name,
                npc.Sex,
                role,
                npc.Alignment,
                npc.HitPoints,
                npc.ArmorClass,
                attributes));
            AppendBatchField(result, isEnglish ? "Appearance" : "Внешность", npc.Appearance);
            AppendBatchField(result, isEnglish ? "Proficiencies" : "Навыки", npc.Proficiencies);
            AppendBatchField(result, isEnglish ? "Equipment" : "Снаряжение", npc.Equipment);
            return result.ToString();
        }

        private void AppendBatchField(StringBuilder result, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            string normalized = value.Replace("\r\n", "\n").Trim();
            string indented = normalized.Replace("\n", "\n   ");
            result.Append("   ");
            result.Append(label);
            result.Append(": ");
            result.AppendLine(indented);
        }

        private string GenerateBatchCharacterName(CharacterGenerationResult npc)
        {
            if (characterNameService == null) characterNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            bool female = string.Equals(npc.Sex, "Female", StringComparison.OrdinalIgnoreCase);
            return characterNameService.GeneratePersonalName(characterNameRandom, SelectedCharacterNameCultureKey(), female, !isEnglish);
        }

        private int AttributeSortIndex(string attribute)
        {
            switch (attribute)
            {
                case "STR": return 0;
                case "INT": return 1;
                case "WIL": return 2;
                case "DEX": return 3;
                case "CON": return 4;
                case "CHA": return 5;
                default: return 99;
            }
        }

        private void ShowCharacterBatchOutput(string text)
        {
            using (Form dialog = new Form())
            using (TextBox output = new TextBox())
            using (Button close = new Button())
            {
                dialog.Text = isEnglish ? "Generated NPCs" : "Сгенерированные NPC";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Size = new Size(980, 640);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = true;

                output.Multiline = true;
                output.ReadOnly = true;
                output.ScrollBars = ScrollBars.Both;
                output.WordWrap = false;
                output.Font = new Font(FontFamily.GenericMonospace, 9f);
                output.Dock = DockStyle.Fill;
                output.Text = text;

                close.Text = isEnglish ? "Close" : "Закрыть";
                close.Dock = DockStyle.Bottom;
                close.Height = 34;
                close.Click += (s, e) => dialog.Close();

                dialog.Controls.Add(output);
                dialog.Controls.Add(close);
                dialog.ShowDialog(this);
            }
        }

        private Dictionary<string, int> ReadCharacterAttributes()
        {
            return new Dictionary<string, int>
            {
                { "STR", (int)characterAttributes["STR"].Value },
                { "INT", (int)characterAttributes["INT"].Value },
                { "WIL", (int)characterAttributes["WIL"].Value },
                { "DEX", (int)characterAttributes["DEX"].Value },
                { "CON", (int)characterAttributes["CON"].Value },
                { "CHA", (int)characterAttributes["CHA"].Value }
            };
        }

        private void ApplyCharacterGenerationResult(CharacterGenerationResult result)
        {
            if (result == null) return;

            SetCombo(cmbCharacterKind, result.Kind);
            SetCombo(cmbCharacterClass, result.ClassName);
            SetCombo(cmbCharacterOccupation, result.Occupation);
            SetCombo(cmbCharacterAlignment, result.Alignment);
            SetCombo(cmbCharacterSex, result.Sex);
            txtCharacterTemplate.Text = result.Template ?? "";
            txtCharacterHomeland.Text = result.Homeland ?? "";
            nudCharacterLevel.Value = Clamp(result.Level, nudCharacterLevel);
            nudCharacterXp.Value = Clamp(result.Experience, nudCharacterXp);
            nudCharacterHp.Value = Clamp(result.HitPoints, nudCharacterHp);
            nudCharacterAc.Value = Clamp(result.ArmorClass, nudCharacterAc);
            nudCharacterAge.Value = Clamp(result.Age, nudCharacterAge);
            ApplyCharacterAttributes(result.Attributes);
            txtCharacterLanguages.Text = result.Languages ?? "";
            txtCharacterSpells.Text = result.Spells ?? "";
            txtCharacterEquipment.Text = result.Equipment ?? "";
            txtCharacterAppearance.Text = result.Appearance ?? "";
            txtCharacterBackground.Text = result.Background ?? "";
            txtCharacterNotes.Text = result.Notes ?? "";
            SetCheckedProficiencies(result.Proficiencies ?? "");

            if (result.GenerateName)
            {
                ApplyGeneratedCharacterNameIfEnabled();
            }
        }

        private void ApplyCharacterAttributes(Dictionary<string, int> attributes)
        {
            if (attributes == null) return;
            foreach (KeyValuePair<string, int> attribute in attributes)
            {
                if (characterAttributes.ContainsKey(attribute.Key))
                {
                    characterAttributes[attribute.Key].Value = Clamp(attribute.Value, characterAttributes[attribute.Key]);
                }
            }
        }
    }
}
