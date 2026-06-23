using System.Collections.Generic;

namespace OSRCGG
{
    // Запрос описывает только входы генератора, поэтому сервис не зависит от WinForms-контролов.
    internal sealed class CharacterGenerationRequest
    {
        public bool IsEnglish { get; set; }
        public string CurrentKind { get; set; }
        public string CurrentClassName { get; set; }
        public int RequestedLevel { get; set; }
        public int MaximumLevel { get; set; }
        public Dictionary<string, int> Attributes { get; set; }
    }

    // Результат генерации переносит наружу готовый снимок персонажа без знания о том,
    // где он будет показан: во вкладке, тесте или будущем импортере.
    internal sealed class CharacterGenerationResult
    {
        public string Kind { get; set; }
        public string ClassName { get; set; }
        public string Occupation { get; set; }
        public string Template { get; set; }
        public string Alignment { get; set; }
        public string Homeland { get; set; }
        public string Sex { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int HitPoints { get; set; }
        public int ArmorClass { get; set; }
        public int Age { get; set; }
        public Dictionary<string, int> Attributes { get; set; }
        public string Proficiencies { get; set; }
        public string Languages { get; set; }
        public string Spells { get; set; }
        public string Equipment { get; set; }
        public string Appearance { get; set; }
        public string Background { get; set; }
        public string Notes { get; set; }
        public bool GenerateName { get; set; }
    }
}
