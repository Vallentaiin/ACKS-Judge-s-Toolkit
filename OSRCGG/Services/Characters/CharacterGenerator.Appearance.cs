using System;
using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed partial class CharacterGenerator
    {
        // Внешность, возраст и броски персонажа собраны в отдельный блок генерации.
        private string GenerateNpcAppearance()
        {
            string race = GetAppearanceRace();
            bool female = GetAppearanceSexIsFemale();
            int height = RollBaselineHeight(race, female);
            int weight = RollBaselineWeight(race, female);
            string build = ApplyBuild(race, ref height, ref weight);
            string eyeColor = RollEyeColor(race);
            string hairColor = RollHairColor(race);
            string hairTexture = RollHairTexture(race);
            string skinColor = RollSkinColor(race);
            string features = RollPhysicalFeatures();

            return L("Race baseline: ", "Базовая раса: ") + LocalizeAppearanceValue(race) + ", " + (female ? L("female", "жен.") : L("male", "муж.")) + Environment.NewLine +
                L("Height/Weight: ", "Рост/вес: ") + FormatHeight(height) + ", " + FormatWeight(weight) + Environment.NewLine +
                L("Build: ", "Телосложение: ") + build + Environment.NewLine +
                L("Eyes: ", "Глаза: ") + LocalizeAppearanceValue(eyeColor) + Environment.NewLine +
                L("Hair: ", "Волосы: ") + LocalizeAppearanceValue(hairColor) + ", " + LocalizeAppearanceValue(hairTexture) + Environment.NewLine +
                L("Skin: ", "Кожа: ") + LocalizeAppearanceValue(skinColor) + Environment.NewLine +
                L("Physical features: ", "Физические особенности: ") + LocalizeAppearanceValue(features);
        }

        private string GetAppearanceRace()
        {
            string className = currentClassName;
            if (className.Contains("Dwarven")) return "Dwarf";
            if (className.Contains("Elven")) return "Elf";
            if (className.Contains("Nobiran")) return "Nobiran";
            if (className.Contains("Zaharan")) return "Zaharan";
            return "Human";
        }

        private bool GetAppearanceSexIsFemale()
        {
            if (currentSex == "Female") return true;
            if (currentSex == "Male") return false;
            return characterRandom.Next(2) == 0;
        }

        private int RollBaselineHeight(string race, bool female)
        {
            if (race == "Dwarf") return (female ? 41 : 43) + RollDice(2, 4);
            if (race == "Elf") return (female ? 57 : 61) + RollDice(2, 4);
            if (race == "Gnome") return (female ? 32 : 34) + RollDice(2, 4);
            if (race == "Halfling") return (female ? 30 : 32) + RollDice(2, 4);
            return (female ? 55 : 60) + RollDice(2, 6);
        }

        private int RollBaselineWeight(string race, bool female)
        {
            if (race == "Dwarf") return (female ? 110 : 130) + RollDice(4, 10);
            if (race == "Elf") return (female ? 90 : 110) + RollDice(6, 6);
            if (race == "Gnome") return (female ? 25 : 30) + RollDice(4, 6);
            if (race == "Halfling") return (female ? 20 : 25) + RollDice(4, 8);
            return (female ? 90 : 110) + RollDice(8, 6);
        }

        private string ApplyBuild(string race, ref int height, ref int weight)
        {
            int roll = RollDice(2, 6) + AttributeBonus((int)characterAttributes["STR"]) * 2;
            string build;
            int heightPercent = 0;
            int weightPercent = 0;

            if (race == "Dwarf")
            {
                if (roll <= 1) { build = "Small"; heightPercent = -10; weightPercent = -30; }
                else if (roll <= 3) { build = "Slim"; weightPercent = -20; }
                else if (roll <= 7) { build = "Average"; }
                else if (roll <= 11) { build = "Broad"; weightPercent = 20; }
                else if (roll <= 13) { build = "Large"; heightPercent = 10; weightPercent = 30; }
                else { build = "Huge"; heightPercent = 20; weightPercent = 75; }
            }
            else if (race == "Elf")
            {
                if (roll <= 1) { build = "Small"; heightPercent = -10; weightPercent = -30; }
                else if (roll <= 6) { build = "Thin"; weightPercent = -10; }
                else if (roll <= 9) { build = "Average"; }
                else { build = "Tall"; heightPercent = 10; weightPercent = 30; }
            }
            else if (race == "Gnome")
            {
                if (roll <= 2) { build = "Small"; heightPercent = -10; weightPercent = -30; }
                else if (roll <= 4) { build = "Slim"; weightPercent = -20; }
                else if (roll <= 9) { build = "Average"; }
                else if (roll <= 11) { build = "Broad"; weightPercent = 20; }
                else { build = "Large"; heightPercent = 10; weightPercent = 30; }
            }
            else if (race == "Halfling")
            {
                if (roll <= 2) { build = "Small"; heightPercent = -10; weightPercent = -30; }
                else if (roll <= 4) { build = "Slim"; weightPercent = -20; }
                else if (roll <= 8) { build = "Average"; }
                else if (roll <= 11) { build = "Broad"; weightPercent = 20; }
                else { build = "Large"; heightPercent = 10; weightPercent = 30; }
            }
            else
            {
                if (roll <= 1) { build = "Small"; heightPercent = -10; weightPercent = -30; }
                else if (roll <= 4) { build = "Slim"; weightPercent = -20; }
                else if (roll <= 8) { build = "Average"; }
                else if (roll <= 10) { build = "Broad"; weightPercent = 20; }
                else if (roll <= 12) { build = "Large"; heightPercent = 10; weightPercent = 30; }
                else { build = "Huge"; heightPercent = 20; weightPercent = 75; }
            }

            height = ApplyPercent(height, heightPercent);
            weight = ApplyPercent(weight, weightPercent);
            return LocalizeAppearanceValue(build) + L(" (2d6 + STR modifier x2 = ", " (2d6 + модификатор STR x2 = ") + roll + ")";
        }

        private int ApplyPercent(int value, int percent)
        {
            return Math.Max(1, (int)Math.Round(value * (100 + percent) / 100.0));
        }

        private string FormatHeight(int inches)
        {
            int centimeters = (int)Math.Round(inches * 2.54);
            return (inches / 12) + "'" + (inches % 12) + "\" (" + inches + "\", " + centimeters + L(" cm", " см") + ")";
        }

        private string FormatWeight(int pounds)
        {
            double kilograms = pounds * 0.45359237;
            return pounds + L(" lbs", " фунтов") + " (" + kilograms.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + L(" kg", " кг") + ")";
        }

        private string LocalizeAppearanceValue(string value)
        {
            if (isEnglish || string.IsNullOrWhiteSpace(value)) return value;

            string exact;
            if (CharacterGenerationCatalog.AppearanceRu.TryGetValue(value, out exact)) return exact;

            if (value.Contains("; "))
            {
                return string.Join("; ", value.Split(new[] { "; " }, StringSplitOptions.None)
                    .Select(LocalizeAppearanceValue));
            }

            int categoryIndex = value.IndexOf(" - ", StringComparison.Ordinal);
            if (categoryIndex > 0)
            {
                string category = value.Substring(0, categoryIndex);
                string detail = value.Substring(categoryIndex + 3);
                return LocalizeAppearanceValue(category) + " - " + LocalizeAppearanceValue(detail);
            }

            if (value.Contains(", "))
            {
                return string.Join(", ", value.Split(new[] { ", " }, StringSplitOptions.None)
                    .Select(LocalizeAppearanceValue));
            }

            return value;
        }

        private string RollEyeColor(string race)
        {
            if (race == "Dwarf")
            {
                return RollAppearanceTable(new List<RollRange<string>>
                {
                    new RollRange<string>(1, 4, "Brown, Light"),
                    new RollRange<string>(5, 6, "Grey, Dark"),
                    new RollRange<string>(7, 8, "Grey, Light"),
                    new RollRange<string>(9, 10, "Grey-Brown, Dark"),
                    new RollRange<string>(11, 12, "Grey-Brown, Light"),
                    new RollRange<string>(13, 14, "Green, Light"),
                    new RollRange<string>(15, 16, "Green, Dark"),
                    new RollRange<string>(17, 18, "Hazel, Dark"),
                    new RollRange<string>(19, 20, "Hazel, Light")
                });
            }
            if (race == "Elf")
            {
                return RollAppearanceTable(new List<RollRange<string>>
                {
                    new RollRange<string>(1, 2, "Amber, Dark"),
                    new RollRange<string>(3, 4, "Amber, Light"),
                    new RollRange<string>(5, 6, "Blue, Dark"),
                    new RollRange<string>(7, 8, "Blue, Light"),
                    new RollRange<string>(9, 10, "Gold, Dark"),
                    new RollRange<string>(11, 12, "Gold, Light"),
                    new RollRange<string>(13, 14, "Hazel, Dark"),
                    new RollRange<string>(15, 16, "Hazel, Light"),
                    new RollRange<string>(17, 18, "Violet, Dark"),
                    new RollRange<string>(19, 20, "Violet, Light")
                });
            }
            if (race == "Gnome")
            {
                return RollAppearanceTable(EqualD20("Blue, Very Light", "Blue, Light", "Blue, Medium", "Blue, Dark", "Blue, Very Dark"));
            }
            if (race == "Halfling")
            {
                return RollAppearanceTable(new List<RollRange<string>>
                {
                    new RollRange<string>(1, 10, "Black"),
                    new RollRange<string>(11, 15, "Brown, Dark"),
                    new RollRange<string>(16, 20, "Brown, Light")
                });
            }

            return RollCombinedAppearanceTables(CreateHumanEyeTables());
        }

        private string RollHairColor(string race)
        {
            if (race == "Dwarf")
            {
                return RollCombinedAppearanceTables(new List<List<RollRange<string>>>
                {
                    new List<RollRange<string>> { new RollRange<string>(1, 7, "Black"), new RollRange<string>(8, 11, "Chestnut, Dark"), new RollRange<string>(12, 14, "Chestnut, Light"), new RollRange<string>(15, 17, "Grey, Dark"), new RollRange<string>(18, 20, "Grey, Light") },
                    new List<RollRange<string>> { new RollRange<string>(1, 5, "Auburn"), new RollRange<string>(6, 10, "Brown-Black"), new RollRange<string>(11, 15, "Brown, Rufous"), new RollRange<string>(16, 20, "Red, Dark") }
                });
            }
            if (race == "Elf")
            {
                return RollAppearanceTable(EqualD20("Blue-Black", "Gold", "Platinum", "Silver"));
            }
            if (race == "Gnome")
            {
                return RollAppearanceTable(EqualD20("Blonde, Light", "Blonde, Medium", "Blonde, Golden", "Blonde, Platinum", "Blonde, Strawberry"));
            }
            if (race == "Halfling")
            {
                return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 10, "Black"), new RollRange<string>(11, 14, "Brown, Dark"), new RollRange<string>(15, 18, "Brown, Medium"), new RollRange<string>(19, 20, "Brown, Light") });
            }

            return RollCombinedAppearanceTables(CreateHumanHairColorTables());
        }

        private string RollHairTexture(string race)
        {
            if (race == "Dwarf" || race == "Halfling") return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 10, "Curly"), new RollRange<string>(11, 20, "Wavy") });
            if (race == "Elf") return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 10, "Straight"), new RollRange<string>(11, 20, "Wavy") });
            if (race == "Gnome") return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 5, "Curly"), new RollRange<string>(6, 16, "Straight"), new RollRange<string>(17, 20, "Wavy") });
            return RollCombinedAppearanceTables(CreateHumanHairTextureTables());
        }

        private string RollSkinColor(string race)
        {
            if (race == "Dwarf")
            {
                return RollCombinedAppearanceTables(new List<List<RollRange<string>>>
                {
                    EqualD20("Brown, Medium", "Brown, Dark", "Brown, Very Dark", "Ocher", "Sienna"),
                    EqualD20("Brown, Light", "Brown, Medium", "Ocher", "Sienna")
                });
            }
            if (race == "Elf") return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 5, "White, Ivory"), new RollRange<string>(6, 15, "White, Pure"), new RollRange<string>(16, 20, "White, Snow") });
            if (race == "Gnome") return RollAppearanceTable(EqualD20("Brown, Light", "Brown, Medium", "Brown, Dark", "Brown, Very Dark", "Ocher"));
            if (race == "Halfling") return RollAppearanceTable(new List<RollRange<string>> { new RollRange<string>(1, 7, "Light Brown, Ruddy"), new RollRange<string>(8, 13, "Medium Brown, Ruddy"), new RollRange<string>(14, 20, "Pale, Ruddy") });
            return RollCombinedAppearanceTables(CreateHumanSkinTables());
        }

        private string RollPhysicalFeatures()
        {
            List<string> features = new List<string>();
            features.Add(RollPhysicalFeatureAverage());
            int chaBonus = AttributeBonus((int)characterAttributes["CHA"]);
            for (int i = 0; i < Math.Abs(chaBonus); i++)
            {
                features.Add(chaBonus < 0 ? RollPhysicalFeatureNegative() : RollPhysicalFeaturePositive());
            }
            return string.Join("; ", features.Distinct());
        }

        private string RollPhysicalFeatureNegative()
        {
            return RollAppearanceTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 2, "Build - Obese"), new RollRange<string>(3, 4, "Build - Hunchback"), new RollRange<string>(5, 6, "Build - Skeletal"),
                new RollRange<string>(7, 8, "Ears - Crumpled"), new RollRange<string>(9, 10, "Ears - Huge"), new RollRange<string>(11, 12, "Ears - Missing"), new RollRange<string>(13, 14, "Ears - Torn"),
                new RollRange<string>(15, 16, "Eyes - Bulging"), new RollRange<string>(17, 18, "Eyes - Cross-eyed"), new RollRange<string>(19, 20, "Eyes - One eye (mass scar tissue)"), new RollRange<string>(21, 22, "Eyes - One eye (stitched up)"), new RollRange<string>(23, 24, "Eyes - Wall-eyed"), new RollRange<string>(25, 26, "Eyes - Wandering eyes"),
                new RollRange<string>(27, 28, "Hands - Misshapen"), new RollRange<string>(29, 30, "Hands - Missing many fingers"),
                new RollRange<string>(31, 32, "Face - Badly burned"), new RollRange<string>(33, 34, "Face - Chinless"), new RollRange<string>(35, 36, "Face - Disfiguring facial scar"), new RollRange<string>(37, 38, "Face - Patchy facial hair"), new RollRange<string>(39, 40, "Face - Wispy facial hair"),
                new RollRange<string>(41, 42, "Hair - Dirty/greasy"), new RollRange<string>(43, 44, "Hair - Lank and thin"), new RollRange<string>(45, 46, "Hair - Lice-ridden"), new RollRange<string>(47, 48, "Hair - Tangled/knotted"),
                new RollRange<string>(49, 50, "Legs - Club foot"), new RollRange<string>(51, 52, "Legs - Misshapen"),
                new RollRange<string>(53, 54, "Mouth - Constantly drooling"), new RollRange<string>(55, 56, "Mouth - Discolored teeth"), new RollRange<string>(57, 58, "Mouth - Filed teeth"), new RollRange<string>(59, 60, "Mouth - Frog-like"), new RollRange<string>(61, 62, "Mouth - Huge overbite"), new RollRange<string>(63, 64, "Mouth - Huge underbite"), new RollRange<string>(65, 66, "Mouth - Large buck teeth"), new RollRange<string>(67, 68, "Mouth - Large snaggletooth"), new RollRange<string>(69, 70, "Mouth - Missing many teeth"), new RollRange<string>(71, 72, "Mouth - Toothless"),
                new RollRange<string>(73, 74, "Nose - Encrusted"), new RollRange<string>(75, 76, "Nose - Huge"), new RollRange<string>(77, 78, "Nose - Smashed"), new RollRange<string>(79, 80, "Nose - Missing"), new RollRange<string>(81, 82, "Nose - Warty"),
                new RollRange<string>(83, 84, "Skin - Covered in boils"), new RollRange<string>(85, 86, "Skin - Covered in scarification"), new RollRange<string>(87, 88, "Skin - Covered in crude tattoos"), new RollRange<string>(89, 90, "Skin - Heavily scarred"), new RollRange<string>(91, 92, "Skin - Filthy"), new RollRange<string>(93, 94, "Skin - Peeling"), new RollRange<string>(95, 96, "Skin - Scabrous"), new RollRange<string>(97, 98, "Skin - Pock marked"), new RollRange<string>(99, 100, "Skin - Warty")
            }, 100);
        }

        private string RollPhysicalFeatureAverage()
        {
            return RollAppearanceTable(new List<RollRange<string>>
            {
                new RollRange<string>(1, 2, "Build - Barrel-chested"), new RollRange<string>(3, 4, "Build - Chubby"), new RollRange<string>(5, 6, "Build - Skinny"), new RollRange<string>(7, 8, "Build - Stocky"), new RollRange<string>(9, 10, "Build - Tiny"),
                new RollRange<string>(11, 12, "Ears - Large"), new RollRange<string>(13, 14, "Ears - Small"),
                new RollRange<string>(15, 16, "Eyes - Different colors"), new RollRange<string>(17, 18, "Eyes - Large"), new RollRange<string>(19, 20, "Eyes - Narrow"), new RollRange<string>(21, 22, "Eyes - One eye (eye patch)"), new RollRange<string>(23, 24, "Eyes - Unusual color"),
                new RollRange<string>(25, 26, "Face - Heavy frown/laugh lines"), new RollRange<string>(27, 28, "Face - Obvious birthmark"), new RollRange<string>(29, 30, "Face - Obvious mole"), new RollRange<string>(31, 32, "Face - Piercing"), new RollRange<string>(33, 34, "Face - Tattooed"),
                new RollRange<string>(35, 36, "Hair - Mallen streak"), new RollRange<string>(37, 38, "Hair - Prematurely greying"), new RollRange<string>(39, 40, "Hair - Receding/thin"),
                new RollRange<string>(41, 42, "Hands - Callused"), new RollRange<string>(43, 44, "Hands - Long nails"), new RollRange<string>(45, 46, "Hands - Missing finger"), new RollRange<string>(47, 48, "Hands - Missing hand (capped)"), new RollRange<string>(49, 50, "Hands - Missing hand (carved prosthetic)"), new RollRange<string>(51, 52, "Hands - Missing hand (hook)"), new RollRange<string>(53, 54, "Hands - Tattooed knuckles"),
                new RollRange<string>(55, 56, "Legs - Peg leg"), new RollRange<string>(57, 58, "Legs - Skinny"), new RollRange<string>(59, 60, "Legs - Short"),
                new RollRange<string>(61, 62, "Mouth - Deviated septum"), new RollRange<string>(63, 64, "Mouth - Diastema"), new RollRange<string>(65, 66, "Mouth - Lip piercing"), new RollRange<string>(67, 68, "Mouth - Missing tooth"), new RollRange<string>(69, 70, "Mouth - Replacement tooth"), new RollRange<string>(71, 72, "Mouth - Thin lips"),
                new RollRange<string>(73, 74, "Nose - Aquiline"), new RollRange<string>(75, 76, "Nose - Broken"), new RollRange<string>(77, 78, "Nose - Large"), new RollRange<string>(79, 80, "Nose - Pierced"), new RollRange<string>(81, 82, "Nose - Small"), new RollRange<string>(83, 84, "Nose - Upturned"),
                new RollRange<string>(85, 86, "Skin - Deeply tanned"), new RollRange<string>(87, 88, "Skin - Freckled"), new RollRange<string>(89, 90, "Skin - Hirsute"), new RollRange<string>(91, 92, "Skin - Minor scars"), new RollRange<string>(93, 94, "Skin - Ruddy"), new RollRange<string>(95, 96, "Skin - Tattooed"), new RollRange<string>(97, 98, "Skin - Unusually pale"), new RollRange<string>(99, 100, "Skin - Weather-beaten")
            }, 100);
        }

        private string RollPhysicalFeaturePositive()
        {
            while (true)
            {
                string result = RollAppearanceTable(new List<RollRange<string>>
                {
                    new RollRange<string>(1, 3, "Build - Athletic"), new RollRange<string>(4, 6, "Build - Broad-chested/bosomy"), new RollRange<string>(7, 9, "Build - Good posture"), new RollRange<string>(10, 12, "Build - Slim"), new RollRange<string>(13, 15, "Build - Well-proportioned"),
                    new RollRange<string>(16, 18, "Eyes - Clear"), new RollRange<string>(19, 21, "Eyes - Commanding gaze"), new RollRange<string>(22, 24, "Eyes - Mesmerizing gaze"), new RollRange<string>(25, 27, "Eyes - Piercing gaze"), new RollRange<string>(28, 30, "Eyes - Striking color"),
                    new RollRange<string>(31, 33, "Hands - Graceful"), new RollRange<string>(34, 36, "Hands - Strong"),
                    new RollRange<string>(37, 39, "Face - Beauty spot"), new RollRange<string>(40, 42, "Face - Dashing facial scar"), new RollRange<string>(43, 45, "Face - Distinguished features"), new RollRange<string>(46, 48, "Face - Chiseled/fine features"), new RollRange<string>(49, 51, "Face - Heroic/graceful jawline"), new RollRange<string>(52, 54, "Face - Honest"), new RollRange<string>(55, 57, "Face - Striking/beautifying tattoo"), new RollRange<string>(58, 60, "Face - Youthful countenance"),
                    new RollRange<string>(61, 63, "Hair - Glossy"), new RollRange<string>(64, 66, "Hair - Lustrous"), new RollRange<string>(67, 69, "Hair - Luxurious/silken"),
                    new RollRange<string>(70, 72, "Legs - Long"), new RollRange<string>(73, 75, "Legs - Muscular/well-toned"), new RollRange<string>(76, 78, "Legs - Slim"),
                    new RollRange<string>(79, 81, "Mouth - Charming/winning smile"), new RollRange<string>(82, 84, "Mouth - Dazzling smile"), new RollRange<string>(85, 87, "Mouth - Full/sensuous lips"), new RollRange<string>(88, 90, "Mouth - Perfect teeth"),
                    new RollRange<string>(91, 93, "Skin - Flawless"), new RollRange<string>(94, 96, "Skin - Glossy"), new RollRange<string>(97, 99, "Skin - Healthy complexion"), new RollRange<string>(100, 100, "Roll twice")
                }, 100);
                if (result != "Roll twice") return result;
                return RollPhysicalFeaturePositive() + "; " + RollPhysicalFeaturePositive();
            }
        }

        private string RollAppearanceTable(List<RollRange<string>> table, int die = 20)
        {
            return RollOnTable(table, die);
        }

        private string RollCombinedAppearanceTables(List<List<RollRange<string>>> tables)
        {
            return RollAppearanceTable(tables[characterRandom.Next(tables.Count)]);
        }

        private List<RollRange<string>> EqualD20(params string[] values)
        {
            List<RollRange<string>> table = new List<RollRange<string>>();
            int start = 1;
            for (int i = 0; i < values.Length; i++)
            {
                int end = (int)Math.Round((i + 1) * 20.0 / values.Length);
                table.Add(new RollRange<string>(start, end, values[i]));
                start = end + 1;
            }
            return table;
        }

        private List<List<RollRange<string>>> CreateHumanEyeTables()
        {
            return new List<List<RollRange<string>>>
            {
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Black"), new RollRange<string>(7, 9, "Brown, Dark"), new RollRange<string>(10, 11, "Brown, Medium"), new RollRange<string>(12, 13, "Brown, Light"), new RollRange<string>(14, 14, "Green, Medium"), new RollRange<string>(15, 17, "Hazel, Dark"), new RollRange<string>(18, 20, "Hazel, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 2, "Blue-Grey, Medium"), new RollRange<string>(3, 4, "Blue-Grey, Light"), new RollRange<string>(5, 6, "Brown, Medium"), new RollRange<string>(7, 8, "Brown, Light"), new RollRange<string>(9, 10, "Grey, Dark"), new RollRange<string>(11, 12, "Grey, Medium"), new RollRange<string>(13, 14, "Grey-Brown, Dark"), new RollRange<string>(15, 16, "Grey-Brown, Medium"), new RollRange<string>(17, 17, "Green-Grey, Medium"), new RollRange<string>(18, 18, "Green-Grey, Light"), new RollRange<string>(19, 19, "Hazel, Medium"), new RollRange<string>(20, 20, "Hazel, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 2, "Blue, Medium"), new RollRange<string>(3, 5, "Blue, Light"), new RollRange<string>(6, 7, "Blue-Grey, Medium"), new RollRange<string>(8, 9, "Blue-Grey, Light"), new RollRange<string>(10, 11, "Blue-Green, Medium"), new RollRange<string>(12, 13, "Blue-Green, Light"), new RollRange<string>(14, 14, "Grey, Medium"), new RollRange<string>(15, 15, "Grey, Light"), new RollRange<string>(16, 16, "Grey, Very Light"), new RollRange<string>(17, 17, "Green, Medium"), new RollRange<string>(18, 19, "Green, Light"), new RollRange<string>(20, 20, "Violet, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Black"), new RollRange<string>(6, 10, "Brown, Dark"), new RollRange<string>(11, 15, "Brown, Medium"), new RollRange<string>(16, 20, "Brown, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 4, "Brown, Dark"), new RollRange<string>(5, 8, "Brown, Medium"), new RollRange<string>(9, 12, "Brown, Light"), new RollRange<string>(13, 16, "Hazel, Dark"), new RollRange<string>(17, 20, "Hazel, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 20, "Black") },
                new List<RollRange<string>> { new RollRange<string>(1, 4, "Brown, Dark"), new RollRange<string>(5, 8, "Brown, Medium"), new RollRange<string>(9, 10, "Brown, Light"), new RollRange<string>(11, 12, "Green, Dark"), new RollRange<string>(13, 14, "Green, Medium"), new RollRange<string>(15, 17, "Hazel, Dark"), new RollRange<string>(18, 20, "Hazel, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 1, "Blue, Medium"), new RollRange<string>(2, 4, "Blue, Light"), new RollRange<string>(5, 5, "Blue-Grey, Medium"), new RollRange<string>(6, 7, "Blue-Grey, Light"), new RollRange<string>(8, 9, "Brown, Light"), new RollRange<string>(10, 10, "Green, Medium"), new RollRange<string>(11, 13, "Green, Light"), new RollRange<string>(14, 14, "Green-Grey, Medium"), new RollRange<string>(15, 16, "Green-Grey, Light"), new RollRange<string>(17, 17, "Hazel, Medium"), new RollRange<string>(18, 20, "Hazel, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Black"), new RollRange<string>(11, 20, "Brown, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 9, "Black"), new RollRange<string>(10, 10, "Blue, Light"), new RollRange<string>(11, 13, "Brown, Dark"), new RollRange<string>(14, 16, "Brown, Medium"), new RollRange<string>(17, 19, "Brown, Light"), new RollRange<string>(20, 20, "Grey, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 8, "Black"), new RollRange<string>(9, 10, "Brown, Dark"), new RollRange<string>(11, 13, "Brown, Medium"), new RollRange<string>(14, 16, "Brown, Light"), new RollRange<string>(17, 18, "Hazel, Dark"), new RollRange<string>(19, 20, "Hazel, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 3, "Blue-Grey, Medium"), new RollRange<string>(4, 6, "Brown, Dark"), new RollRange<string>(7, 9, "Brown, Medium"), new RollRange<string>(10, 11, "Brown, Light"), new RollRange<string>(12, 13, "Grey, Dark"), new RollRange<string>(14, 15, "Grey, Medium"), new RollRange<string>(16, 17, "Grey-Brown, Dark"), new RollRange<string>(18, 20, "Grey-Brown, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 3, "Amber, Dark"), new RollRange<string>(4, 6, "Amber, Medium"), new RollRange<string>(7, 10, "Brown, Dark"), new RollRange<string>(11, 13, "Grey-Brown, Dark"), new RollRange<string>(14, 16, "Grey-Brown, Medium"), new RollRange<string>(17, 18, "Green-Brown, Dark"), new RollRange<string>(19, 20, "Green-Brown, Medium") }
            };
        }

        private List<List<RollRange<string>>> CreateHumanHairColorTables()
        {
            return new List<List<RollRange<string>>>
            {
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Auburn, Dark"), new RollRange<string>(6, 10, "Auburn, Medium"), new RollRange<string>(11, 15, "Brown, Rufous"), new RollRange<string>(16, 20, "Brown-Black") },
                new List<RollRange<string>> { new RollRange<string>(1, 3, "Auburn, Dark"), new RollRange<string>(4, 6, "Auburn, Medium"), new RollRange<string>(7, 10, "Black"), new RollRange<string>(11, 12, "Blonde, Dark"), new RollRange<string>(13, 14, "Brown, Dark"), new RollRange<string>(15, 16, "Brown, Golden"), new RollRange<string>(17, 18, "Brown, Rufous"), new RollRange<string>(19, 20, "Red, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Blonde, Dark"), new RollRange<string>(6, 10, "Blonde, Golden"), new RollRange<string>(11, 12, "Blonde, Platinum"), new RollRange<string>(13, 16, "Brown, Golden"), new RollRange<string>(17, 20, "Brown, Rufous") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Black"), new RollRange<string>(11, 20, "Brown, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 20, "Black") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Black"), new RollRange<string>(6, 10, "Brown, Ash"), new RollRange<string>(11, 15, "Brown, Dark"), new RollRange<string>(16, 20, "Blonde, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 9, "Black"), new RollRange<string>(10, 10, "Brown, Ash"), new RollRange<string>(11, 19, "Brown, Dark"), new RollRange<string>(20, 20, "Blonde, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 2, "Auburn, Dark"), new RollRange<string>(3, 5, "Auburn, Medium"), new RollRange<string>(6, 8, "Blonde, Golden"), new RollRange<string>(9, 11, "Blonde, Strawberry"), new RollRange<string>(12, 14, "Brown, Golden"), new RollRange<string>(15, 16, "Brown, Rufous"), new RollRange<string>(17, 18, "Red, Dark"), new RollRange<string>(19, 20, "Red, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 8, "Black"), new RollRange<string>(9, 12, "Blue-Black"), new RollRange<string>(13, 20, "Brown, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 4, "Auburn, Dark"), new RollRange<string>(5, 8, "Black"), new RollRange<string>(9, 12, "Brown, Dark"), new RollRange<string>(13, 16, "Brown, Rufous"), new RollRange<string>(17, 20, "Brown-Black") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Black"), new RollRange<string>(11, 20, "Blue-Black") }
            };
        }

        private List<List<RollRange<string>>> CreateHumanHairTextureTables()
        {
            return new List<List<RollRange<string>>>
            {
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Straight"), new RollRange<string>(11, 20, "Wavy") },
                new List<RollRange<string>> { new RollRange<string>(1, 16, "Straight"), new RollRange<string>(17, 20, "Wavy") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Curly"), new RollRange<string>(11, 20, "Wavy") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Curly"), new RollRange<string>(6, 12, "Straight"), new RollRange<string>(13, 20, "Wavy") },
                new List<RollRange<string>> { new RollRange<string>(1, 20, "Tightly curled/kinky") },
                new List<RollRange<string>> { new RollRange<string>(1, 20, "Straight") }
            };
        }

        private List<List<RollRange<string>>> CreateHumanSkinTables()
        {
            return new List<List<RollRange<string>>>
            {
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Brown, Light"), new RollRange<string>(11, 20, "Olive, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Brown, Very Light"), new RollRange<string>(7, 20, "Brown, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 20, "Pale") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Brown, Dark"), new RollRange<string>(11, 20, "Brown, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 10, "Brown, Medium"), new RollRange<string>(11, 20, "Brown, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 7, "Brown, Dark"), new RollRange<string>(8, 14, "Brown, Medium"), new RollRange<string>(15, 20, "Brown-Black") },
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Brown, Very Light"), new RollRange<string>(7, 8, "Brown, Light"), new RollRange<string>(9, 15, "Pale, Freckled"), new RollRange<string>(16, 20, "Pale") },
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Brown, Dark"), new RollRange<string>(7, 13, "Brown, Medium"), new RollRange<string>(14, 20, "Olive, Dark") },
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Ocher, Dark"), new RollRange<string>(7, 13, "Ocher, Medium"), new RollRange<string>(14, 20, "Ocher, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Ocher, Dark"), new RollRange<string>(6, 10, "Ocher, Medium"), new RollRange<string>(11, 15, "Olive, Dark"), new RollRange<string>(16, 20, "Olive, Reddish") },
                new List<RollRange<string>> { new RollRange<string>(1, 4, "Brown, Dark"), new RollRange<string>(5, 8, "Brown, Medium"), new RollRange<string>(9, 10, "Ocher, Dark"), new RollRange<string>(11, 12, "Ocher, Medium"), new RollRange<string>(13, 16, "Olive, Dark"), new RollRange<string>(17, 20, "Olive, Medium") },
                new List<RollRange<string>> { new RollRange<string>(1, 5, "Brown, Very Light"), new RollRange<string>(6, 20, "Brown, Light") },
                new List<RollRange<string>> { new RollRange<string>(1, 6, "Copper"), new RollRange<string>(7, 13, "Olive, Sienna"), new RollRange<string>(14, 20, "Reddish Brown") }
            };
        }

        private int RollNpcAge(string occupation)
        {
            if (occupation.Contains("Ship Captain")) return 17 + RollDice(3, 6);
            if (occupation.Contains("Mage") || occupation.Contains("Magician") || occupation.Contains("Infantry")) return 17 + RollDice(2, 6);
            if (occupation.Contains("Healer") || occupation.Contains("Sage") || occupation.Contains("Bookseller")) return 21 + RollDice(1, 6);
            return 17 + RollDice(1, 6);
        }

        private int RollDropLowest(int dice, int drop)
        {
            List<int> rolls = new List<int>();
            for (int i = 0; i < dice; i++) rolls.Add(characterRandom.Next(1, 7));
            return rolls.OrderByDescending(r => r).Take(dice - drop).Sum();
        }

        private int RollDice(int count, int sides)
        {
            int total = 0;
            for (int i = 0; i < count; i++) total += characterRandom.Next(1, sides + 1);
            return total;
        }

        private int AttributeBonus(int score)
        {
            return CharacterRulesService.AttributeBonus(score);
        }
    }
}
