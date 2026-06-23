using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OSRCGG
{
    public class MapSettlementRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MarketClass { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        public string Race { get; set; }
        public string LandValue { get; set; }
        public double[] BaseDemands { get; set; }
        public double[] CurrentDemands { get; set; }
        public DateTime UpdatedAt { get; set; }

        public MapSettlementRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            MarketClass = 6;
            Q = -1;
            R = -1;
            Race = "Human";
            LandValue = "";
            BaseDemands = new double[AcksRules.DemandCount];
            CurrentDemands = new double[AcksRules.DemandCount];
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
                if (MarketClass < 1 || MarketClass > 6) return name;
                return name + " [Class " + AcksRules.ToRoman(MarketClass) + "]";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static string ToRoman(int value)
        {
            return AcksRules.ToRoman(value);
        }
    }

    public class HexCellRecord
    {
        public int Q { get; set; }
        public int R { get; set; }
        public string Terrain { get; set; }
        public string Elevation { get; set; }
        public string Water { get; set; }
        public string WaterFeatureName { get; set; }

        public HexCellRecord()
        {
            Terrain = "Grasslands";
            Elevation = "Plains";
            Water = "None";
            WaterFeatureName = "";
        }
    }

    public class MapEdgeRecord
    {
        public int AQ { get; set; }
        public int AR { get; set; }
        public int BQ { get; set; }
        public int BR { get; set; }
        public string Kind { get; set; }
        public string FeatureName { get; set; }

        public MapEdgeRecord()
        {
            Kind = "Road";
            FeatureName = "";
        }

        public string NormalizedKey()
        {
            string a = AQ + "," + AR;
            string b = BQ + "," + BR;
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b + "|" + Kind : b + "|" + a + "|" + Kind;
        }
    }

    public class HexMapRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<HexCellRecord> Cells { get; set; }
        public List<MapSettlementRecord> Settlements { get; set; }
        public List<MapEdgeRecord> Roads { get; set; }
        public List<MapEdgeRecord> Rivers { get; set; }
        public List<DomainRecord> Domains { get; set; }
        public List<RealmRecord> Realms { get; set; }
        public List<VassalLinkRecord> VassalLinks { get; set; }
        public List<HexFeatureRecord> Features { get; set; }
        public List<DungeonRecord> Dungeons { get; set; }
        public DateTime UpdatedAt { get; set; }

        public HexMapRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            Width = 12;
            Height = 10;
            Cells = new List<HexCellRecord>();
            Settlements = new List<MapSettlementRecord>();
            Roads = new List<MapEdgeRecord>();
            Rivers = new List<MapEdgeRecord>();
            Domains = new List<DomainRecord>();
            Realms = new List<RealmRecord>();
            VassalLinks = new List<VassalLinkRecord>();
            Features = new List<HexFeatureRecord>();
            Dungeons = new List<DungeonRecord>();
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(Name) ? "(unnamed map)" : Name; }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal class MapPathResult
    {
        public MapSettlementRecord Target { get; set; }
        public int DistanceHexes { get; set; }
        public bool HasRoad { get; set; }
    }

    public class HexFeatureRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Subtype { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        public string IconKey { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string DungeonId { get; set; }
        public string DungeonType { get; set; }
        public int DungeonLevel { get; set; }
        public string DungeonSize { get; set; }
        public DateTime UpdatedAt { get; set; }

        public HexFeatureRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            Kind = "Natural";
            Subtype = "";
            Q = -1;
            R = -1;
            IconKey = "";
            Description = "";
            Severity = "";
            DungeonId = "";
            DungeonType = "";
            DungeonLevel = 1;
            DungeonSize = "Standard";
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed feature)" : Name;
                string subtype = string.IsNullOrWhiteSpace(Subtype) ? Kind : Subtype;
                return name + " [" + subtype + "]";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
