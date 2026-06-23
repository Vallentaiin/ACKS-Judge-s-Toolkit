using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OSRCGG
{
    public sealed class NameCultureInfo
    {
        public string Key { get; set; }
        public string Label { get; set; }

        public override string ToString()
        {
            return Label;
        }
    }

    internal sealed class NamePack
    {
        public string Key { get; set; }
        public string[] MaleNames { get; set; }
        public string[] FemaleNames { get; set; }
        public string[] Surnames { get; set; }
        public string[] SettlementRoots { get; set; }
        public string[] SettlementSuffixes { get; set; }
        public string[] RealmPrefixes { get; set; }
        public string[] RealmSuffixes { get; set; }
    }

    public sealed class NameGenerationService
    {
        private readonly Dictionary<string, NamePack> packs = new Dictionary<string, NamePack>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> cultureLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> culturePackAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> RussianCultureLabels = CreateRussianCultureLabels();
        private static readonly string[] SettlementQualifiers =
        {
            "North", "South", "East", "West", "Old", "New", "High", "Low",
            "Green", "Red", "White", "Black", "Silver", "Golden", "Stone", "River"
        };

        private static readonly string[] SettlementMiddleParts =
        {
            "brook", "cliff", "cross", "dale", "fall", "field", "gate", "grove",
            "hall", "haven", "hill", "keep", "mill", "reach", "spring", "watch"
        };

        public static NameGenerationService CreateDefault(string startDirectory)
        {
            NameGenerationService service = new NameGenerationService();
            service.RegisterBuiltInPacks();
            service.LoadManifestCultures(startDirectory);
            return service;
        }

        public List<NameCultureInfo> GetCultures()
        {
            return GetCultures(true);
        }

        public List<NameCultureInfo> GetCultures(bool english)
        {
            return cultureLabels
                .Keys
                .OrderBy(key => GetCultureLabel(key, english))
                .Select(key => new NameCultureInfo { Key = key, Label = GetCultureLabel(key, english) })
                .ToList();
        }

        public string GeneratePersonalName(Random random, string cultureKey, bool female)
        {
            NamePack pack = ResolvePack(cultureKey);
            string given = Pick(random, female ? pack.FemaleNames : pack.MaleNames);
            string surname = Pick(random, pack.Surnames);
            return string.IsNullOrWhiteSpace(surname) ? given : given + " " + surname;
        }

        public string GeneratePersonalName(Random random, string cultureKey, bool female, bool russianOutput)
        {
            string name = GeneratePersonalName(random, cultureKey, female);
            return russianOutput ? TransliterateName(name, cultureKey) : name;
        }

        public string GenerateSettlementName(Random random, string cultureKey)
        {
            NamePack pack = ResolvePack(cultureKey);
            string root = Pick(random, pack.SettlementRoots);
            string suffix = Pick(random, pack.SettlementSuffixes);
            int style = random.Next(100);
            if (style < 24)
            {
                return root;
            }

            if (style < 68)
            {
                return root + suffix;
            }

            if (style < 84)
            {
                return Pick(random, SettlementQualifiers) + " " + root + suffix;
            }

            string second = Pick(random, pack.SettlementRoots);
            if (string.Equals(second, root, StringComparison.OrdinalIgnoreCase))
            {
                second = Pick(random, SettlementMiddleParts);
            }

            return root + second.ToLowerInvariant() + suffix;
        }

        public string GenerateSettlementName(Random random, string cultureKey, bool russianOutput)
        {
            string name = GenerateSettlementName(random, cultureKey);
            return russianOutput ? TransliterateName(name, cultureKey) : name;
        }

        public string GenerateDomainName(Random random, string cultureKey, string settlementName)
        {
            string baseName = string.IsNullOrWhiteSpace(settlementName)
                ? GenerateSettlementName(random, cultureKey)
                : settlementName;

            string[] patterns =
            {
                "Domain of {0}",
                "{0} March",
                "{0} County",
                "{0} Vale",
                "{0} Land"
            };

            return string.Format(Pick(random, patterns), baseName);
        }

        public string GenerateDomainName(Random random, string cultureKey, string settlementName, bool russianOutput)
        {
            if (!russianOutput)
            {
                return GenerateDomainName(random, cultureKey, settlementName);
            }

            string baseName = string.IsNullOrWhiteSpace(settlementName)
                ? GenerateSettlementName(random, cultureKey, true)
                : TransliterateName(settlementName, cultureKey);

            string[] patterns =
            {
                "Домен {0}",
                "Марка {0}",
                "Графство {0}",
                "Долина {0}",
                "Земли {0}"
            };

            return string.Format(Pick(random, patterns), baseName);
        }

        public string GenerateRealmName(Random random, string cultureKey, string capitalName, string tier)
        {
            NamePack pack = ResolvePack(cultureKey);
            string baseName = string.IsNullOrWhiteSpace(capitalName)
                ? GenerateSettlementName(random, cultureKey)
                : capitalName;
            string prefix = Pick(random, pack.RealmPrefixes);
            string suffix = Pick(random, pack.RealmSuffixes);

            if (!string.IsNullOrWhiteSpace(tier) && random.Next(100) < 45)
            {
                string tierName = RealmTitleCatalog.RealmTitle(cultureKey, tier, false, "");
                return tierName + " of " + baseName;
            }

            return prefix + " " + baseName + suffix;
        }

        public string GenerateRealmName(Random random, string cultureKey, string capitalName, string tier, bool russianOutput)
        {
            if (!russianOutput)
            {
                return GenerateRealmName(random, cultureKey, capitalName, tier);
            }

            string baseName = string.IsNullOrWhiteSpace(capitalName)
                ? GenerateSettlementName(random, cultureKey, true)
                : TransliterateName(capitalName, cultureKey);
            string tierName = RealmTitleCatalog.RealmTitle(cultureKey, tier, true, "");

            string[] patterns = RussianRealmPatterns(tierName, tier);

            return string.Format(Pick(random, patterns), baseName);
        }

        private string[] RussianRealmPatterns(string tierName, string tier)
        {
            if (string.Equals(tier, "Barony", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Владение {0}", "Земли {0}" };
            }

            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Виконтство {0}", "Владение {0}" };
            }

            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Графство {0}", "Марка {0}" };
            }

            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Герцогство {0}", "Марка {0}" };
            }

            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Княжество {0}", "Держава {0}" };
            }

            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { tierName + " {0}", "Держава {0}", "Корона {0}" };
            }

            return new[] { tierName + " {0}", "Держава {0}", "Владение {0}" };
        }

        public string GenerateDynastyName(Random random, string cultureKey)
        {
            NamePack pack = ResolvePack(cultureKey);
            return Pick(random, pack.Surnames);
        }

        public string GenerateDynastyName(Random random, string cultureKey, bool russianOutput)
        {
            string name = GenerateDynastyName(random, cultureKey);
            return russianOutput ? TransliterateName(name, cultureKey) : name;
        }

        public string GenerateFeatureName(Random random, string cultureKey, string featureKind, bool russianOutput)
        {
            string baseName = GenerateSettlementName(random, cultureKey, russianOutput);
            string kind = string.IsNullOrWhiteSpace(featureKind) ? "" : featureKind.Trim();

            if (russianOutput)
            {
                if (string.Equals(kind, "Ocean", StringComparison.OrdinalIgnoreCase)) return "Океан " + baseName;
                if (string.Equals(kind, "Sea", StringComparison.OrdinalIgnoreCase)) return "Море " + baseName;
                if (string.Equals(kind, "Lake", StringComparison.OrdinalIgnoreCase)) return "Озеро " + baseName;
                if (string.Equals(kind, "River", StringComparison.OrdinalIgnoreCase)) return "Река " + baseName;
                return baseName;
            }

            if (string.Equals(kind, "Ocean", StringComparison.OrdinalIgnoreCase)) return baseName + " Ocean";
            if (string.Equals(kind, "Sea", StringComparison.OrdinalIgnoreCase)) return baseName + " Sea";
            if (string.Equals(kind, "Lake", StringComparison.OrdinalIgnoreCase)) return "Lake " + baseName;
            if (string.Equals(kind, "River", StringComparison.OrdinalIgnoreCase)) return baseName + " River";
            return baseName;
        }

        private void LoadManifestCultures(string startDirectory)
        {
            string directory = FindManifestDirectory(startDirectory);
            if (string.IsNullOrWhiteSpace(directory)) return;

            foreach (string path in Directory.GetFiles(directory, "lists part *.txt"))
            {
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Replace("\t", "    ");
                    if (!line.StartsWith("  ") || line.StartsWith("    ")) continue;

                    string trimmed = line.Trim();
                    if (!trimmed.EndsWith(":", StringComparison.Ordinal)) continue;
                    string key = trimmed.Substring(0, trimmed.Length - 1).Trim();
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!cultureLabels.ContainsKey(key))
                    {
                        cultureLabels[key] = MakeCultureLabel(key);
                    }
                }
            }
        }

        private string FindManifestDirectory(string startDirectory)
        {
            string directory = string.IsNullOrWhiteSpace(startDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : startDirectory;

            for (int i = 0; i < 6 && !string.IsNullOrWhiteSpace(directory); i++)
            {
                if (Directory.GetFiles(directory, "lists part *.txt").Length > 0)
                {
                    return directory;
                }

                DirectoryInfo parent = Directory.GetParent(directory);
                directory = parent == null ? null : parent.FullName;
            }

            return null;
        }

        private void RegisterBuiltInPacks()
        {
            // Базовые наборы дают рабочую офлайн-генерацию; внешние HTML/SPARQL-источники позже смогут расширять те же культуры.
            RegisterPack("english",
                new[] { "Aldric", "Cedric", "Edwin", "Godwin", "Leofric", "Oswin", "Rowan", "Wystan" },
                new[] { "Aveline", "Edith", "Elena", "Isolde", "Mabel", "Rowena", "Winifred", "Ysabel" },
                new[] { "Ashford", "Blackwood", "Fairborne", "Hawthorne", "Kingsley", "Underhill", "Westmere" },
                new[] { "Ash", "Briar", "Dun", "Elden", "Grey", "High", "Kings", "Raven", "Stone", "West" },
                new[] { "bridge", "bury", "ford", "ham", "mere", "ton", "wick", "worth" },
                new[] { "Kingdom of", "March of", "Realm of", "Crown of" },
                new[] { "", "shire", "mark" });

            RegisterPack("russian",
                new[] { "Boris", "Dobrynya", "Mstislav", "Oleg", "Radoslav", "Sviatoslav", "Vsevolod", "Yaroslav" },
                new[] { "Bogdana", "Lada", "Lyubava", "Miroslava", "Rogneda", "Vesna", "Yaroslava", "Zlata" },
                new[] { "Belov", "Dragomirov", "Morozov", "Rusanov", "Sokolov", "Volkov", "Zorin" },
                new[] { "Bel", "Bor", "Drag", "Grad", "Kamen", "Moroz", "Nov", "Rusan", "Volch", "Zlat" },
                new[] { "grad", "gorod", "ovo", "sk", "vka", "ye" },
                new[] { "Knyazdom of", "Tsardom of", "High Realm of", "Land of" },
                new[] { "", "sk", "grad" });

            RegisterPack("german",
                new[] { "Adalhard", "Bruno", "Conrad", "Dietrich", "Emmerich", "Gerhard", "Lothar", "Wolfram" },
                new[] { "Adelheid", "Brunhild", "Gertrud", "Gisela", "Hedwig", "Irmgard", "Matilda", "Ursula" },
                new[] { "Eberhart", "Falken", "Grunwald", "Hohenberg", "Ritter", "Stein", "Waldmann" },
                new[] { "Adler", "Berg", "Eber", "Falk", "Grun", "Hohen", "Stein", "Wald", "Wolf" },
                new[] { "berg", "bruck", "dorf", "feld", "furt", "heim", "stadt", "wald" },
                new[] { "Reich of", "Duchy of", "March of", "Crown of" },
                new[] { "", "mark", "land" });

            RegisterPack("old_norse",
                new[] { "Arnbjorn", "Eirik", "Halfdan", "Hakon", "Ketil", "Ragnvald", "Sigurd", "Ulf" },
                new[] { "Astrid", "Freydis", "Gudrun", "Helga", "Ingrid", "Ragnhild", "Sigrid", "Thora" },
                new[] { "Bearclaw", "Eirikson", "Frostmane", "Ironhand", "Ravensson", "Stormborn", "Ulfson" },
                new[] { "Arn", "Berg", "Frost", "Hrafn", "Is", "Skald", "Storm", "Ulf", "Vik" },
                new[] { "by", "fjord", "gard", "heim", "holt", "vik" },
                new[] { "Jarldom of", "Sea-Kingdom of", "Hold of", "Realm of" },
                new[] { "", "gard", "heim" });

            RegisterPack("arabic",
                new[] { "Adil", "Farid", "Harun", "Jamal", "Khalid", "Mahmud", "Rashid", "Zahir" },
                new[] { "Amina", "Dalia", "Farida", "Jamila", "Layla", "Nadira", "Samira", "Zahra" },
                new[] { "al-Bahir", "al-Faris", "al-Hakim", "al-Qadir", "al-Rashid", "ibn Salim", "ibn Zayd" },
                new[] { "Ain", "Dar", "Jabal", "Ksar", "Qasr", "Ras", "Sahra", "Wadi", "Zahr" },
                new[] { "", "abad", "iya", "un", "ya" },
                new[] { "Emirate of", "Sultanate of", "Caliphate of", "Realm of" },
                new[] { "", " al-Kabir", " al-Qadim" });

            RegisterPack("persian",
                new[] { "Ardashir", "Bahram", "Darius", "Farhad", "Khosrow", "Rostam", "Shapur", "Vardan" },
                new[] { "Anahita", "Azarmidokht", "Gordiya", "Mahin", "Parisa", "Roxana", "Shirin", "Yasmin" },
                new[] { "Arsacid", "Bahrami", "Daryan", "Farrokh", "Mehraban", "Rostami", "Zand" },
                new[] { "Ard", "Bah", "Dar", "Far", "Mehr", "Nish", "Rost", "Shahr", "Yazd" },
                new[] { "abad", "an", "dar", "gard", "pur", "shahr" },
                new[] { "Shahdom of", "Satrapy of", "Crown of", "Realm of" },
                new[] { "", "shahr", "an" });

            RegisterPack("japanese",
                new[] { "Akihiro", "Daichi", "Haruto", "Katsuro", "Masaru", "Noboru", "Ren", "Takeshi" },
                new[] { "Akari", "Emi", "Hana", "Kaede", "Miyu", "Sakura", "Tomoe", "Yui" },
                new[] { "Akiyama", "Fujiwara", "Kuroda", "Mori", "Takeda", "Uesugi", "Yamamoto" },
                new[] { "Aki", "Fuji", "Hana", "Kawa", "Mori", "Naka", "Shiro", "Taka", "Yama" },
                new[] { "gawa", "hara", "mori", "shima", "ta", "yama" },
                new[] { "Province of", "Shogunate of", "Domain of", "Realm of" },
                new[] { "", " no Kuni", "han" });

            RegisterAdditionalPacks();
            RegisterFantasyPacks();

            RegisterPack("generic",
                new[] { "Aren", "Borin", "Cador", "Darian", "Edris", "Kael", "Marek", "Tovan" },
                new[] { "Ari", "Brina", "Dalia", "Elira", "Kaela", "Mira", "Neris", "Talia" },
                new[] { "Brightmere", "Duskfall", "Highward", "Ironvale", "Moonridge", "Starling", "Thorn" },
                new[] { "Bright", "Dusk", "High", "Iron", "Moon", "Red", "Star", "Thorn", "White" },
                new[] { "fall", "gate", "hold", "mere", "ridge", "ton", "vale", "watch" },
                new[] { "Realm of", "Dominion of", "March of", "Crown of" },
                new[] { "", "land", "mark" });

            RegisterManifestCultureAliases();
        }

        private void RegisterAdditionalPacks()
        {
            // Эти наборы являются встроенной офлайн-базой для культур из файлов источников.
            // Они не парсят сайты и не требуют интернет-соединения во время работы программы.
            RegisterPack("celtic_pack",
                new[] { "Aedan", "Bran", "Cadoc", "Conall", "Duncan", "Fergus", "Llywelyn", "Talan" },
                new[] { "Aisling", "Brigid", "Caitrin", "Deirdre", "Eithne", "Gwenllian", "Mairwen", "Rhiannon" },
                new[] { "Mac Bran", "Mac Conall", "O'Dunlaing", "ap Cadoc", "Brennan", "Glyndwr", "Kincaid" },
                new[] { "Aber", "Bryn", "Caer", "Dun", "Glen", "Inis", "Llan", "Pen", "Tir" },
                new[] { "", "dun", "more", "ness", "ton", "wyn" },
                new[] { "High Kingdom of", "Clanlands of", "Realm of", "Crown of" },
                new[] { "", "moor", "wyn" });

            RegisterPack("romance_pack",
                new[] { "Aurelian", "Bastien", "Cesare", "Domenico", "Etienne", "Lorenzo", "Marcellus", "Ramon" },
                new[] { "Adelina", "Beatrice", "Celeste", "Elise", "Isabella", "Lucia", "Mariana", "Sabina" },
                new[] { "Bellamy", "Carbone", "Delacroix", "Ferrara", "Montclair", "Rossi", "Valerio" },
                new[] { "Aqua", "Bel", "Castel", "Flor", "Monte", "Nova", "Port", "San", "Val" },
                new[] { "a", "ano", "eiro", "ella", "mont", "ona", "port", "ville" },
                new[] { "Principality of", "Duchy of", "Crown of", "Realm of" },
                new[] { "", "ia", "mont" });

            RegisterPack("ancient_roman_pack",
                new[] { "Aelius", "Cassius", "Decimus", "Gaius", "Lucius", "Marcus", "Quintus", "Titus" },
                new[] { "Aelia", "Cassia", "Claudia", "Cornelia", "Flavia", "Julia", "Livia", "Valeria" },
                new[] { "Aelianus", "Cassian", "Claudian", "Flavian", "Julian", "Marcellus", "Valerian" },
                new[] { "Aqua", "Castra", "Forum", "Julia", "Nova", "Portus", "Septem", "Villa" },
                new[] { "", "lia", "num", "polis", "ta", "um" },
                new[] { "Provincia of", "Imperium of", "Dioecesis of", "Realm of" },
                new[] { "", "um", "ia" });

            RegisterPack("caucasus_pack",
                new[] { "Aram", "Avtandil", "Davit", "Gagik", "Giorgi", "Levan", "Sargis", "Vakhtang" },
                new[] { "Anahit", "Ketevan", "Mariam", "Nane", "Nino", "Shushan", "Tamar", "Tinatin" },
                new[] { "Artsruni", "Bagratid", "Dadiani", "Mamikonian", "Orbeliani", "Pahlavuni", "Zakaridze" },
                new[] { "Akhal", "Ani", "Arag", "Dvin", "Kart", "Mtskhet", "Tbil", "Vard" },
                new[] { "ani", "eti", "isi", "kala", "shen", "van" },
                new[] { "Nakharardom of", "Kingdom of", "March of", "Realm of" },
                new[] { "", "eti", "k" });

            RegisterPack("turkic_pack",
                new[] { "Alp", "Arslan", "Batu", "Bektash", "Ilterish", "Kutlu", "Orhan", "Toghrul" },
                new[] { "Aybike", "Burla", "Esma", "Gulbahar", "Ilbilge", "Selcan", "Tomris", "Yildiz" },
                new[] { "Alpsoy", "Arslanid", "Bektashi", "Demir", "Karahan", "Kutlug", "Yildirim" },
                new[] { "Ak", "Altin", "Besh", "Demir", "Kara", "Kizil", "Ordu", "Sary", "Yedi" },
                new[] { "balik", "kent", "ordu", "su", "tag", "yurt" },
                new[] { "Khanate of", "Beylik of", "Horde of", "Realm of" },
                new[] { "", " Yurt", " Khanate" });

            RegisterPack("mesopotamian_pack",
                new[] { "Adad", "Ashur", "Bel", "Enlil", "Hammurabi", "Nabu", "Naram", "Sin" },
                new[] { "Amat", "Beltani", "Enheduanna", "Ishtar", "Kubaba", "Ninsun", "Shala", "Tashmetu" },
                new[] { "Ashurid", "Bel-iddin", "Enlilani", "Nabuid", "Sin-magir", "Ur-Nammu", "Zimri" },
                new[] { "Akkad", "Ashur", "Babil", "Dur", "Eridu", "Kish", "Lagash", "Nippur", "Ur" },
                new[] { "", "esh", "ki", "lib", "mur", "tum" },
                new[] { "Kingdom of", "Empire of", "City-State of", "Realm of" },
                new[] { "", "ki", "um" });

            RegisterPack("egyptian_pack",
                new[] { "Amenhotep", "Horemheb", "Khaemwaset", "Menes", "Nekau", "Ramose", "Senenmut", "Thutmose" },
                new[] { "Ahhotep", "Baket", "Hatshepsut", "Merit", "Nefertari", "Neith", "Tia", "Tuya" },
                new[] { "Amenemhat", "Hori", "Kagemni", "Menkaure", "Nakhtef", "Ramessid", "Userkaf" },
                new[] { "Abu", "Bast", "Djed", "Ineb", "Khem", "Men", "Nekhen", "Per", "Waset" },
                new[] { "", "et", "hotep", "khet", "polis", "ra" },
                new[] { "Nome of", "Kingdom of", "Black Land of", "Realm of" },
                new[] { "", "et", "ra" });

            RegisterPack("chinese_pack",
                new[] { "An", "Bo", "Chen", "Jian", "Liang", "Shen", "Wei", "Zhao" },
                new[] { "Ai", "Fang", "Hua", "Lan", "Lian", "Mei", "Xiu", "Yan" },
                new[] { "Chen", "Guo", "Li", "Liu", "Sun", "Wang", "Wu", "Zhang" },
                new[] { "An", "Chang", "Guang", "Han", "Jin", "Long", "Qing", "Shan", "Yun" },
                new[] { "", "cheng", "du", "jing", "shan", "zhou" },
                new[] { "Kingdom of", "Mandate of", "Prefecture of", "Realm of" },
                new[] { "", " Zhou", " Guo" });

            RegisterPack("korean_pack",
                new[] { "Do-yun", "Hwan", "Ji-ho", "Min-jun", "Seo-jun", "Tae", "Won", "Yeong" },
                new[] { "Ara", "Eun-ji", "Hana", "Ji-woo", "Min-seo", "Seoyeon", "Yuna", "Yuri" },
                new[] { "Choi", "Gim", "Jeong", "Kang", "Lee", "Park", "Yun" },
                new[] { "Baek", "Han", "Jin", "Nam", "Pyong", "Seo", "Song", "Won" },
                new[] { "", "ju", "san", "seong", "won" },
                new[] { "Kingdom of", "Province of", "Realm of", "Crown of" },
                new[] { "", "guk", "seong" });

            RegisterPack("indian_pack",
                new[] { "Arjun", "Bhaskar", "Devendra", "Harsha", "Kiran", "Rajan", "Vikram", "Yash" },
                new[] { "Anika", "Devika", "Indira", "Kalyani", "Mira", "Priya", "Savitri", "Vasudha" },
                new[] { "Chandra", "Gupta", "Kapoor", "Patel", "Rao", "Sharma", "Varma" },
                new[] { "Amar", "Chandra", "Deva", "Kali", "Naga", "Pura", "Raja", "Vijaya" },
                new[] { "", "garh", "gram", "nagar", "pur", "pura" },
                new[] { "Raj of", "Maharajya of", "Mandala of", "Realm of" },
                new[] { "", "pur", "desh" });

            RegisterPack("mongolic_pack",
                new[] { "Altan", "Batu", "Bayan", "Chagatai", "Khasar", "Mongke", "Temur", "Yesugei" },
                new[] { "Altantsetseg", "Borte", "Khulan", "Mandukhai", "Sarnai", "Soyolmaa", "Toregene", "Yisui" },
                new[] { "Borjigin", "Kereyid", "Naiman", "Oirat", "Onggirat", "Tatar", "Uriankhai" },
                new[] { "Altan", "Bayan", "Khara", "Koke", "Mergen", "Ordu", "Sari", "Tumen" },
                new[] { "gol", "khot", "nuur", "tal", "tologoi", "uur" },
                new[] { "Khanate of", "Horde of", "Ulus of", "Realm of" },
                new[] { "", " Ulus", " Khanate" });

            RegisterPack("tibetan_pack",
                new[] { "Dorje", "Jampa", "Karma", "Lobsang", "Namgyal", "Pema", "Tenzin", "Wangchuk" },
                new[] { "Deki", "Dolma", "Kelsang", "Lhamo", "Pema", "Sonam", "Tsering", "Yangchen" },
                new[] { "Gyatso", "Khangsar", "Namgyal", "Rinpoche", "Tashi", "Wangdu", "Zangpo" },
                new[] { "Chok", "Ganden", "Kham", "Lha", "Nam", "Pema", "Tashi", "Yar" },
                new[] { "", "dzong", "ling", "ri", "tang", "tse" },
                new[] { "Kingdom of", "Dzong of", "Realm of", "Highland of" },
                new[] { "", " Ling", " Dzong" });

            RegisterPack("southeast_asian_pack",
                new[] { "Ananda", "Boun", "Chai", "Dara", "Kiet", "Somchai", "Sophon", "Vannak" },
                new[] { "Achara", "Bopha", "Chan", "Dara", "Kanya", "Mali", "Sokha", "Vanna" },
                new[] { "Ang", "Chakri", "Kosal", "Phan", "Sok", "Suriya", "Vong" },
                new[] { "Ang", "Ban", "Chiang", "Dong", "Kampong", "Luang", "Phnom", "Si" },
                new[] { "", "buri", "kok", "luang", "nakhon", "thani" },
                new[] { "Mandala of", "Kingdom of", "Realm of", "Crown of" },
                new[] { "", "buri", "thani" });

            RegisterPack("african_pack",
                new[] { "Adebayo", "Chibuzo", "Jabari", "Kwame", "Mandla", "Nnamdi", "Omari", "Tafari" },
                new[] { "Abena", "Adaeze", "Amina", "Ayotunde", "Nala", "Nomsa", "Sade", "Zola" },
                new[] { "Afolayan", "Diop", "Keita", "Mensah", "Okonkwo", "Toure", "Zulu" },
                new[] { "Aba", "Boma", "Ile", "Kum", "Mali", "Nia", "Oyo", "Sanga", "Zan" },
                new[] { "", "ba", "ko", "la", "ra", "zi" },
                new[] { "Kingdom of", "Chiefdom of", "Realm of", "Confederacy of" },
                new[] { "", "land", "ba" });

            RegisterPack("ethiopian_pack",
                new[] { "Amha", "Dawit", "Gebre", "Iyasu", "Kassa", "Mikael", "Tewodros", "Yohannes" },
                new[] { "Abeba", "Aster", "Desta", "Hirut", "Makeda", "Menen", "Sahle", "Zewditu" },
                new[] { "Aksumite", "Gondar", "Mariam", "Selassie", "Tafari", "Wolde", "Zagwe" },
                new[] { "Adwa", "Aksum", "Debre", "Gondar", "Lal", "Mek", "Tana", "Wollo" },
                new[] { "", "ab", "dar", "ela", "wa" },
                new[] { "Kingdom of", "Negusate of", "Highland of", "Realm of" },
                new[] { "", "wa", "dar" });
        }

        private void RegisterFantasyPacks()
        {
            // Фэнтезийные наборы являются оригинальными офлайн-списками для нечеловеческих доменов ACKS:
            // отдельные корни уменьшают повторяемость названий при массовой генерации регионов.
            RegisterPack("dwarf",
                new[]
                {
                    "Bardin", "Beldrak", "Borin", "Dagrim", "Dorin", "Durgrim", "Fargrim", "Garrik",
                    "Haldor", "Kargun", "Kazrik", "Korin", "Morgrim", "Norvik", "Ordrin", "Rurik",
                    "Skalf", "Thrain", "Torvik", "Varrik", "Vondrak", "Yorgrim"
                },
                new[]
                {
                    "Aldra", "Bera", "Dagna", "Dora", "Eldis", "Freyda", "Gilda", "Grenda",
                    "Hilda", "Kelda", "Morga", "Nalda", "Ragna", "Sigrun", "Tora", "Varda"
                },
                new[]
                {
                    "Amberdelve", "Anvilborn", "Deepmantle", "Emberhand", "Forgeheart", "Goldvein",
                    "Granitebrow", "Hammerdeep", "Ironbrow", "Runeshield", "Silverdelve", "Stonebeard",
                    "Trueanvil", "Vaultkeeper"
                },
                new[]
                {
                    "Anvil", "Barak", "Deep", "Dur", "Ember", "Forge", "Gold", "Granite", "Hammer",
                    "Iron", "Kaz", "Krag", "Mith", "Rune", "Silver", "Stone", "Thor", "Vault"
                },
                new[] { "bar", "delve", "deep", "dun", "forge", "gate", "grum", "hall", "hold", "kar", "khaz", "vault" },
                new[] { "Vault of", "Hold of", "Deep Realm of", "Kingdom Under" },
                new[] { "", "hold", "deep", "vault" });

            RegisterPack("elf",
                new[]
                {
                    "Aelar", "Aerendil", "Caelion", "Elarion", "Faelar", "Galendir", "Itharion", "Laerion",
                    "Maelion", "Naerion", "Orithil", "Saelion", "Thalion", "Vaelir", "Yllarion"
                },
                new[]
                {
                    "Aelira", "Caelith", "Elaria", "Faelwen", "Ilyria", "Laerwen", "Maeriel", "Nimriel",
                    "Saelith", "Sylara", "Taelwen", "Vaelora", "Yllaria"
                },
                new[]
                {
                    "Brightleaf", "Dawnbough", "Evenstar", "Greenbough", "Moonbrook", "Silverbough",
                    "Starwillow", "Sunpetal", "Thornsong", "Windwhisper"
                },
                new[]
                {
                    "Ael", "Briar", "Cael", "Dawn", "Elar", "Faen", "Glen", "Ithil", "Laer", "Moon",
                    "Nim", "Sae", "Silver", "Star", "Syl", "Thorn", "Vael", "Willow"
                },
                new[] { "bough", "dell", "fall", "glade", "grove", "lind", "mere", "song", "thil", "vale", "wen" },
                new[] { "Fastness of", "Green Realm of", "Moon Court of", "Sylvan Realm of" },
                new[] { "", "wood", "glade", "vale" });

            RegisterPack("orc",
                new[]
                {
                    "Agdur", "Barg", "Drog", "Ghaz", "Ghoruk", "Grask", "Karg", "Mogdur", "Narg",
                    "Raguk", "Sharg", "Thog", "Urzag", "Varg", "Zog"
                },
                new[]
                {
                    "Agra", "Borga", "Draga", "Ghazra", "Grakka", "Korga", "Mogra", "Narga",
                    "Rakka", "Sharga", "Ugla", "Vorga", "Zagra"
                },
                new[]
                {
                    "Blacktusk", "Bloodfang", "Bonecleaver", "Brokenaxe", "Fangmaw", "Grimhide",
                    "Ironjaw", "Redhand", "Skullsplitter", "Wolfscar"
                },
                new[]
                {
                    "Axe", "Black", "Blood", "Bone", "Fang", "Ghor", "Grim", "Iron", "Krag",
                    "Red", "Skull", "Stone", "Tusk", "Ur", "Wolf"
                },
                new[] { "clan", "fang", "gar", "gor", "hold", "maw", "rak", "scar", "tusk", "zag" },
                new[] { "Horde of", "Clan of", "Warhold of", "Skull-Realm of" },
                new[] { "", " Horde", " Clan", " Warhold" });

            RegisterPack("beastman",
                new[]
                {
                    "Brak", "Chor", "Drakk", "Gnar", "Gor", "Grish", "Hruk", "Karn", "Krull",
                    "Murn", "Rhor", "Sark", "Skarn", "Thrak", "Ugruk", "Vorn"
                },
                new[]
                {
                    "Bara", "Churra", "Drakka", "Garna", "Ghora", "Grisha", "Karna", "Murna",
                    "Rhora", "Sarra", "Thraxa", "Ugra", "Vorna"
                },
                new[]
                {
                    "Blackhorn", "Bloodmane", "Brokenhorn", "Darkscale", "Fetidmaw", "Hookclaw",
                    "Redfang", "Rotfur", "Scaleback", "Shadowhoof", "Skullhorn", "Yelloweye"
                },
                new[]
                {
                    "Black", "Blood", "Bone", "Broken", "Claw", "Dark", "Fetid", "Gnarl", "Horn",
                    "Maw", "Red", "Rot", "Scale", "Shadow", "Skull", "Thorn", "Yellow"
                },
                new[] { "claw", "den", "fang", "horn", "maw", "pit", "scar", "scale", "snarl", "tor", "warren" },
                new[] { "Beast-Clan of", "Howling Hold of", "Savage Clan of", "War-Den of" },
                new[] { "", " Den", " Clan", " Maw" });

            RegisterPack("human_clan",
                new[]
                {
                    "Agar", "Batur", "Branok", "Draven", "Garr", "Hrod", "Kragan", "Mord",
                    "Radan", "Rurik", "Skold", "Taran", "Ulfar", "Veles", "Yar", "Zoran"
                },
                new[]
                {
                    "Alka", "Brana", "Dara", "Gordana", "Hilda", "Kara", "Mila", "Radka",
                    "Runa", "Sava", "Skara", "Tala", "Vesna", "Yaga", "Zora"
                },
                new[]
                {
                    "Bearhide", "Bloodoath", "Boarhelm", "Crowbanner", "Elkheart", "Firebrand",
                    "Frostwolf", "Ironhide", "Ravenmark", "Redaxe", "Stormhair", "Wolfkin"
                },
                new[]
                {
                    "Ash", "Bear", "Boar", "Crow", "Elk", "Fire", "Frost", "Iron",
                    "Raven", "Red", "Skull", "Storm", "Wolf"
                },
                new[] { "camp", "clan", "crag", "hold", "mark", "moot", "stead", "stone", "tor", "watch" },
                new[] { "Clan of", "Tribe of", "War-Camp of", "Oathhold of" },
                new[] { "", " Clan", " Moot", " Hold" });
        }

        private void RegisterPack(string key, string[] male, string[] female, string[] surnames, string[] roots, string[] suffixes, string[] realmPrefixes, string[] realmSuffixes)
        {
            packs[key] = new NamePack
            {
                Key = key,
                MaleNames = male,
                FemaleNames = female,
                Surnames = surnames,
                SettlementRoots = roots,
                SettlementSuffixes = suffixes,
                RealmPrefixes = realmPrefixes,
                RealmSuffixes = realmSuffixes
            };

            if (!key.EndsWith("_pack", StringComparison.OrdinalIgnoreCase))
            {
                cultureLabels[key] = MakeCultureLabel(key);
            }
        }

        private void RegisterManifestCultureAliases()
        {
            // Алиасы сохраняют в UI все культуры из manifests, но позволяют нескольким близким
            // культурам временно использовать один общий офлайн-набор имён.
            RegisterCultureAlias("germanic_ancient", "german");
            RegisterCultureAlias("bavarian", "german");
            RegisterCultureAlias("swiss", "german");
            RegisterCultureAlias("irish", "celtic_pack");
            RegisterCultureAlias("scottish", "celtic_pack");
            RegisterCultureAlias("celtic", "celtic_pack");
            RegisterCultureAlias("scandinavian", "old_norse");
            RegisterCultureAlias("danish", "old_norse");
            RegisterCultureAlias("norman", "romance_pack");
            RegisterCultureAlias("slavic_generic", "russian");
            RegisterCultureAlias("romance", "romance_pack");
            RegisterCultureAlias("french", "romance_pack");
            RegisterCultureAlias("italian", "romance_pack");
            RegisterCultureAlias("spanish", "romance_pack");
            RegisterCultureAlias("catalan", "romance_pack");
            RegisterCultureAlias("latin_ancient_roman", "ancient_roman_pack");
            RegisterCultureAlias("auran", "ancient_roman_pack");
            RegisterCultureAlias("auran_empire", "ancient_roman_pack");
            RegisterCultureAlias("argollean", "elf");
            RegisterCultureAlias("argollean_elven", "elf");
            RegisterCultureAlias("somirean", "indian_pack");
            RegisterCultureAlias("jutlandic", "old_norse");

            RegisterCultureAlias("armenian", "caucasus_pack");
            RegisterCultureAlias("georgian", "caucasus_pack");
            RegisterCultureAlias("chechen", "caucasus_pack");
            RegisterCultureAlias("dagestani", "caucasus_pack");
            RegisterCultureAlias("byzantine", "romance_pack");
            RegisterCultureAlias("turkish", "turkic_pack");
            RegisterCultureAlias("steppe_turkic", "turkic_pack");
            RegisterCultureAlias("mesopotamian", "mesopotamian_pack");
            RegisterCultureAlias("sumerian", "mesopotamian_pack");
            RegisterCultureAlias("assyrian", "mesopotamian_pack");
            RegisterCultureAlias("babylonian", "mesopotamian_pack");
            RegisterCultureAlias("egyptian_ancient", "egyptian_pack");
            RegisterCultureAlias("phoenician", "arabic");
            RegisterCultureAlias("carthaginian", "arabic");
            RegisterCultureAlias("canaanite", "arabic");
            RegisterCultureAlias("hittite", "mesopotamian_pack");
            RegisterCultureAlias("urartian", "caucasus_pack");

            RegisterCultureAlias("chinese", "chinese_pack");
            RegisterCultureAlias("korean", "korean_pack");
            RegisterCultureAlias("hokkaido", "japanese");
            RegisterCultureAlias("indian", "indian_pack");
            RegisterCultureAlias("mongolic", "mongolic_pack");
            RegisterCultureAlias("tibetan", "tibetan_pack");
            RegisterCultureAlias("manchu", "mongolic_pack");
            RegisterCultureAlias("vietnamese", "southeast_asian_pack");
            RegisterCultureAlias("southeast_asian_generic", "southeast_asian_pack");

            RegisterCultureAlias("african_generic", "african_pack");
            RegisterCultureAlias("yoruba", "african_pack");
            RegisterCultureAlias("igbo", "african_pack");
            RegisterCultureAlias("swahili", "african_pack");
            RegisterCultureAlias("zulu", "african_pack");
            RegisterCultureAlias("ethiopian", "ethiopian_pack");
            RegisterCultureAlias("nubian", "egyptian_pack");

        }

        private void RegisterCultureAlias(string cultureKey, string packKey)
        {
            culturePackAliases[cultureKey] = packKey;
            if (!cultureLabels.ContainsKey(cultureKey))
            {
                cultureLabels[cultureKey] = MakeCultureLabel(cultureKey);
            }
        }

        private NamePack ResolvePack(string cultureKey)
        {
            if (!string.IsNullOrWhiteSpace(cultureKey))
            {
                NamePack exact;
                if (packs.TryGetValue(cultureKey, out exact)) return exact;

                string key = cultureKey.ToLowerInvariant();
                string packKey;
                if (culturePackAliases.TryGetValue(key, out packKey) && packs.TryGetValue(packKey, out exact))
                {
                    return exact;
                }

                if (key.Contains("slavic") || key.Contains("russian")) return packs["russian"];
                if (key.Contains("dwarf")) return packs["dwarf"];
                if (key.Contains("elf") || key.Contains("elven")) return packs["elf"];
                if (key.Contains("orc")) return packs["orc"];
                if (key.Contains("beastman") || key.Contains("beastmen")) return packs["beastman"];
                if (key.Contains("german") || key.Contains("bavarian") || key.Contains("swiss")) return packs["german"];
                if (key.Contains("norse") || key.Contains("scandinavian") || key.Contains("danish")) return packs["old_norse"];
                if (key.Contains("arabic") || key.Contains("canaanite") || key.Contains("phoenician")) return packs["arabic"];
                if (key.Contains("persian") || key.Contains("armenian") || key.Contains("georgian")) return packs["persian"];
                if (key.Contains("japanese") || key.Contains("hokkaido")) return packs["japanese"];
                if (key.Contains("english") || key.Contains("norman") || key.Contains("celtic") || key.Contains("irish") || key.Contains("scottish")) return packs["english"];
            }

            return packs["generic"];
        }

        private string Pick(Random random, string[] values)
        {
            if (values == null || values.Length == 0) return "";
            return values[random.Next(values.Length)];
        }

        private string TransliterateName(string value)
        {
            return TransliterateName(value, null);
        }

        private string TransliterateName(string value, string cultureKey)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            string result = value;
            if (UsesEnglishFantasyPhonetics(cultureKey))
            {
                result = ApplyEnglishFantasyPhonetics(result);
            }

            return TransliterateNameCore(result);
        }

        private bool UsesEnglishFantasyPhonetics(string cultureKey)
        {
            NamePack pack = ResolvePack(cultureKey);
            string key = pack == null ? "" : pack.Key;
            return string.Equals(key, "english", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "generic", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "human_clan", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "dwarf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "elf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "orc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "beastman", StringComparison.OrdinalIgnoreCase);
        }

        private string ApplyEnglishFantasyPhonetics(string value)
        {
            // Эти правила дают русской локализации не буквенную транслитерацию, а звучание,
            // близкое к привычной фэнтезийной адаптации: Ravenbury -> Рейвенбьюри.
            string result = value;
            string[,] rules =
            {
                { "Hawthorne", "Хоторн" }, { "Blackwood", "Блэквуд" }, { "Fairborne", "Фейрберн" },
                { "Underhill", "Андерхилл" }, { "Westmere", "Вестмир" }, { "Ashford", "Эшфорд" },
                { "Kingsley", "Кингсли" }, { "Aveline", "Эвелин" }, { "Winifred", "Винифред" },
                { "Brightleaf", "Брайтлиф" }, { "Dawnbough", "Доунбау" }, { "Evenstar", "Ивенстар" },
                { "Greenbough", "Гринбау" }, { "Moonbrook", "Мунбрук" }, { "Silverbough", "Сильвербау" },
                { "Starwillow", "Старвиллоу" }, { "Sunpetal", "Санпетал" }, { "Thornsong", "Торнсонг" },
                { "Windwhisper", "Виндхвиспер" }, { "Amberdelve", "Эмберделв" }, { "Anvilborn", "Энвилборн" },
                { "Deepmantle", "Дипмантл" }, { "Emberhand", "Эмберхэнд" }, { "Forgeheart", "Форджхарт" },
                { "Goldvein", "Голдвейн" }, { "Granitebrow", "Гранитбрау" }, { "Hammerdeep", "Хаммердип" },
                { "Ironbrow", "Айронбрау" }, { "Runeshield", "Руншилд" }, { "Silverdelve", "Сильверделв" },
                { "Stonebeard", "Стоунбирд" }, { "Trueanvil", "Труэнвил" }, { "Vaultkeeper", "Волткипер" },
                { "Blacktusk", "Блэктаск" }, { "Bloodfang", "Бладфанг" }, { "Bonecleaver", "Боункливер" },
                { "Brokenaxe", "Броукенакс" }, { "Fangmaw", "Фангмо" }, { "Grimhide", "Гримхайд" },
                { "Ironjaw", "Айронджо" }, { "Redhand", "Редхэнд" }, { "Skullsplitter", "Скаллсплиттер" },
                { "Wolfscar", "Вулфскар" }, { "Blackhorn", "Блэкхорн" }, { "Bloodmane", "Бладмейн" },
                { "Brokenhorn", "Броукенхорн" }, { "Darkscale", "Даркскейл" }, { "Fetidmaw", "Фетидмо" },
                { "Hookclaw", "Хуккло" }, { "Redfang", "Редфанг" }, { "Rotfur", "Ротфур" },
                { "Scaleback", "Скейлбэк" }, { "Shadowhoof", "Шэдоухуф" }, { "Skullhorn", "Скаллхорн" },
                { "Yelloweye", "Йеллоуай" },
                { "Bearhide", "Беархайд" }, { "Bloodoath", "Бладоут" }, { "Boarhelm", "Борхелм" },
                { "Crowbanner", "Кроубаннер" }, { "Elkheart", "Элкхарт" }, { "Firebrand", "Файрбрэнд" },
                { "Frostwolf", "Фроствулф" }, { "Ironhide", "Айронхайд" }, { "Ravenmark", "Рейвенмарк" },
                { "Redaxe", "Редакс" }, { "Stormhair", "Штормхейр" }, { "Wolfkin", "Вулфкин" },
                { "Batur", "Батур" }, { "Branok", "Бранок" }, { "Draven", "Дрейвен" },
                { "Kragan", "Краган" }, { "Radan", "Радан" }, { "Skold", "Сколд" },
                { "Taran", "Таран" }, { "Ulfar", "Ульфар" }, { "Veles", "Велес" },
                { "Zoran", "Зоран" }, { "Brana", "Брана" }, { "Gordana", "Гордана" },
                { "Radka", "Радка" }, { "Runa", "Руна" }, { "Skara", "Скара" },
                { "Tala", "Тала" }, { "Yaga", "Яга" }, { "Zora", "Зора" },

                { "Aldric", "Олдрик" }, { "Cedric", "Седрик" }, { "Edwin", "Эдвин" },
                { "Godwin", "Годвин" }, { "Leofric", "Леофрик" }, { "Oswin", "Освин" },
                { "Rowan", "Роуэн" }, { "Wystan", "Вистан" }, { "Edith", "Эдит" },
                { "Elena", "Элена" }, { "Isolde", "Изольда" }, { "Mabel", "Мейбл" },
                { "Rowena", "Роуэна" }, { "Ysabel", "Изабель" },

                { "Raven", "Рейвен" }, { "Stone", "Стоун" }, { "Silver", "Сильвер" },
                { "Elden", "Элден" }, { "Fair", "Фейр" }, { "Grey", "Грей" },
                { "Briar", "Брайар" }, { "Kings", "Кингс" }, { "High", "Хай" },
                { "Ash", "Эш" },
                { "Bright", "Брайт" }, { "White", "Уайт" }, { "Black", "Блэк" },
                { "Green", "Грин" }, { "Golden", "Голден" }, { "North", "Норт" },
                { "South", "Саут" }, { "East", "Ист" }, { "West", "Вест" },
                { "Old", "Олд" }, { "New", "Нью" }, { "Low", "Лоу" },
                { "River", "Ривер" }, { "Red", "Ред" }, { "Moon", "Мун" },
                { "Star", "Стар" }, { "Thorn", "Торн" }, { "Dusk", "Даск" },
                { "Iron", "Айрон" }, { "Deep", "Дип" }, { "Forge", "Фордж" },
                { "Gold", "Голд" }, { "Granite", "Гранит" }, { "Hammer", "Хаммер" },
                { "Rune", "Рун" }, { "Vault", "Волт" }, { "Blood", "Блад" },
                { "Bone", "Боун" }, { "Fang", "Фанг" }, { "Grim", "Грим" },
                { "Skull", "Скалл" }, { "Wolf", "Вулф" }, { "Bear", "Беар" },
                { "Boar", "Бор" }, { "Crow", "Кроу" }, { "Elk", "Элк" },
                { "Fire", "Файр" }, { "Frost", "Фрост" }, { "Storm", "Шторм" },
                { "Claw", "Кло" },
                { "Dark", "Дарк" }, { "Horn", "Хорн" }, { "Scale", "Скейл" },
                { "Shadow", "Шэдоу" }, { "Yellow", "Йеллоу" }, { "Broken", "Броукен" },

                { "bridge", "бридж" }, { "borne", "берн" }, { "bury", "бьюри" },
                { "worth", "ворт" }, { "wick", "вик" }, { "ford", "форд" },
                { "dale", "дейл" }, { "field", "филд" }, { "gate", "гейт" },
                { "grove", "гроув" }, { "haven", "хейвен" }, { "mere", "мир" },
                { "shire", "шир" }, { "brook", "брук" }, { "cliff", "клифф" },
                { "cross", "кросс" }, { "fall", "фолл" }, { "hall", "холл" },
                { "hill", "хилл" }, { "keep", "кип" }, { "mill", "милл" },
                { "reach", "рич" }, { "spring", "спринг" }, { "watch", "вотч" },
                { "wood", "вуд" }, { "vale", "вейл" }, { "land", "ленд" },
                { "hold", "холд" }, { "ridge", "ридж" }, { "mark", "марк" },
                { "camp", "кэмп" }, { "clan", "клан" }, { "crag", "крэг" },
                { "moot", "мут" }, { "stead", "стед" }, { "tor", "тор" },
                { "ton", "тон" }, { "ham", "хэм" }, { "bough", "бау" },
                { "dell", "делл" }, { "glade", "глейд" }, { "lind", "линд" },
                { "song", "сонг" }, { "willow", "виллоу" }, { "leaf", "лиф" },
                { "petal", "петал" }, { "whisper", "хвиспер" }, { "delve", "делв" },
                { "anvil", "энвил" }, { "mantle", "мантл" }, { "heart", "харт" },
                { "vein", "вейн" }, { "brow", "брау" }, { "beard", "бирд" },
                { "shield", "шилд" }, { "tusk", "таск" }, { "cleaver", "кливер" },
                { "axe", "акс" }, { "hide", "хайд" }, { "jaw", "джо" },
                { "hand", "хэнд" }, { "scar", "скар" }, { "mane", "мейн" },
                { "maw", "мо" }, { "hoof", "хуф" }, { "eye", "ай" }
            };

            for (int i = 0; i < rules.GetLength(0); i++)
            {
                result = ReplaceNamePart(result, rules[i, 0], rules[i, 1]);
            }

            return result;
        }

        private string ReplaceNamePart(string value, string source, string target)
        {
            return Regex.Replace(
                value,
                Regex.Escape(source),
                match => MatchNamePartCase(value, target, match.Value, match.Index),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private string MatchNamePartCase(string fullValue, string target, string source, int index)
        {
            if (string.IsNullOrWhiteSpace(target)) return target;
            if (source.All(c => !char.IsLetter(c) || char.IsUpper(c))) return target.ToUpperInvariant();
            if (char.IsUpper(source[0]) && IsNameWordStart(fullValue, index)) return UpperFirst(target);
            return LowerFirst(target);
        }

        private bool IsNameWordStart(string value, int index)
        {
            if (index <= 0) return true;
            char previous = value[index - 1];
            return char.IsWhiteSpace(previous) || previous == '-' || previous == '\'' || previous == '’';
        }

        private string TransliterateNameCore(string value)
        {
            string result = value;
            string[,] digraphs =
            {
                { "sch", "щ" }, { "sh", "ш" }, { "ch", "ч" }, { "zh", "ж" },
                { "kh", "х" }, { "ph", "ф" }, { "th", "т" }, { "gh", "г" },
                { "ya", "я" }, { "yo", "ё" }, { "yu", "ю" }, { "ye", "е" },
                { "ae", "э" }, { "oe", "ё" }
            };

            foreach (string casing in new[] { "lower", "upper", "title" })
            {
                for (int i = 0; i < digraphs.GetLength(0); i++)
                {
                    string source = digraphs[i, 0];
                    string target = digraphs[i, 1];
                    if (casing == "upper")
                    {
                        result = result.Replace(source.ToUpperInvariant(), target.ToUpperInvariant());
                    }
                    else if (casing == "title")
                    {
                        result = result.Replace(char.ToUpperInvariant(source[0]) + source.Substring(1), char.ToUpperInvariant(target[0]) + target.Substring(1));
                    }
                    else
                    {
                        result = result.Replace(source, target);
                    }
                }
            }

            Dictionary<char, string> map = new Dictionary<char, string>
            {
                { 'a', "а" }, { 'b', "б" }, { 'c', "к" }, { 'd', "д" }, { 'e', "е" }, { 'f', "ф" },
                { 'g', "г" }, { 'h', "х" }, { 'i', "и" }, { 'j', "дж" }, { 'k', "к" }, { 'l', "л" },
                { 'm', "м" }, { 'n', "н" }, { 'o', "о" }, { 'p', "п" }, { 'q', "к" }, { 'r', "р" },
                { 's', "с" }, { 't', "т" }, { 'u', "у" }, { 'v', "в" }, { 'w', "в" }, { 'x', "кс" },
                { 'y', "и" }, { 'z', "з" }
            };

            List<string> chars = new List<string>();
            foreach (char raw in result)
            {
                char lower = char.ToLowerInvariant(raw);
                string mapped;
                if (map.TryGetValue(lower, out mapped))
                {
                    chars.Add(char.IsUpper(raw) ? UpperFirst(mapped) : mapped);
                }
                else
                {
                    chars.Add(raw.ToString());
                }
            }

            return string.Join("", chars);
        }

        private string UpperFirst(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToUpperInvariant(value[0]) + (value.Length > 1 ? value.Substring(1) : "");
        }

        private string LowerFirst(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToLowerInvariant(value[0]) + (value.Length > 1 ? value.Substring(1) : "");
        }

        private string LocalizeRealmTier(string tier)
        {
            return RealmTitleCatalog.RealmTitle("", tier, true, "");
        }

        private string GetCultureLabel(string key, bool english)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";

            if (!english)
            {
                string russian;
                if (RussianCultureLabels.TryGetValue(key, out russian))
                {
                    return russian;
                }
            }

            string label;
            return cultureLabels.TryGetValue(key, out label) ? label : MakeCultureLabel(key);
        }

        private static Dictionary<string, string> CreateRussianCultureLabels()
        {
            // UI показывает русские названия культур, но ключи остаются английскими и не ломают сохранения.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "african_generic", "Африканская обобщённая" },
                { "arabic", "Арабская" },
                { "armenian", "Армянская" },
                { "argollean", "Арголлейская" },
                { "argollean_elven", "Арголлейская эльфийская" },
                { "assyrian", "Ассирийская" },
                { "auran", "Ауранская" },
                { "auran_empire", "Ауранская имперская" },
                { "bavarian", "Баварская" },
                { "babylonian", "Вавилонская" },
                { "beastman", "Зверолюдская" },
                { "beastmen", "Зверолюдская" },
                { "byzantine", "Византийская" },
                { "canaanite", "Ханаанская" },
                { "carthaginian", "Карфагенская" },
                { "catalan", "Каталонская" },
                { "celtic", "Кельтская" },
                { "chechen", "Чеченская" },
                { "chinese", "Китайская" },
                { "danish", "Датская" },
                { "dagestani", "Дагестанская" },
                { "dwarf", "Дварфийская" },
                { "dwarven", "Дварфийская" },
                { "egyptian_ancient", "Древнеегипетская" },
                { "elf", "Эльфийская" },
                { "elven", "Эльфийская" },
                { "english", "Английская" },
                { "ethiopian", "Эфиопская" },
                { "french", "Французская" },
                { "generic", "Обобщённая" },
                { "georgian", "Грузинская" },
                { "german", "Немецкая" },
                { "germanic_ancient", "Древнегерманская" },
                { "hittite", "Хеттская" },
                { "hokkaido", "Хоккайдо" },
                { "human_clan", "Клановая человеческая" },
                { "igbo", "Игбо" },
                { "indian", "Индийская" },
                { "irish", "Ирландская" },
                { "italian", "Итальянская" },
                { "japanese", "Японская" },
                { "jutlandic", "Ютландская" },
                { "korean", "Корейская" },
                { "latin_ancient_roman", "Древнеримская" },
                { "manchu", "Маньчжурская" },
                { "mesopotamian", "Месопотамская" },
                { "mongolic", "Монгольская" },
                { "norman", "Норманнская" },
                { "nubian", "Нубийская" },
                { "old_norse", "Древнескандинавская" },
                { "orc", "Орочья" },
                { "persian", "Персидская" },
                { "phoenician", "Финикийская" },
                { "romance", "Романская" },
                { "russian", "Русская" },
                { "scandinavian", "Скандинавская" },
                { "scottish", "Шотландская" },
                { "slavic_generic", "Славянская обобщённая" },
                { "somirean", "Сомирейская" },
                { "southeast_asian_generic", "Юго-восточноазиатская обобщённая" },
                { "spanish", "Испанская" },
                { "steppe_turkic", "Степная тюркская" },
                { "sumerian", "Шумерская" },
                { "swahili", "Суахили" },
                { "swiss", "Швейцарская" },
                { "tibetan", "Тибетская" },
                { "turkish", "Турецкая" },
                { "urartian", "Урартская" },
                { "vietnamese", "Вьетнамская" },
                { "yoruba", "Йоруба" },
                { "zulu", "Зулусская" }
            };
        }

        private string MakeCultureLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            string[] parts = key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : "");
            }

            return string.Join(" ", parts);
        }
    }
}
