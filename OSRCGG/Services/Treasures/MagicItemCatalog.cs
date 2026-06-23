using System;

namespace OSRCGG
{
    internal static class MagicItemCatalog
    {
        public static string Roll(TreasureTableMode mode, string kind, Random random)
        {
            random = random ?? new Random();
            kind = string.IsNullOrWhiteSpace(kind) ? "Any" : kind.Trim();

            if (mode == TreasureTableMode.Heroic || IsRarity(kind))
            {
                return RollByRarity(NormalizeRarity(kind), random);
            }

            if (string.Equals(kind, "Any", StringComparison.OrdinalIgnoreCase)) return RollClassicAny(random);
            if (string.Equals(kind, "SwordWeaponArmor", StringComparison.OrdinalIgnoreCase))
            {
                string[] kinds = { "Sword", "Weapon", "Armor" };
                return RollClassic(kinds[random.Next(kinds.Length)], random);
            }

            return RollClassic(kind, random);
        }

        public static string RollByRarity(string rarity, Random random)
        {
            random = random ?? new Random();
            switch (NormalizeRarity(rarity))
            {
                case "Common": return TreasureDice.RollOnTable(random, CommonItems, 100);
                case "Uncommon": return TreasureDice.RollOnTable(random, UncommonItems, 100);
                case "Rare": return TreasureDice.RollOnTable(random, RareItems, 100);
                case "Very Rare": return TreasureDice.RollOnTable(random, VeryRareItems, 100);
                default: return TreasureDice.RollOnTable(random, LegendaryItems, 100);
            }
        }

        public static string RollClassic(string kind, Random random)
        {
            random = random ?? new Random();
            switch ((kind ?? "").Trim().ToLowerInvariant())
            {
                case "potion": case "potions": return TreasureDice.RollOnTable(random, ClassicPotions, 100);
                case "ring": case "rings": return TreasureDice.RollOnTable(random, ClassicRings, 100);
                case "scroll": case "scrolls": return TreasureDice.RollOnTable(random, ClassicScrolls, 100);
                case "implement": case "implements": return TreasureDice.RollOnTable(random, ClassicImplements, 100);
                case "misc": case "miscellaneous item": case "miscellaneous magic items": return TreasureDice.RollOnTable(random, ClassicMiscItems, 100);
                case "sword": case "swords": return TreasureDice.RollOnTable(random, ClassicSwords, 100);
                case "weapon": case "miscellaneous weapons": return TreasureDice.RollOnTable(random, ClassicWeapons, 100);
                case "armor": case "armors": case "armor and shield": return TreasureDice.RollOnTable(random, ClassicArmors, 100);
                default: return RollClassicAny(random);
            }
        }

        private static string RollClassicAny(Random random)
        {
            int roll = random.Next(1, 101);
            if (roll <= 20) return RollClassic("Potion", random);
            if (roll <= 25) return RollClassic("Ring", random);
            if (roll <= 56) return RollClassic("Scroll", random);
            if (roll <= 61) return RollClassic("Implement", random);
            if (roll <= 66) return RollClassic("Miscellaneous Item", random);
            if (roll <= 87) return RollClassic("Sword", random);
            if (roll <= 92) return RollClassic("Weapon", random);
            return RollClassic("Armor", random);
        }

        private static bool IsRarity(string value)
        {
            value = NormalizeRarity(value);
            return value == "Common" || value == "Uncommon" || value == "Rare" || value == "Very Rare" || value == "Legendary";
        }

        private static string NormalizeRarity(string rarity)
        {
            string value = (rarity ?? "").Trim();
            if (string.Equals(value, "Common", StringComparison.OrdinalIgnoreCase)) return "Common";
            if (string.Equals(value, "Uncommon", StringComparison.OrdinalIgnoreCase)) return "Uncommon";
            if (string.Equals(value, "Rare", StringComparison.OrdinalIgnoreCase)) return "Rare";
            if (string.Equals(value, "Very Rare", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "VeryRare", StringComparison.OrdinalIgnoreCase)) return "Very Rare";
            if (string.Equals(value, "Legendary", StringComparison.OrdinalIgnoreCase)) return "Legendary";
            return "Common";
        }

        private static RollRange<string> R(int min, int max, string value)
        {
            return new RollRange<string>(min, max, value);
        }

        private static readonly RollRange<string>[] ClassicPotions =
        {
            R(1,1,"Potion of Adjust Self"), R(2,3,"Potion of Allure"), R(4,4,"Potion of Angelic Aura"),
            R(5,5,"Potion of Arcane Armor"), R(6,6,"Potion of Blast Ward"), R(7,7,"Potion of Clairaudiency"),
            R(8,8,"Potion of Clairvoyancy"), R(9,9,"Potion of Cure Critical Injury"), R(10,10,"Potion of Cure Disease"),
            R(11,13,"Potion of Cure Light Injury"), R(14,14,"Potion of Cure Major Injury"), R(15,15,"Potion of Cure Moderate Injury"),
            R(16,16,"Potion of Cure Serious Injury"), R(17,17,"Potion of Death Ward"), R(18,18,"Potion of Deflect Ordinary Missiles"),
            R(19,19,"Potion of Deflect Ordinary Weapons"), R(20,20,"Potion of Delay Disease"), R(21,21,"Potion of Delay Poison"),
            R(22,22,"Potion of Depetrification"), R(23,23,"Potion of Disappearing"), R(24,24,"Potion of Discern Evil"),
            R(25,26,"Potion of Discern Invisible"), R(27,27,"Potion of Discern Magic"), R(28,28,"Potion of Divine Armor"),
            R(29,29,"Potion of Divine Protection"), R(30,30,"Potion of Dragon Control"), R(31,32,"Potion of Energy Invulnerability (roll for type)"),
            R(33,34,"Potion of Energy Protection (roll for type)"), R(35,35,"Potion of Flight"), R(36,36,"Potion of Freedom"),
            R(37,37,"Potion of Gaseous Form"), R(38,38,"Potion of Giant Control"), R(39,39,"Potion of Giant Strength"),
            R(40,40,"Potion of Growth"), R(41,41,"Potion of Guise Self"), R(42,43,"Potion of Hallucination"),
            R(44,44,"Healing Salve"), R(45,45,"Potion of Inaudibility"), R(46,46,"Potion of Indiscernibility"),
            R(47,48,"Potion of Invisibility"), R(49,49,"Potion of Invulnerability to Evil"), R(50,50,"Potion of Leaping"),
            R(51,51,"Potion of Levitation"), R(52,52,"Potion of Lightless Vision"), R(53,53,"Potion of Locate Object"),
            R(54,54,"Potion of Locate Treasure"), R(55,55,"Potion of Necromantic Invulnerability"), R(56,56,"Potion of Necromantic Potence"),
            R(57,58,"Potion of Neutralize Poison"), R(59,60,"Potion of Ogre Strength"), R(61,61,"Oil of Extra-Sharpness"),
            R(62,62,"Oil of Ooze"), R(63,64,"Oil of Sharpness"), R(65,65,"Oil of Slickness"), R(66,66,"Oil of the Secret Fire"),
            R(67,68,"Potion of Passion"), R(69,70,"Potion of Physical Invulnerability (roll for type)"),
            R(71,72,"Potion of Physical Protection (roll for type)"), R(73,73,"Potion of Poison"), R(74,74,"Potion of Recuperation"),
            R(75,75,"Potion of Remove Curse"), R(76,76,"Potion of Shimmer"), R(77,77,"Potion of Shrinking"),
            R(78,78,"Potion of Skinchange"), R(79,79,"Potion of Speak with Beasts"), R(80,80,"Potion of Speak with Plants"),
            R(81,81,"Potion of Spellward"), R(82,82,"Potion of Spider Climbing"), R(83,83,"Potion of Supreme Valiance"),
            R(84,84,"Potion of Swift Sword"), R(85,85,"Potion of Swift Sword, Sustained"), R(86,86,"Potion of Swimming"),
            R(87,87,"Potion of Telekinesis"), R(88,88,"Potion of Telepathy"), R(89,89,"Potion of Tongues"),
            R(90,90,"Potion of Transform Self"), R(91,91,"Potion of Trollblood"), R(92,92,"Potion of True Seeing"),
            R(93,94,"Potion of Valiance"), R(95,95,"Potion of Vigor"), R(96,96,"Potion of Water Breathing"),
            R(97,97,"Potion of Water Walking"), R(98,98,"Potion of Winged Flight"), R(99,99,"Potion of X-Ray Vision"),
            R(100,100,"Potion of Youth")
        };

        private static readonly RollRange<string>[] ClassicRings =
        {
            R(1,4,"Ring of Anti-Magic"), R(5,8,"Ring of Beast Control"), R(9,13,"Ring of Feebleness"),
            R(14,24,"Ring of Fire Protection"), R(25,28,"Ring of Genie Summoning"), R(29,33,"Ring of Hallucination"),
            R(34,38,"Ring of Humanoid Control"), R(39,49,"Ring of Invisibility"), R(50,54,"Ring of Plant Control"),
            R(55,67,"Ring of Protection +1"), R(68,73,"Ring of Protection +2"), R(74,78,"Ring of Protection +2, 5' radius"),
            R(79,81,"Ring of Protection +3"), R(82,82,"Ring of Protection +3, 5' radius"), R(83,83,"Ring of the Queen's Heart"),
            R(84,84,"Ring of Regeneration"), R(85,85,"Seal of Chaos"), R(86,89,"Ring of Telekinetic Force"),
            R(90,95,"Ring of Water Walking"), R(96,96,"Ring of Wishes"), R(97,100,"Ring of X-Ray Vision")
        };

        private static readonly RollRange<string>[] ClassicScrolls =
        {
            R(1,10,"Scroll of Creature Warding"), R(11,13,"Cursed Scroll"), R(14,18,"Scroll of Magic Warding"),
            R(19,32,"Spell Scroll (1 level)"), R(33,41,"Spell Scroll (2 levels)"), R(42,48,"Spell Scroll (3 levels)"),
            R(49,53,"Spell Scroll (4 levels)"), R(54,57,"Spell Scroll (5 levels)"), R(58,61,"Spell Scroll (6 levels)"),
            R(62,64,"Spell Scroll (7 levels)"), R(65,67,"Spell Scroll (8 levels)"), R(68,69,"Spell Scroll (9 levels)"),
            R(70,71,"Spell Scroll (10 levels)"), R(72,72,"Spell Scroll (12 levels)"), R(73,73,"Spell Scroll (14 levels)"),
            R(74,74,"Spell Scroll (16 levels)"), R(75,75,"Spell Scroll (18 levels)"), R(76,76,"Spell Scroll (20 levels)"),
            R(77,77,"Spell Scroll (22 levels)"), R(78,78,"Spell Scroll (24 levels)"), R(79,79,"Spell Scroll (ritual)"),
            R(80,84,"Treasure Map (Treasure Type B)"), R(85,88,"Treasure Map (Treasure Type D)"),
            R(89,91,"Treasure Map (Treasure Type H)"), R(92,94,"Treasure Map (Treasure Type N)"),
            R(95,97,"Treasure Map (Treasure Type Q)"), R(98,100,"Treasure Map (Treasure Type R)")
        };

        private static readonly RollRange<string>[] ClassicImplements =
        {
            R(1,5,"Rod of Eternal Torture"), R(6,7,"Rod of Nullification"), R(8,9,"Staff of Domination"),
            R(10,11,"Staff of Elemental Power (roll for element)"), R(12,19,"Staff of Smiting"), R(20,27,"Staff of the Healer"),
            R(28,28,"Staff of the Mage"), R(29,30,"Staff of Wilting"), R(31,35,"Wand of Fear"), R(36,41,"Wand of Figments"),
            R(42,47,"Wand of Fireballs"), R(48,52,"Wand of Foe Discernment"), R(53,57,"Wand of Frost"),
            R(58,62,"Wand of Impetus"), R(63,65,"Wand of Item Negation"), R(66,75,"Wand of Mage Missiles"),
            R(76,82,"Wand of Magic Discernment"), R(83,86,"Wand of Metal Location"), R(87,91,"Wand of Paralysis"),
            R(92,96,"Wand of Thunderbolts"), R(97,99,"Wand of Transformation"), R(100,100,"Legendary implement (staff of the archmage, rod of cataclysm, or rod of resurrection)")
        };

        private static readonly RollRange<string>[] ClassicMiscItems =
        {
            R(1,2,"Amulet of Indiscernibility"), R(3,3,"Bag of Oblivion"), R(4,4,"Basin of Conjuring Water Elementals"),
            R(5,5,"Belt of Dwarvenkind"), R(6,8,"Belt of Giant Strength"), R(9,9,"Boots of Bounding"),
            R(10,10,"Boots of Levitating"), R(11,12,"Boots of Striding"), R(13,15,"Boots of the Elven Ranger"),
            R(16,16,"Boots of the Prizefighter"), R(17,19,"Bottomless Bag"), R(20,23,"Bracers of Armor"),
            R(24,24,"Brooch of Arcane Armor"), R(25,25,"Candle of Contemplation (2d6)"), R(26,26,"Censer of Prophetic Dreams"),
            R(27,27,"Chariot of the Gods"), R(28,28,"Chime of Unlocking"), R(29,31,"Cloak of Protection +1"),
            R(32,33,"Cloak of Protection +2"), R(34,34,"Cloak of Protection +3"), R(35,37,"Cloak of Skinchanging"),
            R(38,40,"Cloak of the Elven Ranger"), R(41,41,"Collapsible Boat"), R(42,42,"Collar of Conversation"),
            R(43,43,"Crucible of Conjuring Fire Elementals"), R(44,45,"Crystal Ball"), R(46,46,"Crystal Ball, Clairaudient"),
            R(47,47,"Crystal Ball, Telepathic"), R(48,48,"Cube of Cold Immunity"), R(49,50,"Dead Man's Hand"),
            R(51,51,"Drums of Terror"), R(52,53,"Dust of Revelation (3d10)"), R(54,55,"Dust of Vanishing (3d10)"),
            R(56,56,"Elven Rope"), R(57,57,"Faerie Seed (2d4)"), R(58,61,"Gauntlets of Ogre Strength"),
            R(62,62,"Gem of Conjuring Earth Elementals"), R(63,63,"Gem of Force"), R(64,66,"Genie Bottle"),
            R(67,67,"Hammer of the Master Smith"), R(68,69,"Helm of Awe"), R(70,71,"Helm of Comprehension"),
            R(72,73,"Helm of Disalignment"), R(74,75,"Helm of Telepathy"), R(76,76,"Helm of Teleportation"),
            R(77,77,"Horn of the Eagles"), R(78,78,"Implement of the Night Sky"), R(79,79,"Inexhaustible Flask"),
            R(80,82,"Magic Carpet"), R(83,84,"Marvelous Dwarven Mechanism"), R(85,85,"Mirror of Nemesis"),
            R(86,86,"Necklace of Acclimatization"), R(87,87,"Rope of Ascent"), R(88,90,"Scarab of Life"),
            R(91,92,"Seismic Horn"), R(93,93,"Thurible of Conjuring Air Elementals"), R(94,94,"Visor of the Eagle"),
            R(95,96,"Witch's Broom"), R(97,97,"Woodwind of the Woodlands"), R(98,100,"Legendary miscellaneous magic item")
        };

        private static readonly RollRange<string>[] ClassicSwords =
        {
            R(1,2,"Dagger +1, Delver's"), R(3,27,"Sword +1"), R(28,47,"Sword +1, +2 versus X"),
            R(48,62,"Sword +1, +3 versus X"), R(63,69,"Sword +1, Bright"), R(70,71,"Sword +1, Deathless"),
            R(72,73,"Sword +1, Flamebrand"), R(74,78,"Sword +1, Locating"), R(79,88,"Sword +2"),
            R(89,90,"Sword +2, Beguilement"), R(91,92,"Sword +2, Command"), R(93,94,"Sword +2, Valor"),
            R(95,99,"Sword +3"), R(100,100,"Legendary sword")
        };

        private static readonly RollRange<string>[] ClassicWeapons =
        {
            R(1,2,"Arrow +3, Death"), R(3,20,"Ammunition +1 (2d10)"), R(21,28,"Ammunition +2 (2d10)"),
            R(29,32,"Ammunition +3 (2d10)"), R(33,38,"Axe +1"), R(39,39,"Axe +1/+2 versus X"),
            R(40,40,"Axe +1/+3 versus X"), R(41,43,"Axe +2"), R(44,45,"Axe +3"),
            R(46,51,"Bludgeon +1"), R(52,52,"Bludgeon +1/+2 versus X"), R(53,53,"Bludgeon +1/+3 versus X"),
            R(54,56,"Bludgeon +2"), R(57,58,"Bludgeon +3"), R(59,67,"Bow +1"), R(68,68,"Bow +1/+2 versus X"),
            R(69,69,"Bow +1/+3 versus X"), R(70,73,"Bow +2"), R(74,75,"Bow +3"), R(76,76,"Crossbow +1, Dragon's Breath"),
            R(77,77,"Javelin +3, Hurling"), R(78,80,"Other Weapon +1"), R(81,81,"Other Weapon +1/+2 versus X"),
            R(82,82,"Other Weapon +2"), R(83,83,"Other Weapon +3"), R(84,89,"Spear +1"),
            R(90,90,"Spear +1/+2 versus X"), R(91,91,"Spear +1/+3 versus X"), R(92,94,"Spear +2"),
            R(95,95,"Spear +2, Wolf-Fang"), R(96,97,"Spear +3"), R(98,100,"Legendary miscellaneous weapon")
        };

        private static readonly RollRange<string>[] ClassicArmors =
        {
            R(1,21,"Armor +1"), R(22,37,"Armor +2"), R(38,49,"Armor +3"),
            R(50,70,"Shield +1"), R(71,71,"Shield +1, Burden"), R(72,87,"Shield +2"),
            R(88,99,"Shield +3"), R(100,100,"Legendary armor or shield")
        };

        private static readonly RollRange<string>[] CommonItems =
        {
            R(1,25,"Ammunition +1 (1)"), R(26,26,"Healing Salve"), R(27,27,"Oil of Ooze"), R(28,30,"Oil of Sharpness"),
            R(31,31,"Oil of Slickness"), R(32,32,"Potion of Adjust Self"), R(33,34,"Potion of Allure"), R(35,35,"Potion of Arcane Armor"),
            R(36,41,"Potion of Cure Light Injury"), R(42,42,"Potion of Cure Moderate Injury"), R(43,43,"Potion of Delay Disease"),
            R(44,44,"Potion of Delay Poison"), R(45,45,"Potion of Discern Evil"), R(46,46,"Potion of Discern Invisible"),
            R(47,47,"Potion of Discern Magic"), R(48,48,"Potion of Divine Armor"), R(49,50,"Potion of Energy Protection (roll for type)"),
            R(51,51,"Potion of Hallucination"), R(52,52,"Potion of Leaping"), R(53,53,"Potion of Levitation"), R(54,54,"Potion of Locate Object"),
            R(55,55,"Potion of Ogre Strength"), R(56,57,"Potion of Physical Protection (roll for type)"), R(58,58,"Potion of Shimmer"),
            R(59,59,"Potion of Spider Climbing"), R(60,60,"Potion of Swift Sword"), R(61,61,"Potion of Swimming"),
            R(62,71,"Scroll of Creature Warding"), R(72,91,"Spell Scroll (1 level)"), R(92,98,"Spell Scroll (2 levels)"),
            R(99,100,"Treasure Map (Treasure Type B)")
        };

        private static readonly RollRange<string>[] UncommonItems =
        {
            R(1,2,"Ammunition +1 (2d10)"), R(3,6,"Armor +1"), R(7,7,"Arrow +3, Death"), R(8,8,"Axe +1"),
            R(9,9,"Bludgeon +1"), R(10,10,"Bow +1"), R(11,11,"Bracers of Armor +1"), R(12,13,"Cursed Scroll"),
            R(14,14,"Elven Rope"), R(15,15,"Mirror of Nemesis"), R(16,16,"Oil of Extra-Sharpness"), R(17,17,"Oil of the Secret Fire"),
            R(18,18,"Other Weapon +1"), R(19,19,"Potion of Angelic Aura"), R(20,20,"Potion of Blast Ward"),
            R(21,21,"Potion of Clairaudiency"), R(22,22,"Potion of Clairvoyancy"), R(23,23,"Potion of Cure Critical Injury"),
            R(24,24,"Potion of Cure Disease"), R(25,25,"Potion of Cure Major Injury"), R(26,26,"Potion of Cure Serious Injury"),
            R(27,27,"Potion of Death Ward"), R(28,28,"Potion of Deflect Ordinary Missiles"), R(29,29,"Potion of Deflect Ordinary Weapons"),
            R(30,30,"Potion of Depetrification"), R(31,31,"Potion of Disappearing"), R(32,32,"Potion of Divine Protection"),
            R(33,33,"Potion of Dragon Control"), R(34,35,"Potion of Energy Invulnerability (roll for type)"), R(36,36,"Potion of Flight"),
            R(37,37,"Potion of Freedom"), R(38,38,"Potion of Gaseous Form"), R(39,39,"Potion of Giant Control"), R(40,40,"Potion of Giant Strength"),
            R(41,41,"Potion of Growth"), R(42,42,"Potion of Guise Self"), R(43,43,"Potion of Inaudibility"), R(44,44,"Potion of Invisibility"),
            R(45,45,"Potion of Invulnerability to Evil"), R(46,46,"Potion of Lightless Vision"), R(47,47,"Potion of Locate Treasure"),
            R(48,48,"Potion of Necromantic Potence"), R(49,50,"Potion of Neutralize Poison"), R(51,53,"Potion of Passion"),
            R(54,55,"Potion of Physical Invulnerability (roll for type)"), R(56,56,"Potion of Poison"), R(57,57,"Potion of Recuperation"),
            R(58,58,"Potion of Remove Curse"), R(59,59,"Potion of Shrinking"), R(60,60,"Potion of Skinchange"),
            R(61,61,"Potion of Speak with Beasts"), R(62,62,"Potion of Spellward"), R(63,63,"Potion of Supreme Valiance"),
            R(64,64,"Potion of Swift Sword, Sustained"), R(65,65,"Potion of Transform Self"), R(66,66,"Potion of Trollblood"),
            R(67,67,"Potion of Valiance"), R(68,68,"Potion of Vigor"), R(69,69,"Potion of Water Breathing"), R(70,70,"Potion of Water Walking"),
            R(71,71,"Potion of Winged Flight"), R(72,72,"Ring of Feebleness"), R(73,73,"Ring of Hallucination"), R(74,74,"Scroll of Magic Warding"),
            R(75,76,"Shield +1"), R(77,77,"Spear +1"), R(78,80,"Spell Scroll (3 levels)"), R(81,82,"Spell Scroll (4 levels)"),
            R(83,84,"Spell Scroll (5 levels)"), R(85,85,"Spell Scroll (6 levels)"), R(86,86,"Spell Scroll (7 levels)"),
            R(87,87,"Spell Scroll (8 levels)"), R(88,88,"Spell Scroll (9 levels)"), R(89,89,"Spell Scroll (10 levels)"),
            R(90,97,"Sword +1"), R(98,99,"Treasure Map (Treasure Type D)"), R(100,100,"Woodwind of the Woodlands")
        };

        private static readonly RollRange<string>[] RareItems =
        {
            R(1,3,"Ammunition +2 (2d10)"), R(4,8,"Armor +2"), R(9,9,"Axe +1/+2 versus X"), R(10,10,"Axe +1/+3 versus X"),
            R(11,11,"Axe +2"), R(12,12,"Basin of Conjuring Water Elementals"), R(13,13,"Bludgeon +1/+2 versus X"),
            R(14,14,"Bludgeon +1/+3 versus X"), R(15,15,"Bludgeon +2"), R(16,16,"Boots of Bounding"), R(17,17,"Boots of Striding"),
            R(18,18,"Boots of the Elven Ranger"), R(19,19,"Bow +1/+2 versus X"), R(20,20,"Bow +1/+3 versus X"), R(21,21,"Bow +2"),
            R(22,22,"Bracers of Armor +2"), R(23,23,"Brooch of Arcane Armor"), R(24,24,"Candle of Contemplation (2d6)"),
            R(25,25,"Censer of Prophetic Dreams"), R(26,26,"Chime of Unlocking"), R(27,27,"Cloak of Protection +1"),
            R(28,28,"Cloak of the Elven Ranger"), R(29,29,"Crossbow +1, Dragon's Breath"), R(30,30,"Crucible of Conjuring Fire Elementals"),
            R(31,31,"Faerie Seed (2d4)"), R(32,32,"Gem of Conjuring Earth Elementals"), R(33,33,"Hammer of the Master Smith"),
            R(34,34,"Helm of Disalignment"), R(35,35,"Other Weapon +1/+2 versus X"), R(36,36,"Other Weapon +2"),
            R(37,37,"Potion of Indiscernibility"), R(38,38,"Potion of Necromantic Invulnerability"), R(39,39,"Potion of Speak with Plants"),
            R(40,40,"Potion of Telekinesis"), R(41,41,"Potion of Telepathy"), R(42,42,"Potion of Tongues"), R(43,43,"Potion of True Seeing"),
            R(44,44,"Potion of X-Ray Vision"), R(45,45,"Ring of Genie Summoning"), R(46,46,"Ring of Protection +1"),
            R(47,47,"Ring of Water Walking"), R(48,48,"Rope of Ascent"), R(49,49,"Shield +1, Burden"), R(50,51,"Shield +2"),
            R(52,52,"Spear +1/+2 versus X"), R(53,53,"Spear +1/+3 versus X"), R(54,54,"Spear +2"),
            R(55,58,"Spell Scroll (12 levels)"), R(59,61,"Spell Scroll (14 levels)"), R(62,63,"Spell Scroll (16 levels)"),
            R(64,65,"Spell Scroll (18 levels)"), R(66,67,"Spell Scroll (20 levels)"), R(68,68,"Spell Scroll (22 levels)"),
            R(69,69,"Spell Scroll (24 levels)"), R(70,70,"Staff of Smiting"), R(71,71,"Staff of the Healer"),
            R(72,76,"Sword +1, +2 versus X"), R(77,77,"Sword +1, +3 versus X"), R(78,78,"Sword +1, Bright"),
            R(79,79,"Sword +1, Deathless"), R(80,80,"Sword +1, Locating"), R(81,85,"Sword +2"), R(86,86,"Sword +2, Beguilement"),
            R(87,87,"Sword +2, Command"), R(88,88,"Sword +2, Valor"), R(89,89,"Thurible of Conjuring Air Elementals"),
            R(90,91,"Treasure Map (Treasure Type H)"), R(92,93,"Treasure Map (Treasure Type N)"), R(94,95,"Treasure Map (Treasure Type Q)"),
            R(96,96,"Treasure Map (Treasure Type Q, N)"), R(97,97,"Wand of Figments"), R(98,98,"Wand of Foe Discernment"),
            R(99,99,"Wand of Mage Missiles"), R(100,100,"Wand of Magic Discernment")
        };

        private static readonly RollRange<string>[] VeryRareItems =
        {
            R(1,2,"Ammunition +3 (2d10)"), R(3,3,"Amulet of Indiscernibility"), R(4,7,"Armor +3"), R(8,8,"Axe +3"),
            R(9,9,"Bag of Oblivion"), R(10,10,"Belt of Dwarvenkind"), R(11,11,"Belt of Giant Strength"), R(12,12,"Bludgeon +3"),
            R(13,13,"Boots of Levitating"), R(14,14,"Boots of the Prizefighter"), R(15,16,"Bottomless Bag"), R(17,17,"Bow +3"),
            R(18,18,"Bracers of Armor +3"), R(19,20,"Bracers of Armor +4"), R(21,21,"Cloak of Protection +2"),
            R(22,22,"Cloak of Protection +3"), R(23,24,"Cloak of Skinchanging"), R(25,25,"Collar of Conversation"),
            R(26,26,"Crystal Ball"), R(27,27,"Crystal Ball, Clairaudient"), R(28,28,"Crystal Ball, Telepathic"),
            R(29,29,"Cube of Cold Immunity"), R(30,30,"Dagger +1, Delver's"), R(31,31,"Dead Man's Hand"),
            R(32,32,"Dust of Revelation (3d10)"), R(33,33,"Dust of Vanishing (3d10)"), R(34,35,"Gauntlets of Ogre Strength"),
            R(36,36,"Gem of Force"), R(37,37,"Helm of Awe"), R(38,38,"Helm of Comprehension"), R(39,39,"Helm of Telepathy"),
            R(40,40,"Helm of Teleportation"), R(41,41,"Horn of the Eagles"), R(42,42,"Implement of the Night Sky"),
            R(43,43,"Javelin +3, Hurling"), R(44,45,"Magic Carpet"), R(46,46,"Marvelous Dwarven Mechanism"),
            R(47,47,"Necklace of Acclimatization"), R(48,48,"Other Weapon +3"), R(49,49,"Potion of Youth"),
            R(50,50,"Ring of Anti-Magic"), R(51,51,"Ring of Beast Control"), R(52,52,"Ring of Fire Protection"),
            R(53,53,"Ring of Humanoid Control"), R(54,55,"Ring of Invisibility"), R(56,56,"Ring of Plant Control"),
            R(57,57,"Ring of Protection +2"), R(58,58,"Ring of Protection +2, 5' radius"), R(59,59,"Ring of Protection +3"),
            R(60,60,"Ring of Protection +3, 5' radius"), R(61,61,"Ring of Telekinetic Force"), R(62,62,"Ring of X-Ray Vision"),
            R(63,63,"Rod of Eternal Torture"), R(64,65,"Rod of Nullification"), R(66,66,"Scarab of Life"), R(67,67,"Seismic Horn"),
            R(68,69,"Shield +3"), R(70,70,"Spear +2, Wolf-Fang"), R(71,71,"Spear +3"), R(72,74,"Spell Scroll (7th level ritual)"),
            R(75,76,"Spell Scroll (8th level ritual)"), R(77,78,"Spell Scrolls (1d3 7th level rituals)"), R(79,79,"Staff of Domination"),
            R(80,80,"Staff of Wilting"), R(81,81,"Sword +1, Flamebrand"), R(82,86,"Sword +3"),
            R(87,88,"Treasure Map (Treasure Type R)"), R(89,89,"Treasure Map (Treasure Type R, N)"), R(90,90,"Visor of the Eagle"),
            R(91,91,"Wand of Fear"), R(92,92,"Wand of Fireballs"), R(93,93,"Wand of Frost"), R(94,94,"Wand of Impetus"),
            R(95,95,"Wand of Item Negation"), R(96,96,"Wand of Metal Location"), R(97,97,"Wand of Paralysis"),
            R(98,98,"Wand of Thunderbolts"), R(99,99,"Wand of Transformation"), R(100,100,"Witch's Broom")
        };

        private static readonly RollRange<string>[] LegendaryItems =
        {
            R(1,1,"Armor of the First Vaultlord"), R(2,2,"Armor of the Invincible Conqueror"), R(3,3,"Boots of the Imperial Warmistress"),
            R(4,4,"Bow of the Great Eagles"), R(5,5,"Bow of the Unconquered Sun"), R(6,7,"Bracers of Armor +5"),
            R(8,9,"Bracers of Armor +6"), R(10,11,"Bracers of Armor +7"), R(12,12,"Bracers of the Imperial Warmistress"),
            R(13,13,"Chalice of Blood"), R(14,16,"Chariot of the Gods"), R(17,19,"Collapsible Boat"),
            R(20,20,"Corselet of the Imperial Warmistress"), R(21,21,"Diadem of the Imperial Warmistress"), R(22,23,"Drums of Terror"),
            R(24,24,"Emblem of the Eagle"), R(25,25,"Fire-eater Sword"), R(26,28,"Genie Bottle"),
            R(29,29,"Glaive of the Blade-Goddess"), R(30,30,"Great Axe of the North"), R(31,31,"Holy Talisman of the Winged Sun"),
            R(32,34,"Inexhaustible Flask"), R(35,35,"Iron Crown of the Sorcerer-Kings"), R(36,37,"Iron Mask of Cyfaraun"),
            R(38,38,"Iron-bound Book of Xisuthros"), R(39,39,"Mask of the Basilisk"), R(40,40,"Red Sword of the Warlord"),
            R(41,43,"Ring of Regeneration"), R(44,44,"Ring of the Queen's Heart"), R(45,46,"Ring of Wishes"),
            R(47,50,"Rod of Cataclysm"), R(51,53,"Rod of Resurrection"), R(54,54,"Scourge of Law"), R(55,55,"Seal of Chaos"),
            R(56,56,"Shield of the Empyrean Heavens"), R(57,57,"Shining Spear"), R(58,63,"Spell Scroll (9th level ritual)"),
            R(64,66,"Spell Scrolls (1d3 8th level rituals)"), R(67,68,"Spell Scrolls (1d3 9th level rituals)"),
            R(69,70,"Staff of Elemental Power (air)"), R(71,72,"Staff of Elemental Power (earth)"), R(73,74,"Staff of Elemental Power (fire)"),
            R(75,76,"Staff of Elemental Power (water)"), R(77,77,"Staff of the Archmage"), R(78,79,"Staff of the Mage"),
            R(80,80,"Sword of Fortune"), R(81,81,"Sword of Kings"), R(82,91,"Treasure Maps (1d4 maps totaling 4x Treasure Type R)"),
            R(92,96,"Treasure Maps (1d8 maps totaling 8x Treasure Type R)"), R(97,97,"Visor of the Vampire"), R(98,98,"Vorpal Sword"),
            R(99,100,"War Hammer +2, Dwarven Hurler")
        };
    }
}
