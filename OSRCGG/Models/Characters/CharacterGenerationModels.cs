namespace OSRCGG
{
    internal class CharacterClassDefinition
    {
        public string Name { get; private set; }
        public string KeyAttributes { get; private set; }
        public int HitDie { get; private set; }
        public string RaceHint { get; private set; }

        public CharacterClassDefinition(string name, string keyAttributes, int hitDie, string raceHint)
        {
            Name = name;
            KeyAttributes = keyAttributes;
            HitDie = hitDie;
            RaceHint = raceHint;
        }
    }

    internal class RollRange<T>
    {
        public int Min { get; private set; }
        public int Max { get; private set; }
        public T Value { get; private set; }

        public RollRange(int min, int max, T value)
        {
            Min = min;
            Max = max;
            Value = value;
        }

        public bool Contains(int roll)
        {
            return roll >= Min && roll <= Max;
        }
    }

    internal class CharacterTemplate
    {
        public string Name { get; private set; }
        public string Proficiencies { get; private set; }
        public string Equipment { get; private set; }
        public string Spells { get; private set; }

        public CharacterTemplate(string name, string proficiencies, string equipment, string spells = "")
        {
            Name = name;
            Proficiencies = proficiencies;
            Equipment = equipment;
            Spells = spells;
        }
    }

    internal class NpcOccupationResult
    {
        public string Category { get; private set; }
        public string Occupation { get; private set; }

        public NpcOccupationResult(string category, string occupation)
        {
            Category = category;
            Occupation = occupation;
        }
    }
}
