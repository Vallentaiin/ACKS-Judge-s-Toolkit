using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OSRCGG
{
    public class DungeonRecord : IStorable, IDisplayRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DungeonType { get; set; }
        public string Size { get; set; }
        public int RecommendedLevel { get; set; }
        public string ChallengeTier { get; set; }
        public string Notes { get; set; }
        public List<DungeonLevelRecord> Levels { get; set; }
        public List<DungeonEncounterRecord> WanderingEncounters { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DungeonRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            DungeonType = "Natural caverns";
            Size = "Standard";
            RecommendedLevel = 1;
            ChallengeTier = "Low";
            Notes = "";
            Levels = new List<DungeonLevelRecord>();
            WanderingEncounters = new List<DungeonEncounterRecord>();
            UpdatedAt = DateTime.Now;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed dungeon)" : Name;
                return name + " [" + DungeonType + ", L" + RecommendedLevel + "]";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class DungeonLevelRecord
    {
        public int LevelNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<DungeonRoomRecord> Rooms { get; set; }
        public List<DungeonConnectionRecord> Connections { get; set; }
        public List<DungeonDoorRecord> Doors { get; set; }

        public DungeonLevelRecord()
        {
            LevelNumber = 1;
            Width = 16;
            Height = 12;
            Rooms = new List<DungeonRoomRecord>();
            Connections = new List<DungeonConnectionRecord>();
            Doors = new List<DungeonDoorRecord>();
        }
    }

    public class DungeonRoomRecord
    {
        public string Id { get; set; }
        public int LevelNumber { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Shape { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public string Monster { get; set; }
        public string MonsterKey { get; set; }
        public string MonsterCountExpression { get; set; }
        public int MonsterCount { get; set; }
        public int MonsterXpEach { get; set; }
        public int MonsterXpTotal { get; set; }
        public string MonsterTreasureType { get; set; }
        public bool MonsterLair { get; set; }
        public string Treasure { get; set; }
        public string Trap { get; set; }
        public string TrapKey { get; set; }
        public int TrapLevel { get; set; }
        public string TrapTrigger { get; set; }
        public string TrapEffect { get; set; }
        public string UniqueFeature { get; set; }

        public DungeonRoomRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            LevelNumber = 1;
            Width = 2;
            Height = 2;
            Shape = "Rectangle";
            Kind = "Empty";
            Title = "";
            Details = "";
            Monster = "";
            MonsterKey = "";
            MonsterCountExpression = "";
            MonsterCount = 0;
            MonsterXpEach = 0;
            MonsterXpTotal = 0;
            MonsterTreasureType = "";
            MonsterLair = false;
            Treasure = "";
            Trap = "";
            TrapKey = "";
            TrapLevel = 0;
            TrapTrigger = "";
            TrapEffect = "";
            UniqueFeature = "";
        }

        public override string ToString()
        {
            string title = string.IsNullOrWhiteSpace(Title) ? "Room" : Title;
            return "L" + LevelNumber + " " + title + " [" + Kind + "]";
        }
    }

    public class DungeonConnectionRecord
    {
        public string FromRoomId { get; set; }
        public string ToRoomId { get; set; }
        public string Kind { get; set; }
        public int PassageWidth { get; set; }
        public string DoorKind { get; set; }
        public List<DungeonPathPointRecord> PathPoints { get; set; }

        public DungeonConnectionRecord()
        {
            FromRoomId = "";
            ToRoomId = "";
            Kind = "Corridor";
            PassageWidth = 1;
            DoorKind = "";
            PathPoints = new List<DungeonPathPointRecord>();
        }
    }

    // Ручные точки изгиба коридора. Без них редактор вынужден каждый раз
    // строить автоломаную между комнатами и не может сохранять правку формы.
    public class DungeonPathPointRecord
    {
        public double X { get; set; }
        public double Y { get; set; }

        public DungeonPathPointRecord()
        {
            X = 0;
            Y = 0;
        }
    }

    // Дверь, тайная дверь или тайный проход являются отдельной сущностью плана,
    // чтобы их можно было ставить и удалять независимо от коридора.
    public class DungeonDoorRecord
    {
        public string Id { get; set; }
        public int LevelNumber { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Kind { get; set; }
        public string Orientation { get; set; }
        public string FromRoomId { get; set; }
        public string ToRoomId { get; set; }
        public string Notes { get; set; }

        public DungeonDoorRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            LevelNumber = 1;
            X = 0;
            Y = 0;
            Kind = "Door";
            Orientation = "Vertical";
            FromRoomId = "";
            ToRoomId = "";
            Notes = "";
        }
    }

    public class DungeonEncounterRecord
    {
        public int DungeonLevel { get; set; }
        public int Roll { get; set; }
        public int MonsterLevel { get; set; }
        public string Monster { get; set; }
        public string CountExpression { get; set; }
        public string Notes { get; set; }

        public DungeonEncounterRecord()
        {
            DungeonLevel = 1;
            Roll = 1;
            MonsterLevel = 1;
            Monster = "";
            CountExpression = "1d6";
            Notes = "";
        }

        public override string ToString()
        {
            return Roll + " -> ML" + MonsterLevel + ": " + CountExpression + " " + Monster;
        }
    }

    public class DungeonGenerationOptions
    {
        public string Seed { get; set; }
        public string Name { get; set; }
        public string DungeonType { get; set; }
        public string Size { get; set; }
        public int RecommendedLevel { get; set; }
        public bool RussianOutput { get; set; }

        public DungeonGenerationOptions()
        {
            Seed = "";
            Name = "";
            DungeonType = "";
            Size = "Standard";
            RecommendedLevel = 1;
            RussianOutput = false;
        }
    }
}
