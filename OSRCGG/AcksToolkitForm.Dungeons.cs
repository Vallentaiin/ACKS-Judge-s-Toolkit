using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private readonly string dungeonLibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OSRCGG",
            "dungeons.xml");

        private TabPage tabPageDungeons;
        private ListBox lstDungeons;
        private TextBox txtDungeonName;
        private ComboBox cmbDungeonType;
        private ComboBox cmbDungeonSize;
        private NumericUpDown nudDungeonLevel;
        private NumericUpDown nudDungeonSeed;
        private ComboBox cmbDungeonLevelView;
        private DoubleBufferedPanel pnlDungeonMap;
        private ListBox lstDungeonRooms;
        private ComboBox cmbDungeonRoomKind;
        private ComboBox cmbDungeonRoomShape;
        private ComboBox cmbDungeonPassageKind;
        private ComboBox cmbDungeonDoorKind;
        private NumericUpDown nudDungeonPassageWidth;
        private NumericUpDown nudDungeonRoomWidth;
        private NumericUpDown nudDungeonRoomHeight;
        private TextBox txtDungeonRoomTitle;
        private TextBox txtDungeonRoomMonster;
        private TextBox txtDungeonRoomTreasure;
        private TextBox txtDungeonRoomTrap;
        private TextBox txtDungeonRoomDetails;
        private Label lblDungeonEncounters;
        private ToolTip dungeonToolTip;
        private TextBox txtDungeonEncounters;
        private Button btnDungeonGenerate;
        private Button btnDungeonNewSeed;
        private Button btnDungeonSave;
        private Button btnDungeonSaveToMap;
        private Button btnDungeonDelete;
        private Button btnDungeonImport;
        private Button btnDungeonExport;
        private Button btnDungeonExportPng;
        private Button btnDungeonAddRoom;
        private Button btnDungeonApplyRoom;
        private Button btnDungeonRemoveRoom;
        private Button btnDungeonToolSelect;
        private Button btnDungeonToolConnect;
        private Button btnDungeonToolDisconnect;
        private Button btnDungeonToolAddRoom;
        private Button btnDungeonToolDoor;
        private Button btnDungeonZoomOut;
        private Button btnDungeonZoomReset;
        private Button btnDungeonZoomIn;
        private Button btnDungeonAddLevel;
        private Button btnDungeonRemoveLevel;
        private List<DungeonRecord> dungeonLibrary = new List<DungeonRecord>();
        private DungeonRecord currentDungeon;
        private HexFeatureRecord currentDungeonMapFeature;
        private readonly Dictionary<string, RectangleF> dungeonRoomBounds = new Dictionary<string, RectangleF>();
        private DungeonEditorTool dungeonEditorTool = DungeonEditorTool.SelectMove;
        private DungeonRoomRecord dungeonConnectionStartRoom;
        private DungeonRoomRecord dungeonDisconnectStartRoom;
        private DungeonRoomRecord dungeonDragRoom;
        private DungeonRoomRecord dungeonResizeRoom;
        private DungeonDoorRecord dungeonSelectedDoor;
        private DungeonDoorRecord dungeonDragDoor;
        private DungeonConnectionRecord dungeonSelectedConnection;
        private DungeonConnectionRecord dungeonDragConnection;
        private DungeonConnectionRecord dungeonPendingConnectionDrag;
        private DungeonPathPointRecord dungeonFreeConnectionStartPoint;
        private DungeonRenderLayout dungeonRenderLayout;
        private readonly HashSet<DungeonConnectionRecord> dungeonGridPathBuildStack = new HashSet<DungeonConnectionRecord>();
        private readonly List<DungeonDoorAnchor> dungeonRoomDoorAnchors = new List<DungeonDoorAnchor>();
        private Point dungeonDragStartPoint;
        private int dungeonDragStartX;
        private int dungeonDragStartY;
        private int dungeonDragStartWidth;
        private int dungeonDragStartHeight;
        private double dungeonDragDoorStartX;
        private double dungeonDragDoorStartY;
        private DungeonRoomResizeHandle dungeonResizeHandle = DungeonRoomResizeHandle.None;
        private int dungeonSelectedPathPointIndex = -1;
        private int dungeonDragPathPointIndex = -1;
        private int dungeonPendingConnectionInsertIndex = -1;
        private PointF dungeonPendingConnectionGridPoint;
        private bool dungeonDragging;
        private bool dungeonResizing;
        private bool dungeonDoorDragging;
        private bool dungeonPathPointDragging;
        private bool dungeonPanning;
        private Point dungeonPanLastPoint;
        private PointF dungeonPanOffset = PointF.Empty;
        private float dungeonZoom = 1f;
        private bool dungeonUiLoading;

        private enum DungeonEditorTool
        {
            SelectMove,
            Connect,
            Disconnect,
            AddRoom,
            PlaceDoor
        }

        private enum DungeonRoomResizeHandle
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private sealed class DungeonDoorAnchor
        {
            public DungeonDoorRecord Door { get; set; }
            public DungeonConnectionRecord Connection { get; set; }
            public double RelativeX { get; set; }
            public double RelativeY { get; set; }
            public bool RoomOnly { get; set; }
            public bool OnRoomBoundary { get; set; }
            public string RoomWall { get; set; }
        }

        private void InitializeDungeonTab()
        {
            if (tabControl1 == null || tabPageDungeons != null) return;

            LoadDungeonLibrary();
            tabPageDungeons = new TabPage(isEnglish ? "Dungeons" : "Данжи");
            tabControl1.TabPages.Add(tabPageDungeons);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));

            root.Controls.Add(BuildDungeonLibraryPanel(), 0, 0);
            root.Controls.Add(BuildDungeonMapPanel(), 1, 0);
            root.Controls.Add(BuildDungeonRoomPanel(), 2, 0);

            tabPageDungeons.Controls.Add(root);
            RefreshDungeonLibraryUi();
            UpdateDungeonLanguage();
        }

        private Control BuildDungeonLibraryPanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };
            Label libraryLabel = new Label
            {
                Name = "lblDungeonLibrary",
                Location = new Point(0, 0),
                Size = new Size(220, 20),
                Font = UiTheme.CreateFont(FontStyle.Bold)
            };
            lstDungeons = new ListBox { Location = new Point(0, 24), Size = new Size(220, 220), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lstDungeons.SelectedIndexChanged += (s, e) =>
            {
                if (dungeonUiLoading) return;
                DungeonRecord selected = lstDungeons.SelectedItem as DungeonRecord;
                if (selected != null) LoadDungeonToEditor(XmlSerialization.Clone(selected), null);
            };

            Label nameLabel = new Label { Name = "lblDungeonName", Location = new Point(0, 254), Size = new Size(220, 18) };
            txtDungeonName = new TextBox { Location = new Point(0, 274), Size = new Size(220, 24) };
            Label typeLabel = new Label { Name = "lblDungeonType", Location = new Point(0, 304), Size = new Size(220, 18) };
            cmbDungeonType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, 324), Size = new Size(220, 24) };
            foreach (DungeonTypeDefinition type in DungeonCatalog.DungeonTypes)
            {
                cmbDungeonType.Items.Add(new DungeonTypeItem(type.Name, isEnglish ? type.Name : type.RussianName));
            }
            Label sizeLabel = new Label { Name = "lblDungeonSize", Location = new Point(0, 354), Size = new Size(126, 18) };
            Label recommendedLevelLabel = new Label { Name = "lblDungeonRecommendedLevel", Location = new Point(134, 354), Size = new Size(86, 18) };
            cmbDungeonSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, 374), Size = new Size(126, 24) };
            cmbDungeonSize.Items.AddRange(new object[]
            {
                new DungeonTypeItem("Lair", isEnglish ? "Lair" : "Логово"),
                new DungeonTypeItem("Small", isEnglish ? "Small" : "Малый"),
                new DungeonTypeItem("Standard", isEnglish ? "Standard" : "Стандартный"),
                new DungeonTypeItem("Large", isEnglish ? "Large" : "Большой"),
                new DungeonTypeItem("Megadungeon", isEnglish ? "Megadungeon" : "Мегаданж")
            });
            cmbDungeonSize.SelectedIndex = 2;
            nudDungeonLevel = new NumericUpDown { Location = new Point(134, 374), Size = new Size(86, 24), Minimum = DungeonCatalog.MinDungeonLevel, Maximum = DungeonCatalog.MaxDungeonLevel, Value = DungeonCatalog.MinDungeonLevel };
            Label seedLabel = new Label { Name = "lblDungeonSeed", Location = new Point(0, 404), Size = new Size(220, 18) };
            nudDungeonSeed = new NumericUpDown { Location = new Point(0, 424), Size = new Size(126, 24), Minimum = 0, Maximum = 999999999, Value = NextDungeonSeedValue() };
            btnDungeonNewSeed = new Button { Location = new Point(134, 424), Size = new Size(86, 24) };
            dungeonToolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 150 };
            dungeonToolTip.SetToolTip(nudDungeonSeed, isEnglish ? "Generate rolls a new seed each time. The same seed with the same settings repeats the dungeon." : "Генерация каждый раз задаёт новый seed. Тот же seed с теми же настройками повторяет данж.");
            dungeonToolTip.SetToolTip(btnDungeonNewSeed, isEnglish ? "Generate a new seed." : "Сгенерировать новый seed.");

            btnDungeonGenerate = new Button { Location = new Point(0, 460), Size = new Size(220, 30) };
            btnDungeonSave = new Button { Location = new Point(0, 498), Size = new Size(106, 28) };
            btnDungeonDelete = new Button { Location = new Point(114, 498), Size = new Size(106, 28) };
            btnDungeonImport = new Button { Location = new Point(0, 534), Size = new Size(106, 28) };
            btnDungeonExport = new Button { Location = new Point(114, 534), Size = new Size(106, 28) };
            btnDungeonExportPng = new Button { Location = new Point(0, 570), Size = new Size(220, 28) };
            btnDungeonSaveToMap = new Button { Location = new Point(0, 606), Size = new Size(220, 28), Visible = false };

            btnDungeonGenerate.Click += (s, e) => GenerateDungeonFromUi();
            btnDungeonNewSeed.Click += (s, e) => SetNewDungeonSeed();
            btnDungeonSave.Click += (s, e) => SaveCurrentDungeonToLibrary();
            btnDungeonDelete.Click += (s, e) => DeleteSelectedDungeon();
            btnDungeonImport.Click += (s, e) => ImportDungeonFromXml();
            btnDungeonExport.Click += (s, e) => ExportCurrentDungeonToXml();
            btnDungeonExportPng.Click += (s, e) => ExportCurrentDungeonToPng();
            btnDungeonSaveToMap.Click += (s, e) => SaveCurrentDungeonBackToMapFeature();

            foreach (Button button in new[] { btnDungeonGenerate, btnDungeonNewSeed, btnDungeonSave, btnDungeonImport, btnDungeonExport, btnDungeonExportPng, btnDungeonSaveToMap })
            {
                UiTheme.StyleCommandButton(button, UiTheme.PositiveButtonColor);
            }
            UiTheme.StyleNegativeButton(btnDungeonDelete);

            panel.Controls.Add(libraryLabel);
            panel.Controls.Add(lstDungeons);
            panel.Controls.Add(nameLabel);
            panel.Controls.Add(txtDungeonName);
            panel.Controls.Add(typeLabel);
            panel.Controls.Add(cmbDungeonType);
            panel.Controls.Add(sizeLabel);
            panel.Controls.Add(recommendedLevelLabel);
            panel.Controls.Add(cmbDungeonSize);
            panel.Controls.Add(nudDungeonLevel);
            panel.Controls.Add(seedLabel);
            panel.Controls.Add(nudDungeonSeed);
            panel.Controls.Add(btnDungeonNewSeed);
            panel.Controls.Add(btnDungeonGenerate);
            panel.Controls.Add(btnDungeonSave);
            panel.Controls.Add(btnDungeonDelete);
            panel.Controls.Add(btnDungeonImport);
            panel.Controls.Add(btnDungeonExport);
            panel.Controls.Add(btnDungeonExportPng);
            panel.Controls.Add(btnDungeonSaveToMap);
            return panel;
        }

        private Control BuildDungeonMapPanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };
            Label levelLabel = new Label { Name = "lblDungeonLevelView", Location = new Point(0, 0), Size = new Size(90, 24) };
            cmbDungeonLevelView = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(96, 0), Size = new Size(120, 24) };
            cmbDungeonLevelView.SelectedIndexChanged += (s, e) =>
            {
                RefreshDungeonRoomList();
                pnlDungeonMap.Invalidate();
            };
            btnDungeonAddLevel = new Button { Location = new Point(222, 0), Size = new Size(46, 24) };
            btnDungeonRemoveLevel = new Button { Location = new Point(274, 0), Size = new Size(46, 24) };
            btnDungeonAddLevel.Click += (s, e) => AddDungeonLevel();
            btnDungeonRemoveLevel.Click += (s, e) => RemoveSelectedDungeonLevel();
            UiTheme.StyleCommandButton(btnDungeonAddLevel, UiTheme.PositiveButtonColor);
            UiTheme.StyleNegativeButton(btnDungeonRemoveLevel);

            btnDungeonToolSelect = CreateDungeonToolButton(DungeonEditorTool.SelectMove, 334, 0);
            btnDungeonToolConnect = CreateDungeonToolButton(DungeonEditorTool.Connect, 430, 0);
            btnDungeonToolDisconnect = CreateDungeonToolButton(DungeonEditorTool.Disconnect, 526, 0);
            btnDungeonToolAddRoom = CreateDungeonToolButton(DungeonEditorTool.AddRoom, 622, 0);
            btnDungeonToolDoor = CreateDungeonToolButton(DungeonEditorTool.PlaceDoor, 718, 0);
            btnDungeonZoomOut = new Button { Location = new Point(814, 0), Size = new Size(34, 24), Text = "-" };
            btnDungeonZoomReset = new Button { Location = new Point(852, 0), Size = new Size(50, 24), Text = "100%" };
            btnDungeonZoomIn = new Button { Location = new Point(906, 0), Size = new Size(34, 24), Text = "+" };
            btnDungeonZoomOut.Click += (s, e) => ZoomDungeonMap(0.85f, new Point(pnlDungeonMap.Width / 2, pnlDungeonMap.Height / 2));
            btnDungeonZoomReset.Click += (s, e) => ResetDungeonMapView();
            btnDungeonZoomIn.Click += (s, e) => ZoomDungeonMap(1.18f, new Point(pnlDungeonMap.Width / 2, pnlDungeonMap.Height / 2));
            UiTheme.StyleCommandButton(btnDungeonZoomOut, UiTheme.NeutralButtonColor);
            UiTheme.StyleCommandButton(btnDungeonZoomReset, UiTheme.NeutralButtonColor);
            UiTheme.StyleCommandButton(btnDungeonZoomIn, UiTheme.NeutralButtonColor);
            cmbDungeonPassageKind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, 30), Size = new Size(150, 24) };
            cmbDungeonPassageKind.Items.AddRange(new object[]
            {
                new DungeonTypeItem("Corridor", isEnglish ? "Corridor" : "Коридор"),
                new DungeonTypeItem("Passage", isEnglish ? "Passage" : "Проход"),
                new DungeonTypeItem("Stairs", isEnglish ? "Stairs" : "Лестница"),
                new DungeonTypeItem("Overpass", isEnglish ? "Overpass/underpass" : "Над/под")
            });
            cmbDungeonPassageKind.SelectedIndex = 0;
            cmbDungeonDoorKind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(158, 30), Size = new Size(150, 24) };
            cmbDungeonDoorKind.Items.AddRange(new object[]
            {
                new DungeonTypeItem("Door", isEnglish ? "Door" : "Дверь"),
                new DungeonTypeItem("SecretDoor", isEnglish ? "Secret door" : "Тайная дверь"),
                new DungeonTypeItem("SecretPassage", isEnglish ? "Secret passage" : "Тайный проход")
            });
            cmbDungeonDoorKind.SelectedIndex = 0;
            nudDungeonPassageWidth = new NumericUpDown { Location = new Point(316, 30), Size = new Size(58, 24), Minimum = 1, Maximum = 4, Value = 1 };

            pnlDungeonMap = new DoubleBufferedPanel
            {
                Location = new Point(0, 62),
                Size = new Size(600, 490),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(233, 225, 186),
                TabStop = true
            };
            pnlDungeonMap.Paint += (s, e) => DrawDungeonEditorMap(e.Graphics, pnlDungeonMap.ClientRectangle);
            pnlDungeonMap.MouseDown += DungeonMap_MouseDown;
            pnlDungeonMap.MouseMove += DungeonMap_MouseMove;
            pnlDungeonMap.MouseUp += DungeonMap_MouseUp;
            pnlDungeonMap.MouseWheel += DungeonMap_MouseWheel;
            pnlDungeonMap.MouseEnter += (s, e) => pnlDungeonMap.Focus();
            pnlDungeonMap.KeyDown += DungeonMap_KeyDown;
            pnlDungeonMap.MouseDoubleClick += (s, e) => SelectDungeonRoomAtPoint(e.Location);

            lblDungeonEncounters = new Label
            {
                Name = "lblDungeonEncounters",
                Location = new Point(0, 560),
                Size = new Size(600, 18),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = UiTheme.CreateFont(FontStyle.Bold)
            };

            txtDungeonEncounters = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(0, 582),
                Size = new Size(600, 88),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            panel.Resize += (s, e) =>
            {
                pnlDungeonMap.Size = new Size(Math.Max(200, panel.ClientSize.Width), Math.Max(200, panel.ClientSize.Height - 202));
                lblDungeonEncounters.Location = new Point(0, pnlDungeonMap.Bottom + 8);
                lblDungeonEncounters.Size = new Size(Math.Max(200, panel.ClientSize.Width), 18);
                txtDungeonEncounters.Location = new Point(0, lblDungeonEncounters.Bottom + 4);
                txtDungeonEncounters.Size = new Size(Math.Max(200, panel.ClientSize.Width), Math.Max(70, panel.ClientSize.Height - txtDungeonEncounters.Top));
            };

            panel.Controls.Add(levelLabel);
            panel.Controls.Add(cmbDungeonLevelView);
            panel.Controls.Add(btnDungeonAddLevel);
            panel.Controls.Add(btnDungeonRemoveLevel);
            panel.Controls.Add(btnDungeonToolSelect);
            panel.Controls.Add(btnDungeonToolConnect);
            panel.Controls.Add(btnDungeonToolDisconnect);
            panel.Controls.Add(btnDungeonToolAddRoom);
            panel.Controls.Add(btnDungeonToolDoor);
            panel.Controls.Add(btnDungeonZoomOut);
            panel.Controls.Add(btnDungeonZoomReset);
            panel.Controls.Add(btnDungeonZoomIn);
            panel.Controls.Add(cmbDungeonPassageKind);
            panel.Controls.Add(cmbDungeonDoorKind);
            panel.Controls.Add(nudDungeonPassageWidth);
            panel.Controls.Add(pnlDungeonMap);
            panel.Controls.Add(lblDungeonEncounters);
            panel.Controls.Add(txtDungeonEncounters);
            return panel;
        }

        private Button CreateDungeonToolButton(DungeonEditorTool tool, int x, int y)
        {
            Button button = new Button
            {
                Location = new Point(x, y),
                Size = new Size(88, 24),
                Tag = tool
            };
            button.Click += (s, e) => SetDungeonEditorTool(tool);
            UiTheme.StyleCommandButton(button, UiTheme.NeutralButtonColor);
            return button;
        }

        private Control BuildDungeonRoomPanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            Label roomsLabel = new Label { Name = "lblDungeonRooms", Location = new Point(0, 0), Size = new Size(260, 20), Font = UiTheme.CreateFont(FontStyle.Bold) };
            lstDungeonRooms = new ListBox { Location = new Point(0, 24), Size = new Size(260, 180), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lstDungeonRooms.Format += (s, e) =>
            {
                DungeonRoomRecord room = e.ListItem as DungeonRoomRecord;
                if (room != null) e.Value = FormatDungeonRoomListItem(room);
            };
            lstDungeonRooms.SelectedIndexChanged += (s, e) => LoadSelectedDungeonRoomToEditor();
            lstDungeonRooms.KeyDown += DungeonMap_KeyDown;

            Label roomKindLabel = new Label { Name = "lblDungeonRoomKind", Location = new Point(0, 214), Size = new Size(260, 18) };
            cmbDungeonRoomKind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, 234), Size = new Size(260, 24) };
            FillDungeonRoomKindCombo("Empty");
            Label roomShapeLabel = new Label { Name = "lblDungeonRoomShape", Location = new Point(0, 264), Size = new Size(260, 18) };
            cmbDungeonRoomShape = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, 284), Size = new Size(260, 24) };
            FillDungeonRoomShapeCombo("Rectangle");
            Label roomSizeLabel = new Label { Name = "lblDungeonRoomSize", Location = new Point(0, 318), Size = new Size(82, 20) };
            nudDungeonRoomWidth = new NumericUpDown { Location = new Point(88, 316), Size = new Size(78, 24), Minimum = 1, Maximum = 20, Value = 3 };
            nudDungeonRoomHeight = new NumericUpDown { Location = new Point(174, 316), Size = new Size(78, 24), Minimum = 1, Maximum = 20, Value = 2 };
            Label roomTitleLabel = new Label { Name = "lblDungeonRoomTitle", Location = new Point(0, 350), Size = new Size(260, 18) };
            txtDungeonRoomTitle = new TextBox { Location = new Point(0, 370), Size = new Size(260, 24) };
            Label roomMonsterLabel = new Label { Name = "lblDungeonRoomMonster", Location = new Point(0, 402), Size = new Size(260, 18) };
            txtDungeonRoomMonster = new TextBox { Location = new Point(0, 422), Size = new Size(260, 24) };
            Label roomTreasureLabel = new Label { Name = "lblDungeonRoomTreasure", Location = new Point(0, 454), Size = new Size(260, 18) };
            txtDungeonRoomTreasure = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(0, 474),
                Size = new Size(260, 76),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Label roomTrapLabel = new Label { Name = "lblDungeonRoomTrap", Location = new Point(0, 560), Size = new Size(260, 18) };
            txtDungeonRoomTrap = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(0, 580),
                Size = new Size(260, 118),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Label roomDetailsLabel = new Label { Name = "lblDungeonRoomDetails", Location = new Point(0, 708), Size = new Size(260, 18) };
            txtDungeonRoomDetails = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(0, 728),
                Size = new Size(260, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnDungeonApplyRoom = new Button { Location = new Point(0, 848), Size = new Size(82, 28) };
            btnDungeonAddRoom = new Button { Location = new Point(90, 848), Size = new Size(82, 28) };
            btnDungeonRemoveRoom = new Button { Location = new Point(180, 848), Size = new Size(82, 28) };
            btnDungeonApplyRoom.Click += (s, e) => ApplyDungeonRoomEditor();
            btnDungeonAddRoom.Click += (s, e) => AddDungeonRoom();
            btnDungeonRemoveRoom.Click += (s, e) => RemoveSelectedDungeonRoom();
            UiTheme.StylePositiveButton(btnDungeonApplyRoom);
            UiTheme.StyleCommandButton(btnDungeonAddRoom, UiTheme.PositiveButtonColor);
            UiTheme.StyleNegativeButton(btnDungeonRemoveRoom);

            panel.Controls.Add(roomsLabel);
            panel.Controls.Add(lstDungeonRooms);
            panel.Controls.Add(roomKindLabel);
            panel.Controls.Add(cmbDungeonRoomKind);
            panel.Controls.Add(roomShapeLabel);
            panel.Controls.Add(cmbDungeonRoomShape);
            panel.Controls.Add(roomSizeLabel);
            panel.Controls.Add(nudDungeonRoomWidth);
            panel.Controls.Add(nudDungeonRoomHeight);
            panel.Controls.Add(roomTitleLabel);
            panel.Controls.Add(txtDungeonRoomTitle);
            panel.Controls.Add(roomMonsterLabel);
            panel.Controls.Add(txtDungeonRoomMonster);
            panel.Controls.Add(roomTreasureLabel);
            panel.Controls.Add(txtDungeonRoomTreasure);
            panel.Controls.Add(roomTrapLabel);
            panel.Controls.Add(txtDungeonRoomTrap);
            panel.Controls.Add(roomDetailsLabel);
            panel.Controls.Add(txtDungeonRoomDetails);
            panel.Controls.Add(btnDungeonApplyRoom);
            panel.Controls.Add(btnDungeonAddRoom);
            panel.Controls.Add(btnDungeonRemoveRoom);
            return panel;
        }

        private void UpdateDungeonLanguage()
        {
            if (tabPageDungeons == null) return;
            SetNamedText(tabPageDungeons, "lblDungeonRoomKind", isEnglish ? "Room content" : "Содержимое");
            SetNamedText(tabPageDungeons, "lblDungeonRoomShape", isEnglish ? "Shape" : "Форма");
            SetNamedText(tabPageDungeons, "lblDungeonRoomTitle", isEnglish ? "Title" : "Название");
            SetNamedText(tabPageDungeons, "lblDungeonRoomMonster", isEnglish ? "Monster / count" : "Монстры / число");
            SetNamedText(tabPageDungeons, "lblDungeonRoomTreasure", isEnglish ? "Treasure" : "Сокровища");
            SetNamedText(tabPageDungeons, "lblDungeonRoomTrap", isEnglish ? "Trap / hazard" : "Ловушка / опасность");
            SetNamedText(tabPageDungeons, "lblDungeonRoomDetails", isEnglish ? "Details" : "Описание");
            tabPageDungeons.Text = isEnglish ? "Dungeons" : "Данжи";
            SetNamedText(tabPageDungeons, "lblDungeonLibrary", isEnglish ? "Dungeon library" : "Библиотека данжей");
            SetNamedText(tabPageDungeons, "lblDungeonName", isEnglish ? "Name, type, size, danger, seed" : "Название, тип, размер, опасность, seed");
            SetNamedText(tabPageDungeons, "lblDungeonLevelView", isEnglish ? "Floor" : "Этаж");
            SetNamedText(tabPageDungeons, "lblDungeonRooms", isEnglish ? "Rooms" : "Комнаты");
            SetNamedText(tabPageDungeons, "lblDungeonRoomSize", isEnglish ? "Size W/H" : "Размер Ш/В");
            SetNamedText(tabPageDungeons, "lblDungeonEncounters", isEnglish ? "Wandering encounters" : "Блуждающие встречи");
            if (btnDungeonAddLevel != null) btnDungeonAddLevel.Text = isEnglish ? "+Floor" : "+Эт.";
            if (btnDungeonRemoveLevel != null) btnDungeonRemoveLevel.Text = isEnglish ? "-Floor" : "-Эт.";
            if (btnDungeonToolSelect != null) btnDungeonToolSelect.Text = isEnglish ? "Select" : "Выбор";
            if (btnDungeonToolConnect != null) btnDungeonToolConnect.Text = isEnglish ? "Connect" : "Связать";
            if (btnDungeonToolDisconnect != null) btnDungeonToolDisconnect.Text = isEnglish ? "Break" : "Разрыв";
            if (btnDungeonToolAddRoom != null) btnDungeonToolAddRoom.Text = isEnglish ? "Room" : "Комната";
            if (btnDungeonToolDoor != null) btnDungeonToolDoor.Text = isEnglish ? "Door" : "Дверь";
            if (btnDungeonZoomReset != null) btnDungeonZoomReset.Text = "100%";
            if (btnDungeonGenerate != null) btnDungeonGenerate.Text = isEnglish ? "Generate dungeon" : "Сгенерировать данж";
            if (btnDungeonSave != null) btnDungeonSave.Text = isEnglish ? "Save" : "Сохранить";
            if (btnDungeonDelete != null) btnDungeonDelete.Text = isEnglish ? "Delete" : "Удалить";
            if (btnDungeonImport != null) btnDungeonImport.Text = isEnglish ? "Import" : "Импорт";
            if (btnDungeonExport != null) btnDungeonExport.Text = isEnglish ? "Export" : "Экспорт";
            if (btnDungeonExportPng != null) btnDungeonExportPng.Text = isEnglish ? "Export PNG" : "Экспорт PNG";
            if (btnDungeonSaveToMap != null) btnDungeonSaveToMap.Text = isEnglish ? "Save to map feature" : "Сохранить в особенность карты";
            if (btnDungeonApplyRoom != null) btnDungeonApplyRoom.Text = isEnglish ? "Apply" : "Применить";
            if (btnDungeonAddRoom != null) btnDungeonAddRoom.Text = isEnglish ? "Add" : "Добавить";
            if (btnDungeonRemoveRoom != null) btnDungeonRemoveRoom.Text = isEnglish ? "Remove" : "Удалить";
            SetNamedText(tabPageDungeons, "lblDungeonName", isEnglish ? "Name" : "Название");
            SetNamedText(tabPageDungeons, "lblDungeonType", isEnglish ? "Type" : "Тип");
            SetNamedText(tabPageDungeons, "lblDungeonSize", isEnglish ? "Size" : "Размер");
            SetNamedText(tabPageDungeons, "lblDungeonRecommendedLevel", isEnglish ? "Danger" : "Опасность");
            SetNamedText(tabPageDungeons, "lblDungeonSeed", "Seed");
            if (btnDungeonNewSeed != null) btnDungeonNewSeed.Text = isEnglish ? "New" : "Новый";
            if (dungeonToolTip != null && nudDungeonSeed != null)
            {
                dungeonToolTip.SetToolTip(nudDungeonSeed, isEnglish
                    ? "Generate rolls a new seed each time. The same seed with the same settings repeats the dungeon."
                    : "Генерация каждый раз задаёт новый seed. Тот же seed с теми же настройками повторяет данж.");
            }
            if (dungeonToolTip != null && btnDungeonNewSeed != null)
            {
                dungeonToolTip.SetToolTip(btnDungeonNewSeed, isEnglish ? "Generate a new seed." : "Сгенерировать новый seed.");
            }
            FillDungeonRoomKindCombo(SelectedDungeonItemValue(cmbDungeonRoomKind));
            FillDungeonRoomShapeCombo(SelectedDungeonItemValue(cmbDungeonRoomShape));
            FillDungeonPassageKindCombo(SelectedDungeonItemValue(cmbDungeonPassageKind));
            FillDungeonDoorKindCombo(SelectedDungeonItemValue(cmbDungeonDoorKind));
            UpdateDungeonToolButtonStyles();
            RefreshDungeonRoomList();
            RefreshDungeonEncounterText();
            pnlDungeonMap?.Invalidate();
        }

        private void SetNamedText(Control root, string name, string text)
        {
            if (root == null) return;
            foreach (Control control in root.Controls.Find(name, true))
            {
                control.Text = text;
            }
        }

        private void SetDungeonEditorTool(DungeonEditorTool tool)
        {
            dungeonEditorTool = tool;
            dungeonConnectionStartRoom = null;
            dungeonDisconnectStartRoom = null;
            dungeonDragging = false;
            dungeonResizing = false;
            dungeonDoorDragging = false;
            dungeonPathPointDragging = false;
            dungeonDragRoom = null;
            dungeonResizeRoom = null;
            dungeonResizeHandle = DungeonRoomResizeHandle.None;
            dungeonDragDoor = null;
            dungeonDragConnection = null;
            dungeonPendingConnectionDrag = null;
            dungeonFreeConnectionStartPoint = null;
            dungeonRoomDoorAnchors.Clear();
            dungeonDragPathPointIndex = -1;
            UpdateDungeonToolButtonStyles();
            pnlDungeonMap?.Invalidate();
        }

        private void ClearDungeonPlanSelection()
        {
            dungeonSelectedDoor = null;
            dungeonSelectedConnection = null;
            dungeonSelectedPathPointIndex = -1;
            dungeonConnectionStartRoom = null;
            dungeonDisconnectStartRoom = null;
            dungeonFreeConnectionStartPoint = null;
            dungeonResizeHandle = DungeonRoomResizeHandle.None;
        }

        private void ResetDungeonMapView()
        {
            dungeonZoom = 1f;
            dungeonPanOffset = PointF.Empty;
            dungeonPanning = false;
            pnlDungeonMap?.Invalidate();
        }

        private void ZoomDungeonMap(float factor, Point anchor)
        {
            if (pnlDungeonMap == null) return;
            if (dungeonRenderLayout == null)
            {
                dungeonZoom = Clamp(dungeonZoom * factor, 0.45f, 3.5f);
                pnlDungeonMap.Invalidate();
                return;
            }

            PointF gridBefore = new PointF(
                (anchor.X - dungeonRenderLayout.OffsetX) / dungeonRenderLayout.Scale,
                (anchor.Y - dungeonRenderLayout.OffsetY) / dungeonRenderLayout.Scale);
            float nextZoom = Clamp(dungeonZoom * factor, 0.45f, 3.5f);
            dungeonZoom = nextZoom;
            dungeonPanOffset = new PointF(
                anchor.X - dungeonRenderLayout.BaseOffsetX - gridBefore.X * dungeonRenderLayout.BaseScale * dungeonZoom,
                anchor.Y - dungeonRenderLayout.BaseOffsetY - gridBefore.Y * dungeonRenderLayout.BaseScale * dungeonZoom);
            pnlDungeonMap.Invalidate();
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void UpdateDungeonToolButtonStyles()
        {
            foreach (Button button in new[] { btnDungeonToolSelect, btnDungeonToolConnect, btnDungeonToolDisconnect, btnDungeonToolAddRoom, btnDungeonToolDoor })
            {
                if (button == null) continue;
                DungeonEditorTool tool = button.Tag is DungeonEditorTool ? (DungeonEditorTool)button.Tag : DungeonEditorTool.SelectMove;
                UiTheme.StyleCommandButton(button, tool == dungeonEditorTool ? UiTheme.PositiveButtonColor : UiTheme.NeutralButtonColor);
            }
        }

        private void LoadDungeonLibrary()
        {
            dungeonLibrary = new XmlRecordStore<DungeonRecord>(dungeonLibraryPath).Load();
            foreach (DungeonRecord dungeon in dungeonLibrary)
            {
                NormalizeDungeonForEditor(dungeon);
            }
        }

        private void SaveDungeonLibrary()
        {
            new XmlRecordStore<DungeonRecord>(dungeonLibraryPath).Save(dungeonLibrary);
        }

        private void RefreshDungeonLibraryUi()
        {
            if (lstDungeons == null) return;
            dungeonUiLoading = true;
            string selectedId = (lstDungeons.SelectedItem as DungeonRecord)?.Id;
            lstDungeons.Items.Clear();
            foreach (DungeonRecord dungeon in dungeonLibrary.OrderBy(d => d.Name))
            {
                lstDungeons.Items.Add(dungeon);
                if (!string.IsNullOrWhiteSpace(selectedId) && dungeon.Id == selectedId)
                {
                    lstDungeons.SelectedItem = dungeon;
                }
            }
            dungeonUiLoading = false;
        }

        private static int NextDungeonSeedValue()
        {
            int value = (int)((uint)BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) % 999999999u);
            return value == 0 ? 1 : value;
        }

        private void SetNewDungeonSeed()
        {
            if (nudDungeonSeed == null) return;
            nudDungeonSeed.Value = NextDungeonSeedValue();
        }

        private void GenerateDungeonFromUi()
        {
            SetNewDungeonSeed();
            DungeonGenerationOptions options = new DungeonGenerationOptions
            {
                Name = string.IsNullOrWhiteSpace(txtDungeonName.Text) ? "" : txtDungeonName.Text.Trim(),
                DungeonType = SelectedDungeonItemValue(cmbDungeonType),
                Size = SelectedDungeonItemValue(cmbDungeonSize),
                RecommendedLevel = (int)nudDungeonLevel.Value,
                Seed = nudDungeonSeed.Value.ToString(),
                RussianOutput = !isEnglish
            };
            DungeonRecord dungeon = new DungeonGenerator().Generate(options);
            LoadDungeonToEditor(dungeon, null);
        }

        private void LoadDungeonToEditor(DungeonRecord dungeon, HexFeatureRecord mapFeature)
        {
            // Редактор работает с копией: пользователь может править данж и только
            // явной кнопкой сохранить его в библиотеку или обратно в особенность карты.
            currentDungeon = dungeon == null ? null : XmlSerialization.Clone(dungeon);
            currentDungeonMapFeature = mapFeature;
            NormalizeDungeonForEditor(currentDungeon);
            ResetDungeonMapView();

            dungeonUiLoading = true;
            try
            {
                if (currentDungeon != null)
                {
                    txtDungeonName.Text = currentDungeon.Name;
                    SelectDungeonItem(cmbDungeonType, currentDungeon.DungeonType);
                    SelectDungeonItem(cmbDungeonSize, currentDungeon.Size);
                    nudDungeonLevel.Value = Math.Max(nudDungeonLevel.Minimum, Math.Min(nudDungeonLevel.Maximum, currentDungeon.RecommendedLevel));
                }
                RefreshDungeonLevelView();
                RefreshDungeonRoomList();
                RefreshDungeonEncounterText();
            }
            finally
            {
                dungeonUiLoading = false;
            }

            if (btnDungeonSaveToMap != null) btnDungeonSaveToMap.Visible = currentDungeonMapFeature != null;
            pnlDungeonMap?.Invalidate();
        }

        private void OpenDungeonInDungeonTab(DungeonRecord dungeon, HexFeatureRecord mapFeature)
        {
            if (tabPageDungeons == null) InitializeDungeonTab();
            LoadDungeonToEditor(dungeon, mapFeature);
            if (tabControl1 != null && tabPageDungeons != null) tabControl1.SelectedTab = tabPageDungeons;
        }

        private bool TryLinkDungeonFromLibraryToMapFeature(HexFeatureRecord feature)
        {
            if (feature == null) return false;
            LoadDungeonLibrary();
            if (dungeonLibrary.Count == 0)
            {
                MessageBox.Show(this,
                    isEnglish ? "Dungeon library is empty." : "Библиотека данжей пуста.",
                    isEnglish ? "Dungeons" : "Данжи");
                return false;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = isEnglish ? "Link dungeon" : "Привязать данж";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.ClientSize = new Size(360, 120);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;

                ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 18), Size = new Size(336, 24) };
                foreach (DungeonRecord dungeon in dungeonLibrary.OrderBy(d => d.Name)) combo.Items.Add(dungeon);
                if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                Button ok = new Button { Text = isEnglish ? "Link" : "Привязать", DialogResult = DialogResult.OK, Location = new Point(128, 70), Size = new Size(104, 28) };
                Button cancel = new Button { Text = isEnglish ? "Cancel" : "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(244, 70), Size = new Size(104, 28) };
                UiTheme.StylePositiveButton(ok);
                UiTheme.StyleNegativeButton(cancel);
                dialog.Controls.Add(combo);
                dialog.Controls.Add(ok);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                UiTheme.ApplyUniformFonts(dialog);
                UiTheme.ApplyThemeColors(dialog);

                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                DungeonRecord selected = combo.SelectedItem as DungeonRecord;
                if (selected == null) return false;

                // Привязка копирует выбранный данж в карту и обновляет внешнюю метку
                // особенности, чтобы экспорт карты оставался самодостаточным.
                DungeonRecord clone = XmlSerialization.Clone(selected);
                NormalizeDungeonForEditor(clone);
                if (currentMap.Dungeons == null) currentMap.Dungeons = new List<DungeonRecord>();
                currentMap.Dungeons.RemoveAll(d => d != null && string.Equals(d.Id, clone.Id, StringComparison.OrdinalIgnoreCase));
                currentMap.Dungeons.Add(clone);
                feature.DungeonId = clone.Id;
                feature.DungeonType = clone.DungeonType;
                feature.Subtype = clone.DungeonType;
                feature.DungeonSize = clone.Size;
                feature.DungeonLevel = DungeonCatalog.ClampDungeonLevel(clone.RecommendedLevel);
                feature.Name = clone.Name;
                feature.IconKey = DungeonCatalog.DungeonTypeIconKey(clone.DungeonType);
                feature.UpdatedAt = DateTime.Now;
                return true;
            }
        }

        private void SaveCurrentDungeonToLibrary()
        {
            if (currentDungeon == null) return;
            ApplyDungeonHeaderFromUi();
            NormalizeDungeonForEditor(currentDungeon);
            currentDungeon.UpdatedAt = DateTime.Now;
            dungeonLibrary.RemoveAll(d => d.Id == currentDungeon.Id);
            dungeonLibrary.Add(XmlSerialization.Clone(currentDungeon));
            SaveDungeonLibrary();
            RefreshDungeonLibraryUi();
        }

        private void SaveCurrentDungeonBackToMapFeature()
        {
            if (currentDungeon == null || currentDungeonMapFeature == null || currentMap == null) return;
            ApplyDungeonHeaderFromUi();
            NormalizeDungeonForEditor(currentDungeon);
            // Сохранение из вкладки данжей меняет и сам DungeonRecord, и краткую
            // карточку HexFeatureRecord, которую видит карта.
            if (currentMap.Dungeons == null) currentMap.Dungeons = new List<DungeonRecord>();
            currentMap.Dungeons.RemoveAll(d => d != null && string.Equals(d.Id, currentDungeon.Id, StringComparison.OrdinalIgnoreCase));
            currentMap.Dungeons.Add(XmlSerialization.Clone(currentDungeon));
            currentDungeonMapFeature.DungeonId = currentDungeon.Id;
            currentDungeonMapFeature.Name = currentDungeon.Name;
            currentDungeonMapFeature.Subtype = currentDungeon.DungeonType;
            currentDungeonMapFeature.DungeonType = currentDungeon.DungeonType;
            currentDungeonMapFeature.DungeonLevel = DungeonCatalog.ClampDungeonLevel(currentDungeon.RecommendedLevel);
            currentDungeonMapFeature.DungeonSize = currentDungeon.Size;
            currentDungeonMapFeature.IconKey = DungeonCatalog.DungeonTypeIconKey(currentDungeon.DungeonType);
            currentDungeonMapFeature.UpdatedAt = DateTime.Now;
            RebuildCurrentMapIndex();
            pnlHexMap?.Invalidate();
        }

        private void DeleteSelectedDungeon()
        {
            DungeonRecord selected = lstDungeons == null ? null : lstDungeons.SelectedItem as DungeonRecord;
            if (selected == null) return;
            DialogResult result = MessageBox.Show(this,
                isEnglish ? "Delete selected dungeon?" : "Удалить выбранный данж?",
                isEnglish ? "Dungeons" : "Данжи",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            dungeonLibrary.RemoveAll(d => d.Id == selected.Id);
            SaveDungeonLibrary();
            RefreshDungeonLibraryUi();
        }

        private void ImportDungeonFromXml()
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Dungeon XML (*.xml)|*.xml|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            DungeonRecord dungeon = XmlSerialization.DeserializeFile<DungeonRecord>(dialog.FileName);
            if (dungeon == null) return;
            NormalizeDungeonForEditor(dungeon);
            LoadDungeonToEditor(dungeon, null);
            SaveCurrentDungeonToLibrary();
        }

        private void ExportCurrentDungeonToXml()
        {
            if (currentDungeon == null) return;
            ApplyDungeonHeaderFromUi();
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Dungeon XML (*.xml)|*.xml",
                FileName = SafeDungeonFileName(currentDungeon.Name) + ".xml"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            XmlSerialization.SerializeFile(dialog.FileName, currentDungeon);
        }

        private void ExportCurrentDungeonToPng()
        {
            if (currentDungeon == null) return;
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PNG image (*.png)|*.png",
                FileName = SafeDungeonFileName(currentDungeon.Name) + ".png"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            int width = 2400;
            int mapHeight = 1050;
            int levelCount = currentDungeon.Levels == null ? 0 : currentDungeon.Levels.Count;
            int encounterLines = DungeonEncounterDisplayLines(currentDungeon).Count;
            int legendHeight = Math.Max(260, 170 + encounterLines * 30);
            int height = Math.Max(1300, 150 + levelCount * (mapHeight + 90) + legendHeight);
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                graphics.Clear(Color.FromArgb(233, 225, 186));
                using (Font titleFont = new Font(UiTheme.FontFamily, 18f, FontStyle.Bold, GraphicsUnit.Point))
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    graphics.DrawString(currentDungeon.DisplayName, titleFont, textBrush, 24, 20);
                }

                int y = 70;
                foreach (DungeonLevelRecord level in currentDungeon.Levels)
                {
                    using (Font levelFont = new Font(UiTheme.FontFamily, 15f, FontStyle.Bold, GraphicsUnit.Point))
                    using (Brush textBrush = new SolidBrush(Color.FromArgb(45, 34, 24)))
                    {
                        graphics.DrawString(isEnglish ? "Floor " + level.LevelNumber : "Этаж " + level.LevelNumber, levelFont, textBrush, 24, y);
                    }

                    DrawDungeonLevel(graphics, new Rectangle(24, y + 34, width - 48, mapHeight), currentDungeon, level.LevelNumber, false);
                    y += mapHeight + 90;
                }
                DrawDungeonPngLegend(graphics, new Rectangle(24, y, width - 48, legendHeight), currentDungeon);
                bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void DrawDungeonPngLegend(Graphics graphics, Rectangle bounds, DungeonRecord dungeon)
        {
            using (Font headerFont = new Font(UiTheme.FontFamily, 15f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font textFont = new Font(UiTheme.FontFamily, 12f, FontStyle.Regular, GraphicsUnit.Point))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(40, 32, 24)))
            {
                int y = bounds.Top;
                graphics.DrawString(isEnglish ? "Legend" : "Легенда", headerFont, textBrush, bounds.Left, y);
                y += 34;
                DrawDungeonLegendLine(graphics, textFont, textBrush, bounds.Left, y, isEnglish ? "Corridor / passage" : "Коридор / проход", false, false);
                y += 32;
                DrawDungeonLegendLine(graphics, textFont, textBrush, bounds.Left, y, isEnglish ? "Over/under passage" : "Проход выше/ниже", true, false);
                y += 32;
                DrawDungeonLegendDoor(graphics, textFont, textBrush, bounds.Left, y, isEnglish ? "Door" : "Дверь", false);
                y += 32;
                DrawDungeonLegendDoor(graphics, textFont, textBrush, bounds.Left, y, isEnglish ? "Secret door / passage" : "Тайная дверь / проход", true);

                int x = bounds.Left + 560;
                y = bounds.Top;
                graphics.DrawString(isEnglish ? "Wandering encounters" : "Блуждающие встречи", headerFont, textBrush, x, y);
                y += 34;
                foreach (string line in DungeonEncounterDisplayLines(dungeon))
                {
                    graphics.DrawString(line, textFont, textBrush, x, y);
                    y += 28;
                    if (y > bounds.Bottom - 30)
                    {
                        x += 620;
                        y = bounds.Top + 34;
                    }
                }
            }
        }

        private void DrawDungeonLegendLine(Graphics graphics, Font font, Brush textBrush, int x, int y, string label, bool dashed, bool secret)
        {
            using (Pen floor = new Pen(Color.FromArgb(202, 190, 147), 16f))
            using (Pen edge = new Pen(Color.FromArgb(104, 72, 40), 3f))
            {
                floor.StartCap = floor.EndCap = LineCap.Flat;
                edge.StartCap = edge.EndCap = LineCap.Flat;
                if (dashed || secret) floor.DashStyle = DashStyle.Dash;
                if (dashed) edge.DashStyle = DashStyle.Dot;
                graphics.DrawLine(floor, x, y + 10, x + 140, y + 10);
                graphics.DrawLine(edge, x, y + 10, x + 140, y + 10);
            }
            graphics.DrawString(label, font, textBrush, x + 160, y);
        }

        private void DrawDungeonLegendDoor(Graphics graphics, Font font, Brush textBrush, int x, int y, string label, bool secret)
        {
            RectangleF door = new RectangleF(x + 58, y + 2, 24, 14);
            using (Brush fill = new SolidBrush(secret ? Color.FromArgb(225, 120, 118, 104) : Color.WhiteSmoke))
            using (Pen outline = new Pen(Color.FromArgb(50, 38, 28), 2f))
            {
                if (secret) outline.DashStyle = DashStyle.Dash;
                graphics.FillRectangle(fill, door);
                graphics.DrawRectangle(outline, door.X, door.Y, door.Width, door.Height);
            }
            graphics.DrawString(label, font, textBrush, x + 160, y);
        }

        private string SafeDungeonFileName(string name)
        {
            string value = string.IsNullOrWhiteSpace(name) ? "dungeon" : name.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }

        private void ApplyDungeonHeaderFromUi()
        {
            if (currentDungeon == null) return;
            currentDungeon.Name = string.IsNullOrWhiteSpace(txtDungeonName.Text) ? currentDungeon.Name : txtDungeonName.Text.Trim();
            currentDungeon.DungeonType = SelectedDungeonItemValue(cmbDungeonType);
            currentDungeon.Size = SelectedDungeonItemValue(cmbDungeonSize);
            currentDungeon.RecommendedLevel = DungeonCatalog.ClampDungeonLevel((int)nudDungeonLevel.Value);
        }

        private void RefreshDungeonLevelView()
        {
            if (cmbDungeonLevelView == null) return;
            int selected = SelectedDungeonLevelNumber();
            cmbDungeonLevelView.Items.Clear();
            if (currentDungeon != null && currentDungeon.Levels != null)
            {
                foreach (DungeonLevelRecord level in currentDungeon.Levels.OrderBy(l => l.LevelNumber))
                {
                    cmbDungeonLevelView.Items.Add(new DungeonLevelItem(level.LevelNumber, isEnglish ? "Floor " + level.LevelNumber : "Этаж " + level.LevelNumber));
                }
            }
            if (cmbDungeonLevelView.Items.Count > 0)
            {
                int index = 0;
                for (int i = 0; i < cmbDungeonLevelView.Items.Count; i++)
                {
                    DungeonLevelItem item = cmbDungeonLevelView.Items[i] as DungeonLevelItem;
                    if (item != null && item.LevelNumber == selected) index = i;
                }
                cmbDungeonLevelView.SelectedIndex = index;
            }
        }

        private void RefreshDungeonRoomList()
        {
            if (lstDungeonRooms == null) return;
            string selectedId = (lstDungeonRooms.SelectedItem as DungeonRoomRecord)?.Id;
            lstDungeonRooms.Items.Clear();
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level != null && level.Rooms != null)
            {
                foreach (DungeonRoomRecord room in level.Rooms.OrderBy(r => r.Y).ThenBy(r => r.X))
                {
                    lstDungeonRooms.Items.Add(room);
                    if (!string.IsNullOrWhiteSpace(selectedId) && room.Id == selectedId) lstDungeonRooms.SelectedItem = room;
                }
            }
        }

        private string FormatDungeonRoomListItem(DungeonRoomRecord room)
        {
            if (room == null) return "";
            string title = DisplayRoomTitle(room);
            string kind = LocalizeRoomKind(room.Kind);
            return (isEnglish ? "L" : "У") + room.LevelNumber + " " + title + " [" + kind + "]";
        }

        private void RefreshDungeonEncounterText()
        {
            if (txtDungeonEncounters == null) return;
            if (currentDungeon == null || currentDungeon.WanderingEncounters == null)
            {
                txtDungeonEncounters.Text = "";
                return;
            }

            txtDungeonEncounters.Text = string.Join(Environment.NewLine, DungeonEncounterDisplayLines(currentDungeon));
        }

        private List<string> DungeonEncounterDisplayLines(DungeonRecord dungeon)
        {
            List<string> lines = new List<string>();
            if (dungeon == null || dungeon.WanderingEncounters == null) return lines;
            foreach (var group in dungeon.WanderingEncounters
                .Where(e => e != null)
                .OrderBy(e => e.DungeonLevel)
                .ThenBy(e => e.Roll)
                .GroupBy(e => e.DungeonLevel))
            {
                lines.Add(isEnglish
                    ? "Floor " + group.Key + ": d12 -> Monster Level -> encounter"
                    : "Этаж " + group.Key + ": d12 -> уровень монстров -> встреча");
                foreach (DungeonEncounterRecord encounter in group)
                {
                    int monsterLevel = encounter.MonsterLevel < 1 ? 1 : encounter.MonsterLevel;
                    lines.Add("  " + encounter.Roll + " -> ML" + monsterLevel + ": "
                        + encounter.CountExpression + " " + DisplayDungeonText(encounter.Monster));
                }
            }

            return lines;
        }

        private DungeonLevelRecord SelectedDungeonLevel()
        {
            int number = SelectedDungeonLevelNumber();
            return currentDungeon == null || currentDungeon.Levels == null
                ? null
                : currentDungeon.Levels.FirstOrDefault(l => l.LevelNumber == number);
        }

        private int SelectedDungeonLevelNumber()
        {
            DungeonLevelItem item = cmbDungeonLevelView == null ? null : cmbDungeonLevelView.SelectedItem as DungeonLevelItem;
            if (item != null) return item.LevelNumber;
            return currentDungeon == null || currentDungeon.Levels == null || currentDungeon.Levels.Count == 0
                ? 1
                : currentDungeon.Levels.Min(l => l.LevelNumber);
        }

        private void SelectDungeonLevelNumber(int number)
        {
            if (cmbDungeonLevelView == null) return;
            for (int i = 0; i < cmbDungeonLevelView.Items.Count; i++)
            {
                DungeonLevelItem item = cmbDungeonLevelView.Items[i] as DungeonLevelItem;
                if (item != null && item.LevelNumber == number)
                {
                    cmbDungeonLevelView.SelectedIndex = i;
                    return;
                }
            }
            if (cmbDungeonLevelView.Items.Count > 0) cmbDungeonLevelView.SelectedIndex = 0;
        }

        private void LoadSelectedDungeonRoomToEditor()
        {
            DungeonRoomRecord room = lstDungeonRooms == null ? null : lstDungeonRooms.SelectedItem as DungeonRoomRecord;
            if (room == null) return;
            dungeonSelectedDoor = null;
            dungeonSelectedConnection = null;
            dungeonSelectedPathPointIndex = -1;
            SelectRoomKind(room.Kind);
            SelectDungeonItem(cmbDungeonRoomShape, room.Shape);
            if (nudDungeonRoomWidth != null) nudDungeonRoomWidth.Value = Math.Max(nudDungeonRoomWidth.Minimum, Math.Min(nudDungeonRoomWidth.Maximum, room.Width));
            if (nudDungeonRoomHeight != null) nudDungeonRoomHeight.Value = Math.Max(nudDungeonRoomHeight.Minimum, Math.Min(nudDungeonRoomHeight.Maximum, room.Height));
            txtDungeonRoomTitle.Text = DisplayRoomTitle(room);
            txtDungeonRoomMonster.Text = DisplayDungeonText(room.Monster);
            txtDungeonRoomTreasure.Text = DisplayDungeonText(room.Treasure);
            txtDungeonRoomTrap.Text = DisplayDungeonText(room.Trap);
            txtDungeonRoomDetails.Text = DisplayDungeonText(room.Details);
            pnlDungeonMap?.Invalidate();
        }

        private void ApplyDungeonRoomEditor()
        {
            DungeonRoomRecord room = lstDungeonRooms == null ? null : lstDungeonRooms.SelectedItem as DungeonRoomRecord;
            if (room == null) return;
            DungeonLevelRecord level = SelectedDungeonLevel();
            int nextWidth = nudDungeonRoomWidth == null ? room.Width : (int)nudDungeonRoomWidth.Value;
            int nextHeight = nudDungeonRoomHeight == null ? room.Height : (int)nudDungeonRoomHeight.Value;
            DungeonRoomRecord probe = new DungeonRoomRecord
            {
                Id = room.Id,
                X = room.X,
                Y = room.Y,
                Width = Math.Max(1, nextWidth),
                Height = Math.Max(1, nextHeight)
            };
            if (level != null && !CanPlaceDungeonRoom(level, probe, room.X, room.Y, room.Id))
            {
                MessageBox.Show(
                    isEnglish ? "The room cannot be resized here: it would overlap another room or leave the level." : "Комнату нельзя изменить до такого размера: она налезет на другую комнату или выйдет за уровень.",
                    isEnglish ? "Dungeon room" : "Комната данжа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            List<DungeonDoorAnchor> anchors = CaptureDungeonDoorAnchors(level, room);
            room.Kind = SelectedDungeonItemValue(cmbDungeonRoomKind);
            if (string.IsNullOrWhiteSpace(room.Kind)) room.Kind = "Empty";
            room.Shape = SelectedDungeonItemValue(cmbDungeonRoomShape);
            if (string.IsNullOrWhiteSpace(room.Shape)) room.Shape = "Rectangle";
            room.Width = Math.Max(1, nextWidth);
            room.Height = Math.Max(1, nextHeight);
            room.Title = txtDungeonRoomTitle.Text.Trim();
            room.Monster = txtDungeonRoomMonster.Text.Trim();
            room.Treasure = txtDungeonRoomTreasure.Text.Trim();
            room.Trap = txtDungeonRoomTrap.Text.Trim();
            room.Details = txtDungeonRoomDetails.Text.Trim();
            RestoreDungeonDoorAnchors(level, room, anchors);
            RefreshDungeonRoomList();
            pnlDungeonMap?.Invalidate();
        }

        private void AddDungeonRoom()
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null) return;
            int width = nudDungeonRoomWidth == null ? 3 : (int)nudDungeonRoomWidth.Value;
            int height = nudDungeonRoomHeight == null ? 2 : (int)nudDungeonRoomHeight.Value;
            width = Math.Max(1, Math.Min(level.Width - 2, width));
            height = Math.Max(1, Math.Min(level.Height - 2, height));
            Point candidate = FindFreeDungeonRoomPosition(level, width, height, 1, 1, null);
            DungeonRoomRecord room = new DungeonRoomRecord
            {
                LevelNumber = level.LevelNumber,
                X = candidate.X,
                Y = candidate.Y,
                Width = width,
                Height = height,
                Shape = SelectedDungeonItemValue(cmbDungeonRoomShape),
                Title = isEnglish ? "New room" : "Новая комната",
                Kind = "Empty"
            };
            if (string.IsNullOrWhiteSpace(room.Shape)) room.Shape = "Rectangle";
            level.Rooms.Add(room);
            RefreshDungeonRoomList();
            lstDungeonRooms.SelectedItem = room;
            pnlDungeonMap?.Invalidate();
        }

        private void AddDungeonRoomAtPoint(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || dungeonRenderLayout == null) return;
            int x;
            int y;
            if (!TryViewPointToDungeonGrid(point, out x, out y)) return;

            DungeonRoomRecord room = new DungeonRoomRecord
            {
                LevelNumber = level.LevelNumber,
                X = Math.Max(1, Math.Min(level.Width - 4, x)),
                Y = Math.Max(1, Math.Min(level.Height - 3, y)),
                Width = nudDungeonRoomWidth == null ? 3 : (int)nudDungeonRoomWidth.Value,
                Height = nudDungeonRoomHeight == null ? 2 : (int)nudDungeonRoomHeight.Value,
                Shape = SelectedDungeonItemValue(cmbDungeonRoomShape),
                Kind = "Empty",
                Title = isEnglish ? "New room" : "Новая комната"
            };
            if (string.IsNullOrWhiteSpace(room.Shape)) room.Shape = "Rectangle";
            room.Width = Math.Max(1, Math.Min(level.Width - 2, room.Width));
            room.Height = Math.Max(1, Math.Min(level.Height - 2, room.Height));
            room.X = Math.Max(1, Math.Min(level.Width - room.Width - 1, room.X));
            room.Y = Math.Max(1, Math.Min(level.Height - room.Height - 1, room.Y));

            if (!CanPlaceDungeonRoom(level, room, room.X, room.Y, null))
            {
                Point free = FindFreeDungeonRoomPosition(level, room.Width, room.Height, room.X, room.Y, null);
                room.X = free.X;
                room.Y = free.Y;
            }

            level.Rooms.Add(room);
            RefreshDungeonRoomList();
            lstDungeonRooms.SelectedItem = room;
            pnlDungeonMap?.Invalidate();
        }

        private void RemoveSelectedDungeonRoom()
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            DungeonRoomRecord room = lstDungeonRooms == null ? null : lstDungeonRooms.SelectedItem as DungeonRoomRecord;
            if (level == null || room == null) return;
            level.Rooms.RemoveAll(r => r.Id == room.Id);
            if (level.Connections != null) level.Connections.RemoveAll(c => c.FromRoomId == room.Id || c.ToRoomId == room.Id);
            if (level.Doors != null) level.Doors.RemoveAll(d => d.FromRoomId == room.Id || d.ToRoomId == room.Id);
            ClearDungeonPlanSelection();
            RefreshDungeonRoomList();
            pnlDungeonMap?.Invalidate();
        }

        private void AddDungeonLevel()
        {
            if (currentDungeon == null) return;
            if (currentDungeon.Levels == null) currentDungeon.Levels = new List<DungeonLevelRecord>();
            int number = currentDungeon.Levels.Count == 0 ? 1 : currentDungeon.Levels.Max(l => l.LevelNumber) + 1;
            DungeonLevelRecord level = new DungeonLevelRecord
            {
                LevelNumber = number,
                Width = 24,
                Height = 18,
                Rooms = new List<DungeonRoomRecord>(),
                Connections = new List<DungeonConnectionRecord>(),
                Doors = new List<DungeonDoorRecord>()
            };
            DungeonRoomRecord entrance = new DungeonRoomRecord
            {
                LevelNumber = number,
                X = level.Width / 2 - 1,
                Y = level.Height / 2 - 1,
                Width = 3,
                Height = 2,
                Shape = "Rectangle",
                Kind = "Entrance",
                Title = isEnglish ? "Entrance" : "Вход",
                Details = isEnglish ? "Starting room for this dungeon floor." : "Начальная комната этого этажа данжа."
            };
            level.Rooms.Add(entrance);
            currentDungeon.Levels.Add(level);
            NormalizeDungeonForEditor(currentDungeon);
            number = level.LevelNumber;
            RefreshDungeonLevelView();
            SelectDungeonLevelNumber(number);
            RefreshDungeonRoomList();
            lstDungeonRooms.SelectedItem = entrance;
            pnlDungeonMap?.Invalidate();
        }

        private void RemoveSelectedDungeonLevel()
        {
            if (currentDungeon == null || currentDungeon.Levels == null || currentDungeon.Levels.Count <= 1) return;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null) return;
            int deletedLevelNumber = level.LevelNumber;
            currentDungeon.Levels.Remove(level);
            NormalizeDungeonForEditor(currentDungeon);
            int preferredPrevious = Math.Max(1, deletedLevelNumber - 1);
            int next = currentDungeon.Levels
                .Where(l => l.LevelNumber <= preferredPrevious)
                .OrderByDescending(l => l.LevelNumber)
                .Select(l => l.LevelNumber)
                .FirstOrDefault();
            if (next <= 0)
            {
                next = currentDungeon.Levels.OrderBy(l => l.LevelNumber).Select(l => l.LevelNumber).FirstOrDefault();
            }
            RefreshDungeonLevelView();
            SelectDungeonLevelNumber(next);
            RefreshDungeonRoomList();
            pnlDungeonMap?.Invalidate();
        }

        private void SelectDungeonRoomAtPoint(Point point)
        {
            DungeonRoomRecord room = FindDungeonRoomAtPoint(point);
            if (room != null) lstDungeonRooms.SelectedItem = room;
            else if (lstDungeonRooms != null) lstDungeonRooms.ClearSelected();
        }

        private void DungeonMap_MouseDown(object sender, MouseEventArgs e)
        {
            pnlDungeonMap.Focus();
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                dungeonPanning = true;
                dungeonPanLastPoint = e.Location;
                pnlDungeonMap.Cursor = Cursors.Hand;
                return;
            }

            if (e.Button != MouseButtons.Left) return;
            DungeonRoomRecord room = FindDungeonRoomAtPoint(e.Location);

            if (dungeonEditorTool == DungeonEditorTool.Connect)
            {
                dungeonSelectedDoor = null;
                dungeonSelectedConnection = null;
                HandleDungeonConnectClick(room, e.Location);
                return;
            }

            if (dungeonEditorTool == DungeonEditorTool.Disconnect)
            {
                if (TryRemoveDungeonDoorAtPoint(e.Location)) return;
                if (TryRemoveDungeonConnectionAtPoint(e.Location)) return;
                HandleDungeonDisconnectClick(room);
                return;
            }

            if (dungeonEditorTool == DungeonEditorTool.PlaceDoor)
            {
                AddDungeonDoorAtPoint(e.Location);
                return;
            }

            if (dungeonEditorTool == DungeonEditorTool.AddRoom)
            {
                if (room == null) AddDungeonRoomAtPoint(e.Location);
                else lstDungeonRooms.SelectedItem = room;
                return;
            }

            DungeonDoorRecord door = FindDungeonDoorAtPoint(e.Location);
            if (door != null)
            {
                ClearDungeonPlanSelection();
                dungeonSelectedDoor = door;
                if (lstDungeonRooms != null) lstDungeonRooms.ClearSelected();
                dungeonDragDoor = door;
                dungeonDragStartPoint = e.Location;
                dungeonDragDoorStartX = door.X;
                dungeonDragDoorStartY = door.Y;
                dungeonDoorDragging = true;
                pnlDungeonMap.Cursor = Cursors.SizeAll;
                pnlDungeonMap.Invalidate();
                return;
            }

            DungeonConnectionRecord handleConnection;
            int handleIndex;
            if (TryFindDungeonConnectionHandleAtPoint(e.Location, out handleConnection, out handleIndex))
            {
                ClearDungeonPlanSelection();
                dungeonSelectedConnection = handleConnection;
                dungeonSelectedPathPointIndex = handleIndex;
                dungeonDragConnection = handleConnection;
                dungeonDragPathPointIndex = handleIndex;
                dungeonPathPointDragging = true;
                pnlDungeonMap.Cursor = Cursors.SizeAll;
                pnlDungeonMap.Invalidate();
                return;
            }

            DungeonConnectionRecord connection;
            PointF nearestGrid;
            int insertIndex;
            if (TryFindDungeonConnectionHit(e.Location, out connection, out nearestGrid, out insertIndex))
            {
                ClearDungeonPlanSelection();
                dungeonSelectedConnection = connection;
                dungeonPendingConnectionDrag = connection;
                dungeonPendingConnectionGridPoint = nearestGrid;
                dungeonPendingConnectionInsertIndex = insertIndex;
                dungeonDragStartPoint = e.Location;
                pnlDungeonMap.Invalidate();
                return;
            }

            DungeonRoomRecord resizeRoom;
            DungeonRoomResizeHandle resizeHandle;
            if (TryFindDungeonRoomResizeHandleAtPoint(e.Location, out resizeRoom, out resizeHandle))
            {
                DungeonLevelRecord level = SelectedDungeonLevel();
                ClearDungeonPlanSelection();
                lstDungeonRooms.SelectedItem = resizeRoom;
                dungeonResizeRoom = resizeRoom;
                dungeonResizeHandle = resizeHandle;
                dungeonDragStartPoint = e.Location;
                dungeonDragStartX = resizeRoom.X;
                dungeonDragStartY = resizeRoom.Y;
                dungeonDragStartWidth = resizeRoom.Width;
                dungeonDragStartHeight = resizeRoom.Height;
                dungeonRoomDoorAnchors.Clear();
                dungeonRoomDoorAnchors.AddRange(CaptureDungeonDoorAnchors(level, resizeRoom));
                dungeonResizing = true;
                pnlDungeonMap.Cursor = CursorForDungeonResizeHandle(resizeHandle);
                pnlDungeonMap.Invalidate();
                return;
            }

            if (room == null)
            {
                ClearDungeonPlanSelection();
                if (lstDungeonRooms != null) lstDungeonRooms.ClearSelected();
                pnlDungeonMap.Invalidate();
                return;
            }
            ClearDungeonPlanSelection();
            lstDungeonRooms.SelectedItem = room;
            dungeonDragRoom = room;
            dungeonDragStartPoint = e.Location;
            dungeonDragStartX = room.X;
            dungeonDragStartY = room.Y;
            dungeonDragStartWidth = room.Width;
            dungeonDragStartHeight = room.Height;
            dungeonRoomDoorAnchors.Clear();
            dungeonRoomDoorAnchors.AddRange(CaptureDungeonDoorAnchors(SelectedDungeonLevel(), room));
            dungeonDragging = true;
            pnlDungeonMap.Cursor = Cursors.SizeAll;
        }

        private void DungeonMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (dungeonPanning)
            {
                dungeonPanOffset.X += e.X - dungeonPanLastPoint.X;
                dungeonPanOffset.Y += e.Y - dungeonPanLastPoint.Y;
                dungeonPanLastPoint = e.Location;
                pnlDungeonMap.Invalidate();
                return;
            }

            if (dungeonDoorDragging && dungeonDragDoor != null)
            {
                DungeonConnectionRecord connection;
                PointF screenPoint;
                string orientation;
                if (TryFindDungeonDoorPlacement(e.Location, out connection, out screenPoint, out orientation))
                {
                    ApplyDungeonDoorPlacement(dungeonDragDoor, connection, screenPoint, orientation);
                    pnlDungeonMap.Invalidate();
                }
                return;
            }

            if (dungeonPathPointDragging && dungeonDragConnection != null && dungeonDragPathPointIndex >= 0)
            {
                UpdateDungeonConnectionPathPoint(dungeonDragConnection, dungeonDragPathPointIndex, e.Location);
                pnlDungeonMap.Invalidate();
                return;
            }

            if (dungeonPendingConnectionDrag != null)
            {
                int dxScreen = e.Location.X - dungeonDragStartPoint.X;
                int dyScreen = e.Location.Y - dungeonDragStartPoint.Y;
                if (dxScreen * dxScreen + dyScreen * dyScreen > 16)
                {
                    if (dungeonPendingConnectionDrag.PathPoints == null)
                    {
                        dungeonPendingConnectionDrag.PathPoints = new List<DungeonPathPointRecord>();
                    }

                    int index = Math.Max(0, Math.Min(dungeonPendingConnectionInsertIndex, dungeonPendingConnectionDrag.PathPoints.Count));
                    dungeonPendingConnectionDrag.PathPoints.Insert(index, new DungeonPathPointRecord
                    {
                        X = dungeonPendingConnectionGridPoint.X,
                        Y = dungeonPendingConnectionGridPoint.Y
                    });
                    dungeonSelectedConnection = dungeonPendingConnectionDrag;
                    dungeonSelectedPathPointIndex = index;
                    dungeonDragConnection = dungeonPendingConnectionDrag;
                    dungeonDragPathPointIndex = index;
                    dungeonPathPointDragging = true;
                    dungeonPendingConnectionDrag = null;
                    dungeonPendingConnectionInsertIndex = -1;
                    UpdateDungeonConnectionPathPoint(dungeonDragConnection, dungeonDragPathPointIndex, e.Location);
                    pnlDungeonMap.Cursor = Cursors.SizeAll;
                    pnlDungeonMap.Invalidate();
                }
                return;
            }

            if (dungeonResizing && dungeonResizeRoom != null)
            {
                ResizeDungeonRoomWithMouse(e.Location);
                return;
            }

            if (!dungeonDragging || dungeonDragRoom == null || dungeonRenderLayout == null)
            {
                UpdateDungeonMapHoverCursor(e.Location);
                return;
            }
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null) return;

            int dx = (int)Math.Round((e.Location.X - dungeonDragStartPoint.X) / dungeonRenderLayout.Scale);
            int dy = (int)Math.Round((e.Location.Y - dungeonDragStartPoint.Y) / dungeonRenderLayout.Scale);
            int nextX = Math.Max(1, Math.Min(level.Width - dungeonDragRoom.Width - 1, dungeonDragStartX + dx));
            int nextY = Math.Max(1, Math.Min(level.Height - dungeonDragRoom.Height - 1, dungeonDragStartY + dy));
            if (nextX == dungeonDragRoom.X && nextY == dungeonDragRoom.Y) return;
            if (!CanPlaceDungeonRoom(level, dungeonDragRoom, nextX, nextY, dungeonDragRoom.Id)) return;

            ApplyDungeonRoomGeometry(level, dungeonDragRoom, nextX, nextY, dungeonDragRoom.Width, dungeonDragRoom.Height, dungeonRoomDoorAnchors);
            pnlDungeonMap.Invalidate();
        }

        private void DungeonMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (dungeonPanning)
            {
                dungeonPanning = false;
                pnlDungeonMap.Cursor = Cursors.Default;
                return;
            }

            if (dungeonDoorDragging)
            {
                dungeonDoorDragging = false;
                dungeonDragDoor = null;
                pnlDungeonMap.Cursor = Cursors.Default;
                return;
            }

            if (dungeonPathPointDragging)
            {
                dungeonPathPointDragging = false;
                dungeonDragConnection = null;
                dungeonDragPathPointIndex = -1;
                DungeonLevelRecord level = SelectedDungeonLevel();
                if (level != null)
                {
                    NormalizeDungeonLevelConnectionPaths(level);
                }
                pnlDungeonMap.Cursor = Cursors.Default;
                return;
            }

            dungeonPendingConnectionDrag = null;
            dungeonPendingConnectionInsertIndex = -1;

            if (dungeonResizing)
            {
                dungeonResizing = false;
                dungeonResizeRoom = null;
                dungeonResizeHandle = DungeonRoomResizeHandle.None;
                dungeonRoomDoorAnchors.Clear();
                pnlDungeonMap.Cursor = Cursors.Default;
                RefreshDungeonRoomList();
                return;
            }

            if (!dungeonDragging) return;
            dungeonDragging = false;
            dungeonDragRoom = null;
            dungeonRoomDoorAnchors.Clear();
            pnlDungeonMap.Cursor = Cursors.Default;
            RefreshDungeonRoomList();
        }

        private void DungeonMap_MouseWheel(object sender, MouseEventArgs e)
        {
            ZoomDungeonMap(e.Delta > 0 ? 1.12f : 0.89f, e.Location);
        }

        private void DungeonMap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (RemoveSelectedDungeonPlanObject())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private bool RemoveSelectedDungeonPlanObject()
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null) return false;

            if (dungeonSelectedDoor != null && level.Doors != null)
            {
                string id = dungeonSelectedDoor.Id;
                if (level.Doors.RemoveAll(d => d != null && d.Id == id) > 0)
                {
                    dungeonSelectedDoor = null;
                    pnlDungeonMap?.Invalidate();
                    return true;
                }
            }

            if (dungeonSelectedConnection != null && level.Connections != null)
            {
                if (dungeonSelectedPathPointIndex >= 0
                    && dungeonSelectedConnection.PathPoints != null
                    && dungeonSelectedPathPointIndex < dungeonSelectedConnection.PathPoints.Count)
                {
                    dungeonSelectedConnection.PathPoints.RemoveAt(dungeonSelectedPathPointIndex);
                    dungeonSelectedPathPointIndex = -1;
                    NormalizeDungeonLevelConnectionPaths(level);
                    pnlDungeonMap?.Invalidate();
                    return true;
                }

                DungeonConnectionRecord connection = dungeonSelectedConnection;
                if (level.Connections.Remove(connection))
                {
                    if (level.Doors != null)
                    {
                        level.Doors.RemoveAll(d => d != null && SameDungeonDoorConnection(d, connection.FromRoomId, connection.ToRoomId));
                    }
                    dungeonSelectedConnection = null;
                    pnlDungeonMap?.Invalidate();
                    return true;
                }
            }

            if (lstDungeonRooms == null || lstDungeonRooms.SelectedItem == null) return false;
            RemoveSelectedDungeonRoom();
            return true;
        }

        private DungeonRoomRecord FindDungeonRoomAtPoint(Point point)
        {
            foreach (KeyValuePair<string, RectangleF> pair in dungeonRoomBounds.OrderBy(p => p.Value.Width * p.Value.Height))
            {
                if (!pair.Value.Contains(point)) continue;
                DungeonLevelRecord level = SelectedDungeonLevel();
                return level == null ? null : level.Rooms.FirstOrDefault(r => r.Id == pair.Key);
            }

            return null;
        }

        private void DrawDungeonRoomResizeHandles(Graphics graphics, RectangleF roomBounds, float scale)
        {
            foreach (DungeonRoomResizeHandle handle in DungeonRoomResizeHandles())
            {
                RectangleF bounds = DungeonRoomResizeHandleBounds(roomBounds, handle, scale);
                using (Brush fill = new SolidBrush(Color.FromArgb(255, 245, 206)))
                using (Pen outline = new Pen(Color.FromArgb(80, 58, 30), Math.Max(1.2f, scale * 0.045f)))
                {
                    graphics.FillRectangle(fill, bounds);
                    graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
            }
        }

        private bool TryFindDungeonRoomResizeHandleAtPoint(Point point, out DungeonRoomRecord room, out DungeonRoomResizeHandle handle)
        {
            room = null;
            handle = DungeonRoomResizeHandle.None;
            if (dungeonEditorTool != DungeonEditorTool.SelectMove) return false;
            DungeonRoomRecord selected = lstDungeonRooms == null ? null : lstDungeonRooms.SelectedItem as DungeonRoomRecord;
            if (selected == null) return false;
            RectangleF roomBounds;
            if (!dungeonRoomBounds.TryGetValue(selected.Id, out roomBounds)) return false;

            foreach (DungeonRoomResizeHandle candidate in DungeonRoomResizeHandles())
            {
                RectangleF bounds = DungeonRoomResizeHandleBounds(roomBounds, candidate, dungeonRenderLayout == null ? 10f : dungeonRenderLayout.Scale);
                bounds.Inflate(4f, 4f);
                if (!bounds.Contains(point)) continue;
                room = selected;
                handle = candidate;
                return true;
            }

            return false;
        }

        private DungeonRoomResizeHandle[] DungeonRoomResizeHandles()
        {
            return new[]
            {
                DungeonRoomResizeHandle.TopLeft,
                DungeonRoomResizeHandle.TopRight,
                DungeonRoomResizeHandle.BottomLeft,
                DungeonRoomResizeHandle.BottomRight,
                DungeonRoomResizeHandle.Left,
                DungeonRoomResizeHandle.Right,
                DungeonRoomResizeHandle.Top,
                DungeonRoomResizeHandle.Bottom
            };
        }

        private RectangleF DungeonRoomResizeHandleBounds(RectangleF roomBounds, DungeonRoomResizeHandle handle, float scale)
        {
            float size = Math.Max(8f, Math.Min(16f, scale * 0.36f));
            float x = roomBounds.Left + roomBounds.Width / 2f;
            float y = roomBounds.Top + roomBounds.Height / 2f;

            if (handle == DungeonRoomResizeHandle.Left || handle == DungeonRoomResizeHandle.TopLeft || handle == DungeonRoomResizeHandle.BottomLeft)
            {
                x = roomBounds.Left;
            }
            else if (handle == DungeonRoomResizeHandle.Right || handle == DungeonRoomResizeHandle.TopRight || handle == DungeonRoomResizeHandle.BottomRight)
            {
                x = roomBounds.Right;
            }

            if (handle == DungeonRoomResizeHandle.Top || handle == DungeonRoomResizeHandle.TopLeft || handle == DungeonRoomResizeHandle.TopRight)
            {
                y = roomBounds.Top;
            }
            else if (handle == DungeonRoomResizeHandle.Bottom || handle == DungeonRoomResizeHandle.BottomLeft || handle == DungeonRoomResizeHandle.BottomRight)
            {
                y = roomBounds.Bottom;
            }

            return new RectangleF(x - size / 2f, y - size / 2f, size, size);
        }

        private Cursor CursorForDungeonResizeHandle(DungeonRoomResizeHandle handle)
        {
            if (handle == DungeonRoomResizeHandle.Left || handle == DungeonRoomResizeHandle.Right) return Cursors.SizeWE;
            if (handle == DungeonRoomResizeHandle.Top || handle == DungeonRoomResizeHandle.Bottom) return Cursors.SizeNS;
            if (handle == DungeonRoomResizeHandle.TopLeft || handle == DungeonRoomResizeHandle.BottomRight) return Cursors.SizeNWSE;
            if (handle == DungeonRoomResizeHandle.TopRight || handle == DungeonRoomResizeHandle.BottomLeft) return Cursors.SizeNESW;
            return Cursors.Default;
        }

        private void UpdateDungeonMapHoverCursor(Point point)
        {
            if (pnlDungeonMap == null || dungeonPanning || dungeonDoorDragging || dungeonPathPointDragging || dungeonDragging || dungeonResizing) return;
            DungeonRoomRecord room;
            DungeonRoomResizeHandle handle;
            pnlDungeonMap.Cursor = TryFindDungeonRoomResizeHandleAtPoint(point, out room, out handle)
                ? CursorForDungeonResizeHandle(handle)
                : Cursors.Default;
        }

        private void ResizeDungeonRoomWithMouse(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || dungeonResizeRoom == null || dungeonRenderLayout == null) return;

            int dx = (int)Math.Round((point.X - dungeonDragStartPoint.X) / dungeonRenderLayout.Scale);
            int dy = (int)Math.Round((point.Y - dungeonDragStartPoint.Y) / dungeonRenderLayout.Scale);
            int nextX = dungeonDragStartX;
            int nextY = dungeonDragStartY;
            int nextWidth = dungeonDragStartWidth;
            int nextHeight = dungeonDragStartHeight;
            int startRight = dungeonDragStartX + dungeonDragStartWidth;
            int startBottom = dungeonDragStartY + dungeonDragStartHeight;

            if (dungeonResizeHandle == DungeonRoomResizeHandle.Left
                || dungeonResizeHandle == DungeonRoomResizeHandle.TopLeft
                || dungeonResizeHandle == DungeonRoomResizeHandle.BottomLeft)
            {
                nextX = Math.Min(startRight - 1, Math.Max(1, dungeonDragStartX + dx));
                nextWidth = startRight - nextX;
            }
            else if (dungeonResizeHandle == DungeonRoomResizeHandle.Right
                || dungeonResizeHandle == DungeonRoomResizeHandle.TopRight
                || dungeonResizeHandle == DungeonRoomResizeHandle.BottomRight)
            {
                nextWidth = Math.Max(1, dungeonDragStartWidth + dx);
            }

            if (dungeonResizeHandle == DungeonRoomResizeHandle.Top
                || dungeonResizeHandle == DungeonRoomResizeHandle.TopLeft
                || dungeonResizeHandle == DungeonRoomResizeHandle.TopRight)
            {
                nextY = Math.Min(startBottom - 1, Math.Max(1, dungeonDragStartY + dy));
                nextHeight = startBottom - nextY;
            }
            else if (dungeonResizeHandle == DungeonRoomResizeHandle.Bottom
                || dungeonResizeHandle == DungeonRoomResizeHandle.BottomLeft
                || dungeonResizeHandle == DungeonRoomResizeHandle.BottomRight)
            {
                nextHeight = Math.Max(1, dungeonDragStartHeight + dy);
            }

            nextX = Math.Max(1, Math.Min(Math.Max(1, level.Width - 2), nextX));
            nextY = Math.Max(1, Math.Min(Math.Max(1, level.Height - 2), nextY));
            nextWidth = Math.Max(1, Math.Min(Math.Max(1, level.Width - nextX - 1), nextWidth));
            nextHeight = Math.Max(1, Math.Min(Math.Max(1, level.Height - nextY - 1), nextHeight));
            if (nextX == dungeonResizeRoom.X
                && nextY == dungeonResizeRoom.Y
                && nextWidth == dungeonResizeRoom.Width
                && nextHeight == dungeonResizeRoom.Height)
            {
                return;
            }

            DungeonRoomRecord probe = new DungeonRoomRecord { Width = nextWidth, Height = nextHeight };
            if (!CanPlaceDungeonRoom(level, probe, nextX, nextY, dungeonResizeRoom.Id)) return;

            ApplyDungeonRoomGeometry(level, dungeonResizeRoom, nextX, nextY, nextWidth, nextHeight, dungeonRoomDoorAnchors);
            pnlDungeonMap.Invalidate();
        }

        private void ApplyDungeonRoomGeometry(DungeonLevelRecord level, DungeonRoomRecord room, int x, int y, int width, int height, List<DungeonDoorAnchor> anchors)
        {
            if (room == null) return;
            room.X = x;
            room.Y = y;
            room.Width = Math.Max(1, width);
            room.Height = Math.Max(1, height);
            RestoreDungeonDoorAnchors(level, room, anchors);
            NormalizeDungeonLevelConnectionPaths(level);
            SnapDungeonDoorsForLevel(level);
            SyncDungeonRoomSizeControls(room);
        }

        private void SyncDungeonRoomSizeControls(DungeonRoomRecord room)
        {
            if (room == null) return;
            if (nudDungeonRoomWidth != null)
            {
                nudDungeonRoomWidth.Value = Math.Max(nudDungeonRoomWidth.Minimum, Math.Min(nudDungeonRoomWidth.Maximum, room.Width));
            }

            if (nudDungeonRoomHeight != null)
            {
                nudDungeonRoomHeight.Value = Math.Max(nudDungeonRoomHeight.Minimum, Math.Min(nudDungeonRoomHeight.Maximum, room.Height));
            }
        }

        private List<DungeonDoorAnchor> CaptureDungeonDoorAnchors(DungeonLevelRecord level, DungeonRoomRecord room)
        {
            List<DungeonDoorAnchor> anchors = new List<DungeonDoorAnchor>();
            if (level == null || room == null || level.Doors == null) return anchors;

            foreach (DungeonDoorRecord door in level.Doors)
            {
                if (door == null) continue;
                bool touchesRoom = string.Equals(door.FromRoomId, room.Id, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(door.ToRoomId, room.Id, StringComparison.OrdinalIgnoreCase);
                if (!touchesRoom) continue;

                DungeonConnectionRecord connection = level.Connections == null
                    ? null
                    : level.Connections.FirstOrDefault(c => SameDungeonDoorConnection(door, c.FromRoomId, c.ToRoomId));
                string wall = DungeonRoomWallForDoor(door, room);
                anchors.Add(new DungeonDoorAnchor
                {
                    Door = door,
                    Connection = connection,
                    RelativeX = door.X - room.X,
                    RelativeY = door.Y - room.Y,
                    RoomOnly = string.IsNullOrWhiteSpace(door.ToRoomId),
                    OnRoomBoundary = !string.IsNullOrWhiteSpace(wall),
                    RoomWall = wall
                });
            }

            return anchors;
        }

        private void RestoreDungeonDoorAnchors(DungeonLevelRecord level, DungeonRoomRecord room, List<DungeonDoorAnchor> anchors)
        {
            if (level == null || room == null || anchors == null || anchors.Count == 0) return;
            foreach (DungeonDoorAnchor anchor in anchors)
            {
                if (anchor == null || anchor.Door == null) continue;
                if (level.Doors != null && !level.Doors.Any(d => d != null && d.Id == anchor.Door.Id)) continue;

                if (anchor.RoomOnly || string.IsNullOrWhiteSpace(anchor.Door.ToRoomId))
                {
                    MoveDungeonDoorToStoredRoomWall(anchor.Door, room, anchor);
                    continue;
                }

                if (anchor.OnRoomBoundary)
                {
                    NormalizeDungeonConnectionPathPoints(level, anchor.Connection);
                    SnapDungeonDoorToConnection(level, anchor.Door, anchor.Connection);
                    continue;
                }

                SnapDungeonDoorToConnection(level, anchor.Door, anchor.Connection);
            }
        }

        private string DungeonRoomWallForDoor(DungeonDoorRecord door, DungeonRoomRecord room)
        {
            if (door == null || room == null) return "";
            const double epsilon = 0.01;
            bool xWithin = door.X >= room.X - epsilon && door.X <= room.X + room.Width + epsilon;
            bool yWithin = door.Y >= room.Y - epsilon && door.Y <= room.Y + room.Height + epsilon;
            if (Math.Abs(door.X - room.X) <= epsilon && yWithin) return "Left";
            if (Math.Abs(door.X - (room.X + room.Width)) <= epsilon && yWithin) return "Right";
            if (Math.Abs(door.Y - room.Y) <= epsilon && xWithin) return "Top";
            if (Math.Abs(door.Y - (room.Y + room.Height)) <= epsilon && xWithin) return "Bottom";
            return "";
        }

        private void MoveDungeonDoorToStoredRoomWall(DungeonDoorRecord door, DungeonRoomRecord room, DungeonDoorAnchor anchor)
        {
            if (door == null || room == null || anchor == null) return;
            if (anchor.RoomWall == "Left" || anchor.RoomWall == "Right")
            {
                door.X = anchor.RoomWall == "Left" ? room.X : room.X + room.Width;
                door.Y = ClampDouble(room.Y + anchor.RelativeY, room.Y, room.Y + room.Height);
                door.Orientation = "Vertical";
                return;
            }

            if (anchor.RoomWall == "Top" || anchor.RoomWall == "Bottom")
            {
                door.X = ClampDouble(room.X + anchor.RelativeX, room.X, room.X + room.Width);
                door.Y = anchor.RoomWall == "Top" ? room.Y : room.Y + room.Height;
                door.Orientation = "Horizontal";
                return;
            }

            door.X = room.X + anchor.RelativeX;
            door.Y = room.Y + anchor.RelativeY;
            MoveDungeonDoorToNearestRoomWall(door, room);
        }

        private void MoveDungeonDoorToRoomEdgeForConnection(DungeonLevelRecord level, DungeonDoorRecord door, DungeonRoomRecord room, DungeonConnectionRecord connection)
        {
            if (level == null || door == null || room == null || connection == null) return;
            double targetX = room.X + room.Width / 2.0;
            double targetY = room.Y + room.Height / 2.0;
            if (connection.PathPoints != null && connection.PathPoints.Count > 0)
            {
                DungeonPathPointRecord point = string.Equals(connection.FromRoomId, room.Id, StringComparison.OrdinalIgnoreCase)
                    ? connection.PathPoints.First()
                    : connection.PathPoints.Last();
                targetX = point.X;
                targetY = point.Y;
            }
            else if (level.Rooms != null)
            {
                string otherId = string.Equals(connection.FromRoomId, room.Id, StringComparison.OrdinalIgnoreCase)
                    ? connection.ToRoomId
                    : connection.FromRoomId;
                DungeonRoomRecord other = level.Rooms.FirstOrDefault(r => r != null && r.Id == otherId);
                if (other != null)
                {
                    targetX = other.X + other.Width / 2.0;
                    targetY = other.Y + other.Height / 2.0;
                }
            }

            double x;
            double y;
            string orientation;
            FindDungeonDoorPointOnRoomEdge(room, targetX, targetY, out x, out y, out orientation);
            door.X = x;
            door.Y = y;
            door.Orientation = orientation;
        }

        private void SnapDungeonDoorToConnection(DungeonDoorRecord door, DungeonConnectionRecord connection)
        {
            SnapDungeonDoorToConnection(SelectedDungeonLevel(), door, connection);
        }

        private void SnapDungeonDoorToConnection(DungeonLevelRecord level, DungeonDoorRecord door, DungeonConnectionRecord connection)
        {
            if (door == null || connection == null || level == null || level.Rooms == null) return;
            DungeonPathPointRecord nearest;
            DungeonPathPointRecord segmentA;
            DungeonPathPointRecord segmentB;
            double distance;
            if (!TryNearestLegalDungeonConnectionGridPoint(level, connection, new PointF((float)door.X, (float)door.Y), out nearest, out segmentA, out segmentB, out distance)) return;
            ApplyDungeonDoorGridPlacement(door, connection, nearest, DungeonDoorOrientationForGridSegment(segmentA, segmentB, false));
        }

        private void SnapDungeonDoorsForLevel(DungeonLevelRecord level)
        {
            if (level == null || level.Doors == null) return;
            foreach (DungeonConnectionRecord connection in DungeonConnectionSnapshot(level))
            {
                SnapDungeonDoorsToConnection(level, connection);
            }

            foreach (DungeonDoorRecord door in level.Doors)
            {
                if (door == null) continue;
                if (!string.IsNullOrWhiteSpace(door.ToRoomId)) continue;
                DungeonRoomRecord room = level.Rooms == null
                    ? null
                    : level.Rooms.FirstOrDefault(r => r != null && r.Id == door.FromRoomId);
                if (room == null) continue;
                if (!string.IsNullOrWhiteSpace(DungeonRoomWallForDoor(door, room))
                    && !DungeonGeometry.IsPointInsideRoomInterior(room, door.X, door.Y, 0.04))
                {
                    continue;
                }

                MoveDungeonDoorToNearestRoomWall(door, room);
            }
        }

        private void MoveDungeonDoorToNearestRoomWall(DungeonDoorRecord door, DungeonRoomRecord room)
        {
            if (door == null || room == null) return;
            double left = Math.Abs(door.X - room.X);
            double right = Math.Abs(door.X - (room.X + room.Width));
            double top = Math.Abs(door.Y - room.Y);
            double bottom = Math.Abs(door.Y - (room.Y + room.Height));
            double best = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            if (best == left)
            {
                door.X = room.X;
                door.Y = ClampDouble(door.Y, room.Y, room.Y + room.Height);
                door.Orientation = "Vertical";
            }
            else if (best == right)
            {
                door.X = room.X + room.Width;
                door.Y = ClampDouble(door.Y, room.Y, room.Y + room.Height);
                door.Orientation = "Vertical";
            }
            else if (best == top)
            {
                door.X = ClampDouble(door.X, room.X, room.X + room.Width);
                door.Y = room.Y;
                door.Orientation = "Horizontal";
            }
            else
            {
                door.X = ClampDouble(door.X, room.X, room.X + room.Width);
                door.Y = room.Y + room.Height;
                door.Orientation = "Horizontal";
            }
        }

        private void FindDungeonDoorPointOnRoomEdge(DungeonRoomRecord room, double targetX, double targetY, out double x, out double y, out string orientation)
        {
            DungeonGeometry.FindRoomEdgePoint(room, targetX, targetY, out x, out y, out orientation);
        }

        private double ClampDouble(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void HandleDungeonConnectClick(DungeonRoomRecord room, Point point)
        {
            if (room == null)
            {
                AddOrExtendFreeDungeonConnection(point);
                return;
            }

            dungeonFreeConnectionStartPoint = null;
            if (dungeonConnectionStartRoom == null)
            {
                dungeonConnectionStartRoom = room;
                lstDungeonRooms.SelectedItem = room;
                pnlDungeonMap.Invalidate();
                return;
            }

            if (room.Id != dungeonConnectionStartRoom.Id)
            {
                DungeonLevelRecord level = SelectedDungeonLevel();
                if (level != null)
                {
                    AddDungeonConnection(level, dungeonConnectionStartRoom.Id, room.Id);
                }
            }

            dungeonConnectionStartRoom = null;
            pnlDungeonMap.Invalidate();
        }

        private void AddOrExtendFreeDungeonConnection(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || dungeonRenderLayout == null) return;
            if (level.Connections == null) level.Connections = new List<DungeonConnectionRecord>();

            PointF grid = ScreenPointToDungeonGrid(point, true);
            DungeonConnectionRecord snappedConnection;
            PointF snappedGrid;
            int snappedInsertIndex;
            if (TryFindDungeonConnectionHit(point, out snappedConnection, out snappedGrid, out snappedInsertIndex))
            {
                grid = snappedGrid;
            }

            DungeonPathPointRecord nextPoint = new DungeonPathPointRecord { X = grid.X, Y = grid.Y };
            if (dungeonFreeConnectionStartPoint == null)
            {
                dungeonFreeConnectionStartPoint = nextPoint;
                if (lstDungeonRooms != null) lstDungeonRooms.ClearSelected();
                pnlDungeonMap.Invalidate();
                return;
            }

            if (AlmostSame(dungeonFreeConnectionStartPoint.X, nextPoint.X)
                && AlmostSame(dungeonFreeConnectionStartPoint.Y, nextPoint.Y))
            {
                return;
            }

            string kind = SelectedDungeonItemValue(cmbDungeonPassageKind);
            if (string.IsNullOrWhiteSpace(kind)) kind = "Corridor";
            DungeonConnectionRecord connection = new DungeonConnectionRecord
            {
                FromRoomId = "free:" + Guid.NewGuid().ToString("N") + ":a",
                ToRoomId = "free:" + Guid.NewGuid().ToString("N") + ":b",
                Kind = kind,
                PassageWidth = nudDungeonPassageWidth == null ? 1 : (int)nudDungeonPassageWidth.Value,
                DoorKind = "",
                PathPoints = CleanDungeonPathPoints(new List<DungeonPathPointRecord>
                {
                    dungeonFreeConnectionStartPoint,
                    nextPoint
                })
            };

            level.Connections.Add(connection);
            dungeonSelectedConnection = connection;
            dungeonSelectedPathPointIndex = connection.PathPoints == null ? -1 : Math.Max(0, connection.PathPoints.Count - 1);
            dungeonFreeConnectionStartPoint = nextPoint;
            pnlDungeonMap.Invalidate();
        }

        private void HandleDungeonDisconnectClick(DungeonRoomRecord room)
        {
            if (room == null) return;
            if (dungeonDisconnectStartRoom == null)
            {
                dungeonDisconnectStartRoom = room;
                lstDungeonRooms.SelectedItem = room;
                pnlDungeonMap.Invalidate();
                return;
            }

            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level != null && level.Connections != null)
            {
                level.Connections.RemoveAll(c => SameDungeonConnection(c, dungeonDisconnectStartRoom.Id, room.Id));
            }

            dungeonDisconnectStartRoom = null;
            pnlDungeonMap.Invalidate();
        }

        private void AddDungeonConnection(DungeonLevelRecord level, string fromRoomId, string toRoomId)
        {
            if (level == null || string.IsNullOrWhiteSpace(fromRoomId) || string.IsNullOrWhiteSpace(toRoomId)) return;
            if (fromRoomId == toRoomId) return;
            if (level.Connections == null) level.Connections = new List<DungeonConnectionRecord>();
            if (level.Connections.Any(c => SameDungeonConnection(c, fromRoomId, toRoomId))) return;

            string kind = SelectedDungeonItemValue(cmbDungeonPassageKind);
            if (string.IsNullOrWhiteSpace(kind)) kind = "Corridor";

            DungeonConnectionRecord connection = new DungeonConnectionRecord
            {
                FromRoomId = fromRoomId,
                ToRoomId = toRoomId,
                Kind = kind,
                PassageWidth = nudDungeonPassageWidth == null ? 1 : (int)nudDungeonPassageWidth.Value,
                DoorKind = ""
            };
            AssignEditorDungeonPath(level, connection);
            level.Connections.Add(connection);
        }

        private void AssignEditorDungeonPath(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || level.Rooms == null) return;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            if (from == null || to == null) return;

            DungeonPathPointRecord sharedStart;
            DungeonPathPointRecord sharedEnd;
            if (TryBuildAdjacentDungeonPassageGrid(from, to, out sharedStart, out sharedEnd))
            {
                connection.PathPoints = new List<DungeonPathPointRecord>();
                return;
            }

            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            List<DungeonPathPointRecord> directPath;
            if (TryBuildAxisAlignedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildDirectDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildStubbedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (TryBuildNearStraightDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            connection.PathPoints = BuildEditorDungeonPath(level, from, to, passageWidth);
        }

        private List<DungeonPathPointRecord> BuildEditorDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            List<DungeonPathPointRecord> directPath;
            if (TryBuildAxisAlignedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            if (TryBuildDirectDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            if (TryBuildStubbedDungeonPath(level, from, to, passageWidth, out directPath))
            {
                return directPath;
            }

            DungeonPathPointRecord start = EditorDungeonBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = EditorDungeonBoundaryPathPoint(to, from, passageWidth);
            List<List<DungeonPathPointRecord>> candidates = BuildEditorDungeonPathCandidates(level, from, to, start, end, passageWidth);
            candidates = candidates
                .Select(path => NormalizeDungeonPathEntrances(path, from, to, passageWidth))
                .ToList();

            List<List<DungeonPathPointRecord>> validCandidates = candidates
                .Where(path => DungeonPathIsOrthogonal(path))
                .Where(path => DungeonPathHasValidRoomEntrances(path, from, to, passageWidth))
                .Where(path => !DungeonGeometry.PathHasTinyStep(path, 1.05))
                .Where(path => !DungeonGeometry.PathHasTightSelfParallelRun(path, 1.05, 0.75))
                .Where(path => !DungeonPathPassesUnderLinkedRoom(level, path, from.Id, to.Id, passageWidth))
                .ToList();
            List<IList<DungeonPathPointRecord>> existingPaths = GetDungeonExistingPathRecords(level, null);
            List<DungeonPathPointRecord> best = validCandidates
                .Where(path => !DungeonPathHitsRoom(level, path, from.Id, to.Id, passageWidth))
                .Where(path => !DungeonPathHitsLinkedRoomInterior(level, path, from.Id, to.Id))
                .Where(path => !DungeonPathRunsTooCloseToExisting(path, existingPaths))
                .OrderBy(path => DungeonPathCost(path, existingPaths))
                .FirstOrDefault();
            if (best != null) return best;

            return validCandidates
                .Where(path => !DungeonPathHitsRoom(level, path, from.Id, to.Id, passageWidth))
                .Where(path => !DungeonPathHitsLinkedRoomInterior(level, path, from.Id, to.Id))
                .Where(path => !DungeonPathRunsTooCloseToExisting(path, existingPaths))
                .OrderBy(path => DungeonPathCost(path, existingPaths))
                .FirstOrDefault()
                ?? EditorOrthogonalFallbackPath(level, from, to, passageWidth, start, end);
        }

        private bool TryBuildDirectDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            List<DungeonPathPointRecord> candidate = CleanDungeonPathPoints(new List<DungeonPathPointRecord>
            {
                EditorDungeonBoundaryPathPoint(from, to, passageWidth),
                EditorDungeonBoundaryPathPoint(to, from, passageWidth)
            });
            candidate = NormalizeDungeonPathEntrances(candidate, from, to, passageWidth);
            if (candidate.Count != 2) return false;
            if (DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to) && !DungeonPathIsOrthogonal(candidate)) return false;
            if (!DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathRunsTooCloseToExisting(level, candidate)) return false;

            path = candidate;
            return true;
        }

        private bool TryBuildStubbedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            DungeonPathPointRecord start = EditorDungeonBoundaryPathPoint(from, to, passageWidth);
            DungeonPathPointRecord end = EditorDungeonBoundaryPathPoint(to, from, passageWidth);
            DungeonPathPointRecord startOutside = RoomOutsideGridPoint(from, RoomCenterX(to), RoomCenterY(to), passageWidth);
            DungeonPathPointRecord endOutside = RoomOutsideGridPoint(to, RoomCenterX(from), RoomCenterY(from), passageWidth);
            List<List<DungeonPathPointRecord>> candidates = BuildEditorStubbedDungeonPathCandidates(level, start, startOutside, endOutside, end);
            candidates = candidates
                .Select(candidate => NormalizeDungeonPathEntrances(candidate, from, to, passageWidth))
                .ToList();
            List<List<DungeonPathPointRecord>> validCandidates = candidates
                .Where(candidate => DungeonPathIsOrthogonal(candidate))
                .Where(candidate => DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth))
                .Where(candidate => !DungeonGeometry.PathHasTinyStep(candidate, 1.05))
                .Where(candidate => !DungeonGeometry.PathHasTightSelfParallelRun(candidate, 1.05, 0.75))
                .Where(candidate => !DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .ToList();

            List<IList<DungeonPathPointRecord>> existingPaths = GetDungeonExistingPathRecords(level, null);
            List<DungeonPathPointRecord> best = validCandidates
                .Where(candidate => !DungeonPathRunsTooCloseToExisting(candidate, existingPaths))
                .OrderBy(candidate => DungeonPathCost(candidate, existingPaths))
                .FirstOrDefault();
            if (best == null)
            {
                best = candidates
                    .Where(candidate => DungeonPathIsOrthogonal(candidate))
                    .Where(candidate => DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth))
                    .Where(candidate => !DungeonGeometry.PathHasTinyStep(candidate, 1.05))
                    .Where(candidate => !DungeonGeometry.PathHasTightSelfParallelRun(candidate, 1.05, 0.75))
                    .Where(candidate => !DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                    .Where(candidate => !DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                    .Where(candidate => !DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                    .Where(candidate => !DungeonPathRunsTooCloseToExisting(candidate, existingPaths))
                    .OrderBy(candidate => DungeonPathCost(candidate, existingPaths))
                    .FirstOrDefault();
            }

            if (best == null) return false;

            path = best;
            return true;
        }

        private List<List<DungeonPathPointRecord>> BuildEditorStubbedDungeonPathCandidates(
            DungeonLevelRecord level,
            DungeonPathPointRecord start,
            DungeonPathPointRecord startOutside,
            DungeonPathPointRecord endOutside,
            DungeonPathPointRecord end)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            if (start == null || startOutside == null || endOutside == null || end == null) return candidates;

            // Ручной редактор использует ту же геометрию, что и генератор: сначала короткий выход из стены,
            // затем поворот снаружи комнаты. Это не дает коридору наполовину уходить под комнату.
            AddDungeonPathCandidate(candidates, start, startOutside, endOutside, end);
            AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = endOutside.Y }, endOutside, end);
            AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = endOutside.X, Y = startOutside.Y }, endOutside, end);

            double midX = Math.Round((startOutside.X + endOutside.X) / 2.0);
            double midY = Math.Round((startOutside.Y + endOutside.Y) / 2.0);
            AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = midX, Y = startOutside.Y }, new DungeonPathPointRecord { X = midX, Y = endOutside.Y }, endOutside, end);
            AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = midY }, new DungeonPathPointRecord { X = endOutside.X, Y = midY }, endOutside, end);

            List<double> xLanes = new List<double> { startOutside.X, endOutside.X, midX };
            List<double> yLanes = new List<double> { startOutside.Y, endOutside.Y, midY };
            if (level != null)
            {
                xLanes.Add(1);
                xLanes.Add(Math.Max(1, level.Width - 1));
                yLanes.Add(1);
                yLanes.Add(Math.Max(1, level.Height - 1));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    xLanes.Add(Math.Max(1, room.X - 1));
                    xLanes.Add(Math.Min(Math.Max(1, level.Width - 1), room.X + room.Width + 1));
                    yLanes.Add(Math.Max(1, room.Y - 1));
                    yLanes.Add(Math.Min(Math.Max(1, level.Height - 1), room.Y + room.Height + 1));
                }
            }

            foreach (double laneX in xLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.X)).Take(28))
            {
                AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = laneX, Y = startOutside.Y }, new DungeonPathPointRecord { X = laneX, Y = endOutside.Y }, endOutside, end);
            }

            foreach (double laneY in yLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.Y)).Take(28))
            {
                AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = laneY }, new DungeonPathPointRecord { X = endOutside.X, Y = laneY }, endOutside, end);
            }

            List<double> compactXLanes = xLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.X)).Take(12).ToList();
            List<double> compactYLanes = yLanes.Distinct().OrderBy(lane => Math.Abs(lane - startOutside.Y)).Take(12).ToList();
            foreach (double laneX in compactXLanes)
            {
                foreach (double laneY in compactYLanes)
                {
                    AddDungeonPathCandidate(candidates,
                        start,
                        startOutside,
                        new DungeonPathPointRecord { X = laneX, Y = startOutside.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = endOutside.X, Y = laneY },
                        endOutside,
                        end);
                    AddDungeonPathCandidate(candidates,
                        start,
                        startOutside,
                        new DungeonPathPointRecord { X = startOutside.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = endOutside.Y },
                        endOutside,
                        end);
                }
            }

            return candidates;
        }

        private List<List<DungeonPathPointRecord>> BuildEditorDungeonPathCandidates(
            DungeonLevelRecord level,
            DungeonRoomRecord from,
            DungeonRoomRecord to,
            DungeonPathPointRecord start,
            DungeonPathPointRecord end,
            int passageWidth)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            if (start == null || end == null) return candidates;

            double midX = Math.Round((start.X + end.X) / 2.0);
            double midY = Math.Round((start.Y + end.Y) / 2.0);
            AddDungeonPathCandidate(candidates, start, end);
            AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = end.X, Y = start.Y }, end);
            AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = end.Y }, end);
            AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = midX, Y = start.Y }, new DungeonPathPointRecord { X = midX, Y = end.Y }, end);
            AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = midY }, new DungeonPathPointRecord { X = end.X, Y = midY }, end);

            List<double> xLanes = new List<double> { midX };
            List<double> yLanes = new List<double> { midY };
            if (level != null)
            {
                xLanes.Add(1);
                xLanes.Add(Math.Max(1, level.Width - 1));
                yLanes.Add(1);
                yLanes.Add(Math.Max(1, level.Height - 1));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    xLanes.Add(Math.Max(1, room.X - 1));
                    xLanes.Add(Math.Min(Math.Max(1, level.Width - 1), room.X + room.Width + 1));
                    yLanes.Add(Math.Max(1, room.Y - 1));
                    yLanes.Add(Math.Min(Math.Max(1, level.Height - 1), room.Y + room.Height + 1));
                }
            }

            foreach (double laneX in xLanes.Distinct().Take(24))
            {
                AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = laneX, Y = start.Y }, new DungeonPathPointRecord { X = laneX, Y = end.Y }, end);
            }

            foreach (double laneY in yLanes.Distinct().Take(24))
            {
                AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = laneY }, new DungeonPathPointRecord { X = end.X, Y = laneY }, end);
            }

            foreach (double laneX in xLanes.Distinct().Take(12))
            {
                foreach (double laneY in yLanes.Distinct().Take(12))
                {
                    AddDungeonPathCandidate(candidates,
                        start,
                        new DungeonPathPointRecord { X = laneX, Y = start.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = end.X, Y = laneY },
                        end);
                    AddDungeonPathCandidate(candidates,
                        start,
                        new DungeonPathPointRecord { X = start.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = end.Y },
                        end);
                }
            }

            return candidates;
        }

        private void AddDungeonPathCandidate(List<List<DungeonPathPointRecord>> candidates, params DungeonPathPointRecord[] points)
        {
            if (candidates == null || points == null || points.Length < 2) return;
            List<DungeonPathPointRecord> cleaned = CleanDungeonPathPoints(points.Select(CloneDungeonPathPoint).ToList());
            if (cleaned.Count < 2) return;
            if (DungeonGeometry.PathHasTinyStep(cleaned, 1.05)) return;
            if (DungeonGeometry.PathHasTightSelfParallelRun(cleaned, 1.05, 0.75)) return;
            if (candidates.Any(path => DungeonPathSequenceEquals(path, cleaned))) return;
            candidates.Add(cleaned);
        }

        private List<DungeonPathPointRecord> EditorOrthogonalFallbackPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, DungeonPathPointRecord start, DungeonPathPointRecord end)
        {
            List<List<DungeonPathPointRecord>> candidates = new List<List<DungeonPathPointRecord>>();
            bool bothBoxes = DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to);
            if (start != null && end != null)
            {
                if (!bothBoxes || AlmostSame(start.X, end.X) || AlmostSame(start.Y, end.Y))
                {
                    AddDungeonPathCandidate(candidates, start, end);
                }

                DungeonPathPointRecord startOutside = RoomOutsideGridPoint(from, RoomCenterX(to), RoomCenterY(to), passageWidth);
                DungeonPathPointRecord endOutside = RoomOutsideGridPoint(to, RoomCenterX(from), RoomCenterY(from), passageWidth);
                AddDungeonPathCandidate(candidates, start, startOutside, endOutside, end);
                AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = endOutside.X, Y = startOutside.Y }, endOutside, end);
                AddDungeonPathCandidate(candidates, start, startOutside, new DungeonPathPointRecord { X = startOutside.X, Y = endOutside.Y }, endOutside, end);
                AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = end.X, Y = start.Y }, end);
                AddDungeonPathCandidate(candidates, start, new DungeonPathPointRecord { X = start.X, Y = end.Y }, end);
            }

            foreach (DungeonPathPointRecord fromPortal in BuildEditorFallbackPortals(from, passageWidth))
            {
                DungeonPathPointRecord fromOutside = EditorOutsidePortalPoint(from, fromPortal);
                foreach (DungeonPathPointRecord toPortal in BuildEditorFallbackPortals(to, passageWidth))
                {
                    DungeonPathPointRecord toOutside = EditorOutsidePortalPoint(to, toPortal);
                    AddDungeonPathCandidate(candidates, fromPortal, fromOutside, toOutside, toPortal);
                    AddDungeonPathCandidate(candidates, fromPortal, fromOutside, new DungeonPathPointRecord { X = toOutside.X, Y = fromOutside.Y }, toOutside, toPortal);
                    AddDungeonPathCandidate(candidates, fromPortal, fromOutside, new DungeonPathPointRecord { X = fromOutside.X, Y = toOutside.Y }, toOutside, toPortal);
                    AddEditorPortalLaneCandidates(candidates, level, fromPortal, fromOutside, toOutside, toPortal);
                }
            }

            List<List<DungeonPathPointRecord>> normalizedCandidates = candidates
                .Select(candidate => NormalizeDungeonPathEntrances(candidate, from, to, passageWidth))
                .Where(candidate => DungeonPathIsOrthogonal(candidate))
                .Where(candidate => DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth))
                .Where(candidate => !DungeonGeometry.PathHasTinyStep(candidate, 1.05))
                .Where(candidate => !DungeonGeometry.PathHasTightSelfParallelRun(candidate, 1.05, 0.75))
                .ToList();

            List<IList<DungeonPathPointRecord>> existingPaths = GetDungeonExistingPathRecords(level, null);
            List<DungeonPathPointRecord> best = normalizedCandidates
                .Where(candidate => DungeonPathIsOrthogonal(candidate))
                .Where(candidate => !DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !DungeonPathRunsTooCloseToExisting(candidate, existingPaths))
                .OrderBy(candidate => DungeonPathCost(candidate, null))
                .FirstOrDefault();
            if (best != null) return best;

            best = normalizedCandidates
                .Where(candidate => !DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth))
                .Where(candidate => !DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .OrderBy(candidate => DungeonPathCost(candidate, existingPaths))
                .FirstOrDefault();
            if (best != null) return best;

            best = normalizedCandidates
                .Where(candidate => !DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id))
                .Where(candidate => !DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth))
                .OrderBy(candidate => DungeonPathCost(candidate, existingPaths))
                .FirstOrDefault();
            if (best != null) return best;

            if (start != null && end != null)
            {
                List<DungeonPathPointRecord> direct = CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
                if ((!bothBoxes || DungeonPathIsOrthogonal(direct))
                    && !DungeonGeometry.PathHasTinyStep(direct, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(direct, 1.05, 0.75)
                    && !DungeonPathHitsRoom(level, direct, from.Id, to.Id, passageWidth)
                    && !DungeonPathHitsLinkedRoomInterior(level, direct, from.Id, to.Id)
                    && !DungeonPathPassesUnderLinkedRoom(level, direct, from.Id, to.Id, passageWidth))
                {
                    return direct;
                }

                List<DungeonPathPointRecord> orthogonal = NormalizeDungeonPathEntrances(new List<DungeonPathPointRecord>
                {
                    start,
                    new DungeonPathPointRecord { X = end.X, Y = start.Y },
                    end
                }, from, to, passageWidth);
                if (DungeonPathIsOrthogonal(orthogonal)
                    && DungeonPathHasValidRoomEntrances(orthogonal, from, to, passageWidth)
                    && !DungeonGeometry.PathHasTinyStep(orthogonal, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(orthogonal, 1.05, 0.75)
                    && !DungeonPathHitsLinkedRoomInterior(level, orthogonal, from.Id, to.Id)
                    && !DungeonPathPassesUnderLinkedRoom(level, orthogonal, from.Id, to.Id, passageWidth))
                {
                    return orthogonal;
                }

                orthogonal = NormalizeDungeonPathEntrances(new List<DungeonPathPointRecord>
                {
                    start,
                    new DungeonPathPointRecord { X = start.X, Y = end.Y },
                    end
                }, from, to, passageWidth);
                if (DungeonPathIsOrthogonal(orthogonal)
                    && DungeonPathHasValidRoomEntrances(orthogonal, from, to, passageWidth)
                    && !DungeonGeometry.PathHasTinyStep(orthogonal, 1.05)
                    && !DungeonGeometry.PathHasTightSelfParallelRun(orthogonal, 1.05, 0.75)
                    && !DungeonPathHitsLinkedRoomInterior(level, orthogonal, from.Id, to.Id)
                    && !DungeonPathPassesUnderLinkedRoom(level, orthogonal, from.Id, to.Id, passageWidth))
                {
                    return orthogonal;
                }
            }

            return CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
        }

        private List<DungeonPathPointRecord> BuildEditorFallbackPortals(DungeonRoomRecord room, int passageWidth)
        {
            List<DungeonPathPointRecord> portals = new List<DungeonPathPointRecord>();
            if (room == null) return portals;

            if (!DungeonGeometry.UsesBoxEdges(room))
            {
                double centerX = RoomCenterX(room);
                double centerY = RoomCenterY(room);
                double rx = Math.Max(0.5, room.Width / 2.0);
                double ry = Math.Max(0.5, room.Height / 2.0);
                AddEditorPortal(portals, new DungeonPathPointRecord { X = centerX - rx, Y = centerY });
                AddEditorPortal(portals, new DungeonPathPointRecord { X = centerX + rx, Y = centerY });
                AddEditorPortal(portals, new DungeonPathPointRecord { X = centerX, Y = centerY - ry });
                AddEditorPortal(portals, new DungeonPathPointRecord { X = centerX, Y = centerY + ry });
                return portals;
            }

            double clearance = Math.Min(1.0, 0.45 + Math.Max(1, Math.Min(4, passageWidth)) * 0.13);
            double left = room.X;
            double right = room.X + room.Width;
            double top = room.Y;
            double bottom = room.Y + room.Height;
            double x = ClampDouble(RoomCenterX(room), left + clearance, right - clearance);
            double y = ClampDouble(RoomCenterY(room), top + clearance, bottom - clearance);
            AddEditorPortal(portals, new DungeonPathPointRecord { X = x, Y = top });
            AddEditorPortal(portals, new DungeonPathPointRecord { X = x, Y = bottom });
            AddEditorPortal(portals, new DungeonPathPointRecord { X = left, Y = y });
            AddEditorPortal(portals, new DungeonPathPointRecord { X = right, Y = y });

            return portals;
        }

        private void AddEditorPortal(List<DungeonPathPointRecord> portals, DungeonPathPointRecord portal)
        {
            if (portals == null || portal == null) return;
            if (portals.Any(existing => AlmostSame(existing.X, portal.X) && AlmostSame(existing.Y, portal.Y))) return;
            portals.Add(portal);
        }

        private DungeonPathPointRecord EditorOutsidePortalPoint(DungeonRoomRecord room, DungeonPathPointRecord portal)
        {
            if (room == null || portal == null) return CloneDungeonPathPoint(portal);
            const double offset = 0.85;
            if (DungeonGeometry.UsesBoxEdges(room))
            {
                if (AlmostSame(portal.X, room.X)) return new DungeonPathPointRecord { X = portal.X - offset, Y = portal.Y };
                if (AlmostSame(portal.X, room.X + room.Width)) return new DungeonPathPointRecord { X = portal.X + offset, Y = portal.Y };
                if (AlmostSame(portal.Y, room.Y)) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y - offset };
                if (AlmostSame(portal.Y, room.Y + room.Height)) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y + offset };
            }

            double dx = portal.X - RoomCenterX(room);
            double dy = portal.Y - RoomCenterY(room);
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.0001) return new DungeonPathPointRecord { X = portal.X, Y = portal.Y - offset };
            return new DungeonPathPointRecord { X = portal.X + dx / length * offset, Y = portal.Y + dy / length * offset };
        }

        private void AddEditorPortalLaneCandidates(
            List<List<DungeonPathPointRecord>> candidates,
            DungeonLevelRecord level,
            DungeonPathPointRecord fromPortal,
            DungeonPathPointRecord fromOutside,
            DungeonPathPointRecord toOutside,
            DungeonPathPointRecord toPortal)
        {
            if (candidates == null || fromPortal == null || fromOutside == null || toOutside == null || toPortal == null) return;
            List<double> xLanes = BuildEditorPortalLaneValues(level, fromOutside.X, toOutside.X, true);
            List<double> yLanes = BuildEditorPortalLaneValues(level, fromOutside.Y, toOutside.Y, false);

            foreach (double laneX in xLanes)
            {
                AddDungeonPathCandidate(candidates,
                    fromPortal,
                    fromOutside,
                    new DungeonPathPointRecord { X = laneX, Y = fromOutside.Y },
                    new DungeonPathPointRecord { X = laneX, Y = toOutside.Y },
                    toOutside,
                    toPortal);
            }

            foreach (double laneY in yLanes)
            {
                AddDungeonPathCandidate(candidates,
                    fromPortal,
                    fromOutside,
                    new DungeonPathPointRecord { X = fromOutside.X, Y = laneY },
                    new DungeonPathPointRecord { X = toOutside.X, Y = laneY },
                    toOutside,
                    toPortal);
            }

            foreach (double laneX in xLanes.Take(8))
            {
                foreach (double laneY in yLanes.Take(8))
                {
                    AddDungeonPathCandidate(candidates,
                        fromPortal,
                        fromOutside,
                        new DungeonPathPointRecord { X = laneX, Y = fromOutside.Y },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = toOutside.X, Y = laneY },
                        toOutside,
                        toPortal);
                    AddDungeonPathCandidate(candidates,
                        fromPortal,
                        fromOutside,
                        new DungeonPathPointRecord { X = fromOutside.X, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = laneY },
                        new DungeonPathPointRecord { X = laneX, Y = toOutside.Y },
                        toOutside,
                        toPortal);
                }
            }
        }

        private List<double> BuildEditorPortalLaneValues(DungeonLevelRecord level, double start, double end, bool horizontalAxis)
        {
            List<double> lanes = new List<double> { start, end, Math.Round((start + end) / 2.0) };
            if (level != null)
            {
                double max = horizontalAxis ? level.Width - 1 : level.Height - 1;
                lanes.Add(1);
                lanes.Add(Math.Max(1, max));
                foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
                {
                    if (room == null) continue;
                    double low = horizontalAxis ? room.X : room.Y;
                    double high = horizontalAxis ? room.X + room.Width : room.Y + room.Height;
                    lanes.Add(ClampDouble(low - 1.2, 1, max));
                    lanes.Add(ClampDouble(high + 1.2, 1, max));
                }
            }

            return lanes
                .Distinct()
                .OrderBy(lane => Math.Min(Math.Abs(lane - start), Math.Abs(lane - end)))
                .Take(8)
                .ToList();
        }

        private bool DungeonPathSequenceEquals(List<DungeonPathPointRecord> a, List<DungeonPathPointRecord> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!AlmostSame(a[i].X, b[i].X) || !AlmostSame(a[i].Y, b[i].Y)) return false;
            }

            return true;
        }

        private bool DungeonPathIsOrthogonal(List<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count < 2) return false;
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord previous = points[i - 1];
                DungeonPathPointRecord current = points[i];
                if (previous == null || current == null) return false;
                if (!AlmostSame(previous.X, current.X) && !AlmostSame(previous.Y, current.Y)) return false;
            }

            return true;
        }

        private DungeonPathPointRecord EditorDungeonBoundaryPathPoint(DungeonRoomRecord room, DungeonRoomRecord target, int passageWidth)
        {
            return RoomEdgeGridPoint(room, RoomCenterX(target), RoomCenterY(target), passageWidth);
        }

        private bool TryRemoveDungeonConnectionAtPoint(Point point)
        {
            DungeonConnectionRecord connection = FindDungeonConnectionAtPoint(point);
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (connection == null || level == null || level.Connections == null) return false;
            level.Connections.Remove(connection);
            if (level.Doors != null)
            {
                level.Doors.RemoveAll(d => d != null
                    && SameDungeonDoorConnection(d, connection.FromRoomId, connection.ToRoomId));
            }

            if (ReferenceEquals(dungeonSelectedConnection, connection)) dungeonSelectedConnection = null;
            dungeonSelectedPathPointIndex = -1;
            pnlDungeonMap.Invalidate();
            return true;
        }

        private void AddDungeonDoorAtPoint(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || dungeonRenderLayout == null) return;
            if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();

            // Дверь хранится отдельно от коридора: инструмент только привязывает её к ближайшему проходу,
            // если пользователь кликнул достаточно близко к нему.
            string kind = SelectedDungeonItemValue(cmbDungeonDoorKind);
            if (string.IsNullOrWhiteSpace(kind)) kind = "Door";

            DungeonConnectionRecord connection;
            PointF screenPoint;
            string orientation;
            if (!TryFindDungeonDoorPlacement(point, out connection, out screenPoint, out orientation))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            DungeonDoorRecord door = new DungeonDoorRecord
            {
                Kind = kind,
                LevelNumber = level.LevelNumber
            };
            ApplyDungeonDoorPlacement(door, connection, screenPoint, orientation);

            level.Doors.Add(door);
            dungeonSelectedDoor = door;
            dungeonSelectedConnection = null;
            dungeonSelectedPathPointIndex = -1;
            pnlDungeonMap.Invalidate();
        }

        private bool TryRemoveDungeonDoorAtPoint(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Doors == null || dungeonRenderLayout == null) return false;
            for (int i = level.Doors.Count - 1; i >= 0; i--)
            {
                DungeonDoorRecord door = level.Doors[i];
                if (door == null) continue;
                RectangleF bounds = DungeonDoorBounds(door, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                bounds.Inflate(5f, 5f);
                if (!bounds.Contains(point)) continue;
                level.Doors.RemoveAt(i);
                if (dungeonSelectedDoor != null && dungeonSelectedDoor.Id == door.Id) dungeonSelectedDoor = null;
                pnlDungeonMap.Invalidate();
                return true;
            }

            return false;
        }

        private DungeonDoorRecord FindDungeonDoorAtPoint(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Doors == null || dungeonRenderLayout == null) return null;
            for (int i = level.Doors.Count - 1; i >= 0; i--)
            {
                DungeonDoorRecord door = level.Doors[i];
                if (door == null) continue;
                RectangleF bounds = DungeonDoorBounds(door, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                bounds.Inflate(6f, 6f);
                if (bounds.Contains(point)) return door;
            }

            return null;
        }

        private void ApplyDungeonDoorPlacement(DungeonDoorRecord door, DungeonConnectionRecord connection, PointF screenPoint, string orientation)
        {
            if (door == null || dungeonRenderLayout == null) return;
            door.X = (screenPoint.X - dungeonRenderLayout.OffsetX) / dungeonRenderLayout.Scale;
            door.Y = (screenPoint.Y - dungeonRenderLayout.OffsetY) / dungeonRenderLayout.Scale;
            ApplyDungeonDoorGridPlacement(door, connection, new DungeonPathPointRecord { X = door.X, Y = door.Y }, orientation);
        }

        private void ApplyDungeonDoorGridPlacement(DungeonDoorRecord door, DungeonConnectionRecord connection, DungeonPathPointRecord gridPoint, string orientation)
        {
            if (door == null || gridPoint == null) return;
            door.X = gridPoint.X;
            door.Y = gridPoint.Y;
            door.Orientation = orientation;
            door.FromRoomId = connection == null ? "" : connection.FromRoomId;
            door.ToRoomId = connection == null ? "" : connection.ToRoomId;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level != null) door.LevelNumber = level.LevelNumber;
        }

        private bool TryFindDungeonConnectionHandleAtPoint(Point point, out DungeonConnectionRecord connection, out int pointIndex)
        {
            connection = null;
            pointIndex = -1;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Connections == null || dungeonRenderLayout == null) return false;
            foreach (DungeonConnectionRecord candidate in DungeonConnectionSnapshot(level))
            {
                if (candidate == null || candidate.PathPoints == null) continue;
                BuildDungeonConnectionGridPoints(level, candidate);
                int firstEditable = DungeonConnectionFirstEditablePathPointIndex(level, candidate);
                int lastEditable = DungeonConnectionLastEditablePathPointIndex(level, candidate);
                for (int i = firstEditable; i <= lastEditable; i++)
                {
                    RectangleF bounds = DungeonPathPointHandleBounds(candidate.PathPoints[i], dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                    bounds.Inflate(4f, 4f);
                    if (!bounds.Contains(point)) continue;
                    connection = candidate;
                    pointIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindDungeonConnectionHit(Point point, out DungeonConnectionRecord connection, out PointF nearestGrid, out int insertIndex)
        {
            connection = null;
            nearestGrid = PointF.Empty;
            insertIndex = -1;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Connections == null || level.Rooms == null || dungeonRenderLayout == null) return false;

            Dictionary<string, DungeonRoomRecord> roomsById = level.Rooms.ToDictionary(r => r.Id, r => r);
            double bestDistance = double.MaxValue;
            PointF bestNearest = PointF.Empty;
            int bestSegmentIndex = -1;
            DungeonConnectionRecord bestConnection = null;
            foreach (DungeonConnectionRecord candidate in DungeonConnectionSnapshot(level))
            {
                DungeonRoomRecord a;
                DungeonRoomRecord b;
                roomsById.TryGetValue(candidate.FromRoomId, out a);
                roomsById.TryGetValue(candidate.ToRoomId, out b);
                using (GraphicsPath path = BuildDungeonConnectionPath(candidate, a, b, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale))
                {
                    PointF nearest;
                    PointF segmentA;
                    PointF segmentB;
                    double distance;
                    int segmentIndex;
                    if (!TryNearestPointOnPath(point, path, out nearest, out segmentA, out segmentB, out distance, out segmentIndex)) continue;
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    bestNearest = nearest;
                    bestSegmentIndex = segmentIndex;
                    bestConnection = candidate;
                }
            }

            if (bestConnection == null || bestDistance > Math.Max(10f, dungeonRenderLayout.Scale * 0.35f)) return false;
            connection = bestConnection;
            nearestGrid = ScreenPointToDungeonGrid(bestNearest, true);
            insertIndex = Math.Max(0, Math.Min(bestSegmentIndex, bestConnection.PathPoints == null ? 0 : bestConnection.PathPoints.Count));
            return true;
        }

        private void UpdateDungeonConnectionPathPoint(DungeonConnectionRecord connection, int pointIndex, Point screenPoint)
        {
            if (connection == null || connection.PathPoints == null || pointIndex < 0 || pointIndex >= connection.PathPoints.Count) return;
            PointF grid = ScreenPointToDungeonGrid(screenPoint, true);
            connection.PathPoints[pointIndex].X = grid.X;
            connection.PathPoints[pointIndex].Y = grid.Y;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level != null)
            {
                NormalizeDungeonConnectionPathPoints(level, connection);
                if (connection.PathPoints != null)
                {
                    pointIndex = connection.PathPoints.Count == 0
                        ? -1
                        : Math.Max(0, Math.Min(pointIndex, connection.PathPoints.Count - 1));
                }
            }

            dungeonSelectedConnection = connection;
            dungeonSelectedPathPointIndex = pointIndex;
            SnapDungeonDoorsToConnection(connection);
        }

        private void SnapDungeonDoorsToConnection(DungeonConnectionRecord connection)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            SnapDungeonDoorsToConnection(level, connection);
        }

        private void SnapDungeonDoorsToConnection(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (connection == null || level == null || level.Doors == null || level.Rooms == null) return;
            foreach (DungeonDoorRecord door in level.Doors)
            {
                if (!SameDungeonDoorConnection(door, connection.FromRoomId, connection.ToRoomId)) continue;
                SnapDungeonDoorToConnection(level, door, connection);
            }
        }

        private PointF ScreenPointToDungeonGrid(PointF screenPoint, bool snapHalfCell)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (dungeonRenderLayout == null || level == null) return PointF.Empty;
            double x = (screenPoint.X - dungeonRenderLayout.OffsetX) / dungeonRenderLayout.Scale;
            double y = (screenPoint.Y - dungeonRenderLayout.OffsetY) / dungeonRenderLayout.Scale;
            if (snapHalfCell)
            {
                x = Math.Round(x * 2.0) / 2.0;
                y = Math.Round(y * 2.0) / 2.0;
            }

            x = Math.Max(0.25, Math.Min(level.Width - 0.25, x));
            y = Math.Max(0.25, Math.Min(level.Height - 0.25, y));
            return new PointF((float)x, (float)y);
        }

        private DungeonConnectionRecord FindDungeonConnectionAtPoint(Point point)
        {
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Connections == null || level.Rooms == null || dungeonRenderLayout == null) return null;
            Dictionary<string, DungeonRoomRecord> roomsById = level.Rooms.ToDictionary(r => r.Id, r => r);
            foreach (DungeonConnectionRecord connection in DungeonConnectionSnapshot(level))
            {
                DungeonRoomRecord a;
                DungeonRoomRecord b;
                roomsById.TryGetValue(connection.FromRoomId, out a);
                roomsById.TryGetValue(connection.ToRoomId, out b);
                using (GraphicsPath path = BuildDungeonConnectionPath(connection, a, b, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale))
                {
                    double distance = DistanceToPath(point, path);
                    if (distance <= Math.Max(6f, dungeonRenderLayout.Scale * 0.22f)) return connection;
                }
            }

            return null;
        }

        private bool TryFindDungeonDoorPlacement(Point point, out DungeonConnectionRecord connection, out PointF screenPoint, out string orientation)
        {
            connection = null;
            screenPoint = PointF.Empty;
            orientation = "Vertical";
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Rooms == null || dungeonRenderLayout == null) return false;

            PointF gridPoint = ScreenPointToDungeonGrid(point, false);
            double bestDistance = double.MaxValue;
            PointF bestPoint = PointF.Empty;
            PointF bestA = PointF.Empty;
            PointF bestB = PointF.Empty;
            DungeonConnectionRecord bestConnection = null;
            foreach (DungeonConnectionRecord candidate in DungeonConnectionSnapshot(level))
            {
                DungeonPathPointRecord nearestGrid;
                DungeonPathPointRecord segmentGridA;
                DungeonPathPointRecord segmentGridB;
                double gridDistance;
                if (!TryNearestLegalDungeonConnectionGridPoint(level, candidate, gridPoint, out nearestGrid, out segmentGridA, out segmentGridB, out gridDistance)) continue;
                double screenDistance = gridDistance * dungeonRenderLayout.Scale;
                if (screenDistance >= bestDistance) continue;
                bestDistance = screenDistance;
                bestPoint = DungeonGridPointToScreen(nearestGrid, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                bestA = DungeonGridPointToScreen(segmentGridA, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                bestB = DungeonGridPointToScreen(segmentGridB, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
                bestConnection = candidate;
            }

            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null) continue;
                PointF nearest;
                PointF wallA;
                PointF wallB;
                double distance;
                if (!TryNearestDungeonRoomWallPoint(point, room, out nearest, out wallA, out wallB, out distance)) continue;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestPoint = nearest;
                bestA = wallA;
                bestB = wallB;
                bestConnection = new DungeonConnectionRecord
                {
                    FromRoomId = room.Id,
                    ToRoomId = ""
                };
            }

            if (bestConnection == null || bestDistance > Math.Max(16f, dungeonRenderLayout.Scale * 0.55f)) return false;
            connection = bestConnection;
            screenPoint = bestPoint;
            bool roomWallPlacement = string.IsNullOrWhiteSpace(bestConnection.ToRoomId);
            orientation = DungeonDoorOrientationForSegment(bestA, bestB, roomWallPlacement);
            return true;
        }

        private bool TryNearestLegalDungeonConnectionGridPoint(
            DungeonLevelRecord level,
            DungeonConnectionRecord connection,
            PointF gridPoint,
            out DungeonPathPointRecord nearest,
            out DungeonPathPointRecord segmentA,
            out DungeonPathPointRecord segmentB,
            out double distance)
        {
            nearest = null;
            segmentA = null;
            segmentB = null;
            distance = double.MaxValue;
            if (level == null || connection == null) return false;

            List<DungeonPathPointRecord> points = BuildDungeonConnectionGridPoints(level, connection);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                if (!DungeonConnectionSegmentCanHoldDoor(level, a, b)) continue;

                DungeonPathPointRecord candidate = NearestPointOnGridSegment(gridPoint, a, b);
                if (IsDungeonGridPointInsideAnyRoomInterior(level, candidate.X, candidate.Y)) continue;

                double dx = candidate.X - gridPoint.X;
                double dy = candidate.Y - gridPoint.Y;
                double candidateDistance = Math.Sqrt(dx * dx + dy * dy);
                if (candidateDistance >= distance) continue;

                distance = candidateDistance;
                nearest = candidate;
                segmentA = a;
                segmentB = b;
            }

            return nearest != null;
        }

        private bool DungeonConnectionSegmentCanHoldDoor(DungeonLevelRecord level, DungeonPathPointRecord a, DungeonPathPointRecord b)
        {
            if (level == null || a == null || b == null) return false;
            if (AlmostSame(a.X, b.X) && AlmostSame(a.Y, b.Y)) return false;
            foreach (DungeonRoomRecord room in level.Rooms ?? new List<DungeonRoomRecord>())
            {
                if (room == null) continue;
                if (DungeonGeometry.SegmentCrossesRoomInterior(a, b, room)) return false;
                if (DungeonGeometry.SegmentRunsAlongRoomBoundary(a, b, room, 0.08)) return false;
            }

            return true;
        }

        private DungeonPathPointRecord NearestPointOnGridSegment(PointF point, DungeonPathPointRecord a, DungeonPathPointRecord b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 0.000001) return CloneDungeonPathPoint(a);

            double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared;
            t = Math.Max(0.0, Math.Min(1.0, t));
            return new DungeonPathPointRecord
            {
                X = a.X + dx * t,
                Y = a.Y + dy * t
            };
        }

        private bool IsDungeonGridPointInsideAnyRoomInterior(DungeonLevelRecord level, double x, double y)
        {
            if (level == null || level.Rooms == null) return false;
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null) continue;
                if (DungeonGeometry.IsPointInsideRoomInterior(room, x, y, 0.06)) return true;
            }

            return false;
        }

        private bool TryNearestDungeonRoomWallPoint(Point point, DungeonRoomRecord room, out PointF nearest, out PointF wallA, out PointF wallB, out double distance)
        {
            nearest = PointF.Empty;
            wallA = PointF.Empty;
            wallB = PointF.Empty;
            distance = double.MaxValue;
            if (room == null || dungeonRenderLayout == null) return false;
            RectangleF rect = RoomScreenRect(room, dungeonRenderLayout.OffsetX, dungeonRenderLayout.OffsetY, dungeonRenderLayout.Scale);
            PointF[] corners =
            {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left, rect.Bottom)
            };

            for (int i = 0; i < 4; i++)
            {
                PointF a = corners[i];
                PointF b = corners[(i + 1) % 4];
                PointF candidate = NearestPointOnSegment(point, a, b);
                double dx = point.X - candidate.X;
                double dy = point.Y - candidate.Y;
                double candidateDistance = Math.Sqrt(dx * dx + dy * dy);
                if (candidateDistance >= distance) continue;
                distance = candidateDistance;
                nearest = candidate;
                wallA = a;
                wallB = b;
            }

            return distance < double.MaxValue;
        }

        private void DrawDungeonEditorMap(Graphics graphics, Rectangle bounds)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.FromArgb(233, 225, 186));
            if (currentDungeon == null)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(80, 60, 44)))
                {
                    graphics.DrawString(isEnglish ? "Generate or open a dungeon." : "Сгенерируйте или откройте данж.", Font, brush, 16, 16);
                }
                return;
            }

            DrawDungeonLevel(graphics, bounds, currentDungeon, SelectedDungeonLevelNumber(), true);
        }

        private void DrawDungeonLevel(Graphics graphics, Rectangle bounds, DungeonRecord dungeon, int levelNumber, bool trackBounds)
        {
            if (trackBounds) dungeonRoomBounds.Clear();
            DungeonLevelRecord level = dungeon == null || dungeon.Levels == null ? null : dungeon.Levels.FirstOrDefault(l => l.LevelNumber == levelNumber);
            if (level == null) return;

            Rectangle drawing = new Rectangle(bounds.Left + 16, bounds.Top + 16, Math.Max(40, bounds.Width - 32), Math.Max(40, bounds.Height - 32));
            float cellW = drawing.Width / Math.Max(1f, level.Width);
            float cellH = drawing.Height / Math.Max(1f, level.Height);
            float baseScale = Math.Min(cellW, cellH);
            float baseOffsetX = drawing.Left + (drawing.Width - level.Width * baseScale) / 2f;
            float baseOffsetY = drawing.Top + (drawing.Height - level.Height * baseScale) / 2f;
            float scale = baseScale * (trackBounds ? dungeonZoom : 1f);
            float offsetX = baseOffsetX + (trackBounds ? dungeonPanOffset.X : 0f);
            float offsetY = baseOffsetY + (trackBounds ? dungeonPanOffset.Y : 0f);
            if (trackBounds)
            {
                dungeonRenderLayout = new DungeonRenderLayout(offsetX, offsetY, scale, baseOffsetX, baseOffsetY, baseScale);
            }

            NormalizeDungeonLevelConnectionPaths(level);
            List<DungeonRoomRecord> roomSnapshot = DungeonRoomSnapshot(level);
            List<DungeonConnectionRecord> connectionSnapshot = DungeonConnectionSnapshot(level);
            List<DungeonDoorRecord> doorSnapshot = DungeonDoorSnapshot(level);

            using (Pen gridPen = new Pen(Color.FromArgb(60, 88, 78, 60), 1f))
            {
                for (int x = 0; x <= level.Width; x++)
                {
                    graphics.DrawLine(gridPen, offsetX + x * scale, offsetY, offsetX + x * scale, offsetY + level.Height * scale);
                }
                for (int y = 0; y <= level.Height; y++)
                {
                    graphics.DrawLine(gridPen, offsetX, offsetY + y * scale, offsetX + level.Width * scale, offsetY + y * scale);
                }
            }

            Dictionary<string, DungeonRoomRecord> roomsById = roomSnapshot
                .Where(r => !string.IsNullOrWhiteSpace(r.Id))
                .GroupBy(r => r.Id)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (DungeonConnectionRecord connection in connectionSnapshot)
            {
                DungeonRoomRecord a;
                DungeonRoomRecord b;
                roomsById.TryGetValue(connection.FromRoomId, out a);
                roomsById.TryGetValue(connection.ToRoomId, out b);
                if (IsDungeonOverpass(connection)) continue;
                DrawDungeonConnection(graphics, connection, a, b, offsetX, offsetY, scale);
            }
            DrawDungeonConnectionJunctions(graphics, level, connectionSnapshot, offsetX, offsetY, scale);

            DungeonRoomRecord selected = lstDungeonRooms == null ? null : lstDungeonRooms.SelectedItem as DungeonRoomRecord;
            foreach (DungeonRoomRecord room in roomSnapshot)
            {
                RectangleF rect = new RectangleF(
                    offsetX + room.X * scale,
                    offsetY + room.Y * scale,
                    Math.Max(scale * 1.4f, room.Width * scale),
                    Math.Max(scale * 1.4f, room.Height * scale));
                if (trackBounds) dungeonRoomBounds[room.Id] = rect;

                bool selectedRoom = selected != null && selected.Id == room.Id;
                bool pendingStart = (dungeonConnectionStartRoom != null && dungeonConnectionStartRoom.Id == room.Id)
                    || (dungeonDisconnectStartRoom != null && dungeonDisconnectStartRoom.Id == room.Id);
                DrawDungeonRoomShape(graphics, room, rect, selectedRoom, pendingStart);

                string label = DisplayRoomTitle(room);
                float labelSize = trackBounds
                    ? Math.Max(7f, Math.Min(10f, scale * 0.32f))
                    : Math.Max(12f, Math.Min(20f, scale * 0.32f));
                using (Font font = new Font(UiTheme.FontFamily, labelSize, FontStyle.Bold, GraphicsUnit.Point))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    graphics.DrawString(label, font, brush, rect, format);
                }
            }

            foreach (DungeonConnectionRecord connection in connectionSnapshot)
            {
                if (!IsDungeonOverpass(connection)) continue;
                DungeonRoomRecord a;
                DungeonRoomRecord b;
                roomsById.TryGetValue(connection.FromRoomId, out a);
                roomsById.TryGetValue(connection.ToRoomId, out b);
                DrawDungeonConnection(graphics, connection, a, b, offsetX, offsetY, scale);
            }

            foreach (DungeonDoorRecord door in doorSnapshot)
            {
                DrawDungeonDoorEntity(graphics, door, offsetX, offsetY, scale);
            }

            if (trackBounds && dungeonFreeConnectionStartPoint != null)
            {
                DrawDungeonFreeConnectionStartMarker(graphics, dungeonFreeConnectionStartPoint, offsetX, offsetY, scale);
            }

            if (trackBounds && selected != null)
            {
                RectangleF selectedBounds;
                if (dungeonRoomBounds.TryGetValue(selected.Id, out selectedBounds))
                {
                    DrawDungeonRoomResizeHandles(graphics, selectedBounds, scale);
                }
            }

            if (trackBounds)
            {
                DrawDungeonConnectionHandles(graphics, offsetX, offsetY, scale);
            }
        }

        private void DrawDungeonConnection(Graphics graphics, DungeonConnectionRecord connection, DungeonRoomRecord a, DungeonRoomRecord b, float offsetX, float offsetY, float scale)
        {
            int widthCells = Math.Max(1, Math.Min(4, connection == null ? 1 : connection.PassageWidth));
            float lineWidth = Math.Max(3f, scale * (0.42f + widthCells * 0.22f));
            bool secret = IsSecretDungeonDoor(connection);
            bool overpass = IsDungeonOverpass(connection);
            bool selected = ReferenceEquals(connection, dungeonSelectedConnection);
            using (Pen shadow = new Pen(Color.FromArgb(105, 48, 35, 24), lineWidth + 3f))
            using (Pen floor = new Pen(overpass ? Color.FromArgb(170, 202, 190, 147) : Color.FromArgb(202, 190, 147), lineWidth))
            using (Pen edge = new Pen(selected ? Color.FromArgb(230, 184, 38) : secret ? Color.FromArgb(120, 85, 72, 55) : Color.FromArgb(104, 72, 40), Math.Max(1f, selected ? lineWidth * 0.26f : lineWidth * 0.18f)))
            using (GraphicsPath path = BuildDungeonConnectionPath(connection, a, b, offsetX, offsetY, scale))
            {
                shadow.StartCap = shadow.EndCap = LineCap.Flat;
                floor.StartCap = floor.EndCap = LineCap.Flat;
                edge.StartCap = edge.EndCap = LineCap.Flat;
                shadow.LineJoin = LineJoin.MiterClipped;
                floor.LineJoin = LineJoin.MiterClipped;
                edge.LineJoin = LineJoin.MiterClipped;
                if (secret) floor.DashStyle = DashStyle.Dash;
                graphics.DrawPath(shadow, path);
                graphics.DrawPath(floor, path);
                graphics.DrawPath(edge, path);
                if (overpass)
                {
                    using (Pen dash = new Pen(Color.FromArgb(92, 58, 32), Math.Max(2f, lineWidth * 0.22f)))
                    {
                        dash.StartCap = dash.EndCap = LineCap.Flat;
                        dash.LineJoin = LineJoin.MiterClipped;
                        dash.DashStyle = DashStyle.Dash;
                        graphics.DrawPath(dash, path);
                    }
                }
            }
        }

        private static List<DungeonRoomRecord> DungeonRoomSnapshot(DungeonLevelRecord level)
        {
            return level == null || level.Rooms == null
                ? new List<DungeonRoomRecord>()
                : level.Rooms.Where(room => room != null).ToList();
        }

        private static List<DungeonConnectionRecord> DungeonConnectionSnapshot(DungeonLevelRecord level)
        {
            return level == null || level.Connections == null
                ? new List<DungeonConnectionRecord>()
                : level.Connections.Where(connection => connection != null).ToList();
        }

        private static List<DungeonDoorRecord> DungeonDoorSnapshot(DungeonLevelRecord level)
        {
            return level == null || level.Doors == null
                ? new List<DungeonDoorRecord>()
                : level.Doors.Where(door => door != null).ToList();
        }

        private void DrawDungeonConnectionJunctions(Graphics graphics, DungeonLevelRecord level, IList<DungeonConnectionRecord> connections, float offsetX, float offsetY, float scale)
        {
            if (graphics == null || level == null || connections == null) return;
            List<Tuple<PointF, int>> junctions = new List<Tuple<PointF, int>>();
            List<Tuple<DungeonConnectionRecord, PointF, PointF>> segments = new List<Tuple<DungeonConnectionRecord, PointF, PointF>>();
            foreach (DungeonConnectionRecord connection in connections)
            {
                if (connection == null || IsDungeonOverpass(connection)) continue;
                List<PointF> points = BuildDungeonConnectionScreenPoints(level, connection, offsetX, offsetY, scale);
                for (int i = 1; i < points.Count; i++)
                {
                    segments.Add(Tuple.Create(connection, points[i - 1], points[i]));
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    if (ReferenceEquals(segments[i].Item1, segments[j].Item1)) continue;
                    PointF cross;
                    if (!TryDungeonSegmentJunctionPoint(segments[i].Item2, segments[i].Item3, segments[j].Item2, segments[j].Item3, out cross)) continue;
                    if (junctions.Any(existing => SegmentLength(existing.Item1, cross) < Math.Max(2f, scale * 0.12f))) continue;
                    int width = Math.Max(segments[i].Item1.PassageWidth, segments[j].Item1.PassageWidth);
                    junctions.Add(Tuple.Create(cross, width));
                }
            }

            foreach (Tuple<PointF, int> junction in junctions)
            {
                float size = Math.Max(8f, scale * (0.42f + Math.Max(1, junction.Item2) * 0.22f));
                RectangleF bounds = new RectangleF(junction.Item1.X - size / 2f, junction.Item1.Y - size / 2f, size, size);
                using (Brush fill = new SolidBrush(Color.FromArgb(202, 190, 147)))
                using (Pen outline = new Pen(Color.FromArgb(104, 72, 40), Math.Max(1f, size * 0.12f)))
                {
                    graphics.FillRectangle(fill, bounds);
                    graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
            }
        }

        private void DrawDungeonFreeConnectionStartMarker(Graphics graphics, DungeonPathPointRecord point, float offsetX, float offsetY, float scale)
        {
            PointF center = DungeonGridPointToScreen(point, offsetX, offsetY, scale);
            float size = Math.Max(10f, scale * 0.35f);
            RectangleF bounds = new RectangleF(center.X - size / 2f, center.Y - size / 2f, size, size);
            using (Brush fill = new SolidBrush(Color.FromArgb(255, 221, 74)))
            using (Pen outline = new Pen(Color.FromArgb(80, 58, 30), Math.Max(1.4f, scale * 0.06f)))
            {
                graphics.FillRectangle(fill, bounds);
                graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        private bool TryDungeonSegmentJunctionPoint(PointF a, PointF b, PointF c, PointF d, out PointF cross)
        {
            cross = PointF.Empty;
            double rX = b.X - a.X;
            double rY = b.Y - a.Y;
            double sX = d.X - c.X;
            double sY = d.Y - c.Y;
            double denominator = rX * sY - rY * sX;
            if (Math.Abs(denominator) < 0.001)
            {
                if (TryDungeonEndpointOnSegment(a, c, d, out cross)) return true;
                if (TryDungeonEndpointOnSegment(b, c, d, out cross)) return true;
                if (TryDungeonEndpointOnSegment(c, a, b, out cross)) return true;
                if (TryDungeonEndpointOnSegment(d, a, b, out cross)) return true;
                return false;
            }

            double qX = c.X - a.X;
            double qY = c.Y - a.Y;
            double t = (qX * sY - qY * sX) / denominator;
            double u = (qX * rY - qY * rX) / denominator;
            if (t < 0.03 || t > 0.97 || u < 0.03 || u > 0.97) return false;

            cross = new PointF((float)(a.X + t * rX), (float)(a.Y + t * rY));
            return true;
        }

        private bool TryDungeonEndpointOnSegment(PointF endpoint, PointF a, PointF b, out PointF junction)
        {
            junction = PointF.Empty;
            float length = SegmentLength(a, b);
            if (length < 0.1f) return false;
            double distance = DistanceToScreenSegment(endpoint, a, b);
            if (distance > Math.Max(1.5f, length * 0.015f)) return false;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double t = ((endpoint.X - a.X) * dx + (endpoint.Y - a.Y) * dy) / (dx * dx + dy * dy);
            if (t <= 0.03 || t >= 0.97) return false;
            junction = endpoint;
            return true;
        }

        private double DistanceToScreenSegment(PointF point, PointF a, PointF b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001)
            {
                double px = point.X - a.X;
                double py = point.Y - a.Y;
                return Math.Sqrt(px * px + py * py);
            }

            double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double x = a.X + t * dx;
            double y = a.Y + t * dy;
            double ox = point.X - x;
            double oy = point.Y - y;
            return Math.Sqrt(ox * ox + oy * oy);
        }

        private void DrawDungeonConnectionHandles(Graphics graphics, float offsetX, float offsetY, float scale)
        {
            if (dungeonSelectedConnection == null || dungeonSelectedConnection.PathPoints == null) return;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level != null)
            {
                BuildDungeonConnectionGridPoints(level, dungeonSelectedConnection);
            }

            int firstEditable = DungeonConnectionFirstEditablePathPointIndex(level, dungeonSelectedConnection);
            int lastEditable = DungeonConnectionLastEditablePathPointIndex(level, dungeonSelectedConnection);
            for (int i = firstEditable; i <= lastEditable; i++)
            {
                DungeonPathPointRecord point = dungeonSelectedConnection.PathPoints[i];
                if (level != null && IsDungeonGridPointInsideAnyRoomInterior(level, point.X, point.Y)) continue;
                RectangleF bounds = DungeonPathPointHandleBounds(point, offsetX, offsetY, scale);
                bool selectedPoint = i == dungeonSelectedPathPointIndex;
                using (Brush fill = new SolidBrush(selectedPoint ? Color.FromArgb(255, 221, 74) : Color.FromArgb(245, 238, 190)))
                using (Pen outline = new Pen(Color.FromArgb(80, 58, 30), Math.Max(1.2f, scale * 0.04f)))
                {
                    graphics.FillRectangle(fill, bounds);
                    graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
            }
        }

        private RectangleF DungeonPathPointHandleBounds(DungeonPathPointRecord point, float offsetX, float offsetY, float scale)
        {
            PointF center = DungeonGridPointToScreen(point, offsetX, offsetY, scale);
            float size = Math.Max(8f, scale * 0.24f);
            return new RectangleF(center.X - size / 2f, center.Y - size / 2f, size, size);
        }

        private int DungeonConnectionFirstEditablePathPointIndex(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (!DungeonConnectionHasBothRooms(level, connection)) return 0;
            return 1;
        }

        private int DungeonConnectionLastEditablePathPointIndex(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (connection == null || connection.PathPoints == null) return -1;
            if (!DungeonConnectionHasBothRooms(level, connection)) return connection.PathPoints.Count - 1;
            return connection.PathPoints.Count - 2;
        }

        private bool DungeonConnectionHasBothRooms(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || level.Rooms == null || connection == null) return false;
            return level.Rooms.Any(r => r != null && r.Id == connection.FromRoomId)
                && level.Rooms.Any(r => r != null && r.Id == connection.ToRoomId);
        }

        private GraphicsPath BuildDungeonConnectionPath(DungeonConnectionRecord connection, DungeonRoomRecord a, DungeonRoomRecord b, float offsetX, float offsetY, float scale)
        {
            DungeonLevelRecord level = DungeonConnectionPathLevel(connection, a, b);
            return BuildDungeonConnectionPath(level, connection, offsetX, offsetY, scale);
        }

        private DungeonLevelRecord DungeonConnectionPathLevel(DungeonConnectionRecord connection, DungeonRoomRecord a, DungeonRoomRecord b)
        {
            DungeonLevelRecord selected = SelectedDungeonLevel();
            if (selected != null && selected.Connections != null && selected.Connections.Contains(connection)) return selected;

            DungeonLevelRecord fallback = new DungeonLevelRecord
            {
                Rooms = new List<DungeonRoomRecord>(),
                Connections = new List<DungeonConnectionRecord>()
            };
            if (a != null) fallback.Rooms.Add(a);
            if (b != null && !fallback.Rooms.Any(r => r != null && r.Id == b.Id)) fallback.Rooms.Add(b);
            if (connection != null) fallback.Connections.Add(connection);
            return fallback;
        }

        private GraphicsPath BuildDungeonConnectionPath(DungeonLevelRecord level, DungeonConnectionRecord connection, float offsetX, float offsetY, float scale)
        {
            GraphicsPath path = new GraphicsPath();
            if (connection == null) return path;

            // Вся геометрия прохода считается в координатах сетки. Экранный масштаб
            // только переводит готовые точки в пиксели, поэтому зум не меняет форму коридора.
            List<DungeonPathPointRecord> gridPoints = BuildDungeonConnectionGridPoints(level, connection);
            for (int i = 1; i < gridPoints.Count; i++)
            {
                AddDungeonPathLine(path,
                    DungeonGridPointToScreen(gridPoints[i - 1], offsetX, offsetY, scale),
                    DungeonGridPointToScreen(gridPoints[i], offsetX, offsetY, scale));
            }

            return path;
        }

        private void AddDungeonPathLine(GraphicsPath path, PointF start, PointF end)
        {
            if (path == null) return;
            if (SegmentLength(start, end) < 0.1f) return;
            path.AddLine(start, end);
        }

        private List<PointF> BuildDungeonConnectionScreenPoints(DungeonLevelRecord level, DungeonConnectionRecord connection, float offsetX, float offsetY, float scale)
        {
            List<PointF> points = new List<PointF>();
            foreach (DungeonPathPointRecord point in BuildDungeonConnectionGridPoints(level, connection))
            {
                points.Add(DungeonGridPointToScreen(point, offsetX, offsetY, scale));
            }

            return points;
        }

        private List<DungeonPathPointRecord> BuildDungeonConnectionGridPoints(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            List<DungeonPathPointRecord> points = new List<DungeonPathPointRecord>();
            if (level == null || connection == null) return points;
            if (!dungeonGridPathBuildStack.Add(connection))
            {
                return connection.PathPoints == null
                    ? points
                    : CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
            }

            try
            {
            DungeonRoomRecord a = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord b = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            List<DungeonPathPointRecord> pathPoints = connection.PathPoints ?? new List<DungeonPathPointRecord>();
            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));

            if (pathPoints.Count > 0)
            {
                if (a != null && b != null)
                {
                    List<DungeonPathPointRecord> storedPath = CleanDungeonPathPoints(pathPoints.Select(CloneDungeonPathPoint).ToList());
                    if (DungeonStoredConnectionPathIsCanonical(level, connection, storedPath, a, b, passageWidth))
                    {
                        StoreDungeonConnectionPath(connection, storedPath);
                        return storedPath;
                    }

                    List<DungeonPathPointRecord> directPath;
                    if (pathPoints.Count > 2 && TryBuildNearStraightDungeonPath(level, a, b, passageWidth, out directPath))
                    {
                        StoreDungeonConnectionPath(connection, directPath);
                        return directPath;
                    }

                    DungeonPathPointRecord startTarget = pathPoints.Count > 1
                        ? pathPoints[1]
                        : new DungeonPathPointRecord { X = RoomCenterX(b), Y = RoomCenterY(b) };
                    DungeonPathPointRecord endTarget = pathPoints.Count > 1
                        ? pathPoints[pathPoints.Count - 2]
                        : new DungeonPathPointRecord { X = RoomCenterX(a), Y = RoomCenterY(a) };
                    DungeonPathPointRecord startEdge = RoomEdgeGridPoint(a, startTarget.X, startTarget.Y, passageWidth);
                    DungeonPathPointRecord endEdge = RoomEdgeGridPoint(b, endTarget.X, endTarget.Y, passageWidth);
                    List<DungeonPathPointRecord> visiblePathPoints = pathPoints
                        .Select(CloneDungeonPathPoint)
                        .Where(point => !DungeonGeometry.IsPointInsideRoomInterior(a, point.X, point.Y, 0.01)
                            && !DungeonGeometry.IsPointInsideRoomInterior(b, point.X, point.Y, 0.01))
                        .ToList();

                    if (visiblePathPoints.Count > 0 && DungeonGridDistance(visiblePathPoints[0], startEdge) <= 0.75)
                    {
                        visiblePathPoints.RemoveAt(0);
                    }

                    if (visiblePathPoints.Count > 0 && DungeonGridDistance(visiblePathPoints[visiblePathPoints.Count - 1], endEdge) <= 0.75)
                    {
                        visiblePathPoints.RemoveAt(visiblePathPoints.Count - 1);
                    }

                    points.Add(startEdge);
                    points.AddRange(visiblePathPoints);
                    points.Add(endEdge);
                    points = NormalizeDungeonPathEntrances(points, a, b, passageWidth);
                    if (DungeonConnectionPathNeedsRepair(level, connection, points, a, b, passageWidth))
                    {
                        List<DungeonPathPointRecord> rebuilt = BuildEditorDungeonPathIgnoringConnection(level, connection, a, b, passageWidth);
                        StoreDungeonConnectionPath(connection, rebuilt);
                        return rebuilt;
                    }
                }
                else
                {
                    points.AddRange(pathPoints.Select(CloneDungeonPathPoint));
                }

                points = CleanDungeonPathPoints(points);
                StoreDungeonConnectionPath(connection, points);
                return points;
            }

            if (a == null || b == null) return points;
            DungeonPathPointRecord start;
            DungeonPathPointRecord end;
            if (TryBuildAdjacentDungeonPassageGrid(a, b, out start, out end))
            {
                points.Add(start);
                points.Add(end);
                StoreDungeonConnectionPath(connection, points);
                return points;
            }

            List<DungeonPathPointRecord> direct;
            if (TryBuildNearStraightDungeonPath(level, a, b, passageWidth, out direct))
            {
                StoreDungeonConnectionPath(connection, direct);
                return direct;
            }

            List<DungeonPathPointRecord> built = BuildEditorDungeonPath(level, a, b, passageWidth);
            StoreDungeonConnectionPath(connection, built);
            return built;
            }
            finally
            {
                dungeonGridPathBuildStack.Remove(connection);
            }
        }

        private void StoreDungeonConnectionPath(DungeonConnectionRecord connection, List<DungeonPathPointRecord> points)
        {
            if (connection == null || points == null) return;
            List<DungeonPathPointRecord> cleaned = CleanDungeonPathPoints(points.Select(CloneDungeonPathPoint).ToList());
            if (DungeonPathSequenceEquals(connection.PathPoints ?? new List<DungeonPathPointRecord>(), cleaned)) return;
            connection.PathPoints = cleaned;
            dungeonSelectedPathPointIndex = Math.Min(dungeonSelectedPathPointIndex, connection.PathPoints.Count - 1);
            dungeonDragPathPointIndex = Math.Min(dungeonDragPathPointIndex, connection.PathPoints.Count - 1);
        }

        private void NormalizeDungeonLevelConnectionPaths(DungeonLevelRecord level)
        {
            if (level == null || level.Connections == null) return;
            foreach (DungeonConnectionRecord connection in level.Connections.ToList())
            {
                NormalizeDungeonConnectionPathPoints(level, connection);
            }

            SnapDungeonDoorsForLevel(level);
        }

        private void NormalizeDungeonConnectionPathPoints(DungeonLevelRecord level, DungeonConnectionRecord connection)
        {
            if (level == null || connection == null || IsDungeonOverpass(connection) || connection.PathPoints == null || connection.PathPoints.Count == 0) return;
            DungeonRoomRecord from = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r.Id == connection.FromRoomId);
            DungeonRoomRecord to = level.Rooms == null ? null : level.Rooms.FirstOrDefault(r => r.Id == connection.ToRoomId);
            int passageWidth = Math.Max(1, Math.Min(4, connection.PassageWidth));
            if (from == null || to == null)
            {
                connection.PathPoints = CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
                return;
            }

            List<DungeonPathPointRecord> storedPath = CleanDungeonPathPoints(connection.PathPoints.Select(CloneDungeonPathPoint).ToList());
            if (DungeonStoredConnectionPathIsCanonical(level, connection, storedPath, from, to, passageWidth))
            {
                connection.PathPoints = storedPath;
                return;
            }

            List<DungeonPathPointRecord> normalized = new List<DungeonPathPointRecord>();
            DungeonPathPointRecord first = storedPath.First();
            DungeonPathPointRecord last = storedPath.Last();
            DungeonPathPointRecord firstTarget = storedPath.Count > 1 ? storedPath[1] : first;
            DungeonPathPointRecord lastTarget = storedPath.Count > 1 ? storedPath[storedPath.Count - 2] : last;
            normalized.Add(RoomEdgeGridPoint(from, firstTarget.X, firstTarget.Y, passageWidth));

            for (int i = 1; i < storedPath.Count - 1; i++)
            {
                DungeonPathPointRecord point = storedPath[i];
                if (point == null) continue;
                if (IsDungeonGridPointInsideAnyRoomInterior(level, (float)point.X, (float)point.Y)) continue;
                normalized.Add(CloneDungeonPathPoint(point));
            }

            normalized.Add(RoomEdgeGridPoint(to, lastTarget.X, lastTarget.Y, passageWidth));
            normalized = NormalizeDungeonPathEntrances(normalized, from, to, passageWidth);
            if (normalized.Count >= 2)
            {
                connection.PathPoints = normalized;
            }

            List<DungeonPathPointRecord> directPath;
            if (connection.PathPoints.Count > 2 && TryBuildNearStraightDungeonPath(level, from, to, passageWidth, out directPath))
            {
                connection.PathPoints = directPath;
                return;
            }

            if (DungeonConnectionPathNeedsRepair(level, connection, connection.PathPoints, from, to, passageWidth))
            {
                // Если старый маршрут всё равно режет комнату, строим его заново тем же
                // безопасным маршрутизатором, которым пользуется ручное соединение комнат.
                connection.PathPoints = BuildEditorDungeonPathIgnoringConnection(level, connection, from, to, passageWidth);
                ClampDungeonSelectedPathPoint(connection);
            }
        }

        private List<DungeonPathPointRecord> NormalizeDungeonPathEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            points = CleanDungeonPathPoints((points ?? new List<DungeonPathPointRecord>())
                .Where(point => point != null)
                .Select(CloneDungeonPathPoint)
                .ToList());
            if (points.Count < 2 || from == null || to == null) return points;

            DungeonPathPointRecord startTarget = points.Count > 1
                ? points[1]
                : new DungeonPathPointRecord { X = RoomCenterX(to), Y = RoomCenterY(to) };
            points[0] = RoomEdgeGridPoint(from, startTarget.X, startTarget.Y, passageWidth);

            int lastIndex = points.Count - 1;
            DungeonPathPointRecord endTarget = points.Count > 1
                ? points[lastIndex - 1]
                : new DungeonPathPointRecord { X = RoomCenterX(from), Y = RoomCenterY(from) };
            points[lastIndex] = RoomEdgeGridPoint(to, endTarget.X, endTarget.Y, passageWidth);

            return CleanDungeonPathPoints(points);
        }

        private bool DungeonStoredConnectionPathIsCanonical(DungeonLevelRecord level, DungeonConnectionRecord connection, List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            if (level == null || points == null || points.Count < 2 || from == null || to == null) return false;
            if (!DungeonPathHasValidRoomEntrances(points, from, to, passageWidth)) return false;
            if (DungeonGeometry.PathHasTinyStep(points, 1.05)) return false;
            if (DungeonGeometry.PathHasTightSelfParallelRun(points, 1.05, 0.75)) return false;
            if (DungeonPathHitsRoom(level, points, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathHitsLinkedRoomInterior(level, points, from.Id, to.Id)) return false;
            if (DungeonPathPassesUnderLinkedRoom(level, points, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathRunsTooCloseToExisting(level, points, connection)) return false;
            return true;
        }

        private bool DungeonConnectionPathNeedsRepair(
            DungeonLevelRecord level,
            DungeonConnectionRecord connection,
            List<DungeonPathPointRecord> points,
            DungeonRoomRecord from,
            DungeonRoomRecord to,
            int passageWidth)
        {
            if (points == null || points.Count < 2 || from == null || to == null) return true;
            return !DungeonPathHasValidRoomEntrances(points, from, to, passageWidth)
                || DungeonGeometry.PathHasTinyStep(points, 1.05)
                || DungeonGeometry.PathHasTightSelfParallelRun(points, 1.05, 0.75)
                || DungeonPathHitsRoom(level, points, from.Id, to.Id, passageWidth)
                || DungeonPathHitsLinkedRoomInterior(level, points, from.Id, to.Id)
                || DungeonPathPassesUnderLinkedRoom(level, points, from.Id, to.Id, passageWidth)
                || DungeonPathRunsTooCloseToExisting(level, points, connection);
        }

        private List<DungeonPathPointRecord> BuildEditorDungeonPathIgnoringConnection(
            DungeonLevelRecord level,
            DungeonConnectionRecord connection,
            DungeonRoomRecord from,
            DungeonRoomRecord to,
            int passageWidth)
        {
            if (level == null || connection == null || level.Connections == null || !level.Connections.Contains(connection))
            {
                return BuildEditorDungeonPath(level, from, to, passageWidth);
            }

            int index = level.Connections.IndexOf(connection);
            level.Connections.RemoveAt(index);
            try
            {
                return BuildEditorDungeonPath(level, from, to, passageWidth);
            }
            finally
            {
                level.Connections.Insert(Math.Min(index, level.Connections.Count), connection);
            }
        }

        private void ClampDungeonSelectedPathPoint(DungeonConnectionRecord connection)
        {
            if (!ReferenceEquals(connection, dungeonSelectedConnection)) return;
            int count = connection == null || connection.PathPoints == null ? 0 : connection.PathPoints.Count;
            if (count <= 0)
            {
                dungeonSelectedPathPointIndex = -1;
                dungeonDragPathPointIndex = -1;
                return;
            }

            dungeonSelectedPathPointIndex = Math.Min(dungeonSelectedPathPointIndex, count - 1);
            dungeonDragPathPointIndex = Math.Min(dungeonDragPathPointIndex, count - 1);
        }

        private bool TryBuildNearStraightDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;
            passageWidth = Math.Max(1, Math.Min(4, passageWidth));
            DungeonPathPointRecord start = RoomEdgeGridPoint(from, RoomCenterX(to), RoomCenterY(to), passageWidth);
            DungeonPathPointRecord end = RoomEdgeGridPoint(to, RoomCenterX(from), RoomCenterY(from), passageWidth);
            if (!ShouldPreferStraightDungeonPath(start, end)) return false;

            List<DungeonPathPointRecord> candidate = CleanDungeonPathPoints(new List<DungeonPathPointRecord> { start, end });
            candidate = NormalizeDungeonPathEntrances(candidate, from, to, passageWidth);
            if (candidate.Count != 2) return false;
            if (DungeonGeometry.UsesBoxEdges(from) && DungeonGeometry.UsesBoxEdges(to) && !DungeonPathIsOrthogonal(candidate)) return false;
            if (!DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathRunsTooCloseToExisting(level, candidate)) return false;
            path = candidate;
            return true;
        }

        private bool TryBuildAxisAlignedDungeonPath(DungeonLevelRecord level, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth, out List<DungeonPathPointRecord> path)
        {
            path = null;
            if (level == null || from == null || to == null) return false;

            passageWidth = Math.Max(1, Math.Min(4, passageWidth));
            double clearance = DungeonPassageCornerClearance(passageWidth);
            List<DungeonPathPointRecord> candidate = null;

            if (from.X + from.Width <= to.X || to.X + to.Width <= from.X)
            {
                double overlapTop = Math.Max(from.Y, to.Y);
                double overlapBottom = Math.Min(from.Y + from.Height, to.Y + to.Height);
                double low = overlapTop;
                double high = overlapBottom;
                if (DungeonGeometry.UsesBoxEdges(from))
                {
                    low = Math.Max(low, from.Y + clearance);
                    high = Math.Min(high, from.Y + from.Height - clearance);
                }

                if (DungeonGeometry.UsesBoxEdges(to))
                {
                    low = Math.Max(low, to.Y + clearance);
                    high = Math.Min(high, to.Y + to.Height - clearance);
                }

                if (overlapBottom > overlapTop)
                {
                    double y = low <= high
                        ? ClampDouble((RoomCenterY(from) + RoomCenterY(to)) / 2.0, low, high)
                        : (overlapTop + overlapBottom) / 2.0;
                    bool useCornerClearance = low <= high;
                    bool fromIsLeft = from.X + from.Width <= to.X;
                    DungeonPathPointRecord start;
                    DungeonPathPointRecord end;
                    if (DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(from, true, fromIsLeft, y, passageWidth, useCornerClearance, out start)
                        && DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(to, true, !fromIsLeft, y, passageWidth, useCornerClearance, out end))
                    {
                        candidate = new List<DungeonPathPointRecord> { start, end };
                    }
                }
            }
            else if (from.Y + from.Height <= to.Y || to.Y + to.Height <= from.Y)
            {
                double overlapLeft = Math.Max(from.X, to.X);
                double overlapRight = Math.Min(from.X + from.Width, to.X + to.Width);
                double low = overlapLeft;
                double high = overlapRight;
                if (DungeonGeometry.UsesBoxEdges(from))
                {
                    low = Math.Max(low, from.X + clearance);
                    high = Math.Min(high, from.X + from.Width - clearance);
                }

                if (DungeonGeometry.UsesBoxEdges(to))
                {
                    low = Math.Max(low, to.X + clearance);
                    high = Math.Min(high, to.X + to.Width - clearance);
                }

                if (overlapRight > overlapLeft)
                {
                    double x = low <= high
                        ? ClampDouble((RoomCenterX(from) + RoomCenterX(to)) / 2.0, low, high)
                        : (overlapLeft + overlapRight) / 2.0;
                    bool useCornerClearance = low <= high;
                    bool fromIsAbove = from.Y + from.Height <= to.Y;
                    DungeonPathPointRecord start;
                    DungeonPathPointRecord end;
                    if (DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(from, false, fromIsAbove, x, passageWidth, useCornerClearance, out start)
                        && DungeonGeometry.TryFindAxisAlignedPassageEdgePoint(to, false, !fromIsAbove, x, passageWidth, useCornerClearance, out end))
                    {
                        candidate = new List<DungeonPathPointRecord> { start, end };
                    }
                }
            }

            if (candidate == null) return false;
            candidate = CleanDungeonPathPoints(candidate);
            if (candidate.Count != 2) return false;
            if (!DungeonPathHasValidRoomEntrances(candidate, from, to, passageWidth)) return false;
            if (DungeonPathHitsRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathHitsLinkedRoomInterior(level, candidate, from.Id, to.Id)) return false;
            if (DungeonPathPassesUnderLinkedRoom(level, candidate, from.Id, to.Id, passageWidth)) return false;
            if (DungeonPathRunsTooCloseToExisting(level, candidate)) return false;
            path = candidate;
            return true;
        }

        private static bool ShouldPreferStraightDungeonPath(DungeonPathPointRecord start, DungeonPathPointRecord end)
        {
            if (start == null || end == null) return false;
            const double alignmentTolerance = 0.45;
            return Math.Abs(start.X - end.X) <= alignmentTolerance
                || Math.Abs(start.Y - end.Y) <= alignmentTolerance;
        }

        private static double DungeonPassageCornerClearance(int passageWidth)
        {
            int width = Math.Max(1, Math.Min(4, passageWidth));
            return Math.Min(1.0, 0.45 + width * 0.13);
        }

        private DungeonPathPointRecord CloneDungeonPathPoint(DungeonPathPointRecord point)
        {
            return point == null
                ? new DungeonPathPointRecord()
                : new DungeonPathPointRecord { X = point.X, Y = point.Y };
        }

        private static double DungeonGridDistance(DungeonPathPointRecord a, DungeonPathPointRecord b)
        {
            if (a == null || b == null) return double.MaxValue;
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private bool TryBuildAdjacentDungeonPassageGrid(DungeonRoomRecord a, DungeonRoomRecord b, out DungeonPathPointRecord start, out DungeonPathPointRecord end)
        {
            start = null;
            end = null;
            if (a == null || b == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(a) || !DungeonGeometry.UsesBoxEdges(b)) return false;
            double span;
            const double half = 0.1;

            if (a.X + a.Width == b.X || b.X + b.Width == a.X)
            {
                double sharedTop = Math.Max(a.Y, b.Y);
                double sharedBottom = Math.Min(a.Y + a.Height, b.Y + b.Height);
                span = sharedBottom - sharedTop;
                if (span <= 0) return false;

                double x = a.X + a.Width == b.X ? a.X + a.Width : b.X + b.Width;
                double y = sharedTop + span / 2.0;
                start = new DungeonPathPointRecord { X = x - half, Y = y };
                end = new DungeonPathPointRecord { X = x + half, Y = y };
                return true;
            }

            if (a.Y + a.Height == b.Y || b.Y + b.Height == a.Y)
            {
                double sharedLeft = Math.Max(a.X, b.X);
                double sharedRight = Math.Min(a.X + a.Width, b.X + b.Width);
                span = sharedRight - sharedLeft;
                if (span <= 0) return false;

                double x = sharedLeft + span / 2.0;
                double y = a.Y + a.Height == b.Y ? a.Y + a.Height : b.Y + b.Height;
                start = new DungeonPathPointRecord { X = x, Y = y - half };
                end = new DungeonPathPointRecord { X = x, Y = y + half };
                return true;
            }

            return false;
        }

        private DungeonPathPointRecord RoomEdgeGridPoint(DungeonRoomRecord room, double targetX, double targetY)
        {
            return RoomEdgeGridPoint(room, targetX, targetY, 1);
        }

        private DungeonPathPointRecord RoomEdgeGridPoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth)
        {
            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, targetX, targetY, passageWidth, out x, out y, out orientation);
            return new DungeonPathPointRecord { X = x, Y = y };
        }

        private DungeonPathPointRecord RoomOutsideGridPoint(DungeonRoomRecord room, double targetX, double targetY, int passageWidth)
        {
            return DungeonGeometry.OutsidePassagePoint(room, targetX, targetY, passageWidth, 0.85, false);
        }

        private PointF DungeonGridPointToScreen(DungeonPathPointRecord point, float offsetX, float offsetY, float scale)
        {
            if (point == null) return PointF.Empty;
            return new PointF(offsetX + (float)point.X * scale, offsetY + (float)point.Y * scale);
        }

        private PointF DungeonConnectionElbow(PointF a, PointF b)
        {
            return Math.Abs(a.X - b.X) >= Math.Abs(a.Y - b.Y)
                ? new PointF(b.X, a.Y)
                : new PointF(a.X, b.Y);
        }

        private void DrawDungeonDoorEntity(Graphics graphics, DungeonDoorRecord door, float offsetX, float offsetY, float scale)
        {
            if (door == null) return;
            bool secretDoor = string.Equals(door.Kind, "SecretDoor", StringComparison.OrdinalIgnoreCase);
            bool secretPassage = string.Equals(door.Kind, "SecretPassage", StringComparison.OrdinalIgnoreCase);
            bool selected = dungeonSelectedDoor != null && dungeonSelectedDoor.Id == door.Id;

            using (GraphicsPath path = CreateDungeonDoorPath(door, offsetX, offsetY, scale))
            using (Brush fill = new SolidBrush(secretPassage
                ? Color.FromArgb(225, 204, 190, 128)
                : secretDoor ? Color.FromArgb(225, 120, 118, 104) : Color.WhiteSmoke))
            using (Pen outline = new Pen(selected ? Color.Gold : secretPassage ? Color.FromArgb(70, 48, 24) : Color.FromArgb(50, 38, 28), Math.Max(selected ? 2.2f : 1.3f, scale * (selected ? 0.085f : 0.055f))))
            {
                if (secretDoor || secretPassage) outline.DashStyle = DashStyle.Dash;
                graphics.FillPath(fill, path);
                graphics.DrawPath(outline, path);
            }

            if (secretPassage)
            {
                PointF center = DungeonDoorScreenPoint(door, offsetX, offsetY, scale);
                using (Brush brush = new SolidBrush(Color.FromArgb(45, 34, 24)))
                {
                    float dot = Math.Max(2.5f, scale * 0.08f);
                    graphics.FillEllipse(brush, center.X - dot / 2f, center.Y - dot / 2f, dot, dot);
                }
            }
        }

        private RectangleF DungeonDoorBounds(DungeonDoorRecord door, float offsetX, float offsetY, float scale)
        {
            using (GraphicsPath path = CreateDungeonDoorPath(door, offsetX, offsetY, scale))
            {
                return path.GetBounds();
            }
        }

        private GraphicsPath CreateDungeonDoorPath(DungeonDoorRecord door, float offsetX, float offsetY, float scale)
        {
            PointF center = DungeonDoorScreenPoint(door, offsetX, offsetY, scale);
            int widthCells = DungeonDoorPassageWidth(door);
            float longSide = Math.Max(12f, scale * (0.58f + Math.Max(0, widthCells - 1) * 0.22f));
            float shortSide = Math.Max(4f, scale * 0.16f);
            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(new RectangleF(-longSide / 2f, -shortSide / 2f, longSide, shortSide));
            using (Matrix matrix = new Matrix())
            {
                matrix.Rotate(DungeonDoorAngleDegrees(door, center, offsetX, offsetY, scale), MatrixOrder.Append);
                matrix.Translate(center.X, center.Y, MatrixOrder.Append);
                path.Transform(matrix);
            }

            return path;
        }

        private PointF DungeonDoorScreenPoint(DungeonDoorRecord door, float offsetX, float offsetY, float scale)
        {
            return new PointF(offsetX + (float)door.X * scale, offsetY + (float)door.Y * scale);
        }

        private float DungeonDoorAngleDegrees(DungeonDoorRecord door, PointF center, float offsetX, float offsetY, float scale)
        {
            if (door != null && !string.IsNullOrWhiteSpace(door.Orientation))
            {
                return DoorOrientationToAngle(door.Orientation);
            }

            PointF segmentA;
            PointF segmentB;
            if (TryFindDoorConnectionSegment(door, center, offsetX, offsetY, scale, out segmentA, out segmentB))
            {
                return NormalizeDungeonDoorAngle(DungeonSegmentAngle(segmentA, segmentB) + 90.0);
            }

            return DoorOrientationToAngle(door == null ? "" : door.Orientation);
        }

        private bool TryFindDoorConnectionSegment(DungeonDoorRecord door, PointF center, float offsetX, float offsetY, float scale, out PointF segmentA, out PointF segmentB)
        {
            segmentA = PointF.Empty;
            segmentB = PointF.Empty;
            if (door == null || string.IsNullOrWhiteSpace(door.ToRoomId)) return false;
            DungeonLevelRecord level = SelectedDungeonLevel();
            if (level == null || level.Connections == null || level.Rooms == null) return false;
            DungeonConnectionRecord connection = level.Connections.FirstOrDefault(c => SameDungeonDoorConnection(door, c.FromRoomId, c.ToRoomId));
            if (connection == null) return false;
            DungeonPathPointRecord nearest;
            DungeonPathPointRecord gridA;
            DungeonPathPointRecord gridB;
            double distance;
            if (TryNearestLegalDungeonConnectionGridPoint(level, connection, new PointF((float)door.X, (float)door.Y), out nearest, out gridA, out gridB, out distance))
            {
                segmentA = DungeonGridPointToScreen(gridA, offsetX, offsetY, scale);
                segmentB = DungeonGridPointToScreen(gridB, offsetX, offsetY, scale);
                return true;
            }

            return false;
        }

        private string DungeonDoorOrientationForSegment(PointF a, PointF b, bool alignWithSegment)
        {
            double angle = DungeonSegmentAngle(a, b);
            if (!alignWithSegment) angle += 90.0;
            return DungeonDoorOrientationFromAngle(angle);
        }

        private string DungeonDoorOrientationForGridSegment(DungeonPathPointRecord a, DungeonPathPointRecord b, bool alignWithSegment)
        {
            if (a == null || b == null) return "Vertical";
            return DungeonDoorOrientationForSegment(new PointF((float)a.X, (float)a.Y), new PointF((float)b.X, (float)b.Y), alignWithSegment);
        }

        private double DungeonSegmentAngle(PointF a, PointF b)
        {
            return Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI;
        }

        private string DungeonDoorOrientationFromAngle(double angle)
        {
            angle = NormalizeDungeonDoorAngle(angle);
            double abs = Math.Abs(angle);
            if (abs <= 0.01 || Math.Abs(abs - 180.0) <= 0.01) return "Horizontal";
            if (Math.Abs(abs - 90.0) <= 0.01) return "Vertical";
            return "Angle:" + angle.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private float DoorOrientationToAngle(string orientation)
        {
            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase)) return 0f;
            if (string.Equals(orientation, "Vertical", StringComparison.OrdinalIgnoreCase)) return 90f;
            if (!string.IsNullOrWhiteSpace(orientation)
                && orientation.Trim().StartsWith("Angle:", StringComparison.OrdinalIgnoreCase))
            {
                double angle;
                if (double.TryParse(orientation.Trim().Substring("Angle:".Length), NumberStyles.Float, CultureInfo.InvariantCulture, out angle))
                {
                    return NormalizeDungeonDoorAngle(angle);
                }
            }

            return 90f;
        }

        private float NormalizeDungeonDoorAngle(double angle)
        {
            while (angle <= -180.0) angle += 360.0;
            while (angle > 180.0) angle -= 360.0;
            return (float)angle;
        }

        private int DungeonDoorPassageWidth(DungeonDoorRecord door)
        {
            if (door == null || string.IsNullOrWhiteSpace(door.ToRoomId)) return 1;
            DungeonLevelRecord level = SelectedDungeonLevel();
            DungeonConnectionRecord connection = level == null || level.Connections == null
                ? null
                : level.Connections.FirstOrDefault(c => SameDungeonDoorConnection(door, c.FromRoomId, c.ToRoomId));
            return Math.Max(1, Math.Min(4, connection == null ? 1 : connection.PassageWidth));
        }

        private bool IsSecretDungeonDoor(DungeonConnectionRecord connection)
        {
            if (connection == null) return false;
            return string.Equals(connection.Kind, "SecretDoor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(connection.DoorKind, "SecretDoor", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDungeonOverpass(DungeonConnectionRecord connection)
        {
            return connection != null
                && string.Equals(connection.Kind, "Overpass", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryBuildAdjacentDungeonPassage(
            DungeonRoomRecord a,
            DungeonRoomRecord b,
            float offsetX,
            float offsetY,
            float scale,
            out PointF start,
            out PointF end)
        {
            start = PointF.Empty;
            end = PointF.Empty;
            if (!DungeonGeometry.UsesBoxEdges(a) || !DungeonGeometry.UsesBoxEdges(b)) return false;
            RectangleF ar = RoomScreenRect(a, offsetX, offsetY, scale);
            RectangleF br = RoomScreenRect(b, offsetX, offsetY, scale);
            float span;

            if (a.X + a.Width == b.X || b.X + b.Width == a.X)
            {
                float sharedTop = Math.Max(ar.Top, br.Top);
                float sharedBottom = Math.Min(ar.Bottom, br.Bottom);
                span = sharedBottom - sharedTop;
                if (span <= 0f) return false;

                float x = a.X + a.Width == b.X ? ar.Right : br.Right;
                float y = sharedTop + span / 2f;
                float half = Math.Max(2f, scale * 0.10f);
                start = new PointF(x - half, y);
                end = new PointF(x + half, y);
                return true;
            }

            if (a.Y + a.Height == b.Y || b.Y + b.Height == a.Y)
            {
                float sharedLeft = Math.Max(ar.Left, br.Left);
                float sharedRight = Math.Min(ar.Right, br.Right);
                span = sharedRight - sharedLeft;
                if (span <= 0f) return false;

                float x = sharedLeft + span / 2f;
                float y = a.Y + a.Height == b.Y ? ar.Bottom : br.Bottom;
                float half = Math.Max(2f, scale * 0.10f);
                start = new PointF(x, y - half);
                end = new PointF(x, y + half);
                return true;
            }

            return false;
        }

        private PointF RoomEdgePoint(DungeonRoomRecord from, DungeonRoomRecord to, float offsetX, float offsetY, float scale)
        {
            return RoomEdgePoint(from, RoomCenter(to, offsetX, offsetY, scale), offsetX, offsetY, scale);
        }

        private PointF RoomEdgePoint(DungeonRoomRecord from, PointF target, float offsetX, float offsetY, float scale)
        {
            double gridX = (target.X - offsetX) / scale;
            double gridY = (target.Y - offsetY) / scale;
            DungeonPathPointRecord edge = RoomEdgeGridPoint(from, gridX, gridY);
            return DungeonGridPointToScreen(edge, offsetX, offsetY, scale);
        }

        private RectangleF RoomScreenRect(DungeonRoomRecord room, float offsetX, float offsetY, float scale)
        {
            if (room == null) return RectangleF.Empty;
            return new RectangleF(
                offsetX + room.X * scale,
                offsetY + room.Y * scale,
                Math.Max(scale * 1.4f, room.Width * scale),
                Math.Max(scale * 1.4f, room.Height * scale));
        }

        private bool TryGetPathMidSegment(GraphicsPath path, out PointF a, out PointF b)
        {
            a = PointF.Empty;
            b = PointF.Empty;
            if (path == null || path.PointCount < 2) return false;
            PointF[] points = path.PathPoints;
            float total = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                total += SegmentLength(points[i - 1], points[i]);
            }

            float halfway = total / 2f;
            float walked = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                float segment = SegmentLength(points[i - 1], points[i]);
                if (walked + segment >= halfway)
                {
                    a = points[i - 1];
                    b = points[i];
                    return true;
                }

                walked += segment;
            }

            a = points[points.Length - 2];
            b = points[points.Length - 1];
            return true;
        }

        private float SegmentLength(PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void DrawDungeonRoomShape(Graphics graphics, DungeonRoomRecord room, RectangleF rect, bool selectedRoom, bool pendingStart)
        {
            Color fill = DungeonRoomColor(room.Kind);
            Color outline = pendingStart ? Color.DeepSkyBlue : selectedRoom ? Color.Gold : Color.FromArgb(56, 44, 30);
            float outlineWidth = pendingStart || selectedRoom ? 3f : 1.5f;
            using (GraphicsPath path = CreateDungeonRoomPath(room, rect))
            using (Brush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(outline, outlineWidth))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath CreateDungeonRoomPath(DungeonRoomRecord room, RectangleF rect)
        {
            GraphicsPath path = new GraphicsPath();
            string shape = room == null ? "Rectangle" : room.Shape;
            if (string.Equals(shape, "Circle", StringComparison.OrdinalIgnoreCase) || string.Equals(shape, "Oval", StringComparison.OrdinalIgnoreCase))
            {
                path.AddEllipse(rect);
                return path;
            }

            if (string.Equals(shape, "Cavern", StringComparison.OrdinalIgnoreCase))
            {
                float wobbleX = Math.Max(3f, rect.Width * 0.08f);
                float wobbleY = Math.Max(3f, rect.Height * 0.08f);
                PointF[] points =
                {
                    new PointF(rect.Left + wobbleX, rect.Top),
                    new PointF(rect.Right - wobbleX * 0.5f, rect.Top + wobbleY),
                    new PointF(rect.Right, rect.Top + rect.Height * 0.42f),
                    new PointF(rect.Right - wobbleX, rect.Bottom - wobbleY * 0.4f),
                    new PointF(rect.Left + rect.Width * 0.48f, rect.Bottom),
                    new PointF(rect.Left, rect.Bottom - wobbleY),
                    new PointF(rect.Left + wobbleX * 0.4f, rect.Top + rect.Height * 0.45f)
                };
                path.AddClosedCurve(points, 0.45f);
                return path;
            }

            path.AddRectangle(rect);
            return path;
        }

        private PointF RoomCenter(DungeonRoomRecord room, float offsetX, float offsetY, float scale)
        {
            return new PointF(offsetX + (room.X + room.Width / 2f) * scale, offsetY + (room.Y + room.Height / 2f) * scale);
        }

        private Color DungeonRoomColor(string kind)
        {
            if (string.Equals(kind, "Monster", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(217, 151, 119);
            if (string.Equals(kind, "Trap", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(215, 188, 91);
            if (string.Equals(kind, "Unique", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(157, 191, 151);
            if (string.Equals(kind, "Stairs", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(150, 170, 210);
            if (string.Equals(kind, "Entrance", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(205, 205, 174);
            return Color.FromArgb(230, 222, 196);
        }

        private bool TryViewPointToDungeonGrid(Point point, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (dungeonRenderLayout == null || dungeonRenderLayout.Scale <= 0) return false;
            x = (int)Math.Floor((point.X - dungeonRenderLayout.OffsetX) / dungeonRenderLayout.Scale);
            y = (int)Math.Floor((point.Y - dungeonRenderLayout.OffsetY) / dungeonRenderLayout.Scale);
            return true;
        }

        private bool CanPlaceDungeonRoom(DungeonLevelRecord level, DungeonRoomRecord room, int x, int y, string ignoreRoomId)
        {
            if (level == null || room == null) return false;
            Rectangle candidate = new Rectangle(x, y, Math.Max(1, room.Width), Math.Max(1, room.Height));
            if (candidate.Left < 0 || candidate.Top < 0 || candidate.Right >= level.Width || candidate.Bottom >= level.Height) return false;
            foreach (DungeonRoomRecord other in level.Rooms ?? new List<DungeonRoomRecord>())
            {
                if (other == null) continue;
                if (!string.IsNullOrWhiteSpace(ignoreRoomId) && other.Id == ignoreRoomId) continue;
                Rectangle occupied = new Rectangle(other.X, other.Y, Math.Max(1, other.Width), Math.Max(1, other.Height));
                if (candidate.IntersectsWith(occupied)) return false;
            }

            DungeonRoomRecord candidateRoom = new DungeonRoomRecord
            {
                X = x,
                Y = y,
                Width = Math.Max(1, room.Width),
                Height = Math.Max(1, room.Height),
                Shape = string.IsNullOrWhiteSpace(room.Shape) ? "Rectangle" : room.Shape
            };
            foreach (DungeonConnectionRecord connection in DungeonConnectionSnapshot(level))
            {
                if (connection == null || IsDungeonOverpass(connection)) continue;
                if (!string.IsNullOrWhiteSpace(ignoreRoomId)
                    && (string.Equals(connection.FromRoomId, ignoreRoomId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(connection.ToRoomId, ignoreRoomId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                List<DungeonPathPointRecord> points = BuildDungeonConnectionGridPoints(level, connection);
                double margin = DungeonGeometry.PassageHalfWidthCells(connection.PassageWidth);
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(points[i - 1], points[i], candidateRoom, margin)) return false;
                }
            }

            return true;
        }

        private static double RoomCenterX(DungeonRoomRecord room)
        {
            return room == null ? 0 : room.X + room.Width / 2.0;
        }

        private static double RoomCenterY(DungeonRoomRecord room)
        {
            return room == null ? 0 : room.Y + room.Height / 2.0;
        }

        private static bool AlmostSame(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        private static List<DungeonPathPointRecord> CleanDungeonPathPoints(List<DungeonPathPointRecord> points)
        {
            List<DungeonPathPointRecord> cleaned = new List<DungeonPathPointRecord>();
            foreach (DungeonPathPointRecord point in points ?? new List<DungeonPathPointRecord>())
            {
                if (point == null) continue;
                if (cleaned.Count > 0 && AlmostSame(cleaned[cleaned.Count - 1].X, point.X) && AlmostSame(cleaned[cleaned.Count - 1].Y, point.Y)) continue;
                cleaned.Add(point);
            }

            for (int i = cleaned.Count - 2; i >= 1; i--)
            {
                DungeonPathPointRecord previous = cleaned[i - 1];
                DungeonPathPointRecord current = cleaned[i];
                DungeonPathPointRecord next = cleaned[i + 1];
                bool sameX = AlmostSame(previous.X, current.X) && AlmostSame(current.X, next.X);
                bool sameY = AlmostSame(previous.Y, current.Y) && AlmostSame(current.Y, next.Y);
                if (sameX || sameY) cleaned.RemoveAt(i);
            }

            return cleaned;
        }

        private bool DungeonPathHasValidRoomEntrances(List<DungeonPathPointRecord> points, DungeonRoomRecord from, DungeonRoomRecord to, int passageWidth)
        {
            if (points == null || points.Count < 2) return false;
            return DungeonRoomEntranceIsValid(from, points[0], points[1], passageWidth)
                && DungeonRoomEntranceIsValid(to, points[points.Count - 1], points[points.Count - 2], passageWidth);
        }

        private bool DungeonRoomEntranceIsValid(DungeonRoomRecord room, DungeonPathPointRecord edge, DungeonPathPointRecord outside, int passageWidth)
        {
            if (room == null || edge == null || outside == null) return false;
            if (!DungeonGeometry.UsesBoxEdges(room))
            {
                return !DungeonGeometry.IsPointInsideRoomInterior(room, outside.X, outside.Y, 0.02);
            }

            double x;
            double y;
            string orientation;
            DungeonGeometry.FindRoomPassageEdgePoint(room, edge.X, edge.Y, passageWidth, out x, out y, out orientation);
            const double entranceTolerance = 0.12;
            if (Math.Abs(edge.X - x) > entranceTolerance || Math.Abs(edge.Y - y) > entranceTolerance) return false;

            if (string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase))
            {
                if (Math.Abs(edge.X - outside.X) > entranceTolerance) return false;
                return edge.Y <= RoomCenterY(room)
                    ? outside.Y <= edge.Y + 0.02
                    : outside.Y >= edge.Y - 0.02;
            }

            if (Math.Abs(edge.Y - outside.Y) > entranceTolerance) return false;
            return edge.X <= RoomCenterX(room)
                ? outside.X <= edge.X + 0.02
                : outside.X >= edge.X - 0.02;
        }

        private bool DungeonPathHitsRoom(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == fromRoomId || room.Id == toRoomId) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(points[i - 1], points[i], room, margin)) return true;
                }
            }

            return false;
        }

        private double DungeonPathRoomHitPenalty(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return 0;
            double penalty = 0;
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            foreach (DungeonRoomRecord room in level.Rooms)
            {
                if (room == null || room.Id == fromRoomId || room.Id == toRoomId) continue;
                for (int i = 1; i < points.Count; i++)
                {
                    if (DungeonGeometry.SegmentCrossesRoomBuffer(points[i - 1], points[i], room, margin)) penalty += 10000;
                }
            }

            return penalty;
        }

        private bool DungeonPathHitsLinkedRoomInterior(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r != null && r.Id == fromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r != null && r.Id == toRoomId);
            foreach (DungeonPathPointRecord point in points)
            {
                if (point == null) continue;
                if (from != null && DungeonGeometry.IsPointInsideRoomInterior(from, point.X, point.Y, 0.01)) return true;
                if (to != null && DungeonGeometry.IsPointInsideRoomInterior(to, point.X, point.Y, 0.01)) return true;
            }

            for (int i = 1; i < points.Count; i++)
            {
                if (from != null && DungeonGeometry.SegmentCrossesRoomInterior(points[i - 1], points[i], from)) return true;
                if (to != null && DungeonGeometry.SegmentCrossesRoomInterior(points[i - 1], points[i], to)) return true;
                if (from != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(points[i - 1], points[i], from, 0.08)) return true;
                if (to != null && DungeonGeometry.SegmentRunsAlongRoomBoundary(points[i - 1], points[i], to, 0.08)) return true;
            }

            return false;
        }

        private bool DungeonPathPassesUnderLinkedRoom(DungeonLevelRecord level, List<DungeonPathPointRecord> points, string fromRoomId, string toRoomId, int passageWidth)
        {
            if (level == null || level.Rooms == null || points == null || points.Count < 2) return false;
            DungeonRoomRecord from = level.Rooms.FirstOrDefault(r => r != null && r.Id == fromRoomId);
            DungeonRoomRecord to = level.Rooms.FirstOrDefault(r => r != null && r.Id == toRoomId);
            double margin = DungeonGeometry.PassageHalfWidthCells(passageWidth);
            for (int i = 1; i < points.Count; i++)
            {
                DungeonPathPointRecord a = points[i - 1];
                DungeonPathPointRecord b = points[i];
                if (a == null || b == null) continue;

                bool entranceFrom = i == 1;
                bool entranceTo = i == points.Count - 1;
                if (!entranceFrom && from != null && DungeonGeometry.SegmentCrossesRoomBuffer(a, b, from, margin)) return true;
                if (!entranceTo && to != null && DungeonGeometry.SegmentCrossesRoomBuffer(a, b, to, margin)) return true;
            }

            return false;
        }

        private double DungeonPathCost(DungeonLevelRecord level, List<DungeonPathPointRecord> points)
        {
            if (points == null || points.Count < 2) return double.MaxValue;
            double cost = DungeonGeometry.PathLength(points) + DungeonGeometry.PathShapePenalty(points);

            if (level == null || level.Connections == null) return cost;
            List<IList<DungeonPathPointRecord>> existingPaths = new List<IList<DungeonPathPointRecord>>();
            foreach (DungeonConnectionRecord existing in level.Connections)
            {
                if (existing == null || IsDungeonOverpass(existing)) continue;
                if (existing.PathPoints == null || existing.PathPoints.Count < 2) continue;
                // Оценка стоимости не должна перестраивать чужой коридор, иначе возможен цикл
                // BuildDungeonConnectionGridPoints -> BuildEditorDungeonPath -> DungeonPathCost.
                List<DungeonPathPointRecord> existingPoints = CleanDungeonPathPoints(existing.PathPoints.Select(CloneDungeonPathPoint).ToList());
                if (existingPoints.Count < 2) continue;
                existingPaths.Add(existingPoints);
            }

            return cost + DungeonGeometry.PathInteractionPenalty(points, existingPaths);
        }

        private double DungeonPathCost(List<DungeonPathPointRecord> points, IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            if (points == null || points.Count < 2) return double.MaxValue;
            return DungeonGeometry.PathLength(points)
                + DungeonGeometry.PathShapePenalty(points)
                + DungeonGeometry.PathInteractionPenalty(points, existingPaths);
        }

        private List<IList<DungeonPathPointRecord>> GetDungeonExistingPathRecords(DungeonLevelRecord level, DungeonConnectionRecord ignore)
        {
            List<IList<DungeonPathPointRecord>> existingPaths = new List<IList<DungeonPathPointRecord>>();
            if (level == null || level.Connections == null) return existingPaths;
            foreach (DungeonConnectionRecord existing in level.Connections)
            {
                if (existing == null || ReferenceEquals(existing, ignore) || IsDungeonOverpass(existing)) continue;
                if (existing.PathPoints == null || existing.PathPoints.Count < 2) continue;
                List<DungeonPathPointRecord> existingPoints = CleanDungeonPathPoints(existing.PathPoints.Select(CloneDungeonPathPoint).ToList());
                if (existingPoints.Count >= 2) existingPaths.Add(existingPoints);
            }

            return existingPaths;
        }

        private bool DungeonPathRunsTooCloseToExisting(DungeonLevelRecord level, List<DungeonPathPointRecord> points)
        {
            return DungeonPathRunsTooCloseToExisting(level, points, null);
        }

        private bool DungeonPathRunsTooCloseToExisting(DungeonLevelRecord level, List<DungeonPathPointRecord> points, DungeonConnectionRecord ignore)
        {
            if (level == null || level.Connections == null || points == null || points.Count < 2) return false;
            List<IList<DungeonPathPointRecord>> existingPaths = new List<IList<DungeonPathPointRecord>>();
            foreach (DungeonConnectionRecord existing in level.Connections)
            {
                if (existing == null || ReferenceEquals(existing, ignore) || IsDungeonOverpass(existing)) continue;
                if (existing.PathPoints == null || existing.PathPoints.Count < 2) continue;
                List<DungeonPathPointRecord> existingPoints = CleanDungeonPathPoints(existing.PathPoints.Select(CloneDungeonPathPoint).ToList());
                if (existingPoints.Count >= 2) existingPaths.Add(existingPoints);
            }

            return DungeonGeometry.PathRunsTooCloseToAny(points, existingPaths, 1.05, 1.25, true);
        }

        private bool DungeonPathRunsTooCloseToExisting(List<DungeonPathPointRecord> points, IList<IList<DungeonPathPointRecord>> existingPaths)
        {
            return DungeonGeometry.PathRunsTooCloseToAny(points, existingPaths, 1.05, 1.25, true);
        }

        private Point FindFreeDungeonRoomPosition(DungeonLevelRecord level, int width, int height, int preferredX, int preferredY, string ignoreRoomId)
        {
            DungeonRoomRecord probe = new DungeonRoomRecord { Width = Math.Max(1, width), Height = Math.Max(1, height) };
            int bestX = Math.Max(1, Math.Min(level.Width - probe.Width - 1, preferredX));
            int bestY = Math.Max(1, Math.Min(level.Height - probe.Height - 1, preferredY));
            int maxRadius = Math.Max(level.Width, level.Height);
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int y = Math.Max(1, bestY - radius); y <= Math.Min(level.Height - probe.Height - 1, bestY + radius); y++)
                {
                    for (int x = Math.Max(1, bestX - radius); x <= Math.Min(level.Width - probe.Width - 1, bestX + radius); x++)
                    {
                        if (Math.Abs(x - bestX) + Math.Abs(y - bestY) > radius) continue;
                        if (CanPlaceDungeonRoom(level, probe, x, y, ignoreRoomId)) return new Point(x, y);
                    }
                }
            }

            return new Point(bestX, bestY);
        }

        private bool SameDungeonConnection(DungeonConnectionRecord connection, string a, string b)
        {
            return connection != null
                && ((connection.FromRoomId == a && connection.ToRoomId == b)
                    || (connection.FromRoomId == b && connection.ToRoomId == a));
        }

        private bool SameDungeonDoorConnection(DungeonDoorRecord door, string a, string b)
        {
            return door != null
                && ((door.FromRoomId == a && door.ToRoomId == b)
                    || (door.FromRoomId == b && door.ToRoomId == a));
        }

        private double DistanceToSegment(Point point, PointF a, PointF b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001)
            {
                double px = point.X - a.X;
                double py = point.Y - a.Y;
                return Math.Sqrt(px * px + py * py);
            }

            double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double x = a.X + t * dx;
            double y = a.Y + t * dy;
            double ox = point.X - x;
            double oy = point.Y - y;
            return Math.Sqrt(ox * ox + oy * oy);
        }

        private PointF NearestPointOnSegment(Point point, PointF a, PointF b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double t = Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001
                ? 0
                : ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            return new PointF((float)(a.X + dx * t), (float)(a.Y + dy * t));
        }

        private double DistanceToPath(Point point, GraphicsPath path)
        {
            if (path == null || path.PointCount < 2) return double.MaxValue;
            PointF[] points = path.PathPoints;
            double best = double.MaxValue;
            for (int i = 1; i < points.Length; i++)
            {
                best = Math.Min(best, DistanceToSegment(point, points[i - 1], points[i]));
            }

            return best;
        }

        private bool TryNearestPointOnPath(Point point, GraphicsPath path, out PointF nearest, out PointF segmentA, out PointF segmentB, out double distance)
        {
            int segmentIndex;
            return TryNearestPointOnPath(point, path, out nearest, out segmentA, out segmentB, out distance, out segmentIndex);
        }

        private bool TryNearestPointOnPath(Point point, GraphicsPath path, out PointF nearest, out PointF segmentA, out PointF segmentB, out double distance, out int segmentIndex)
        {
            return TryNearestPointOnPath(new PointF(point.X, point.Y), path, out nearest, out segmentA, out segmentB, out distance, out segmentIndex);
        }

        private bool TryNearestPointOnPath(PointF point, GraphicsPath path, out PointF nearest, out PointF segmentA, out PointF segmentB, out double distance)
        {
            int segmentIndex;
            return TryNearestPointOnPath(point, path, out nearest, out segmentA, out segmentB, out distance, out segmentIndex);
        }

        private bool TryNearestPointOnPath(PointF point, GraphicsPath path, out PointF nearest, out PointF segmentA, out PointF segmentB, out double distance, out int segmentIndex)
        {
            nearest = PointF.Empty;
            segmentA = PointF.Empty;
            segmentB = PointF.Empty;
            distance = double.MaxValue;
            segmentIndex = -1;
            if (path == null || path.PointCount < 2) return false;
            PointF[] points = path.PathPoints;
            for (int i = 1; i < points.Length; i++)
            {
                PointF a = points[i - 1];
                PointF b = points[i];
                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double t = Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001
                    ? 0
                    : ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
                t = Math.Max(0, Math.Min(1, t));
                PointF candidate = new PointF((float)(a.X + dx * t), (float)(a.Y + dy * t));
                double ox = point.X - candidate.X;
                double oy = point.Y - candidate.Y;
                double candidateDistance = Math.Sqrt(ox * ox + oy * oy);
                if (candidateDistance >= distance) continue;
                distance = candidateDistance;
                nearest = candidate;
                segmentA = a;
                segmentB = b;
                segmentIndex = i - 1;
            }

            return distance < double.MaxValue;
        }

        private void NormalizeDungeonForEditor(DungeonRecord dungeon)
        {
            if (dungeon == null) return;
            MapDataNormalizer.NormalizeDungeon(dungeon);
            if (dungeon.Levels.Count == 0)
            {
                dungeon.Levels.Add(new DungeonLevelRecord());
            }
            foreach (DungeonLevelRecord level in dungeon.Levels)
            {
                if (level.Rooms == null) level.Rooms = new List<DungeonRoomRecord>();
                if (level.Connections == null) level.Connections = new List<DungeonConnectionRecord>();
                if (level.Doors == null) level.Doors = new List<DungeonDoorRecord>();
                foreach (DungeonRoomRecord room in level.Rooms)
                {
                    if (string.IsNullOrWhiteSpace(room.Id)) room.Id = Guid.NewGuid().ToString("N");
                    if (room.LevelNumber <= 0) room.LevelNumber = level.LevelNumber;
                    if (string.IsNullOrWhiteSpace(room.Kind)) room.Kind = "Empty";
                    if (string.IsNullOrWhiteSpace(room.Shape)) room.Shape = "Rectangle";
                    if (room.Width <= 0) room.Width = 2;
                    if (room.Height <= 0) room.Height = 2;
                }
                RepairDungeonRoomOverlaps(level);
            }
        }

        private void RepairDungeonRoomOverlaps(DungeonLevelRecord level)
        {
            if (level == null || level.Rooms == null) return;
            List<DungeonRoomRecord> placed = new List<DungeonRoomRecord>();
            foreach (DungeonRoomRecord room in level.Rooms.OrderBy(r => r.Y).ThenBy(r => r.X).ToList())
            {
                if (room == null) continue;
                room.Width = Math.Max(1, room.Width);
                room.Height = Math.Max(1, room.Height);
                room.X = Math.Max(1, Math.Min(level.Width - room.Width - 1, room.X));
                room.Y = Math.Max(1, Math.Min(level.Height - room.Height - 1, room.Y));
                bool overlaps = placed.Any(other => new Rectangle(room.X, room.Y, room.Width, room.Height)
                    .IntersectsWith(new Rectangle(other.X, other.Y, other.Width, other.Height)));
                if (overlaps)
                {
                    DungeonLevelRecord placedLevel = new DungeonLevelRecord
                    {
                        Width = level.Width,
                        Height = level.Height,
                        Rooms = placed,
                        Connections = new List<DungeonConnectionRecord>(),
                        Doors = new List<DungeonDoorRecord>()
                    };
                    Point free = FindFreeDungeonRoomPosition(placedLevel, room.Width, room.Height, room.X, room.Y, room.Id);
                    room.X = free.X;
                    room.Y = free.Y;
                }
                placed.Add(room);
            }
        }

        private void SelectRoomKind(string kind)
        {
            SelectDungeonItem(cmbDungeonRoomKind, kind);
        }

        private string SelectedDungeonItemValue(ComboBox combo)
        {
            DungeonTypeItem item = combo == null ? null : combo.SelectedItem as DungeonTypeItem;
            return item == null ? "" : item.Value;
        }

        private void FillDungeonRoomKindCombo(string selected)
        {
            if (cmbDungeonRoomKind == null) return;
            cmbDungeonRoomKind.Items.Clear();
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Empty", isEnglish ? "Empty" : "Пустая"));
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Monster", isEnglish ? "Monster" : "Монстры"));
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Trap", isEnglish ? "Trap" : "Ловушка"));
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Unique", isEnglish ? "Unique" : "Особенность"));
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Stairs", isEnglish ? "Stairs" : "Лестница"));
            cmbDungeonRoomKind.Items.Add(new DungeonTypeItem("Entrance", isEnglish ? "Entrance" : "Вход"));
            SelectDungeonItem(cmbDungeonRoomKind, string.IsNullOrWhiteSpace(selected) ? "Empty" : selected);
        }

        private void FillDungeonRoomShapeCombo(string selected)
        {
            if (cmbDungeonRoomShape == null) return;
            cmbDungeonRoomShape.Items.Clear();
            cmbDungeonRoomShape.Items.Add(new DungeonTypeItem("Rectangle", isEnglish ? "Rectangle" : "Прямоугольная"));
            cmbDungeonRoomShape.Items.Add(new DungeonTypeItem("Narrow", isEnglish ? "Narrow" : "Узкая"));
            cmbDungeonRoomShape.Items.Add(new DungeonTypeItem("Circle", isEnglish ? "Circle" : "Круглая"));
            cmbDungeonRoomShape.Items.Add(new DungeonTypeItem("Oval", isEnglish ? "Oval" : "Овальная"));
            cmbDungeonRoomShape.Items.Add(new DungeonTypeItem("Cavern", isEnglish ? "Cavern" : "Пещерная"));
            SelectDungeonItem(cmbDungeonRoomShape, string.IsNullOrWhiteSpace(selected) ? "Rectangle" : selected);
        }

        private void FillDungeonPassageKindCombo(string selected)
        {
            if (cmbDungeonPassageKind == null) return;
            cmbDungeonPassageKind.Items.Clear();
            cmbDungeonPassageKind.Items.Add(new DungeonTypeItem("Corridor", isEnglish ? "Corridor" : "Коридор"));
            cmbDungeonPassageKind.Items.Add(new DungeonTypeItem("Passage", isEnglish ? "Passage" : "Проход"));
            cmbDungeonPassageKind.Items.Add(new DungeonTypeItem("Stairs", isEnglish ? "Stairs" : "Лестница"));
            cmbDungeonPassageKind.Items.Add(new DungeonTypeItem("Overpass", isEnglish ? "Overpass/underpass" : "Над/под"));
            if (string.Equals(selected, "Door", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selected, "SecretDoor", StringComparison.OrdinalIgnoreCase))
            {
                selected = "Passage";
            }
            SelectDungeonItem(cmbDungeonPassageKind, string.IsNullOrWhiteSpace(selected) ? "Corridor" : selected);
        }

        private void FillDungeonDoorKindCombo(string selected)
        {
            if (cmbDungeonDoorKind == null) return;
            cmbDungeonDoorKind.Items.Clear();
            cmbDungeonDoorKind.Items.Add(new DungeonTypeItem("Door", isEnglish ? "Door" : "Дверь"));
            cmbDungeonDoorKind.Items.Add(new DungeonTypeItem("SecretDoor", isEnglish ? "Secret door" : "Тайная дверь"));
            cmbDungeonDoorKind.Items.Add(new DungeonTypeItem("SecretPassage", isEnglish ? "Secret passage" : "Тайный проход"));
            SelectDungeonItem(cmbDungeonDoorKind, string.IsNullOrWhiteSpace(selected) ? "Door" : selected);
            if (cmbDungeonDoorKind.SelectedIndex < 0 && cmbDungeonDoorKind.Items.Count > 0)
            {
                cmbDungeonDoorKind.SelectedIndex = 0;
            }
        }

        private string DisplayRoomTitle(DungeonRoomRecord room)
        {
            if (room == null) return "";
            if (string.IsNullOrWhiteSpace(room.Title)) return LocalizeRoomKind(room.Kind);
            return DisplayDungeonText(room.Title);
        }

        private string LocalizeRoomKind(string kind)
        {
            if (isEnglish) return string.IsNullOrWhiteSpace(kind) ? "Room" : kind;
            if (string.Equals(kind, "Empty", StringComparison.OrdinalIgnoreCase)) return "Пустая";
            if (string.Equals(kind, "Monster", StringComparison.OrdinalIgnoreCase)) return "Монстры";
            if (string.Equals(kind, "Trap", StringComparison.OrdinalIgnoreCase)) return "Ловушка";
            if (string.Equals(kind, "Unique", StringComparison.OrdinalIgnoreCase)) return "Особенность";
            if (string.Equals(kind, "Stairs", StringComparison.OrdinalIgnoreCase)) return "Лестница";
            if (string.Equals(kind, "StairsUp", StringComparison.OrdinalIgnoreCase)) return "Лестница вверх";
            if (string.Equals(kind, "StairsDown", StringComparison.OrdinalIgnoreCase)) return "Лестница вниз";
            if (string.Equals(kind, "Entrance", StringComparison.OrdinalIgnoreCase)) return "Вход";
            return string.IsNullOrWhiteSpace(kind) ? "Комната" : kind;
        }

        private string DisplayDungeonText(string value)
        {
            if (isEnglish || string.IsNullOrWhiteSpace(value)) return value ?? "";
            string translatedMonster = LocalizeDungeonMonster(value);
            if (!string.Equals(translatedMonster, value, StringComparison.OrdinalIgnoreCase)) return translatedMonster;
            string translatedTreasure = LocalizeDungeonTreasure(value);
            if (!string.Equals(translatedTreasure, value, StringComparison.OrdinalIgnoreCase)) return translatedTreasure;
            string translatedTrap = LocalizeDungeonTrap(value);
            if (!string.Equals(translatedTrap, value, StringComparison.OrdinalIgnoreCase)) return translatedTrap;
            switch (value)
            {
                case "Empty chamber": return "Пустая комната";
                case "Occupied chamber": return "Занятая комната";
                case "Hazard": return "Опасность";
                case "Unique feature": return "Особенность";
                case "Entrance": return "Вход";
                case "Stairs down": return "Лестница вниз";
                case "Stairs up": return "Лестница вверх";
                case "The first mapped room of the dungeon.": return "Первая отмеченная комната данжа.";
                case "A stocked encounter room.": return "Комната с подготовленной встречей.";
                case "A dangerous room without an obvious guardian.": return "Опасная комната без очевидного стража.";
                case "A distinctive room intended as a memorable dungeon feature.": return "Запоминающаяся комната с особой деталью данжа.";
                case "Dust, old marks, and room for player-driven exploration.": return "Пыль, старые следы и пространство для исследования игроками.";
                default: return value;
            }
        }

        private string LocalizeDungeonMonster(string value)
        {
            switch (value)
            {
                case "goblins": return "гоблины";
                case "giant rats": return "гигантские крысы";
                case "skeletons": return "скелеты";
                case "zombies": return "зомби";
                case "kobolds": return "кобольды";
                case "orcs": return "орки";
                case "hobgoblins": return "хобгоблины";
                case "giant ants": return "гигантские муравьи";
                case "giant wasps": return "гигантские осы";
                case "giant spiders": return "гигантские пауки";
                case "ghouls": return "упыри";
                case "ogre": return "огр";
                case "ogres": return "огры";
                case "troll": return "тролль";
                case "mummy": return "мумия";
                case "dragon": return "дракон";
                case "elemental": return "элементаль";
                case "demon": return "демон";
                case "vampire": return "вампир";
                default: return value;
            }
        }

        private string LocalizeDungeonTreasure(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf(" gp in ", StringComparison.OrdinalIgnoreCase) < 0) return value;
            string text = value.Replace(" gp in ", " зм в ");
            text = text.Replace("coins", "монетах");
            text = text.Replace("gems", "самоцветах");
            text = text.Replace("jewelry", "украшениях");
            text = text.Replace("art object", "предметах искусства");
            text = text.Replace("minor magic item", "малых магических предметах");
            text = text.Replace("trade goods", "товарах");
            return text;
        }

        private string LocalizeDungeonTrap(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            string text = value.Replace("(level ", "(уровень ");
            text = text.Replace("pit trap", "яма-ловушка");
            text = text.Replace("falling stones", "падающие камни");
            text = text.Replace("poison needle", "отравленная игла");
            text = text.Replace("spear launcher", "копьемет");
            text = text.Replace("collapsing floor", "обрушивающийся пол");
            text = text.Replace("alarm glyph", "охранный знак");
            text = text.Replace("scything blade", "серповидное лезвие");
            return text;
        }

        private void SelectDungeonItem(ComboBox combo, string value)
        {
            if (combo == null || string.IsNullOrWhiteSpace(value)) return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                DungeonTypeItem item = combo.Items[i] as DungeonTypeItem;
                if (item != null && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private sealed class DungeonTypeItem
        {
            public string Value { get; private set; }
            public string Label { get; private set; }

            public DungeonTypeItem(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public override string ToString()
            {
                return Label;
            }
        }

        private sealed class DungeonLevelItem
        {
            public int LevelNumber { get; private set; }
            public string Label { get; private set; }

            public DungeonLevelItem(int levelNumber, string label)
            {
                LevelNumber = levelNumber;
                Label = label;
            }

            public override string ToString()
            {
                return Label;
            }
        }

        private sealed class DungeonRenderLayout
        {
            public float OffsetX { get; private set; }
            public float OffsetY { get; private set; }
            public float Scale { get; private set; }
            public float BaseOffsetX { get; private set; }
            public float BaseOffsetY { get; private set; }
            public float BaseScale { get; private set; }

            public DungeonRenderLayout(float offsetX, float offsetY, float scale, float baseOffsetX, float baseOffsetY, float baseScale)
            {
                OffsetX = offsetX;
                OffsetY = offsetY;
                Scale = scale;
                BaseOffsetX = baseOffsetX;
                BaseOffsetY = baseOffsetY;
                BaseScale = baseScale;
            }
        }
    }
}
