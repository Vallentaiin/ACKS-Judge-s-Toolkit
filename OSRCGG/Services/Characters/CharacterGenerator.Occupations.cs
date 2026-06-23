using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class CharacterGenerator
    {
        // Таблицы городских занятий NPC изолированы от UI-сценариев генерации.
        private T RollOnTable<T>(List<RollRange<T>> table, int die)
        {
            int roll = characterRandom.Next(1, die + 1);
            RollRange<T> result = table.FirstOrDefault(r => r.Contains(roll));
            return result == null ? table.Last().Value : result.Value;
        }

        private NpcOccupationResult RollGeneralStreetOccupation()
        {
            while (true)
            {
                int roll = characterRandom.Next(1, 101);
                if ((roll >= 84 && roll <= 85) || (roll >= 92 && roll <= 93) ||
                    (roll >= 96 && roll <= 97) || roll == 99)
                {
                    continue;
                }

                if (roll <= 26) return RollLaborerOccupation();
                if (roll <= 60) return RollArtisanOccupation("Artisan");
                if (roll <= 74) return RollMerchantOccupation("Merchant");
                if (roll <= 76) return RollSpecialistOccupation();
                if (roll <= 81) return RollHostellerOccupation();
                if (roll <= 83) return RollEntertainerOccupation();
                if (roll <= 91) return RollMercenaryOccupation();
                if (roll <= 95) return RollEcclesiasticOccupation();
                if (roll == 98) return RollMagicianOccupation();
                return new NpcOccupationResult("Professional", "Patrician");
            }
        }

        private NpcOccupationResult RollLaborerOccupation()
        {
            return new NpcOccupationResult("Laborer", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 3, "Barber"),
                new RollRange<string>(4, 6, "Bath Attendant/Masseuse"),
                new RollRange<string>(7, 8, "Bricklayer"),
                new RollRange<string>(9, 19, "Cook"),
                new RollRange<string>(20, 22, "Dockworker"),
                new RollRange<string>(23, 25, "Fuller/Launderer"),
                new RollRange<string>(26, 32, "Gondolier/Rower"),
                new RollRange<string>(33, 34, "Gongfarmer/Streetcleaner"),
                new RollRange<string>(35, 40, "Hawker"),
                new RollRange<string>(41, 48, "Hostler/Stablehand"),
                new RollRange<string>(49, 51, "Maidservant"),
                new RollRange<string>(52, 59, "Prostitute"),
                new RollRange<string>(60, 60, "Ratcatcher"),
                new RollRange<string>(61, 61, "Roofer/Tiler"),
                new RollRange<string>(62, 64, "Sailor/Fisher"),
                new RollRange<string>(65, 73, "Scullion"),
                new RollRange<string>(74, 75, "Sawyer/Woodcutter"),
                new RollRange<string>(76, 78, "Teamster"),
                new RollRange<string>(79, 90, "Tavernworker"),
                new RollRange<string>(91, 100, "Unskilled Laborer")
            }, 100));
        }

        private NpcOccupationResult RollMerchantOccupation(string category)
        {
            return new NpcOccupationResult(category, RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 1, "Bookseller"),
                new RollRange<string>(2, 6, "Chandler/Upholder"),
                new RollRange<string>(7, 8, "Coppermonger"),
                new RollRange<string>(9, 20, "Cornmonger"),
                new RollRange<string>(21, 31, "Draper"),
                new RollRange<string>(32, 38, "Fishmonger"),
                new RollRange<string>(39, 44, "Fripperer"),
                new RollRange<string>(45, 46, "Furrier"),
                new RollRange<string>(47, 48, "Greengrocer"),
                new RollRange<string>(49, 52, "Horsemonger"),
                new RollRange<string>(53, 61, "Ironmonger"),
                new RollRange<string>(62, 66, "Lawyer"),
                new RollRange<string>(67, 75, "Lumbermonger"),
                new RollRange<string>(76, 80, "Mercer"),
                new RollRange<string>(81, 82, "Oilmonger"),
                new RollRange<string>(83, 88, "Peltmonger/Skinner"),
                new RollRange<string>(89, 91, "Poulterer"),
                new RollRange<string>(92, 95, "Salter/Pepperer"),
                new RollRange<string>(96, 100, "Vintner")
            }, 100));
        }

        private NpcOccupationResult RollArtisanOccupation(string category)
        {
            return new NpcOccupationResult(category, RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 2, "Apothecary"),
                new RollRange<string>(3, 4, "Armorer"),
                new RollRange<string>(5, 6, "Baker"),
                new RollRange<string>(7, 8, "Blacksmith"),
                new RollRange<string>(9, 9, "Bookbinder"),
                new RollRange<string>(10, 11, "Bowyer/Fletcher"),
                new RollRange<string>(12, 14, "Brewer"),
                new RollRange<string>(15, 16, "Brickmaker"),
                new RollRange<string>(17, 21, "Butcher"),
                new RollRange<string>(22, 22, "Cabinetmaker"),
                new RollRange<string>(23, 25, "Candlemaker"),
                new RollRange<string>(26, 27, "Capper/Hatter"),
                new RollRange<string>(28, 28, "Carpenter"),
                new RollRange<string>(29, 31, "Chaloner/Tapicer"),
                new RollRange<string>(32, 37, "Clothmaker"),
                new RollRange<string>(38, 39, "Cobbler/Cordwainer"),
                new RollRange<string>(40, 41, "Confectioner"),
                new RollRange<string>(42, 42, "Cooper"),
                new RollRange<string>(43, 44, "Coppersmith"),
                new RollRange<string>(45, 45, "Corder/Ropemaker"),
                new RollRange<string>(46, 48, "Decorative Artist"),
                new RollRange<string>(49, 49, "Florist"),
                new RollRange<string>(50, 50, "Gemcutter"),
                new RollRange<string>(51, 52, "Glassworker"),
                new RollRange<string>(53, 56, "Goldsmith"),
                new RollRange<string>(57, 58, "Hornworker"),
                new RollRange<string>(59, 60, "Illuminator"),
                new RollRange<string>(61, 61, "Jeweler"),
                new RollRange<string>(62, 62, "Locksmith"),
                new RollRange<string>(63, 64, "Mason"),
                new RollRange<string>(65, 65, "Parchmentmaker"),
                new RollRange<string>(66, 66, "Perfumer"),
                new RollRange<string>(67, 69, "Potter"),
                new RollRange<string>(70, 71, "Saddler/Fuster"),
                new RollRange<string>(72, 75, "Scribe"),
                new RollRange<string>(76, 76, "Shipwright"),
                new RollRange<string>(77, 77, "Silversmith"),
                new RollRange<string>(78, 83, "Spinner"),
                new RollRange<string>(84, 89, "Tailor/Seamstress"),
                new RollRange<string>(90, 93, "Tanner/Tawer"),
                new RollRange<string>(94, 94, "Taxidermist"),
                new RollRange<string>(95, 96, "Tinker/Toymaker"),
                new RollRange<string>(97, 97, "Wainwright"),
                new RollRange<string>(98, 99, "Weaponsmith"),
                new RollRange<string>(100, 100, "Wheelwright")
            }, 100));
        }

        private NpcOccupationResult RollHostellerOccupation()
        {
            return new NpcOccupationResult("Hosteller", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 60, "Cantinakeeper"),
                new RollRange<string>(61, 85, "Tavernkeeper"),
                new RollRange<string>(86, 95, "Innkeeper"),
                new RollRange<string>(96, 100, "Brothelkeeper")
            }, 100));
        }

        private NpcOccupationResult RollEntertainerOccupation()
        {
            string occupation = RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 20, "Actor"),
                new RollRange<string>(21, 49, "Musician"),
                new RollRange<string>(50, 73, "Dancer"),
                new RollRange<string>(74, 100, "Carouser")
            }, 100);
            return new NpcOccupationResult("Performer", occupation);
        }

        private NpcOccupationResult RollEcclesiasticOccupation()
        {
            return new NpcOccupationResult("Minor Ecclesiastic", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 2, "Anchorite"),
                new RollRange<string>(3, 10, "Oracle"),
                new RollRange<string>(11, 20, "Almsgiver/Missionary"),
                new RollRange<string>(21, 35, "Village Witch"),
                new RollRange<string>(36, 65, "Seminarian"),
                new RollRange<string>(66, 80, "Hospitalist/Medician"),
                new RollRange<string>(81, 90, "Sacred Courtesan"),
                new RollRange<string>(91, 97, "Inquisitor"),
                new RollRange<string>(98, 100, "Cultist/Heretic")
            }, 100));
        }

        private NpcOccupationResult RollMagicianOccupation()
        {
            return new NpcOccupationResult("Minor Magician", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 2, "Augur"),
                new RollRange<string>(3, 10, "Occultist"),
                new RollRange<string>(11, 20, "Astrologer"),
                new RollRange<string>(21, 35, "Hedge Magician"),
                new RollRange<string>(36, 65, "Apprentice Mage"),
                new RollRange<string>(66, 80, "Prestidigitator"),
                new RollRange<string>(81, 90, "Charlatan"),
                new RollRange<string>(91, 97, "Failed Apprentice"),
                new RollRange<string>(98, 100, "Apprentice Warlock")
            }, 100));
        }

        private NpcOccupationResult RollMercenaryOccupation()
        {
            return new NpcOccupationResult("Mercenary", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 30, "Light Infantry"),
                new RollRange<string>(31, 45, "Heavy Infantry"),
                new RollRange<string>(46, 65, characterRandom.Next(2) == 0 ? "Bowman" : "Slinger"),
                new RollRange<string>(66, 72, "Crossbowman"),
                new RollRange<string>(73, 79, "Longbowman"),
                new RollRange<string>(80, 85, "Light Cavalry"),
                new RollRange<string>(86, 89, "Horse Archer"),
                new RollRange<string>(90, 93, "Medium Cavalry"),
                new RollRange<string>(94, 97, "Heavy Cavalry"),
                new RollRange<string>(98, 100, "Cataphract")
            }, 100));
        }

        private NpcOccupationResult RollSpecialistOccupation()
        {
            return new NpcOccupationResult("Specialist", RollOnTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 6, "Alchemist (apprentice)"),
                new RollRange<string>(7, 9, "Alchemist (assistant)"),
                new RollRange<string>(10, 12, "Alchemist"),
                new RollRange<string>(13, 17, "Animal Trainer (domestic)"),
                new RollRange<string>(18, 20, "Animal Trainer (wild)"),
                new RollRange<string>(21, 22, "Animal Trainer (giant)"),
                new RollRange<string>(23, 23, "Animal Trainer (fantastic)"),
                new RollRange<string>(24, 26, "Artillerist"),
                new RollRange<string>(27, 32, "Engineer (apprentice)"),
                new RollRange<string>(33, 35, "Engineer (assistant)"),
                new RollRange<string>(36, 37, "Engineer"),
                new RollRange<string>(38, 43, "Healer"),
                new RollRange<string>(44, 46, "Healer (Physicker)"),
                new RollRange<string>(47, 48, "Healer (Chirugeon)"),
                new RollRange<string>(49, 53, "Marshal (Light Infantry)"),
                new RollRange<string>(54, 56, "Marshal (Bow)"),
                new RollRange<string>(57, 59, "Marshal (Heavy Infantry)"),
                new RollRange<string>(60, 62, "Marshal (Light Cavalry)"),
                new RollRange<string>(63, 63, "Marshal (Heavy Cavalry)"),
                new RollRange<string>(64, 64, "Marshal (Horse Archer)"),
                new RollRange<string>(65, 65, "Marshal (Cataphract)"),
                new RollRange<string>(66, 70, "Navigator"),
                new RollRange<string>(71, 73, "Quartermaster"),
                new RollRange<string>(74, 79, "Sage (apprentice)"),
                new RollRange<string>(80, 82, "Sage (assistant)"),
                new RollRange<string>(83, 84, "Sage"),
                new RollRange<string>(85, 89, "Scout (Pathfinder)"),
                new RollRange<string>(90, 94, "Scout (Surveyor)"),
                new RollRange<string>(95, 97, "Siege Engineer"),
                new RollRange<string>(98, 100, "Ship Captain")
            }, 100));
        }
    }
}
