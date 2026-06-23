using System;
using System.Collections.Generic;

namespace OSRCGG
{
    internal sealed partial class CharacterGenerator
    {
        // Экипировка и магические предметы NPC отделены от заполнения UI-полей.
        private string GetZeroLevelNpcEquipment(NpcOccupationResult occupation)
        {
            if (occupation.Category == "Mercenary")
            {
                return L("Mercenary troop equipment: ", "Снаряжение типа войск: ") +
                    GetMercenaryTroopEquipment(occupation.Occupation);
            }

            return L("Tools, clothing, and possessions appropriate to occupation.", "Инструменты, одежда и имущество по профессии.");
        }

        private string GetMercenaryTroopEquipment(string occupation)
        {
            Dictionary<string, string> equipment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Light Infantry", "3 javelins, short sword, shield, leather armor"},
                {"Heavy Infantry", "spear, short sword, shield, banded plate armor"},
                {"Slinger", "sling, short sword, shield, 20 sling stones"},
                {"Bowman", "short bow, short sword, leather armor, 20 arrows"},
                {"Crossbowman", "arbalest, short sword, shield, chain mail, 20 bolts"},
                {"Longbowman", "long bow, short sword, chain mail, 20 arrows"},
                {"Light Cavalry", "3 javelins, sword, shield, scale armor, light warhorse"},
                {"Horse Archer", "composite bow, scimitar, leather armor, light warhorse, 20 arrows"},
                {"Medium Cavalry", "lance, sword, shield, lamellar armor, scale-barded medium warhorse"},
                {"Heavy Cavalry", "lance, sword, shield, plate armor, chain-barded medium warhorse"},
                {"Cataphract", "composite bow, lance, sword, plate armor, lamellar-barded medium warhorse, 20 arrows"}
            };

            string result;
            return equipment.TryGetValue(occupation, out result)
                ? result
                : L("Weapons and armor appropriate to troop type.", "Оружие и броня по типу войск.");
        }

        private string AppendNpcMagicItems(string equipment, int level)
        {
            List<string> items = GenerateNpcMagicItems(level);
            if (items.Count == 0) return equipment;

            return equipment + Environment.NewLine +
                L("Magic items: ", "Магические предметы: ") + string.Join(", ", items);
        }

        private List<string> GenerateNpcMagicItems(int level)
        {
            List<string> items = new List<string>();
            AddMagicItems(items, "Common", GetNpcMagicItemCount(level, "Common"));
            AddMagicItems(items, "Uncommon", GetNpcMagicItemCount(level, "Uncommon"));
            AddMagicItems(items, "Rare", GetNpcMagicItemCount(level, "Rare"));
            AddMagicItems(items, "Very Rare", GetNpcMagicItemCount(level, "Very Rare"));
            AddMagicItems(items, "Legendary", GetNpcMagicItemCount(level, "Legendary"));
            return items;
        }

        private void AddMagicItems(List<string> items, string rarity, int count)
        {
            for (int i = 0; i < count; i++)
            {
                items.Add(rarity + ": " + RollMagicItem(rarity));
            }
        }

        private int GetNpcMagicItemCount(int level, string rarity)
        {
            level = Math.Max(0, Math.Min(14, level));
            if (rarity == "Common")
            {
                if (level == 0) return Chance(1) ? 1 : 0;
                if (level == 1) return Chance(30) ? 1 : 0;
                if (level == 2) return Chance(90) ? 1 : 0;
                if (level == 3) return 1;
                if (level == 4) return Math.Max(0, RollDice(1, 4) - 1);
                if (level == 5) return 2;
                if (level == 6) return 4;
                if (level == 7) return 4;
                if (level == 8) return 5;
                if (level == 9) return 5;
                if (level == 10) return 5;
                if (level == 11) return 7;
                if (level == 12) return 8;
                return 10;
            }
            if (rarity == "Uncommon")
            {
                if (level <= 2) return 0;
                if (level == 3) return Chance(15) ? 1 : 0;
                if (level == 4) return Chance(40) ? 1 : 0;
                if (level == 5) return 1;
                if (level == 6) return 2;
                if (level == 7) return 2;
                if (level == 8) return 3;
                if (level == 9) return 3;
                if (level == 10) return 5;
                if (level == 11) return 7;
                if (level == 12) return 7;
                return 10;
            }
            if (rarity == "Rare")
            {
                if (level <= 6) return 0;
                if (level == 7) return Chance(66) ? 1 : 0;
                if (level == 8) return 1;
                if (level == 9) return 2;
                if (level == 10) return 3;
                if (level == 11) return 7;
                if (level == 12) return 7;
                if (level == 13) return 9;
                return 10;
            }
            if (rarity == "Very Rare")
            {
                if (level <= 7) return 0;
                if (level == 8) return Chance(10) ? 1 : 0;
                if (level == 9) return Chance(50) ? 1 : 0;
                if (level == 10) return Chance(75) ? 1 : 0;
                if (level == 11) return 2;
                if (level == 12) return 4;
                if (level == 13) return 5;
                return 10;
            }
            if (rarity == "Legendary")
            {
                if (level <= 12) return 0;
                if (level == 13) return Math.Max(0, RollDice(1, 4) - 1);
                return 6;
            }
            return 0;
        }

        private bool Chance(int percent)
        {
            return characterRandom.Next(1, 101) <= percent;
        }

        private string RollMagicItem(string rarity)
        {
            return MagicItemCatalog.RollByRarity(rarity, characterRandom);
        }
    }
}