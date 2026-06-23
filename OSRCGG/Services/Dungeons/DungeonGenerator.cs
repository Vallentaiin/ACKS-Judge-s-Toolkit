using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed class DungeonGenerator
    {
        private sealed class PendingRoom
        {
            public int X { get; set; }
            public int Y { get; set; }
            public string ParentKey { get; set; }
        }

        private sealed class DungeonMonsterPick
        {
            public string Monster { get; set; }
            public string CountExpression { get; set; }
            public int Count { get; set; }
            public int XpEach { get; set; }
            public int TotalXp { get; set; }
            public string TreasureType { get; set; }
            public int LairPercent { get; set; }
            public bool IsLair { get; set; }
        }

        private sealed class DungeonMonsterStats
        {
            public int XpEach { get; private set; }
            public string TreasureType { get; private set; }
            public int LairPercent { get; private set; }

            public DungeonMonsterStats(int xpEach, string treasureType, int lairPercent)
            {
                XpEach = xpEach;
                TreasureType = treasureType ?? "";
                LairPercent = lairPercent;
            }
        }

        private sealed class DungeonTrapPick
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string RussianName { get; set; }
            public int Level { get; set; }
            public string Trigger { get; set; }
            public string RussianTrigger { get; set; }
            public string Effect { get; set; }
            public string RussianEffect { get; set; }
        }

        private sealed class DungeonTrapDefinition
        {
            public string Key { get; private set; }
            public string Name { get; private set; }
            public string RussianName { get; private set; }
            public string[] TriggerKeys { get; private set; }
            public string[] Effects { get; private set; }
            public string[] RussianEffects { get; private set; }

            public DungeonTrapDefinition(string key, string name, string russianName, string[] triggerKeys, string[] effects, string[] russianEffects)
            {
                Key = key ?? "";
                Name = name ?? "";
                RussianName = russianName ?? "";
                TriggerKeys = triggerKeys ?? new string[0];
                Effects = effects ?? new string[0];
                RussianEffects = russianEffects ?? new string[0];
            }
        }

        private static readonly DungeonMonsterPick[][] MonsterLevelTables =
        {
            MonsterRows(new[,] { { "Goblin", "2d4" }, { "Kobold", "4d4" }, { "Morlock", "1d12" }, { "Orc", "2d4" }, { "Beetle, Luminous", "1d8" }, { "Centipede, Giant", "2d4" }, { "Ferret, Giant", "1d8" }, { "Rat, Giant", "3d6" }, { "Men, Brigand", "2d4" }, { "Strige", "1d10" }, { "Skeleton", "3d4" }, { "NPC Party (Lvl 1)", "1d4+2" } }),
            MonsterRows(new[,] { { "Beastman, Gnoll", "1d6" }, { "Beastman, Hobgoblin", "1d6" }, { "Lizardman", "2d4" }, { "Troglodyte", "1d8" }, { "Fly, Giant Carnivorous", "1d8" }, { "Locust, Cavern", "1d10" }, { "Bat, Giant", "1d10" }, { "Snake, Viper", "1d8" }, { "Attercop, Foul", "1d4" }, { "Ghoul, Grave", "1d6" }, { "Zombie", "2d4" }, { "NPC Party (Lvl 2)", "1d4+2" } }),
            MonsterRows(new[,] { { "Bugbear", "2d4" }, { "Lycanthrope, Werewolf", "1d6" }, { "Ogre", "1d6" }, { "Hobgholl", "1d6" }, { "Ant, Giant", "2d4" }, { "Scorpion, Giant", "1d6" }, { "Wolf, Dire", "1d4" }, { "Carrion Horror", "1d3" }, { "Attercop, Hideous", "1d3" }, { "Spriggan", "2d4" }, { "Wight", "1d8" }, { "NPC Party (Lvl 4)", "1d4+2" } }),
            MonsterRows(new[,] { { "Lycanthrope, Wereboar", "1d4" }, { "Lycanthrope, Weretiger", "1d4" }, { "Minotaur", "1d6" }, { "Medusa", "1d3" }, { "Acanthaspis, Giant", "1d4" }, { "Bear, Cave", "1" }, { "Spider, Giant Tarantula", "1d3" }, { "Snake, Python", "1d3" }, { "Attercop, Monstrous", "1d3" }, { "Gargoyle", "1d6" }, { "Mummy", "1d4" }, { "NPC Party (Lvl 5)", "1d4+2" } }),
            MonsterRows(new[,] { { "Ettin", "1d2" }, { "Giant, Hill", "1d4" }, { "Giant, Stone", "1d2" }, { "Troll", "1d8" }, { "Amphisbaena", "1" }, { "Wasp, Giant Parasitic", "1" }, { "Worm, Giant Grey", "1d3" }, { "Ooze, Ochre", "1" }, { "Arane", "1" }, { "Basilisk, Petrifying", "1d6" }, { "Necropede", "1d2" }, { "NPC Party (Lvl 8)", "1d4+3" } }),
            MonsterRows(new[,] { { "Cyclops", "1" }, { "Giant, Fire or Frost", "1d2" }, { "Hag", "1d2" }, { "Titan, Lesser", "1d2" }, { "Blob, Black", "1" }, { "Crocodile, Giant", "1d3" }, { "Skittering Maw", "1" }, { "Worm, Giant Black", "1d2" }, { "Attercop, Demonic", "1" }, { "Gorgon", "1d2" }, { "Mummy Lord", "1" }, { "NPC Party (Lvl 11)", "1d4+3" } })
        };

        private static readonly string[,] UnprotectedTreasureTypes =
        {
            { "A", "B", "C", "D", "E", "F" },
            { "C", "D", "E", "F", "G", "H" },
            { "E", "F", "G", "H", "I", "J" },
            { "G", "H", "I", "J", "K", "L" },
            { "I", "J", "K", "L", "M", "N" },
            { "M", "N", "O", "P", "Q", "R" }
        };

        private static readonly Dictionary<string, DungeonMonsterStats> MonsterStats =
            new Dictionary<string, DungeonMonsterStats>(StringComparer.OrdinalIgnoreCase)
            {
                { "Acanthaspis, Giant", Stats(500, "K", 15) },
                { "Amphisbaena", Stats(820, "I", 10) },
                { "Ant, Giant", Stats(80, "N", 10) },
                { "Arane", Stats(1070, "J", 70) },
                { "Attercop, Demonic", Stats(3650, "N, H x2", 70) },
                { "Attercop, Foul", Stats(38, "B / 2", 70) },
                { "Attercop, Hideous", Stats(80, "B", 70) },
                { "Attercop, Monstrous", Stats(190, "D", 70) },
                { "Bat, Giant", Stats(29, "none", 35) },
                { "Bear, Cave", Stats(320, "none", 25) },
                { "Beastman, Gnoll", Stats(20, "G", 20) },
                { "Beastman, Hobgoblin", Stats(15, "E", 25) },
                { "Beetle, Luminous", Stats(15, "A x2", 40) },
                { "Blob, Black", Stats(2250, "none", 0) },
                { "Bugbear", Stats(65, "L", 25) },
                { "Carrion Horror", Stats(170, "C", 25) },
                { "Centipede, Giant", Stats(6, "A / 2", 10) },
                { "Crocodile, Giant", Stats(1800, "none", 0) },
                { "Cyclops", Stats(1400, "O", 20) },
                { "Ettin", Stats(850, "N, H", 20) },
                { "Ferret, Giant", Stats(15, "A", 25) },
                { "Fly, Giant Carnivorous", Stats(29, "C", 35) },
                { "Gargoyle", Stats(190, "I", 20) },
                { "Ghoul, Grave", Stats(38, "E", 20) },
                { "Giant, Fire or Frost", Stats(1000, "O", 35) },
                { "Giant, Hill", Stats(600, "N", 25) },
                { "Giant, Stone", Stats(700, "N", 25) },
                { "Gorgon", Stats(3900, "R", 40) },
                { "Hag", Stats(2500, "R", 70) },
                { "Hobgholl", Stats(80, "G", 35) },
                { "Kobold", Stats(5, "E", 40) },
                { "Lizardman", Stats(35, "J", 30) },
                { "Locust, Cavern", Stats(29, "none", 30) },
                { "Lycanthrope, Wereboar", Stats(215, "J", 20) },
                { "Lycanthrope, Weretiger", Stats(350, "J", 15) },
                { "Lycanthrope, Werewolf", Stats(135, "J", 25) },
                { "Medusa", Stats(245, "H", 50) },
                { "Men, Brigand", Stats(20, "G", 20) },
                { "Minotaur", Stats(320, "L", 20) },
                { "Morlock", Stats(10, "E", 35) },
                { "Mummy", Stats(660, "N x2", 20) },
                { "Mummy Lord", Stats(4200, "Q", 50) },
                { "Necropede", Stats(820, "M", 35) },
                { "Ogre", Stats(140, "J", 20) },
                { "Ooze, Ochre", Stats(500, "none", 0) },
                { "Orc", Stats(10, "G", 35) },
                { "Rat, Giant", Stats(5, "A", 10) },
                { "Scorpion, Giant", Stats(135, "F", 50) },
                { "Skeleton", Stats(13, "C", 35) },
                { "Skittering Maw", Stats(1600, "P", 10) },
                { "Snake, Python", Stats(350, "none", 10) },
                { "Snake, Viper", Stats(29, "none", 25) },
                { "Spider, Giant Tarantula", Stats(190, "F", 50) },
                { "Spriggan", Stats(65, "H", 25) },
                { "Strige", Stats(6, "C", 40) },
                { "Titan, Lesser", Stats(1200, "N, H", 40) },
                { "Troglodyte", Stats(20, "G", 15) },
                { "Troll", Stats(680, "O", 40) },
                { "Wasp, Giant Parasitic", Stats(660, "M", 20) },
                { "Wight", Stats(80, "H", 70) },
                { "Wolf, Dire", Stats(140, "none", 10) },
                { "Worm, Giant Black", Stats(4200, "P x2", 25) },
                { "Worm, Giant Grey", Stats(570, "K", 25) },
                { "Zombie", Stats(29, "F", 35) },
                { "NPC Party (Lvl 1)", Stats(20, "", 0) },
                { "NPC Party (Lvl 2)", Stats(50, "", 0) },
                { "NPC Party (Lvl 4)", Stats(190, "", 0) },
                { "NPC Party (Lvl 5)", Stats(350, "", 0) },
                { "NPC Party (Lvl 8)", Stats(1200, "", 0) },
                { "NPC Party (Lvl 11)", Stats(4200, "", 0) }
            };

        private static readonly string[] EnglishDungeonNamePrefixes = { "Black", "Grey", "Hollow", "Old", "Broken", "Silent", "Ashen", "Red" };
        private static readonly string[] RussianDungeonNamePrefixes = { "Черного", "Серого", "Пустого", "Старого", "Расколотого", "Тихого", "Пепельного", "Красного" };
        private static readonly DungeonTrapDefinition[] TrapDefinitions =
        {
            Trap(
                "CeilingCollapse",
                "Ceiling collapse trap",
                "Обрушение потолка",
                new[] { "PressurePlate", "Counterweights", "LeverPulley" },
                new[]
                {
                    "10' radius, Blast save; falling stones deal 1d6 bludgeoning damage, half on save.",
                    "10' radius, Blast save; falling stones deal 3d6 bludgeoning damage, half on save.",
                    "10' radius, Blast save; falling stones deal 5d6 bludgeoning damage, half on save.",
                    "10' radius, Blast save; falling stones deal 7d6 bludgeoning damage, half on save.",
                    "10' radius, Blast save; falling stones deal 9d6 bludgeoning damage and knock failed saves prone, half damage on save.",
                    "10' radius, Blast save; falling stones deal 11d6+11 bludgeoning damage and knock failed saves prone, half damage on save."
                },
                new[]
                {
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 1d6 дробящего урона, при успехе половина.",
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 3d6 дробящего урона, при успехе половина.",
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 5d6 дробящего урона, при успехе половина.",
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 7d6 дробящего урона, при успехе половина.",
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 9d6 дробящего урона и сбивают с ног при провале, при успехе половина урона.",
                    "радиус 10 футов, спасбросок Blast; падающие камни наносят 11d6+11 дробящего урона и сбивают с ног при провале, при успехе половина урона."
                }),
            Trap(
                "Deadfall",
                "Deadfall trap",
                "Падающий груз",
                new[] { "Tripwire", "Counterweights", "WeightSensitive" },
                new[]
                {
                    "5' diameter, Blast save; deadfall deals 1d12 bludgeoning damage, avoided on save.",
                    "10' diameter, Blast save; deadfall deals 3d12 bludgeoning damage, avoided on save.",
                    "10' diameter, Blast save; deadfall deals 5d12 bludgeoning damage, avoided on save.",
                    "10' diameter, Blast save; deadfall deals 7d12 bludgeoning damage and knocks failed saves prone.",
                    "10' diameter, Blast save; deadfall deals 9d12 bludgeoning damage and knocks failed saves prone.",
                    "10' line, Blast save; deadfall instantly kills creatures that fail the save."
                },
                new[]
                {
                    "диаметр 5 футов, спасбросок Blast; груз наносит 1d12 дробящего урона, при успехе уклонение.",
                    "диаметр 10 футов, спасбросок Blast; груз наносит 3d12 дробящего урона, при успехе уклонение.",
                    "диаметр 10 футов, спасбросок Blast; груз наносит 5d12 дробящего урона, при успехе уклонение.",
                    "диаметр 10 футов, спасбросок Blast; груз наносит 7d12 дробящего урона и сбивает с ног при провале.",
                    "диаметр 10 футов, спасбросок Blast; груз наносит 9d12 дробящего урона и сбивает с ног при провале.",
                    "линия 10 футов, спасбросок Blast; груз мгновенно убивает существ при провале."
                }),
            Trap(
                "ExcavatedEarthPit",
                "Excavated earth pit trap",
                "Земляная яма-ловушка",
                new[] { "PivotingFloor", "PressurePlate", "Tripwire" },
                PitEffects(false),
                PitEffects(true)),
            Trap(
                "Fire",
                "Fire trap",
                "Огненная ловушка",
                new[] { "PressurePlate", "Proximity", "Runes" },
                new[]
                {
                    "10' diameter, Blast save; burning oil deals 1d8 fire damage now and 1d8 next round, or 1d4 on save.",
                    "40' x 20' cone, Blast save; naphtha deals 2d4 fire damage, half on save.",
                    "20' diameter sphere, Blast save; flammable gas deals 5d6 fire damage, half on save.",
                    "20' diameter sphere, Blast save; flammable gas deals 7d6+7 fire damage, half on save.",
                    "20' diameter sphere, Blast save; sticky gas deals 9d6 fire damage and starts failed saves burning.",
                    "5' diameter sphere, Death save; magma disintegrates the creature on a failed save."
                },
                new[]
                {
                    "диаметр 10 футов, спасбросок Blast; горящее масло наносит 1d8 огненного урона сразу и 1d8 в следующий раунд, либо 1d4 при успехе.",
                    "конус 40 x 20 футов, спасбросок Blast; нафта наносит 2d4 огненного урона, при успехе половина.",
                    "сфера 20 футов, спасбросок Blast; горючий газ наносит 5d6 огненного урона, при успехе половина.",
                    "сфера 20 футов, спасбросок Blast; горючий газ наносит 7d6+7 огненного урона, при успехе половина.",
                    "сфера 20 футов, спасбросок Blast; липкий газ наносит 9d6 огненного урона и поджигает проваливших спасбросок.",
                    "сфера 5 футов, спасбросок Death; магма дезинтегрирует существо при провале."
                }),
            Trap(
                "Missile",
                "Missile trap",
                "Стрелковая ловушка",
                new[] { "Tripwire", "GearSystem", "PressurePlate" },
                new[]
                {
                    "one crossbow bolt attacks as a 1st level fighter for 1d6+1 piercing damage.",
                    "three crossbow bolts each attack as a 3rd level fighter for 1d6+1 piercing damage.",
                    "one ballista bolt attacks as a 5th level fighter for 3d6 piercing damage; line splash uses Blast save for half.",
                    "one poisoned crossbow bolt attacks as a 7th level fighter for 1d6+1 piercing damage; damaged target must Death save or die in 1 turn.",
                    "nine arbalest bolts each attack as a 9th level fighter for 1d10 piercing damage.",
                    "three ballista bolts each attack as an 11th level fighter, using the 3rd level ballista effect."
                },
                new[]
                {
                    "один арбалетный болт атакует как воин 1-го уровня и наносит 1d6+1 колющего урона.",
                    "три арбалетных болта атакуют как воин 3-го уровня и наносят по 1d6+1 колющего урона.",
                    "один болт баллисты атакует как воин 5-го уровня и наносит 3d6 колющего урона; линия поражения дает спасбросок Blast на половину.",
                    "один отравленный арбалетный болт атакует как воин 7-го уровня и наносит 1d6+1 колющего урона; раненая цель делает Death save или умирает через 1 ход.",
                    "девять тяжелых арбалетных болтов атакуют как воин 9-го уровня и наносят по 1d10 колющего урона.",
                    "три болта баллисты атакуют как воин 11-го уровня, используя эффект баллисты 3-го уровня."
                }),
            Trap(
                "Needle",
                "Needle trap",
                "Игла-ловушка",
                new[] { "HiddenButton", "GearSystem", "PressurePlate" },
                new[]
                {
                    "needle attacks the triggerer as a 1st level fighter for 1 piercing damage; cobra venom, Death save +2 or 1d6 poison damage after 1 turn.",
                    "needle attacks as a 3rd level fighter for 1 piercing damage; wyvern venom, Death save +1 or 7d6 poison damage after 1 round.",
                    "needle attacks as a 5th level fighter for 1 piercing damage; wyvern venom, Death save +1 or 7d6 poison damage after 1 round.",
                    "needle attacks as a 7th level fighter for 1 piercing damage; black worm venom, Death save or die instantly.",
                    "two needles attack as 9th level fighters for 1 piercing damage each; dragon blood venom, Death save or die, -4 if both needles damage.",
                    "black hepatizon needle attacks as an 11th level fighter for 1 piercing damage; cursed poison drains 1d8 maximum hp per failed Death save each round for 10 rounds."
                },
                new[]
                {
                    "игла атакует сработавшего как воин 1-го уровня и наносит 1 колющего урона; яд кобры, Death save +2 или 1d6 урона ядом через 1 ход.",
                    "игла атакует как воин 3-го уровня и наносит 1 колющего урона; яд виверны, Death save +1 или 7d6 урона ядом через 1 раунд.",
                    "игла атакует как воин 5-го уровня и наносит 1 колющего урона; яд виверны, Death save +1 или 7d6 урона ядом через 1 раунд.",
                    "игла атакует как воин 7-го уровня и наносит 1 колющего урона; яд черного червя, Death save или мгновенная смерть.",
                    "две иглы атакуют как воины 9-го уровня и наносят по 1 колющего урона; кровь дракона, Death save или смерть, -4 если ранили обе иглы.",
                    "игла из черного гепатизона атакует как воин 11-го уровня и наносит 1 колющего урона; проклятый яд 10 раундов отнимает 1d8 максимальных hp за каждый проваленный Death save."
                }),
            Trap(
                "Portcullis",
                "Portcullis trap",
                "Падающая решетка",
                new[] { "Tripwire", "Counterweights", "LeverPulley" },
                new[]
                {
                    "triggering creature makes a Blast save; on failure it also suffers 1d6 piercing damage.",
                    "triggering creature makes a Blast save; on failure it also suffers 3d6 piercing damage.",
                    "triggering creature makes a Blast save; on failure it suffers 5d6 piercing damage, is knocked prone, and gets stuck.",
                    "Blast save; failure deals 7d6 piercing damage, prone, stuck; success deals half damage and prone.",
                    "Blast save; failure deals 9d6 piercing damage, prone, stuck; success deals half damage and prone.",
                    "Blast save; failure deals 11d6 piercing damage and a Piercing Mortal Wounds permanent wound."
                },
                new[]
                {
                    "сработавшее существо делает спасбросок Blast; при провале оно также получает 1d6 колющего урона.",
                    "сработавшее существо делает спасбросок Blast; при провале оно также получает 3d6 колющего урона.",
                    "сработавшее существо делает спасбросок Blast; при провале получает 5d6 колющего урона, падает и застревает.",
                    "спасбросок Blast; провал наносит 7d6 колющего урона, сбивает и застревает; успех наносит половину и сбивает.",
                    "спасбросок Blast; провал наносит 9d6 колющего урона, сбивает и застревает; успех наносит половину и сбивает.",
                    "спасбросок Blast; провал наносит 11d6 колющего урона и постоянную рану по таблице Piercing Mortal Wounds."
                }),
            Trap(
                "RockCutPit",
                "Rock-cut pit trap",
                "Каменная яма-ловушка",
                new[] { "PivotingFloor", "PressurePlate", "WeightSensitive" },
                PitEffects(false),
                PitEffects(true)),
            Trap(
                "RollingRock",
                "Rolling rock trap",
                "Катящийся валун",
                new[] { "Tripwire", "Counterweights", "LeverPulley" },
                new[]
                {
                    "5' wide boulder, 30' path, Blast save; 1d6 bludgeoning damage and prone on failed save.",
                    "10' wide boulder, 30' path, Blast save; 2d6 bludgeoning damage and prone on failed save.",
                    "10' wide boulder, 45' path, Blast save; 2d6 bludgeoning damage and prone on failed save.",
                    "10' wide boulder, 45' path, Blast save; 4d6 bludgeoning damage and prone on failed save.",
                    "10' wide boulder, 45' path, Blast save; 6d6 bludgeoning damage and prone on failed save.",
                    "10' wide boulder, 45' path, Blast save; 8d6 bludgeoning damage and prone on failed save."
                },
                new[]
                {
                    "валун шириной 5 футов, путь 30 футов, спасбросок Blast; 1d6 дробящего урона и падение при провале.",
                    "валун шириной 10 футов, путь 30 футов, спасбросок Blast; 2d6 дробящего урона и падение при провале.",
                    "валун шириной 10 футов, путь 45 футов, спасбросок Blast; 2d6 дробящего урона и падение при провале.",
                    "валун шириной 10 футов, путь 45 футов, спасбросок Blast; 4d6 дробящего урона и падение при провале.",
                    "валун шириной 10 футов, путь 45 футов, спасбросок Blast; 6d6 дробящего урона и падение при провале.",
                    "валун шириной 10 футов, путь 45 футов, спасбросок Blast; 8d6 дробящего урона и падение при провале."
                }),
            Trap(
                "ScythingBlade",
                "Scything blade trap",
                "Серп-лезвие",
                new[] { "Tripwire", "GearSystem", "PressurePlate" },
                new[]
                {
                    "10' line, Blast save; affected creatures suffer 1d8 slashing damage.",
                    "10' line, Blast save; affected creatures suffer 3d8 slashing damage.",
                    "10' line, Blast save; affected creatures suffer 5d8 slashing damage.",
                    "10' line, Blast save; 5d8 slashing damage, then a second Blast save or 5d8 more on the return swing.",
                    "10' line, Blast save; 7d8 slashing damage, then a second Blast save or 7d8 more on the return swing.",
                    "10' line, Blast save; 7d8 slashing damage and a Slashing Mortal Wounds permanent wound on failed save."
                },
                new[]
                {
                    "линия 10 футов, спасбросок Blast; задетые существа получают 1d8 рубящего урона.",
                    "линия 10 футов, спасбросок Blast; задетые существа получают 3d8 рубящего урона.",
                    "линия 10 футов, спасбросок Blast; задетые существа получают 5d8 рубящего урона.",
                    "линия 10 футов, спасбросок Blast; 5d8 рубящего урона, затем второй Blast save или еще 5d8 на обратном взмахе.",
                    "линия 10 футов, спасбросок Blast; 7d8 рубящего урона, затем второй Blast save или еще 7d8 на обратном взмахе.",
                    "линия 10 футов, спасбросок Blast; 7d8 рубящего урона и постоянная рана по таблице Slashing Mortal Wounds при провале."
                }),
            Trap(
                "SpringSnare",
                "Spring snare trap",
                "Пружинная петля",
                new[] { "Tripwire", "WeightSensitive", "HiddenButton" },
                new[]
                {
                    "5' diameter, Paralysis save; 1d6 bludgeoning damage, hoisted 10' up, restrained until escape.",
                    "10' diameter, Paralysis save; 2d6 bludgeoning damage, hoisted 20' up, restrained until escape.",
                    "20' diameter, Paralysis save; 3d6 bludgeoning damage, hoisted 30' up, restrained until escape.",
                    "20' diameter, Paralysis save; 4d6 bludgeoning damage, hoisted 40' up, restrained until escape.",
                    "20' diameter, Paralysis save; 5d6 bludgeoning damage, hoisted 50' up, restrained until escape.",
                    "25' diameter, Paralysis save; 5d6 bludgeoning damage, hoisted 50' up, restrained until escape."
                },
                new[]
                {
                    "диаметр 5 футов, спасбросок Paralysis; 1d6 дробящего урона, подъем на 10 футов, цель удержана до освобождения.",
                    "диаметр 10 футов, спасбросок Paralysis; 2d6 дробящего урона, подъем на 20 футов, цель удержана до освобождения.",
                    "диаметр 20 футов, спасбросок Paralysis; 3d6 дробящего урона, подъем на 30 футов, цель удержана до освобождения.",
                    "диаметр 20 футов, спасбросок Paralysis; 4d6 дробящего урона, подъем на 40 футов, цель удержана до освобождения.",
                    "диаметр 20 футов, спасбросок Paralysis; 5d6 дробящего урона, подъем на 50 футов, цель удержана до освобождения.",
                    "диаметр 25 футов, спасбросок Paralysis; 5d6 дробящего урона, подъем на 50 футов, цель удержана до освобождения."
                }),
            Trap(
                "SwingingLog",
                "Swinging log trap",
                "Качающийся бревенчатый таран",
                new[] { "Tripwire", "Counterweights", "LeverPulley" },
                new[]
                {
                    "10' line, Blast save; affected creatures suffer 1d8 bludgeoning damage.",
                    "10' line, Blast save; affected creatures suffer 3d8 bludgeoning damage and are knocked prone.",
                    "10' line, Blast save; affected creatures suffer 5d8 bludgeoning damage and are knocked prone.",
                    "10' line, Blast save; 5d8 bludgeoning damage, prone, forced back 1' per damage; wall impact adds 1d6 per full 10'.",
                    "10' line, Blast save; 7d8 bludgeoning damage, prone, forced back 1' per damage; wall impact adds 1d6 per full 10'.",
                    "10' line, Blast save; 9d8 bludgeoning damage, prone, forced back 1' per damage, wall impact, and stunned until next initiative end."
                },
                new[]
                {
                    "линия 10 футов, спасбросок Blast; задетые существа получают 1d8 дробящего урона.",
                    "линия 10 футов, спасбросок Blast; задетые существа получают 3d8 дробящего урона и падают.",
                    "линия 10 футов, спасбросок Blast; задетые существа получают 5d8 дробящего урона и падают.",
                    "линия 10 футов, спасбросок Blast; 5d8 дробящего урона, падение, отталкивание на 1 фут за единицу урона; удар о стену добавляет 1d6 за каждые полные 10 футов.",
                    "линия 10 футов, спасбросок Blast; 7d8 дробящего урона, падение, отталкивание на 1 фут за единицу урона; удар о стену добавляет 1d6 за каждые полные 10 футов.",
                    "линия 10 футов, спасбросок Blast; 9d8 дробящего урона, падение, отталкивание, удар о стену и оглушение до конца следующей инициативы."
                }),
            Trap(
                "WhippingBranch",
                "Whipping branch trap",
                "Хлещущая ветвь",
                new[] { "Tripwire", "LeverPulley", "PressurePlate" },
                new[]
                {
                    "branch attacks the triggerer as a 1st level fighter for 1d6+1 piercing damage.",
                    "branch attacks as a 3rd level fighter for 3d6+3 piercing damage.",
                    "branch attacks as a 5th level fighter for 5d6+5 piercing damage.",
                    "branch attacks as a 7th level fighter for 5d6+5 piercing damage; rockfish venom, Death save +1 or 4d6 poison damage after 1 turn.",
                    "branch attacks as a 9th level fighter for 5d6+5 piercing damage; wyvern venom, Death save +1 or 6d6 poison damage after 1 turn.",
                    "branch attacks as a 9th level fighter for 5d6+5 piercing damage; dragon blood poison, Death save or die instantly."
                },
                new[]
                {
                    "ветвь атакует сработавшего как воин 1-го уровня и наносит 1d6+1 колющего урона.",
                    "ветвь атакует как воин 3-го уровня и наносит 3d6+3 колющего урона.",
                    "ветвь атакует как воин 5-го уровня и наносит 5d6+5 колющего урона.",
                    "ветвь атакует как воин 7-го уровня и наносит 5d6+5 колющего урона; яд рыбы-камня, Death save +1 или 4d6 урона ядом через 1 ход.",
                    "ветвь атакует как воин 9-го уровня и наносит 5d6+5 колющего урона; яд виверны, Death save +1 или 6d6 урона ядом через 1 ход.",
                    "ветвь атакует как воин 9-го уровня и наносит 5d6+5 колющего урона; кровь дракона, Death save или мгновенная смерть."
                })
        };
        private static readonly string[] EnglishUniqueFeatures = { "sealed idol", "flooded passage", "ancient mural", "talking door", "bottomless shaft", "strange machine", "cursed altar", "fungal garden" };
        private static readonly string[] RussianUniqueFeatures = { "запечатанный идол", "затопленный проход", "древняя фреска", "говорящая дверь", "бездонная шахта", "странная машина", "проклятый алтарь", "грибной сад" };

        private static DungeonMonsterStats Stats(int xpEach, string treasureType, int lairPercent)
        {
            return new DungeonMonsterStats(xpEach, treasureType, lairPercent);
        }

        private static DungeonTrapDefinition Trap(string key, string name, string russianName, string[] triggerKeys, string[] effects, string[] russianEffects)
        {
            return new DungeonTrapDefinition(key, name, russianName, triggerKeys, effects, russianEffects);
        }

        private static string[] PitEffects(bool russian)
        {
            return russian
                ? new[]
                {
                    "яма глубиной 10 футов; падение наносит 1d6 дробящего урона.",
                    "яма глубиной 20 футов; падение наносит 2d6 дробящего урона.",
                    "яма глубиной 10 футов с шипами; падение 1d6 дробящего урона плюс 1d4 шипов по 1d6 колющего урона.",
                    "яма глубиной 20 футов с шипами; падение 2d6 дробящего урона плюс 1d4 шипов по 1d6 колющего урона.",
                    "яма глубиной 30 футов с шипами; падение 3d6 дробящего урона плюс 1d4 шипов по 1d6 колющего урона.",
                    "яма глубиной 40 футов с шипами; падение 4d6 дробящего урона плюс 1d4 шипов по 1d6 колющего урона."
                }
                : new[]
                {
                    "10' deep pit; falling deals 1d6 bludgeoning damage.",
                    "20' deep pit; falling deals 2d6 bludgeoning damage.",
                    "10' deep spiked pit; falling deals 1d6 bludgeoning damage plus 1d4 spikes for 1d6 piercing damage each.",
                    "20' deep spiked pit; falling deals 2d6 bludgeoning damage plus 1d4 spikes for 1d6 piercing damage each.",
                    "30' deep spiked pit; falling deals 3d6 bludgeoning damage plus 1d4 spikes for 1d6 piercing damage each.",
                    "40' deep spiked pit; falling deals 4d6 bludgeoning damage plus 1d4 spikes for 1d6 piercing damage each."
                };
        }

        public DungeonRecord Generate(DungeonGenerationOptions options)
        {
            options = options ?? new DungeonGenerationOptions();
            Random random = string.IsNullOrWhiteSpace(options.Seed)
                ? new Random()
                : new Random(StableSeed(BuildStableSeedKey(options)));
            return Generate(options, random);
        }

        public DungeonRecord Generate(DungeonGenerationOptions options, Random random)
        {
            options = options ?? new DungeonGenerationOptions();
            random = random ?? new Random();
            string size = NormalizeSize(options.Size);
            int level = DungeonCatalog.ClampDungeonLevel(options.RecommendedLevel);
            string dungeonType = string.IsNullOrWhiteSpace(options.DungeonType)
                ? DungeonCatalog.DungeonTypes[random.Next(DungeonCatalog.DungeonTypes.Count)].Name
                : DungeonCatalog.NormalizeDungeonType(options.DungeonType);
            int levelCount = PickLevelCount(size, random);
            int roomCount = PickRoomCount(size, levelCount, random);

            // Генератор не знает ничего о карте: он получает только тип, размер,
            // уровень и seed. Это позволяет использовать один и тот же код во вкладке
            // данжей и при автогенерации особенностей гекса.
            DungeonRecord dungeon = new DungeonRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(options.Name) ? BuildDungeonName(dungeonType, options.RussianOutput, random) : options.Name.Trim(),
                DungeonType = dungeonType,
                Size = size,
                RecommendedLevel = level,
                ChallengeTier = level <= 3 ? "Low" : level <= 6 ? "Mid" : "High",
                Notes = BuildDungeonNotes(dungeonType, size, options.RussianOutput),
                UpdatedAt = DateTime.Now
            };

            int remainingRooms = roomCount;
            DungeonRoomRecord previousStairs = null;
            for (int levelIndex = 1; levelIndex <= levelCount; levelIndex++)
            {
                int roomsHere = levelIndex == levelCount
                    ? remainingRooms
                    : Math.Max(2, roomCount / levelCount + random.Next(-2, 3));
                remainingRooms = Math.Max(0, remainingRooms - roomsHere);

                DungeonLevelRecord dungeonLevel = BuildLevel(levelIndex, roomsHere, level, dungeonType, size, random, options.RussianOutput);
                DungeonRoomRecord stairRoom = dungeonLevel.Rooms.OrderByDescending(r => r.X + r.Y).FirstOrDefault();
                if (stairRoom != null && levelIndex < levelCount)
                {
                    stairRoom.Kind = "StairsDown";
                    stairRoom.Title = options.RussianOutput ? "Лестница вниз" : "Stairs down";
                    stairRoom.Details = options.RussianOutput ? "Переход на следующий уровень подземелья." : "Transition to the next dungeon level.";
                }

                if (previousStairs != null && dungeonLevel.Rooms.Count > 0)
                {
                    DungeonRoomRecord upRoom = dungeonLevel.Rooms.OrderBy(r => r.X + r.Y).First();
                    upRoom.Kind = "StairsUp";
                    upRoom.Title = options.RussianOutput ? "Лестница вверх" : "Stairs up";
                    upRoom.Details = options.RussianOutput ? "Переход на предыдущий уровень подземелья." : "Transition to the previous dungeon level.";
                    dungeonLevel.Connections.Add(new DungeonConnectionRecord
                    {
                        FromRoomId = previousStairs.Id,
                        ToRoomId = upRoom.Id,
                        Kind = "Stairs",
                        PassageWidth = 1,
                        DoorKind = ""
                    });
                }

                previousStairs = stairRoom;
                dungeon.Levels.Add(dungeonLevel);
            }

            EnsureSingleRoomLairIsOccupied(dungeon, level, size, dungeonType, random, options.RussianOutput);
            dungeon.WanderingEncounters = BuildEncounterTables(dungeon, level, dungeonType, random, options.RussianOutput);
            return dungeon;
        }

        private DungeonLevelRecord BuildLevel(int levelNumber, int roomCount, int recommendedLevel, string dungeonType, string size, Random random, bool russian)
        {
            bool isLair = string.Equals(size, "Lair", StringComparison.OrdinalIgnoreCase);
            const int spacingX = 6;
            const int spacingY = 5;
            DungeonLevelRecord level = new DungeonLevelRecord
            {
                LevelNumber = levelNumber,
                Width = Math.Max(18, (int)Math.Ceiling(Math.Sqrt(roomCount) * spacingX) + 8),
                Height = Math.Max(14, (int)Math.Ceiling(Math.Sqrt(roomCount) * spacingY) + 8),
                Rooms = new List<DungeonRoomRecord>(),
                Connections = new List<DungeonConnectionRecord>(),
                Doors = new List<DungeonDoorRecord>()
            };

            Dictionary<string, PendingRoom> pending = new Dictionary<string, PendingRoom>();
            List<PendingRoom> pendingRooms = new List<PendingRoom>();
            PendingRoom start = new PendingRoom { X = level.Width / 2, Y = level.Height / 2, ParentKey = "" };
            pending[RoomKey(start.X, start.Y)] = start;
            pendingRooms.Add(start);

            // Сетка строится как связный граф комнат: сначала дерево от центральной
            // комнаты, затем редкие петли, чтобы карта не была линейной кишкой.
            int attempts = 0;
            while (pending.Count < roomCount && attempts < roomCount * 80)
            {
                attempts++;
                PendingRoom source = pendingRooms[random.Next(pendingRooms.Count)];
                int dir = random.Next(4);
                int x = source.X + (dir == 0 ? spacingX : dir == 1 ? -spacingX : 0);
                int y = source.Y + (dir == 2 ? spacingY : dir == 3 ? -spacingY : 0);
                x = Math.Max(1, Math.Min(level.Width - 5, x));
                y = Math.Max(1, Math.Min(level.Height - 4, y));
                string key = RoomKey(x, y);
                if (pending.ContainsKey(key)) continue;
                if (pendingRooms.Any(other => Math.Abs(other.X - x) < 3 && Math.Abs(other.Y - y) < 3)) continue;

                PendingRoom created = new PendingRoom { X = x, Y = y, ParentKey = RoomKey(source.X, source.Y) };
                pending[key] = created;
                pendingRooms.Add(created);
            }

            Dictionary<string, DungeonRoomRecord> roomsByPosition = new Dictionary<string, DungeonRoomRecord>();
            foreach (PendingRoom pendingRoom in pendingRooms)
            {
                string shape = PickRoomShape(dungeonType, random);
                int width = random.Next(2, 5);
                int height = random.Next(2, 4);
                if (shape == "Narrow")
                {
                    if (random.Next(2) == 0)
                    {
                        width = random.Next(1, 3);
                        height = random.Next(3, 6);
                    }
                    else
                    {
                        width = random.Next(3, 6);
                        height = random.Next(1, 3);
                    }
                }

                DungeonRoomRecord room = new DungeonRoomRecord
                {
                    LevelNumber = levelNumber,
                    X = pendingRoom.X,
                    Y = pendingRoom.Y,
                    Width = width,
                    Height = height,
                    Shape = shape
                };
                MoveGeneratedRoomToFreeSpot(level, level.Rooms, room, pendingRoom.X, pendingRoom.Y);
                StockRoom(room, recommendedLevel + levelNumber - 1, dungeonType, isLair, random, russian);
                level.Rooms.Add(room);
                roomsByPosition[RoomKey(pendingRoom.X, pendingRoom.Y)] = room;
            }

            DungeonRoomRecord entrance = level.Rooms.OrderBy(r => Math.Abs(r.X - level.Width / 2) + Math.Abs(r.Y - level.Height / 2)).FirstOrDefault();
            if (entrance != null && levelNumber == 1 && !isLair)
            {
                entrance.Kind = "Entrance";
                entrance.Title = russian ? "Вход" : "Entrance";
                entrance.Details = russian ? "Первая отмеченная комната данжа." : "The first mapped room of the dungeon.";
            }

            foreach (PendingRoom pendingRoom in pendingRooms)
            {
                if (string.IsNullOrWhiteSpace(pendingRoom.ParentKey)) continue;
                DungeonRoomRecord from;
                DungeonRoomRecord to;
                if (!roomsByPosition.TryGetValue(pendingRoom.ParentKey, out from)) continue;
                if (!roomsByPosition.TryGetValue(RoomKey(pendingRoom.X, pendingRoom.Y), out to)) continue;
                DungeonConnectionRecord connection = new DungeonConnectionRecord
                {
                    FromRoomId = from.Id,
                    ToRoomId = to.Id,
                    Kind = "Corridor",
                    PassageWidth = PickPassageWidth(random),
                    DoorKind = ""
                };
                AssignGeneratedDungeonPath(level, connection, from, to);
                level.Connections.Add(connection);
                AddGeneratedDoor(level, connection, PickDoorKind(random, 28, 8));
            }

            AddLoopConnections(level, random);
            RepairGeneratedDungeonConnectionPaths(level);
            return level;
        }

        private void AddLoopConnections(DungeonLevelRecord level, Random random)
        {
            if (level == null || level.Rooms == null || level.Rooms.Count < 6) return;
            int loops = Math.Max(1, level.Rooms.Count / 9);
            int attempts = loops * 8;
            int added = 0;
            while (added < loops && attempts-- > 0)
            {
                DungeonRoomRecord a = level.Rooms[random.Next(level.Rooms.Count)];
                DungeonRoomRecord b = level.Rooms
                    .Where(r => r.Id != a.Id)
                    .OrderBy(r => Math.Abs(r.X - a.X) + Math.Abs(r.Y - a.Y))
                    .Skip(random.Next(Math.Min(3, level.Rooms.Count - 1)))
                    .FirstOrDefault();
                if (b == null) continue;
                if (level.Connections.Any(c => SameConnection(c, a.Id, b.Id))) continue;
                if (GeneratedRoomGraphDistance(level, a.Id, b.Id, 3) <= 3) continue;
                DungeonConnectionRecord connection = new DungeonConnectionRecord
                {
                    FromRoomId = a.Id,
                    ToRoomId = b.Id,
                    Kind = "Corridor",
                    PassageWidth = PickPassageWidth(random),
                    DoorKind = ""
                };
                AssignGeneratedDungeonPath(level, connection, a, b);
                if (!GeneratedLoopConnectionIsUseful(level, connection, a, b)) continue;
                level.Connections.Add(connection);
                AddGeneratedDoor(level, connection, PickDoorKind(random, 24, 14));
                added++;
            }
        }

        private static int GeneratedRoomGraphDistance(DungeonLevelRecord level, string fromRoomId, string toRoomId, int maxDepth)
        {
            if (level == null || level.Connections == null) return int.MaxValue;
            if (string.IsNullOrWhiteSpace(fromRoomId) || string.IsNullOrWhiteSpace(toRoomId)) return int.MaxValue;
            if (fromRoomId == toRoomId) return 0;

            Queue<Tuple<string, int>> queue = new Queue<Tuple<string, int>>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(Tuple.Create(fromRoomId, 0));
            visited.Add(fromRoomId);
            while (queue.Count > 0)
            {
                Tuple<string, int> current = queue.Dequeue();
                if (current.Item2 >= maxDepth) continue;
                foreach (DungeonConnectionRecord connection in level.Connections)
                {
                    if (connection == null) continue;
                    string next = null;
                    if (connection.FromRoomId == current.Item1) next = connection.ToRoomId;
                    else if (connection.ToRoomId == current.Item1) next = connection.FromRoomId;
                    if (string.IsNullOrWhiteSpace(next) || visited.Contains(next)) continue;
                    if (next == toRoomId) return current.Item2 + 1;
                    visited.Add(next);
                    queue.Enqueue(Tuple.Create(next, current.Item2 + 1));
                }
            }

            return int.MaxValue;
        }

        private static bool GeneratedLoopConnectionIsUseful(DungeonLevelRecord level, DungeonConnectionRecord connection, DungeonRoomRecord from, DungeonRoomRecord to)
        {
            if (level == null || connection == null || from == null || to == null) return false;
            if (connection.PathPoints == null || connection.PathPoints.Count < 2) return false;
            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            if (!GeneratedPathHasValidRoomEntrances(connection.PathPoints, from, to, passageWidth)) return false;
            if (DungeonGeometry.PathHasTinyStep(connection.PathPoints, 1.05)) return false;
            if (DungeonGeometry.PathHasTightSelfParallelRun(connection.PathPoints, 1.05, 0.75)) return false;
            if (GeneratedPathHitsRoom(level, connection.PathPoints, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathHitsLinkedRoomInterior(level, connection.PathPoints, from.Id, to.Id)) return false;
            if (GeneratedPathPassesUnderLinkedRoom(level, connection.PathPoints, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathRunsTooCloseToExisting(level, connection.PathPoints)) return false;
            return true;
        }

        private void RepairGeneratedDungeonConnectionPaths(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null || level.Rooms == null) return;
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                for (int i = 0; i < level.Connections.Count; i++)
                {
                    DungeonConnectionRecord connection = level.Connections[i];
                    if (!GeneratedConnectionNeedsRepair(level, connection)) continue;

                    DungeonRoomRecord from = level.Rooms.FirstOrDefault(room => room != null && room.Id == connection.FromRoomId);
                    DungeonRoomRecord to = level.Rooms.FirstOrDefault(room => room != null && room.Id == connection.ToRoomId);
                    if (from == null || to == null) continue;

                    level.Connections.RemoveAt(i);
                    AssignGeneratedDungeonPath(level, connection, from, to);
                    level.Connections.Insert(i, connection);
                    RealignGeneratedDoors(level, connection, from, to);
                    changed = true;
                }

                if (!changed) break;
            }
        }

        private static bool GeneratedConnectionNeedsRepair(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(room => room != null && room.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(room => room != null && room.Id == connection.ToRoomId);
            if (from == null || to == null) return false;
            List<DungeonPathPointRecord> points = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            return points.Count < 2
                || !GeneratedPathHasValidRoomEntrances(points, from, to, passageWidth)
                || DungeonGeometry.PathHasTinyStep(points, 1.05)
                || DungeonGeometry.PathHasTightSelfParallelRun(points, 1.05, 0.75)
                || GeneratedPathHitsRoom(level, points, from.Id, to.Id, passageWidth)
                || GeneratedPathHitsLinkedRoomInterior(level, points, from.Id, to.Id)
                || GeneratedPathPassesUnderLinkedRoom(level, points, from.Id, to.Id, passageWidth)
                || GeneratedPathRunsTooCloseToExisting(level, points, connection);
        }

        private void RealignGeneratedDoors(DungeonLevelRecord level, DungeonConnectionRecord connection, DungeonRoomRecord from, DungeonRoomRecord to)
        {
            if (level == null || level.Doors == null || connection == null || from == null || to == null) return;
            foreach (DungeonDoorRecord door in level.Doors)
            {
                if (door == null) continue;
                bool sameDirection = door.FromRoomId == connection.FromRoomId && door.ToRoomId == connection.ToRoomId;
                bool reverseDirection = door.FromRoomId == connection.ToRoomId && door.ToRoomId == connection.FromRoomId;
                if (!sameDirection && !reverseDirection) continue;

                DungeonRoomRecord doorRoom = sameDirection ? from : to;
                DungeonPathPointRecord pathPoint = connection.PathPoints == null
                    ? null
                    : sameDirection
                        ? connection.PathPoints.FirstOrDefault()
                        : connection.PathPoints.LastOrDefault();
                if (pathPoint == null) continue;

                double x;
                double y;
                string orientation;
                FindDoorPointOnRoomEdge(doorRoom, pathPoint.X, pathPoint.Y, out x, out y, out orientation);
                door.X = x;
                door.Y = y;
                door.Orientation = orientation;
            }
        }

        private void AssignGeneratedDungeonPath(DungeonLevelRecord level, DungeonConnectionRecord connection, DungeonRoomRecord from, DungeonRoomRecord to)
        {
            if (level == null || connection == null || from == null || to == null) return;
            double sharedX;
            double sharedY;
            string sharedOrientation;
            if (TryFindSharedDoorPoint(from, to, out sharedX, out sharedY, out sharedOrientation))
            {
                connection.PathPoints = new List<DungeonPathPointRecord>();
                return;
            }

            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            List<DungeonPathPointRecord> directPath;
            if (TryBuildAxisAlignedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildDirectDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildStubbedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildNearStraightDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            connection.PathPoints = BuildGeneratedDungeonPath(level, from, to, passageWidth);
        }

        private List<DungeonPathPointRecord> BuildGeneratedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            List<DungeonPathPointRecord> directPath;
            if (TryBuildAxisAlignedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            if (TryBuildDirectDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            if (TryBuildStubbedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            DungeonPathPointRecord start = RoomBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = RoomBoundaryPathPoint(to, from, passageWidth);
            List<List<DungeonPathPointRecord>> candidates = BuildGeneratedDungeonPathCandidates(level, from, to, start, end, passageWidth);
            candidates = candidates
                .Select(path => NormalizeGeneratedPathEntrances(path, from, to, passageWidth))
                .ToList();
            List<List<DungeonPathPointRecord>> validCandidates = candidates
                .Where(path => GeneratedPathIsOrthogonal(path))
                .Where(path => GeneratedPathHasValidRoomEntrances(path, from, to, passageWidth))
                .Where(path => !DungeonGeometry.PathHasTinyStep(path, 1.05))
                .Where(path => !DungeonGeometry.PathHasTightSelfParallelRun(path, 1.05, 0.75))
                .Where(path => !GeneratedPathPassesUnderLinkedRoom(level, path, from.Id, to.Id, passageWidth))
                .ToList();
            List<IList<DungeonPathPointRecord>> existingPaths = GeneratedExistingPathSnapshots(level);
            List<DungeonPathPointRecord> best = PickLowestCostGeneratedPath(validCandidates
                .Where(path => !GeneratedPathHitsRoom(level, path, from.Id, to.Id, passageWidth))
                .Where(path => !GeneratedPathHitsLinkedRoomInterior(level, path, from.Id, to.Id))
                .Where(path => !GeneratedPathRunsTooCloseToExisting(path, existingPaths)),
                existingPaths);
            if (best != null) return best;

            return TryBuildVisibilityGeneratedDungeonPath(level, from, to, passageWidth)
                ?? GeneratedOrthogonalFallbackPath(level, from, to, passageWidth, start, end);
        }

        private bool TryBuildDirectDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            List<DungeonPathPointRecord> candidate = CleanDungeonPathPoints(new List<DungeonPathPointRecord>
            {
                RoomBoundaryPathPoint(from, to, passageWidth),
                RoomBoundaryPathPoint(to, from, passageWidth)
            });
            candidate = NormalizeGeneratedPathEntrances(candidate, from, to, passageWidth);
            if (candidate.Count != 2) return false;
            if (DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to) && !GeneratedPathIsOrthogonal(candidate)) return false;
            if (!GeneratedPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathRunsTooCloseToExisting(level, candidate)) return false;

            path = candidate;
            return true;
        }

        private bool TryBuildStubbedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            DungeonPathPointRecord start = RoomBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = RoomBoundaryPathPoint(to, from, passageWidth);
            DungeonPathPointRecord startOutside = RoomOutsidePathPoint(from, to, passageWidth);
            DungeonPathPointRecord endOutside = RoomOutsidePathPoint(to, from, passageWidth);
            List<List<DungeonPathPointRecord>> candidates = BuildGeneratedStubbedDungeonPathCandidates(level, start, startOutside, endOutside, end);
            candidates = candidates
                .Select(candidate => NormalizeGeneratedPathEntrances(candidate, from, to, passageWidth))
                .ToList();
            List<List<DungeonPathPointRecord>> validCandidates = candidates
                .Where(candidate => GeneratedPathIsOrthogonal(candidate))
                .Where(candidate => GeneratedPathHasValidRoomEntrances(candidate, from, to, passageWidth))
                .Where(candidate => !DungeonGeometry.PathHasTinyStep(candidate, 1.05))
                .Where(candidate => !DungeonGeometry.PathHasTightSelfParallelRun(candidate, 1.05, 0.75))
                .Where(candidate => !GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .ToList();

            List<IList<DungeonPathPointRecord>> existingPaths = GeneratedExistingPathSnapshots(level);
            List<DungeonPathPointRecord> best = PickLowestCostGeneratedPath(validCandidates
                .Where(candidate => !GeneratedPathRunsTooCloseToExisting(candidate, existingPaths)),
                existingPaths);

            if (best == null) return false;

            path = best;
            return true;
        }

        private List<List<DungeonPathPointRecord>> BuildGeneratedStubbedDungeonPathCandidates(
            DungeonLevelRecord level,
            DungeonPathPointRecord start,
            DungeonPathPointRecord startOutside,
            DungeonPathPointRecord endOutside,
            DungeonPathPointRecord end)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            if (start == null || startOutside == null || endOutside == null || end == null) return candidates;

            // Сначала пробуем маршруты, где коридор выходит из стены коротким перпендикулярным участком
            // и поворачивает уже снаружи комнаты. Так генератор не оставляет "ступеньку" внутри проема.
            AddGeneratedPathCandidate(candidates, start, startOutside, endOutside, end);
            AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = endOutside.Y }, endOutside, end);
            AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = endOutside.X, Y = startOutside.Y }, endOutside, end);

            double midX = Math.Round((startOutside.X + endOutside.X) / 2.0);
            double midY = Math.Round((startOutside.Y + endOutside.Y) / 2.0);
            AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = midX, Y = startOutside.Y }, new DungeonPathPointRecord { X = midX, Y = endOutside.Y }, endOutside, end);
            AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = midY }, new DungeonPathPointRecord { X = endOutside.X, Y = midY }, endOutside, end);

            List<double> xLanes = new List<double> { startOutside.X, endOutside.X, midX };
            List<double> yLanes = new List<double> { startOutside.Y, endOutside.Y, midY };
            if (level != null)
            {
                xLanes.Add(1);
                xLanes.Add(Math.Max(1, level.Width - 1));
                yLanes.Add(1);
                yLanes.Add(Math.Max(1, level.Height - 1));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    xLanes.Add(Math.Max(1, room.X - 1));
                    xLanes.Add(Math.Min(Math.Max(1, level.Width - 1), room.X + room.Width + 1));
                    yLanes.Add(Math.Max(1, room.Y - 1));
                    yLanes.Add(Math.Min(Math.Max(1, level.Height - 1), room.Y + room.Height + 1));
                }
            }

            foreach (double laneX in xLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.X)).Take(14))
            {
                AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = laneX, Y = startOutside.Y }, new DungeonPathPointRecord { X = laneX, Y = endOutside.Y }, endOutside, end);
            }

            foreach (double laneY in yLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.Y)).Take(14))
            {
                AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = laneY }, new DungeonPathPointRecord { X = endOutside.X, Y = laneY }, endOutside, end);
            }

            List<double> compactXLanes = xLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.X)).Take(7).ToList();
            List<double> compactYLanes = yLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.Y)).Take(7).ToList();
            foreach (double laneX in compactXLanes)
            {
                foreach (double laneY in compactYLanes)
                {
                    AddGeneratedPathCandidate(candidates,
                        start,
                        startOutside,
                        new DungeonPathPointRecord { X = laneX, Y = startOutside.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = endOutside.X, Y = laneY },
                        endOutside,
                        end);
                    AddGeneratedPathCandidate(candidates,
                        start,
                        startOutside,
                        new DungeonPathPointRecord { X = startOutside.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = endOutside.Y },
                        endOutside,
                        end);
                }
            }

            return candidates;
        }

        private List<List<DungeonPathPointRecord>> BuildGeneratedDungeonPathCandidates(
            DungeonLevelRecord level,
            DungeonRoomRecord from,
            DungeonRoomRecord to,
            DungeonPathPointRecord start,
            DungeonPathPointRecord end,
            int passageWidth)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            if (start == null || end == null) return candidates;

            double midX = Math.Round((start.X + end.X) / 2.0);
            double midY = Math.Round((start.Y + end.Y) / 2.0);
            AddGeneratedPathCandidate(candidates, start, end);
            AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = end.X, Y = start.Y }, end);
            AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = end.Y }, end);
            AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = midX, Y = start.Y }, new DungeonPathPointRecord { X = midX, Y = end.Y }, end);
            AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = midY }, new DungeonPathPointRecord { X = end.X, Y = midY }, end);

            List<double> xLanes = new List<double> { midX };
            List<double> yLanes = new List<double> { midY };
            if (level != null)
            {
                xLanes.Add(1);
                xLanes.Add(Math.Max(1, level.Width - 1));
                yLanes.Add(1);
                yLanes.Add(Math.Max(1, level.Height - 1));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    xLanes.Add(Math.Max(1, room.X - 1));
                    xLanes.Add(Math.Min(Math.Max(1, level.Width - 1), room.X + room.Width + 1));
                    yLanes.Add(Math.Max(1, room.Y - 1));
                    yLanes.Add(Math.Min(Math.Max(1, level.Height - 1), room.Y + room.Height + 1));
                }
            }

            foreach (double laneX in xLanes.Distinct().Take(12))
            {
                AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = laneX, Y = start.Y }, new DungeonPathPointRecord { X = laneX, Y = end.Y }, end);
            }

            foreach (double laneY in yLanes.Distinct().Take(12))
            {
                AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = laneY }, new DungeonPathPointRecord { X = end.X, Y = laneY }, end);
            }

            foreach (double laneX in xLanes.Distinct().Take(7))
            {
                foreach (double laneY in yLanes.Distinct().Take(7))
                {
                    AddGeneratedPathCandidate(candidates,
                        start,
                        new DungeonPathPointRecord { X = laneX, Y = start.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = end.X, Y = laneY },
                        end);
                    AddGeneratedPathCandidate(candidates,
                        start,
                        new DungeonPathPointRecord { X = start.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = end.Y },
                        end);
                }
            }

            return candidates;
        }

        private bool TryBuildAxisAlignedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            passageWidth = Math.Max(1, Math.Min(4, passageWidth));
            double clearance = GeneratedPassageCornerClearance(passageWidth);
            List<DungeonPathPointRecord> candidate = null;

            if (from.X + from.Width <= to.X || to.X + to.Width <= from.X)
            {
                double overlapTop = Math.Max(from.Y, to.Y);
                double overlapBottom = Math.Min(from.Y + from.Height, to.Y + to.Height);
                double low = overlapTop;
                double high = overlapBottom;
                if (DungeonGeometry.UsesBoxEdges(from))
                {
                    low = Math.Max(low, from.Y + clearance);
                    high = Math.Min(high, from.Y + from.Height - clearance);
                }

                if (DungeonGeometry.UsesBoxEdges(to))
                {
                    low = Math.Max(low, to.Y + clearance);
                    high = Math.Min(high, to.Y + to.Height - clearance);
                }

                if (overlapBottom > overlapTop)
                {
                    double y = low <= high
                        ? ClampDouble((RoomCenterY(from) + RoomCenterY(to)) / 2.0, low, high)
                        : (overlapTop + overlapBottom) / 2.0;
                    bool useCornerClearance = low <= high;
                    bool fromIsLeft = from.X + from.Width <= to.X;
                    DungeonPathPointRecord start;
                    DungeonPathPointRecord end;
                    if (DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(from, true, fromIsLeft, y, passageWidth, useCornerClearance, out start)
                        && DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(to, true, !fromIsLeft, y, passageWidth, useCornerClearance, out end))
                    {
                        candidate = new List<DungeonPathPointRecord> { start, end };
                    }
                }
            }
            else if (from.Y + from.Height <= to.Y || to.Y + to.Height <= from.Y)
            {
                double overlapLeft = Math.Max(from.X, to.X);
                double overlapRight = Math.Min(from.X + from.Width, to.X + to.Width);
                double low = overlapLeft;
                double high = overlapRight;
                if (DungeonGeometry.UsesBoxEdges(from))
                {
                    low = Math.Max(low, from.X + clearance);
                    high = Math.Min(high, from.X + from.Width - clearance);
                }

                if (DungeonGeometry.UsesBoxEdges(to))
                {
                    low = Math.Max(low, to.X + clearance);
                    high = Math.Min(high, to.X + to.Width - clearance);
                }

                if (overlapRight > overlapLeft)
                {
                    double x = low <= high
                        ? ClampDouble((RoomCenterX(from) + RoomCenterX(to)) / 2.0, low, high)
                        : (overlapLeft + overlapRight) / 2.0;
                    bool useCornerClearance = low <= high;
                    bool fromIsAbove = from.Y + from.Height <= to.Y;
                    DungeonPathPointRecord start;
                    DungeonPathPointRecord end;
                    if (DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(from, false, fromIsAbove, x, passageWidth, useCornerClearance, out start)
                        && DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(to, false, !fromIsAbove, x, passageWidth, useCornerClearance, out end))
                    {
                        candidate = new List<DungeonPathPointRecord> { start, end };
                    }
                }
            }

            if (candidate == null) return false;
            candidate = CleanDungeonPathPoints(candidate);
            if (candidate.Count != 2) return false;
            if (!GeneratedPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathRunsTooCloseToExisting(level, candidate)) return false;
            path = candidate;
            return true;
        }

        private void AddGeneratedPathCandidate(List<List<DungeonPathPointRecord>> candidates, params DungeonPathPointRecord[] points)
        {
            if (candidates == null || points == null || points.Length < 2) return;
            List<DungeonPathPointRecord> cleaned = CleanDungeonPathPoints(points.Select(ClonePathPoint).ToList());
            if (cleaned.Count < 2) return;
            if (DungeonGeometry.PathHasTinyStep(cleaned, 1.05)) return;
            if (DungeonGeometry.PathHasTightSelfParallelRun(cleaned, 1.05, 0.75)) return;
            if (candidates.Any(path => GeneratedPathSequenceEquals(path, cleaned))) return;
            candidates.Add(cleaned);
        }

        private List<DungeonPathPointRecord> GeneratedOrthogonalFallbackPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, DungeonPathPointRecord start, DungeonPathPointRecord end)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            bool bothBoxes = DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to);
            List<IList<DungeonPathPointRecord>> existingPaths = GeneratedExistingPathSnapshots(level);
            if (start != null && end != null)
            {
                if (!bothBoxes || AlmostSame(start.X, end.X) || AlmostSame(start.Y, end.Y))
                {
                    AddGeneratedPathCandidate(candidates, start, end);
                }

                DungeonPathPointRecord startOutside = RoomOutsidePathPoint(from, to, passageWidth);
                DungeonPathPointRecord endOutside = RoomOutsidePathPoint(to, from, passageWidth);
                AddGeneratedPathCandidate(candidates, start, startOutside, endOutside, end);
                AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = endOutside.X, Y = startOutside.Y }, endOutside, end);
                AddGeneratedPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = endOutside.Y }, endOutside, end);
                AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = end.X, Y = start.Y }, end);
                AddGeneratedPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = end.Y }, end);
            }

            foreach (DungeonPathPointRecord fromPortal in BuildGeneratedFallbackPortals(from, passageWidth))
            {
                DungeonPathPointRecord fromOutside = GeneratedOutsidePortalPoint(from, fromPortal);
                foreach (DungeonPathPointRecord toPortal in BuildGeneratedFallbackPortals(to, passageWidth))
                {
                    DungeonPathPointRecord toOutside = GeneratedOutsidePortalPoint(to, toPortal);
                    AddGeneratedPathCandidate(candidates, fromPortal, fromOutside, toOutside, toPortal);
                    AddGeneratedPathCandidate(candidates, fromPortal, fromOutside, new DungeonPathPointRecord { X = toOutside.X, Y = fromOutside.Y }, toOutside, toPortal);
                    AddGeneratedPathCandidate(candidates, fromPortal, fromOutside, new DungeonPathPointRecord { X = fromOutside.X, Y = toOutside.Y }, toOutside, toPortal);
                    AddGeneratedPortalLaneCandidates(candidates, level, fromPortal, fromOutside, toOutside, toPortal);
                }
            }

            List<List<DungeonPathPointRecord>> normalizedCandidates = candidates
                .Select(candidate => NormalizeGeneratedPathEntrances(candidate, from, to, passageWidth))
                .Where(candidate => GeneratedPathIsOrthogonal(candidate))
                .Where(candidate => GeneratedPathHasValidRoomEntrances(candidate, from, to, passageWidth))
                .Where(candidate => !DungeonGeometry.PathHasTinyStep(candidate, 1.05))
                .Where(candidate => !DungeonGeometry.PathHasTightSelfParallelRun(candidate, 1.05, 0.75))
                .ToList();

            List<DungeonPathPointRecord> best = PickLowestCostGeneratedPath(normalizedCandidates
                .Where(candidate => !GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !GeneratedPathRunsTooCloseToExisting(candidate, existingPaths)),
                null);
            if (best != null) return best;

            best = PickLowestCostGeneratedPath(normalizedCandidates
                .Where(candidate => !GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)),
                existingPaths);
            if (best != null) return best;

            best = PickLowestCostGeneratedPath(normalizedCandidates
                .Where(candidate => !GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)),
                existingPaths);
            if (best != null) return best;

            if (start != null && end != null)
            {
                List<DungeonPathPointRecord> direct = CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
                if ((!bothBoxes || GeneratedPathIsOrthogonal(direct))
                    && !DungeonGeometry.PathHasTinyStep(direct, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(direct, 1.05, 0.75)
                    && !GeneratedPathHitsRoom(level, direct, from.Id, to.Id, passageWidth)
                    && !GeneratedPathHitsLinkedRoomInterior(level, direct, from.Id, to.Id)
                    && !GeneratedPathPassesUnderLinkedRoom(level, direct, from.Id, to.Id, passageWidth)
                    && !GeneratedPathRunsTooCloseToExisting(direct, existingPaths))
                {
                    return direct;
                }

                List<DungeonPathPointRecord> orthogonal = NormalizeGeneratedPathEntrances(new List<DungeonPathPointRecord>
                {
                    start,
                    new DungeonPathPointRecord { X = end.X, Y = start.Y },
                    end
                }, from, to, passageWidth);
                if (GeneratedPathIsOrthogonal(orthogonal)
                    && GeneratedPathHasValidRoomEntrances(orthogonal, from, to, passageWidth)
                    && !DungeonGeometry.PathHasTinyStep(orthogonal, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(orthogonal, 1.05, 0.75)
                    && !GeneratedPathHitsLinkedRoomInterior(level, orthogonal, from.Id, to.Id)
                    && !GeneratedPathPassesUnderLinkedRoom(level, orthogonal, from.Id, to.Id, passageWidth)
                    && !GeneratedPathRunsTooCloseToExisting(orthogonal, existingPaths))
                {
                    return orthogonal;
                }

                orthogonal = NormalizeGeneratedPathEntrances(new List<DungeonPathPointRecord>
                {
                    start,
                    new DungeonPathPointRecord { X = start.X, Y = end.Y },
                    end
                }, from, to, passageWidth);
                if (GeneratedPathIsOrthogonal(orthogonal)
                    && GeneratedPathHasValidRoomEntrances(orthogonal, from, to, passageWidth)
                    && !DungeonGeometry.PathHasTinyStep(orthogonal, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(orthogonal, 1.05, 0.75)
                    && !GeneratedPathHitsLinkedRoomInterior(level, orthogonal, from.Id, to.Id)
                    && !GeneratedPathPassesUnderLinkedRoom(level, orthogonal, from.Id, to.Id, passageWidth)
                    && !GeneratedPathRunsTooCloseToExisting(orthogonal, existingPaths))
                {
                    return orthogonal;
                }
            }

            return CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
        }

        private static List<DungeonPathPointRecord> BuildGeneratedFallbackPortals(DungeonRoomRecord room, int passageWidth)
        {
            List<DungeonPathPointRecord> portals = new List<DungeonPathPointRecord>();
            if (room == null) return portals;

            if (!DungeonGeometry.UsesBoxEdges(room))
            {
                double centerX = RoomCenterX(room);
                double centerY = RoomCenterY(room);
                double rx = Math.Max(0.5, room.Width / 2.0);
                double ry = Math.Max(0.5, room.Height / 2.0);
                AddGeneratedPortal(portals, new DungeonPathPointRecord { X = centerX - rx, Y = centerY });
                AddGeneratedPortal(portals, new DungeonPathPointRecord { X = centerX + rx, Y = centerY });
                AddGeneratedPortal(portals, new DungeonPathPointRecord { X = centerX, Y = centerY - ry });
                AddGeneratedPortal(portals, new DungeonPathPointRecord { X = centerX, Y = centerY + ry });
                return portals;
            }

            double clearance = GeneratedPassageCornerClearance(passageWidth);
            double left = room.X;
            double right = room.X + room.Width;
            double top = room.Y;
            double bottom = room.Y + room.Height;
            double x = ClampDouble(RoomCenterX(room), left + clearance, right - clearance);
            double y = ClampDouble(RoomCenterY(room), top + clearance, bottom - clearance);
            AddGeneratedPortal(portals, new DungeonPathPointRecord { X = x, Y = top });
            AddGeneratedPortal(portals, new DungeonPathPointRecord { X = x, Y = bottom });
            AddGeneratedPortal(portals, new DungeonPathPointRecord { X = left, Y = y });
            AddGeneratedPortal(portals, new DungeonPathPointRecord { X = right, Y = y });

            return portals;
        }

        private static void AddGeneratedPortal(List<DungeonPathPointRecord> portals, DungeonPathPointRecord portal)
        {
            if (portals == null || portal == null) return;
            if (portals.Any(existing => AlmostSame(existing.X, portal.X) && AlmostSame(existing.Y, portal.Y))) return;
            portals.Add(portal);
        }

        private static DungeonPathPointRecord GeneratedOutsidePortalPoint(DungeonRoomRecord room, DungeonPathPointRecord portal)
        {
            if (room == null || portal == null) return ClonePathPoint(portal);
            const double offset = 0.85;
            if (DungeonGeometry.UsesBoxEdges(room))
            {
                if (AlmostSame(portal.X, room.X)) return new DungeonPathPointRecord { X = portal.X - offset, Y = portal.Y };
                if (AlmostSame(portal.X, room.X + room.Width)) return new DungeonPathPointRecord { X = portal.X + offset, Y = portal.Y };
                if (AlmostSame(portal.Y, room.Y)) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y - offset };
                if (AlmostSame(portal.Y, room.Y + room.Height)) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y + offset };
            }

            double dx = portal.X - RoomCenterX(room);
            double dy = portal.Y - RoomCenterY(room);
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.0001) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y - offset };
            return new DungeonPathPointRecord { X = portal.X + dx / length * offset, Y = portal.Y + dy / length * offset };
        }

        private void AddGeneratedPortalLaneCandidates(
            List<List<DungeonPathPointRecord>> candidates,
            DungeonLevelRecord level,
            DungeonPathPointRecord fromPortal,
            DungeonPathPointRecord fromOutside,
            DungeonPathPointRecord toOutside,
            DungeonPathPointRecord toPortal)
        {
            if (candidates == null || fromPortal == null || fromOutside == null || toOutside == null || toPortal == null) return;
            List<double> xLanes = BuildGeneratedPortalLaneValues(level, fromOutside.X, toOutside.X, true);
            List<double> yLanes = BuildGeneratedPortalLaneValues(level, fromOutside.Y, toOutside.Y, false);

            foreach (double laneX in xLanes)
            {
                AddGeneratedPathCandidate(candidates,
                    fromPortal,
                    fromOutside,
                    new DungeonPathPointRecord { X = laneX, Y = fromOutside.Y },
                    new DungeonPathPointRecord { X = laneX, Y = toOutside.Y },
                    toOutside,
                    toPortal);
            }

            foreach (double laneY in yLanes)
            {
                AddGeneratedPathCandidate(candidates,
                    fromPortal,
                    fromOutside,
                    new DungeonPathPointRecord { X = fromOutside.X, Y = laneY },
                    new DungeonPathPointRecord { X = toOutside.X, Y = laneY },
                    toOutside,
                    toPortal);
            }

            foreach (double laneX in xLanes.Take(8))
            {
                foreach (double laneY in yLanes.Take(8))
                {
                    AddGeneratedPathCandidate(candidates,
                        fromPortal,
                        fromOutside,
                        new DungeonPathPointRecord { X = laneX, Y = fromOutside.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = toOutside.X, Y = laneY },
                        toOutside,
                        toPortal);
                    AddGeneratedPathCandidate(candidates,
                        fromPortal,
                        fromOutside,
                        new DungeonPathPointRecord { X = fromOutside.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = toOutside.Y },
                        toOutside,
                        toPortal);
                }
            }
        }

        private static List<double> BuildGeneratedPortalLaneValues(DungeonLevelRecord level, double start, double end, bool horizontalAxis)
        {
            List<double> lanes = new List<double> { start, end, Math.Round((start + end) / 2.0) };
            if (level != null)
            {
                double max = horizontalAxis ? level.Width - 1 : level.Height - 1;
                lanes.Add(1);
                lanes.Add(Math.Max(1, max));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    double low = horizontalAxis ? room.X : room.Y;
                    double high = horizontalAxis ? room.X + room.Width : room.Y + room.Height;
                    lanes.Add(ClampDouble(low - 1.2, 1, max));
                    lanes.Add(ClampDouble(high + 1.2, 1, max));
                }
            }

            return lanes
                .Distinct()
                .OrderBy(lane => Math.Min(Math.Abs(lane - start), Math.Abs(lane - end)))
                .Take(8)
                .ToList();
        }

        private static DungeonPathPointRecord ClonePathPoint(DungeonPathPointRecord point)
        {
            return point == null ? new DungeonPathPointRecord() : new DungeonPathPointRecord { X = point.X, Y = point.Y };
        }

        private static bool GeneratedPathSequenceEquals(List<DungeonPathPointRecord> a, List<DungeonPathPointRecord> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!AlmostSame(a[i].X, b[i].X) || !AlmostSame(a[i].Y, b[i].Y)) return false;
            }

            return true;
        }

        private static bool GeneratedPathIsOrthogonal(List<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count < 2) return false;
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord previous = points[i - 1];
                DungeonPathPointRecord current = points[i];
                if (previous == null || current == null) return false;
                if (!AlmostSame(previous.X, current.X) && !AlmostSame(previous.Y, current.Y)) return false;
            }

            return true;
        }

        private bool TryBuildNearStraightDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;
            passageWidth = Math.Max(1, Math.Min(4, passageWidth));
            DungeonPathPointRecord start = RoomBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = RoomBoundaryPathPoint(to, from, passageWidth);
            if (!ShouldPreferStraightDungeonPath(start, end)) return false;

            List<DungeonPathPointRecord> candidate = CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
            candidate = NormalizeGeneratedPathEntrances(candidate, from, to, passageWidth);
            if (candidate.Count != 2) return false;
            if (DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to) && !GeneratedPathIsOrthogonal(candidate)) return false;
            if (!GeneratedPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (GeneratedPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (GeneratedPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (GeneratedPathRunsTooCloseToExisting(level, candidate)) return false;
            path = candidate;
            return true;
        }

        private List<DungeonPathPointRecord> TryBuildVisibilityGeneratedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            if (level == null || from == null || to == null) return null;

            DungeonPathPointRecord start = RoomBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = RoomBoundaryPathPoint(to, from, passageWidth);
            List<double> xLanes = BuildGeneratedVisibilityLanes(level, start.X, end.X, true);
            List<double> yLanes = BuildGeneratedVisibilityLanes(level, start.Y, end.Y, false);
            List<IList<DungeonPathPointRecord>> existingPaths = GeneratedExistingPathSnapshots(level);
            List<DungeonPathPointRecord> nodes = new List<DungeonPathPointRecord>
            {
                start,
                end
            };

            foreach (double x in xLanes)
            {
                foreach (double y in yLanes)
                {
                    DungeonPathPointRecord node = new DungeonPathPointRecord { X = x, Y = y };
                    if (GeneratedVisibilityNodeIsBlocked(level, node, from.Id, to.Id)) continue;
                    if (nodes.Any(existing => GeneratedPointsAlmostSame(existing, node))) continue;
                    nodes.Add(node);
                }
            }

            Dictionary<int, List<int>> nodesByX = BuildGeneratedVisibilityAxisIndex(nodes, true);
            Dictionary<int, List<int>> nodesByY = BuildGeneratedVisibilityAxisIndex(nodes, false);
            const double infinity = double.MaxValue / 4.0;
            double[] distance = Enumerable.Repeat(infinity, nodes.Count).ToArray();
            int[] parent = Enumerable.Repeat(-1, nodes.Count).ToArray();
            bool[] visited = new bool[nodes.Count];
            distance[0] = 0;

            for (int step = 0; step < nodes.Count; step++)
            {
                int current = -1;
                double bestDistance = infinity;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (visited[i] || distance[i] >= bestDistance) continue;
                    bestDistance = distance[i];
                    current = i;
                }

                if (current < 0 || current == 1) break;
                visited[current] = true;

                List<int> sameXNodes;
                if (nodesByX.TryGetValue(GeneratedVisibilityAxisKey(nodes[current].X), out sameXNodes))
                {
                    foreach (int next in sameXNodes)
                    {
                        RelaxGeneratedVisibilityNeighbor(current, next, nodes, visited, parent, distance, level, from.Id, to.Id, passageWidth, existingPaths);
                    }
                }

                List<int> sameYNodes;
                if (nodesByY.TryGetValue(GeneratedVisibilityAxisKey(nodes[current].Y), out sameYNodes))
                {
                    foreach (int next in sameYNodes)
                    {
                        RelaxGeneratedVisibilityNeighbor(current, next, nodes, visited, parent, distance, level, from.Id, to.Id, passageWidth, existingPaths);
                    }
                }
            }

            if (parent[1] < 0) return null;

            List<DungeonPathPointRecord> path = new List<DungeonPathPointRecord>();
            for (int index = 1; index >= 0; index = parent[index])
            {
                path.Add(ClonePathPoint(nodes[index]));
                if (index == 0) break;
            }

            path.Reverse();
            path = NormalizeGeneratedPathEntrances(path, from, to, passageWidth);
            if (!GeneratedPathIsOrthogonal(path)) return null;
            if (!GeneratedPathHasValidRoomEntrances(path, from, to, passageWidth)) return null;
            if (DungeonGeometry.PathHasTinyStep(path, 1.05)) return null;
            if (DungeonGeometry.PathHasTightSelfParallelRun(path, 1.05, 0.75)) return null;
            if (GeneratedPathHitsRoom(level, path, from.Id, to.Id, passageWidth)) return null;
            if (GeneratedPathHitsLinkedRoomInterior(level, path, from.Id, to.Id)) return null;
            if (GeneratedPathPassesUnderLinkedRoom(level, path, from.Id, to.Id, passageWidth)) return null;
            if (GeneratedPathRunsTooCloseToExisting(path, existingPaths)) return null;
            return path;
        }

        private static Dictionary<int, List<int>> BuildGeneratedVisibilityAxisIndex(List<DungeonPathPointRecord> nodes, bool useX)
        {
            Dictionary<int, List<int>> index = new Dictionary<int, List<int>>();
            if (nodes == null) return index;
            for (int i = 0; i < nodes.Count; i++)
            {
                DungeonPathPointRecord node = nodes[i];
                if (node == null) continue;
                int key = GeneratedVisibilityAxisKey(useX ? node.X : node.Y);
                List<int> indexes;
                if (!index.TryGetValue(key, out indexes))
                {
                    indexes = new List<int>();
                    index[key] = indexes;
                }

                indexes.Add(i);
            }

            return index;
        }

        private static int GeneratedVisibilityAxisKey(double value)
        {
            return (int)Math.Round(value * 1000.0);
        }

        private static void RelaxGeneratedVisibilityNeighbor(
            int current,
            int next,
            List<DungeonPathPointRecord> nodes,
            bool[] visited,
            int[] parent,
            double[] distance,
            DungeonLevelRecord level,
            string fromRoomId,
            string toRoomId,
            int passageWidth,
            IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (nodes == null || visited == null || parent == null || distance == null) return;
            if (next < 0 || next >= nodes.Count || current == next || visited[next]) return;
            DungeonPathPointRecord currentNode = nodes[current];
            DungeonPathPointRecord nextNode = nodes[next];
            if (currentNode == null || nextNode == null) return;
            if (!GeneratedVisibilitySegmentIsSafe(level, currentNode, nextNode, fromRoomId, toRoomId, passageWidth, existingPaths)) return;

            double candidateDistance = distance[current]
                + Math.Abs(currentNode.X - nextNode.X)
                + Math.Abs(currentNode.Y - nextNode.Y)
                + (parent[current] >= 0 && GeneratedVisibilityWouldTurn(nodes[parent[current]], currentNode, nextNode) ? 0.4 : 0.0);
            if (candidateDistance >= distance[next]) return;
            distance[next] = candidateDistance;
            parent[next] = current;
        }

        private List<double> BuildGeneratedVisibilityLanes(DungeonLevelRecord level, double start, double end, bool horizontalAxis)
        {
            List<double> lanes = new List<double> { start, end, Math.Round((start + end) / 2.0), 1, horizontalAxis ? level.Width - 1 : level.Height - 1 };
            foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
            {
                if (room == null) continue;
                double low = horizontalAxis ? room.X : room.Y;
                double high = horizontalAxis ? room.X + room.Width : room.Y + room.Height;
                double max = horizontalAxis ? level.Width - 1 : level.Height - 1;
                lanes.Add(ClampDouble(low - 1.2, 1, max));
                lanes.Add(ClampDouble(high + 1.2, 1, max));
                lanes.Add(ClampDouble(low - 2.2, 1, max));
                lanes.Add(ClampDouble(high + 2.2, 1, max));
            }

            return lanes
                .Distinct()
                .OrderBy(lane => Math.Min(Math.Abs(lane - start), Math.Abs(lane - end)))
                .Take(24)
                .ToList();
        }

        private static bool GeneratedVisibilityWouldTurn(DungeonPathPointRecord previous, DungeonPathPointRecord current, DungeonPathPointRecord next)
        {
            if (previous == null || current == null || next == null) return false;
            bool firstVertical = AlmostSame(previous.X, current.X);
            bool secondVertical = AlmostSame(current.X, next.X);
            return firstVertical != secondVertical;
        }

        private static bool GeneratedPointsAlmostSame(DungeonPathPointRecord a, DungeonPathPointRecord b)
        {
            return a != null && b != null && AlmostSame(a.X, b.X) && AlmostSame(a.Y, b.Y);
        }

        private static bool GeneratedVisibilityNodeIsBlocked(DungeonLevelRecord level, DungeonPathPointRecord point, string fromRoomId, string toRoomId)
        {
            if (level == null || level.Rooms == null || point == null) return false;
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == fromRoomId || room.Id == toRoomId) continue;
                if (DungeonGeometry.IsPointInsideRoomBuffer(room, point.X, point.Y, 0.34)) return true;
            }

            return false;
        }

        private static bool GeneratedVisibilitySegmentIsSafe(
            DungeonLevelRecord level,
            DungeonPathPointRecord a,
            DungeonPathPointRecord b,
            string fromRoomId,
            string toRoomId,
            int passageWidth,
            IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (level == null || a == null || b == null) return false;
            if (AlmostSame(a.X, b.X) && AlmostSame(a.Y, b.Y)) return false;
            List<DungeonPathPointRecord> segment = new List<DungeonPathPointRecord> { a, b };
            if (GeneratedPathHitsRoom(level, segment, fromRoomId, toRoomId, passageWidth)) return false;
            if (GeneratedPathHitsLinkedRoomInterior(level, segment, fromRoomId, toRoomId)) return false;
            if (GeneratedPathRunsTooCloseToExisting(segment, existingPaths)) return false;
            return true;
        }

        private static bool ShouldPreferStraightDungeonPath(DungeonPathPointRecord start, DungeonPathPointRecord end)
        {
            if (start == null || end == null) return false;
            const double alignmentTolerance = 0.45;
            return Math.Abs(start.X - end.X) <= alignmentTolerance
                || Math.Abs(start.Y - end.Y) <= alignmentTolerance;
        }

        private static double GeneratedPassageCornerClearance(int passageWidth)
        {
            int width = Math.Max(1, Math.Min(4, passageWidth));
            return Math.Min(1.0, 0.45 + width * 0.13);
        }

        private static double ClampDouble(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private DungeonPathPointRecord RoomBoundaryPathPoint(DungeonRoomRecord room, DungeonRoomRecord target, int passageWidth)
        {
            // Конечные точки коридора хранятся на видимой границе комнаты: без служебного хвоста снаружи.
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, RoomCenterX(target), RoomCenterY(target), passageWidth, out x, out y, out orientation);
            return new DungeonPathPointRecord { X = x, Y = y };
        }

        private DungeonPathPointRecord RoomOutsidePathPoint(DungeonRoomRecord room, DungeonRoomRecord target, int passageWidth)
        {
            return DungeonGeometry.OutsidePassagePoint(room, RoomCenterX(target), RoomCenterY(target), passageWidth, 0.85, false);
        }

        private DungeonPathPointRecord RoomBoundaryPathPoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth)
        {
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, targetX, targetY, passageWidth, out x, out y, out orientation);
            return new DungeonPathPointRecord { X = x, Y = y };
        }

        private List<DungeonPathPointRecord> NormalizeGeneratedPathEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            points = CleanDungeonPathPoints((points ?? new List<DungeonPathPointRecord>())
                .Where(point => point != null)
                .Select(ClonePathPoint)
                .ToList());
            if (points.Count < 2 || from == null || to == null) return points;

            DungeonPathPointRecord startTarget = points.Count > 1
                ? points[1]
                : new DungeonPathPointRecord { X = RoomCenterX(to), Y = RoomCenterY(to) };
            points[0] = RoomBoundaryPathPoint(from, startTarget.X, startTarget.Y, passageWidth);

            int lastIndex = points.Count - 1;
            DungeonPathPointRecord endTarget = points.Count > 1
                ? points[lastIndex - 1]
                : new DungeonPathPointRecord { X = RoomCenterX(from), Y = RoomCenterY(from) };
            points[lastIndex] = RoomBoundaryPathPoint(to, endTarget.X, endTarget.Y, passageWidth);

            return CleanDungeonPathPoints(points);
        }

        private static bool AlmostSame(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        private static List<DungeonPathPointRecord> CleanDungeonPathPoints(List<DungeonPathPointRecord> points)
        {
            List<DungeonPathPointRecord> cleaned = new List<DungeonPathPointRecord>();
            foreach (DungeonPathPointRecord point in points ?? new List<DungeonPathPointRecord>())
            {
                if (point == null) continue;
                if (cleaned.Count > 0 && AlmostSame(cleaned[cleaned.Count - 1].X, point.X) && AlmostSame(cleaned[cleaned.Count - 1].Y, point.Y)) continue;
                cleaned.Add(point);
            }

            for (int i = cleaned.Count - 2; i >= 1; i--)
            {
                DungeonPathPointRecord previous = cleaned[i - 1];
                DungeonPathPointRecord current = cleaned[i];
                DungeonPathPointRecord next = cleaned[i + 1];
                bool sameX = AlmostSame(previous.X, current.X) && AlmostSame(current.X, next.X);
                bool sameY = AlmostSame(previous.Y, current.Y) && AlmostSame(current.Y, next.Y);
                if (sameX || sameY) cleaned.RemoveAt(i);
            }

            return cleaned;
        }

        private static double GeneratedPathCost(DungeonLevelRecord level, List<DungeonPathPointRecord> points)
        {
            return GeneratedPathCost(points, GeneratedExistingPathSnapshots(level));
        }

        private static double GeneratedPathCost(List<DungeonPathPointRecord> points, IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (points == null || points.Count < 2) return double.MaxValue;
            double cost = DungeonGeometry.PathLength(points) + DungeonGeometry.PathShapePenalty(points);
            if (existingPaths == null || existingPaths.Count == 0) return cost;

            // Перекрёсток допустим, но длинное наложение коридоров почти всегда выглядит как ошибка карты.
            return cost + DungeonGeometry.PathInteractionPenalty(
                points,
                existingPaths);
        }

        private static List<DungeonPathPointRecord> PickLowestCostGeneratedPath(
            IEnumerable<List<DungeonPathPointRecord>> candidates,
            IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            // Сохраняем поведение OrderBy(...).FirstOrDefault(): при равной цене выигрывает первый кандидат,
            // но не сортируем весь список маршрутов ради одного минимального значения.
            List<DungeonPathPointRecord> best = null;
            double bestCost = 0;
            if (candidates == null) return null;
            foreach (List<DungeonPathPointRecord> candidate in candidates)
            {
                double cost = GeneratedPathCost(candidate, existingPaths);
                if (best == null || Comparer<double>.Default.Compare(cost, bestCost) < 0)
                {
                    bestCost = cost;
                    best = candidate;
                }
            }

            return best;
        }

        private static bool GeneratedPathRunsTooCloseToExisting(DungeonLevelRecord level, List<DungeonPathPointRecord> points)
        {
            return GeneratedPathRunsTooCloseToExisting(level, points, null);
        }

        private static bool GeneratedPathRunsTooCloseToExisting(DungeonLevelRecord level, List<DungeonPathPointRecord> points, DungeonConnectionRecord ignore)
        {
            return GeneratedPathRunsTooCloseToExisting(points, GeneratedExistingPathSnapshots(level, ignore));
        }

        private static bool GeneratedPathRunsTooCloseToExisting(List<DungeonPathPointRecord> points, IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (points == null || points.Count < 2 || existingPaths == null || existingPaths.Count == 0) return false;
            return DungeonGeometry.PathRunsTooCloseToAny(
                points,
                existingPaths,
                1.05,
                1.25,
                true);
        }

        private static List<IList<DungeonPathPointRecord>> GeneratedExistingPathSnapshots(DungeonLevelRecord level)
        {
            return GeneratedExistingPathSnapshots(level, null);
        }

        private static List<IList<DungeonPathPointRecord>> GeneratedExistingPathSnapshots(DungeonLevelRecord level, DungeonConnectionRecord ignore)
        {
            List<IList<DungeonPathPointRecord>> paths = new List<IList<DungeonPathPointRecord>>();
            if (level == null || level.Connections == null) return paths;
            foreach (DungeonConnectionRecord existing in level.Connections)
            {
                if (existing == null || ReferenceEquals(existing, ignore)) continue;
                if (existing.PathPoints == null || existing.PathPoints.Count < 2) continue;
                paths.Add(existing.PathPoints);
            }

            return paths;
        }

        private static bool GeneratedPathHasValidRoomEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            if (points == null || points.Count < 2) return false;
            return GeneratedRoomEntranceIsValid(from, points[0], points[1], passageWidth)
                && GeneratedRoomEntranceIsValid(to, points[points.Count - 1], points[points.Count - 2], passageWidth);
        }

        private static bool GeneratedRoomEntranceIsValid(DungeonRoomRecord room, DungeonPathPointRecord edge, DungeonPathPointRecord outside, int passageWidth)
        {
            if (room == null || edge == null || outside == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(room))
            {
                return !DungeonGeometry.IsPointInsideRoomInterior(room, outside.X, outside.Y, 0.02);
            }

            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, edge.X, edge.Y, passageWidth, out x, out y, out orientation);
            const double entranceTolerance = 0.12;
            if (Math.Abs(edge.X - x) > entranceTolerance || Math.Abs(edge.Y - y) > entranceTolerance) return false;

            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase))
            {
                if (Math.Abs(edge.X - outside.X) > entranceTolerance) return false;
                return edge.Y <= RoomCenterY(room)
                    ? outside.Y <= edge.Y + 0.02
                    : outside.Y >= edge.Y - 0.02;
            }

            if (Math.Abs(edge.Y - outside.Y) > entranceTolerance) return false;
            return edge.X <= RoomCenterX(room)
                ? outside.X <= edge.X + 0.02
                : outside.X >= edge.X - 0.02;
        }

        private static bool GeneratedPathHitsRoom(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                foreach (DungeonRoomRecord room in level.Rooms)
                {
                    if (room == null) continue;
                    if (room.Id == fromRoomId || room.Id == toRoomId) continue;
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(a, b, room, margin)) return true;
                }
            }

            return false;
        }

        private static double GeneratedPathRoomHitPenalty(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return 0;
            double penalty = 0;
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                foreach (DungeonRoomRecord room in level.Rooms)
                {
                    if (room == null) continue;
                    if (room.Id == fromRoomId || room.Id == toRoomId) continue;
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(a, b, room, margin)) penalty += 10000;
                }
            }

            return penalty;
        }

        private static bool GeneratedPathHitsLinkedRoomInterior(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r != null && r.Id == fromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r != null && r.Id == toRoomId);
            foreach (DungeonPathPointRecord point in points)
            {
                if (point == null) continue;
                if (from != null && DungeonGeometry.IsPointInsideRoomInterior(from, point.X, point.Y, 0.01)) return true;
                if (to != null && DungeonGeometry.IsPointInsideRoomInterior(to, point.X, point.Y, 0.01)) return true;
            }

            for (int i = 1; i < points.Count; i++)
            {
                if (from != null && SegmentCrossesRoomInterior(points[i - 1], points[i], from)) return true;
                if (to != null && SegmentCrossesRoomInterior(points[i - 1], points[i], to)) return true;
                if (from != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(points[i - 1], points[i], from, 0.08)) return true;
                if (to != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(points[i - 1], points[i], to, 0.08)) return true;
            }

            return false;
        }

        private static bool GeneratedPathPassesUnderLinkedRoom(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r != null && r.Id == fromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r != null && r.Id == toRoomId);
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                if (a == null || b == null) continue;

                bool entranceFrom = i == 1;
                bool entranceTo = i == points.Count - 1;
                if (!entranceFrom && from != null && DungeonGeometry.SegmentCrossesRoomBuffer(a, b, from, margin)) return true;
                if (!entranceTo && to != null && DungeonGeometry.SegmentCrossesRoomBuffer(a, b, to, margin)) return true;
            }

            return false;
        }

        private static bool SegmentCrossesRoomInterior(DungeonPathPointRecord a, DungeonPathPointRecord b, DungeonRoomRecord room)
        {
            return DungeonGeometry.SegmentCrossesRoomInterior(a, b, room);
        }

        private string PickRoomShape(string dungeonType, Random random)
        {
            int roll = random.Next(100);
            if (dungeonType == "Natural caverns" || dungeonType == "Giant burrow")
            {
                if (roll < 55) return "Cavern";
                if (roll < 76) return "Oval";
            }

            if (roll < 10) return "Circle";
            if (roll < 24) return "Oval";
            if (roll < 34) return "Narrow";
            return "Rectangle";
        }

        private int PickPassageWidth(Random random)
        {
            int roll = random.Next(100);
            if (roll < 70) return 1;
            if (roll < 93) return 2;
            return 3;
        }

        private string PickDoorKind(Random random, int doorChance, int secretDoorChance)
        {
            int roll = random.Next(100);
            if (roll < secretDoorChance) return "SecretDoor";
            if (roll < secretDoorChance + doorChance) return "Door";
            return "";
        }

        private void AddGeneratedDoor(DungeonLevelRecord level, DungeonConnectionRecord connection, string kind)
        {
            if (level == null || connection == null || string.IsNullOrWhiteSpace(kind)) return;
            if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return;

            double x;
            double y;
            string orientation;
            DungeonPathPointRecord firstPathPoint = connection.PathPoints == null ? null : connection.PathPoints.FirstOrDefault();
            if (firstPathPoint != null)
            {
                FindDoorPointOnRoomEdge(from, firstPathPoint.X, firstPathPoint.Y, out x, out y, out orientation);
            }
            else if (!TryFindSharedDoorPoint(from, to, out x, out y, out orientation))
            {
                FindDoorPointOnRoomEdge(from, RoomCenterX(to), RoomCenterY(to), out x, out y, out orientation);
            }

            level.Doors.Add(new DungeonDoorRecord
            {
                LevelNumber = level.LevelNumber,
                X = x,
                Y = y,
                Kind = kind,
                Orientation = orientation,
                FromRoomId = connection.FromRoomId,
                ToRoomId = connection.ToRoomId
            });
        }

        private static double RoomCenterX(DungeonRoomRecord room)
        {
            return room.X + room.Width / 2.0;
        }

        private static double RoomCenterY(DungeonRoomRecord room)
        {
            return room.Y + room.Height / 2.0;
        }

        private static bool TryFindSharedDoorPoint(DungeonRoomRecord from, DungeonRoomRecord to, out double x, out double y, out string orientation)
        {
            x = 0;
            y = 0;
            orientation = "Vertical";
            if (from == null || to == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(from) || !DungeonGeometry.UsesBoxEdges(to)) return false;

            if (from.X + from.Width == to.X || to.X + to.Width == from.X)
            {
                double top = Math.Max(from.Y, to.Y);
                double bottom = Math.Min(from.Y + from.Height, to.Y + to.Height);
                if (bottom <= top) return false;
                x = from.X + from.Width == to.X ? from.X + from.Width : from.X;
                y = (top + bottom) / 2.0;
                orientation = "Vertical";
                return true;
            }

            if (from.Y + from.Height == to.Y || to.Y + to.Height == from.Y)
            {
                double left = Math.Max(from.X, to.X);
                double right = Math.Min(from.X + from.Width, to.X + to.Width);
                if (right <= left) return false;
                x = (left + right) / 2.0;
                y = from.Y + from.Height == to.Y ? from.Y + from.Height : from.Y;
                orientation = "Horizontal";
                return true;
            }

            return false;
        }

        private static void FindDoorPointOnRoomEdge(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            DungeonGeometry.FindRoomEdgePoint(room, targetX, targetY, out x, out y, out orientation);
        }

        private bool SameConnection(DungeonConnectionRecord connection, string a, string b)
        {
            return connection != null
                && ((connection.FromRoomId == a && connection.ToRoomId == b)
                    || (connection.FromRoomId == b && connection.ToRoomId == a));
        }

        private void StockRoom(DungeonRoomRecord room, int level, string dungeonType, bool isLair, Random random, bool russian)
        {
            // Наполнение держится в одном месте, чтобы ручной редактор мог менять
            // результат без знания о таблицах монстров, ловушек и сокровищ.
            int roll = random.Next(1, 7);
            if (isLair && roll == 5) roll = 4;
            if (roll <= 2)
            {
                room.Kind = "Empty";
                room.Title = russian ? "Пустая комната" : "Empty chamber";
                room.Details = russian ? "Пыль, старые следы и пространство для исследования игроками." : "Dust, old marks, and room for player-driven exploration.";
                if (random.Next(100) < 15) room.Treasure = PickUnprotectedTreasure(level, random, russian);
                return;
            }

            if (roll <= 4)
            {
                room.Kind = "Monster";
                room.Title = russian ? "Занятая комната" : "Occupied chamber";
                DungeonMonsterPick monster = RollPlacedMonster(PickMonsterForDungeonLevel(DungeonLevelFromRecommendedLevel(level), dungeonType, random), random, isLair);
                AssignMonsterToRoom(room, monster, russian);
                room.Treasure = monster.IsLair ? PickMonsterTreasure(monster, random, russian) : "";
                room.Details = russian ? "Комната с подготовленной встречей." : "A stocked encounter room.";
                room.Details = AppendDungeonDetail(room.Details, MonsterXpDetail(monster, russian));
                return;
            }

            if (roll == 5)
            {
                room.Kind = "Trap";
                room.Title = russian ? "Опасность" : "Hazard";
                AssignTrapToRoom(room, RollTrap(level, random), russian);
                room.Treasure = random.Next(100) < 30 ? PickUnprotectedTreasure(level, random, russian) : "";
                room.Details = russian ? "Опасная комната без очевидного стража." : "A dangerous room without an obvious guardian.";
                return;
            }

            room.Kind = "Unique";
            room.Title = russian ? "Особенность" : "Unique feature";
            room.UniqueFeature = PickUniqueFeature(dungeonType, random, russian);
            room.Treasure = random.Next(100) < 25 ? PickTreasure(level, "Hoarder", random, russian) : "";
            room.Details = russian ? "Запоминающаяся комната с особой деталью данжа." : "A distinctive room intended as a memorable dungeon feature.";
        }

        private void EnsureSingleRoomLairIsOccupied(DungeonRecord dungeon, int recommendedLevel, string size, string dungeonType, Random random, bool russian)
        {
            if (!string.Equals(size, "Lair", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(dungeonType, "Monster lair", StringComparison.OrdinalIgnoreCase)) return;
            if (dungeon == null || dungeon.Levels == null) return;
            List<DungeonRoomRecord> rooms = dungeon.Levels
                .Where(level => level != null && level.Rooms != null)
                .SelectMany(level => level.Rooms)
                .Where(room => room != null)
                .ToList();
            if (rooms.Count == 0) return;
            if (rooms.Count != 1 && rooms.Any(room => string.Equals(room.Kind, "Monster", StringComparison.OrdinalIgnoreCase))) return;

            StockMonsterLairRoom(rooms[0], recommendedLevel + Math.Max(0, rooms[0].LevelNumber - 1), dungeonType, random, russian);
        }

        private void StockMonsterLairRoom(DungeonRoomRecord room, int level, string dungeonType, Random random, bool russian)
        {
            if (room == null) return;
            DungeonMonsterPick monster = RollPlacedMonster(PickMonsterForDungeonLevel(DungeonLevelFromRecommendedLevel(level), dungeonType, random), random, true);
            room.Kind = "Monster";
            room.Title = russian ? "Логово монстра" : "Monster lair";
            AssignMonsterToRoom(room, monster, russian);
            room.Treasure = PickMonsterTreasure(monster, random, russian);
            room.Trap = "";
            room.TrapKey = "";
            room.TrapLevel = 0;
            room.TrapTrigger = "";
            room.TrapEffect = "";
            room.UniqueFeature = "";
            room.Details = russian
                ? "Единственная комната логова: здесь находится основной обитатель и его сокровища."
                : "The lair's only mapped chamber: the resident monster is here with its treasure.";
            room.Details = AppendDungeonDetail(room.Details, MonsterXpDetail(monster, russian));
        }

        private List<DungeonEncounterRecord> BuildEncounterTables(DungeonRecord dungeon, int recommendedLevel, string dungeonType, Random random, bool russian)
        {
            List<DungeonEncounterRecord> table = new List<DungeonEncounterRecord>();
            foreach (DungeonLevelRecord level in dungeon.Levels ?? new List<DungeonLevelRecord>())
            {
                int dungeonLevel = DungeonLevelFromRecommendedLevel(recommendedLevel + level.LevelNumber - 1);
                for (int roll = 1; roll <= 12; roll++)
                {
                    int monsterLevel = MonsterLevelForDungeonRoll(dungeonLevel, roll);
                    DungeonMonsterPick monster = PickMonsterForMonsterLevel(monsterLevel, dungeonType, random);
                    table.Add(new DungeonEncounterRecord
                    {
                        DungeonLevel = level.LevelNumber,
                        Roll = roll,
                        MonsterLevel = monsterLevel,
                        Monster = russian ? LocalizeMonster(monster.Monster) : monster.Monster,
                        CountExpression = monster.CountExpression,
                        Notes = roll == 12 ? (russian ? "Пик опасности для этого уровня" : "Highest danger band for this level") : ""
                    });
                }
            }

            return table;
        }

        private int DungeonLevelFromRecommendedLevel(int recommendedLevel)
        {
            return DungeonCatalog.ClampDungeonLevel(recommendedLevel);
        }

        private int MonsterLevelForDungeonRoll(int dungeonLevel, int roll)
        {
            dungeonLevel = Math.Max(1, Math.Min(6, dungeonLevel));
            roll = Math.Max(1, Math.Min(12, roll));
            switch (dungeonLevel)
            {
                case 1: return roll <= 9 ? 1 : roll <= 11 ? 2 : 3;
                case 2: return roll <= 3 ? 1 : roll <= 9 ? 2 : roll <= 11 ? 3 : 4;
                case 3: return roll == 1 ? 1 : roll <= 3 ? 2 : roll <= 9 ? 3 : roll <= 11 ? 4 : 5;
                case 4: return roll == 1 ? 2 : roll <= 3 ? 3 : roll <= 9 ? 4 : roll <= 11 ? 5 : 6;
                case 5: return roll == 1 ? 3 : roll <= 3 ? 4 : roll <= 9 ? 5 : 6;
                default: return roll == 1 ? 4 : roll <= 3 ? 5 : 6;
            }
        }

        private DungeonMonsterPick PickMonsterForDungeonLevel(int dungeonLevel, string dungeonType, Random random)
        {
            return PickMonsterForMonsterLevel(dungeonLevel, dungeonType, random);
        }

        private DungeonMonsterPick PickMonsterForMonsterLevel(int monsterLevel, string dungeonType, Random random)
        {
            monsterLevel = Math.Max(1, Math.Min(6, monsterLevel));
            DungeonMonsterPick[] table = MonsterTable(monsterLevel);
            DungeonMonsterPick picked;
            if (dungeonType == "Giant insect hive" && random.Next(100) < 70)
            {
                picked = monsterLevel <= 2
                    ? CreateMonsterPick("Ant, Giant", "2d4")
                    : CreateMonsterPick("Wasp, Giant Parasitic", "1");
            }
            else if (dungeonType == "Humanoid warren" && random.Next(100) < 70)
            {
                picked = monsterLevel <= 2
                    ? CreateMonsterPick("Orc", "2d4")
                    : CreateMonsterPick(monsterLevel <= 4 ? "Ogre" : "Ettin", monsterLevel <= 4 ? "1d6" : "1d2");
            }
            else if (dungeonType == "Tomb" && random.Next(100) < 70)
            {
                picked = monsterLevel <= 2
                    ? CreateMonsterPick("Skeleton", "3d4")
                    : CreateMonsterPick(monsterLevel <= 4 ? "Mummy" : "Mummy Lord", monsterLevel <= 4 ? "1d4" : "1");
            }
            else if (dungeonType == "Natural caverns" && random.Next(100) < 45)
            {
                picked = monsterLevel <= 2
                    ? CreateMonsterPick("Centipede, Giant", "2d4")
                    : CreateMonsterPick(monsterLevel <= 4 ? "Troll" : "Worm, Giant Grey", monsterLevel <= 4 ? "1d8" : "1d3");
            }
            else
            {
                picked = table[random.Next(table.Length)];
            }

            return CloneMonsterPick(picked);
        }

        private static DungeonMonsterPick[] MonsterTable(int monsterLevel)
        {
            int index = Math.Max(1, Math.Min(6, monsterLevel)) - 1;
            return MonsterLevelTables[index];
        }

        private static DungeonMonsterPick[] MonsterRows(string[,] rows)
        {
            DungeonMonsterPick[] result = new DungeonMonsterPick[rows.GetLength(0)];
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                result[i] = new DungeonMonsterPick
                {
                    Monster = rows[i, 0],
                    CountExpression = rows[i, 1]
                };
            }

            return result;
        }

        private static DungeonMonsterPick CreateMonsterPick(string monster, string countExpression)
        {
            DungeonMonsterStats stats = FindMonsterStats(monster);
            return new DungeonMonsterPick
            {
                Monster = monster ?? "",
                CountExpression = string.IsNullOrWhiteSpace(countExpression) ? "1" : countExpression,
                XpEach = stats.XpEach,
                TreasureType = stats.TreasureType,
                LairPercent = stats.LairPercent
            };
        }

        private static DungeonMonsterPick CloneMonsterPick(DungeonMonsterPick source)
        {
            DungeonMonsterStats stats = FindMonsterStats(source == null ? "" : source.Monster);
            return new DungeonMonsterPick
            {
                Monster = source == null ? "" : source.Monster,
                CountExpression = source == null ? "1" : source.CountExpression,
                Count = source == null ? 0 : source.Count,
                XpEach = source == null || source.XpEach <= 0 ? stats.XpEach : source.XpEach,
                TotalXp = source == null ? 0 : source.TotalXp,
                TreasureType = source == null || string.IsNullOrWhiteSpace(source.TreasureType) ? stats.TreasureType : source.TreasureType,
                LairPercent = source == null || source.LairPercent <= 0 ? stats.LairPercent : source.LairPercent,
                IsLair = source != null && source.IsLair
            };
        }

        private static DungeonMonsterStats FindMonsterStats(string monster)
        {
            DungeonMonsterStats stats;
            if (MonsterStats == null) return Stats(0, "", 0);
            if (MonsterStats.TryGetValue(monster ?? "", out stats)) return stats;
            return Stats(0, "", 0);
        }

        private static DungeonMonsterPick RollPlacedMonster(DungeonMonsterPick picked, Random random, bool forceLair)
        {
            DungeonMonsterPick result = CloneMonsterPick(picked);
            result.Count = Math.Max(1, TreasureDice.RollExpression(random, result.CountExpression));
            result.TotalXp = Math.Max(0, result.Count * result.XpEach);
            result.IsLair = forceLair || TreasureDice.Chance(random, result.LairPercent);
            return result;
        }

        private void AssignMonsterToRoom(DungeonRoomRecord room, DungeonMonsterPick monster, bool russian)
        {
            if (room == null || monster == null) return;
            string displayName = russian ? LocalizeMonster(monster.Monster) : monster.Monster;
            room.Monster = monster.Count + " (" + monster.CountExpression + ") " + displayName;
            room.MonsterKey = monster.Monster;
            room.MonsterCountExpression = monster.CountExpression;
            room.MonsterCount = monster.Count;
            room.MonsterXpEach = monster.XpEach;
            room.MonsterXpTotal = monster.TotalXp;
            room.MonsterTreasureType = monster.TreasureType;
            room.MonsterLair = monster.IsLair;
        }

        private string PickMonsterTreasure(DungeonMonsterPick monster, Random random, bool russian)
        {
            if (monster == null || TreasureTypeIsNone(monster.TreasureType)) return "";
            string treasure = string.IsNullOrWhiteSpace(monster.TreasureType)
                ? new TreasureGenerator().GenerateCompactForDungeonTreasure(monster.TotalXp, "Hoarder", random, russian)
                : new TreasureGenerator().GenerateCompactForDungeonTreasureType(monster.TreasureType, monster.TotalXp, random, russian);
            return MonsterTreasurePrefix(monster, russian) + treasure;
        }

        private string PickUnprotectedTreasure(int level, Random random, bool russian)
        {
            int dungeonLevel = Math.Max(1, Math.Min(6, DungeonLevelFromRecommendedLevel(level)));
            int roll = random.Next(1, 7);
            string treasureType = UnprotectedTreasureTypes[dungeonLevel - 1, roll - 1];
            TreasureHoardResult hoard = new TreasureGenerator().Generate(new TreasureGenerationOptions
            {
                TableMode = TreasureTableMode.Classic,
                TreasureType = treasureType
            }, random);
            return (russian ? "Незащищенное сокровище d6=" : "Unprotected treasure d6=")
                + roll
                + ": "
                + new TreasureGenerator().FormatCompact(hoard, russian);
        }

        private string PickTreasure(int level, string preferredCategory, Random random, bool russian)
        {
            int dungeonLevel = DungeonLevelFromRecommendedLevel(level);
            int fallbackXp = 25 * dungeonLevel * dungeonLevel;
            return new TreasureGenerator().GenerateCompactForDungeonTreasure(fallbackXp, preferredCategory, random, russian);
        }

        private static bool TreasureTypeIsNone(string treasureType)
        {
            return string.IsNullOrWhiteSpace(treasureType)
                || string.Equals(treasureType.Trim(), "none", StringComparison.OrdinalIgnoreCase);
        }

        private static string MonsterTreasurePrefix(DungeonMonsterPick monster, bool russian)
        {
            string type = string.IsNullOrWhiteSpace(monster.TreasureType) ? "XP x4" : monster.TreasureType;
            return russian
                ? "Логово; XP " + monster.TotalXp + " (" + monster.Count + " x " + monster.XpEach + "), TT " + type + ": "
                : "Lair; XP " + monster.TotalXp + " (" + monster.Count + " x " + monster.XpEach + "), TT " + type + ": ";
        }

        private static string MonsterXpDetail(DungeonMonsterPick monster, bool russian)
        {
            if (monster == null) return "";
            string type = TreasureTypeIsNone(monster.TreasureType) ? (russian ? "нет" : "none") : monster.TreasureType;
            return russian
                ? "Монстры: " + monster.Count + " (" + monster.CountExpression + "), " + monster.XpEach + " XP каждый, всего " + monster.TotalXp + " XP; TT " + type + "; логово: " + (monster.IsLair ? "да" : "нет") + "."
                : "Monsters: " + monster.Count + " (" + monster.CountExpression + "), " + monster.XpEach + " XP each, " + monster.TotalXp + " XP total; TT " + type + "; lair: " + (monster.IsLair ? "yes" : "no") + ".";
        }

        private static string AppendDungeonDetail(string text, string addition)
        {
            if (string.IsNullOrWhiteSpace(addition)) return text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return addition;
            return text.TrimEnd() + " " + addition;
        }

        private string PickTrap(int level, Random random, bool russian)
        {
            return FormatTrap(RollTrap(level, random), russian);
        }

        private DungeonTrapPick RollTrap(int level, Random random)
        {
            random = random ?? new Random();
            DungeonTrapDefinition definition = TrapDefinitions[random.Next(TrapDefinitions.Length)];
            int trapLevel = DungeonLevelFromRecommendedLevel(level);
            trapLevel = Math.Max(1, Math.Min(6, trapLevel));
            string[] triggerKeys = definition.TriggerKeys == null || definition.TriggerKeys.Length == 0
                ? new[] { "PressurePlate" }
                : definition.TriggerKeys;
            string triggerKey = triggerKeys[random.Next(triggerKeys.Length)];
            return new DungeonTrapPick
            {
                Key = definition.Key,
                Name = definition.Name,
                RussianName = definition.RussianName,
                Level = trapLevel,
                Trigger = TrapTriggerName(triggerKey, false),
                RussianTrigger = TrapTriggerName(triggerKey, true),
                Effect = TrapEffectAtLevel(definition.Effects, trapLevel),
                RussianEffect = TrapEffectAtLevel(definition.RussianEffects, trapLevel)
            };
        }

        private void AssignTrapToRoom(DungeonRoomRecord room, DungeonTrapPick trap, bool russian)
        {
            if (room == null || trap == null) return;
            room.Trap = FormatTrap(trap, russian);
            room.TrapKey = trap.Key;
            room.TrapLevel = trap.Level;
            room.TrapTrigger = russian ? trap.RussianTrigger : trap.Trigger;
            room.TrapEffect = russian ? trap.RussianEffect : trap.Effect;
        }

        private static string FormatTrap(DungeonTrapPick trap, bool russian)
        {
            if (trap == null) return "";
            if (russian)
            {
                return trap.RussianName
                    + "; уровень ловушки " + trap.Level
                    + "; механизм: " + trap.RussianTrigger
                    + " (срабатывает на 1-2 на d6)"
                    + "; эффект: " + trap.RussianEffect;
            }

            return trap.Name
                + "; trap level " + trap.Level
                + "; trigger: " + trap.Trigger
                + " (activates on 1-2 on 1d6)"
                + "; effect: " + trap.Effect;
        }

        private static string TrapEffectAtLevel(string[] effects, int trapLevel)
        {
            if (effects == null || effects.Length == 0) return "";
            int index = Math.Max(1, Math.Min(6, trapLevel)) - 1;
            if (index >= effects.Length) index = effects.Length - 1;
            return effects[index] ?? "";
        }

        private static string TrapTriggerName(string key, bool russian)
        {
            switch ((key ?? "").Trim())
            {
                case "Counterweights": return russian ? "противовесы" : "counterweights";
                case "GearSystem": return russian ? "система шестерней" : "gear system";
                case "HiddenButton": return russian ? "скрытая кнопка" : "hidden button";
                case "LeverPulley": return russian ? "рычаги и блоки" : "levers and pulleys";
                case "PivotingFloor": return russian ? "поворотные плиты пола" : "pivoting floor tiles";
                case "PressurePlate": return russian ? "нажимная плита" : "pressure plate";
                case "Proximity": return russian ? "датчик близости" : "proximity trigger";
                case "Runes": return russian ? "руны" : "runes";
                case "Tripwire": return russian ? "растяжка" : "tripwire";
                case "WeightSensitive": return russian ? "платформа по весу" : "weight-sensitive platform";
                default: return russian ? "нажимная плита" : "pressure plate";
            }
        }

        private string PickUniqueFeature(string dungeonType, Random random, bool russian)
        {
            string[] common = russian ? RussianUniqueFeatures : EnglishUniqueFeatures;

            if (russian && dungeonType == "Wizard's dungeon") return "нестабильная магическая лаборатория";
            if (russian && dungeonType == "Temple") return "оскверненное святилище";
            if (russian && dungeonType == "Sewers") return "скрытый шлюз";
            if (russian && dungeonType == "Abandoned mine") return "обрушенный рудный подъемник";
            if (dungeonType == "Wizard's dungeon") return "unstable arcane laboratory";
            if (dungeonType == "Temple") return "desecrated shrine";
            if (dungeonType == "Sewers") return "hidden sluice gate";
            if (dungeonType == "Abandoned mine") return "collapsed ore lift";
            return common[random.Next(common.Length)];
        }

        private string LocalizeMonster(string monster)
        {
            switch ((monster ?? "").Trim().ToLowerInvariant())
            {
                case "goblin": return "гоблины";
                case "kobold": return "кобольды";
                case "morlock": return "морлоки";
                case "orc": return "орки";
                case "beastman, orc": return "орки-зверолюды";
                case "beastman, gnoll": return "гноллы-зверолюды";
                case "beastman, hobgoblin": return "хобгоблины-зверолюды";
                case "beetle, luminous": return "светящиеся жуки";
                case "centipede, giant": return "гигантские многоножки";
                case "ferret, giant": return "гигантские хорьки";
                case "rat, giant": return "гигантские крысы";
                case "men, brigand": return "разбойники";
                case "strige": return "стирджи";
                case "skeleton": return "скелеты";
                case "lizardman": return "людоящеры";
                case "troglodyte": return "троглодиты";
                case "fly, giant carnivorous": return "гигантские хищные мухи";
                case "locust, cavern": return "пещерная саранча";
                case "bat, giant": return "гигантские летучие мыши";
                case "snake, viper": return "гадюки";
                case "attercop, foul": return "скверные аттеркопы";
                case "ghoul, grave": return "могильные упыри";
                case "zombie": return "зомби";
                case "bugbear": return "багбиры";
                case "lycanthrope, werewolf": return "ликантропы-волки";
                case "hobgholl": return "хобголлы";
                case "ant, giant": return "гигантские муравьи";
                case "scorpion, giant": return "гигантские скорпионы";
                case "wolf, dire": return "лютые волки";
                case "carrion horror": return "падальные ужасы";
                case "attercop, hideous": return "омерзительные аттеркопы";
                case "spriggan": return "спригганы";
                case "lycanthrope, wereboar": return "ликантропы-кабаны";
                case "lycanthrope, weretiger": return "ликантропы-тигры";
                case "acanthaspis, giant": return "гигантские акантасписы";
                case "bear, cave": return "пещерные медведи";
                case "spider, giant tarantula": return "гигантские тарантулы";
                case "snake, python": return "питоны";
                case "attercop, monstrous": return "чудовищные аттеркопы";
                case "gargoyle": return "гаргульи";
                case "ettin": return "эттины";
                case "giant, hill": return "холмовые гиганты";
                case "giant, stone": return "каменные гиганты";
                case "amphisbaena": return "амфисбена";
                case "wasp, giant parasitic": return "гигантская паразитическая оса";
                case "worm, giant grey": return "гигантский серый червь";
                case "ooze, ochre": return "охряная слизь";
                case "arane": return "араны";
                case "basilisk, petrifying": return "петрифицирующие василиски";
                case "necropede": return "некропеды";
                case "cyclops": return "циклопы";
                case "giant, fire or frost": return "огненные или ледяные гиганты";
                case "hag": return "карги";
                case "titan, lesser": return "младшие титаны";
                case "blob, black": return "черные слизи";
                case "crocodile, giant": return "гигантские крокодилы";
                case "skittering maw": return "скиттеринг мо";
                case "worm, giant black": return "гигантский черный червь";
                case "attercop, demonic": return "демонические аттеркопы";
                case "gorgon": return "горгоны";
                case "mummy lord": return "владыка-мумия";
                case "goblins": return "гоблины";
                case "giant rats": return "гигантские крысы";
                case "skeletons": return "скелеты";
                case "zombies": return "зомби";
                case "kobolds": return "кобольды";
                case "stirges": return "стирджи";
                case "spitting cobra": return "плюющаяся кобра";
                case "murder maggot": return "смертоносная личинка";
                case "orcs": return "орки";
                case "hobgoblins": return "хобгоблины";
                case "giant ants": return "гигантские муравьи";
                case "giant wasps": return "гигантские осы";
                case "giant spiders": return "гигантские пауки";
                case "ghouls": return "упыри";
                case "mustard mold": return "горчичная плесень";
                case "violet fungus": return "фиолетовый гриб";
                case "wood golem": return "деревянный голем";
                case "ogre": return "огр";
                case "ogres": return "огры";
                case "minotaur": return "минотавр";
                case "shadow": return "тень";
                case "doppelganger": return "двойник";
                case "gelatinous mass": return "желатиновая масса";
                case "grey goo": return "серая слизь";
                case "giant scorpion": return "гигантский скорпион";
                case "animated statue": return "ожившая статуя";
                case "troll": return "тролль";
                case "rust beast": return "ржавильщик";
                case "ochre ooze": return "охряная слизь";
                case "mummy": return "мумия";
                case "bronze statue": return "бронзовая статуя";
                case "wight": return "умертвие";
                case "wyvern": return "виверна";
                case "giant serpent": return "гигантская змея";
                case "hill giant": return "холмовой гигант";
                case "bone golem": return "костяной голем";
                case "invisible stalker": return "невидимый охотник";
                case "spectre": return "призрак";
                case "basilisk": return "василиск";
                case "chimera": return "химера";
                case "hellion": return "исчадие";
                case "black blob": return "черная слизь";
                case "dragon": return "дракон";
                case "elemental": return "элементаль";
                case "efreeti": return "ифрит";
                case "djinni": return "джинн";
                case "vampire": return "вампир";
                case "marid": return "марид";
                case "demon": return "демон";
                case "child of Nasga": return "дитя Насги";
                default: return monster;
            }
        }

        private int PickLevelCount(string size, Random random)
        {
            if (size == "Lair") return 1;
            if (size == "Small") return 1;
            if (size == "Standard") return random.Next(100) < 65 ? 1 : 2;
            if (size == "Large") return random.Next(2, 4);
            return random.Next(4, 7);
        }

        private int PickRoomCount(string size, int levels, Random random)
        {
            if (size == "Lair") return random.Next(1, 4);
            if (size == "Small") return random.Next(2, 11);
            if (size == "Standard") return random.Next(15, 26);
            if (size == "Large") return random.Next(60, 121);
            return random.Next(140, 241);
        }

        public static string NormalizeSize(string size)
        {
            if (string.Equals(size, "Lair", StringComparison.OrdinalIgnoreCase)) return "Lair";
            if (string.Equals(size, "Small", StringComparison.OrdinalIgnoreCase)) return "Small";
            if (string.Equals(size, "Large", StringComparison.OrdinalIgnoreCase)) return "Large";
            if (string.Equals(size, "Megadungeon", StringComparison.OrdinalIgnoreCase)) return "Megadungeon";
            return "Standard";
        }

        private string BuildDungeonName(string dungeonType, bool russian, Random random)
        {
            string type = DungeonCatalog.LocalizeDungeonType(dungeonType, russian);
            string[] prefixes = russian ? RussianDungeonNamePrefixes : EnglishDungeonNamePrefixes;
            string prefix = prefixes[random.Next(prefixes.Length)];
            return russian ? type + " " + prefix + " холма" : prefix + " " + type;
        }

        private string BuildDungeonNotes(string dungeonType, string size, bool russian)
        {
            if (russian)
            {
                return "Сгенерировано по упрощенной процедуре ACKS: тип " + DungeonCatalog.LocalizeDungeonType(dungeonType, true)
                    + ", размер " + size + ". Комнаты можно править вручную.";
            }

            return "Generated with a simplified ACKS dungeon procedure: " + dungeonType + ", " + size + ". Rooms can be edited manually.";
        }

        private static void MoveGeneratedRoomToFreeSpot(DungeonLevelRecord level, List<DungeonRoomRecord> placed, DungeonRoomRecord room, int preferredX, int preferredY)
        {
            if (level == null || room == null) return;
            int maxX = Math.Max(0, level.Width - Math.Max(1, room.Width) - 1);
            int maxY = Math.Max(0, level.Height - Math.Max(1, room.Height) - 1);
            int startX = ClampInt(preferredX, 0, maxX);
            int startY = ClampInt(preferredY, 0, maxY);
            int maxRadius = Math.Max(level.Width, level.Height);
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int x = ClampInt(startX + dx, 0, maxX);
                        int y = ClampInt(startY + dy, 0, maxY);
                        if (!CanPlaceGeneratedRoom(level, placed, room, x, y)) continue;
                        room.X = x;
                        room.Y = y;
                        return;
                    }
                }
            }

            room.X = startX;
            room.Y = startY;
        }

        private static bool CanPlaceGeneratedRoom(DungeonLevelRecord level, List<DungeonRoomRecord> placed, DungeonRoomRecord room, int x, int y)
        {
            if (level == null || room == null) return false;
            int width = Math.Max(1, room.Width);
            int height = Math.Max(1, room.Height);
            if (x < 0 || y < 0 || x + width >= level.Width || y + height >= level.Height) return false;
            foreach (DungeonRoomRecord other in placed ?? new List<DungeonRoomRecord>())
            {
                if (other == null) continue;
                if (RectanglesTooClose(x, y, width, height, other.X, other.Y, Math.Max(1, other.Width), Math.Max(1, other.Height), 1)) return false;
            }

            return true;
        }

        private static bool RectanglesTooClose(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh, int clearance)
        {
            return ax - clearance < bx + bw
                && ax + aw + clearance > bx
                && ay - clearance < by + bh
                && ay + ah + clearance > by;
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private string RoomKey(int x, int y)
        {
            return x + "," + y;
        }

        private static string BuildStableSeedKey(DungeonGenerationOptions options)
        {
            options = options ?? new DungeonGenerationOptions();
            string size = NormalizeSize(options.Size);
            int level = DungeonCatalog.ClampDungeonLevel(options.RecommendedLevel);
            string dungeonType = string.IsNullOrWhiteSpace(options.DungeonType)
                ? ""
                : DungeonCatalog.NormalizeDungeonType(options.DungeonType);

            // Полный ключ защищает от ситуации, когда тот же seed игнорирует уровень, размер или тип данжа.
            return (options.Seed ?? "")
                + "|type=" + dungeonType
                + "|size=" + size
                + "|level=" + level.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int StableSeed(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text ?? "")
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return hash == int.MinValue ? 1 : Math.Abs(hash);
            }
        }
    }
}
