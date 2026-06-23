using System.Collections.Generic;

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
            ClearCharacterLibrarySelectionForNewCharacter();
            ApplyCharacterGenerationResult(characterGenerator.GenerateNpc(BuildCharacterGenerationRequest()));
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
                RequestedLevel = (int)nudCharacterLevel.Value,
                MaximumLevel = (int)nudCharacterLevel.Maximum,
                Attributes = ReadCharacterAttributes()
            };
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
