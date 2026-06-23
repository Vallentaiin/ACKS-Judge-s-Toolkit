using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OSRCGG
{
    public class DomainFinancialSummary
    {
        public double AverageLandValue { get; set; }
        public int RequiredStrongholdValue { get; set; }
        public double PeasantRevenue { get; set; }
        public double UrbanIncome { get; set; }
        public double Expenses { get; set; }
        public double NetIncome { get; set; }
    }

    public class DomainMoraleSummary
    {
        public int BaseMorale { get; set; }
        public int PersonalAuthority { get; set; }
        public double NetIncome { get; set; }
        public List<string> Lines { get; private set; }

        public DomainMoraleSummary()
        {
            Lines = new List<string>();
        }
    }

    public static class AcksDomainRules
    {
        private static readonly int[] IncomeBandCeilings =
        {
            25, 75, 150, 300, 600, 1200, 2400, 5000, 10000,
            20000, 45000, 75000, 150000, 425000
        };

        public static int AttributeModifier(int score)
        {
            if (score <= 3) return -3;
            if (score <= 5) return -2;
            if (score <= 8) return -1;
            if (score <= 12) return 0;
            if (score <= 15) return 1;
            if (score <= 17) return 2;
            return 3;
        }

        public static int RequiredStrongholdValue(DomainRecord domain)
        {
            if (domain == null) return 0;
            int hexes = Math.Max(1, domain.Hexes == null ? 0 : domain.Hexes.Count);
            return StrongholdValuePerSixMileHex(domain.Classification) * hexes;
        }

        public static int StrongholdValuePerSixMileHex(string classification)
        {
            if (string.Equals(classification, "Civilized", StringComparison.OrdinalIgnoreCase)) return 15000;
            if (string.Equals(classification, "Borderlands", StringComparison.OrdinalIgnoreCase)) return 22500;
            return 30000;
        }

        public static int MaxFamiliesPerHex(DomainRecord domain)
        {
            if (domain == null) return 0;
            if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)) return 125;
            if (string.Equals(domain.Classification, "Civilized", StringComparison.OrdinalIgnoreCase)) return 780;
            if (string.Equals(domain.Classification, "Borderlands", StringComparison.OrdinalIgnoreCase)) return 375;
            return 185;
        }

        public static double AverageLandValue(DomainRecord domain)
        {
            if (domain == null) return 6;

            if (string.Equals(domain.LandValueMode, "PerHex", StringComparison.OrdinalIgnoreCase)
                && domain.Hexes != null
                && domain.Hexes.Count > 0)
            {
                return domain.Hexes.Average(h => ClampLandValue(h.LandValueGp));
            }

            if (string.Equals(domain.LandValueMode, "DomainWide", StringComparison.OrdinalIgnoreCase)
                || string.Equals(domain.LandValueMode, "Manual", StringComparison.OrdinalIgnoreCase))
            {
                return ClampLandValue(domain.FixedLandValueGp);
            }

            return 6;
        }

        public static int ClampLandValue(int value)
        {
            return Math.Max(3, Math.Min(9, value));
        }

        public static DomainFinancialSummary CalculateFinancials(DomainRecord domain)
        {
            DomainFinancialSummary summary = new DomainFinancialSummary();
            if (domain == null) return summary;

            int peasantFamilies = Math.Max(0, domain.PeasantFamilies);
            int urbanFamilies = Math.Max(0, domain.UrbanFamilies);
            summary.AverageLandValue = AverageLandValue(domain);
            summary.RequiredStrongholdValue = RequiredStrongholdValue(domain);

            double peasantRevenuePerFamily = summary.AverageLandValue + 4 + Math.Max(0, domain.TaxGpPerFamily);
            summary.PeasantRevenue = peasantFamilies * peasantRevenuePerFamily;
            summary.UrbanIncome = urbanFamilies * UrbanNetIncomePerFamily(urbanFamilies);

            int garrison = Math.Max(0, domain.GarrisonGpPerFamily);
            int liturgies = Math.Max(0, domain.LiturgiesGpPerFamily);
            int tithes = Math.Max(0, domain.TithesGpPerFamily);
            int maintenance = Math.Max(0, domain.MaintenanceGpPerFamily);
            summary.Expenses = peasantFamilies * (garrison + liturgies + tithes + maintenance);
            summary.NetIncome = summary.PeasantRevenue + summary.UrbanIncome - summary.Expenses;
            return summary;
        }

        public static double UrbanNetIncomePerFamily(int urbanFamilies)
        {
            if (urbanFamilies < 75) return 0;
            if (urbanFamilies < 250) return 2.0;
            if (urbanFamilies < 5000) return 2.5;
            if (urbanFamilies < 20000) return 3.0;
            return 3.5;
        }

        public static int PersonalAuthority(int rulerLevel, double netMonthlyIncome)
        {
            int band = IncomeBand(netMonthlyIncome);
            return ClampMorale(rulerLevel - band);
        }

        public static int IncomeBand(double netMonthlyIncome)
        {
            double income = Math.Max(0, netMonthlyIncome);
            for (int i = 0; i < IncomeBandCeilings.Length; i++)
            {
                if (income <= IncomeBandCeilings[i]) return i + 1;
            }

            return IncomeBandCeilings.Length + 1;
        }

        public static DomainMoraleSummary CalculateMorale(DomainRecord domain)
        {
            DomainMoraleSummary morale = new DomainMoraleSummary();
            if (domain == null) return morale;

            DomainFinancialSummary financials = CalculateFinancials(domain);
            morale.NetIncome = financials.NetIncome;

            bool hasRuler = domain.Ruler != null
                && !string.IsNullOrWhiteSpace(domain.Ruler.SourceMode)
                && !string.Equals(domain.Ruler.SourceMode, "None", StringComparison.OrdinalIgnoreCase)
                && domain.Ruler.Snapshot != null
                && !string.IsNullOrWhiteSpace(domain.Ruler.Snapshot.Name);
            CharacterRecord ruler = hasRuler ? domain.Ruler.Snapshot : null;
            int level = ruler == null ? 0 : Math.Max(0, ruler.Level);
            int cha = ruler == null ? 9 : ruler.CHA;
            int chaMod = hasRuler ? AttributeModifier(cha) : 0;
            int personalAuthority = hasRuler ? PersonalAuthority(level, financials.NetIncome) : 0;
            int leadership = hasRuler && domain.Ruler.HasLeadership() ? 1 : 0;
            int stronghold = StrongholdModifier(domain.StrongholdValueGp, financials.RequiredStrongholdValue);
            int classification = ClassificationModifier(domain.Classification);
            int extraTroops = ExtraTroopModifier(domain.Classification, domain.GarrisonGpPerFamily);
            int alignment = hasRuler ? AlignmentModifier(domain.DomainAlignment, ruler == null ? "" : ruler.Alignment) : 0;

            morale.PersonalAuthority = personalAuthority;
            morale.Lines.Add("Personal authority: " + Signed(personalAuthority));
            morale.Lines.Add("CHA " + cha + ": " + Signed(chaMod));
            morale.Lines.Add("Leadership: " + Signed(leadership));
            morale.Lines.Add("Stronghold: " + Signed(stronghold));
            morale.Lines.Add("Classification: " + Signed(classification));
            morale.Lines.Add("Extra garrison: " + Signed(extraTroops));
            morale.Lines.Add("Alignment/religion: " + Signed(alignment));

            morale.BaseMorale = ClampMorale(personalAuthority + chaMod + leadership + stronghold + classification + extraTroops + alignment);
            return morale;
        }

        public static int StrongholdModifier(int actualValue, int requiredValue)
        {
            if (requiredValue <= 0 || actualValue >= requiredValue) return 0;
            if (actualValue >= requiredValue / 2.0) return -1;
            if (actualValue >= requiredValue / 4.0) return -2;
            return -3;
        }

        public static int ClassificationModifier(string classification)
        {
            if (string.Equals(classification, "Borderlands", StringComparison.OrdinalIgnoreCase)) return -1;
            if (string.Equals(classification, "Outlands", StringComparison.OrdinalIgnoreCase)) return -2;
            return 0;
        }

        public static int ExtraTroopModifier(string classification, int garrisonGpPerFamily)
        {
            int extra = Math.Max(0, garrisonGpPerFamily - 2);
            if (string.Equals(classification, "Borderlands", StringComparison.OrdinalIgnoreCase)) return Math.Min(1, extra);
            if (string.Equals(classification, "Outlands", StringComparison.OrdinalIgnoreCase)) return Math.Min(2, extra);
            return 0;
        }

        public static int AlignmentModifier(string domainAlignment, string rulerAlignment)
        {
            string domain = NormalizeAlignment(domainAlignment);
            string ruler = NormalizeAlignment(rulerAlignment);
            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(ruler)) return 0;
            if (domain == ruler) return 0;
            if (domain == "Neutral" || ruler == "Neutral") return -1;
            return -2;
        }

        public static int ClampMorale(int value)
        {
            return Math.Max(-4, Math.Min(4, value));
        }

        public static string Signed(int value)
        {
            return value >= 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
        }

        public static string NormalizeAlignment(string alignment)
        {
            if (string.IsNullOrWhiteSpace(alignment)) return "";
            if (alignment.IndexOf("Law", StringComparison.OrdinalIgnoreCase) >= 0) return "Lawful";
            if (alignment.IndexOf("Chaos", StringComparison.OrdinalIgnoreCase) >= 0) return "Chaotic";
            if (alignment.IndexOf("Chaotic", StringComparison.OrdinalIgnoreCase) >= 0) return "Chaotic";
            if (alignment.IndexOf("Neutral", StringComparison.OrdinalIgnoreCase) >= 0) return "Neutral";
            return alignment.Trim();
        }

        public static string BuildSummary(DomainRecord domain)
        {
            if (domain == null) return "";

            DomainFinancialSummary financials = CalculateFinancials(domain);
            DomainMoraleSummary morale = CalculateMorale(domain);
            int hexes = domain.Hexes == null ? 0 : domain.Hexes.Count;
            int maxFamilies = MaxFamiliesPerHex(domain) * Math.Max(1, hexes);

            List<string> lines = new List<string>();
            lines.Add(domain.DisplayName);
            lines.Add("Type: " + domain.DomainType + ", race: " + (string.IsNullOrWhiteSpace(domain.Race) ? "Human" : domain.Race));
            lines.Add("Hexes: " + hexes + ", families: " + domain.PeasantFamilies + "/" + maxFamilies);
            lines.Add("Ruler: " + (domain.Ruler == null ? "(none)" : domain.Ruler.DisplayName));
            lines.Add("Avg land value: " + financials.AverageLandValue.ToString("0.##", CultureInfo.InvariantCulture) + " gp");
            lines.Add("Stronghold: " + domain.StrongholdValueGp + " / " + financials.RequiredStrongholdValue + " gp");
            string strongholdSite = domain.StrongholdQ >= 0 && domain.StrongholdR >= 0
                ? "Q " + domain.StrongholdQ.ToString(CultureInfo.InvariantCulture) + ", R " + domain.StrongholdR.ToString(CultureInfo.InvariantCulture)
                : "(not placed)";
            lines.Add("Stronghold site: " + strongholdSite + (domain.StrongholdInSettlement ? ", in settlement" : ", separate")
                + (domain.StrongholdActsAsMarketClassVI ? ", counts as Class VI base" : ""));
            lines.Add("Net income: " + financials.NetIncome.ToString("0.##", CultureInfo.InvariantCulture) + " gp/month");
            lines.Add("Base morale: " + AcksDomainRules.Signed(morale.BaseMorale) + ", current: " + AcksDomainRules.Signed(domain.CurrentMorale));
            lines.AddRange(morale.Lines);
            return string.Join(Environment.NewLine, lines);
        }
    }
}
