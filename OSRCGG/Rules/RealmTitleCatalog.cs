using System;
using System.Collections.Generic;

namespace OSRCGG
{
    public static class RealmTitleCatalog
    {
        private static readonly Dictionary<string, string> TierRanks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Empire", "Empire" },
            { "Kingdom", "Kingdom" },
            { "Principality", "Principality" },
            { "Duchy", "Duchy" },
            { "County", "County" },
            { "Viscounty", "Viscounty" },
            { "Barony", "Barony" }
        };

        private static readonly Dictionary<string, string> CultureFamilies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "auran", "Auran" },
            { "auran_empire", "Auran" },
            { "argollean", "Argollean" },
            { "argollean_elven", "Argollean" },
            { "elf", "Argollean" },
            { "elven", "Argollean" },
            { "somirean", "Somirean" },
            { "indian", "Somirean" },
            { "jutlandic", "Jutlandic" },
            { "old_norse", "Jutlandic" },
            { "scandinavian", "Jutlandic" },
            { "danish", "Jutlandic" }
        };

        private static readonly Dictionary<string, Dictionary<string, TitleForms>> EnglishTitles = CreateEnglishTitles();
        private static readonly Dictionary<string, Dictionary<string, TitleForms>> RussianTitles = CreateRussianTitles();
        private static readonly Dictionary<string, Dictionary<string, string>> EnglishRealmTitles = CreateEnglishRealmTitles();
        private static readonly Dictionary<string, Dictionary<string, string>> RussianRealmTitles = CreateRussianRealmTitles();

        public static string RealmTitle(string cultureKey, string tier, bool russian, string overrideTitle)
        {
            if (!string.IsNullOrWhiteSpace(overrideTitle)) return overrideTitle.Trim();
            Dictionary<string, string> byRank = GetRealmTable(cultureKey, russian);
            string rank = NormalizeTier(tier);
            string title;
            return byRank.TryGetValue(rank, out title) ? title : byRank["Realm"];
        }

        public static string RulerTitle(string cultureKey, string tier, bool female, bool russian, string maleOverride, string femaleOverride)
        {
            if (female && !string.IsNullOrWhiteSpace(femaleOverride)) return femaleOverride.Trim();
            if (!female && !string.IsNullOrWhiteSpace(maleOverride)) return maleOverride.Trim();

            Dictionary<string, TitleForms> byRank = GetRulerTable(cultureKey, russian);
            string rank = NormalizeTier(tier);
            TitleForms title;
            if (!byRank.TryGetValue(rank, out title)) title = byRank["Realm"];
            return female ? title.Female : title.Male;
        }

        public static string NormalizeTier(string tier)
        {
            string rank;
            return !string.IsNullOrWhiteSpace(tier) && TierRanks.TryGetValue(tier, out rank) ? rank : "Realm";
        }

        private static Dictionary<string, TitleForms> GetRulerTable(string cultureKey, bool russian)
        {
            string family = ResolveCultureFamily(cultureKey);
            Dictionary<string, Dictionary<string, TitleForms>> source = russian ? RussianTitles : EnglishTitles;
            Dictionary<string, TitleForms> table;
            return source.TryGetValue(family, out table) ? table : source["Common"];
        }

        private static Dictionary<string, string> GetRealmTable(string cultureKey, bool russian)
        {
            string family = ResolveCultureFamily(cultureKey);
            Dictionary<string, Dictionary<string, string>> source = russian ? RussianRealmTitles : EnglishRealmTitles;
            Dictionary<string, string> table;
            return source.TryGetValue(family, out table) ? table : source["Common"];
        }

        private static string ResolveCultureFamily(string cultureKey)
        {
            if (string.IsNullOrWhiteSpace(cultureKey)) return "Common";
            string family;
            return CultureFamilies.TryGetValue(cultureKey, out family) ? family : "Common";
        }

        private static Dictionary<string, Dictionary<string, TitleForms>> CreateEnglishTitles()
        {
            return new Dictionary<string, Dictionary<string, TitleForms>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Common", Forms(Pair("Empire", "Emperor", "Empress"), Pair("Kingdom", "King", "Queen"), Pair("Principality", "Prince", "Princess"), Pair("Duchy", "Duke", "Duchess"), Pair("County", "Count", "Countess"), Pair("Viscounty", "Viscount", "Viscountess"), Pair("Barony", "Baron", "Baroness"), Pair("Realm", "Lord", "Lady")) },
                { "Auran", Forms(Pair("Empire", "Tarkun", "Tarkun"), Pair("Kingdom", "Exarch", "Exarch"), Pair("Principality", "Prefect", "Prefect"), Pair("Duchy", "Palatine", "Palatine"), Pair("County", "Legate", "Legate"), Pair("Viscounty", "Tribune", "Tribune"), Pair("Barony", "Castellan", "Castellan"), Pair("Realm", "Lord", "Lady")) },
                { "Argollean", Forms(Pair("Empire", "Ard-ri", "Ard-ri"), Pair("Kingdom", "Ri-ruirech", "Ri-ruirech"), Pair("Principality", "Ri", "Ri"), Pair("Duchy", "Diuc", "Diuc"), Pair("County", "Iarla", "Iarla"), Pair("Viscounty", "Ard-tiarna", "Ard-tiarna"), Pair("Barony", "Tiarna", "Tiarna"), Pair("Realm", "Tiarna", "Tiarna")) },
                { "Somirean", Forms(Pair("Empire", "Maharaja", "Maharani"), Pair("Kingdom", "Raja", "Rani"), Pair("Principality", "Deshmukh", "Deshmukh"), Pair("Duchy", "Zammin", "Zammin"), Pair("County", "Mansab", "Mansab"), Pair("Viscounty", "Sardar", "Sardar"), Pair("Barony", "Jagir", "Jagir"), Pair("Realm", "Raja", "Rani")) },
                { "Jutlandic", Forms(Pair("Empire", "High King", "High Queen"), Pair("Kingdom", "King", "Queen"), Pair("Principality", "Prince", "Princess"), Pair("Duchy", "Duke", "Duchess"), Pair("County", "Jarl", "Jarl"), Pair("Viscounty", "Reeve", "Reeve"), Pair("Barony", "Thane", "Thane"), Pair("Realm", "Lord", "Lady")) }
            };
        }

        private static Dictionary<string, Dictionary<string, TitleForms>> CreateRussianTitles()
        {
            return new Dictionary<string, Dictionary<string, TitleForms>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Common", Forms(Pair("Empire", "Император", "Императрица"), Pair("Kingdom", "Король", "Королева"), Pair("Principality", "Князь", "Княгиня"), Pair("Duchy", "Герцог", "Герцогиня"), Pair("County", "Граф", "Графиня"), Pair("Viscounty", "Виконт", "Виконтесса"), Pair("Barony", "Барон", "Баронесса"), Pair("Realm", "Правитель", "Правительница")) },
                { "Auran", Forms(Pair("Empire", "Таркун", "Таркун"), Pair("Kingdom", "Экзарх", "Экзарх"), Pair("Principality", "Префект", "Префект"), Pair("Duchy", "Палатин", "Палатин"), Pair("County", "Легат", "Легат"), Pair("Viscounty", "Трибун", "Трибун"), Pair("Barony", "Кастелян", "Кастелян"), Pair("Realm", "Правитель", "Правительница")) },
                { "Argollean", Forms(Pair("Empire", "Ард-ри", "Ард-ри"), Pair("Kingdom", "Ри-руирех", "Ри-руирех"), Pair("Principality", "Ри", "Ри"), Pair("Duchy", "Диук", "Диук"), Pair("County", "Иарла", "Иарла"), Pair("Viscounty", "Ард-тиарна", "Ард-тиарна"), Pair("Barony", "Тиарна", "Тиарна"), Pair("Realm", "Тиарна", "Тиарна")) },
                { "Somirean", Forms(Pair("Empire", "Махараджа", "Махарани"), Pair("Kingdom", "Раджа", "Рани"), Pair("Principality", "Дешмукх", "Дешмукх"), Pair("Duchy", "Заммин", "Заммин"), Pair("County", "Мансаб", "Мансаб"), Pair("Viscounty", "Сардар", "Сардар"), Pair("Barony", "Джагир", "Джагир"), Pair("Realm", "Раджа", "Рани")) },
                { "Jutlandic", Forms(Pair("Empire", "Верховный король", "Верховная королева"), Pair("Kingdom", "Король", "Королева"), Pair("Principality", "Князь", "Княгиня"), Pair("Duchy", "Герцог", "Герцогиня"), Pair("County", "Ярл", "Ярл"), Pair("Viscounty", "Рив", "Рив"), Pair("Barony", "Тейн", "Тейн"), Pair("Realm", "Правитель", "Правительница")) }
            };
        }

        private static Dictionary<string, Dictionary<string, string>> CreateEnglishRealmTitles()
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Common", RealmForms("Empire", "Kingdom", "Principality", "Duchy", "County", "Viscounty", "Barony", "Realm") },
                { "Auran", RealmForms("Tarkunate", "Exarchate", "Prefecture", "Palatinate", "Legation", "Tribunate", "Castellany", "Realm") },
                { "Argollean", RealmForms("Ard-riate", "Ri-ruirech", "Ri", "Diucate", "Iarlath", "Ard-tiarna", "Tiarna", "Realm") },
                { "Somirean", RealmForms("Maharajya", "Raj", "Deshmukh", "Zamindari", "Mansab", "Sardari", "Jagir", "Realm") },
                { "Jutlandic", RealmForms("High Kingdom", "Kingdom", "Principality", "Duchy", "Jarldom", "Reevehood", "Thanedom", "Realm") }
            };
        }

        private static Dictionary<string, Dictionary<string, string>> CreateRussianRealmTitles()
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Common", RealmForms("Империя", "Королевство", "Княжество", "Герцогство", "Графство", "Виконтство", "Баронство", "Держава") },
                { "Auran", RealmForms("Таркунат", "Экзархат", "Префектура", "Палатинат", "Легатство", "Трибунат", "Кастелянство", "Держава") },
                { "Argollean", RealmForms("Ард-риат", "Ри-руирех", "Ри", "Диукат", "Иарлат", "Ард-тиарна", "Тиарна", "Держава") },
                { "Somirean", RealmForms("Махараджья", "Радж", "Дешмукх", "Заминдари", "Мансаб", "Сардари", "Джагир", "Держава") },
                { "Jutlandic", RealmForms("Верховное королевство", "Королевство", "Княжество", "Герцогство", "Ярлство", "Ривство", "Тейнство", "Держава") }
            };
        }

        private static Dictionary<string, TitleForms> Forms(params KeyValuePair<string, TitleForms>[] pairs)
        {
            Dictionary<string, TitleForms> result = new Dictionary<string, TitleForms>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, TitleForms> pair in pairs) result[pair.Key] = pair.Value;
            return result;
        }

        private static Dictionary<string, string> RealmForms(string empire, string kingdom, string principality, string duchy, string county, string viscounty, string barony, string realm)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Empire", empire }, { "Kingdom", kingdom }, { "Principality", principality }, { "Duchy", duchy },
                { "County", county }, { "Viscounty", viscounty }, { "Barony", barony }, { "Realm", realm }
            };
        }

        private static KeyValuePair<string, TitleForms> Pair(string tier, string male, string female)
        {
            return new KeyValuePair<string, TitleForms>(tier, new TitleForms(male, female));
        }

        private sealed class TitleForms
        {
            public string Male { get; private set; }
            public string Female { get; private set; }

            public TitleForms(string male, string female)
            {
                Male = male;
                Female = female;
            }
        }
    }
}
