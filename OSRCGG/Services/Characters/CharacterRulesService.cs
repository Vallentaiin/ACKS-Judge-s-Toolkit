using System;
using System.Linq;

namespace OSRCGG
{
    public static class CharacterRulesService
    {
        public static int AttributeBonus(int score)
        {
            if (score <= 3) return -3;
            if (score <= 5) return -2;
            if (score <= 8) return -1;
            if (score <= 12) return 0;
            if (score <= 15) return 1;
            if (score <= 17) return 2;
            return 3;
        }

        public static string NormalizeProficiencyName(string proficiency)
        {
            if (string.IsNullOrWhiteSpace(proficiency)) return "";

            string value = proficiency.Trim();
            int paren = value.IndexOf('(');
            if (paren > 0) value = value.Substring(0, paren).Trim();

            int number = value.IndexOfAny("0123456789".ToCharArray());
            if (number > 0) value = value.Substring(0, number).Trim();

            return value;
        }

        public static string[] SplitProficiencies(string proficiencies)
        {
            if (string.IsNullOrWhiteSpace(proficiencies)) return new string[0];

            return proficiencies
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }

        public static int GeneralProficiencyCountForLevel(int level, int intelligenceBonus)
        {
            int count = 1 + Math.Max(0, intelligenceBonus);
            if (level >= 5) count++;
            if (level >= 9) count++;
            if (level >= 13) count++;
            return count;
        }

        public static int ClassProficiencyCountForLevel(string className, int level)
        {
            int count = 1;
            if (IsSlowProficiencyClass(className))
            {
                if (level >= 6) count++;
                if (level >= 12) count++;
                return count;
            }

            if (IsMediumProficiencyClass(className))
            {
                if (level >= 4) count++;
                if (level >= 8) count++;
                if (level >= 12) count++;
                return count;
            }

            if (level >= 3) count++;
            if (level >= 6) count++;
            if (level >= 9) count++;
            if (level >= 12) count++;
            return count;
        }

        private static bool IsSlowProficiencyClass(string className)
        {
            return ContainsClassPart(className, "Mage") ||
                   ContainsClassPart(className, "Wonderworker") ||
                   ContainsClassPart(className, "Shaman") ||
                   ContainsClassPart(className, "Warlock") ||
                   ContainsClassPart(className, "Witch");
        }

        private static bool IsMediumProficiencyClass(string className)
        {
            return ContainsClassPart(className, "Thief") ||
                   ContainsClassPart(className, "Bard") ||
                   ContainsClassPart(className, "Nightblade") ||
                   ContainsClassPart(className, "Bladedancer") ||
                   ContainsClassPart(className, "Priestess") ||
                   ContainsClassPart(className, "Craftpriest");
        }

        private static bool ContainsClassPart(string className, string part)
        {
            return (className ?? "").IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
