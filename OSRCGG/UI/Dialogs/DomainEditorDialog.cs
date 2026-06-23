using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public class DomainEditorDialog : Form
    {
        private readonly bool isEnglish;
        private readonly List<CharacterRecord> characters;
        private readonly DomainRecord originalDomain;

        private TextBox txtName;
        private ComboBox cmbType;
        private ComboBox cmbRace;
        private ComboBox cmbClassification;
        private ComboBox cmbAlignment;
        private ComboBox cmbLandValueMode;
        private NumericUpDown nudFixedLandValue;
        private NumericUpDown nudPeasantFamilies;
        private NumericUpDown nudUrbanFamilies;
        private NumericUpDown nudStrongholdValue;
        private NumericUpDown nudGarrison;
        private NumericUpDown nudTax;
        private NumericUpDown nudLiturgies;
        private NumericUpDown nudTithes;
        private NumericUpDown nudMaintenance;
        private NumericUpDown nudCurrentMorale;
        private ComboBox cmbRulerSource;
        private ComboBox cmbRulerLibrary;
        private TextBox txtRulerName;
        private ComboBox cmbRulerClass;
        private NumericUpDown nudRulerLevel;
        private NumericUpDown nudRulerCha;
        private ComboBox cmbRulerAlignment;
        private CheckBox chkRulerLeadership;
        private CheckBox chkSaveRuler;
        private TextBox txtNotes;
        private TextBox txtSummary;

        public DomainRecord Domain { get; private set; }

        public DomainEditorDialog(bool isEnglish, DomainRecord domain, IEnumerable<CharacterRecord> characterLibrary)
        {
            this.isEnglish = isEnglish;
            characters = characterLibrary == null
                ? new List<CharacterRecord>()
                : characterLibrary.OrderBy(c => c.Name).ThenBy(c => c.ClassName).ToList();
            originalDomain = CloneDomain(domain ?? new DomainRecord());
            Domain = CloneDomain(originalDomain);

            Text = isEnglish ? "Domain" : "\u0414\u043e\u043c\u0435\u043d";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(680, 640);
            Size = new Size(760, 720);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildUi();
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
            LoadFromDomain(Domain);
            UpdateRulerControls();
            UpdateSummary();
        }

        private void BuildUi()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.BackColor = Color.FromArgb(42, 42, 42);
            Controls.Add(panel);

            int left = 16;
            int mid = 360;
            int y = 14;
            int labelWidth = 150;
            int inputWidth = 190;

            AddSection(panel, isEnglish ? "Domain" : "\u0414\u043e\u043c\u0435\u043d", left, y);
            y += 28;
            txtName = AddText(panel, left, y, labelWidth, inputWidth, isEnglish ? "Name" : "\u041d\u0430\u0437\u0432\u0430\u043d\u0438\u0435");
            cmbType = AddCombo(panel, mid, y, labelWidth, inputWidth, isEnglish ? "Type" : "\u0422\u0438\u043f", new[] { "Ordinary", "Clanhold", "Transitional", "Dwarven Vault", "Elven Fastness", "Chaotic" });
            y += 34;
            cmbClassification = AddCombo(panel, left, y, labelWidth, inputWidth, isEnglish ? "Classification" : "\u0417\u0435\u043c\u043b\u0438", new[] { "Civilized", "Borderlands", "Outlands" });
            cmbAlignment = AddCombo(panel, mid, y, labelWidth, inputWidth, isEnglish ? "Alignment" : "\u041c\u0438\u0440\u043e\u0432\u043e\u0437\u0437\u0440\u0435\u043d\u0438\u0435", new[] { "Lawful", "Neutral", "Chaotic" });
            y += 34;
            cmbRace = AddCombo(panel, left, y, labelWidth, inputWidth, isEnglish ? "Race" : "\u0420\u0430\u0441\u0430", new[] { "Human", "Dwarf", "Elf", "Orc", "Beastman" });
            y += 34;
            cmbLandValueMode = AddCombo(panel, left, y, labelWidth, inputWidth, isEnglish ? "Land value mode" : "Land value", new[] { "Fixed6", "DomainWide", "PerHex", "Manual" });
            nudFixedLandValue = AddNumber(panel, mid, y, labelWidth, 90, isEnglish ? "Land value" : "Land value", 3, 9, 6);
            y += 42;

            AddSection(panel, isEnglish ? "Families and money" : "\u0421\u0435\u043c\u044c\u0438 \u0438 \u0434\u0435\u043d\u044c\u0433\u0438", left, y);
            y += 28;
            nudPeasantFamilies = AddNumber(panel, left, y, labelWidth, 110, isEnglish ? "Peasant families" : "\u0421\u0435\u043b\u044c\u0441\u043a\u0438\u0435 \u0441\u0435\u043c\u044c\u0438", 0, 10000000, 0);
            nudUrbanFamilies = AddNumber(panel, mid, y, labelWidth, 110, isEnglish ? "Urban families" : "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0435\u043c\u044c\u0438", 0, 10000000, 0);
            y += 34;
            nudStrongholdValue = AddNumber(panel, left, y, labelWidth, 130, isEnglish ? "Stronghold gp" : "\u041a\u0440\u0435\u043f\u043e\u0441\u0442\u044c gp", 0, 200000000, 0);
            nudGarrison = AddNumber(panel, mid, y, labelWidth, 90, isEnglish ? "Garrison/family" : "\u0413\u0430\u0440\u043d\u0438\u0437\u043e\u043d/\u0441\u0435\u043c\u044c\u044e", 0, 20, 2);
            y += 34;
            nudTax = AddNumber(panel, left, y, labelWidth, 90, isEnglish ? "Tax/family" : "\u041d\u0430\u043b\u043e\u0433/\u0441\u0435\u043c\u044c\u044e", 0, 20, 2);
            nudLiturgies = AddNumber(panel, mid, y, labelWidth, 90, isEnglish ? "Liturgies/family" : "Liturgies", 0, 20, 1);
            y += 34;
            nudTithes = AddNumber(panel, left, y, labelWidth, 90, isEnglish ? "Tithes/family" : "Tithes", 0, 20, 1);
            nudMaintenance = AddNumber(panel, mid, y, labelWidth, 90, isEnglish ? "Maintenance/family" : "\u0421\u043e\u0434\u0435\u0440\u0436\u0430\u043d\u0438\u0435", 0, 20, 1);
            y += 34;
            nudCurrentMorale = AddNumber(panel, left, y, labelWidth, 90, isEnglish ? "Current morale" : "\u0422\u0435\u043a\u0443\u0449\u0430\u044f \u043c\u043e\u0440\u0430\u043b\u044c", -4, 4, 0);
            y += 42;

            AddSection(panel, isEnglish ? "Ruler" : "\u041f\u0440\u0430\u0432\u0438\u0442\u0435\u043b\u044c", left, y);
            y += 28;
            cmbRulerSource = AddCombo(panel, left, y, labelWidth, inputWidth, isEnglish ? "Source" : "\u0418\u0441\u0442\u043e\u0447\u043d\u0438\u043a", new[] { "None", "Library", "Manual", "Generated" });
            cmbRulerLibrary = AddCombo(panel, mid, y, labelWidth, inputWidth, isEnglish ? "Library PC/NPC" : "\u0418\u0437 \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0438", Array.Empty<string>());
            cmbRulerLibrary.DataSource = characters;
            y += 34;
            txtRulerName = AddText(panel, left, y, labelWidth, inputWidth, isEnglish ? "Ruler name" : "\u0418\u043c\u044f");
            cmbRulerClass = AddCombo(panel, mid, y, labelWidth, inputWidth, isEnglish ? "Class" : "\u041a\u043b\u0430\u0441\u0441", new[] { "Fighter", "Thief", "Venturer", "Mage", "Crusader", "Explorer", "Nobiran Wonderworker", "NPC" });
            y += 34;
            nudRulerLevel = AddNumber(panel, left, y, labelWidth, 90, isEnglish ? "Level" : "\u0423\u0440\u043e\u0432\u0435\u043d\u044c", 0, 14, 1);
            nudRulerCha = AddNumber(panel, mid, y, labelWidth, 90, "CHA", 3, 18, 9);
            y += 34;
            cmbRulerAlignment = AddCombo(panel, left, y, labelWidth, inputWidth, isEnglish ? "Ruler alignment" : "\u041c\u0438\u0440\u043e\u0432\u043e\u0437\u0437\u0440\u0435\u043d\u0438\u0435", new[] { "Lawful", "Neutral", "Chaotic" });
            chkRulerLeadership = AddCheck(panel, mid, y, isEnglish ? "Leadership proficiency" : "Leadership");
            y += 32;
            chkSaveRuler = AddCheck(panel, left, y, isEnglish ? "Save manual/generated ruler to character library" : "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u0440\u0430\u0432\u0438\u0442\u0435\u043b\u044f \u0432 \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0443");
            Button btnGenerate = CreateButton(isEnglish ? "Generate ruler" : "\u0421\u0433\u0435\u043d\u0435\u0440\u0438\u0440\u043e\u0432\u0430\u0442\u044c", UiTheme.PositiveButtonColor);
            btnGenerate.SetBounds(mid, y - 2, 190, 28);
            btnGenerate.Click += (s, e) => GenerateRuler();
            panel.Controls.Add(btnGenerate);
            y += 42;

            AddSection(panel, isEnglish ? "Notes and result" : "\u0417\u0430\u043c\u0435\u0442\u043a\u0438 \u0438 \u0438\u0442\u043e\u0433", left, y);
            y += 28;
            AddLabel(panel, isEnglish ? "Notes" : "\u0417\u0430\u043c\u0435\u0442\u043a\u0438", left, y, labelWidth, 20);
            txtNotes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
            txtNotes.SetBounds(left + labelWidth, y, 470, 64);
            panel.Controls.Add(txtNotes);
            y += 74;
            txtSummary = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(55, 55, 55), ForeColor = Color.White };
            txtSummary.SetBounds(left, y, 650, 120);
            panel.Controls.Add(txtSummary);
            y += 134;

            Button btnOk = CreateButton(isEnglish ? "Save" : "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c", UiTheme.PositiveButtonColor);
            Button btnCancel = CreateButton(isEnglish ? "Cancel" : "\u041e\u0442\u043c\u0435\u043d\u0430", UiTheme.NegativeButtonColor);
            btnOk.SetBounds(left, y, 130, 32);
            btnCancel.SetBounds(left + 140, y, 130, 32);
            btnOk.Click += (s, e) => SaveAndClose();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            panel.Controls.Add(btnOk);
            panel.Controls.Add(btnCancel);
            panel.AutoScrollMinSize = new Size(680, y + 52);

            EventHandler update = (s, e) => UpdateSummary();
            cmbType.SelectedIndexChanged += update;
            cmbRace.SelectedIndexChanged += update;
            cmbClassification.SelectedIndexChanged += update;
            cmbAlignment.SelectedIndexChanged += update;
            cmbLandValueMode.SelectedIndexChanged += update;
            nudFixedLandValue.ValueChanged += update;
            nudPeasantFamilies.ValueChanged += update;
            nudUrbanFamilies.ValueChanged += update;
            nudStrongholdValue.ValueChanged += update;
            nudGarrison.ValueChanged += update;
            nudTax.ValueChanged += update;
            nudLiturgies.ValueChanged += update;
            nudTithes.ValueChanged += update;
            nudMaintenance.ValueChanged += update;
            nudCurrentMorale.ValueChanged += update;
            cmbRulerSource.SelectedIndexChanged += (s, e) => { UpdateRulerControls(); UpdateSummary(); };
            cmbRulerLibrary.SelectedIndexChanged += (s, e) => { LoadSelectedLibraryRuler(); UpdateSummary(); };
            txtRulerName.TextChanged += update;
            cmbRulerClass.SelectedIndexChanged += update;
            nudRulerLevel.ValueChanged += update;
            nudRulerCha.ValueChanged += update;
            cmbRulerAlignment.SelectedIndexChanged += update;
            chkRulerLeadership.CheckedChanged += update;
        }

        private void LoadFromDomain(DomainRecord domain)
        {
            txtName.Text = domain.Name;
            SelectCombo(cmbType, domain.DomainType);
            SelectCombo(cmbRace, string.IsNullOrWhiteSpace(domain.Race) ? "Human" : domain.Race);
            SelectCombo(cmbClassification, domain.Classification);
            SelectCombo(cmbAlignment, domain.DomainAlignment);
            SelectCombo(cmbLandValueMode, domain.LandValueMode);
            nudFixedLandValue.Value = ClampDecimal(domain.FixedLandValueGp, nudFixedLandValue);
            nudPeasantFamilies.Value = ClampDecimal(domain.PeasantFamilies, nudPeasantFamilies);
            nudUrbanFamilies.Value = ClampDecimal(domain.UrbanFamilies, nudUrbanFamilies);
            nudStrongholdValue.Value = ClampDecimal(domain.StrongholdValueGp, nudStrongholdValue);
            nudGarrison.Value = ClampDecimal(domain.GarrisonGpPerFamily, nudGarrison);
            nudTax.Value = ClampDecimal(domain.TaxGpPerFamily, nudTax);
            nudLiturgies.Value = ClampDecimal(domain.LiturgiesGpPerFamily, nudLiturgies);
            nudTithes.Value = ClampDecimal(domain.TithesGpPerFamily, nudTithes);
            nudMaintenance.Value = ClampDecimal(domain.MaintenanceGpPerFamily, nudMaintenance);
            nudCurrentMorale.Value = ClampDecimal(domain.CurrentMorale, nudCurrentMorale);
            txtNotes.Text = domain.Notes;

            DomainRulerRecord ruler = domain.Ruler ?? new DomainRulerRecord();
            string rulerSource = string.IsNullOrWhiteSpace(ruler.SourceMode) ? "None" : NormalizeRulerSource(ruler.SourceMode);
            SelectCombo(cmbRulerSource, rulerSource);
            bool libraryRulerFound = false;
            if (!string.IsNullOrWhiteSpace(ruler.LibraryCharacterId))
            {
                for (int i = 0; i < cmbRulerLibrary.Items.Count; i++)
                {
                    CharacterRecord character = cmbRulerLibrary.Items[i] as CharacterRecord;
                    if (character != null && character.Id == ruler.LibraryCharacterId)
                    {
                        cmbRulerLibrary.SelectedIndex = i;
                        libraryRulerFound = true;
                        break;
                    }
                }
            }

            if (rulerSource == "Library" && !libraryRulerFound)
            {
                SelectCombo(cmbRulerSource, ruler.Snapshot != null && !string.IsNullOrWhiteSpace(ruler.Snapshot.Name) ? "Manual" : "None");
            }

            LoadRulerSnapshot(ruler.Snapshot);
            chkSaveRuler.Checked = ruler.SaveGeneratedToLibrary;
        }

        private void LoadRulerSnapshot(CharacterRecord ruler)
        {
            if (ruler == null) ruler = new CharacterRecord { Kind = "NPC", Name = "", ClassName = "Fighter", Level = 1, CHA = 9, Alignment = "Neutral" };
            txtRulerName.Text = ruler.Name;
            SelectCombo(cmbRulerClass, string.IsNullOrWhiteSpace(ruler.ClassName) ? "NPC" : ruler.ClassName);
            nudRulerLevel.Value = ClampDecimal(ruler.Level, nudRulerLevel);
            nudRulerCha.Value = ClampDecimal(ruler.CHA <= 0 ? 9 : ruler.CHA, nudRulerCha);
            SelectCombo(cmbRulerAlignment, string.IsNullOrWhiteSpace(ruler.Alignment) ? "Neutral" : AcksDomainRules.NormalizeAlignment(ruler.Alignment));
            chkRulerLeadership.Checked = !string.IsNullOrWhiteSpace(ruler.Proficiencies)
                && ruler.Proficiencies.IndexOf("Leadership", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SaveAndClose()
        {
            ApplyToDomain(Domain);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyToDomain(DomainRecord domain)
        {
            domain.Name = string.IsNullOrWhiteSpace(txtName.Text) ? (isEnglish ? "Unnamed domain" : "\u0411\u0435\u0437\u044b\u043c\u044f\u043d\u043d\u044b\u0439 \u0434\u043e\u043c\u0435\u043d") : txtName.Text.Trim();
            domain.DomainType = ComboText(cmbType, "Ordinary");
            domain.Race = ComboText(cmbRace, "Human");
            domain.Classification = ComboText(cmbClassification, "Outlands");
            domain.DomainAlignment = ComboText(cmbAlignment, "Neutral");
            if (domain.DomainType == "Dwarven Vault") domain.Race = "Dwarf";
            if (domain.DomainType == "Elven Fastness") domain.Race = "Elf";
            if (domain.DomainType == "Clanhold")
            {
                if (domain.Race == "Dwarf" || domain.Race == "Elf") domain.Race = "Human";
                domain.Classification = "Outlands";
                if (domain.Race == "Orc" || domain.Race == "Beastman") domain.DomainAlignment = "Chaotic";
            }
            domain.LandValueMode = ComboText(cmbLandValueMode, "Fixed6");
            domain.FixedLandValueGp = (int)nudFixedLandValue.Value;
            domain.PeasantFamilies = (int)nudPeasantFamilies.Value;
            domain.UrbanFamilies = (int)nudUrbanFamilies.Value;
            domain.StrongholdValueGp = (int)nudStrongholdValue.Value;
            domain.GarrisonGpPerFamily = (int)nudGarrison.Value;
            domain.TaxGpPerFamily = (int)nudTax.Value;
            domain.LiturgiesGpPerFamily = (int)nudLiturgies.Value;
            domain.TithesGpPerFamily = (int)nudTithes.Value;
            domain.MaintenanceGpPerFamily = (int)nudMaintenance.Value;
            domain.CurrentMorale = (int)nudCurrentMorale.Value;
            domain.Notes = txtNotes.Text;
            domain.Ruler = BuildRulerRecord();
            if (domain.DomainType == "Clanhold"
                && (domain.Race == "Orc" || domain.Race == "Beastman")
                && domain.Ruler != null
                && domain.Ruler.Snapshot != null)
            {
                domain.Ruler.Snapshot.Alignment = "Chaotic";
            }
            domain.BaseMorale = AcksDomainRules.CalculateMorale(domain).BaseMorale;
            domain.UpdatedAt = DateTime.Now;
        }

        private DomainRulerRecord BuildRulerRecord()
        {
            string source = ComboText(cmbRulerSource, "None");
            if (source == "None")
            {
                return new DomainRulerRecord();
            }

            if (source == "Library")
            {
                CharacterRecord selected = cmbRulerLibrary.SelectedItem as CharacterRecord;
                DomainRulerRecord ruler = DomainRulerRecord.FromCharacter(selected, "Library");
                ruler.SaveGeneratedToLibrary = false;
                return ruler;
            }

            CharacterRecord snapshot = new CharacterRecord
            {
                Kind = "NPC",
                Name = string.IsNullOrWhiteSpace(txtRulerName.Text) ? (isEnglish ? "Generated ruler" : "\u041f\u0440\u0430\u0432\u0438\u0442\u0435\u043b\u044c") : txtRulerName.Text.Trim(),
                ClassName = ComboText(cmbRulerClass, "NPC"),
                Level = (int)nudRulerLevel.Value,
                CHA = (int)nudRulerCha.Value,
                Alignment = ComboText(cmbRulerAlignment, "Neutral"),
                Proficiencies = chkRulerLeadership.Checked ? "Leadership" : ""
            };

            return new DomainRulerRecord
            {
                SourceMode = source,
                Snapshot = snapshot,
                LibraryCharacterId = "",
                SaveGeneratedToLibrary = chkSaveRuler.Checked
            };
        }

        private void UpdateSummary()
        {
            if (txtSummary == null) return;
            DomainRecord preview = CloneDomain(Domain);
            ApplyToDomain(preview);
            txtSummary.Text = AcksDomainRules.BuildSummary(preview);
        }

        private void UpdateRulerControls()
        {
            string source = ComboText(cmbRulerSource, "None");
            bool library = source == "Library";
            bool manual = source == "Manual" || source == "Generated";

            cmbRulerLibrary.Enabled = library;
            txtRulerName.Enabled = manual;
            cmbRulerClass.Enabled = manual;
            nudRulerLevel.Enabled = manual;
            nudRulerCha.Enabled = manual;
            cmbRulerAlignment.Enabled = manual;
            chkRulerLeadership.Enabled = manual;
            chkSaveRuler.Enabled = manual;
        }

        private void LoadSelectedLibraryRuler()
        {
            if (ComboText(cmbRulerSource, "None") != "Library") return;
            CharacterRecord selected = cmbRulerLibrary.SelectedItem as CharacterRecord;
            if (selected == null) return;
            LoadRulerSnapshot(selected);
        }

        private void GenerateRuler()
        {
            Random random = new Random();
            string[] classes = { "Fighter", "Venturer", "Crusader", "Thief", "Mage" };
            DomainRecord preview = CloneDomain(Domain);
            ApplyToDomain(preview);
            double currentNetIncome = AcksDomainRules.CalculateFinancials(preview).NetIncome;

            SelectCombo(cmbRulerSource, "Generated");
            txtRulerName.Text = isEnglish ? "Generated ruler" : "\u041f\u0440\u0430\u0432\u0438\u0442\u0435\u043b\u044c";
            SelectCombo(cmbRulerClass, classes[random.Next(classes.Length)]);
            nudRulerLevel.Value = Math.Max(1, Math.Min(14, AcksDomainRules.IncomeBand(currentNetIncome)));
            nudRulerCha.Value = Roll3d6(random);
            SelectCombo(cmbRulerAlignment, new[] { "Lawful", "Neutral", "Chaotic" }[random.Next(3)]);
            chkRulerLeadership.Checked = random.Next(1, 7) >= 5;
            chkSaveRuler.Checked = true;
            UpdateRulerControls();
            UpdateSummary();
        }

        private static int Roll3d6(Random random)
        {
            return random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7);
        }

        private string NormalizeRulerSource(string source)
        {
            if (source == "LibraryCharacter") return "Library";
            if (source == "ManualSnapshot") return "Manual";
            if (source == "GeneratedNpc") return "Generated";
            return source;
        }

        private static DomainRecord CloneDomain(DomainRecord source)
        {
            DomainRecord clone = new DomainRecord();
            if (source == null) return clone;

            clone.Id = source.Id;
            clone.Name = source.Name;
            clone.DomainType = source.DomainType;
            clone.Race = source.Race;
            clone.Classification = source.Classification;
            clone.DomainAlignment = source.DomainAlignment;
            clone.LandValueMode = source.LandValueMode;
            clone.FixedLandValueGp = source.FixedLandValueGp;
            clone.PeasantFamilies = source.PeasantFamilies;
            clone.UrbanFamilies = source.UrbanFamilies;
            clone.StrongholdValueGp = source.StrongholdValueGp;
            clone.GarrisonGpPerFamily = source.GarrisonGpPerFamily;
            clone.TaxGpPerFamily = source.TaxGpPerFamily;
            clone.LiturgiesGpPerFamily = source.LiturgiesGpPerFamily;
            clone.TithesGpPerFamily = source.TithesGpPerFamily;
            clone.MaintenanceGpPerFamily = source.MaintenanceGpPerFamily;
            clone.BaseMorale = source.BaseMorale;
            clone.CurrentMorale = source.CurrentMorale;
            clone.ColorArgb = source.ColorArgb;
            clone.RealmId = source.RealmId;
            clone.CapitalSettlementId = source.CapitalSettlementId;
            clone.SettlementIds = source.SettlementIds == null ? new List<string>() : new List<string>(source.SettlementIds);
            clone.StrongholdId = source.StrongholdId;
            clone.StrongholdName = source.StrongholdName;
            clone.StrongholdQ = source.StrongholdQ;
            clone.StrongholdR = source.StrongholdR;
            clone.StrongholdType = source.StrongholdType;
            clone.StrongholdIconKey = source.StrongholdIconKey;
            clone.StrongholdInSettlement = source.StrongholdInSettlement;
            clone.StrongholdSettlementId = source.StrongholdSettlementId;
            clone.StrongholdActsAsMarketClassVI = source.StrongholdActsAsMarketClassVI;
            clone.StrongholdSecuresDomain = source.StrongholdSecuresDomain;
            clone.StrongholdIsUnderground = source.StrongholdIsUnderground;
            clone.StrongholdNaturalMajesty = source.StrongholdNaturalMajesty;
            clone.Notes = source.Notes;
            clone.UpdatedAt = source.UpdatedAt;
            clone.Hexes = source.Hexes == null
                ? new List<DomainHexRecord>()
                : source.Hexes.Select(h => new DomainHexRecord { Q = h.Q, R = h.R, LandValueGp = h.LandValueGp }).ToList();
            clone.Ruler = new DomainRulerRecord
            {
                SourceMode = source.Ruler == null ? "None" : source.Ruler.SourceMode,
                LibraryCharacterId = source.Ruler == null ? "" : source.Ruler.LibraryCharacterId,
                Snapshot = source.Ruler == null ? null : DomainRulerRecord.CloneCharacter(source.Ruler.Snapshot),
                SaveGeneratedToLibrary = source.Ruler != null && source.Ruler.SaveGeneratedToLibrary
            };
            return clone;
        }

        private static void SelectCombo(ComboBox combo, string value)
        {
            if (combo == null) return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                object item = combo.Items[i];
                if (string.Equals(item == null ? "" : item.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private static string ComboText(ComboBox combo, string fallback)
        {
            if (combo == null || combo.SelectedItem == null) return fallback;
            string text = combo.SelectedItem.ToString();
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        private static decimal ClampDecimal(int value, NumericUpDown control)
        {
            return Math.Max(control.Minimum, Math.Min(control.Maximum, value));
        }

        private void AddSection(Control parent, string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y, 620, 22);
            label.Font = UiTheme.CreateFont(FontStyle.Bold);
            label.ForeColor = Color.White;
            parent.Controls.Add(label);
        }

        private Label AddLabel(Control parent, string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y + 3, width, height);
            label.ForeColor = Color.White;
            parent.Controls.Add(label);
            return label;
        }

        private TextBox AddText(Control parent, int x, int y, int labelWidth, int inputWidth, string label)
        {
            AddLabel(parent, label, x, y, labelWidth, 20);
            TextBox text = new TextBox();
            text.SetBounds(x + labelWidth, y, inputWidth, 24);
            parent.Controls.Add(text);
            return text;
        }

        private ComboBox AddCombo(Control parent, int x, int y, int labelWidth, int inputWidth, string label, string[] values)
        {
            AddLabel(parent, label, x, y, labelWidth, 20);
            ComboBox combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.SetBounds(x + labelWidth, y, inputWidth, 24);
            if (values != null && values.Length > 0) combo.Items.AddRange(values.Cast<object>().ToArray());
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            parent.Controls.Add(combo);
            return combo;
        }

        private NumericUpDown AddNumber(Control parent, int x, int y, int labelWidth, int inputWidth, string label, int min, int max, int value)
        {
            AddLabel(parent, label, x, y, labelWidth, 20);
            NumericUpDown number = new NumericUpDown();
            number.Minimum = min;
            number.Maximum = max;
            number.ThousandsSeparator = true;
            number.SetBounds(x + labelWidth, y, inputWidth, 24);
            number.Value = Math.Max(min, Math.Min(max, value));
            parent.Controls.Add(number);
            return number;
        }

        private CheckBox AddCheck(Control parent, int x, int y, string text)
        {
            CheckBox check = new CheckBox();
            check.Text = text;
            check.SetBounds(x, y, 320, 22);
            check.ForeColor = Color.White;
            check.BackColor = Color.Transparent;
            parent.Controls.Add(check);
            return check;
        }

        private Button CreateButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            UiTheme.StyleCommandButton(button, color);
            return button;
        }
    }
}
