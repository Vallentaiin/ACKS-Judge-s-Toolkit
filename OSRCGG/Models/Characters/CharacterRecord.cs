using System;

namespace OSRCGG
{
    public class CharacterRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
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
        public int STR { get; set; }
        public int INT { get; set; }
        public int WIL { get; set; }
        public int DEX { get; set; }
        public int CON { get; set; }
        public int CHA { get; set; }
        public string Proficiencies { get; set; }
        public string Languages { get; set; }
        public string Spells { get; set; }
        public string Equipment { get; set; }
        public string Appearance { get; set; }
        public string Background { get; set; }
        public string Notes { get; set; }
        public DateTime UpdatedAt { get; set; }

        public CharacterRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Kind = "Player";
            Name = "";
            ClassName = "Fighter";
            Occupation = "";
            Template = "";
            Alignment = "Neutral";
            Homeland = "";
            Sex = "";
            Proficiencies = "";
            Languages = "";
            Spells = "";
            Equipment = "";
            Appearance = "";
            Background = "";
            Notes = "";
            UpdatedAt = DateTime.Now;
        }

        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(без имени)" : Name;
                string type = Kind == "NPC" ? "NPC" : "PC";
                string detail = Kind == "NPC" ? Occupation : ClassName;
                return name + " [" + type + "] " + detail;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
