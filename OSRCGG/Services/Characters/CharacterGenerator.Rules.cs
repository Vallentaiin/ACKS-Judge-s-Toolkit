using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class CharacterGenerator
    {
        // Правила классов, шаблоны и списки владений персонажа лежат отдельно от формы.
        private CharacterClassDefinition RollNpcClass()
        {
            return RollOnTable(new List<RollRange<CharacterClassDefinition>>
            {
                new RollRange<CharacterClassDefinition>(1, 35, CharacterGenerationCatalog.Classes.First(c => c.Name == "Fighter")),
                new RollRange<CharacterClassDefinition>(36, 50, CharacterGenerationCatalog.Classes.First(c => c.Name == "Thief")),
                new RollRange<CharacterClassDefinition>(51, 62, CharacterGenerationCatalog.Classes.First(c => c.Name == "Mage")),
                new RollRange<CharacterClassDefinition>(63, 73, CharacterGenerationCatalog.Classes.First(c => c.Name == "Crusader")),
                new RollRange<CharacterClassDefinition>(74, 82, CharacterGenerationCatalog.Classes.First(c => c.Name == "Explorer")),
                new RollRange<CharacterClassDefinition>(83, 90, CharacterGenerationCatalog.Classes.First(c => c.Name == "Venturer")),
                new RollRange<CharacterClassDefinition>(91, 95, CharacterGenerationCatalog.Classes.First(c => c.Name == "Assassin")),
                new RollRange<CharacterClassDefinition>(96, 100, CharacterGenerationCatalog.Classes.First(c => c.Name == "Barbarian"))
            }, 100);
        }

        private string BuildRandomGeneralProficiencies(int count, IEnumerable<string> excluded)
        {
            return string.Join(", ", PickDistinct(CharacterGenerationCatalog.GeneralProficiencies, count, excluded));
        }

        private string BuildRandomClassProficiencies(string className, int count, IEnumerable<string> excluded)
        {
            List<string> pool = new List<string>();
            pool.Add(PickClassProficiency(className));
            pool.AddRange(CharacterGenerationCatalog.ClassProficiencies);
            return string.Join(", ", PickDistinct(pool.ToArray(), count, excluded));
        }

        private List<string> PickDistinct(string[] pool, int count, IEnumerable<string> excluded)
        {
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (excluded != null)
            {
                foreach (string item in excluded)
                {
                    string normalized = NormalizeProficiencyName(item);
                    if (!string.IsNullOrWhiteSpace(normalized)) used.Add(normalized);
                }
            }

            List<string> picks = new List<string>();
            List<string> candidates = pool.Where(p => !used.Contains(NormalizeProficiencyName(p))).Distinct().ToList();
            while (picks.Count < count && candidates.Count > 0)
            {
                int index = characterRandom.Next(candidates.Count);
                string pick = candidates[index];
                candidates.RemoveAt(index);
                picks.Add(pick);
                used.Add(NormalizeProficiencyName(pick));
            }

            return picks;
        }

        private string NormalizeProficiencyName(string proficiency)
        {
            return CharacterRulesService.NormalizeProficiencyName(proficiency);
        }

        private string[] SplitProficiencies(string proficiencies)
        {
            return CharacterRulesService.SplitProficiencies(proficiencies);
        }

        private int GeneralProficiencyCountForLevel(int level)
        {
            int intelligenceBonus = AttributeBonus((int)characterAttributes["INT"]);
            return CharacterRulesService.GeneralProficiencyCountForLevel(level, intelligenceBonus);
        }

        private int ClassProficiencyCountForLevel(string className, int level)
        {
            return CharacterRulesService.ClassProficiencyCountForLevel(className, level);
        }

        private CharacterTemplate RollCharacterTemplate(string className)
        {
            Dictionary<string, CharacterTemplate[]> templates = CreateTemplateMap();
            CharacterTemplate[] list = templates.ContainsKey(className) ? templates[className] : CreateFallbackTemplates(className);
            int roll = RollDice(3, 6);
            int index = roll <= 4 ? 0 : roll <= 6 ? 1 : roll <= 8 ? 2 : roll <= 10 ? 3 : roll <= 12 ? 4 : roll <= 14 ? 5 : roll <= 16 ? 6 : 7;
            CharacterTemplate template = list[index];
            currentNotes = L("Template roll: ", "Бросок шаблона: ") + roll + L(" on the ", " по таблице ") + className + L(" template table.", ".");
            return template;
        }

        private CharacterTemplate[] CreateFallbackTemplates(string className)
        {
            string spells = IsSpellcastingClass(className)
                ? L("Use the rolled ", "Используйте стартовые заклинания из шаблона ") + className + L(" template starting spells.", ".")
                : "";
            string equipment = L("Use the rolled ", "Используйте снаряжение из шаблона ") + className + L(" template equipment.", ".");
            return new[]
            {
                new CharacterTemplate(className + " Template 3-4", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 5-6", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 7-8", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 9-10", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 11-12", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 13-14", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 15-16", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells),
                new CharacterTemplate(className + " Template 17-18", PickClassProficiency(className) + ", " + CharacterGenerationCatalog.GeneralProficiencies[characterRandom.Next(CharacterGenerationCatalog.GeneralProficiencies.Length)], equipment, spells)
            };
        }

        private bool IsSpellcastingClass(string className)
        {
            return className.IndexOf("Mage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Spellsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Priest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Wonderworker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Warlock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Witch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Shaman", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Bladedancer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Nightblade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   className.IndexOf("Craftpriest", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Dictionary<string, CharacterTemplate[]> CreateTemplateMap()
        {
            CharacterTemplate[] fighter =
            {
                new CharacterTemplate("Thug", "Combat Ferocity, Intimidation", "Template equipment from Fighter: Thug."),
                new CharacterTemplate("Ravager", "Berserkergang, Endurance", "Template equipment from Fighter: Ravager."),
                new CharacterTemplate("Corsair", "Swashbuckling, Seafaring", "Template equipment from Fighter: Corsair."),
                new CharacterTemplate("Auxiliary", "Skirmishing, Labor (construction)", "Template equipment from Fighter: Auxiliary."),
                new CharacterTemplate("Legionary", "Fighting Style Specialization (weapon & shield), Siege Engineering", "Template equipment from Fighter: Legionary."),
                new CharacterTemplate("Cataphract", "Mounted Combat, Riding", "Template equipment from Fighter: Cataphract."),
                new CharacterTemplate("Knight", "Command, Military Strategy", "Template equipment from Fighter: Knight."),
                new CharacterTemplate("Champion", "Weapon Focus, Manual of Arms", "Template equipment from Fighter: Champion.")
            };

            return new Dictionary<string, CharacterTemplate[]>()
            {
                {"Fighter", fighter},
                {"Explorer", new [] {
                    new CharacterTemplate("Wanderer", "Running, Labor (construction)", "Template equipment from Explorer: Wanderer."),
                    new CharacterTemplate("Cartographer", "Land Surveying, Mapping", "Template equipment from Explorer: Cartographer."),
                    new CharacterTemplate("Mariner", "Swashbuckling, Seafaring", "Template equipment from Explorer: Mariner."),
                    new CharacterTemplate("Scout", "Skirmishing, Tracking", "Template equipment from Explorer: Scout."),
                    new CharacterTemplate("Guide", "Navigation, Survival", "Template equipment from Explorer: Guide."),
                    new CharacterTemplate("Pathfinder", "Passing Without Trace, Naturalism", "Template equipment from Explorer: Pathfinder."),
                    new CharacterTemplate("Warden", "Precise Shooting, Riding", "Template equipment from Explorer: Warden."),
                    new CharacterTemplate("Beast Hunter", "Ambushing, Beast Friendship", "Template equipment from Explorer: Beast Hunter.") }},
                {"Thief", new [] {
                    new CharacterTemplate("Footpad", "Skulking, Streetwise", "Template equipment from Thief: Footpad."),
                    new CharacterTemplate("Burglar", "Cat Burglary, Lockpicking Expertise", "Template equipment from Thief: Burglar."),
                    new CharacterTemplate("Cutpurse", "Gambling, Bribery", "Template equipment from Thief: Cutpurse."),
                    new CharacterTemplate("Tomb Robber", "Trapfinding, Mapping", "Template equipment from Thief: Tomb Robber."),
                    new CharacterTemplate("Smuggler", "Seafaring, Bargaining", "Template equipment from Thief: Smuggler."),
                    new CharacterTemplate("Assayer", "Art, Appraisal", "Template equipment from Thief: Assayer."),
                    new CharacterTemplate("Sharpshooter", "Precise Shooting, Sniping", "Template equipment from Thief: Sharpshooter."),
                    new CharacterTemplate("Guild Agent", "Bribery, Diplomacy", "Template equipment from Thief: Guild Agent.") }},
                {"Mage", new [] {
                    new CharacterTemplate("Hedge Wizard", "Mastery of Enchantments & Illusions, Healing, Survival", "Template equipment from Mage: Hedge Wizard.", "Beguile humanoid, auditory illusion."),
                    new CharacterTemplate("Seer", "Soothsaying, Knowledge (astrology), Language", "Template equipment from Mage: Seer.", "Detect magic, read languages."),
                    new CharacterTemplate("Elementalist", "Elementalism, Naturalism, Alchemy", "Template equipment from Mage: Elementalist.", "Elemental starting spells."),
                    new CharacterTemplate("Necromancer", "Black Lore of Zahar, Knowledge (occult), Quiet Magic", "Template equipment from Mage: Necromancer.", "Dark arcane starting spells."),
                    new CharacterTemplate("Conjurer", "Mastery of Conjuration & Summoning, Bargaining, Diplomacy", "Template equipment from Mage: Conjurer.", "Summoning-oriented starting spells."),
                    new CharacterTemplate("Loremaster", "Loremastery, Knowledge, Language", "Template equipment from Mage: Loremaster.", "Research-oriented starting spells."),
                    new CharacterTemplate("Artificer", "Magical Engineering, Engineering, Craft", "Template equipment from Mage: Artificer.", "Utility starting spells."),
                    new CharacterTemplate("Battle Mage", "Battle Magic, Precise Shooting, Unflappable Casting", "Template equipment from Mage: Battle Mage.", "Combat starting spells.") }},
                {"Crusader", new [] {
                    new CharacterTemplate("Hermit", "Laying on Hands, Naturalism", "Template equipment from Crusader: Hermit."),
                    new CharacterTemplate("Prophet", "Prophecy, Performance (storytelling)", "Template equipment from Crusader: Prophet."),
                    new CharacterTemplate("Mendicant", "Beast Friendship, Animal Husbandry", "Template equipment from Crusader: Mendicant."),
                    new CharacterTemplate("Proselytizer", "Divine Health, Diplomacy", "Template equipment from Crusader: Proselytizer."),
                    new CharacterTemplate("Priest", "Divine Blessing, Theology 2", "Template equipment from Crusader: Priest."),
                    new CharacterTemplate("Undead Slayer", "Righteous Rebuke, Healing", "Template equipment from Crusader: Undead Slayer."),
                    new CharacterTemplate("Exorcist", "Sensing Evil, Intimidation", "Template equipment from Crusader: Exorcist."),
                    new CharacterTemplate("Templar", "Martial Training (swords/daggers), Riding", "Template equipment from Crusader: Templar.") }},
                {"Venturer", new [] {
                    new CharacterTemplate("Bankrupt", "Running, Gambling, Driving", "Template equipment from Venturer: Bankrupt."),
                    new CharacterTemplate("Peddler", "Bargaining, Driving", "Template equipment from Venturer: Peddler."),
                    new CharacterTemplate("Factor", "Profession (merchant), Diplomacy", "Template equipment from Venturer: Factor."),
                    new CharacterTemplate("Smuggler", "Streetwise, Seafaring", "Template equipment from Venturer: Smuggler."),
                    new CharacterTemplate("Caravaneer", "Riding, Navigation", "Template equipment from Venturer: Caravaneer."),
                    new CharacterTemplate("Guild Merchant", "Bargaining, Profession (merchant) 2", "Template equipment from Venturer: Guild Merchant."),
                    new CharacterTemplate("Explorer-Merchant", "Survival, Mapping", "Template equipment from Venturer: Explorer-Merchant."),
                    new CharacterTemplate("Merchant Prince", "Leadership, Command", "Template equipment from Venturer: Merchant Prince.") }},
                {"Assassin", new [] {
                    new CharacterTemplate("Cutthroat", "Combat Reflexes, Gambling", "Template equipment from Assassin: Cutthroat."),
                    new CharacterTemplate("Bounty Hunter", "Combat Trickery (incapacitate), Tracking", "Template equipment from Assassin: Bounty Hunter."),
                    new CharacterTemplate("Poisoner", "Poisoning, Alchemy", "Template equipment from Assassin: Poisoner."),
                    new CharacterTemplate("Spy", "Disguise, Eavesdropping", "Template equipment from Assassin: Spy."),
                    new CharacterTemplate("Stalker", "Skulking, Ambushing", "Template equipment from Assassin: Stalker."),
                    new CharacterTemplate("Marksman", "Precise Shooting, Sniping", "Template equipment from Assassin: Marksman."),
                    new CharacterTemplate("Guild Killer", "Streetwise, Bribery", "Template equipment from Assassin: Guild Killer."),
                    new CharacterTemplate("Executioner", "Weapon Focus, Intimidation", "Template equipment from Assassin: Executioner.") }},
                {"Barbarian", new [] {
                    new CharacterTemplate("Tribal Warrior", "Ambushing, Tracking, Running, Endurance", "Template equipment from Barbarian: Tribal Warrior."),
                    new CharacterTemplate("Berserker", "Berserkergang, Intimidation, Climbing, Seafaring", "Template equipment from Barbarian: Berserker."),
                    new CharacterTemplate("Sea Rover", "Swashbuckling, Navigation, Climbing, Seafaring", "Template equipment from Barbarian: Sea Rover."),
                    new CharacterTemplate("Skirmisher", "Skirmishing, Endurance, Precise Shooting, Riding", "Template equipment from Barbarian: Skirmisher."),
                    new CharacterTemplate("Death Dealer", "Combat Ferocity, Survival, Climbing, Seafaring", "Template equipment from Barbarian: Death Dealer."),
                    new CharacterTemplate("Pit Fighter", "Combat Reflexes, Gambling, Running, Endurance", "Template equipment from Barbarian: Pit Fighter."),
                    new CharacterTemplate("Hunter", "Beast Friendship, Tracking, Survival, Precise Shooting", "Template equipment from Barbarian: Hunter."),
                    new CharacterTemplate("War Chief", "Command, Leadership, Riding, Military Strategy", "Template equipment from Barbarian: War Chief.") }}
            };
        }
    }
}
