using System.Collections.Generic;

namespace OSRCGG
{
    internal enum TreasureTableMode
    {
        Classic,
        Heroic
    }

    internal enum TreasureGemKind
    {
        Ornamental,
        Gem,
        Brilliant
    }

    internal enum TreasureJewelryKind
    {
        Trinket,
        Jewelry,
        Regalia
    }

    internal sealed class TreasureGenerationOptions
    {
        public TreasureTableMode TableMode { get; set; }
        public string TreasureType { get; set; }
        public bool RussianOutput { get; set; }

        public TreasureGenerationOptions()
        {
            TableMode = TreasureTableMode.Classic;
            TreasureType = "A";
            RussianOutput = false;
        }
    }

    internal sealed class TreasureHoardResult
    {
        public string TreasureType { get; set; }
        public string TreasureCategory { get; set; }
        public TreasureTableMode TableMode { get; set; }
        public int AverageValueGp { get; set; }
        public int AverageMagicItemValueGp { get; set; }
        public int EstimatedValueGp { get; set; }
        public List<TreasureEntry> Entries { get; private set; }

        public TreasureHoardResult()
        {
            TreasureType = "A";
            TreasureCategory = "";
            TableMode = TreasureTableMode.Classic;
            Entries = new List<TreasureEntry>();
        }
    }

    internal sealed class TreasureEntry
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public int ValueGp { get; set; }

        public TreasureEntry()
        {
            Category = "";
            Description = "";
        }
    }

    internal sealed class TreasureTypeSummary
    {
        public string TreasureType { get; private set; }
        public string Category { get; private set; }
        public int AverageValueGp { get; private set; }

        public TreasureTypeSummary(string treasureType, string category, int averageValueGp)
        {
            TreasureType = treasureType;
            Category = category;
            AverageValueGp = averageValueGp;
        }
    }
}
