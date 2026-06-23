using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OSRCGG
{
    public class DomainHexRecord
    {
        public int Q { get; set; }
        public int R { get; set; }
        public int LandValueGp { get; set; }

        public DomainHexRecord()
        {
            LandValueGp = 6;
        }

        public string Key()
        {
            return Q + "," + R;
        }
    }

    public class DomainRulerRecord
    {
        public string SourceMode { get; set; }
        public string LibraryCharacterId { get; set; }
        public CharacterRecord Snapshot { get; set; }
        public bool SaveGeneratedToLibrary { get; set; }

        public DomainRulerRecord()
        {
            SourceMode = "None";
            LibraryCharacterId = "";
            Snapshot = new CharacterRecord
            {
                Kind = "NPC",
                Name = "",
                ClassName = "Fighter",
                Level = 1,
                CHA = 9,
                Alignment = "Neutral",
                Proficiencies = ""
            };
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                if (string.Equals(SourceMode, "None", StringComparison.OrdinalIgnoreCase))
                {
                    return "(no ruler)";
                }

                if (Snapshot == null || string.IsNullOrWhiteSpace(Snapshot.Name))
                {
                    return "(no ruler)";
                }

                string className = string.IsNullOrWhiteSpace(Snapshot.ClassName) ? "NPC" : Snapshot.ClassName;
                return Snapshot.Name + " [" + className + " " + Snapshot.Level + "]";
            }
        }

        public bool HasLeadership()
        {
            return Snapshot != null
                && !string.IsNullOrWhiteSpace(Snapshot.Proficiencies)
                && Snapshot.Proficiencies.IndexOf("Leadership", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static DomainRulerRecord FromCharacter(CharacterRecord character, string sourceMode)
        {
            DomainRulerRecord ruler = new DomainRulerRecord();
            ruler.SourceMode = string.IsNullOrWhiteSpace(sourceMode) ? "LibraryCharacter" : sourceMode;

            if (character == null)
            {
                ruler.SourceMode = "None";
                ruler.LibraryCharacterId = "";
                return ruler;
            }

            ruler.LibraryCharacterId = character.Id;
            ruler.Snapshot = CloneCharacter(character);
            return ruler;
        }

        public static CharacterRecord CloneCharacter(CharacterRecord source)
        {
            if (source == null) return null;

            return new CharacterRecord
            {
                Id = source.Id,
                Kind = source.Kind,
                Name = source.Name,
                ClassName = source.ClassName,
                Occupation = source.Occupation,
                Template = source.Template,
                Alignment = source.Alignment,
                Homeland = source.Homeland,
                Sex = source.Sex,
                Level = source.Level,
                Experience = source.Experience,
                HitPoints = source.HitPoints,
                ArmorClass = source.ArmorClass,
                Age = source.Age,
                STR = source.STR,
                INT = source.INT,
                WIL = source.WIL,
                DEX = source.DEX,
                CON = source.CON,
                CHA = source.CHA,
                Proficiencies = source.Proficiencies,
                Languages = source.Languages,
                Spells = source.Spells,
                Equipment = source.Equipment,
                Appearance = source.Appearance,
                Background = source.Background,
                Notes = source.Notes,
                UpdatedAt = source.UpdatedAt
            };
        }

        public CharacterRecord ToLibraryCharacter()
        {
            CharacterRecord record = CloneCharacter(Snapshot);
            if (record == null) return null;

            if (string.IsNullOrWhiteSpace(record.Id))
            {
                record.Id = Guid.NewGuid().ToString("N");
            }

            record.Kind = string.IsNullOrWhiteSpace(record.Kind) ? "NPC" : record.Kind;
            record.UpdatedAt = DateTime.Now;
            return record;
        }
    }

    public class DomainRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DomainType { get; set; }
        public string Race { get; set; }
        public string Classification { get; set; }
        public string DomainAlignment { get; set; }
        public string LandValueMode { get; set; }
        public int FixedLandValueGp { get; set; }
        public int PeasantFamilies { get; set; }
        public int UrbanFamilies { get; set; }
        public int StrongholdValueGp { get; set; }
        public int GarrisonGpPerFamily { get; set; }
        public int TaxGpPerFamily { get; set; }
        public int LiturgiesGpPerFamily { get; set; }
        public int TithesGpPerFamily { get; set; }
        public int MaintenanceGpPerFamily { get; set; }
        public int BaseMorale { get; set; }
        public int CurrentMorale { get; set; }
        public int ColorArgb { get; set; }
        public string RealmId { get; set; }
        public string CapitalSettlementId { get; set; }
        public List<string> SettlementIds { get; set; }
        public string StrongholdId { get; set; }
        public string StrongholdName { get; set; }
        public int StrongholdQ { get; set; }
        public int StrongholdR { get; set; }
        public string StrongholdType { get; set; }
        public string StrongholdIconKey { get; set; }
        public bool StrongholdInSettlement { get; set; }
        public string StrongholdSettlementId { get; set; }
        public bool StrongholdActsAsMarketClassVI { get; set; }
        public bool StrongholdSecuresDomain { get; set; }
        public bool StrongholdIsUnderground { get; set; }
        public bool StrongholdNaturalMajesty { get; set; }
        public string Notes { get; set; }
        public DomainRulerRecord Ruler { get; set; }
        public List<DomainHexRecord> Hexes { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DomainRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            DomainType = "Ordinary";
            Race = "Human";
            Classification = "Outlands";
            DomainAlignment = "Neutral";
            LandValueMode = "Fixed6";
            FixedLandValueGp = 6;
            PeasantFamilies = 0;
            UrbanFamilies = 0;
            StrongholdValueGp = 0;
            GarrisonGpPerFamily = 2;
            TaxGpPerFamily = 2;
            LiturgiesGpPerFamily = 1;
            TithesGpPerFamily = 1;
            MaintenanceGpPerFamily = 1;
            BaseMorale = 0;
            CurrentMorale = 0;
            ColorArgb = unchecked((int)0x6637A86B);
            RealmId = "";
            CapitalSettlementId = "";
            SettlementIds = new List<string>();
            StrongholdId = Guid.NewGuid().ToString("N");
            StrongholdName = "";
            StrongholdQ = -1;
            StrongholdR = -1;
            StrongholdType = "Fortress";
            StrongholdIconKey = "";
            StrongholdInSettlement = true;
            StrongholdSettlementId = "";
            StrongholdActsAsMarketClassVI = true;
            StrongholdSecuresDomain = true;
            StrongholdIsUnderground = false;
            StrongholdNaturalMajesty = false;
            Notes = "";
            Ruler = new DomainRulerRecord();
            Hexes = new List<DomainHexRecord>();
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed domain)" : Name;
                return name + " [" + Classification + "]";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
