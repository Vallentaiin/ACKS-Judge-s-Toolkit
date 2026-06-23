using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OSRCGG
{
    public class RegionGenerationOptions
    {
        public const int MaxMapSize = 150;

        public string Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ClimateBelt { get; set; }
        public string CivilizationLevel { get; set; }
        public string RealmScale { get; set; }
        public string HumanRealmScale { get; set; }
        public string DwarvenRealmScale { get; set; }
        public string ElvenRealmScale { get; set; }
        public string HumanClanRealmScale { get; set; }
        public string OrcRealmScale { get; set; }
        public string BeastmanRealmScale { get; set; }
        public string TransitionalRealmScale { get; set; }
        public string CultureKey { get; set; }
        public string WaterLayout { get; set; }
        public string Seismicity { get; set; }
        public bool AdvancedMode { get; set; }
        public bool GenerateSettlements { get; set; }
        public bool GenerateStrongholds { get; set; }
        public bool GenerateDomains { get; set; }
        public bool GenerateRealms { get; set; }
        public bool GenerateRulers { get; set; }
        public bool GenerateRoads { get; set; }
        public bool GenerateRivers { get; set; }
        public bool GenerateFeatureNames { get; set; }
        public bool GenerateHexFeatures { get; set; }
        public bool GenerateDungeons { get; set; }
        public bool GenerateSpecialDomains { get; set; }
        public bool GenerateDwarvenDomains { get; set; }
        public bool GenerateElvenDomains { get; set; }
        public bool GenerateClanDomains { get; set; }
        public bool GenerateTransitionalDomains { get; set; }
        public bool GenerateSpecialSettlementsWithoutDomains { get; set; }
        public bool UseRussianNames { get; set; }
        public bool UseDwarvenCultureNames { get; set; }
        public bool UseElvenCultureNames { get; set; }
        public bool UseClanCultureNames { get; set; }
        public bool UseTransitionalCultureNames { get; set; }
        public bool UseSpecialDomainWeights { get; set; }
        public int TerrainZoneCount { get; set; }
        public int TerrainChaosPercent { get; set; }
        public int WaterPercent { get; set; }
        public int LakePercent { get; set; }
        public int RiverPercent { get; set; }
        public int HillsPercent { get; set; }
        public int MountainsPercent { get; set; }
        public int SettlementDensityPercent { get; set; }
        public int DomainCoveragePercent { get; set; }
        public int RealmCount { get; set; }
        public int StateSizeVariancePercent { get; set; }
        public int SpecialDomainPercent { get; set; }
        public int DwarvenDomainWeight { get; set; }
        public int ElvenDomainWeight { get; set; }
        public int ClanDomainWeight { get; set; }
        public int TransitionalDomainWeight { get; set; }
        public int DefaultAgeIndex { get; set; }
        public string LandValueMode { get; set; }

        public RegionGenerationOptions()
        {
            Seed = DateTime.Now.ToString("yyyyMMddHHmmss");
            Width = 24;
            Height = 18;
            ClimateBelt = "Temperate";
            CivilizationLevel = "Borderlands";
            RealmScale = "Balanced";
            HumanRealmScale = "Default";
            DwarvenRealmScale = "Default";
            ElvenRealmScale = "Default";
            HumanClanRealmScale = "Default";
            OrcRealmScale = "Default";
            BeastmanRealmScale = "Default";
            TransitionalRealmScale = "Default";
            CultureKey = "english";
            WaterLayout = "Coast";
            Seismicity = "Normal";
            AdvancedMode = false;
            GenerateSettlements = true;
            GenerateStrongholds = true;
            GenerateDomains = true;
            GenerateRealms = true;
            GenerateRulers = true;
            GenerateRoads = true;
            GenerateRivers = true;
            GenerateFeatureNames = true;
            GenerateHexFeatures = true;
            GenerateDungeons = true;
            GenerateSpecialDomains = false;
            GenerateDwarvenDomains = false;
            GenerateElvenDomains = false;
            GenerateClanDomains = false;
            GenerateTransitionalDomains = false;
            GenerateSpecialSettlementsWithoutDomains = false;
            UseRussianNames = false;
            UseDwarvenCultureNames = true;
            UseElvenCultureNames = true;
            UseClanCultureNames = true;
            UseTransitionalCultureNames = false;
            UseSpecialDomainWeights = false;
            TerrainZoneCount = 10;
            TerrainChaosPercent = 35;
            WaterPercent = 12;
            LakePercent = 4;
            RiverPercent = 35;
            HillsPercent = 22;
            MountainsPercent = 8;
            SettlementDensityPercent = 35;
            DomainCoveragePercent = 45;
            RealmCount = 3;
            StateSizeVariancePercent = 45;
            SpecialDomainPercent = 12;
            DwarvenDomainWeight = 25;
            ElvenDomainWeight = 25;
            ClanDomainWeight = 30;
            TransitionalDomainWeight = 20;
            DefaultAgeIndex = -1;
            LandValueMode = "Fixed6";
        }
    }

    public class GeneratedRegionResult
    {
        public HexMapRecord Map { get; set; }
        public List<string> Log { get; private set; }

        public GeneratedRegionResult()
        {
            Log = new List<string>();
        }
    }

    public class RegionGenerationProgress
    {
        public int Percent { get; set; }
        public string Message { get; set; }

        public RegionGenerationProgress()
        {
            Message = "";
        }

        public RegionGenerationProgress(int percent, string message)
        {
            Percent = Math.Max(0, Math.Min(100, percent));
            Message = message ?? "";
        }
    }

    public class RealmRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tier { get; set; }
        public string TitleOverride { get; set; }
        public string FemaleTitleOverride { get; set; }
        public string CultureKey { get; set; }
        public string CapitalSettlementId { get; set; }
        public string RulerName { get; set; }
        public int RulerLevel { get; set; }
        public int ColorArgb { get; set; }
        public string Notes { get; set; }
        public DateTime UpdatedAt { get; set; }

        public RealmRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            Tier = "County";
            TitleOverride = "";
            FemaleTitleOverride = "";
            CultureKey = "";
            CapitalSettlementId = "";
            RulerName = "";
            RulerLevel = 7;
            ColorArgb = unchecked((int)0x66547AA5);
            Notes = "";
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed realm)" : Name;
                return name + " [" + Tier + "]";
            }
        }
    }

    public class VassalLinkRecord
    {
        public string Id { get; set; }
        public string LiegeRealmId { get; set; }
        public string VassalRealmId { get; set; }
        public string RelationType { get; set; }
        public int Loyalty { get; set; }
        public int TributeGp { get; set; }
        public string Notes { get; set; }

        public VassalLinkRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            LiegeRealmId = "";
            VassalRealmId = "";
            RelationType = "Vassal";
            Loyalty = 0;
            TributeGp = 0;
            Notes = "";
        }
    }
}
