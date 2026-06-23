using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class RealmHierarchyDialog : Form
    {
        private readonly bool isEnglish;
        private readonly HexMapRecord map;
        private readonly List<VassalLinkRecord> layoutLinks = new List<VassalLinkRecord>();
        private readonly ComboBox cmbMode = new ComboBox();
        private readonly ListBox lstRealms = new ListBox();
        private readonly ComboBox cmbLiege = new ComboBox();
        private readonly ComboBox cmbVassal = new ComboBox();
        private readonly ListBox lstLinks = new ListBox();
        private readonly HierarchyCanvas canvas;
        private readonly Button btnViewTool;
        private readonly Button btnLinkTool;
        private readonly Button btnBreakTool;
        private readonly Button btnShowAll;
        private readonly Button btnMoveToLiege;
        private bool refreshingRealmList;
        private bool showAllRealms;
        private string selectedRealmId;
        private HierarchyTool selectedTool = HierarchyTool.View;

        public RealmHierarchyDialog(bool isEnglish, HexMapRecord map)
            : this(isEnglish, map, null)
        {
        }

        public RealmHierarchyDialog(bool isEnglish, HexMapRecord map, string initialRealmId)
        {
            this.isEnglish = isEnglish;
            this.map = map;
            selectedRealmId = initialRealmId;
            EnsureMapLists();
            foreach (VassalLinkRecord link in this.map.VassalLinks.Where(l => l != null))
            {
                layoutLinks.Add(CloneLink(link));
            }

            canvas = new HierarchyCanvas(isEnglish, this.map, layoutLinks);
            canvas.LinkClicked += link => RemoveLink(link, true);
            canvas.LinkRequested += (liegeId, vassalId) => AddOrReplaceLink(liegeId, vassalId, true);
            canvas.RealmClicked += SelectRealmFromCanvas;

            btnViewTool = CreateToolButton(HierarchyTool.View, L("View", "Обзор"));
            btnLinkTool = CreateToolButton(HierarchyTool.CreateLink, L("Link", "Связь"));
            btnBreakTool = CreateToolButton(HierarchyTool.BreakLink, L("Break", "Разрыв"));
            btnShowAll = new Button { Text = L("Show all", "Показать всё"), Width = 120, Height = 28 };
            UiTheme.StyleCommandButton(btnShowAll, UiTheme.NeutralButtonColor);
            btnShowAll.Click += (s, e) => ShowAllRealms();
            btnMoveToLiege = new Button { Text = L("Go up", "Выше к сеньору"), Width = 188, Height = 28 };
            UiTheme.StyleCommandButton(btnMoveToLiege, UiTheme.NeutralButtonColor);
            btnMoveToLiege.Click += (s, e) => MoveToLiege();

            Text = L("Realm hierarchy", "Иерархия держав");
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            Width = 1220;
            Height = 800;

            BuildUi();
            RefreshData(true);
            SelectHierarchyTool(HierarchyTool.View, false);
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
            RestoreToolButtonPaint();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel top = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = false };
            Label modeLabel = new Label { Text = L("Mode", "Режим"), AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 20, 6, 0) };
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Width = 160;
            cmbMode.Margin = new Padding(0, 16, 12, 0);
            cmbMode.Items.Add(new ModeItem("Rulers", L("Rulers", "Правители")));
            cmbMode.Items.Add(new ModeItem("Domains", L("Domains", "Домены")));
            cmbMode.SelectedIndex = 0;
            cmbMode.SelectedIndexChanged += (s, e) =>
            {
                canvas.Mode = SelectedMode();
                canvas.Invalidate();
            };

            Button zoomOut = new Button { Text = "-", Width = 34, Height = 28, Margin = new Padding(12, 18, 0, 0) };
            Button zoomReset = new Button { Text = "100%", Width = 58, Height = 28, Margin = new Padding(4, 18, 0, 0) };
            Button zoomIn = new Button { Text = "+", Width = 34, Height = 28, Margin = new Padding(4, 18, 0, 0) };
            UiTheme.StyleCommandButton(zoomOut, UiTheme.NeutralButtonColor);
            UiTheme.StyleCommandButton(zoomReset, UiTheme.NeutralButtonColor);
            UiTheme.StyleCommandButton(zoomIn, UiTheme.NeutralButtonColor);
            zoomOut.Click += (s, e) => canvas.ZoomBy(0.85f);
            zoomReset.Click += (s, e) => canvas.ResetView();
            zoomIn.Click += (s, e) => canvas.ZoomBy(1.18f);

            top.Controls.Add(modeLabel);
            top.Controls.Add(cmbMode);
            top.Controls.Add(btnViewTool);
            top.Controls.Add(btnLinkTool);
            top.Controls.Add(btnBreakTool);
            top.Controls.Add(zoomOut);
            top.Controls.Add(zoomReset);
            top.Controls.Add(zoomIn);

            Panel side = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            Label realmLabel = new Label { Text = L("Hierarchy root", "Корень схемы"), AutoSize = true, Font = UiTheme.CreateFont(FontStyle.Bold) };
            Label addLabel = new Label { Text = L("Edit vassal link", "Изменить вассальную связь"), AutoSize = true, Font = UiTheme.CreateFont(FontStyle.Bold) };
            Label liegeLabel = new Label { Text = L("Liege", "Сеньор"), AutoSize = true };
            Label vassalLabel = new Label { Text = L("Vassal", "Вассал"), AutoSize = true };
            cmbLiege.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVassal.DropDownStyle = ComboBoxStyle.DropDownList;
            Button add = new Button { Text = L("Add / replace", "Добавить / заменить"), Width = 150 };
            Button remove = new Button { Text = L("Break selected", "Разорвать выбранную"), Width = 150 };
            Button close = new Button { Text = L("Close", "Закрыть"), Width = 140 };
            UiTheme.StylePositiveButton(add);
            UiTheme.StyleNegativeButton(remove);
            UiTheme.StyleCommandButton(close, UiTheme.NeutralButtonColor);
            add.Click += (s, e) => AddOrReplaceSelectedLink();
            remove.Click += (s, e) => RemoveSelectedLink();
            close.Click += (s, e) => Close();

            lstRealms.BorderStyle = BorderStyle.FixedSingle;
            lstRealms.SelectedIndexChanged += (s, e) => SelectRealmFromList();
            lstLinks.BorderStyle = BorderStyle.FixedSingle;
            lstLinks.SelectedIndexChanged += (s, e) => CenterSelectedLink();

            realmLabel.SetBounds(8, 8, 315, 22);
            lstRealms.SetBounds(8, 34, 315, 162);
            btnMoveToLiege.SetBounds(8, 202, 188, 28);
            btnShowAll.SetBounds(203, 202, 120, 28);
            addLabel.SetBounds(8, 248, 315, 22);
            liegeLabel.SetBounds(8, 282, 315, 20);
            cmbLiege.SetBounds(8, 304, 315, 24);
            vassalLabel.SetBounds(8, 336, 315, 20);
            cmbVassal.SetBounds(8, 358, 315, 24);
            add.SetBounds(8, 394, 150, 28);
            remove.SetBounds(173, 394, 150, 28);
            lstLinks.SetBounds(8, 436, 315, 230);
            close.SetBounds(183, 686, 140, 28);

            side.Controls.Add(realmLabel);
            side.Controls.Add(lstRealms);
            side.Controls.Add(btnMoveToLiege);
            side.Controls.Add(btnShowAll);
            side.Controls.Add(addLabel);
            side.Controls.Add(liegeLabel);
            side.Controls.Add(cmbLiege);
            side.Controls.Add(vassalLabel);
            side.Controls.Add(cmbVassal);
            side.Controls.Add(add);
            side.Controls.Add(remove);
            side.Controls.Add(lstLinks);
            side.Controls.Add(close);

            root.Controls.Add(top, 0, 0);
            root.SetColumnSpan(top, 2);
            root.Controls.Add(canvas, 0, 1);
            root.Controls.Add(side, 1, 1);
            Controls.Add(root);
        }

        private Button CreateToolButton(HierarchyTool tool, string label)
        {
            Button button = new Button();
            button.Size = new Size(62, 56);
            button.Margin = new Padding(3, 2, 3, 2);
            button.Padding = Padding.Empty;
            button.Text = "";
            button.Tag = new ToolButtonSpec(tool, label);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.BackColor = UiTheme.Accent2Color;
            button.Paint += ToolButton_Paint;
            button.Click += (s, e) =>
            {
                HierarchyTool next = selectedTool == tool && tool != HierarchyTool.View
                    ? HierarchyTool.View
                    : tool;
                SelectHierarchyTool(next, true);
            };
            return button;
        }

        private void RestoreToolButtonPaint()
        {
            foreach (Button button in new[] { btnViewTool, btnLinkTool, btnBreakTool })
            {
                if (button == null) continue;
                button.Font = new Font("Microsoft Sans Serif", 6.2f, FontStyle.Bold);
                button.Invalidate();
            }
        }

        private void ToolButton_Paint(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;
            ToolButtonSpec spec = button == null ? null : button.Tag as ToolButtonSpec;
            if (button == null || spec == null) return;

            bool selected = spec.Tool == selectedTool;
            Color back = selected ? ControlPaint.Light(UiTheme.PositiveButtonColor, 0.25f) : UiTheme.Accent2Color;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(back);
            Rectangle iconRect = new Rectangle(13, 6, 36, 28);
            DrawToolIcon(e.Graphics, spec.Tool, iconRect, selected ? Color.Black : Color.FromArgb(28, 28, 28));
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush textBrush = new SolidBrush(Color.Black))
            using (Font textFont = new Font("Microsoft Sans Serif", 6.2f, FontStyle.Bold))
            {
                e.Graphics.DrawString(spec.Label, textFont, textBrush, new RectangleF(2, 36, button.Width - 4, 16), format);
            }

            if (selected)
            {
                using (Pen pen = new Pen(Color.Gold, 2f))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, button.Width - 3, button.Height - 3);
                }
            }

            using (Pen light = new Pen(ControlPaint.Light(back, 0.45f)))
            using (Pen dark = new Pen(ControlPaint.Dark(back, 0.45f)))
            {
                e.Graphics.DrawLine(light, 0, 0, button.Width - 1, 0);
                e.Graphics.DrawLine(light, 0, 0, 0, button.Height - 1);
                e.Graphics.DrawLine(dark, 0, button.Height - 1, button.Width - 1, button.Height - 1);
                e.Graphics.DrawLine(dark, button.Width - 1, 0, button.Width - 1, button.Height - 1);
            }
        }

        private void DrawToolIcon(Graphics g, HierarchyTool tool, Rectangle rect, Color color)
        {
            using (Pen pen = new Pen(color, 2.2f))
            using (SolidBrush brush = new SolidBrush(color))
            {
                if (tool == HierarchyTool.View)
                {
                    g.DrawEllipse(pen, rect.Left + 5, rect.Top + 7, rect.Width - 10, rect.Height - 14);
                    g.FillEllipse(brush, rect.Left + rect.Width / 2 - 4, rect.Top + rect.Height / 2 - 4, 8, 8);
                    return;
                }

                if (tool == HierarchyTool.CreateLink)
                {
                    Rectangle left = new Rectangle(rect.Left + 2, rect.Top + 3, 10, 10);
                    Rectangle right = new Rectangle(rect.Right - 12, rect.Bottom - 13, 10, 10);
                    g.FillEllipse(brush, left);
                    g.FillEllipse(brush, right);
                    pen.CustomEndCap = new AdjustableArrowCap(4, 5);
                    g.DrawLines(pen, new[]
                    {
                        new Point(rect.Left + 12, rect.Top + 8),
                        new Point(rect.Left + 19, rect.Top + 8),
                        new Point(rect.Left + 19, rect.Bottom - 8),
                        new Point(rect.Right - 12, rect.Bottom - 8)
                    });
                    return;
                }

                Rectangle a = new Rectangle(rect.Left + 2, rect.Top + 4, 10, 10);
                Rectangle b = new Rectangle(rect.Right - 12, rect.Bottom - 14, 10, 10);
                g.FillEllipse(brush, a);
                g.FillEllipse(brush, b);
                pen.DashStyle = DashStyle.Dash;
                g.DrawLine(pen, rect.Left + 13, rect.Top + 9, rect.Right - 13, rect.Bottom - 9);
                pen.DashStyle = DashStyle.Solid;
                using (Pen cross = new Pen(Color.DarkRed, 2.3f))
                {
                    g.DrawLine(cross, rect.Left + 15, rect.Top + 6, rect.Right - 15, rect.Bottom - 6);
                    g.DrawLine(cross, rect.Left + 15, rect.Bottom - 6, rect.Right - 15, rect.Top + 6);
                }
            }
        }

        private void EnsureMapLists()
        {
            if (map.Realms == null) map.Realms = new List<RealmRecord>();
            if (map.VassalLinks == null) map.VassalLinks = new List<VassalLinkRecord>();
        }

        private void RefreshData(bool resetLayout)
        {
            EnsureMapLists();

            List<RealmRecord> realms = map.Realms
                .Where(r => r != null)
                .OrderByDescending(r => RealmTierRank(r.Tier))
                .ThenBy(r => r.Name)
                .ToList();

            if (string.IsNullOrWhiteSpace(selectedRealmId) || !realms.Any(r => string.Equals(r.Id, selectedRealmId, StringComparison.OrdinalIgnoreCase)))
            {
                RealmRecord firstRoot = GetRealmHierarchyRoots().FirstOrDefault();
                selectedRealmId = firstRoot == null ? (realms.FirstOrDefault() == null ? null : realms.First().Id) : firstRoot.Id;
            }

            FillRealmList(realms);
            FillRealmCombo(cmbLiege, realms);
            FillRealmCombo(cmbVassal, realms);
            FillLinkList();

            canvas.Mode = SelectedMode();
            canvas.Tool = selectedTool;
            canvas.ShowAllRealms = showAllRealms;
            canvas.FocusRealmId = selectedRealmId;
            canvas.MarkLayoutDirty();
            if (resetLayout)
            {
                if (showAllRealms) canvas.ResetView();
                else canvas.CenterOnRealm(selectedRealmId);
            }
            else
            {
                canvas.Invalidate();
            }
        }

        private void FillRealmList(List<RealmRecord> realms)
        {
            refreshingRealmList = true;
            try
            {
                lstRealms.Items.Clear();
                foreach (RealmRecord realm in realms)
                {
                    lstRealms.Items.Add(new RealmItem(realm, DisplayRealm(realm)));
                }

                SelectRealmInList(selectedRealmId);
            }
            finally
            {
                refreshingRealmList = false;
            }
        }

        private void FillRealmCombo(ComboBox combo, List<RealmRecord> realms)
        {
            string selectedId = SelectedRealm(combo) == null ? "" : SelectedRealm(combo).Id;
            combo.Items.Clear();
            foreach (RealmRecord realm in realms)
            {
                combo.Items.Add(new RealmItem(realm, DisplayRealm(realm)));
            }

            int selectedIndex = -1;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                RealmItem item = combo.Items[i] as RealmItem;
                if (item != null && string.Equals(item.Realm.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex >= 0) combo.SelectedIndex = selectedIndex;
            else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void FillLinkList()
        {
            HashSet<string> visibleIds = GetVisibleRealmIds();
            lstLinks.Items.Clear();
            foreach (VassalLinkRecord link in map.VassalLinks.Where(l => l != null).ToList())
            {
                if (visibleIds.Count > 0
                    && (!visibleIds.Contains(link.LiegeRealmId) || !visibleIds.Contains(link.VassalRealmId)))
                {
                    continue;
                }

                RealmRecord liege = RealmById(link.LiegeRealmId);
                RealmRecord vassal = RealmById(link.VassalRealmId);
                if (liege == null || vassal == null) continue;
                lstLinks.Items.Add(new LinkItem(link, DisplayRealm(liege) + " -> " + DisplayRealm(vassal)));
            }
        }

        private HashSet<string> GetVisibleRealmIds()
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (showAllRealms)
            {
                foreach (RealmRecord realm in map.Realms ?? new List<RealmRecord>())
                {
                    if (realm != null && !string.IsNullOrWhiteSpace(realm.Id)) ids.Add(realm.Id);
                }

                return ids;
            }

            if (string.IsNullOrWhiteSpace(selectedRealmId)) return ids;
            Queue<string> queue = new Queue<string>();
            ids.Add(selectedRealmId);
            queue.Enqueue(selectedRealmId);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (VassalLinkRecord link in layoutLinks.Where(l => l != null && string.Equals(l.LiegeRealmId, current, StringComparison.OrdinalIgnoreCase)))
                {
                    if (ids.Add(link.VassalRealmId)) queue.Enqueue(link.VassalRealmId);
                }
            }

            return ids;
        }

        private void SelectRealmFromList()
        {
            if (refreshingRealmList) return;
            RealmItem item = lstRealms.SelectedItem as RealmItem;
            if (item == null || item.Realm == null) return;

            showAllRealms = false;
            selectedRealmId = item.Realm.Id;
            canvas.ShowAllRealms = false;
            canvas.FocusRealmId = selectedRealmId;
            canvas.MarkLayoutDirty();
            canvas.CenterOnRealm(selectedRealmId);
            FillLinkList();
        }

        private void SelectRealmFromCanvas(string realmId)
        {
            if (string.IsNullOrWhiteSpace(realmId)) return;
            showAllRealms = false;
            selectedRealmId = realmId;
            SelectRealmInList(realmId);
            canvas.ShowAllRealms = false;
            canvas.FocusRealmId = selectedRealmId;
            canvas.MarkLayoutDirty();
            canvas.CenterOnRealm(selectedRealmId);
            FillLinkList();
        }

        private void SelectRealmInList(string realmId)
        {
            if (string.IsNullOrWhiteSpace(realmId)) return;
            for (int i = 0; i < lstRealms.Items.Count; i++)
            {
                RealmItem item = lstRealms.Items[i] as RealmItem;
                if (item != null && string.Equals(item.Realm.Id, realmId, StringComparison.OrdinalIgnoreCase))
                {
                    lstRealms.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ShowAllRealms()
        {
            showAllRealms = true;
            canvas.ShowAllRealms = true;
            canvas.FocusRealmId = selectedRealmId;
            canvas.MarkLayoutDirty();
            canvas.ResetView();
            FillLinkList();
        }

        private void MoveToLiege()
        {
            if (string.IsNullOrWhiteSpace(selectedRealmId)) return;

            VassalLinkRecord link = map.VassalLinks == null
                ? null
                : map.VassalLinks.FirstOrDefault(l => l != null && string.Equals(l.VassalRealmId, selectedRealmId, StringComparison.OrdinalIgnoreCase));
            if (link == null || string.IsNullOrWhiteSpace(link.LiegeRealmId))
            {
                MessageBox.Show(this, L("The selected realm has no liege.", "У выбранной державы нет сеньора."), Text);
                return;
            }

            selectedRealmId = link.LiegeRealmId;
            showAllRealms = false;
            SelectRealmInList(selectedRealmId);
            canvas.ShowAllRealms = false;
            canvas.FocusRealmId = selectedRealmId;
            canvas.MarkLayoutDirty();
            canvas.CenterOnRealm(selectedRealmId);
            FillLinkList();
        }

        private void CenterSelectedLink()
        {
            LinkItem item = lstLinks.SelectedItem as LinkItem;
            if (item == null || item.Link == null) return;
            canvas.CenterOnLink(item.Link);
        }

        private void SelectHierarchyTool(HierarchyTool tool, bool fromUser)
        {
            selectedTool = tool;
            canvas.Tool = selectedTool;
            btnViewTool.Invalidate();
            btnLinkTool.Invalidate();
            btnBreakTool.Invalidate();
            canvas.Invalidate();
        }

        private void AddOrReplaceSelectedLink()
        {
            RealmRecord liege = SelectedRealm(cmbLiege);
            RealmRecord vassal = SelectedRealm(cmbVassal);
            if (liege == null || vassal == null) return;
            AddOrReplaceLink(liege.Id, vassal.Id, true);
        }

        private void AddOrReplaceLink(string liegeId, string vassalId, bool updateLayout)
        {
            RealmRecord liege = RealmById(liegeId);
            RealmRecord vassal = RealmById(vassalId);
            if (liege == null || vassal == null) return;
            if (string.Equals(liege.Id, vassal.Id, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, L("A realm cannot be its own liege.", "Держава не может быть собственным сеньором."), Text);
                return;
            }

            if (WouldCreateCycle(liege.Id, vassal.Id))
            {
                MessageBox.Show(this, L("This link would create a cycle.", "Такая связь создаст цикл подчинения."), Text);
                return;
            }

            // У вассала может быть только один сеньор. Новая связь заменяет старую
            // и в сохраненной модели, и в рабочей раскладке текущего окна.
            map.VassalLinks.RemoveAll(l => l != null && string.Equals(l.VassalRealmId, vassal.Id, StringComparison.OrdinalIgnoreCase));
            layoutLinks.RemoveAll(l => l != null && string.Equals(l.VassalRealmId, vassal.Id, StringComparison.OrdinalIgnoreCase));

            VassalLinkRecord link = new VassalLinkRecord
            {
                LiegeRealmId = liege.Id,
                VassalRealmId = vassal.Id,
                RelationType = "Vassal",
                Notes = "Edited in hierarchy view."
            };
            map.VassalLinks.Add(link);
            if (updateLayout) layoutLinks.Add(CloneLink(link));

            if (!showAllRealms && !IsRealmVisibleFromSelected(liege.Id))
            {
                selectedRealmId = liege.Id;
            }

            RefreshData(false);
            canvas.CenterOnRealm(liege.Id);
        }

        private bool IsRealmVisibleFromSelected(string realmId)
        {
            return GetVisibleRealmIds().Contains(realmId);
        }

        private void RemoveSelectedLink()
        {
            LinkItem item = lstLinks.SelectedItem as LinkItem;
            if (item == null || item.Link == null) return;
            RemoveLink(item.Link, true);
        }

        private void RemoveLink(VassalLinkRecord link, bool keepLayout)
        {
            if (link == null) return;

            // Связь удаляется из модели сразу, но при keepLayout узел остается в рабочей
            // раскладке окна до закрытия, чтобы пользователь видел, что именно отвязал.
            map.VassalLinks.RemoveAll(l => l != null && string.Equals(l.Id, link.Id, StringComparison.OrdinalIgnoreCase));
            if (!keepLayout)
            {
                layoutLinks.RemoveAll(l => l != null && string.Equals(l.Id, link.Id, StringComparison.OrdinalIgnoreCase));
            }

            RefreshData(false);
            canvas.CenterOnRealm(link.VassalRealmId);
        }

        private bool WouldCreateCycle(string newLiegeId, string newVassalId)
        {
            // Проверяем цепочку сеньоров вверх, игнорируя старую связь нового вассала:
            // она будет заменена, поэтому не должна давать ложный цикл.
            string cursor = newLiegeId;
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!string.IsNullOrWhiteSpace(cursor) && visited.Add(cursor))
            {
                if (string.Equals(cursor, newVassalId, StringComparison.OrdinalIgnoreCase)) return true;
                VassalLinkRecord parent = map.VassalLinks.FirstOrDefault(l =>
                    l != null
                    && !string.Equals(l.VassalRealmId, newVassalId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(l.VassalRealmId, cursor, StringComparison.OrdinalIgnoreCase));
                cursor = parent == null ? null : parent.LiegeRealmId;
            }

            return false;
        }

        private List<RealmRecord> GetRealmHierarchyRoots()
        {
            if (map == null || map.Realms == null) return new List<RealmRecord>();
            HashSet<string> vassalIds = new HashSet<string>(
                layoutLinks.Where(v => v != null).Select(v => v.VassalRealmId).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            return map.Realms
                .Where(r => r != null && !vassalIds.Contains(r.Id))
                .OrderByDescending(r => RealmTierRank(r.Tier))
                .ThenBy(r => r.Name)
                .ToList();
        }

        private RealmRecord RealmById(string id)
        {
            return map.Realms == null ? null : map.Realms.FirstOrDefault(r => r != null && string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private RealmRecord SelectedRealm(ComboBox combo)
        {
            RealmItem item = combo.SelectedItem as RealmItem;
            return item == null ? null : item.Realm;
        }

        private string SelectedMode()
        {
            ModeItem item = cmbMode.SelectedItem as ModeItem;
            return item == null ? "Rulers" : item.Value;
        }

        private string DisplayRealm(RealmRecord realm)
        {
            if (realm == null) return L("None", "Нет");
            return (string.IsNullOrWhiteSpace(realm.Name) ? L("(unnamed)", "(без названия)") : realm.Name)
                + " [" + LocalizedTier(realm.Tier) + "]";
        }

        private string LocalizedTier(string tier)
        {
            if (isEnglish) return string.IsNullOrWhiteSpace(tier) ? "Realm" : tier;
            if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return "Империя";
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return "Королевство";
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return "Княжество";
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return "Герцогство";
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return "Графство";
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return "Виконтство";
            if (string.Equals(tier, "Barony", StringComparison.OrdinalIgnoreCase)) return "Баронство";
            return "Держава";
        }

        private string L(string english, string russian)
        {
            return isEnglish ? english : russian;
        }

        private static VassalLinkRecord CloneLink(VassalLinkRecord source)
        {
            return new VassalLinkRecord
            {
                Id = source.Id,
                LiegeRealmId = source.LiegeRealmId,
                VassalRealmId = source.VassalRealmId,
                RelationType = source.RelationType,
                Loyalty = source.Loyalty,
                TributeGp = source.TributeGp,
                Notes = source.Notes
            };
        }

        private static int RealmTierRank(string tier)
        {
            if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private enum HierarchyTool
        {
            View,
            CreateLink,
            BreakLink
        }

        private sealed class RealmItem
        {
            public RealmRecord Realm { get; private set; }
            private readonly string label;

            public RealmItem(RealmRecord realm, string label)
            {
                Realm = realm;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }

        private sealed class LinkItem
        {
            public VassalLinkRecord Link { get; private set; }
            private readonly string label;

            public LinkItem(VassalLinkRecord link, string label)
            {
                Link = link;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }

        private sealed class ModeItem
        {
            public string Value { get; private set; }
            private readonly string label;

            public ModeItem(string value, string label)
            {
                Value = value;
                this.label = label;
            }

            public override string ToString()
            {
                return label;
            }
        }

        private sealed class ToolButtonSpec
        {
            public HierarchyTool Tool { get; private set; }
            public string Label { get; private set; }

            public ToolButtonSpec(HierarchyTool tool, string label)
            {
                Tool = tool;
                Label = label;
            }
        }

        private sealed class HierarchyCanvas : Panel
        {
            private const float NodeWidth = 210f;
            private const float NodeHeight = 64f;
            private const float HorizontalStep = 250f;
            private const float VerticalStep = 122f;
            private const float ViewMargin = 56f;
            private readonly bool isEnglish;
            private readonly HexMapRecord map;
            private readonly List<VassalLinkRecord> layoutLinks;
            private readonly Dictionary<string, RectangleF> nodeBounds = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);
            private readonly List<LinkHitBox> linkHitBoxes = new List<LinkHitBox>();
            private RectangleF contentBounds = new RectangleF(0, 0, 1, 1);
            private bool layoutDirty = true;
            private float zoom = 1f;
            private PointF pan = new PointF(24, 24);
            private bool panning;
            private Point lastMouse;
            private string dragSourceRealmId;
            private PointF dragWorldPoint;
            private string hoveredLinkId;

            public event Action<VassalLinkRecord> LinkClicked;
            public event Action<string, string> LinkRequested;
            public event Action<string> RealmClicked;
            public string Mode { get; set; }
            public string FocusRealmId { get; set; }
            public bool ShowAllRealms { get; set; }
            public HierarchyTool Tool { get; set; }

            public HierarchyCanvas(bool isEnglish, HexMapRecord map, List<VassalLinkRecord> layoutLinks)
            {
                this.isEnglish = isEnglish;
                this.map = map;
                this.layoutLinks = layoutLinks;
                Mode = "Rulers";
                Tool = HierarchyTool.View;
                Dock = DockStyle.Fill;
                BackColor = Color.White;
                DoubleBuffered = true;
                TabStop = true;
                SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            }

            public void MarkLayoutDirty()
            {
                layoutDirty = true;
            }

            public void ZoomBy(float factor)
            {
                Point center = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
                ZoomAt(factor, center);
            }

            public void ResetView()
            {
                zoom = 1f;
                EnsureLayout();
                CenterOnContent();
                Invalidate();
            }

            public void CenterOnRealm(string realmId)
            {
                EnsureLayout();
                RectangleF rect;
                if (!string.IsNullOrWhiteSpace(realmId) && nodeBounds.TryGetValue(realmId, out rect))
                {
                    CenterOnWorldPoint(new PointF(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f));
                    return;
                }

                CenterOnContent();
            }

            public void CenterOnLink(VassalLinkRecord link)
            {
                EnsureLayout();
                if (link == null)
                {
                    CenterOnContent();
                    return;
                }

                RectangleF a;
                RectangleF b;
                if (nodeBounds.TryGetValue(link.LiegeRealmId, out a) && nodeBounds.TryGetValue(link.VassalRealmId, out b))
                {
                    CenterOnWorldPoint(new PointF((a.Left + a.Right + b.Left + b.Right) / 4f, (a.Top + a.Bottom + b.Top + b.Bottom) / 4f));
                    return;
                }

                CenterOnRealm(link.LiegeRealmId);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                ZoomAt(e.Delta > 0 ? 1.12f : 0.89f, e.Location);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                lastMouse = e.Location;

                if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
                {
                    panning = true;
                    Cursor = Cursors.SizeAll;
                    return;
                }

                if (e.Button != MouseButtons.Left) return;
                PointF world = ScreenToWorld(e.Location);
                if (Tool == HierarchyTool.BreakLink)
                {
                    VassalLinkRecord hitLink = HitTestLink(world);
                    if (hitLink != null && LinkClicked != null) LinkClicked(hitLink);
                    return;
                }

                if (Tool == HierarchyTool.CreateLink)
                {
                    string outputNode = HitTestNodeOutput(world);
                    if (!string.IsNullOrWhiteSpace(outputNode))
                    {
                        dragSourceRealmId = outputNode;
                        dragWorldPoint = world;
                        Cursor = Cursors.Cross;
                    }
                    return;
                }

                string node = HitTestNode(world);
                if (!string.IsNullOrWhiteSpace(node) && RealmClicked != null)
                {
                    RealmClicked(node);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (panning)
                {
                    pan.X += e.X - lastMouse.X;
                    pan.Y += e.Y - lastMouse.Y;
                    lastMouse = e.Location;
                    ClampPan();
                    Invalidate();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(dragSourceRealmId))
                {
                    dragWorldPoint = ScreenToWorld(e.Location);
                    Invalidate();
                    return;
                }

                PointF world = ScreenToWorld(e.Location);
                VassalLinkRecord hovered = Tool == HierarchyTool.BreakLink ? HitTestLink(world) : null;
                string hoveredId = hovered == null ? null : hovered.Id;
                if (!string.Equals(hoveredId, hoveredLinkId, StringComparison.OrdinalIgnoreCase))
                {
                    hoveredLinkId = hoveredId;
                    Invalidate();
                }

                if (Tool == HierarchyTool.BreakLink) Cursor = hovered == null ? Cursors.Default : Cursors.Hand;
                else if (Tool == HierarchyTool.CreateLink) Cursor = string.IsNullOrWhiteSpace(HitTestNodeOutput(world)) ? Cursors.Default : Cursors.Cross;
                else Cursor = string.IsNullOrWhiteSpace(HitTestNode(world)) ? Cursors.Default : Cursors.Hand;
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (panning)
                {
                    panning = false;
                    Cursor = Cursors.Default;
                    return;
                }

                if (string.IsNullOrWhiteSpace(dragSourceRealmId)) return;
                string source = dragSourceRealmId;
                dragSourceRealmId = null;
                Cursor = Cursors.Default;

                string target = HitTestNodeInput(ScreenToWorld(e.Location));
                if (!string.IsNullOrWhiteSpace(target) && !string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                {
                    if (LinkRequested != null) LinkRequested(source, target);
                }

                Invalidate();
            }

            protected override void OnResize(EventArgs eventargs)
            {
                base.OnResize(eventargs);
                ClampPan();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                EnsureLayout();

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                using (Matrix transform = new Matrix())
                {
                    transform.Translate(pan.X, pan.Y);
                    transform.Scale(zoom, zoom);
                    g.Transform = transform;
                    DrawLinks(g);
                    DrawNodes(g);
                    DrawDraggedLink(g);
                }
            }

            private void EnsureLayout()
            {
                if (!layoutDirty) return;
                BuildLayout();
                ClampPan();
            }

            private void ZoomAt(float factor, Point screenPoint)
            {
                EnsureLayout();
                PointF before = ScreenToWorld(screenPoint);
                zoom = Clamp(zoom * factor, 0.35f, 2.5f);
                pan.X = screenPoint.X - before.X * zoom;
                pan.Y = screenPoint.Y - before.Y * zoom;
                ClampPan();
                Invalidate();
            }

            private PointF ScreenToWorld(Point point)
            {
                return new PointF((point.X - pan.X) / zoom, (point.Y - pan.Y) / zoom);
            }

            private void CenterOnWorldPoint(PointF world)
            {
                pan.X = ClientSize.Width / 2f - world.X * zoom;
                pan.Y = ClientSize.Height / 2f - world.Y * zoom;
                ClampPan();
                Invalidate();
            }

            private void CenterOnContent()
            {
                CenterOnWorldPoint(new PointF(contentBounds.Left + contentBounds.Width / 2f, contentBounds.Top + contentBounds.Height / 2f));
            }

            private void ClampPan()
            {
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

                float contentWidth = contentBounds.Width * zoom;
                float contentHeight = contentBounds.Height * zoom;
                if (contentWidth <= ClientSize.Width - ViewMargin * 2f)
                {
                    pan.X = ClientSize.Width / 2f - (contentBounds.Left + contentBounds.Width / 2f) * zoom;
                }
                else
                {
                    float minX = ClientSize.Width - ViewMargin - contentBounds.Right * zoom;
                    float maxX = ViewMargin - contentBounds.Left * zoom;
                    pan.X = Clamp(pan.X, minX, maxX);
                }

                if (contentHeight <= ClientSize.Height - ViewMargin * 2f)
                {
                    pan.Y = ClientSize.Height / 2f - (contentBounds.Top + contentBounds.Height / 2f) * zoom;
                }
                else
                {
                    float minY = ClientSize.Height - ViewMargin - contentBounds.Bottom * zoom;
                    float maxY = ViewMargin - contentBounds.Top * zoom;
                    pan.Y = Clamp(pan.Y, minY, maxY);
                }
            }

            private void BuildLayout()
            {
                nodeBounds.Clear();
                linkHitBoxes.Clear();
                if (map.Realms == null || map.Realms.Count == 0)
                {
                    contentBounds = new RectangleF(0, 0, 1, 1);
                    layoutDirty = false;
                    return;
                }

                Dictionary<string, RealmRecord> realms = map.Realms
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id))
                    .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

                List<RealmRecord> roots = ResolveLayoutRoots(realms);
                float nextLeaf = 0;
                HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (RealmRecord root in roots)
                {
                    LayoutRealm(root, realms, visited, ref nextLeaf);
                    nextLeaf += 0.65f;
                }

                if (ShowAllRealms)
                {
                    foreach (RealmRecord realm in realms.Values.Where(r => !nodeBounds.ContainsKey(r.Id)).OrderBy(r => r.Name))
                    {
                        PlaceNode(realm, nextLeaf * HorizontalStep, RankToY(realm));
                        nextLeaf += 1.15f;
                    }
                }

                CalculateContentBounds();
                layoutDirty = false;
            }

            private List<RealmRecord> ResolveLayoutRoots(Dictionary<string, RealmRecord> realms)
            {
                if (!ShowAllRealms && !string.IsNullOrWhiteSpace(FocusRealmId) && realms.ContainsKey(FocusRealmId))
                {
                    return new List<RealmRecord> { realms[FocusRealmId] };
                }

                HashSet<string> childIds = new HashSet<string>(
                    layoutLinks.Where(l => l != null).Select(l => l.VassalRealmId).Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.OrdinalIgnoreCase);
                List<RealmRecord> roots = realms.Values
                    .Where(r => !childIds.Contains(r.Id))
                    .OrderByDescending(r => RealmTierRank(r.Tier))
                    .ThenBy(r => r.Name)
                    .ToList();
                if (roots.Count == 0)
                {
                    roots = realms.Values.OrderByDescending(r => RealmTierRank(r.Tier)).ThenBy(r => r.Name).ToList();
                }

                return roots;
            }

            private float LayoutRealm(RealmRecord realm, Dictionary<string, RealmRecord> realms, HashSet<string> visited, ref float nextLeaf)
            {
                if (realm == null || !visited.Add(realm.Id)) return nextLeaf * HorizontalStep;

                List<RealmRecord> children = LayoutChildren(realm, realms).ToList();
                float x;
                if (children.Count == 0)
                {
                    x = nextLeaf * HorizontalStep;
                    nextLeaf += 1.0f;
                }
                else
                {
                    List<float> childCenters = new List<float>();
                    foreach (RealmRecord child in children)
                    {
                        childCenters.Add(LayoutRealm(child, realms, visited, ref nextLeaf));
                    }

                    x = childCenters.Count == 0 ? nextLeaf * HorizontalStep : childCenters.Average();
                }

                PlaceNode(realm, x, RankToY(realm));
                return x;
            }

            private IEnumerable<RealmRecord> LayoutChildren(RealmRecord realm, Dictionary<string, RealmRecord> realms)
            {
                List<string> ids = layoutLinks
                    .Where(l => l != null && string.Equals(l.LiegeRealmId, realm.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.VassalRealmId)
                    .ToList();

                return ids
                    .Select(id => realms.ContainsKey(id) ? realms[id] : null)
                    .Where(r => r != null)
                    .OrderByDescending(r => RealmTierRank(r.Tier))
                    .ThenBy(r => r.Name);
            }

            private void PlaceNode(RealmRecord realm, float centerX, float y)
            {
                nodeBounds[realm.Id] = new RectangleF(centerX - NodeWidth / 2f, y, NodeWidth, NodeHeight);
            }

            private float RankToY(RealmRecord realm)
            {
                int rank = RealmTierRank(realm == null ? "" : realm.Tier);
                return (6 - rank) * VerticalStep + 26f;
            }

            private void CalculateContentBounds()
            {
                if (nodeBounds.Count == 0)
                {
                    contentBounds = new RectangleF(0, 0, 1, 1);
                    return;
                }

                float left = nodeBounds.Values.Min(r => r.Left) - 80f;
                float top = nodeBounds.Values.Min(r => r.Top) - 80f;
                float right = nodeBounds.Values.Max(r => r.Right) + 80f;
                float bottom = nodeBounds.Values.Max(r => r.Bottom) + 80f;
                contentBounds = RectangleF.FromLTRB(left, top, right, bottom);
            }

            private void DrawLinks(Graphics g)
            {
                linkHitBoxes.Clear();
                foreach (VassalLinkRecord link in map.VassalLinks ?? new List<VassalLinkRecord>())
                {
                    List<PointF> points = LinkPath(link);
                    if (points.Count < 2) continue;

                    bool hovered = Tool == HierarchyTool.BreakLink && string.Equals(link.Id, hoveredLinkId, StringComparison.OrdinalIgnoreCase);
                    using (Pen pen = new Pen(hovered ? Color.DarkRed : Color.FromArgb(80, 145, 210), hovered ? 3.1f : 2.2f))
                    {
                        if (Tool == HierarchyTool.BreakLink) pen.DashStyle = hovered ? DashStyle.Solid : DashStyle.Dot;
                        pen.CustomEndCap = new AdjustableArrowCap(5, 6);
                        g.DrawLines(pen, points.ToArray());
                    }

                    linkHitBoxes.Add(new LinkHitBox(link, points));
                }
            }

            private List<PointF> LinkPath(VassalLinkRecord link)
            {
                RectangleF liege;
                RectangleF vassal;
                if (link == null
                    || !nodeBounds.TryGetValue(link.LiegeRealmId, out liege)
                    || !nodeBounds.TryGetValue(link.VassalRealmId, out vassal))
                {
                    return new List<PointF>();
                }

                PointF start = new PointF(liege.Left + liege.Width / 2f, liege.Bottom);
                PointF end = new PointF(vassal.Left + vassal.Width / 2f, vassal.Top);
                float midY = start.Y + Math.Max(26f, (end.Y - start.Y) / 2f);
                if (end.Y <= start.Y)
                {
                    midY = Math.Max(start.Y, end.Y) + 44f;
                }

                return new List<PointF>
                {
                    start,
                    new PointF(start.X, midY),
                    new PointF(end.X, midY),
                    end
                };
            }

            private void DrawNodes(Graphics g)
            {
                Dictionary<string, RealmRecord> realms = map.Realms == null
                    ? new Dictionary<string, RealmRecord>(StringComparer.OrdinalIgnoreCase)
                    : map.Realms.Where(r => r != null && !string.IsNullOrWhiteSpace(r.Id)).ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, RectangleF> pair in nodeBounds)
                {
                    RealmRecord realm;
                    if (!realms.TryGetValue(pair.Key, out realm)) continue;
                    RectangleF rect = pair.Value;
                    bool focused = !ShowAllRealms && string.Equals(pair.Key, FocusRealmId, StringComparison.OrdinalIgnoreCase);

                    using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.White, Color.FromArgb(245, 248, 252), LinearGradientMode.Vertical))
                    using (Pen pen = new Pen(focused ? Color.Goldenrod : Color.FromArgb(145, 172, 205), focused ? 2.2f : 1.2f))
                    using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (Font titleFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                    {
                        g.FillRectangle(brush, rect);
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                        g.DrawString(NodeText(realm), titleFont, Brushes.Black, rect, format);
                    }

                    if (Tool == HierarchyTool.CreateLink)
                    {
                        DrawAnchor(g, new PointF(rect.Left + rect.Width / 2f, rect.Top), Color.FromArgb(75, 125, 190));
                        DrawAnchor(g, new PointF(rect.Left + rect.Width / 2f, rect.Bottom), Color.FromArgb(75, 125, 190));
                    }
                }
            }

            private void DrawAnchor(Graphics g, PointF point, Color color)
            {
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (Pen pen = new Pen(color, 1.6f))
                {
                    g.FillEllipse(brush, point.X - 5f, point.Y - 5f, 10f, 10f);
                    g.DrawEllipse(pen, point.X - 5f, point.Y - 5f, 10f, 10f);
                }
            }

            private void DrawDraggedLink(Graphics g)
            {
                if (string.IsNullOrWhiteSpace(dragSourceRealmId)) return;
                RectangleF source;
                if (!nodeBounds.TryGetValue(dragSourceRealmId, out source)) return;

                PointF start = new PointF(source.Left + source.Width / 2f, source.Bottom);
                float midY = start.Y + 38f;
                using (Pen pen = new Pen(Color.FromArgb(150, 80, 145, 210), 2f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    pen.CustomEndCap = new AdjustableArrowCap(5, 6);
                    g.DrawLines(pen, new[]
                    {
                        start,
                        new PointF(start.X, midY),
                        new PointF(dragWorldPoint.X, midY),
                        dragWorldPoint
                    });
                }
            }

            private string NodeText(RealmRecord realm)
            {
                if (Mode == "Domains")
                {
                    int domains = CountDescendantDomains(realm);
                    return ShortName(realm.Name) + "\n" + LocalizedTier(realm) + ", " + domains + (isEnglish ? " dom." : " дом.");
                }

                string ruler = string.IsNullOrWhiteSpace(realm.RulerName) ? (isEnglish ? "No ruler" : "Нет правителя") : realm.RulerName;
                string title = LocalizedTitle(realm, IsFemaleRealmRuler(realm));
                return title + " " + ShortName(ruler) + (realm.RulerLevel > 0 ? "\nL" + realm.RulerLevel : "");
            }

            private bool IsFemaleRealmRuler(RealmRecord realm)
            {
                if (realm == null || string.IsNullOrWhiteSpace(realm.RulerName) || map.Domains == null) return false;

                foreach (DomainRecord domain in map.Domains)
                {
                    CharacterRecord ruler = domain == null || domain.Ruler == null ? null : domain.Ruler.Snapshot;
                    if (ruler == null) continue;
                    if (string.Equals(ruler.Name, realm.RulerName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(ruler.Sex, "Female", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            private int CountDescendantDomains(RealmRecord realm)
            {
                if (realm == null || map.Domains == null) return 0;
                HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Queue<string> queue = new Queue<string>();
                ids.Add(realm.Id);
                queue.Enqueue(realm.Id);
                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();
                    foreach (VassalLinkRecord link in layoutLinks.Where(l => l != null && string.Equals(l.LiegeRealmId, current, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (ids.Add(link.VassalRealmId)) queue.Enqueue(link.VassalRealmId);
                    }
                }

                return map.Domains.Count(d => d != null && ids.Contains(d.RealmId));
            }

            private VassalLinkRecord HitTestLink(PointF world)
            {
                foreach (LinkHitBox hitBox in linkHitBoxes)
                {
                    if (hitBox.Hit(world, 8f / Math.Max(0.35f, zoom))) return hitBox.Link;
                }

                return null;
            }

            private string HitTestNode(PointF world)
            {
                foreach (KeyValuePair<string, RectangleF> pair in nodeBounds)
                {
                    if (pair.Value.Contains(world)) return pair.Key;
                }

                return null;
            }

            private string HitTestNodeOutput(PointF world)
            {
                foreach (KeyValuePair<string, RectangleF> pair in nodeBounds)
                {
                    PointF output = new PointF(pair.Value.Left + pair.Value.Width / 2f, pair.Value.Bottom);
                    if (Distance(world, output) <= 14f || pair.Value.Contains(world)) return pair.Key;
                }

                return null;
            }

            private string HitTestNodeInput(PointF world)
            {
                foreach (KeyValuePair<string, RectangleF> pair in nodeBounds)
                {
                    PointF input = new PointF(pair.Value.Left + pair.Value.Width / 2f, pair.Value.Top);
                    if (Distance(world, input) <= 18f || pair.Value.Contains(world)) return pair.Key;
                }

                return null;
            }

            private string ShortName(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return isEnglish ? "Realm" : "Держава";
                return name.Length <= 23 ? name : name.Substring(0, 22) + "...";
            }

            private string LocalizedTier(RealmRecord realm)
            {
                if (realm == null) return isEnglish ? "Realm" : "Держава";
                return RealmTitleCatalog.RealmTitle(realm.CultureKey, realm.Tier, !isEnglish, realm.TitleOverride);
            }

            private string LocalizedTitle(RealmRecord realm, bool female)
            {
                if (realm == null) return female ? (isEnglish ? "Lady" : "Правительница") : (isEnglish ? "Lord" : "Правитель");
                return RealmTitleCatalog.RulerTitle(
                    realm.CultureKey,
                    realm.Tier,
                    female,
                    !isEnglish,
                    realm.TitleOverride,
                    realm.FemaleTitleOverride);
            }

            private static float Distance(PointF a, PointF b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            private static float DistanceToSegment(PointF p, PointF a, PointF b)
            {
                float dx = b.X - a.X;
                float dy = b.Y - a.Y;
                if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f) return Distance(p, a);
                float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
                t = Clamp(t, 0f, 1f);
                return Distance(p, new PointF(a.X + t * dx, a.Y + t * dy));
            }

            private static float Clamp(float value, float min, float max)
            {
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }

            private static int RealmTierRank(string tier)
            {
                if (string.Equals(tier, "Empire", StringComparison.OrdinalIgnoreCase)) return 6;
                if (string.Equals(tier, "Kingdom", StringComparison.OrdinalIgnoreCase)) return 5;
                if (string.Equals(tier, "Principality", StringComparison.OrdinalIgnoreCase)) return 4;
                if (string.Equals(tier, "Duchy", StringComparison.OrdinalIgnoreCase)) return 3;
                if (string.Equals(tier, "County", StringComparison.OrdinalIgnoreCase)) return 2;
                if (string.Equals(tier, "Viscounty", StringComparison.OrdinalIgnoreCase)) return 1;
                return 0;
            }

            private sealed class LinkHitBox
            {
                public VassalLinkRecord Link { get; private set; }
                private readonly List<PointF> points;

                public LinkHitBox(VassalLinkRecord link, List<PointF> points)
                {
                    Link = link;
                    this.points = points;
                }

                public bool Hit(PointF point, float tolerance)
                {
                    for (int i = 1; i < points.Count; i++)
                    {
                        if (DistanceToSegment(point, points[i - 1], points[i]) <= tolerance) return true;
                    }

                    return false;
                }
            }
        }
    }
}
