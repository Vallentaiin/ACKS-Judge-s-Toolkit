using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private Label lblMapDomains;
        private ListBox lstMapDomains;
        private Button btnMapDomainNew;
        private Button btnMapDomainEdit;
        private Button btnMapDomainDelete;
        private Button btnMapDomainAddHex;
        private Button btnMapDomainRemoveHex;
        private Button btnMapDomainRecalculate;
        private CheckBox chkMapShowDomains;
        private CheckBox chkMapShowRealms;
        private Label lblMapRealmTier;
        private ComboBox cmbMapRealmTier;
        private Label lblMapRealms;
        private ListBox lstMapRealms;
        private Button btnMapRealmNew;
        private Button btnMapRealmEdit;
        private Button btnMapRealmDelete;
        private Button btnMapRealmCenter;
        private Button btnMapRealmHierarchy;
        private Label lblMapDomainFilters;
        private TextBox txtMapDomainSearch;
        private ComboBox cmbMapDomainClassificationFilter;
        private ComboBox cmbMapDomainTypeFilter;
        private Label lblMapRealmFilters;
        private TextBox txtMapRealmSearch;
        private ComboBox cmbMapRealmTierFilter;
        private ComboBox cmbMapRealmIndependenceFilter;
        private ComboBox cmbMapRealmRaceFilter;
        private TextBox txtMapRealmHierarchy;
        private TextBox txtMapDomainSummary;
        private string selectedMapDomainId;
        private string selectedMapRealmId;
        private bool isRefreshingMapDomainList;
        private bool isRefreshingMapRealmList;
        private bool keepMapDomainSelectionCleared;
        private bool keepMapRealmSelectionCleared;

        private void EnsureMapDomainControls()
        {
            if (lblMapDomains != null || pnlMapTools == null) return;

            chkMapShowDomains = new CheckBox
            {
                Checked = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text = isEnglish ? "Domain overlay" : "\u0414\u043e\u043c\u0435\u043d\u044b \u043d\u0430 \u043a\u0430\u0440\u0442\u0435"
            };
            chkMapShowDomains.CheckedChanged += (s, e) => pnlHexMap.Invalidate();

            chkMapShowRealms = new CheckBox
            {
                Checked = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text = isEnglish ? "Realm overlay" : "\u0420\u0435\u0430\u043b\u043c\u044b \u043d\u0430 \u043a\u0430\u0440\u0442\u0435"
            };
            chkMapShowRealms.CheckedChanged += (s, e) => pnlHexMap.Invalidate();

            lblMapRealmTier = CreateMapDomainLabel(isEnglish ? "Realm tier" : "\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u0440\u0435\u0430\u043b\u043c\u043e\u0432");
            cmbMapRealmTier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            FillMapRealmTierCombo();
            cmbMapRealmTier.SelectedIndexChanged += (s, e) => pnlHexMap.Invalidate();

            lblMapDomains = CreateMapDomainLabel(isEnglish ? "Domains" : "\u0414\u043e\u043c\u0435\u043d\u044b");
            lstMapDomains = new ListBox
            {
                BackColor = Color.FromArgb(58, 58, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstMapDomains.SelectedIndexChanged += (s, e) =>
            {
                if (isRefreshingMapDomainList) return;

                DomainRecord domain = lstMapDomains.SelectedItem as DomainRecord;
                selectedMapDomainId = domain == null ? null : domain.Id;
                if (domain != null) keepMapDomainSelectionCleared = false;
                UpdateMapDomainSummary();
                pnlHexMap.Invalidate();
            };
            lstMapDomains.DoubleClick += (s, e) => CenterMapOnSelectedDomain();

            btnMapDomainNew = CreateMapDomainButton(isEnglish ? "New" : "\u041d\u043e\u0432\u044b\u0439", UiTheme.PositiveButtonColor);
            btnMapDomainEdit = CreateMapDomainButton(isEnglish ? "Edit" : "\u0418\u0437\u043c.", UiTheme.PositiveButtonColor);
            btnMapDomainDelete = CreateMapDomainButton(isEnglish ? "Delete" : "\u0423\u0434\u0430\u043b.", UiTheme.NegativeButtonColor);
            btnMapDomainAddHex = CreateMapDomainButton(isEnglish ? "Add hex" : "+ \u0433\u0435\u043a\u0441", UiTheme.PositiveButtonColor);
            btnMapDomainRemoveHex = CreateMapDomainButton(isEnglish ? "Remove hex" : "- \u0433\u0435\u043a\u0441", UiTheme.NegativeButtonColor);
            btnMapDomainRecalculate = CreateMapDomainButton(isEnglish ? "Recalculate" : "\u041f\u0435\u0440\u0435\u0441\u0447\u0435\u0442", UiTheme.PositiveButtonColor);
            lblMapDomainFilters = CreateMapDomainLabel(isEnglish ? "Domain filters" : "Фильтры доменов");
            txtMapDomainSearch = CreateMapFilterTextBox();
            cmbMapDomainClassificationFilter = CreateMapFilterCombo();
            cmbMapDomainTypeFilter = CreateMapFilterCombo();
            txtMapDomainSearch.TextChanged += (s, e) => RefreshMapDomainList();
            cmbMapDomainClassificationFilter.SelectedIndexChanged += (s, e) => RefreshMapDomainList();
            cmbMapDomainTypeFilter.SelectedIndexChanged += (s, e) => RefreshMapDomainList();
            lblMapRealms = CreateMapDomainLabel(isEnglish ? "Realms" : "Державы");
            lstMapRealms = new ListBox
            {
                BackColor = Color.FromArgb(58, 58, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstMapRealms.Format += (s, e) =>
            {
                RealmRecord realm = e.ListItem as RealmRecord;
                if (realm != null) e.Value = LocalizedRealmDisplayName(realm);
            };
            lstMapRealms.SelectedIndexChanged += (s, e) =>
            {
                if (isRefreshingMapRealmList) return;
                RealmRecord realm = lstMapRealms.SelectedItem as RealmRecord;
                selectedMapRealmId = realm == null ? null : realm.Id;
                UpdateMapRealmHierarchy();
                pnlHexMap.Invalidate();
            };
            lstMapRealms.DoubleClick += (s, e) => CenterMapOnSelectedRealm();

            btnMapRealmNew = CreateMapDomainButton(isEnglish ? "New" : "Новая", UiTheme.PositiveButtonColor);
            btnMapRealmEdit = CreateMapDomainButton(isEnglish ? "Edit" : "Изм.", UiTheme.PositiveButtonColor);
            btnMapRealmDelete = CreateMapDomainButton(isEnglish ? "Delete" : "Удал.", UiTheme.NegativeButtonColor);
            btnMapRealmCenter = CreateMapDomainButton(isEnglish ? "Center" : "Центр", UiTheme.NeutralButtonColor);
            btnMapRealmHierarchy = CreateMapDomainButton(isEnglish ? "Scheme" : "Схема", UiTheme.NeutralButtonColor);
            lblMapRealmFilters = CreateMapDomainLabel(isEnglish ? "Realm filters" : "Фильтры держав");
            txtMapRealmSearch = CreateMapFilterTextBox();
            cmbMapRealmTierFilter = CreateMapFilterCombo();
            cmbMapRealmIndependenceFilter = CreateMapFilterCombo();
            cmbMapRealmRaceFilter = CreateMapFilterCombo();
            txtMapRealmSearch.TextChanged += (s, e) => RefreshMapRealmList();
            cmbMapRealmTierFilter.SelectedIndexChanged += (s, e) => RefreshMapRealmList();
            cmbMapRealmIndependenceFilter.SelectedIndexChanged += (s, e) => RefreshMapRealmList();
            cmbMapRealmRaceFilter.SelectedIndexChanged += (s, e) => RefreshMapRealmList();
            FillMapLibraryFilterCombos();

            btnMapDomainNew.Click += (s, e) => CreateMapDomain();
            btnMapDomainEdit.Click += (s, e) => EditSelectedMapDomain();
            btnMapDomainDelete.Click += (s, e) => DeleteSelectedMapDomain();
            btnMapDomainAddHex.Click += (s, e) => AddSelectedHexToMapDomain();
            btnMapDomainRemoveHex.Click += (s, e) => RemoveSelectedHexFromMapDomain();
            btnMapDomainRecalculate.Click += (s, e) => RecalculateSelectedMapDomain();
            btnMapRealmNew.Click += (s, e) => CreateMapRealm();
            btnMapRealmEdit.Click += (s, e) => EditSelectedMapRealm();
            btnMapRealmDelete.Click += (s, e) => DeleteSelectedMapRealm();
            btnMapRealmCenter.Click += (s, e) => CenterMapOnSelectedRealm();
            btnMapRealmHierarchy.Click += (s, e) => OpenRealmHierarchyDialog();

            txtMapDomainSummary = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(58, 58, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtMapRealmHierarchy = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(58, 58, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlMapTools.Controls.Add(chkMapShowDomains);
            pnlMapTools.Controls.Add(chkMapShowRealms);
            pnlMapTools.Controls.Add(lblMapRealmTier);
            pnlMapTools.Controls.Add(cmbMapRealmTier);
            pnlMapTools.Controls.Add(lblMapDomains);
            pnlMapTools.Controls.Add(lstMapDomains);
            pnlMapTools.Controls.Add(btnMapDomainNew);
            pnlMapTools.Controls.Add(btnMapDomainEdit);
            pnlMapTools.Controls.Add(btnMapDomainDelete);
            pnlMapTools.Controls.Add(btnMapDomainAddHex);
            pnlMapTools.Controls.Add(btnMapDomainRemoveHex);
            pnlMapTools.Controls.Add(btnMapDomainRecalculate);
            pnlMapTools.Controls.Add(txtMapDomainSummary);
            pnlMapTools.Controls.Add(lblMapDomainFilters);
            pnlMapTools.Controls.Add(txtMapDomainSearch);
            pnlMapTools.Controls.Add(cmbMapDomainClassificationFilter);
            pnlMapTools.Controls.Add(cmbMapDomainTypeFilter);
            pnlMapTools.Controls.Add(lblMapRealms);
            pnlMapTools.Controls.Add(lstMapRealms);
            pnlMapTools.Controls.Add(btnMapRealmNew);
            pnlMapTools.Controls.Add(btnMapRealmEdit);
            pnlMapTools.Controls.Add(btnMapRealmDelete);
            pnlMapTools.Controls.Add(btnMapRealmCenter);
            pnlMapTools.Controls.Add(btnMapRealmHierarchy);
            pnlMapTools.Controls.Add(lblMapRealmFilters);
            pnlMapTools.Controls.Add(txtMapRealmSearch);
            pnlMapTools.Controls.Add(cmbMapRealmTierFilter);
            pnlMapTools.Controls.Add(cmbMapRealmIndependenceFilter);
            pnlMapTools.Controls.Add(cmbMapRealmRaceFilter);
            pnlMapTools.Controls.Add(txtMapRealmHierarchy);
        }

        private Label CreateMapDomainLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = UiTheme.CreateFont(FontStyle.Bold)
            };
        }

        private Button CreateMapDomainButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            UiTheme.StyleCommandButton(button, color);
            return button;
        }

        private TextBox CreateMapFilterTextBox()
        {
            return new TextBox
            {
                BackColor = Color.White,
                ForeColor = Color.Black
            };
        }

        private ComboBox CreateMapFilterCombo()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
        }

        private void FillMapLibraryFilterCombos()
        {
            FillFilterCombo(cmbMapDomainClassificationFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any status" : "Любая освоенность"),
                    new FilterItem("Outlands", "Outlands"),
                    new FilterItem("Borderlands", "Borderlands"),
                    new FilterItem("Civilized", "Civilized")
                });
            FillFilterCombo(cmbMapDomainTypeFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any type" : "Любой тип"),
                    new FilterItem("Ordinary", isEnglish ? "Ordinary" : "Обычный"),
                    new FilterItem("Dwarven Vault", isEnglish ? "Dwarven" : "Дварфийский"),
                    new FilterItem("Elven Fastness", isEnglish ? "Elven" : "Эльфийский"),
                    new FilterItem("Clanhold", isEnglish ? "Clanhold" : "Клановый"),
                    new FilterItem("Transitional", isEnglish ? "Transitional" : "Переходный")
                });
            FillFilterCombo(cmbMapRealmTierFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any title" : "Любой титул"),
                    new FilterItem("Empire", isEnglish ? "Empire" : "Империя"),
                    new FilterItem("Kingdom", isEnglish ? "Kingdom" : "Королевство"),
                    new FilterItem("Principality", isEnglish ? "Principality" : "Княжество"),
                    new FilterItem("Duchy", isEnglish ? "Duchy" : "Герцогство"),
                    new FilterItem("County", isEnglish ? "County" : "Графство"),
                    new FilterItem("Viscounty", isEnglish ? "Viscounty" : "Виконтство"),
                    new FilterItem("Barony", isEnglish ? "Barony" : "Баронство")
                });
            FillFilterCombo(cmbMapRealmIndependenceFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any status" : "Любой статус"),
                    new FilterItem("Independent", isEnglish ? "Independent" : "Независимые"),
                    new FilterItem("Vassal", isEnglish ? "Vassals" : "Вассалы")
                });
            FillFilterCombo(cmbMapRealmRaceFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any race" : "Любая раса"),
                    new FilterItem("Human", isEnglish ? "Human" : "Люди"),
                    new FilterItem("HumanClan", isEnglish ? "Human clanholds" : "Клановые люди"),
                    new FilterItem("Dwarf", isEnglish ? "Dwarf" : "Дварфы"),
                    new FilterItem("Elf", isEnglish ? "Elf" : "Эльфы"),
                    new FilterItem("Orc", isEnglish ? "Orc" : "Орки"),
                    new FilterItem("Beastman", isEnglish ? "Beastman" : "Зверолюды")
                });
        }

        private void FillFilterCombo(ComboBox combo, IEnumerable<FilterItem> items)
        {
            if (combo == null) return;
            string selected = SelectedFilterValue(combo);
            combo.Items.Clear();
            foreach (FilterItem item in items)
            {
                combo.Items.Add(item);
            }

            int selectedIndex = 0;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FilterItem item = combo.Items[i] as FilterItem;
                if (item != null && string.Equals(item.Value, selected, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = selectedIndex;
        }

        private string SelectedFilterValue(ComboBox combo)
        {
            FilterItem item = combo == null ? null : combo.SelectedItem as FilterItem;
            return item == null ? "" : item.Value;
        }

        private void FillMapRealmTierCombo()
        {
            if (cmbMapRealmTier == null) return;
            string selected = cmbMapRealmTier.SelectedItem == null ? "All" : (cmbMapRealmTier.SelectedItem as RealmTierItem)?.Value;
            cmbMapRealmTier.Items.Clear();
            cmbMapRealmTier.Items.Add(new RealmTierItem("All", isEnglish ? "All" : "\u0412\u0441\u0435"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Empire", isEnglish ? "Empire" : "\u0418\u043c\u043f\u0435\u0440\u0438\u0438"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Kingdom", isEnglish ? "Kingdom" : "\u041a\u043e\u0440\u043e\u043b\u0435\u0432\u0441\u0442\u0432\u0430"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Principality", isEnglish ? "Principality" : "\u041a\u043d\u044f\u0436\u0435\u0441\u0442\u0432\u0430"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Duchy", isEnglish ? "Duchy" : "\u0413\u0435\u0440\u0446\u043e\u0433\u0441\u0442\u0432\u0430"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("County", isEnglish ? "County" : "\u0413\u0440\u0430\u0444\u0441\u0442\u0432\u0430"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Viscounty", isEnglish ? "Viscounty" : "\u0412\u0438\u043a\u043e\u043d\u0442\u0441\u0442\u0432\u0430"));
            cmbMapRealmTier.Items.Add(new RealmTierItem("Barony", isEnglish ? "Domains / baronies" : "\u0414\u043e\u043c\u0435\u043d\u044b / \u0431\u0430\u0440\u043e\u043d\u0441\u0442\u0432\u0430"));

            for (int i = 0; i < cmbMapRealmTier.Items.Count; i++)
            {
                RealmTierItem item = cmbMapRealmTier.Items[i] as RealmTierItem;
                if (item != null && string.Equals(item.Value, selected, StringComparison.OrdinalIgnoreCase))
                {
                    cmbMapRealmTier.SelectedIndex = i;
                    return;
                }
            }

            if (cmbMapRealmTier.Items.Count > 0) cmbMapRealmTier.SelectedIndex = 0;
        }

        private string SelectedMapRealmTier()
        {
            RealmTierItem item = cmbMapRealmTier == null ? null : cmbMapRealmTier.SelectedItem as RealmTierItem;
            return item == null ? "All" : item.Value;
        }

        private sealed class RealmTierItem
        {
            public string Value { get; private set; }
            private readonly string label;

            public RealmTierItem(string value, string label)
            {
                Value = value;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }

        private sealed class FilterItem
        {
            public string Value { get; private set; }
            private readonly string label;

            public FilterItem(string value, string label)
            {
                Value = value ?? "";
                this.label = label ?? "";
            }

            public override string ToString()
            {
                return label;
            }
        }

        private void LayoutMapDomainControls(int left, int right, int leftWidth, int rightWidth, int y)
        {
            if (lblMapDomains == null) return;
            chkMapShowDomains.SetBounds(left, y, leftWidth, 22);
            chkMapShowRealms.SetBounds(right, y, rightWidth, 22);
            lblMapRealmTier.SetBounds(left, y + 28, 120, 20);
            cmbMapRealmTier.SetBounds(left + 126, y + 26, 170, 24);
            lblMapDomains.SetBounds(left, y + 58, leftWidth, 20);
            lblMapDomainFilters.SetBounds(left, y + 80, leftWidth, 20);
            txtMapDomainSearch.SetBounds(left, y + 102, leftWidth, 24);
            cmbMapDomainClassificationFilter.SetBounds(left, y + 132, 146, 24);
            cmbMapDomainTypeFilter.SetBounds(left + 154, y + 132, 146, 24);
            lstMapDomains.SetBounds(left, y + 164, leftWidth, 104);
            btnMapDomainNew.SetBounds(right, y + 164, 78, 28);
            btnMapDomainEdit.SetBounds(right + 86, y + 164, 78, 28);
            btnMapDomainDelete.SetBounds(right + 172, y + 164, 78, 28);
            btnMapDomainAddHex.SetBounds(right, y + 198, 120, 28);
            btnMapDomainRemoveHex.SetBounds(right + 130, y + 198, 120, 28);
            btnMapDomainRecalculate.SetBounds(right, y + 232, rightWidth, 28);
            txtMapDomainSummary.SetBounds(left, y + 278, 510, 132);
            lblMapRealms.SetBounds(left, y + 420, leftWidth, 20);
            lblMapRealmFilters.SetBounds(left, y + 442, leftWidth, 20);
            txtMapRealmSearch.SetBounds(left, y + 464, leftWidth, 24);
            cmbMapRealmTierFilter.SetBounds(left, y + 494, 146, 24);
            cmbMapRealmIndependenceFilter.SetBounds(left + 154, y + 494, 146, 24);
            cmbMapRealmRaceFilter.SetBounds(left, y + 524, leftWidth, 24);
            lstMapRealms.SetBounds(left, y + 556, leftWidth, 104);
            btnMapRealmNew.SetBounds(right, y + 556, 78, 28);
            btnMapRealmEdit.SetBounds(right + 86, y + 556, 78, 28);
            btnMapRealmDelete.SetBounds(right + 172, y + 556, 78, 28);
            btnMapRealmCenter.SetBounds(right, y + 590, 120, 28);
            btnMapRealmHierarchy.SetBounds(right + 130, y + 590, 120, 28);
            txtMapRealmHierarchy.SetBounds(left, y + 668, 510, 180);
        }

        private void UpdateMapDomainLanguage()
        {
            if (lblMapDomains == null) return;

            chkMapShowDomains.Text = isEnglish ? "Domain overlay" : "\u0414\u043e\u043c\u0435\u043d\u044b \u043d\u0430 \u043a\u0430\u0440\u0442\u0435";
            chkMapShowRealms.Text = isEnglish ? "Realm overlay" : "\u0420\u0435\u0430\u043b\u043c\u044b \u043d\u0430 \u043a\u0430\u0440\u0442\u0435";
            lblMapRealmTier.Text = isEnglish ? "Realm tier" : "\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u0440\u0435\u0430\u043b\u043c\u043e\u0432";
            FillMapRealmTierCombo();
            lblMapDomains.Text = isEnglish ? "Domains" : "\u0414\u043e\u043c\u0435\u043d\u044b";
            btnMapDomainNew.Text = isEnglish ? "New" : "\u041d\u043e\u0432\u044b\u0439";
            btnMapDomainEdit.Text = isEnglish ? "Edit" : "\u0418\u0437\u043c.";
            btnMapDomainDelete.Text = isEnglish ? "Delete" : "\u0423\u0434\u0430\u043b.";
            btnMapDomainAddHex.Text = isEnglish ? "Add hex" : "+ \u0433\u0435\u043a\u0441";
            btnMapDomainRemoveHex.Text = isEnglish ? "Remove hex" : "- \u0433\u0435\u043a\u0441";
            btnMapDomainRecalculate.Text = isEnglish ? "Recalculate" : "\u041f\u0435\u0440\u0435\u0441\u0447\u0435\u0442";
            lblMapDomainFilters.Text = isEnglish ? "Domain filters" : "Фильтры доменов";
            lblMapRealms.Text = isEnglish ? "Realms" : "Державы";
            btnMapRealmNew.Text = isEnglish ? "New" : "Новая";
            btnMapRealmEdit.Text = isEnglish ? "Edit" : "Изм.";
            btnMapRealmDelete.Text = isEnglish ? "Delete" : "Удал.";
            btnMapRealmCenter.Text = isEnglish ? "Center" : "Центр";
            btnMapRealmHierarchy.Text = isEnglish ? "Scheme" : "Схема";
            lblMapRealmFilters.Text = isEnglish ? "Realm filters" : "Фильтры держав";
            FillMapLibraryFilterCombos();
            RefreshMapDomainList();
            RefreshMapRealmList();
            UpdateMapDomainSummary();
            UpdateMapRealmHierarchy();
        }

        private void RefreshMapDomainList()
        {
            if (lstMapDomains == null) return;
            string selectedId = selectedMapDomainId;
            bool keepCleared = keepMapDomainSelectionCleared && string.IsNullOrWhiteSpace(selectedId);

            isRefreshingMapDomainList = true;
            try
            {
                lstMapDomains.DataSource = null;

                if (currentMap == null || currentMap.Domains == null)
                {
                    selectedMapDomainId = null;
                    keepMapDomainSelectionCleared = false;
                    if (txtMapDomainSummary != null) txtMapDomainSummary.Text = "";
                    return;
                }

                List<DomainRecord> domains = currentMap.Domains
                    .Where(MapDomainMatchesFilters)
                    .OrderBy(d => d.Name)
                    .ToList();
                lstMapDomains.DataSource = domains;
                lstMapDomains.DisplayMember = "DisplayName";

                if (!string.IsNullOrWhiteSpace(selectedId))
                {
                    for (int i = 0; i < lstMapDomains.Items.Count; i++)
                    {
                        DomainRecord domain = lstMapDomains.Items[i] as DomainRecord;
                        if (domain != null && domain.Id == selectedId)
                        {
                            lstMapDomains.SelectedIndex = i;
                            selectedMapDomainId = domain.Id;
                            keepMapDomainSelectionCleared = false;
                            UpdateMapDomainSummary();
                            return;
                        }
                    }
                }

                if (keepCleared)
                {
                    lstMapDomains.SelectedIndex = -1;
                    lstMapDomains.ClearSelected();
                    selectedMapDomainId = null;
                    UpdateMapDomainSummary();
                    return;
                }

                if (lstMapDomains.Items.Count > 0)
                {
                    lstMapDomains.SelectedIndex = 0;
                    DomainRecord domain = lstMapDomains.SelectedItem as DomainRecord;
                    selectedMapDomainId = domain == null ? null : domain.Id;
                }
                else
                {
                    selectedMapDomainId = null;
                }

                keepMapDomainSelectionCleared = false;
                UpdateMapDomainSummary();
            }
            finally
            {
                isRefreshingMapDomainList = false;
            }

            RefreshMapRealmList();
        }

        private bool MapDomainMatchesFilters(DomainRecord domain)
        {
            if (domain == null) return false;

            string search = txtMapDomainSearch == null ? "" : txtMapDomainSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search)
                && !ContainsIgnoreCase(domain.Name, search)
                && !ContainsIgnoreCase(domain.DisplayName, search)
                && !ContainsIgnoreCase(domain.Race, search)
                && !ContainsIgnoreCase(domain.DomainType, search)
                && !ContainsIgnoreCase(domain.Classification, search))
            {
                return false;
            }

            string classification = SelectedFilterValue(cmbMapDomainClassificationFilter);
            if (!string.IsNullOrWhiteSpace(classification)
                && !string.Equals(domain.Classification, classification, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string type = SelectedFilterValue(cmbMapDomainTypeFilter);
            return string.IsNullOrWhiteSpace(type)
                || string.Equals(domain.DomainType, type, StringComparison.OrdinalIgnoreCase);
        }

        private bool ContainsIgnoreCase(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private DomainRecord GetSelectedMapDomain()
        {
            DomainRecord selected = lstMapDomains == null ? null : lstMapDomains.SelectedItem as DomainRecord;
            if (selected != null) return selected;
            if (currentMap == null || currentMap.Domains == null || string.IsNullOrWhiteSpace(selectedMapDomainId)) return null;
            return currentMap.Domains.FirstOrDefault(d => d.Id == selectedMapDomainId);
        }

        private DomainRecord GetDomainAtCell(HexCellRecord cell)
        {
            if (currentMap == null || currentMap.Domains == null || cell == null) return null;
            string key = CellKey(cell.Q, cell.R);
            DomainRecord indexed;
            if (currentMapDomainByHex != null && currentMapDomainByHex.TryGetValue(key, out indexed))
            {
                return indexed;
            }

            return currentMap.Domains.FirstOrDefault(d => d.Hexes != null && d.Hexes.Any(h => h.Key() == key));
        }

        private void SelectMapDomainAtCell(HexCellRecord cell)
        {
            DomainRecord domain = GetDomainAtCell(cell);
            if (domain != null)
            {
                SelectMapDomainInList(domain.Id);
                return;
            }

            SelectMapDomainInList(null);
        }

        private void SelectMapRealmAtCell(HexCellRecord cell)
        {
            RealmRecord realm = GetRealmAtCell(cell);
            SelectMapRealmInList(realm == null ? null : realm.Id);
        }

        private RealmRecord GetRealmAtCell(HexCellRecord cell)
        {
            if (currentMap == null || currentMap.Realms == null || cell == null) return null;

            DomainRecord domain = GetDomainAtCell(cell);
            if (domain == null || string.IsNullOrWhiteSpace(domain.RealmId)) return null;

            string selectedTier = SelectedMapRealmTier();
            if (string.Equals(selectedTier, "All", StringComparison.OrdinalIgnoreCase)
                || string.Equals(selectedTier, "Barony", StringComparison.OrdinalIgnoreCase))
            {
                return currentMap.Realms.FirstOrDefault(r => r != null && string.Equals(r.Id, domain.RealmId, StringComparison.OrdinalIgnoreCase));
            }

            return currentMap.Realms
                .Where(r => r != null && string.Equals(r.Tier, selectedTier, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(r => RealmAndVassalIds(r.Id).Contains(domain.RealmId));
        }

        private void SelectMapDomainInList(string domainId)
        {
            selectedMapDomainId = domainId;
            keepMapDomainSelectionCleared = string.IsNullOrWhiteSpace(domainId);
            if (lstMapDomains == null)
            {
                UpdateMapDomainSummary();
                return;
            }

            if (string.IsNullOrWhiteSpace(domainId))
            {
                lstMapDomains.SelectedIndex = -1;
                lstMapDomains.ClearSelected();
                UpdateMapDomainSummary();
                pnlHexMap.Invalidate();
                return;
            }

            keepMapDomainSelectionCleared = false;
            for (int i = 0; i < lstMapDomains.Items.Count; i++)
            {
                DomainRecord domain = lstMapDomains.Items[i] as DomainRecord;
                if (domain == null || !string.Equals(domain.Id, domainId, StringComparison.OrdinalIgnoreCase)) continue;

                if (lstMapDomains.SelectedIndex != i)
                {
                    lstMapDomains.SelectedIndex = i;
                }

                try
                {
                    lstMapDomains.TopIndex = Math.Max(0, Math.Min(i, lstMapDomains.Items.Count - 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                UpdateMapDomainSummary();
                pnlHexMap.Invalidate();
                return;
            }

            UpdateMapDomainSummary();
            pnlHexMap.Invalidate();
        }

        private void CenterMapOnSelectedDomain()
        {
            CenterMapOnDomain(GetSelectedMapDomain());
        }

        private void CenterMapOnDomain(DomainRecord domain)
        {
            if (domain == null || domain.Hexes == null || domain.Hexes.Count == 0 || pnlHexMap == null) return;

            List<PointF> centers = domain.Hexes
                .Select(h => GetCell(h.Q, h.R))
                .Where(c => c != null)
                .Select(c => GetHexCenter(c.Q, c.R))
                .ToList();
            if (centers.Count == 0) return;

            PointF center = new PointF(
                centers.Average(p => p.X),
                centers.Average(p => p.Y));
            int desiredX = (int)Math.Round(center.X * mapZoom - pnlHexMap.ClientSize.Width / 2f);
            int desiredY = (int)Math.Round(center.Y * mapZoom - pnlHexMap.ClientSize.Height / 2f);
            SetMapScroll(new Point(desiredX, desiredY));
            pnlHexMap.Invalidate();
        }

        private void CreateMapDomain()
        {
            if (currentMap == null) return;
            NormalizeMap(currentMap);

            DomainRecord domain = new DomainRecord
            {
                Name = isEnglish ? "New domain" : "\u041d\u043e\u0432\u044b\u0439 \u0434\u043e\u043c\u0435\u043d",
                ColorArgb = GetNextDomainColor()
            };

            if (selectedMapCell != null)
            {
                domain.Hexes.Add(new DomainHexRecord
                {
                    Q = selectedMapCell.Q,
                    R = selectedMapCell.R,
                    LandValueGp = RollDomainLandValue(domain)
                });
            }

            using (DomainEditorDialog dialog = new DomainEditorDialog(isEnglish, domain, characterLibrary))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                domain = dialog.Domain;
                SaveDomainRulerIfRequested(domain);
                RecalculateDomain(domain, false);
                if (domain.Hexes != null)
                {
                    foreach (DomainHexRecord hex in domain.Hexes.ToList())
                    {
                        RemoveCellFromAllDomains(GetCell(hex.Q, hex.R), true);
                    }
                }
                currentMap.Domains.Add(domain);
                selectedMapDomainId = domain.Id;
            }

            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void EditSelectedMapDomain()
        {
            DomainRecord selected = GetSelectedMapDomain();
            if (currentMap == null || selected == null) return;

            using (DomainEditorDialog dialog = new DomainEditorDialog(isEnglish, selected, characterLibrary))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                int index = currentMap.Domains.FindIndex(d => d.Id == selected.Id);
                if (index < 0) return;

                DomainRecord edited = dialog.Domain;
                SaveDomainRulerIfRequested(edited);
                bool currentMoraleWasEdited = edited.CurrentMorale != selected.CurrentMorale;
                RecalculateDomain(edited, !currentMoraleWasEdited, selected.BaseMorale, selected.CurrentMorale);
                currentMap.Domains[index] = edited;
                selectedMapDomainId = edited.Id;
            }

            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void DeleteSelectedMapDomain()
        {
            DomainRecord selected = GetSelectedMapDomain();
            if (currentMap == null || selected == null) return;

            DialogResult result = MessageBox.Show(
                isEnglish ? "Delete selected domain?" : "\u0423\u0434\u0430\u043b\u0438\u0442\u044c \u0432\u044b\u0431\u0440\u0430\u043d\u043d\u044b\u0439 \u0434\u043e\u043c\u0435\u043d?",
                "ACKS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            currentMap.Domains.RemoveAll(d => d.Id == selected.Id);
            selectedMapDomainId = null;
            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void AddSelectedHexToMapDomain()
        {
            DomainRecord domain = GetSelectedMapDomain();
            if (currentMap == null || domain == null || selectedMapCell == null) return;

            RemoveCellFromAllDomains(selectedMapCell, true);

            if (domain.Hexes == null) domain.Hexes = new List<DomainHexRecord>();
            domain.Hexes.Add(new DomainHexRecord
            {
                Q = selectedMapCell.Q,
                R = selectedMapCell.R,
                LandValueGp = RollDomainLandValue(domain)
            });

            RecalculateDomain(domain, true);
            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void RemoveSelectedHexFromMapDomain()
        {
            DomainRecord domain = GetSelectedMapDomain();
            if (domain == null || selectedMapCell == null || domain.Hexes == null) return;

            string key = CellKey(selectedMapCell.Q, selectedMapCell.R);
            domain.Hexes.RemoveAll(h => h.Key() == key);
            RecalculateDomain(domain, true);
            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private bool RemoveCellFromAllDomains(HexCellRecord cell, bool recalculate)
        {
            if (currentMap == null || currentMap.Domains == null || cell == null) return false;

            string key = CellKey(cell.Q, cell.R);
            bool changed = false;
            foreach (DomainRecord domain in currentMap.Domains)
            {
                if (domain.Hexes == null) continue;
                int previousBaseMorale = domain.BaseMorale;
                int previousCurrentMorale = domain.CurrentMorale;
                int removed = domain.Hexes.RemoveAll(h => h.Key() == key);
                if (removed <= 0) continue;

                changed = true;
                if (recalculate)
                {
                    RecalculateDomain(domain, true, previousBaseMorale, previousCurrentMorale);
                }
            }

            return changed;
        }

        private void RecalculateSelectedMapDomain()
        {
            DomainRecord domain = GetSelectedMapDomain();
            if (domain == null) return;

            RecalculateDomain(domain, true);
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void SaveDomainRulerIfRequested(DomainRecord domain)
        {
            if (domain == null || domain.Ruler == null || !domain.Ruler.SaveGeneratedToLibrary) return;

            CharacterRecord record = domain.Ruler.ToLibraryCharacter();
            if (record == null || string.IsNullOrWhiteSpace(record.Name)) return;

            if (string.IsNullOrWhiteSpace(record.Id) || characterLibrary.Any(c => c.Id == record.Id))
            {
                record.Id = Guid.NewGuid().ToString("N");
            }

            characterLibrary.Add(record);
            SaveCharacterLibrary();
            RefreshCharacterList();

            domain.Ruler = DomainRulerRecord.FromCharacter(record, "Library");
            domain.Ruler.SaveGeneratedToLibrary = false;
        }

        private int GetNextDomainColor()
        {
            int[] colors =
            {
                unchecked((int)0x6637A86B),
                unchecked((int)0x668E5AB5),
                unchecked((int)0x66C8883D),
                unchecked((int)0x6680A7D9),
                unchecked((int)0x66C06B6B),
                unchecked((int)0x66B8B044)
            };

            int index = currentMap == null || currentMap.Domains == null ? 0 : currentMap.Domains.Count % colors.Length;
            return colors[index];
        }

        private int RollDomainLandValue(DomainRecord domain)
        {
            if (domain != null && string.Equals(domain.LandValueMode, "PerHex", StringComparison.OrdinalIgnoreCase))
            {
                return mapRandom.Next(1, 4) + mapRandom.Next(1, 4) + mapRandom.Next(1, 4);
            }

            return AcksDomainRules.ClampLandValue(domain == null ? 6 : domain.FixedLandValueGp);
        }

        private void RecalculateDomain(DomainRecord domain, bool shiftCurrent)
        {
            if (domain == null) return;
            RecalculateDomain(domain, shiftCurrent, domain.BaseMorale, domain.CurrentMorale);
        }

        private void RecalculateDomain(DomainRecord domain, bool shiftCurrent, int previousBaseMorale, int previousCurrentMorale)
        {
            if (domain == null) return;

            DomainMoraleSummary morale = AcksDomainRules.CalculateMorale(domain);
            domain.BaseMorale = morale.BaseMorale;
            if (shiftCurrent)
            {
                int delta = domain.BaseMorale - previousBaseMorale;
                domain.CurrentMorale = AcksDomainRules.ClampMorale(previousCurrentMorale + delta);
            }
            else
            {
                domain.CurrentMorale = AcksDomainRules.ClampMorale(domain.CurrentMorale);
            }

            domain.UpdatedAt = DateTime.Now;
        }

        private void NormalizeMapDomains(HexMapRecord map)
        {
            if (map == null) return;
            if (map.Domains == null) map.Domains = new List<DomainRecord>();

            HashSet<string> validCells = new HashSet<string>(map.Cells.Select(c => CellKey(c.Q, c.R)));
            HashSet<string> occupied = new HashSet<string>();

            foreach (DomainRecord domain in map.Domains)
            {
                if (string.IsNullOrWhiteSpace(domain.Id)) domain.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(domain.Name)) domain.Name = isEnglish ? "Unnamed domain" : "\u0411\u0435\u0437\u044b\u043c\u044f\u043d\u043d\u044b\u0439 \u0434\u043e\u043c\u0435\u043d";
                if (string.IsNullOrWhiteSpace(domain.DomainType)) domain.DomainType = "Ordinary";
                domain.Race = NormalizeSettlementRace(domain.Race);
                if (string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase)) domain.Race = "Dwarf";
                if (string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase)) domain.Race = "Elf";
                if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)
                    && (domain.Race == "Dwarf" || domain.Race == "Elf"))
                {
                    domain.Race = "Human";
                }
                if (string.IsNullOrWhiteSpace(domain.Classification)) domain.Classification = "Outlands";
                if (string.IsNullOrWhiteSpace(domain.DomainAlignment)) domain.DomainAlignment = "Neutral";
                if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)) domain.Classification = "Outlands";
                if (string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(domain.Race, "Orc", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(domain.Race, "Beastman", StringComparison.OrdinalIgnoreCase)))
                {
                    domain.DomainAlignment = "Chaotic";
                    if (domain.Ruler != null && domain.Ruler.Snapshot != null) domain.Ruler.Snapshot.Alignment = "Chaotic";
                }
                if (string.IsNullOrWhiteSpace(domain.LandValueMode)) domain.LandValueMode = "Fixed6";
                if (domain.RealmId == null) domain.RealmId = "";
                if (domain.CapitalSettlementId == null) domain.CapitalSettlementId = "";
                if (domain.SettlementIds == null) domain.SettlementIds = new List<string>();
                domain.FixedLandValueGp = AcksDomainRules.ClampLandValue(domain.FixedLandValueGp <= 0 ? 6 : domain.FixedLandValueGp);
                if (domain.Ruler == null) domain.Ruler = new DomainRulerRecord();
                if (domain.Hexes == null) domain.Hexes = new List<DomainHexRecord>();
                NormalizeDomainStronghold(domain, map);

                List<DomainHexRecord> normalizedHexes = new List<DomainHexRecord>();
                foreach (DomainHexRecord hex in domain.Hexes)
                {
                    if (hex == null) continue;
                    string key = CellKey(hex.Q, hex.R);
                    if (!validCells.Contains(key) || occupied.Contains(key)) continue;
                    hex.LandValueGp = AcksDomainRules.ClampLandValue(hex.LandValueGp <= 0 ? domain.FixedLandValueGp : hex.LandValueGp);
                    normalizedHexes.Add(hex);
                    occupied.Add(key);
                }
                domain.Hexes = normalizedHexes;
                NormalizeDomainSettlementLinks(domain, map);
                domain.CurrentMorale = AcksDomainRules.ClampMorale(domain.CurrentMorale);
                domain.BaseMorale = AcksDomainRules.CalculateMorale(domain).BaseMorale;
            }
        }

        private void NormalizeDomainSettlementLinks(DomainRecord domain, HexMapRecord map)
        {
            if (domain == null) return;
            if (domain.SettlementIds == null) domain.SettlementIds = new List<string>();

            HashSet<string> ids = new HashSet<string>(domain.SettlementIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(domain.CapitalSettlementId))
            {
                ids.Add(domain.CapitalSettlementId);
            }

            if (map != null && map.Settlements != null && domain.Hexes != null)
            {
                HashSet<string> domainHexes = new HashSet<string>(domain.Hexes.Select(h => CellKey(h.Q, h.R)));
                foreach (MapSettlementRecord settlement in map.Settlements.Where(s => s != null && domainHexes.Contains(CellKey(s.Q, s.R))))
                {
                    ids.Add(settlement.Id);
                }
            }

            domain.SettlementIds = ids.ToList();
            if (string.IsNullOrWhiteSpace(domain.CapitalSettlementId) && domain.SettlementIds.Count > 0)
            {
                domain.CapitalSettlementId = domain.SettlementIds[0];
            }

            if (map != null && map.Settlements != null && domain.SettlementIds.Count > 1)
            {
                domain.SettlementIds = domain.SettlementIds
                    .OrderBy(id => string.Equals(id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(id =>
                    {
                        MapSettlementRecord settlement = map.Settlements.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
                        return settlement == null ? 99 : settlement.MarketClass;
                    })
                    .ToList();
            }
        }

        private void NormalizeDomainStronghold(DomainRecord domain, HexMapRecord map)
        {
            if (domain == null) return;
            if (IsDomainStrongholdDisabled(domain)) return;

            if (string.IsNullOrWhiteSpace(domain.StrongholdId)) domain.StrongholdId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(domain.StrongholdName))
            {
                domain.StrongholdName = (string.IsNullOrWhiteSpace(domain.Name) ? (isEnglish ? "Stronghold" : "Крепость") : domain.Name)
                    + (isEnglish ? " Stronghold" : ": крепость");
            }

            if (string.IsNullOrWhiteSpace(domain.StrongholdType)) domain.StrongholdType = "Fortress";
            if (domain.StrongholdSettlementId == null) domain.StrongholdSettlementId = "";
            if (!domain.StrongholdSecuresDomain) domain.StrongholdSecuresDomain = true;
            if (!domain.StrongholdActsAsMarketClassVI) domain.StrongholdActsAsMarketClassVI = true;
            domain.StrongholdIconKey = GetDomainStrongholdIconKey(domain);

            bool validHex = map != null
                && map.Cells != null
                && map.Cells.Any(c => c.Q == domain.StrongholdQ && c.R == domain.StrongholdR);
            if (!validHex)
            {
                MapSettlementRecord settlement = map == null || map.Settlements == null
                    ? null
                    : map.Settlements.FirstOrDefault(s => string.Equals(s.Id, domain.CapitalSettlementId, StringComparison.OrdinalIgnoreCase));
                if (settlement != null)
                {
                    domain.StrongholdQ = settlement.Q;
                    domain.StrongholdR = settlement.R;
                    domain.StrongholdInSettlement = true;
                    domain.StrongholdSettlementId = settlement.Id;
                }
                else if (domain.Hexes != null && domain.Hexes.Count > 0)
                {
                    DomainHexRecord first = domain.Hexes[0];
                    domain.StrongholdQ = first.Q;
                    domain.StrongholdR = first.R;
                    domain.StrongholdInSettlement = false;
                    domain.StrongholdSettlementId = "";
                }
            }
        }

        private bool HasVisibleStronghold(DomainRecord domain)
        {
            if (domain == null) return false;
            return domain.StrongholdQ >= 0
                && domain.StrongholdR >= 0
                && (domain.StrongholdSecuresDomain
                    || domain.StrongholdActsAsMarketClassVI
                    || !string.IsNullOrWhiteSpace(domain.StrongholdName)
                    || !string.IsNullOrWhiteSpace(domain.StrongholdId));
        }

        private bool IsDomainStrongholdDisabled(DomainRecord domain)
        {
            if (domain == null) return true;
            return domain.StrongholdQ < 0
                && domain.StrongholdR < 0
                && !domain.StrongholdSecuresDomain
                && !domain.StrongholdActsAsMarketClassVI
                && domain.StrongholdValueGp <= 0
                && string.IsNullOrWhiteSpace(domain.StrongholdName)
                && string.IsNullOrWhiteSpace(domain.StrongholdId);
        }

        private void DrawMapDomains(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null || currentMap.Domains == null) return;

            if (chkMapShowRealms != null && chkMapShowRealms.Checked)
            {
                DrawMapRealms(graphics, visibleWorld);
            }

            if (chkMapShowDomains != null && !chkMapShowDomains.Checked) return;

            foreach (KeyValuePair<DomainRecord, List<HexCellRecord>> pair in GetVisibleDomainCellsByDomain(visibleWorld))
            {
                DomainRecord domain = pair.Key;
                List<HexCellRecord> cells = pair.Value;
                if (domain == null || cells == null || cells.Count == 0) continue;

                bool selected = string.Equals(domain.Id, selectedMapDomainId, StringComparison.OrdinalIgnoreCase);
                Color fill = Color.FromArgb(domain.ColorArgb);
                int alpha = selected ? 138 : 96;
                fill = Color.FromArgb(Math.Min(alpha, fill.A == 0 ? alpha : Math.Max(alpha, fill.A)), fill.R, fill.G, fill.B);
                using (SolidBrush brush = new SolidBrush(fill))
                using (Pen pen = new Pen(selected ? Color.Gold : Color.FromArgb(150, fill.R, fill.G, fill.B), selected ? 3.4f : 1.2f))
                using (Pen innerPen = new Pen(Color.FromArgb(180, Color.White), selected ? 1.1f : 0.1f))
                {
                    foreach (HexCellRecord cell in cells)
                    {
                        PointF center = GetHexCenter(cell.Q, cell.R);
                        PointF[] hex = GetHexPolygon(center);
                        graphics.FillPolygon(brush, hex);
                        graphics.DrawPolygon(pen, hex);
                        if (selected) graphics.DrawPolygon(innerPen, hex);
                    }
                }
            }
        }

        private void DrawMapRealms(Graphics graphics, RectangleF visibleWorld)
        {
            if (currentMap == null) return;

            string selectedTier = SelectedMapRealmTier();
            if (string.Equals(selectedTier, "Barony", StringComparison.OrdinalIgnoreCase))
            {
                DrawMapBaronyRealms(graphics, visibleWorld);
                return;
            }

            if (currentMap.Realms == null || currentMap.Realms.Count == 0) return;
            Dictionary<DomainRecord, List<HexCellRecord>> visibleDomainCells = GetVisibleDomainCellsByDomain(visibleWorld);
            if (visibleDomainCells.Count == 0) return;

            foreach (RealmRecord realm in currentMap.Realms)
            {
                if (realm == null) continue;
                bool selected = string.Equals(realm.Id, selectedMapRealmId, StringComparison.OrdinalIgnoreCase);
                if (!selected
                    && !string.Equals(selectedTier, "All", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(realm.Tier, selectedTier, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<DomainRecord> domains = DomainsForRealmTree(realm);
                if (domains.Count == 0) continue;
                if (!domains.Any(d => d != null && visibleDomainCells.ContainsKey(d))) continue;

                Color color = Color.FromArgb(realm.ColorArgb == 0 ? unchecked((int)0x66547AA5) : realm.ColorArgb);
                int fillAlpha = selected ? 74 : 46;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(fillAlpha, color.R, color.G, color.B)))
                using (Pen pen = new Pen(selected ? Color.Gold : Color.FromArgb(155, color.R, color.G, color.B), selected ? 3.8f : 2.4f))
                {
                    foreach (DomainRecord domain in domains)
                    {
                        List<HexCellRecord> cells;
                        if (domain == null || !visibleDomainCells.TryGetValue(domain, out cells)) continue;
                        foreach (HexCellRecord cell in cells)
                        {
                            PointF center = GetHexCenter(cell.Q, cell.R);
                            PointF[] hex = GetHexPolygon(center);
                            graphics.FillPolygon(brush, hex);
                            graphics.DrawPolygon(pen, hex);
                        }
                    }
                }

            }
        }

        private void DrawMapBaronyRealms(Graphics graphics, RectangleF visibleWorld)
        {
            if (currentMap == null || currentMap.Domains == null) return;
            HashSet<string> selectedRealmIds = RealmAndVassalIds(selectedMapRealmId);

            foreach (KeyValuePair<DomainRecord, List<HexCellRecord>> pair in GetVisibleDomainCellsByDomain(visibleWorld))
            {
                DomainRecord domain = pair.Key;
                List<HexCellRecord> cells = pair.Value;
                if (domain == null || cells == null || cells.Count == 0) continue;
                bool selected = selectedRealmIds.Contains(domain.RealmId);

                Color color = Color.FromArgb(domain.ColorArgb);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(selected ? 72 : 44, color.R, color.G, color.B)))
                using (Pen pen = new Pen(selected ? Color.Gold : Color.FromArgb(165, color.R, color.G, color.B), selected ? 3.4f : 2.2f))
                {
                    foreach (HexCellRecord cell in cells)
                    {
                        PointF center = GetHexCenter(cell.Q, cell.R);
                        PointF[] hex = GetHexPolygon(center);
                        graphics.FillPolygon(brush, hex);
                        graphics.DrawPolygon(pen, hex);
                    }
                }

            }
        }

        private void DrawMapRealmLabels(Graphics graphics, RectangleF visibleWorld)
        {
            if (graphics == null || currentMap == null || chkMapShowRealmLabels == null || !chkMapShowRealmLabels.Checked) return;
            if (chkMapShowRealms != null && !chkMapShowRealms.Checked) return;

            string selectedTier = SelectedMapRealmTier();
            if (string.Equals(selectedTier, "Barony", StringComparison.OrdinalIgnoreCase))
            {
                DrawMapBaronyRealmLabels(graphics, visibleWorld);
                return;
            }

            if (currentMap.Realms == null || currentMap.Realms.Count == 0 || mapZoom < 0.45f) return;
            Dictionary<DomainRecord, List<HexCellRecord>> visibleDomainCells = GetVisibleDomainCellsByDomain(visibleWorld);
            if (visibleDomainCells.Count == 0) return;

            foreach (RealmRecord realm in currentMap.Realms)
            {
                if (realm == null) continue;
                if (!string.Equals(selectedTier, "All", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(realm.Tier, selectedTier, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<DomainRecord> domains = DomainsForRealmTree(realm);
                if (domains.Count == 0) continue;
                if (!domains.Any(d => d != null && visibleDomainCells.ContainsKey(d))) continue;

                PointF labelCenter = RealmLabelCenter(domains);
                if (!visibleWorld.Contains(labelCenter)) continue;

                string label = string.IsNullOrWhiteSpace(realm.Name) ? realm.Tier : realm.Name;
                bool grandTier = realm.Tier == "Empire" || realm.Tier == "Kingdom" || realm.Tier == "Principality";
                string display = grandTier ? AddLetterSpacing(label) : label;
                float fontSize = grandTier ? 14.5f : 11f;
                using (Font font = CreateMapFont(fontSize, grandTier ? FontStyle.Bold : FontStyle.Italic))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.FormatFlags |= StringFormatFlags.NoWrap;
                    RectangleF rect = CenteredSingleLineTextBounds(graphics, display, font, labelCenter, grandTier ? 300f : 220f, 24f, 44f);
                    DrawOutlinedText(graphics, display, font, rect, format, Color.FromArgb(235, 255, 246, 220), Color.FromArgb(170, 40, 30, 20), 2.6f);
                }
            }
        }

        private void DrawMapBaronyRealmLabels(Graphics graphics, RectangleF visibleWorld)
        {
            if (currentMap == null || currentMap.Domains == null || mapZoom < 0.65f) return;

            foreach (DomainRecord domain in GetVisibleDomainCellsByDomain(visibleWorld).Keys)
            {
                if (domain == null || domain.Hexes == null || domain.Hexes.Count == 0) continue;
                PointF labelCenter = RealmLabelCenter(new List<DomainRecord> { domain });
                if (!visibleWorld.Contains(labelCenter)) continue;
                string label = string.IsNullOrWhiteSpace(domain.Name) ? domain.DisplayName : domain.Name;
                using (Font font = CreateMapFont(9.5f, FontStyle.Italic))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.FormatFlags |= StringFormatFlags.NoWrap;
                    RectangleF rect = CenteredSingleLineTextBounds(graphics, label, font, labelCenter, 190f, 18f, 32f);
                    DrawOutlinedText(graphics, label, font, rect, format, Color.FromArgb(235, 255, 246, 220), Color.FromArgb(165, 40, 30, 20), 2.2f);
                }
            }
        }

        private PointF RealmLabelCenter(List<DomainRecord> domains)
        {
            float x = 0f;
            float y = 0f;
            int count = 0;
            foreach (DomainRecord domain in domains)
            {
                if (domain.Hexes == null) continue;
                foreach (DomainHexRecord hex in domain.Hexes)
                {
                    PointF center = GetHexCenter(hex.Q, hex.R);
                    x += center.X;
                    y += center.Y;
                    count++;
                }
            }

            return count == 0 ? PointF.Empty : new PointF(x / count, y / count);
        }

        private void RefreshMapRealmList()
        {
            if (lstMapRealms == null) return;
            string selectedId = selectedMapRealmId;
            bool keepCleared = keepMapRealmSelectionCleared && string.IsNullOrWhiteSpace(selectedId);
            isRefreshingMapRealmList = true;
            try
            {
                lstMapRealms.DataSource = null;
                if (currentMap == null || currentMap.Realms == null)
                {
                    selectedMapRealmId = null;
                    UpdateMapRealmHierarchy();
                    return;
                }

                HashSet<string> vassalIds = new HashSet<string>(
                    (currentMap.VassalLinks ?? new List<VassalLinkRecord>())
                        .Where(v => v != null)
                        .Select(v => v.VassalRealmId)
                        .Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.OrdinalIgnoreCase);
                List<RealmRecord> realms = currentMap.Realms
                    .Where(r => MapRealmMatchesFilters(r, vassalIds))
                    .OrderByDescending(r => MapRealmTierRank(r.Tier))
                    .ThenBy(r => r.Name)
                    .ToList();
                lstMapRealms.DataSource = realms;
                lstMapRealms.DisplayMember = "DisplayName";

                if (!string.IsNullOrWhiteSpace(selectedId))
                {
                    for (int i = 0; i < lstMapRealms.Items.Count; i++)
                    {
                        RealmRecord realm = lstMapRealms.Items[i] as RealmRecord;
                        if (realm != null && string.Equals(realm.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                        {
                            lstMapRealms.SelectedIndex = i;
                            selectedMapRealmId = realm.Id;
                            keepMapRealmSelectionCleared = false;
                            UpdateMapRealmHierarchy();
                            return;
                        }
                    }
                }

                if (keepCleared)
                {
                    lstMapRealms.SelectedIndex = -1;
                    lstMapRealms.ClearSelected();
                    selectedMapRealmId = null;
                    UpdateMapRealmHierarchy();
                    return;
                }

                if (lstMapRealms.Items.Count > 0)
                {
                    lstMapRealms.SelectedIndex = 0;
                    RealmRecord realm = lstMapRealms.SelectedItem as RealmRecord;
                    selectedMapRealmId = realm == null ? null : realm.Id;
                }
                else
                {
                    selectedMapRealmId = null;
                }

                keepMapRealmSelectionCleared = false;
                UpdateMapRealmHierarchy();
            }
            finally
            {
                isRefreshingMapRealmList = false;
            }
        }

        private bool MapRealmMatchesFilters(RealmRecord realm, HashSet<string> vassalIds)
        {
            if (realm == null) return false;

            string search = txtMapRealmSearch == null ? "" : txtMapRealmSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search)
                && !ContainsIgnoreCase(realm.Name, search)
                && !ContainsIgnoreCase(realm.DisplayName, search)
                && !ContainsIgnoreCase(realm.Tier, search)
                && !ContainsIgnoreCase(RealmRaceSummary(realm), search)
                && !ContainsIgnoreCase(realm.RulerName, search))
            {
                return false;
            }

            string tier = SelectedFilterValue(cmbMapRealmTierFilter);
            if (!string.IsNullOrWhiteSpace(tier)
                && !string.Equals(realm.Tier, tier, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string independence = SelectedFilterValue(cmbMapRealmIndependenceFilter);
            bool isVassal = vassalIds != null && vassalIds.Contains(realm.Id);
            if (string.Equals(independence, "Independent", StringComparison.OrdinalIgnoreCase) && isVassal) return false;
            if (string.Equals(independence, "Vassal", StringComparison.OrdinalIgnoreCase) && !isVassal) return false;

            string race = SelectedFilterValue(cmbMapRealmRaceFilter);
            if (!string.IsNullOrWhiteSpace(race) && !RealmHasRace(realm, race)) return false;

            return true;
        }

        private bool RealmHasRace(RealmRecord realm, string race)
        {
            if (realm == null || string.IsNullOrWhiteSpace(race)) return true;
            string normalized = NormalizeRealmRaceFilterKey(race);
            return DomainsForRealmTree(realm)
                .Any(d => string.Equals(RealmDomainRaceKey(d), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private string RealmRaceSummary(RealmRecord realm)
        {
            if (realm == null) return "";
            return string.Join(", ",
                DomainsForRealmTree(realm)
                    .Select(RealmDomainRaceKey)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(LocalizedRealmRaceFilterLabel));
        }

        private string NormalizeRealmRaceFilterKey(string race)
        {
            if (string.Equals(race, "HumanClan", StringComparison.OrdinalIgnoreCase)
                || string.Equals(race, "HumanClanhold", StringComparison.OrdinalIgnoreCase)
                || string.Equals(race, "ClanholdHuman", StringComparison.OrdinalIgnoreCase))
            {
                return "HumanClan";
            }

            return NormalizeSettlementRace(race);
        }

        private string RealmDomainRaceKey(DomainRecord domain)
        {
            if (domain == null) return "";

            string race = NormalizeSettlementRace(domain.Race);
            // Клановые люди в модели остаются людьми по расе; отличает их тип домена.
            if (race == "Human" && string.Equals(domain.DomainType, "Clanhold", StringComparison.OrdinalIgnoreCase))
            {
                return "HumanClan";
            }

            return race;
        }

        private string LocalizedRealmRaceFilterLabel(string raceKey)
        {
            if (string.Equals(raceKey, "HumanClan", StringComparison.OrdinalIgnoreCase))
            {
                return isEnglish ? "Human clanholds" : "Клановые люди";
            }

            return LocalizedSettlementRace(raceKey);
        }

        private RealmRecord GetSelectedMapRealm()
        {
            RealmRecord selected = lstMapRealms == null ? null : lstMapRealms.SelectedItem as RealmRecord;
            if (selected != null) return selected;
            if (currentMap == null || currentMap.Realms == null || string.IsNullOrWhiteSpace(selectedMapRealmId)) return null;
            return currentMap.Realms.FirstOrDefault(r => string.Equals(r.Id, selectedMapRealmId, StringComparison.OrdinalIgnoreCase));
        }

        private void CreateMapRealm()
        {
            if (currentMap == null) return;
            NormalizeMap(currentMap);

            RealmRecord realm = new RealmRecord
            {
                Name = isEnglish ? "New realm" : "Новая держава",
                CultureKey = isEnglish ? "english" : "russian",
                ColorArgb = GetNextDomainColor()
            };

            using (RealmEditorDialog dialog = new RealmEditorDialog(isEnglish, realm, currentMap.Settlements))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                realm = dialog.Realm;
            }

            currentMap.Realms.Add(realm);
            selectedMapRealmId = realm.Id;
            RefreshMapRealmList();
            pnlHexMap.Invalidate();
        }

        private void SelectMapRealmInList(string realmId)
        {
            selectedMapRealmId = realmId;
            keepMapRealmSelectionCleared = string.IsNullOrWhiteSpace(realmId);
            if (lstMapRealms == null)
            {
                UpdateMapRealmHierarchy();
                return;
            }

            if (string.IsNullOrWhiteSpace(realmId))
            {
                lstMapRealms.SelectedIndex = -1;
                lstMapRealms.ClearSelected();
                UpdateMapRealmHierarchy();
                pnlHexMap.Invalidate();
                return;
            }

            keepMapRealmSelectionCleared = false;
            for (int i = 0; i < lstMapRealms.Items.Count; i++)
            {
                RealmRecord realm = lstMapRealms.Items[i] as RealmRecord;
                if (realm == null || !string.Equals(realm.Id, realmId, StringComparison.OrdinalIgnoreCase)) continue;

                if (lstMapRealms.SelectedIndex != i)
                {
                    lstMapRealms.SelectedIndex = i;
                }

                try
                {
                    lstMapRealms.TopIndex = Math.Max(0, Math.Min(i, lstMapRealms.Items.Count - 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                UpdateMapRealmHierarchy();
                pnlHexMap.Invalidate();
                return;
            }

            UpdateMapRealmHierarchy();
            pnlHexMap.Invalidate();
        }

        private void EditSelectedMapRealm()
        {
            RealmRecord selected = GetSelectedMapRealm();
            if (currentMap == null || selected == null) return;

            using (RealmEditorDialog dialog = new RealmEditorDialog(isEnglish, selected, currentMap.Settlements))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                int index = currentMap.Realms.FindIndex(r => r.Id == selected.Id);
                if (index < 0) return;

                RealmRecord edited = dialog.Realm;
                edited.UpdatedAt = DateTime.Now;
                currentMap.Realms[index] = edited;
                selectedMapRealmId = edited.Id;
            }

            RefreshMapRealmList();
            UpdateMapRealmHierarchy();
            pnlHexMap.Invalidate();
        }

        private void DeleteSelectedMapRealm()
        {
            RealmRecord selected = GetSelectedMapRealm();
            if (currentMap == null || selected == null) return;

            DialogResult result = MessageBox.Show(
                isEnglish ? "Delete selected realm? Domains will remain on the map." : "Удалить выбранную державу? Домены останутся на карте.",
                "ACKS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            currentMap.Realms.RemoveAll(r => r.Id == selected.Id);
            foreach (DomainRecord domain in currentMap.Domains.Where(d => d != null && string.Equals(d.RealmId, selected.Id, StringComparison.OrdinalIgnoreCase)))
            {
                domain.RealmId = "";
            }

            currentMap.VassalLinks.RemoveAll(v =>
                string.Equals(v.LiegeRealmId, selected.Id, StringComparison.OrdinalIgnoreCase)
                || string.Equals(v.VassalRealmId, selected.Id, StringComparison.OrdinalIgnoreCase));
            selectedMapRealmId = null;
            RefreshMapRealmList();
            UpdateMapRealmHierarchy();
            pnlHexMap.Invalidate();
        }

        private void CenterMapOnSelectedRealm()
        {
            CenterMapOnRealm(GetSelectedMapRealm());
        }

        private void OpenRealmHierarchyDialog()
        {
            if (currentMap == null)
            {
                MessageBox.Show(
                    isEnglish ? "Create or load a map first." : "Сначала создайте или загрузите карту.",
                    "ACKS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            NormalizeMap(currentMap);
            RealmRecord selected = GetSelectedMapRealm();
            using (RealmHierarchyDialog dialog = new RealmHierarchyDialog(isEnglish, currentMap, selected == null ? null : selected.Id))
            {
                dialog.ShowDialog(this);
            }

            RefreshMapRealmList();
            UpdateMapRealmHierarchy();
            pnlHexMap.Invalidate();
        }

        private void CenterMapOnRealm(RealmRecord realm)
        {
            if (realm == null || currentMap == null || currentMap.Domains == null || pnlHexMap == null) return;

            List<PointF> centers = DomainsForRealmTree(realm)
                .SelectMany(d => d.Hexes ?? new List<DomainHexRecord>())
                .Select(h => GetHexCenter(h.Q, h.R))
                .ToList();
            if (centers.Count == 0) return;

            PointF center = new PointF(centers.Average(p => p.X), centers.Average(p => p.Y));
            int desiredX = (int)Math.Round(center.X * mapZoom - pnlHexMap.ClientSize.Width / 2f);
            int desiredY = (int)Math.Round(center.Y * mapZoom - pnlHexMap.ClientSize.Height / 2f);
            SetMapScroll(new Point(desiredX, desiredY));
            pnlHexMap.Invalidate();
        }

        private List<DomainRecord> DomainsForRealmTree(RealmRecord realm)
        {
            if (realm == null || currentMap == null || currentMap.Domains == null) return new List<DomainRecord>();
            HashSet<string> realmIds = RealmAndVassalIds(realm.Id);
            return currentMap.Domains
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.RealmId) && realmIds.Contains(d.RealmId))
                .ToList();
        }

        private HashSet<string> RealmAndVassalIds(string realmId)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(realmId)) return ids;

            ids.Add(realmId);
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(realmId);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (VassalLinkRecord link in (currentMap == null ? null : currentMap.VassalLinks) ?? new List<VassalLinkRecord>())
                {
                    if (link == null || !string.Equals(link.LiegeRealmId, current, StringComparison.OrdinalIgnoreCase)) continue;
                    if (ids.Add(link.VassalRealmId)) queue.Enqueue(link.VassalRealmId);
                }
            }

            return ids;
        }

        private int MapRealmTierRank(string tier)
        {
            if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private void UpdateMapRealmHierarchy()
        {
            if (txtMapRealmHierarchy == null) return;
            if (currentMap == null || currentMap.Realms == null || currentMap.Realms.Count == 0)
            {
                txtMapRealmHierarchy.Text = isEnglish ? "No realms." : "Держав нет.";
                return;
            }

            RealmRecord selected = GetSelectedMapRealm();
            List<RealmRecord> roots = selected == null
                ? GetRealmHierarchyRoots()
                : new List<RealmRecord> { selected };
            if (roots.Count == 0) roots = currentMap.Realms.OrderBy(r => r.Name).ToList();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(isEnglish ? "Vassal hierarchy:" : "Вассальная иерархия:");
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RealmRecord root in roots)
            {
                AppendRealmVassalTree(builder, root, 0, visited);
            }

            builder.AppendLine();
            builder.AppendLine(isEnglish ? "Territorial hierarchy:" : "Территориальная иерархия:");
            foreach (RealmRecord root in roots)
            {
                AppendRealmTerritory(builder, root, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            txtMapRealmHierarchy.Text = builder.ToString().TrimEnd();
        }

        private List<RealmRecord> GetRealmHierarchyRoots()
        {
            if (currentMap == null || currentMap.Realms == null) return new List<RealmRecord>();
            HashSet<string> vassalIds = new HashSet<string>(
                (currentMap.VassalLinks ?? new List<VassalLinkRecord>()).Select(v => v.VassalRealmId).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            return currentMap.Realms
                .Where(r => r != null && !vassalIds.Contains(r.Id))
                .OrderBy(r => r.Name)
                .ToList();
        }

        private void AppendRealmVassalTree(StringBuilder builder, RealmRecord realm, int depth, HashSet<string> visited)
        {
            if (builder == null || realm == null || !visited.Add(realm.Id)) return;
            builder.Append(new string(' ', depth * 2));
            builder.Append("- ");
            builder.Append(LocalizedRealmDisplayName(realm));
            if (!string.IsNullOrWhiteSpace(realm.RulerName))
            {
                builder.Append(isEnglish ? ", ruler: " : ", правитель: ");
                builder.Append(realm.RulerName);
                if (realm.RulerLevel > 0) builder.Append(" L").Append(realm.RulerLevel);
            }
            builder.AppendLine();

            foreach (RealmRecord vassal in GetRealmVassals(realm))
            {
                AppendRealmVassalTree(builder, vassal, depth + 1, visited);
            }
        }

        private void AppendRealmTerritory(StringBuilder builder, RealmRecord realm, int depth, HashSet<string> visited)
        {
            if (builder == null || realm == null || !visited.Add(realm.Id)) return;
            builder.Append(new string(' ', depth * 2));
            builder.Append("- ");
            builder.Append(LocalizedRealmDisplayName(realm));
            builder.AppendLine();

            foreach (DomainRecord domain in currentMap.Domains
                .Where(d => d != null && string.Equals(d.RealmId, realm.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Name))
            {
                builder.Append(new string(' ', (depth + 1) * 2));
                builder.Append("* ");
                builder.Append(domain.DisplayName);
                builder.Append(isEnglish ? ", ruler: " : ", правитель: ");
                builder.Append(domain.Ruler == null ? (isEnglish ? "none" : "нет") : domain.Ruler.DisplayName);
                builder.AppendLine();
            }

            foreach (RealmRecord vassal in GetRealmVassals(realm))
            {
                AppendRealmTerritory(builder, vassal, depth + 1, visited);
            }
        }

        private IEnumerable<RealmRecord> GetRealmVassals(RealmRecord realm)
        {
            if (realm == null || currentMap == null || currentMap.VassalLinks == null || currentMap.Realms == null)
            {
                return Enumerable.Empty<RealmRecord>();
            }

            HashSet<string> ids = new HashSet<string>(
                currentMap.VassalLinks
                    .Where(v => v != null && string.Equals(v.LiegeRealmId, realm.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.VassalRealmId),
                StringComparer.OrdinalIgnoreCase);
            return currentMap.Realms
                .Where(r => r != null && ids.Contains(r.Id))
                .OrderBy(r => r.Name)
                .ToList();
        }

        private void UpdateMapDomainSummary()
        {
            if (txtMapDomainSummary == null) return;
            DomainRecord domain = GetSelectedMapDomain();
            txtMapDomainSummary.Text = domain == null
                ? (isEnglish ? "No domain selected." : "\u0414\u043e\u043c\u0435\u043d \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d.")
                : AcksDomainRules.BuildSummary(domain);
        }
    }
}
