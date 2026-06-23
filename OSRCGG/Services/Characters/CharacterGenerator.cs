using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    // Сервис процедурной генерации персонажей держит правила ACKS отдельно от формы.
    // Наружу он открывает только запрос и результат, чтобы UI оставался тонким адаптером.
    internal sealed partial class CharacterGenerator
    {
        private readonly Random characterRandom;
        private bool isEnglish;
        private Dictionary<string, int> characterAttributes;
        private string currentClassName;
        private string currentSex;
        private string currentNotes;

        public CharacterGenerator()
            : this(new Random())
        {
        }

        public CharacterGenerator(Random random)
        {
            characterRandom = random ?? new Random();
        }

        public CharacterGenerationResult GeneratePlayer(CharacterGenerationRequest request)
        {
            BeginGeneration(request);

            // Для готового PC бросаем класс, характеристики, шаблон и производные поля одним сценарием.
            CharacterClassDefinition cls = CharacterGenerationCatalog.Classes[characterRandom.Next(CharacterGenerationCatalog.Classes.Length)];
            currentClassName = cls.Name;
            RollCharacterAttributes();
            EnsureKeyAttributes(cls);

            CharacterGenerationResult result = CreateResultShell("Player", cls.Name, "");
            result.Level = 1;
            result.Experience = 0;
            result.HitPoints = RollStartingHp(cls.HitDie);
            result.ArmorClass = RollGeneratedArmorClass();
            result.Age = 17 + RollDice(1, 6);
            result.Alignment = Pick(CharacterGenerationCatalog.Alignments);
            result.Sex = Pick(CharacterGenerationCatalog.Sexes);
            currentSex = result.Sex;

            CharacterTemplate template = RollCharacterTemplate(cls.Name);
            string templateNote = currentNotes;
            result.Template = template.Name;
            result.Homeland = cls.RaceHint == "Human" ? "Auran Empire" : cls.RaceHint + " homeland";
            result.Languages = cls.RaceHint == "Human"
                ? L("Native regional language", "Родной региональный язык")
                : L("Native regional language", "Родной региональный язык") + ", " + cls.RaceHint;
            result.Equipment = template.Equipment + " " + L("Optional fallback: 3d6 x 10 gp starting gold.", "Запасной вариант: 3d6 x 10 зм стартовых денег.");
            result.Spells = template.Spells;
            result.Appearance = "";
            result.Background = "";
            result.GenerateName = true;

            List<string> proficiencies = new List<string> { "Adventuring" };
            proficiencies.AddRange(SplitProficiencies(template.Proficiencies));
            int bonusGeneral = Math.Max(0, AttributeBonus(characterAttributes["INT"]));
            proficiencies.AddRange(PickDistinct(CharacterGenerationCatalog.GeneralProficiencies, bonusGeneral, proficiencies));
            result.Proficiencies = string.Join(", ", proficiencies.Distinct(StringComparer.OrdinalIgnoreCase));
            result.Notes = templateNote + Environment.NewLine +
                L("Template proficiencies: ", "Навыки шаблона: ") + template.Proficiencies + Environment.NewLine +
                L("INT bonus general proficiencies: ", "Бонусные общие навыки от INT: ") + bonusGeneral;

            return Complete(result);
        }

        public CharacterGenerationResult GenerateNpc(CharacterGenerationRequest request)
        {
            int requestedLevel = string.Equals(request.CurrentKind, "NPC", StringComparison.OrdinalIgnoreCase)
                ? request.RequestedLevel
                : 0;

            return requestedLevel <= 0
                ? GenerateZeroLevelNpc(request)
                : GenerateLeveledNpc(request, requestedLevel);
        }

        public string RandomizeProficiencies(CharacterGenerationRequest request)
        {
            BeginGeneration(request);
            string className = string.IsNullOrWhiteSpace(request.CurrentClassName)
                ? CharacterGenerationCatalog.Classes[0].Name
                : request.CurrentClassName;
            CharacterClassDefinition cls = CharacterGenerationCatalog.Classes.FirstOrDefault(c => c.Name == className)
                                           ?? CharacterGenerationCatalog.Classes[0];
            int level = Math.Max(1, request.RequestedLevel);

            List<string> picks = new List<string> { "Adventuring" };
            picks.AddRange(SplitProficiencies(BuildRandomClassProficiencies(cls.Name, ClassProficiencyCountForLevel(cls.Name, level), picks)));
            picks.AddRange(SplitProficiencies(BuildRandomGeneralProficiencies(GeneralProficiencyCountForLevel(level), picks)));

            return string.Join(", ", picks.Distinct());
        }

        public Dictionary<string, int> RollPlayerAttributes(CharacterGenerationRequest request)
        {
            BeginGeneration(request);
            RollCharacterAttributes();
            return new Dictionary<string, int>(characterAttributes, StringComparer.OrdinalIgnoreCase);
        }

        private CharacterGenerationResult GenerateZeroLevelNpc(CharacterGenerationRequest request)
        {
            BeginGeneration(request);

            // NPC 0 уровня строится от профессии, а не от класса: это отдельная ACKS-ветка генерации.
            currentClassName = "Fighter";
            NpcOccupationResult occupation = RollGeneralStreetOccupation();
            RollNpcAttributes();

            CharacterGenerationResult result = CreateResultShell("NPC", currentClassName, occupation.Occupation);
            result.Level = 0;
            result.Experience = 0;
            result.HitPoints = Math.Max(1, RollDice(1, 6) + AttributeBonus(characterAttributes["CON"]));
            result.ArmorClass = RollGeneratedArmorClass();
            result.Age = RollNpcAge(occupation.Occupation);
            result.Alignment = Pick(CharacterGenerationCatalog.Alignments);
            result.Sex = Pick(CharacterGenerationCatalog.Sexes);
            currentSex = result.Sex;
            result.Template = L("0th level NPC - ", "NPC 0 уровня - ") + occupation.Category;
            result.Homeland = "";
            result.Languages = L("Native regional language", "Родной региональный язык");
            result.Spells = "";
            result.Equipment = AppendNpcMagicItems(GetZeroLevelNpcEquipment(occupation), 0);
            result.Appearance = GenerateNpcAppearance();
            result.GenerateName = true;

            string profs = CharacterGenerationCatalog.NpcOccupationProficiencies.ContainsKey(occupation.Occupation)
                ? CharacterGenerationCatalog.NpcOccupationProficiencies[occupation.Occupation]
                : "";
            List<string> finalProfs = SplitProficiencies(profs).ToList();
            int bonusGeneral = Math.Max(0, AttributeBonus(characterAttributes["INT"]));
            finalProfs.AddRange(PickDistinct(CharacterGenerationCatalog.GeneralProficiencies, bonusGeneral, finalProfs));
            result.Proficiencies = string.Join(", ", finalProfs.Distinct(StringComparer.OrdinalIgnoreCase));
            result.Notes =
                L("General/Street occupation: ", "Профессия (общая/уличная): ") + occupation.Category + " -> " + occupation.Occupation + Environment.NewLine +
                L("NPC Occupation Proficiencies: ", "Профессиональные навыки NPC: ") + profs + Environment.NewLine +
                L("INT bonus general proficiencies: ", "Бонусные общие навыки от INT: ") + bonusGeneral + Environment.NewLine +
                L("Final proficiencies: ", "Итоговые навыки: ") + result.Proficiencies + Environment.NewLine +
                L("XP to 1st level: ", "XP до 1 уровня: ") + Math.Max(60, (16 - CountProficiencies(result.Proficiencies)) * 60);

            return Complete(result);
        }

        private CharacterGenerationResult GenerateLeveledNpc(CharacterGenerationRequest request, int requestedLevel)
        {
            BeginGeneration(request);

            // Уровневый NPC использует классовый шаблон, но сохраняет NPC-специфику внешности и добычи.
            int maxLevel = request.MaximumLevel <= 0 ? requestedLevel : request.MaximumLevel;
            int level = Math.Max(1, Math.Min(requestedLevel, maxLevel));
            CharacterClassDefinition cls = RollNpcClass();
            currentClassName = cls.Name;
            RollNpcAttributes();
            EnsureKeyAttributes(cls);

            CharacterTemplate template = RollCharacterTemplate(cls.Name);
            string templateNote = currentNotes;

            CharacterGenerationResult result = CreateResultShell("NPC", cls.Name, "Classed NPC");
            result.Level = level;
            result.Experience = 0;
            result.HitPoints = RollHitPointsForLevel(level, cls.HitDie);
            result.ArmorClass = RollGeneratedArmorClass();
            result.Age = 17 + RollDice(2, 6) + Math.Max(0, level - 1);
            result.Alignment = Pick(CharacterGenerationCatalog.Alignments);
            result.Sex = Pick(CharacterGenerationCatalog.Sexes);
            currentSex = result.Sex;
            result.Template = L("Leveled NPC - ", "Уровневый NPC - ") + template.Name;
            result.Homeland = cls.RaceHint == "Human" ? "Auran Empire" : cls.RaceHint + " homeland";
            result.Languages = cls.RaceHint == "Human"
                ? L("Native regional language", "Родной региональный язык")
                : L("Native regional language", "Родной региональный язык") + ", " + cls.RaceHint;
            result.Spells = !string.IsNullOrWhiteSpace(template.Spells)
                ? template.Spells
                : IsSpellcastingClass(cls.Name)
                    ? L("Roll spell repertoire for class and level.", "Бросьте/выберите репертуар заклинаний по классу и уровню.")
                    : "";
            result.Equipment = AppendNpcMagicItems(template.Equipment, level);
            result.Appearance = GenerateNpcAppearance();
            result.GenerateName = true;

            int classCount = ClassProficiencyCountForLevel(cls.Name, level);
            int generalCount = GeneralProficiencyCountForLevel(level);
            List<string> proficiencies = new List<string> { "Adventuring" };
            proficiencies.AddRange(SplitProficiencies(BuildRandomClassProficiencies(cls.Name, classCount, proficiencies)));
            proficiencies.AddRange(SplitProficiencies(BuildRandomGeneralProficiencies(generalCount, proficiencies)));
            result.Proficiencies = string.Join(", ", proficiencies.Distinct(StringComparer.OrdinalIgnoreCase));
            result.Notes =
                templateNote + Environment.NewLine +
                L("Leveled NPC: ", "Уровневый NPC: ") + cls.Name + " " + level + Environment.NewLine +
                "HP: " + level + "d" + cls.HitDie + L(" with CON modifier per level.", " с модификатором CON за каждый уровень.") + Environment.NewLine +
                L("Class proficiencies rolled: ", "Выбрано классовых навыков: ") + classCount + Environment.NewLine +
                L("General proficiencies rolled: ", "Выбрано общих навыков: ") + generalCount + L(" (includes INT modifier).", " (с учетом модификатора INT).");

            return Complete(result);
        }

        private void BeginGeneration(CharacterGenerationRequest request)
        {
            request = request ?? new CharacterGenerationRequest();
            isEnglish = request.IsEnglish;
            currentNotes = "";
            currentClassName = request.CurrentClassName ?? "";
            currentSex = "";
            characterAttributes = CloneAttributes(request.Attributes);
        }

        private CharacterGenerationResult CreateResultShell(string kind, string className, string occupation)
        {
            return new CharacterGenerationResult
            {
                Kind = kind,
                ClassName = className,
                Occupation = occupation,
                Background = "",
                Attributes = new Dictionary<string, int>(characterAttributes, StringComparer.OrdinalIgnoreCase)
            };
        }

        private CharacterGenerationResult Complete(CharacterGenerationResult result)
        {
            result.Attributes = new Dictionary<string, int>(characterAttributes, StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private Dictionary<string, int> CloneAttributes(Dictionary<string, int> source)
        {
            Dictionary<string, int> attributes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "STR", 10 },
                { "INT", 10 },
                { "WIL", 10 },
                { "DEX", 10 },
                { "CON", 10 },
                { "CHA", 10 }
            };

            if (source == null) return attributes;

            foreach (KeyValuePair<string, int> pair in source)
            {
                if (attributes.ContainsKey(pair.Key))
                {
                    attributes[pair.Key] = pair.Value;
                }
            }

            return attributes;
        }

        private void RollCharacterAttributes()
        {
            List<string> attrs = characterAttributes.Keys.ToList();
            string prime = attrs[characterRandom.Next(attrs.Count)];
            attrs.Remove(prime);
            string second = attrs[characterRandom.Next(attrs.Count)];
            attrs.Remove(second);
            string third = attrs[characterRandom.Next(attrs.Count)];
            attrs.Remove(third);

            characterAttributes[prime] = Math.Max(13, RollDropLowest(5, 2));
            characterAttributes[second] = Math.Max(9, RollDropLowest(4, 1));
            characterAttributes[third] = Math.Max(9, RollDropLowest(4, 1));
            foreach (string attr in attrs)
            {
                characterAttributes[attr] = RollDice(3, 6);
            }
        }

        private void RollNpcAttributes()
        {
            foreach (string attr in characterAttributes.Keys.ToList())
            {
                characterAttributes[attr] = RollDice(3, 6);
            }
        }

        private string PickClassProficiency(string className)
        {
            if (className.Contains("Fighter") || className.Contains("Vaultguard")) return "Weapon Focus";
            if (className.Contains("Explorer")) return "Tracking";
            if (className.Contains("Thief") || className.Contains("Nightblade")) return "Streetwise";
            if (className.Contains("Mage") || className.Contains("Spellsword")) return "Collegiate Wizardry";
            if (className.Contains("Crusader") || className.Contains("Priest") || className.Contains("Shaman")) return "Theology";
            if (className.Contains("Paladin")) return "Theology";
            if (className.Contains("Warlock")) return "Black Lore of Zahar";
            if (className.Contains("Witch")) return "Familiar";
            if (className.Contains("Venturer")) return "Bargaining";
            if (className.Contains("Bard")) return "Performance";
            if (className.Contains("Assassin")) return "Ambushing";
            if (className.Contains("Barbarian")) return "Endurance";
            return CharacterGenerationCatalog.ClassProficiencies[characterRandom.Next(CharacterGenerationCatalog.ClassProficiencies.Length)];
        }

        private void EnsureKeyAttributes(CharacterClassDefinition cls)
        {
            foreach (string key in cls.KeyAttributes.Split(',').Select(k => k.Trim()))
            {
                if (characterAttributes.ContainsKey(key) && characterAttributes[key] < 9)
                {
                    characterAttributes[key] = 9;
                }
            }
        }

        private int RollStartingHp(int hitDie)
        {
            int roll = Math.Max(4, characterRandom.Next(1, hitDie + 1));
            int hp = roll + AttributeBonus(characterAttributes["CON"]);
            return Math.Max(1, hp);
        }

        private int RollHitPointsForLevel(int level, int hitDie)
        {
            int conBonus = AttributeBonus(characterAttributes["CON"]);
            int hp = 0;
            for (int i = 0; i < level; i++)
            {
                hp += Math.Max(1, characterRandom.Next(1, hitDie + 1) + conBonus);
            }
            return Math.Max(1, hp);
        }

        private int RollGeneratedArmorClass()
        {
            return AttributeBonus(characterAttributes["DEX"]);
        }

        private int CountProficiencies(string proficiencies)
        {
            return CharacterRulesService.SplitProficiencies(proficiencies).Length;
        }

        private string Pick(string[] values)
        {
            return values[characterRandom.Next(values.Length)];
        }

        private string L(string english, string russian)
        {
            return isEnglish ? english : russian;
        }
    }
}
