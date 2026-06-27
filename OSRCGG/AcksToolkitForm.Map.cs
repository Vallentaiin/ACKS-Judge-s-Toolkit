using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private const float MapHexSize = 24f;
        private static readonly float MapHexWidth = (float)(Math.Sqrt(3.0) * MapHexSize);
        private static readonly float MapHexHorizontalRadius = MapHexWidth / 2f;
        private static readonly float MapHexRowHeight = MapHexSize * 1.5f;
        private static readonly PointF[] MapHexPointOffsets = BuildMapHexPointOffsets();
        private const int MapScaleMiles = 6;
        private const float MinPngExportZoom = 2f;
        private const int MaxPngExportDimension = 16000;
        private const long MaxPngExportPixels = 90000000L;
        private const string NewGeneratedSettlementOptionId = "__new_generated_map_settlement__";

        private readonly Random mapRandom = new Random();
        private readonly string settlementLibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OSRCGG",
            "settlements.xml");
        private readonly string mapLibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OSRCGG",
            "maps.xml");

        private readonly string[] mapTerrainKeys =
        {
            "Rainforest", "Savanna", "Desert", "Steppe", "Scrub", "Grasslands",
            "Forest", "Taiga", "Tundra", "Marsh", "DeepForest", "DeepTaiga"
        };

        private readonly string[] mapElevationKeys = { "Plains", "Hills", "Mountains" };
        private readonly string[] mapWaterKeys = { "None", "Ocean", "Sea", "Lake" };

        private readonly Dictionary<string, Image> mapImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> scaledMapImages = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Rectangle> mapImageContentBounds = new Dictionary<string, Rectangle>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Button> mapToolButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Button> mapLayerButtons = new Dictionary<int, Button>();
        private readonly Dictionary<string, Button> mapTerrainButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> mapElevationButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> mapWaterButtons = new Dictionary<string, Button>();
        private bool mapPickerButtonsBuilt;
        private List<MapSettlementRecord> settlementLibrary = new List<MapSettlementRecord>();
        private List<HexMapRecord> mapLibrary = new List<HexMapRecord>();
        private HexMapRecord currentMap;
        private Dictionary<string, HexCellRecord> currentMapCellIndex = new Dictionary<string, HexCellRecord>();
        private Dictionary<string, List<MapSettlementRecord>> currentMapSettlementsByCell = new Dictionary<string, List<MapSettlementRecord>>();
        private Dictionary<string, MapSettlementRecord> currentMapSettlementsById = new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<DomainRecord>> currentMapStrongholdsByCell = new Dictionary<string, List<DomainRecord>>();
        private Dictionary<string, DomainRecord> currentMapDomainBySettlementId = new Dictionary<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DomainRecord> currentMapDomainByHex = new Dictionary<string, DomainRecord>();
        private Dictionary<string, List<MapEdgeRecord>> currentMapRoadsByCell = new Dictionary<string, List<MapEdgeRecord>>();
        private Dictionary<string, List<MapEdgeRecord>> currentMapRiversByCell = new Dictionary<string, List<MapEdgeRecord>>();
        private Dictionary<string, List<HexFeatureRecord>> currentMapFeaturesByCell = new Dictionary<string, List<HexFeatureRecord>>();
        private Dictionary<string, DungeonRecord> currentMapDungeonsById = new Dictionary<string, DungeonRecord>(StringComparer.OrdinalIgnoreCase);
        private List<MapFeatureLabelRecord> currentMapWaterFeatureLabels = new List<MapFeatureLabelRecord>();
        private List<MapFeatureLabelRecord> currentMapRiverFeatureLabels = new List<MapFeatureLabelRecord>();
        private List<HexCellRecord> currentMapDrawOrder = new List<HexCellRecord>();
        private List<HexCellRecord> currentPaintVisibleCells;
        private RectangleF currentPaintVisibleWorld = RectangleF.Empty;
        private RectangleF currentMapWorldBounds = RectangleF.Empty;
        private int currentMapMinQ;
        private int currentMapMaxQ;
        private int currentMapMinR;
        private int currentMapMaxR;
        private HexCellRecord selectedMapCell;
        private HexCellRecord pendingMapEdgeStart;
        private bool mapEventsWired;
        private bool mapUiLoading;
        private bool mapDemandGridLoading;
        private bool mapPanning;
        private float mapZoom = 1f;
        private Point mapPanMouseStart;
        private Point mapPanScrollStart;
        private Rectangle buttonBorderSourceRect = Rectangle.Empty;
        private Button btnMapGenerateRegion;
        private Button btnMapRegenerateCivilization;
        private Button btnMapCancelRegionGeneration;
        private ProgressBar prgMapRegionGeneration;
        private CancellationTokenSource mapRegionGenerationCts;
        private Label lblMapEraseMode;
        private ComboBox cmbMapEraseMode;
        private CheckBox chkMapShowRoads;
        private CheckBox chkMapShowRivers;
        private CheckBox chkMapShowSettlements;
        private CheckBox chkMapShowStrongholds;
        private CheckBox chkMapUseSmallMapIcons;
        private CheckBox chkMapShowHexCoordinates;
        private CheckBox chkMapShowLargeHexGrid;
        private CheckBox chkMapShowHexFeatures;
        private CheckBox chkMapEraseAll;
        private CheckBox chkMapEraseRoads;
        private CheckBox chkMapEraseRivers;
        private CheckBox chkMapEraseSettlements;
        private CheckBox chkMapEraseStrongholds;
        private CheckBox chkMapEraseDomains;
        private CheckBox chkMapEraseTerrain;
        private CheckBox chkMapEraseFeatures;
        private bool updatingMapEraseChecks;
        private NameGenerationService regionNameService;
        private Button btnMapExportPng;
        private Button btnMapGenerateFeatureNames;
        private CheckBox chkMapShowFeatureLabels;
        private CheckBox chkMapShowRealmLabels;
        private CheckBox chkMapShowSettlementLabels;
        private CheckBox chkMapShowStrongholdLabels;
        private CheckBox chkMapEraseNames;
        private ToolTip mapHoverToolTip;
        private MapLabelHoverTarget hoveredMapLabelTarget;
        private PrivateFontCollection mapFontCollection;
        private bool mapFontsLoaded;
        private Label lblSettlementLibraryFilters;
        private TextBox txtSettlementLibrarySearch;
        private ComboBox cmbSettlementLibraryClassFilter;
        private ComboBox cmbSettlementLibraryRaceFilter;

        private sealed class MapLabelHoverTarget
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Q { get; set; }
            public int R { get; set; }
            public float LabelOffsetX { get; set; }
            public float LabelOffsetY { get; set; }
            public bool SuppressNeighbors { get; set; }
        }

        private sealed class MapFeatureLabelRecord
        {
            public string Text { get; set; }
            public string DisplayText { get; set; }
            public string Kind { get; set; }
            public PointF Center { get; set; }
            public float FontSize { get; set; }
            public float MinWidth { get; set; }
            public float HorizontalPadding { get; set; }
            public float Height { get; set; }
            public float OutlineWidth { get; set; }
        }

        private sealed class MapPickerSpec
        {
            public string Key { get; set; }
            public string English { get; set; }
            public string Russian { get; set; }
            public string IconKey { get; set; }
            public string SecondaryIconKey { get; set; }
            public Color BackColor { get; set; }
            public int ImageSize { get; set; }
            public string DisplayText { get; set; }
            public bool Selected { get; set; }
        }

        private static PointF[] BuildMapHexPointOffsets()
        {
            PointF[] points = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 180d * (30 + 60 * i);
                points[i] = new PointF(
                    MapHexSize * (float)Math.Cos(angle),
                    MapHexSize * (float)Math.Sin(angle));
            }

            return points;
        }

        private void InitializeMapTab()
        {
            LoadMapImages();
            ConfigureMapDesignerControls();
            LoadSettlementLibrary();
            LoadMapLibrary();
            RefreshSettlementLibraryUi();
            RefreshMapLibraryUi();

            if (currentMap == null)
            {
                CreateNewMap(false);
            }

            UpdateMapLanguage();
        }

        private void ConfigureMapDesignerControls()
        {
            if (tabPageMap == null) return;

            ConfigureMapSizeControls();
            PopulateMapCombo(cmbMapTool, new[] { "Select", "Settlement", "Road", "River", "Terrain", "Elevation", "Water", "Erase", "Stronghold", "Feature" });
            PopulateMapCombo(cmbMapLayer, new[] { "Demands", "Terrain", "Elevation", "Landscape" });
            PopulateMapCombo(cmbMapTerrain, mapTerrainKeys);
            PopulateMapCombo(cmbMapElevation, mapElevationKeys);
            PopulateMapCombo(cmbMapWater, mapWaterKeys);
            PopulateMapCombo(cmbMapSettlementClass, new[] { "I", "II", "III", "IV", "V", "VI" });
            PopulateMapCombo(cmbGeneratedSettlementClass, new[] { "I", "II", "III", "IV", "V", "VI" });
            EnsureSettlementLibraryFilterControls();

            if (cmbMapTool.SelectedIndex < 0) cmbMapTool.SelectedIndex = 0;
            if (cmbMapLayer.SelectedIndex < 0) cmbMapLayer.SelectedIndex = 0;
            if (cmbMapTerrain.SelectedIndex < 0) cmbMapTerrain.SelectedIndex = 5;
            if (cmbMapElevation.SelectedIndex < 0) cmbMapElevation.SelectedIndex = 0;
            if (cmbMapWater.SelectedIndex < 0) cmbMapWater.SelectedIndex = 0;
            if (cmbMapSettlementClass.SelectedIndex < 0) cmbMapSettlementClass.SelectedIndex = 5;
            if (cmbGeneratedSettlementClass.SelectedIndex < 0) cmbGeneratedSettlementClass.SelectedIndex = 5;

            EnsureMapRegionGenerationControls();
            pnlHexMap.AutoScroll = true;
            pnlHexMap.TabStop = true;
            chkMapShowIcons.Checked = true;
            chkMapShowSettlementIcons.Checked = true;
            EnsureMapDisplayAndEraseControls();
            EnsureMapExportAndNameControls();
            LayoutMapToolsForWidePanel();
            ConfigureMapDemandGrid();
            BuildMapPickerButtons();
            EnsureMapDomainControls();
            LayoutMapToolsForWidePanel();
            UpdateMapPickerButtons();

            if (mapEventsWired) return;

            pnlHexMap.Paint += pnlHexMap_Paint;
            pnlHexMap.MouseClick += pnlHexMap_MouseClick;
            pnlHexMap.MouseDoubleClick += pnlHexMap_MouseDoubleClick;
            pnlHexMap.MouseDown += pnlHexMap_MouseDown;
            pnlHexMap.MouseMove += pnlHexMap_MouseMove;
            pnlHexMap.MouseUp += pnlHexMap_MouseUp;
            pnlHexMap.SuppressMouseWheelAutoScroll = true;
            pnlHexMap.MouseWheelWithoutAutoScroll += pnlHexMap_MouseWheel;
            pnlHexMap.MouseEnter += (s, e) => pnlHexMap.Focus();
            pnlHexMap.MouseLeave += (s, e) => ClearHoveredMapLabel();
            pnlHexMap.Resize += (s, e) =>
            {
                UpdateMapScrollSize();
                pnlHexMap.Invalidate();
            };
            pnlMapTools.Resize += (s, e) => LayoutMapToolsForWidePanel();
            pnlHexMap.Scroll += (s, e) => pnlHexMap.Invalidate();
            cmbMapLayer.SelectedIndexChanged += (s, e) => pnlHexMap.Invalidate();
            cmbMapTerrain.SelectedIndexChanged += (s, e) => UpdateMapPickerButtons();
            cmbMapElevation.SelectedIndexChanged += (s, e) => UpdateMapPickerButtons();
            cmbMapWater.SelectedIndexChanged += (s, e) => UpdateMapPickerButtons();
            chkMapShowIcons.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
            chkMapShowSettlementIcons.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
            cmbMapTool.SelectedIndexChanged += (s, e) =>
            {
                pendingMapEdgeStart = null;
                UpdateMapPickerButtons();
                UpdateMapInfoForSelection();
            };
            cmbMapLayer.SelectedIndexChanged += (s, e) => UpdateMapPickerButtons();

            btnMapNew.Click += (s, e) => CreateNewMap(true);
            btnMapGenerateRegion.Click += (s, e) => GenerateRegionFromDialog();
            if (btnMapRegenerateCivilization != null) btnMapRegenerateCivilization.Click += (s, e) => RegenerateCivilizationFromDialog();
            btnMapSave.Click += (s, e) => SaveCurrentMapToLibrary();
            btnMapDelete.Click += (s, e) => DeleteSelectedMap();
            btnMapCalculateTrade.Click += (s, e) => CalculateMapTradeRoutes();
            btnMapExportExcel.Click += (s, e) => ExportCurrentMapToExcel();
            btnMapImportExcel.Click += (s, e) => ImportMapFromExcel();
            if (btnMapExportPng != null) btnMapExportPng.Click += (s, e) => ExportCurrentMapToPng();
            if (btnMapGenerateFeatureNames != null) btnMapGenerateFeatureNames.Click += (s, e) => GenerateNamesForCurrentMap();
            btnMapSaveSettlement.Click += (s, e) => SaveCurrentGeneratorSettlement();
            btnMapCreateEmptySettlement.Click += (s, e) => CreateEmptySettlement();
            btnMapApplyDemands.Click += (s, e) => ApplyDemandGridEdits();
            btnGeneratedSettlementSave.Click += (s, e) => SaveCurrentGeneratorSettlement();
            btnMapExportSettlement.Click += (s, e) => ExportSelectedSettlementToExcel();
            btnMapImportSettlement.Click += (s, e) => ImportSettlementFromExcel();
            btnGeneratorLoadSettlement.Click += (s, e) => LoadSelectedSettlementIntoGenerator();
            btnGeneratorDeleteSettlement.Click += (s, e) => DeleteSelectedGeneratorSettlement();
            lstGeneratorSettlements.DoubleClick += (s, e) => LoadSelectedSettlementIntoGenerator();
            btnTradeUseMarketASettlement.Click += (s, e) => ApplySelectedSettlementToTrade(true);
            btnTradeUsePartnerSettlement.Click += (s, e) => ApplySelectedSettlementToTrade(false);
            cmbTradeMarketASettlement.SelectedIndexChanged += (s, e) => UpdateTradeClassFromSelectedSettlement(true);
            cmbTradePartnerSettlement.SelectedIndexChanged += (s, e) => UpdateTradeClassFromSelectedSettlement(false);
            cmbMapSettlementLibrary.SelectedIndexChanged += (s, e) =>
            {
                if (!mapDemandGridLoading && GetSelectedMapSettlement() == null)
                {
                    LoadDemandGridForCurrentTarget();
                }
            };

            lstMaps.SelectedIndexChanged += (s, e) =>
            {
                if (!mapUiLoading && lstMaps.SelectedItem is HexMapRecord map)
                {
                    LoadMapToEditor(map);
                }
            };

            mapEventsWired = true;
        }

        private void ConfigureMapSizeControls()
        {
            // Боковая панель должна принимать тот же максимум, что и диалог генерации,
            // иначе LoadMapToEditor обрезает сгенерированные 80x80 карты до designer-лимита.
            if (nudMapWidth != null) nudMapWidth.Maximum = RegionGenerationOptions.MaxMapSize;
            if (nudMapHeight != null) nudMapHeight.Maximum = RegionGenerationOptions.MaxMapSize;
        }

        private void EnsureMapDisplayAndEraseControls()
        {
            if (pnlMapTools == null) return;

            if (lblMapEraseMode == null)
            {
                lblMapEraseMode = new Label
                {
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Text = isEnglish ? "Erase mode" : "Что стирать"
                };
                pnlMapTools.Controls.Add(lblMapEraseMode);
            }

            if (cmbMapEraseMode == null)
            {
                cmbMapEraseMode = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                pnlMapTools.Controls.Add(cmbMapEraseMode);
            }

            if (cmbMapEraseMode.Items.Count == 0)
            {
                cmbMapEraseMode.Items.AddRange(new object[] { "All", "Roads", "Rivers", "Settlements", "Strongholds", "Domains", "Terrain/water", "Hex features" });
                cmbMapEraseMode.SelectedIndex = 0;
            }
            cmbMapEraseMode.Visible = false;

            EnsureMapEraseCheckBoxes();

            if (chkMapShowRoads == null)
            {
                chkMapShowRoads = CreateMapDisplayCheckBox(isEnglish ? "Roads" : "Дороги");
                chkMapShowRoads.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowRoads);
            }

            if (chkMapShowRivers == null)
            {
                chkMapShowRivers = CreateMapDisplayCheckBox(isEnglish ? "Rivers" : "Реки");
                chkMapShowRivers.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowRivers);
            }

            if (chkMapShowSettlements == null)
            {
                chkMapShowSettlements = CreateMapDisplayCheckBox(isEnglish ? "Settlements" : "Поселения");
                chkMapShowSettlements.CheckedChanged += (s, e) =>
                {
                    if (chkMapShowSettlementIcons != null) chkMapShowSettlementIcons.Enabled = chkMapShowSettlements.Checked;
                    pnlHexMap.Invalidate();
                };
                pnlMapTools.Controls.Add(chkMapShowSettlements);
            }

            if (chkMapShowStrongholds == null)
            {
                chkMapShowStrongholds = CreateMapDisplayCheckBox(isEnglish ? "Strongholds" : "Крепости");
                chkMapShowStrongholds.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowStrongholds);
            }

            if (chkMapUseSmallMapIcons == null)
            {
                chkMapUseSmallMapIcons = CreateMapDisplayCheckBox(isEnglish ? "Small icons" : "Малые значки");
                chkMapUseSmallMapIcons.Checked = false;
                chkMapUseSmallMapIcons.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapUseSmallMapIcons);
            }

            if (chkMapShowHexCoordinates == null)
            {
                chkMapShowHexCoordinates = CreateMapDisplayCheckBox(isEnglish ? "Hex coordinates" : "Координаты гексов");
                chkMapShowHexCoordinates.Checked = false;
                chkMapShowHexCoordinates.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowHexCoordinates);
            }

            if (chkMapShowLargeHexGrid == null)
            {
                chkMapShowLargeHexGrid = CreateMapDisplayCheckBox(isEnglish ? "24-mile grid" : "Сетка 24 мили");
                chkMapShowLargeHexGrid.Checked = false;
                chkMapShowLargeHexGrid.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowLargeHexGrid);
            }

            if (chkMapShowHexFeatures == null)
            {
                chkMapShowHexFeatures = CreateMapDisplayCheckBox(isEnglish ? "Hex features" : "Особенности гексов");
                chkMapShowHexFeatures.Checked = true;
                chkMapShowHexFeatures.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowHexFeatures);
            }

            if (chkMapShowFeatureLabels == null)
            {
                chkMapShowFeatureLabels = CreateMapDisplayCheckBox(isEnglish ? "Feature names" : "Названия объектов");
                chkMapShowFeatureLabels.Checked = true;
                chkMapShowFeatureLabels.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowFeatureLabels);
            }

            if (chkMapShowRealmLabels == null)
            {
                chkMapShowRealmLabels = CreateMapDisplayCheckBox(isEnglish ? "Realm names" : "Названия держав");
                chkMapShowRealmLabels.Checked = true;
                chkMapShowRealmLabels.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowRealmLabels);
            }

            if (chkMapShowSettlementLabels == null)
            {
                chkMapShowSettlementLabels = CreateMapDisplayCheckBox(isEnglish ? "Settlement names" : "Названия поселений");
                chkMapShowSettlementLabels.Checked = true;
                chkMapShowSettlementLabels.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowSettlementLabels);
            }

            if (chkMapShowStrongholdLabels == null)
            {
                chkMapShowStrongholdLabels = CreateMapDisplayCheckBox(isEnglish ? "Stronghold names" : "Названия крепостей");
                chkMapShowStrongholdLabels.Checked = true;
                chkMapShowStrongholdLabels.CheckedChanged += (s, e) => pnlHexMap.Invalidate();
                pnlMapTools.Controls.Add(chkMapShowStrongholdLabels);
            }
        }

        private void EnsureMapEraseCheckBoxes()
        {
            chkMapEraseAll = EnsureMapEraseCheckBox(chkMapEraseAll, isEnglish ? "All" : "Всё");
            chkMapEraseRoads = EnsureMapEraseCheckBox(chkMapEraseRoads, isEnglish ? "Roads" : "Дороги");
            chkMapEraseRivers = EnsureMapEraseCheckBox(chkMapEraseRivers, isEnglish ? "Rivers" : "Реки");
            chkMapEraseSettlements = EnsureMapEraseCheckBox(chkMapEraseSettlements, isEnglish ? "Settlements" : "Поселения");
            chkMapEraseStrongholds = EnsureMapEraseCheckBox(chkMapEraseStrongholds, isEnglish ? "Strongholds" : "Крепости");
            chkMapEraseDomains = EnsureMapEraseCheckBox(chkMapEraseDomains, isEnglish ? "Domains" : "Домены");
            chkMapEraseTerrain = EnsureMapEraseCheckBox(chkMapEraseTerrain, isEnglish ? "Terrain/water" : "Местность/вода");
            chkMapEraseFeatures = EnsureMapEraseCheckBox(chkMapEraseFeatures, isEnglish ? "Hex features" : "Особенности");
            chkMapEraseNames = EnsureMapEraseCheckBox(chkMapEraseNames, isEnglish ? "Names" : "Названия");

            if (!chkMapEraseAll.Checked)
            {
                updatingMapEraseChecks = true;
                try
                {
                    chkMapEraseAll.Checked = true;
                    chkMapEraseRoads.Checked = true;
                    chkMapEraseRivers.Checked = true;
                    chkMapEraseSettlements.Checked = true;
                    chkMapEraseStrongholds.Checked = true;
                    chkMapEraseDomains.Checked = true;
                    chkMapEraseTerrain.Checked = true;
                    chkMapEraseFeatures.Checked = true;
                    chkMapEraseNames.Checked = true;
                }
                finally
                {
                    updatingMapEraseChecks = false;
                }
            }
        }

        private CheckBox EnsureMapEraseCheckBox(CheckBox checkBox, string text)
        {
            if (checkBox == null)
            {
                checkBox = CreateMapDisplayCheckBox(text);
                checkBox.CheckedChanged += MapEraseCheckBox_CheckedChanged;
                pnlMapTools.Controls.Add(checkBox);
            }

            checkBox.Text = text;
            return checkBox;
        }

        private void MapEraseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingMapEraseChecks) return;

            CheckBox changed = sender as CheckBox;
            updatingMapEraseChecks = true;
            try
            {
                if (changed == chkMapEraseAll)
                {
                    bool checkedAll = chkMapEraseAll.Checked;
                    chkMapEraseRoads.Checked = checkedAll;
                    chkMapEraseRivers.Checked = checkedAll;
                    chkMapEraseSettlements.Checked = checkedAll;
                    chkMapEraseStrongholds.Checked = checkedAll;
                    chkMapEraseDomains.Checked = checkedAll;
                    chkMapEraseTerrain.Checked = checkedAll;
                    chkMapEraseFeatures.Checked = checkedAll;
                    chkMapEraseNames.Checked = checkedAll;
                }
                else if (changed != chkMapEraseAll)
                {
                    bool allSpecific = chkMapEraseRoads.Checked
                        && chkMapEraseRivers.Checked
                        && chkMapEraseSettlements.Checked
                        && chkMapEraseStrongholds.Checked
                        && chkMapEraseDomains.Checked
                        && chkMapEraseTerrain.Checked
                        && chkMapEraseFeatures.Checked
                        && chkMapEraseNames.Checked;
                    chkMapEraseAll.Checked = allSpecific;
                }
            }
            finally
            {
                updatingMapEraseChecks = false;
            }
        }

        private CheckBox CreateMapDisplayCheckBox(string text)
        {
            return new CheckBox
            {
                Text = text,
                Checked = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        private void PopulateMapCombo(ComboBox combo, string[] values)
        {
            if (combo == null) return;
            object selected = combo.SelectedItem;
            combo.Items.Clear();
            combo.Items.AddRange(values.Cast<object>().ToArray());
            if (selected != null && combo.Items.Contains(selected))
            {
                combo.SelectedItem = selected;
            }
        }

        private void EnsureMapRegionGenerationControls()
        {
            if (btnMapGenerateRegion != null || pnlMapTools == null) return;

            btnMapGenerateRegion = new Button
            {
                Text = isEnglish ? "Generate map" : "\u0421\u0433\u0435\u043d\u0435\u0440\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u043a\u0430\u0440\u0442\u0443"
            };
            UiTheme.StylePositiveButton(btnMapGenerateRegion);
            pnlMapTools.Controls.Add(btnMapGenerateRegion);

            btnMapRegenerateCivilization = new Button
            {
                Text = isEnglish ? "Regenerate layers" : "Перегенерировать слои"
            };
            UiTheme.StylePositiveButton(btnMapRegenerateCivilization);
            pnlMapTools.Controls.Add(btnMapRegenerateCivilization);

            btnMapCancelRegionGeneration = new Button
            {
                Text = isEnglish ? "Cancel" : "Отмена",
                Visible = false
            };
            UiTheme.StyleNegativeButton(btnMapCancelRegionGeneration);
            btnMapCancelRegionGeneration.Click += (s, e) =>
            {
                if (mapRegionGenerationCts != null)
                {
                    mapRegionGenerationCts.Cancel();
                    btnMapCancelRegionGeneration.Enabled = false;
                }
            };
            pnlMapTools.Controls.Add(btnMapCancelRegionGeneration);

            prgMapRegionGeneration = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };
            pnlMapTools.Controls.Add(prgMapRegionGeneration);
        }

        private void EnsureMapExportAndNameControls()
        {
            if (pnlMapTools == null) return;

            if (btnMapExportPng == null)
            {
                btnMapExportPng = new Button
                {
                    Text = isEnglish ? "Export PNG" : "Экспорт PNG"
                };
                UiTheme.StylePositiveButton(btnMapExportPng);
                pnlMapTools.Controls.Add(btnMapExportPng);
            }

            if (btnMapGenerateFeatureNames == null)
            {
                btnMapGenerateFeatureNames = new Button
                {
                    Text = isEnglish ? "Generate names" : "Сгенерировать названия"
                };
                UiTheme.StylePositiveButton(btnMapGenerateFeatureNames);
                pnlMapTools.Controls.Add(btnMapGenerateFeatureNames);
            }

            if (mapHoverToolTip == null)
            {
                mapHoverToolTip = new ToolTip
                {
                    InitialDelay = 250,
                    ReshowDelay = 120,
                    AutoPopDelay = 5000,
                    ShowAlways = false
                };
            }
        }

        private void LayoutMapToolsForWidePanel()
        {
            if (pnlMapTools == null) return;

            pnlMapTools.Width = 540;
            int left = 10;
            int right = 270;
            int leftWidth = 250;
            int rightWidth = 250;

            lblMapScale.SetBounds(left, 8, leftWidth, 20);
            lblMapLibrary.SetBounds(left, 34, leftWidth, 20);
            lstMaps.SetBounds(left, 54, leftWidth, 94);
            lblMapName.SetBounds(left, 154, leftWidth, 20);
            txtMapName.SetBounds(left, 174, leftWidth, 24);
            lblMapSize.SetBounds(left, 206, leftWidth, 20);
            nudMapWidth.SetBounds(left, 226, 72, 24);
            nudMapHeight.SetBounds(left + 82, 226, 72, 24);
            btnMapNew.SetBounds(left, 260, 72, 28);
            btnMapSave.SetBounds(left + 82, 260, 92, 28);
            btnMapDelete.SetBounds(left + 184, 260, 76, 28);
            if (btnMapGenerateRegion != null)
            {
                bool generationRunning = btnMapCancelRegionGeneration != null && btnMapCancelRegionGeneration.Visible;
                int generateWidth = generationRunning ? leftWidth - 88 : leftWidth;
                btnMapGenerateRegion.SetBounds(left, 294, generateWidth, 30);
                if (btnMapRegenerateCivilization != null)
                {
                    btnMapRegenerateCivilization.Visible = !generationRunning;
                    btnMapRegenerateCivilization.SetBounds(left, 328, leftWidth, 30);
                }
                if (btnMapCancelRegionGeneration != null) btnMapCancelRegionGeneration.SetBounds(left + generateWidth + 8, 294, 80, 30);
                if (prgMapRegionGeneration != null) prgMapRegionGeneration.SetBounds(left, generationRunning ? 326 : 362, leftWidth, 6);
            }
            int generationBlockShift = btnMapCancelRegionGeneration != null && btnMapCancelRegionGeneration.Visible ? 0 : 36;
            int toolPickerWidth = 152;
            int toolPickerHeight = 380;
            int postToolShift = generationBlockShift + (toolPickerHeight - 180);
            int toolPickerLeft = left + (leftWidth - toolPickerWidth) / 2;
            lblMapTool.SetBounds(toolPickerLeft, 334 + generationBlockShift, toolPickerWidth, 20);
            lblMapTool.TextAlign = ContentAlignment.MiddleCenter;
            flpMapToolButtons.SetBounds(toolPickerLeft, 354 + generationBlockShift, toolPickerWidth, toolPickerHeight);
            cmbMapTool.SetBounds(toolPickerLeft, 354 + generationBlockShift, toolPickerWidth, 24);
            cmbMapTool.Visible = false;
            if (lblMapEraseMode != null) lblMapEraseMode.SetBounds(left, 540 + postToolShift, leftWidth, 20);
            if (cmbMapEraseMode != null) cmbMapEraseMode.SetBounds(left, 560 + postToolShift, leftWidth, 24);
            if (chkMapEraseAll != null) chkMapEraseAll.SetBounds(left, 560 + postToolShift, 78, 22);
            if (chkMapEraseRoads != null) chkMapEraseRoads.SetBounds(left + 86, 560 + postToolShift, 80, 22);
            if (chkMapEraseRivers != null) chkMapEraseRivers.SetBounds(left + 170, 560 + postToolShift, 80, 22);
            if (chkMapEraseSettlements != null) chkMapEraseSettlements.SetBounds(left, 584 + postToolShift, 120, 22);
            if (chkMapEraseStrongholds != null) chkMapEraseStrongholds.SetBounds(left + 126, 584 + postToolShift, 112, 22);
            if (chkMapEraseDomains != null) chkMapEraseDomains.SetBounds(left, 608 + postToolShift, 112, 22);
            if (chkMapEraseTerrain != null) chkMapEraseTerrain.SetBounds(left + 118, 608 + postToolShift, 132, 22);
            if (chkMapEraseFeatures != null) chkMapEraseFeatures.SetBounds(left, 632 + postToolShift, 112, 22);
            if (chkMapEraseNames != null) chkMapEraseNames.SetBounds(left + 118, 632 + postToolShift, 104, 22);
            int layerPickerWidth = 152;
            int layerPickerLeft = left + (leftWidth - layerPickerWidth) / 2;
            lblMapLayer.SetBounds(layerPickerLeft, 688 + postToolShift, layerPickerWidth, 20);
            lblMapLayer.TextAlign = ContentAlignment.MiddleCenter;
            flpMapLayerButtons.SetBounds(layerPickerLeft, 708 + postToolShift, layerPickerWidth, 152);
            cmbMapLayer.SetBounds(left, 708 + postToolShift, leftWidth, 24);
            cmbMapLayer.Visible = false;
            chkMapShowIcons.SetBounds(left, 866 + postToolShift, leftWidth, 22);
            if (chkMapShowRoads != null) chkMapShowRoads.SetBounds(left, 890 + postToolShift, 90, 22);
            if (chkMapShowRivers != null) chkMapShowRivers.SetBounds(left + 96, 890 + postToolShift, 90, 22);
            if (chkMapShowSettlements != null) chkMapShowSettlements.SetBounds(left, 914 + postToolShift, 120, 22);
            if (chkMapShowStrongholds != null) chkMapShowStrongholds.SetBounds(left + 126, 914 + postToolShift, 116, 22);
            chkMapShowSettlementIcons.SetBounds(left, 938 + postToolShift, 132, 22);
            if (chkMapUseSmallMapIcons != null) chkMapUseSmallMapIcons.SetBounds(left + 138, 938 + postToolShift, 112, 22);
            if (chkMapShowFeatureLabels != null) chkMapShowFeatureLabels.SetBounds(left, 962 + postToolShift, 134, 22);
            if (chkMapShowRealmLabels != null) chkMapShowRealmLabels.SetBounds(left + 140, 962 + postToolShift, 122, 22);
            if (chkMapShowSettlementLabels != null) chkMapShowSettlementLabels.SetBounds(left, 986 + postToolShift, leftWidth, 22);
            if (chkMapShowStrongholdLabels != null) chkMapShowStrongholdLabels.SetBounds(left, 1010 + postToolShift, leftWidth, 22);
            if (chkMapShowHexFeatures != null) chkMapShowHexFeatures.SetBounds(left, 1034 + postToolShift, leftWidth, 22);
            if (chkMapShowHexCoordinates != null) chkMapShowHexCoordinates.SetBounds(left, 1058 + postToolShift, leftWidth, 22);
            if (chkMapShowLargeHexGrid != null) chkMapShowLargeHexGrid.SetBounds(left, 1082 + postToolShift, leftWidth, 22);
            int domainControlsTop = 1182 + postToolShift;

            lblMapSettlement.SetBounds(right, 8, rightWidth, 20);
            cmbMapSettlementLibrary.SetBounds(right, 28, rightWidth, 24);
            cmbMapSettlementClass.SetBounds(right, 60, 58, 24);
            btnMapSaveSettlement.SetBounds(right + 68, 58, 182, 28);
            btnMapCreateEmptySettlement.SetBounds(right, 92, rightWidth, 28);
            btnMapExportSettlement.SetBounds(right, 126, 120, 28);
            btnMapImportSettlement.SetBounds(right + 130, 126, 120, 28);
            lblMapDemands.SetBounds(right, 156, rightWidth, 20);
            dgvMapDemands.SetBounds(right, 176, rightWidth, 92);
            btnMapApplyDemands.SetBounds(right, 274, rightWidth, 28);
            lblMapTerrain.SetBounds(right, 306, rightWidth, 20);
            flpMapTerrainButtons.SetBounds(right, 326, rightWidth, 180);
            cmbMapTerrain.SetBounds(right, 326, rightWidth, 24);
            cmbMapTerrain.Visible = false;
            lblMapElevation.SetBounds(right, 514, rightWidth, 20);
            flpMapElevationButtons.SetBounds(right, 534, rightWidth, 56);
            cmbMapElevation.SetBounds(right, 534, rightWidth, 24);
            cmbMapElevation.Visible = false;
            lblMapWater.SetBounds(right, 596, rightWidth, 20);
            flpMapWaterButtons.SetBounds(right, 616, rightWidth, 56);
            cmbMapWater.SetBounds(right, 616, rightWidth, 24);
            cmbMapWater.Visible = false;
            btnMapCalculateTrade.SetBounds(right, 680, rightWidth, 32);
            btnMapExportExcel.SetBounds(right, 716, 120, 28);
            btnMapImportExcel.SetBounds(right + 130, 716, 120, 28);
            if (btnMapExportPng != null) btnMapExportPng.SetBounds(right, 750, 120, 28);
            if (btnMapGenerateFeatureNames != null) btnMapGenerateFeatureNames.SetBounds(right + 130, 750, 120, 28);
            int mapInfoTop = 786;
            int mapInfoHeight = Math.Max(180, domainControlsTop - mapInfoTop - 12);
            lblMapInfo.SetBounds(right, mapInfoTop, rightWidth, mapInfoHeight);
            LayoutMapDomainControls(left, right, leftWidth, rightWidth, domainControlsTop);
        }

        private void RestoreMapPickerButtonStyle()
        {
            // Кнопки инструментов карты имеют собственную пиксельную сетку: иконка,
            // подпись и рамка рисуются вручную, поэтому общая тема не должна менять
            // их шрифт, размер или поведение FlowLayoutPanel.
            ConfigurePickerPanel(flpMapToolButtons);
            ConfigurePickerPanel(flpMapLayerButtons);
            ConfigurePickerPanel(flpMapTerrainButtons);
            ConfigurePickerPanel(flpMapElevationButtons);
            ConfigurePickerPanel(flpMapWaterButtons);

            RestorePickerButtons(mapToolButtons.Values, new Size(72, 72));
            RestorePickerButtons(mapLayerButtons.Values, new Size(72, 72));
            RestorePickerButtons(mapTerrainButtons.Values, new Size(56, 56));
            RestorePickerButtons(mapElevationButtons.Values, new Size(56, 56));
            RestorePickerButtons(mapWaterButtons.Values, new Size(56, 56));
        }

        private void RestorePickerButtons(IEnumerable<Button> buttons, Size size)
        {
            if (buttons == null) return;

            foreach (Button button in buttons)
            {
                if (button == null) continue;

                MapPickerSpec spec = button.Tag as MapPickerSpec;
                button.Size = size;
                button.Margin = new Padding(2);
                button.Padding = new Padding(1);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Font = new Font("Microsoft Sans Serif", 6f, FontStyle.Bold);
                button.UseVisualStyleBackColor = false;
                if (spec != null)
                {
                    button.BackColor = spec.BackColor;
                    button.ForeColor = GetReadableTextColor(spec.BackColor);
                }
                button.Invalidate();
            }
        }

        private void BuildMapPickerButtons()
        {
            if (mapPickerButtonsBuilt) return;
            mapPickerButtonsBuilt = true;

            ConfigurePickerPanel(flpMapToolButtons);
            ConfigurePickerPanel(flpMapLayerButtons);
            ConfigurePickerPanel(flpMapTerrainButtons);
            ConfigurePickerPanel(flpMapElevationButtons);
            ConfigurePickerPanel(flpMapWaterButtons);

            Color pickerNeutral = UiTheme.Accent2Color;
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 0, "Select", "Выбор", "selecticon", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 0));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 1, "City", "Город", "class3", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 1));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 2, "Road", "Дорога", "road", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 2));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 3, "River", "Река", "rivertool", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 3));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 4, "Terrain", "Местн.", "forest", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 4));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 5, "Elevation", "Высота", "mountains", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 5));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 6, "Water", "Вода", "bigwater", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 6));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 7, "Erase", "Стереть", "deleteicon", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 7));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 8, "Fort", "Креп.", "fortress", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 8));
            AddMapPickerButton(flpMapToolButtons, mapToolButtons, 9, "Feature", "Особ.", "featuretool", pickerNeutral, new Size(72, 72), 42, (s, e) => SelectMapComboIndex(cmbMapTool, 9));

            AddMapPickerButton(flpMapLayerButtons, mapLayerButtons, 0, "Demands", "Demands", "goods", pickerNeutral, new Size(72, 72), 30, (s, e) => SelectMapComboIndex(cmbMapLayer, 0));
            AddMapPickerButton(flpMapLayerButtons, mapLayerButtons, 1, "Terrain", "Местн.", "forest", pickerNeutral, new Size(72, 72), 30, (s, e) => SelectMapComboIndex(cmbMapLayer, 1));
            AddMapPickerButton(flpMapLayerButtons, mapLayerButtons, 2, "Elevation", "Высота", "mountains", pickerNeutral, new Size(72, 72), 30, (s, e) => SelectMapComboIndex(cmbMapLayer, 2));
            AddMapPickerButton(flpMapLayerButtons, mapLayerButtons, 3, "Landscape", "Ландшафт", "forest", pickerNeutral, new Size(72, 72), 26, (s, e) => SelectMapComboIndex(cmbMapLayer, 3), "mountains");

            for (int i = 0; i < mapTerrainKeys.Length; i++)
            {
                string key = mapTerrainKeys[i];
                int terrainIndex = i;
                AddMapPickerButton(flpMapTerrainButtons, mapTerrainButtons, key, GetTerrainEnglishLabel(key), GetTerrainRussianLabel(key), GetTerrainIconKey(key), TerrainColor(key), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapTerrain, terrainIndex));
            }

            AddMapPickerButton(flpMapElevationButtons, mapElevationButtons, "Plains", "Plains", "Равн.", null, ElevationColor("Plains"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapElevation, 0));
            AddMapPickerButton(flpMapElevationButtons, mapElevationButtons, "Hills", "Hills", "Холмы", "hills", ElevationColor("Hills"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapElevation, 1));
            AddMapPickerButton(flpMapElevationButtons, mapElevationButtons, "Mountains", "Mountains", "Горы", "mountains", ElevationColor("Mountains"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapElevation, 2));

            AddMapPickerButton(flpMapWaterButtons, mapWaterButtons, "None", "None", "Нет", "notselected", pickerNeutral, new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapWater, 0));
            AddMapPickerButton(flpMapWaterButtons, mapWaterButtons, "Ocean", "Ocean", "Океан", "bigwater", WaterColor("Ocean"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapWater, 1));
            AddMapPickerButton(flpMapWaterButtons, mapWaterButtons, "Sea", "Sea", "Море", "bigwater", WaterColor("Sea"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapWater, 2));
            AddMapPickerButton(flpMapWaterButtons, mapWaterButtons, "Lake", "Lake", "Озеро", "lake", WaterColor("Lake"), new Size(56, 56), 24, (s, e) => SelectMapComboIndex(cmbMapWater, 3));
        }

        private void ConfigurePickerPanel(FlowLayoutPanel panel)
        {
            if (panel == null) return;
            panel.WrapContents = true;
            panel.AutoScroll = false;
            panel.Margin = Padding.Empty;
            panel.Padding = Padding.Empty;
        }

        private Button AddMapPickerButton<T>(
            FlowLayoutPanel panel,
            Dictionary<T, Button> buttons,
            T key,
            string english,
            string russian,
            string iconKey,
            Color backColor,
            Size size,
            int imageSize,
            EventHandler click,
            string secondaryIconKey = null)
        {
            Button button = new Button();
            button.Size = size;
            button.Margin = new Padding(2);
            button.Padding = new Padding(1);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(25, 25, 25);
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
            button.BackColor = backColor;
            button.ForeColor = GetReadableTextColor(backColor);
            button.Font = new Font("Microsoft Sans Serif", 6f, FontStyle.Bold);
            button.UseVisualStyleBackColor = false;
            button.Tag = new MapPickerSpec
            {
                Key = key == null ? "" : key.ToString(),
                English = english,
                Russian = russian,
                IconKey = iconKey,
                SecondaryIconKey = secondaryIconKey,
                BackColor = backColor,
                ImageSize = imageSize,
                DisplayText = isEnglish ? english : russian
            };
            button.Click += click;
            button.Paint += PickerButton_Paint;
            panel.Controls.Add(button);
            buttons[key] = button;
            return button;
        }

        private void PickerButton_Paint(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;
            MapPickerSpec spec = button == null ? null : button.Tag as MapPickerSpec;
            if (button == null || spec == null) return;

            e.Graphics.Clear(button.BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            int textHeight = string.IsNullOrWhiteSpace(spec.DisplayText) ? 0 : 22;
            int imageSize = Math.Min(spec.ImageSize, Math.Max(0, button.Height - textHeight - 8));
            int contentHeight = imageSize + (imageSize > 0 && textHeight > 0 ? 1 : 0) + textHeight;
            int y = Math.Max(4, (button.Height - contentHeight) / 2 + 3);

            bool hasPrimaryIcon = !string.IsNullOrWhiteSpace(spec.IconKey) && mapImages.ContainsKey(spec.IconKey);
            bool hasSecondaryIcon = !string.IsNullOrWhiteSpace(spec.SecondaryIconKey) && mapImages.ContainsKey(spec.SecondaryIconKey);
            if (hasPrimaryIcon && hasSecondaryIcon && imageSize > 0)
            {
                int pairSize = Math.Max(16, Math.Min(imageSize - 2, (button.Width - 12) / 2));
                int imageY = y + Math.Max(0, (imageSize - pairSize) / 2);
                Rectangle primaryRect = new Rectangle(button.Width / 2 - pairSize - 1, imageY, pairSize, pairSize);
                Rectangle secondaryRect = new Rectangle(button.Width / 2 + 1, imageY, pairSize, pairSize);
                ColorMatrixDrawImage(e.Graphics, mapImages[spec.IconKey], primaryRect, button.Enabled ? 1f : 0.38f);
                ColorMatrixDrawImage(e.Graphics, mapImages[spec.SecondaryIconKey], secondaryRect, button.Enabled ? 1f : 0.38f);
                y += imageSize + (textHeight > 0 ? 1 : 0);
            }
            else if (hasPrimaryIcon && imageSize > 0)
            {
                Rectangle imageRect = new Rectangle((button.Width - imageSize) / 2, y, imageSize, imageSize);
                using (Image image = new Bitmap(mapImages[spec.IconKey], new Size(imageSize, imageSize)))
                {
                    ColorMatrixDrawImage(e.Graphics, image, imageRect, button.Enabled ? 1f : 0.38f);
                }
                y += imageSize + (textHeight > 0 ? 1 : 0);
            }

            if (!string.IsNullOrWhiteSpace(spec.DisplayText))
            {
                Rectangle textRect = new Rectangle(2, y, button.Width - 4, textHeight);
                Color textColor = button.Enabled ? GetReadableTextColor(spec.BackColor) : Color.FromArgb(145, 145, 145);
                TextRenderer.DrawText(e.Graphics, spec.DisplayText, button.Font, textRect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }

            Rectangle borderRect = new Rectangle(0, 0, button.Width, button.Height);
            if (mapImages.ContainsKey("buttonborder"))
            {
                Rectangle sourceRect = GetButtonBorderSourceRect();
                ColorMatrixDrawImage(e.Graphics, mapImages["buttonborder"], sourceRect, borderRect, button.Enabled ? 1f : 0.45f);
            }
            else
            {
                using (Pen light = new Pen(ControlPaint.Light(button.BackColor, 0.45f)))
                using (Pen dark = new Pen(ControlPaint.Dark(button.BackColor, 0.45f)))
                {
                    e.Graphics.DrawLine(light, 0, button.Height - 1, 0, 0);
                    e.Graphics.DrawLine(light, 0, 0, button.Width - 1, 0);
                    e.Graphics.DrawLine(dark, button.Width - 1, 0, button.Width - 1, button.Height - 1);
                    e.Graphics.DrawLine(dark, button.Width - 1, button.Height - 1, 0, button.Height - 1);
                }
            }

            if (spec.Selected)
            {
                using (Pen selected = new Pen(Color.Gold, 2f))
                {
                    e.Graphics.DrawRectangle(selected, 1, 1, button.Width - 3, button.Height - 3);
                }
            }
        }

        private void ColorMatrixDrawImage(Graphics graphics, Image image, Rectangle rect, float opacity)
        {
            ColorMatrixDrawImage(graphics, image, new Rectangle(0, 0, image.Width, image.Height), rect, opacity);
        }

        private void ColorMatrixDrawImage(Graphics graphics, Image image, Rectangle sourceRect, Rectangle rect, float opacity)
        {
            if (opacity >= 0.99f)
            {
                graphics.DrawImage(image, rect, sourceRect, GraphicsUnit.Pixel);
                return;
            }

            using (System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                System.Drawing.Imaging.ColorMatrix matrix = new System.Drawing.Imaging.ColorMatrix();
                matrix.Matrix33 = opacity;
                attributes.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                graphics.DrawImage(image, rect, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        private Rectangle GetButtonBorderSourceRect()
        {
            if (!buttonBorderSourceRect.IsEmpty) return buttonBorderSourceRect;
            if (!mapImages.ContainsKey("buttonborder")) return Rectangle.Empty;

            Image border = mapImages["buttonborder"];
            buttonBorderSourceRect = new Rectangle(0, 0, border.Width, border.Height);

            return buttonBorderSourceRect;
        }

        private void SelectMapComboIndex(ComboBox combo, int index)
        {
            if (combo == null || index < 0 || index >= combo.Items.Count) return;

            combo.SelectedIndex = index;
            UpdateMapPickerButtons();
            pnlHexMap.Invalidate();
        }

        private void UpdateMapPickerButtons()
        {
            UpdateIndexedPickerButtons(mapToolButtons, cmbMapTool == null ? -1 : cmbMapTool.SelectedIndex, true);
            UpdateIndexedPickerButtons(mapLayerButtons, cmbMapLayer == null ? -1 : cmbMapLayer.SelectedIndex, true);

            int tool = cmbMapTool == null ? -1 : cmbMapTool.SelectedIndex;
            UpdateKeyedPickerButtons(mapTerrainButtons, GetSelectedTerrainKey(), tool == 4);
            UpdateKeyedPickerButtons(mapElevationButtons, GetSelectedElevationKey(), tool == 5);
            UpdateKeyedPickerButtons(mapWaterButtons, GetSelectedWaterKey(), tool == 6);
        }

        private void UpdateIndexedPickerButtons(Dictionary<int, Button> buttons, int selectedIndex, bool enabled)
        {
            foreach (KeyValuePair<int, Button> pair in buttons)
            {
                ApplyPickerButtonState(pair.Value, pair.Key == selectedIndex, enabled);
            }
        }

        private void UpdateKeyedPickerButtons(Dictionary<string, Button> buttons, string selectedKey, bool enabled)
        {
            foreach (KeyValuePair<string, Button> pair in buttons)
            {
                ApplyPickerButtonState(pair.Value, string.Equals(pair.Key, selectedKey, StringComparison.OrdinalIgnoreCase), enabled);
            }
        }

        private void ApplyPickerButtonState(Button button, bool selected, bool enabled)
        {
            if (button == null) return;

            MapPickerSpec spec = button.Tag as MapPickerSpec;
            if (spec == null) return;

            spec.DisplayText = isEnglish ? spec.English : spec.Russian;
            button.Enabled = enabled;
            button.BackColor = selected ? ControlPaint.Light(spec.BackColor, 0.35f) : spec.BackColor;
            button.ForeColor = GetReadableTextColor(spec.BackColor);
            spec.Selected = selected;
            button.Invalidate();
        }

        private Color GetReadableTextColor(Color backColor)
        {
            int brightness = (backColor.R * 299 + backColor.G * 587 + backColor.B * 114) / 1000;
            return brightness > 150 ? Color.Black : Color.White;
        }

        private string GetTerrainEnglishLabel(string terrain)
        {
            switch (terrain)
            {
                case "Rainforest": return "Rainforest";
                case "Savanna": return "Savanna";
                case "Desert": return "Desert";
                case "Steppe": return "Steppe";
                case "Scrub": return "Scrub";
                case "Grasslands": return "Grasslands";
                case "Forest": return "Forest";
                case "Taiga": return "Taiga";
                case "Tundra": return "Tundra";
                case "Marsh": return "Marsh";
                case "DeepForest": return "Deep forest";
                case "DeepTaiga": return "Deep taiga";
                default: return terrain;
            }
        }

        private string GetTerrainRussianLabel(string terrain)
        {
            switch (terrain)
            {
                case "Rainforest": return "Джунгли";
                case "Savanna": return "Саванна";
                case "Desert": return "Пустыня";
                case "Steppe": return "Степь";
                case "Scrub": return "Кустарник";
                case "Grasslands": return "Луга";
                case "Forest": return "Лес";
                case "Taiga": return "Тайга";
                case "Tundra": return "Тундра";
                case "Marsh": return "Болото";
                case "DeepForest": return "Глубокий лес";
                case "DeepTaiga": return "Глубокая тайга";
                default: return terrain;
            }
        }

        private void ConfigureMapDemandGrid()
        {
            if (dgvMapDemands == null) return;

            dgvMapDemands.AllowUserToAddRows = false;
            dgvMapDemands.AllowUserToDeleteRows = false;
            dgvMapDemands.RowHeadersVisible = false;
            dgvMapDemands.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvMapDemands.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvMapDemands.BackgroundColor = Color.FromArgb(64, 64, 64);
            dgvMapDemands.ForeColor = Color.Black;

            if (dgvMapDemands.Columns.Count > 0) return;

            dgvMapDemands.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMapDemandItem",
                HeaderText = "Good",
                ReadOnly = true,
                Width = 90
            });
            dgvMapDemands.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMapDemandBase",
                HeaderText = "Base",
                Width = 70
            });
            dgvMapDemands.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMapDemandCurrent",
                HeaderText = "Current",
                Width = 70
            });
        }

        private void LoadDemandGridForCurrentTarget()
        {
            if (dgvMapDemands == null) return;

            MapSettlementRecord target = GetDemandEditTarget();
            mapDemandGridLoading = true;
            dgvMapDemands.Rows.Clear();

            if (target != null)
            {
                EnsureDemandArrays(target);
                for (int i = 0; i < MerchandiseNames.Length; i++)
                {
                    dgvMapDemands.Rows.Add(
                        GetMerchandiseDisplayName(i),
                        target.BaseDemands[i].ToString("0.0", CultureInfo.InvariantCulture),
                        target.CurrentDemands[i].ToString("0.0", CultureInfo.InvariantCulture));
                }
            }

            mapDemandGridLoading = false;
        }

        private string GetMerchandiseDisplayName(int index)
        {
            if (index < 0 || index >= MerchandiseNames.Length) return "";
            string englishName = MerchandiseNames[index];
            if (isEnglish) return englishName;
            return ItemNames.ContainsKey(englishName) ? ItemNames[englishName] : englishName;
        }

        private MapSettlementRecord GetDemandEditTarget()
        {
            MapSettlementRecord mapSettlement = GetSelectedMapSettlement();
            MapSettlementRecord librarySettlement = cmbMapSettlementLibrary.SelectedItem as MapSettlementRecord;
            return mapSettlement ?? (IsNewGeneratedSettlementOption(librarySettlement) ? null : librarySettlement);
        }

        private MapSettlementRecord GetSelectedMapSettlement()
        {
            if (currentMap == null || selectedMapCell == null) return null;
            return currentMap.Settlements.FirstOrDefault(s => s.Q == selectedMapCell.Q && s.R == selectedMapCell.R);
        }

        private void ApplyDemandGridEdits()
        {
            MapSettlementRecord target = GetDemandEditTarget();
            if (target == null) return;

            EnsureDemandArrays(target);
            for (int i = 0; i < MerchandiseNames.Length && i < dgvMapDemands.Rows.Count; i++)
            {
                target.BaseDemands[i] = ReadDemandGridValue(i, 1, target.BaseDemands[i]);
                target.CurrentDemands[i] = ReadDemandGridValue(i, 2, target.CurrentDemands[i]);
            }

            target.UpdatedAt = DateTime.Now;
            if (GetSelectedMapSettlement() == null)
            {
                SaveSettlementLibrary();
                string id = target.Id;
                RefreshSettlementLibraryUi();
                SelectSettlementInLibrary(id);
            }

            LoadDemandGridForCurrentTarget();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private double ReadDemandGridValue(int row, int column, double fallback)
        {
            if (row < 0 || row >= dgvMapDemands.Rows.Count) return fallback;
            object value = dgvMapDemands.Rows[row].Cells[column].Value;
            if (value == null) return fallback;

            double parsed;
            if (double.TryParse(value.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private void CreateEmptySettlement()
        {
            int marketClass = ParseClass(cmbMapSettlementClass.Text);
            if (marketClass < 1) marketClass = ParseClass(cmbGeneratedSettlementClass.Text);
            if (marketClass < 1) marketClass = 6;

            string name = txtSettlementName != null && !string.IsNullOrWhiteSpace(txtSettlementName.Text)
                ? txtSettlementName.Text.Trim()
                : (isEnglish ? "Empty city" : "Пустой город");

            using (MapSettlementGenerationDialog dialog = new MapSettlementGenerationDialog(isEnglish, name, marketClass))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null) return;

                MapSettlementRecord record = CreateGeneratedSettlementFromMapCell(dialog.Options, null);
                record.Name = MakeUniqueSettlementName(record.Name, record.MarketClass);
                record.BaseDemands = NormalizeDemandArray(record.BaseDemands);
                record.CurrentDemands = NormalizeDemandArray(record.CurrentDemands);

                settlementLibrary.Add(record);
                SaveSettlementLibrary();
                RefreshSettlementLibraryUi();
                SelectSettlementInLibrary(record.Id);
                LoadDemandGridForCurrentTarget();
            }
        }

        private void EnsureSettlementLibraryFilterControls()
        {
            if (lblSettlementLibraryFilters != null || tabPageGenerator == null) return;

            lblSettlementLibraryFilters = new Label
            {
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = UiTheme.CreateFont(FontStyle.Bold),
                Location = new Point(700, 44),
                Size = new Size(260, 18)
            };

            txtSettlementLibrarySearch = new TextBox
            {
                Location = new Point(700, 64),
                Size = new Size(260, 23)
            };
            cmbSettlementLibraryClassFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(700, 92),
                Size = new Size(84, 23)
            };
            cmbSettlementLibraryRaceFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(792, 92),
                Size = new Size(168, 23)
            };

            txtSettlementLibrarySearch.TextChanged += (s, e) => RefreshSettlementLibraryUi();
            cmbSettlementLibraryClassFilter.SelectedIndexChanged += (s, e) => RefreshSettlementLibraryUi();
            cmbSettlementLibraryRaceFilter.SelectedIndexChanged += (s, e) => RefreshSettlementLibraryUi();

            tabPageGenerator.Controls.Add(lblSettlementLibraryFilters);
            tabPageGenerator.Controls.Add(txtSettlementLibrarySearch);
            tabPageGenerator.Controls.Add(cmbSettlementLibraryClassFilter);
            tabPageGenerator.Controls.Add(cmbSettlementLibraryRaceFilter);
            if (lstGeneratorSettlements != null)
            {
                lstGeneratorSettlements.SetBounds(700, 122, 260, 198);
            }

            FillSettlementLibraryFilterCombos();
        }

        private void FillSettlementLibraryFilterCombos()
        {
            if (lblSettlementLibraryFilters != null)
            {
                lblSettlementLibraryFilters.Text = isEnglish ? "Search and filters" : "Поиск и фильтры";
            }

            FillFilterCombo(cmbSettlementLibraryClassFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any class" : "Любой класс"),
                    new FilterItem("1", "I"),
                    new FilterItem("2", "II"),
                    new FilterItem("3", "III"),
                    new FilterItem("4", "IV"),
                    new FilterItem("5", "V"),
                    new FilterItem("6", "VI")
                });
            FillFilterCombo(cmbSettlementLibraryRaceFilter,
                new[]
                {
                    new FilterItem("", isEnglish ? "Any race" : "Любая раса"),
                    new FilterItem("Human", isEnglish ? "Human" : "Люди"),
                    new FilterItem("Dwarf", isEnglish ? "Dwarf" : "Дварфы"),
                    new FilterItem("Elf", isEnglish ? "Elf" : "Эльфы"),
                    new FilterItem("Orc", isEnglish ? "Orc" : "Орки"),
                    new FilterItem("Beastman", isEnglish ? "Beastman" : "Зверолюды")
                });
        }

        private string MakeUniqueSettlementName(string name, int marketClass)
        {
            string result = string.IsNullOrWhiteSpace(name) ? (isEnglish ? "Empty city" : "Пустой город") : name.Trim();
            if (!settlementLibrary.Any(s => string.Equals(s.Name, result, StringComparison.OrdinalIgnoreCase) && s.MarketClass == marketClass))
            {
                return result;
            }

            int suffix = 2;
            string candidate;
            do
            {
                candidate = result + " " + suffix;
                suffix++;
            }
            while (settlementLibrary.Any(s => string.Equals(s.Name, candidate, StringComparison.OrdinalIgnoreCase) && s.MarketClass == marketClass));

            return candidate;
        }

        private void UpdateMapLanguage()
        {
            if (tabPageMap == null) return;

            tabPageMap.Text = isEnglish ? "Map" : "Карта";
            lblMapScale.Text = isEnglish ? "Scale: 1 hex = 6 miles" : "Масштаб: 1 гекс = 6 миль";
            lblMapLibrary.Text = isEnglish ? "Map library" : "Библиотека карт";
            lblMapName.Text = isEnglish ? "Map name" : "Название карты";
            lblMapSize.Text = isEnglish ? "Size" : "Размер";
            lblMapTool.Text = isEnglish ? "Tool" : "Инструмент";
            lblMapLayer.Text = isEnglish ? "Layer" : "Слой";
            lblMapSettlement.Text = isEnglish ? "Settlements" : "Поселения";
            lblMapTerrain.Text = isEnglish ? "Terrain" : "Местность";
            lblMapElevation.Text = isEnglish ? "Elevation" : "Высотность";
            lblMapWater.Text = isEnglish ? "Water" : "Вода";
            lblMapDemands.Text = isEnglish ? "Selected demands" : "Demands выбранного";
            lblGeneratedSettlementClass.Text = isEnglish ? "Class" : "Класс";

            btnMapNew.Text = isEnglish ? "New" : "Новая";
            if (btnMapGenerateRegion != null) btnMapGenerateRegion.Text = isEnglish ? "Generate map" : "Сгенерировать карту";
            if (btnMapRegenerateCivilization != null) btnMapRegenerateCivilization.Text = isEnglish ? "Regenerate layers" : "Перегенерировать слои";
            if (btnMapCancelRegionGeneration != null) btnMapCancelRegionGeneration.Text = isEnglish ? "Cancel" : "Отмена";
            btnMapSave.Text = isEnglish ? "Save" : "Сохранить";
            btnMapDelete.Text = isEnglish ? "Delete" : "Удалить";
            btnMapCalculateTrade.Text = isEnglish ? "Calculate trade" : "Рассчитать торговлю";
            btnMapExportExcel.Text = isEnglish ? "Export map" : "Экспорт карты";
            btnMapImportExcel.Text = isEnglish ? "Import map" : "Импорт карты";
            if (btnMapExportPng != null) btnMapExportPng.Text = isEnglish ? "Export PNG" : "Экспорт PNG";
            if (btnMapGenerateFeatureNames != null) btnMapGenerateFeatureNames.Text = isEnglish ? "Names" : "Названия";
            btnMapSaveSettlement.Text = isEnglish ? "Save from generator" : "Сохранить из генератора";
            btnMapCreateEmptySettlement.Text = isEnglish ? "Empty city" : "Пустой город";
            btnMapApplyDemands.Text = isEnglish ? "Apply demands" : "Применить demands";
            btnGeneratedSettlementSave.Text = isEnglish ? "Save city" : "Сохранить город";
            btnMapExportSettlement.Text = isEnglish ? "Export city" : "Экспорт города";
            btnMapImportSettlement.Text = isEnglish ? "Import city" : "Импорт города";
            lblGeneratorSettlementLibrary.Text = isEnglish ? "City library" : "Библиотека городов";
            btnGeneratorLoadSettlement.Text = isEnglish ? "Open" : "Открыть";
            btnGeneratorDeleteSettlement.Text = isEnglish ? "Delete" : "Удалить";
            lblTradeMarketASettlement.Text = isEnglish ? "Market A city:" : "Город рынка A:";
            lblTradePartnerSettlement.Text = isEnglish ? "Partner city:" : "Город партнера:";
            btnTradeUseMarketASettlement.Text = isEnglish ? "Use" : "Выбрать";
            btnTradeUsePartnerSettlement.Text = isEnglish ? "Use" : "Выбрать";
            chkMapShowIcons.Text = isEnglish ? "Terrain/elevation icons" : "Значки местности/высот";
            if (lblMapEraseMode != null) lblMapEraseMode.Text = isEnglish ? "Erase mode" : "Что стирать";
            if (chkMapShowRoads != null) chkMapShowRoads.Text = isEnglish ? "Roads" : "Дороги";
            if (chkMapShowRivers != null) chkMapShowRivers.Text = isEnglish ? "Rivers" : "Реки";
            if (chkMapShowSettlements != null) chkMapShowSettlements.Text = isEnglish ? "Settlements" : "Поселения";
            if (chkMapShowStrongholds != null) chkMapShowStrongholds.Text = isEnglish ? "Strongholds" : "Крепости";
            chkMapShowSettlementIcons.Text = isEnglish ? "Settlement icons" : "Значки поселений";
            if (chkMapUseSmallMapIcons != null) chkMapUseSmallMapIcons.Text = isEnglish ? "Small icons" : "Малые значки";
            if (chkMapShowFeatureLabels != null) chkMapShowFeatureLabels.Text = isEnglish ? "Feature names" : "Названия объектов";
            if (chkMapShowRealmLabels != null) chkMapShowRealmLabels.Text = isEnglish ? "Realm names" : "Названия держав";
            if (chkMapShowSettlementLabels != null) chkMapShowSettlementLabels.Text = isEnglish ? "Settlement names" : "Названия поселений";
            if (chkMapShowStrongholdLabels != null) chkMapShowStrongholdLabels.Text = isEnglish ? "Stronghold names" : "Названия крепостей";
            if (chkMapShowHexFeatures != null) chkMapShowHexFeatures.Text = isEnglish ? "Hex features" : "Особенности гексов";
            if (chkMapShowHexCoordinates != null) chkMapShowHexCoordinates.Text = isEnglish ? "Hex coordinates" : "Координаты гексов";
            if (chkMapShowLargeHexGrid != null) chkMapShowLargeHexGrid.Text = isEnglish ? "24-mile grid" : "Сетка 24 мили";
            if (chkMapEraseAll != null) chkMapEraseAll.Text = isEnglish ? "All" : "Всё";
            if (chkMapEraseRoads != null) chkMapEraseRoads.Text = isEnglish ? "Roads" : "Дороги";
            if (chkMapEraseRivers != null) chkMapEraseRivers.Text = isEnglish ? "Rivers" : "Реки";
            if (chkMapEraseSettlements != null) chkMapEraseSettlements.Text = isEnglish ? "Settlements" : "Поселения";
            if (chkMapEraseStrongholds != null) chkMapEraseStrongholds.Text = isEnglish ? "Strongholds" : "Крепости";
            if (chkMapEraseDomains != null) chkMapEraseDomains.Text = isEnglish ? "Domains" : "Домены";
            if (chkMapEraseTerrain != null) chkMapEraseTerrain.Text = isEnglish ? "Terrain/water" : "Местность/вода";
            if (chkMapEraseFeatures != null) chkMapEraseFeatures.Text = isEnglish ? "Hex features" : "Особенности";
            if (chkMapEraseNames != null) chkMapEraseNames.Text = isEnglish ? "Names" : "Названия";
            FillSettlementLibraryFilterCombos();
            if (dgvMapDemands != null && dgvMapDemands.Columns.Count >= 3)
            {
                dgvMapDemands.Columns[0].HeaderText = isEnglish ? "Good" : "Товар";
                dgvMapDemands.Columns[1].HeaderText = isEnglish ? "Base" : "Базовые";
                dgvMapDemands.Columns[2].HeaderText = isEnglish ? "Current" : "Текущие";
            }

            SetLocalizedComboItems(cmbMapTool,
                new[] { "Select", "Settlement", "Road", "River", "Terrain", "Elevation", "Water", "Erase", "Stronghold", "Feature" },
                new[] { "Выбор", "Поселение", "Дорога", "Река", "Местность", "Высотность", "Вода", "Стереть", "Крепость", "Особенность" });
            SetLocalizedComboItems(cmbMapLayer,
                new[] { "Demands", "Terrain", "Elevation", "Landscape" },
                new[] { "Demands", "Местность", "Высотность", "Ландшафт" });
            SetLocalizedComboItems(cmbMapTerrain, mapTerrainKeys,
                new[] { "Джунгли", "Саванна", "Пустыня", "Степь", "Кустарник", "Луга", "Лес", "Тайга", "Тундра", "Болото", "Глубокий лес", "Глубокая тайга" });
            SetLocalizedComboItems(cmbMapElevation, mapElevationKeys, new[] { "Равнины", "Холмы", "Горы" });
            SetLocalizedComboItems(cmbMapWater, mapWaterKeys, new[] { "Нет", "Океан", "Море", "Озеро" });
            SetLocalizedComboItems(cmbMapEraseMode,
                new[] { "All", "Roads", "Rivers", "Settlements", "Strongholds", "Domains", "Terrain/water", "Hex features" },
                new[] { "Всё", "Дороги", "Реки", "Поселения", "Крепости", "Домены", "Местность/вода", "Особенности" });

            RefreshSettlementLibraryUi();
            UpdateMapDomainLanguage();
            UpdateMapPickerButtons();
            UpdateMapInfoForSelection();
            LoadDemandGridForCurrentTarget();
        }

        private void SetLocalizedComboItems(ComboBox combo, string[] english, string[] russian)
        {
            if (combo == null) return;
            int index = combo.SelectedIndex;
            combo.Items.Clear();
            combo.Items.AddRange((isEnglish ? english : russian).Cast<object>().ToArray());
            if (index >= 0 && index < combo.Items.Count) combo.SelectedIndex = index;
            else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void LoadMapImages()
        {
            foreach (Image image in scaledMapImages.Values)
            {
                image.Dispose();
            }
            mapImages.Clear();
            scaledMapImages.Clear();
            mapImageContentBounds.Clear();
            HashSet<string> imageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] names =
            {
                "bigwater",
                "beastmanclanold",
                "buttonborder",
                "class1", "class2", "class3", "class4", "class5", "class6",
                "dwarfclass1", "dwarfclass2", "dwarfclass3", "dwarfclass4", "dwarfclass5", "dwarfclass6",
                "deeotaiga", "deepforest", "deleteicon", "desert", "forest", "goods", "grasslands",
                "elvenclass1", "elvenclass2", "elvenclass3", "elvenclass4", "elvenclass5", "elvenclass6",
                "fortress", "fortressbarbarians", "fortressbeastman", "fortressdwarf", "fortresself", "fortressorcs",
                "hills", "lake", "marsh", "mountains", "rainforest", "river",
                "humanclanold", "notselected", "orcclanold", "rivertool", "road", "savanna", "scrub", "selecticon", "steppe", "taiga", "tundra"
            };

            foreach (string name in names)
            {
                imageNames.Add(name);
            }

            string[] assetDirectories =
            {
                Path.Combine(Application.StartupPath, "MapAssets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapAssets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "MapAssets")
            };
            foreach (string directory in assetDirectories.Where(Directory.Exists))
            {
                foreach (string file in Directory.GetFiles(directory, "*.png"))
                {
                    imageNames.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            foreach (string name in imageNames.OrderBy(n => n))
            {
                string path = Path.Combine(Application.StartupPath, "MapAssets", name + ".png");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapAssets", name + ".png");
                }
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "MapAssets", name + ".png");
                }
                if (!File.Exists(path)) continue;

                try
                {
                    mapImages[name.ToLowerInvariant()] = Image.FromFile(path);
                }
                catch
                {
                    // Отсутствующая картинка не должна ломать редактор: цветной гекс всё равно несёт данные.
                }
            }
        }

        private void LoadSettlementLibrary()
        {
            settlementLibrary = new XmlRecordStore<MapSettlementRecord>(settlementLibraryPath).Load();
            foreach (MapSettlementRecord settlement in settlementLibrary)
            {
                NormalizeSettlementMetadata(settlement);
                EnsureDemandArrays(settlement);
            }
        }

        private void SaveSettlementLibrary()
        {
            new XmlRecordStore<MapSettlementRecord>(settlementLibraryPath).Save(settlementLibrary);
        }

        private void LoadMapLibrary()
        {
            mapLibrary = new XmlRecordStore<HexMapRecord>(mapLibraryPath).Load();
            foreach (HexMapRecord map in mapLibrary)
            {
                NormalizeMap(map);
            }
        }

        private void SaveMapLibrary()
        {
            new XmlRecordStore<HexMapRecord>(mapLibraryPath).Save(mapLibrary);
        }

        private void RefreshSettlementLibraryUi()
        {
            string selectedMapId = GetSelectedSettlementControlId(cmbMapSettlementLibrary);
            string selectedGeneratorId = GetSelectedSettlementControlId(lstGeneratorSettlements);
            string selectedMarketAId = GetSelectedSettlementControlId(cmbTradeMarketASettlement);
            string selectedPartnerId = GetSelectedSettlementControlId(cmbTradePartnerSettlement);

            List<MapSettlementRecord> orderedSettlements = settlementLibrary
                .OrderBy(s => s.MarketClass)
                .ThenBy(s => s.Name)
                .ToList();
            List<MapSettlementRecord> filteredSettlements = orderedSettlements
                .Where(SettlementMatchesLibraryFilters)
                .ToList();

            SetSettlementControlDataSource(cmbMapSettlementLibrary, filteredSettlements, selectedMapId);
            SetSettlementControlDataSource(lstGeneratorSettlements, filteredSettlements, selectedGeneratorId);
            SetSettlementControlDataSource(cmbTradeMarketASettlement, orderedSettlements, selectedMarketAId);
            SetSettlementControlDataSource(cmbTradePartnerSettlement, orderedSettlements, selectedPartnerId);

            LoadDemandGridForCurrentTarget();
        }

        private string GetSelectedSettlementControlId(Control control)
        {
            object selected = null;
            ComboBox combo = control as ComboBox;
            if (combo != null)
            {
                selected = combo.SelectedItem;
            }
            else
            {
                ListBox list = control as ListBox;
                if (list != null)
                {
                    selected = list.SelectedItem;
                }
            }

            MapSettlementRecord record = selected as MapSettlementRecord;
            if (IsNewGeneratedSettlementOption(record)) return null;
            return record == null ? null : record.Id;
        }

        private void SetSettlementControlDataSource(Control control, List<MapSettlementRecord> orderedSettlements, string selectedId)
        {
            if (control == null) return;

            ComboBox combo = control as ComboBox;
            if (combo != null)
            {
                List<MapSettlementRecord> data = orderedSettlements.ToList();
                if (combo == cmbMapSettlementLibrary)
                {
                    data.Insert(0, CreateNewGeneratedSettlementOption());
                }

                combo.DataSource = null;
                combo.DataSource = data;
                SelectSettlementControlItem(combo, selectedId);
                if (combo == cmbMapSettlementLibrary && combo.SelectedIndex < 0 && combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }
                return;
            }

            ListBox list = control as ListBox;
            if (list != null)
            {
                list.DataSource = null;
                list.DataSource = orderedSettlements.ToList();
                SelectSettlementControlItem(list, selectedId);
            }
        }

        private void SelectSettlementInLibrary(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            SelectSettlementControlItem(cmbMapSettlementLibrary, id);
            SelectSettlementControlItem(lstGeneratorSettlements, id);
            SelectSettlementControlItem(cmbTradeMarketASettlement, id);
            SelectSettlementControlItem(cmbTradePartnerSettlement, id);
        }

        private MapSettlementRecord CreateNewGeneratedSettlementOption()
        {
            return new MapSettlementRecord
            {
                Id = NewGeneratedSettlementOptionId,
                Name = isEnglish ? "<New generated settlement...>" : "<Новый город...>",
                MarketClass = 0,
                Q = -1,
                R = -1
            };
        }

        private bool IsNewGeneratedSettlementOption(MapSettlementRecord record)
        {
            return record != null && record.Id == NewGeneratedSettlementOptionId;
        }

        private void SelectSettlementControlItem(Control control, string id)
        {
            if (control == null || string.IsNullOrWhiteSpace(id)) return;

            ComboBox combo = control as ComboBox;
            if (combo != null)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    MapSettlementRecord record = combo.Items[i] as MapSettlementRecord;
                    if (record != null && record.Id == id)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
                return;
            }

            ListBox list = control as ListBox;
            if (list == null) return;

            for (int i = 0; i < list.Items.Count; i++)
            {
                MapSettlementRecord record = list.Items[i] as MapSettlementRecord;
                if (record != null && record.Id == id)
                {
                    list.SelectedIndex = i;
                    return;
                }
            }
        }

        private void RefreshMapLibraryUi()
        {
            if (lstMaps == null) return;
            mapUiLoading = true;
            lstMaps.DataSource = null;
            lstMaps.DataSource = mapLibrary.OrderBy(m => m.Name).ToList();
            mapUiLoading = false;
        }

        private void CreateNewMap(bool readUi)
        {
            int width = readUi && nudMapWidth != null ? (int)nudMapWidth.Value : 12;
            int height = readUi && nudMapHeight != null ? (int)nudMapHeight.Value : 10;
            string name = readUi && txtMapName != null && !string.IsNullOrWhiteSpace(txtMapName.Text)
                ? txtMapName.Text.Trim()
                : (isEnglish ? "New map" : "Новая карта");

            currentMap = new HexMapRecord();
            currentMap.Name = name;
            currentMap.Width = width;
            currentMap.Height = height;
            currentMap.Cells.Clear();

            for (int r = 0; r < height; r++)
            {
                for (int q = 0; q < width; q++)
                {
                    currentMap.Cells.Add(new HexCellRecord { Q = q, R = r });
                }
            }

            selectedMapCell = null;
            pendingMapEdgeStart = null;
            LoadMapToEditor(currentMap);
        }

        private void SetRegionGenerationUi(bool isRunning)
        {
            if (btnMapGenerateRegion != null)
            {
                btnMapGenerateRegion.Enabled = !isRunning;
            }

            if (btnMapRegenerateCivilization != null)
            {
                btnMapRegenerateCivilization.Enabled = !isRunning;
            }

            if (btnMapCancelRegionGeneration != null)
            {
                btnMapCancelRegionGeneration.Visible = isRunning;
                btnMapCancelRegionGeneration.Enabled = isRunning;
            }

            if (prgMapRegionGeneration != null)
            {
                prgMapRegionGeneration.Visible = isRunning;
                prgMapRegionGeneration.Value = 0;
            }

            Cursor = isRunning ? Cursors.WaitCursor : Cursors.Default;
            LayoutMapToolsForWidePanel();
        }

        private void UpdateRegionGenerationProgress(RegionGenerationProgress progress)
        {
            if (progress == null || prgMapRegionGeneration == null) return;
            int value = Math.Max(prgMapRegionGeneration.Minimum, Math.Min(prgMapRegionGeneration.Maximum, progress.Percent));
            if (value >= prgMapRegionGeneration.Minimum && value <= prgMapRegionGeneration.Maximum)
            {
                prgMapRegionGeneration.Value = value;
            }
        }

        private async void GenerateRegionFromDialog()
        {
            if (regionNameService == null)
            {
                regionNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            }

            using (RegionGenerationDialog dialog = new RegionGenerationDialog(
                isEnglish,
                regionNameService,
                nudMapWidth == null ? 24 : (int)nudMapWidth.Value,
                nudMapHeight == null ? 18 : (int)nudMapHeight.Value))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null) return;

                if (currentMap != null && (currentMap.Settlements.Count > 0 || currentMap.Roads.Count > 0 || currentMap.Rivers.Count > 0 || currentMap.Domains.Count > 0))
                {
                    DialogResult replace = MessageBox.Show(
                        isEnglish
                            ? "Replace the current map with a generated region?"
                            : "\u0417\u0430\u043c\u0435\u043d\u0438\u0442\u044c \u0442\u0435\u043a\u0443\u0449\u0443\u044e \u043a\u0430\u0440\u0442\u0443 \u0441\u0433\u0435\u043d\u0435\u0440\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u044b\u043c \u0440\u0435\u0433\u0438\u043e\u043d\u043e\u043c?",
                        "ACKS",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (replace != DialogResult.Yes) return;
                }

                CancellationTokenSource generationCts = new CancellationTokenSource();
                mapRegionGenerationCts = generationCts;
                SetRegionGenerationUi(true);
                IProgress<RegionGenerationProgress> progress = new Progress<RegionGenerationProgress>(UpdateRegionGenerationProgress);

                try
                {
                    RegionMapGenerator generator = new RegionMapGenerator(regionNameService);
                    GeneratedRegionResult result = await Task.Run(
                        () => generator.Generate(dialog.Options, progress, generationCts.Token),
                        generationCts.Token);

                    generationCts.Token.ThrowIfCancellationRequested();
                    UpdateRegionGenerationProgress(new RegionGenerationProgress(96, "Applying demands"));
                    ApplyGeneratedRegionDemands(result.Map, dialog.Options);
                    generationCts.Token.ThrowIfCancellationRequested();
                    UpdateRegionGenerationProgress(new RegionGenerationProgress(98, "Loading map"));
                    LoadMapToEditor(result.Map);

                    MessageBox.Show(
                        isEnglish
                            ? "Region generated.\n\n" + string.Join("\n", result.Log)
                            : "\u0420\u0435\u0433\u0438\u043e\u043d \u0441\u0433\u0435\u043d\u0435\u0440\u0438\u0440\u043e\u0432\u0430\u043d.\n\n" + string.Join("\n", result.Log),
                        isEnglish ? "Region generation" : "\u0413\u0435\u043d\u0435\u0440\u0430\u0446\u0438\u044f \u0440\u0435\u0433\u0438\u043e\u043d\u0430",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show(
                        isEnglish ? "Region generation canceled." : "\u0413\u0435\u043d\u0435\u0440\u0430\u0446\u0438\u044f \u0440\u0435\u0433\u0438\u043e\u043d\u0430 \u043e\u0442\u043c\u0435\u043d\u0435\u043d\u0430.",
                        isEnglish ? "Region generation" : "\u0413\u0435\u043d\u0435\u0440\u0430\u0446\u0438\u044f \u0440\u0435\u0433\u0438\u043e\u043d\u0430",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        (isEnglish ? "Region generation failed:\n" : "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0441\u0433\u0435\u043d\u0435\u0440\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0440\u0435\u0433\u0438\u043e\u043d:\n") + ex.Message,
                        "ACKS",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    if (mapRegionGenerationCts == generationCts) mapRegionGenerationCts = null;
                    generationCts.Dispose();
                    SetRegionGenerationUi(false);
                }
            }
        }

        private bool SettlementMatchesLibraryFilters(MapSettlementRecord settlement)
        {
            if (settlement == null) return false;

            string search = txtSettlementLibrarySearch == null ? "" : txtSettlementLibrarySearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search)
                && !ContainsIgnoreCase(settlement.Name, search)
                && !ContainsIgnoreCase(settlement.DisplayName, search)
                && !ContainsIgnoreCase(settlement.Race, search))
            {
                return false;
            }

            string marketClass = SelectedFilterValue(cmbSettlementLibraryClassFilter);
            if (!string.IsNullOrWhiteSpace(marketClass)
                && settlement.MarketClass.ToString() != marketClass)
            {
                return false;
            }

            string race = SelectedFilterValue(cmbSettlementLibraryRaceFilter);
            return string.IsNullOrWhiteSpace(race)
                || string.Equals(settlement.Race, race, StringComparison.OrdinalIgnoreCase);
        }

        private async void RegenerateCivilizationFromDialog()
        {
            if (currentMap == null || currentMap.Cells == null || currentMap.Cells.Count == 0)
            {
                MessageBox.Show(
                    isEnglish ? "Create or load a map first." : "Сначала создайте или загрузите карту.",
                    "ACKS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (regionNameService == null)
            {
                regionNameService = NameGenerationService.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
            }

            using (CivilizationRegenerationDialog dialog = new CivilizationRegenerationDialog(
                isEnglish,
                regionNameService,
                currentMap))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null) return;

                DialogResult replace = MessageBox.Show(
                    isEnglish
                        ? "Regenerate the selected layers while preserving unchanged map data?"
                        : "Перегенерировать выбранные слои, сохранив остальные данные карты?",
                    isEnglish ? "Civilization regeneration" : "Регенерация цивилизации",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (replace != DialogResult.Yes) return;

                CancellationTokenSource generationCts = new CancellationTokenSource();
                mapRegionGenerationCts = generationCts;
                SetRegionGenerationUi(true);
                IProgress<RegionGenerationProgress> progress = new Progress<RegionGenerationProgress>(UpdateRegionGenerationProgress);

                try
                {
                    RegionMapGenerator generator = new RegionMapGenerator(regionNameService);
                    GeneratedRegionResult result = await Task.Run(
                        () => generator.RegenerateCivilization(currentMap, dialog.Options, progress, generationCts.Token),
                        generationCts.Token);

                    generationCts.Token.ThrowIfCancellationRequested();
                    if (result.Map.Settlements != null
                        && result.Map.Settlements.Count > 0
                        && (dialog.Options.GenerateSettlements || dialog.Options.GenerateDomains || dialog.Options.GenerateRivers))
                    {
                        UpdateRegionGenerationProgress(new RegionGenerationProgress(96, "Applying demands"));
                        ApplyGeneratedRegionDemands(result.Map, dialog.Options);
                    }

                    generationCts.Token.ThrowIfCancellationRequested();
                    UpdateRegionGenerationProgress(new RegionGenerationProgress(98, "Loading map"));
                    LoadMapToEditor(result.Map);

                    MessageBox.Show(
                        isEnglish
                            ? "Civilization layers regenerated.\n\n" + string.Join("\n", result.Log)
                            : "Слои цивилизации перегенерированы.\n\n" + string.Join("\n", result.Log),
                        isEnglish ? "Civilization regeneration" : "Регенерация цивилизации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show(
                        isEnglish ? "Civilization regeneration canceled." : "Регенерация цивилизации отменена.",
                        isEnglish ? "Civilization regeneration" : "Регенерация цивилизации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        (isEnglish ? "Civilization regeneration failed:\n" : "Не удалось перегенерировать цивилизацию:\n") + ex.Message,
                        "ACKS",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    if (mapRegionGenerationCts == generationCts) mapRegionGenerationCts = null;
                    generationCts.Dispose();
                    SetRegionGenerationUi(false);
                }
            }
        }

        private void ApplyGeneratedRegionDemands(HexMapRecord map, RegionGenerationOptions options)
        {
            if (map == null || map.Settlements == null) return;

            HexMapRecord previousMap = currentMap;
            currentMap = map;
            Random random = new Random(StableRegionSeed((options == null ? "" : options.Seed) + "|demands"));
            Dictionary<string, HexCellRecord> cellsByKey = new Dictionary<string, HexCellRecord>();
            foreach (HexCellRecord cell in map.Cells ?? new List<HexCellRecord>())
            {
                if (cell != null) cellsByKey[CellKey(cell.Q, cell.R)] = cell;
            }

            Dictionary<string, DomainRecord> domainsBySettlementId = new Dictionary<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, DomainRecord> domainsByHex = new Dictionary<string, DomainRecord>();
            Dictionary<string, DomainHexRecord> domainHexesByHex = new Dictionary<string, DomainHexRecord>();
            if (map.Domains != null)
            {
                foreach (DomainRecord domain in map.Domains)
                {
                    if (domain == null) continue;
                    AddDomainLookup(domainsBySettlementId, domain.CapitalSettlementId, domain);
                    if (domain.SettlementIds != null)
                    {
                        foreach (string settlementId in domain.SettlementIds)
                        {
                            AddDomainLookup(domainsBySettlementId, settlementId, domain);
                        }
                    }

                    if (domain.Hexes == null) continue;
                    foreach (DomainHexRecord hex in domain.Hexes)
                    {
                        if (hex == null) continue;

                        string key = CellKey(hex.Q, hex.R);
                        domainsByHex[key] = domain;
                        domainHexesByHex[key] = hex;
                    }
                }
            }

            // Массовая генерация поселений использует тот же порядок ACKS, что и ручная генерация города:
            // базовый спрос, модификаторы гекса/воды, возраст, затем land value.
            foreach (MapSettlementRecord settlement in map.Settlements)
            {
                HexCellRecord cell;
                cellsByKey.TryGetValue(CellKey(settlement.Q, settlement.R), out cell);
                double[] demands = BuildCellAdjustedDemands(RollBaseDemandModifiers(random), cell);
                AddDemandAdjustment(demands, GetRaceAdjustment(settlement.Race));
                AddDemandAdjustment(demands, GetAgeAdjustment(PickGeneratedSettlementAgeIndex(settlement.MarketClass, options, random)));

                DomainRecord domain;
                string cellKey = CellKey(settlement.Q, settlement.R);
                if (!domainsBySettlementId.TryGetValue(settlement.Id, out domain))
                {
                    domainsByHex.TryGetValue(cellKey, out domain);
                }

                DomainHexRecord domainHex;
                int landValue = domain == null
                    ? 6
                    : domainHexesByHex.TryGetValue(cellKey, out domainHex)
                        ? AcksDomainRules.ClampLandValue(domainHex.LandValueGp)
                        : AcksDomainRules.ClampLandValue(domain.FixedLandValueGp);
                AddLandValueDemandAdjustment(demands, landValue, random);

                settlement.BaseDemands = NormalizeDemandArray(demands);
                settlement.CurrentDemands = (double[])settlement.BaseDemands.Clone();
                settlement.UpdatedAt = DateTime.Now;
            }

            currentMap = previousMap;
        }

        private int PickGeneratedSettlementAgeIndex(int marketClass, RegionGenerationOptions options, Random random)
        {
            if (options != null && options.DefaultAgeIndex >= 0)
            {
                return Math.Max(0, Math.Min(4, options.DefaultAgeIndex));
            }

            if (random == null) return 1;

            // В ACKS Class I - крупнейший рынок. Такие города чаще старые, но редкий новый
            // крупный центр возможен, поэтому веса не закрывают молодые результаты полностью.
            int classIndex = Math.Max(1, Math.Min(6, marketClass));
            int roll = random.Next(100);
            switch (classIndex)
            {
                case 1:
                    if (roll < 4) return 0;
                    if (roll < 12) return 1;
                    if (roll < 38) return 2;
                    if (roll < 72) return 3;
                    return 4;
                case 2:
                    if (roll < 6) return 0;
                    if (roll < 18) return 1;
                    if (roll < 50) return 2;
                    if (roll < 80) return 3;
                    return 4;
                case 3:
                    if (roll < 10) return 0;
                    if (roll < 30) return 1;
                    if (roll < 64) return 2;
                    if (roll < 86) return 3;
                    return 4;
                case 4:
                    if (roll < 18) return 0;
                    if (roll < 45) return 1;
                    if (roll < 76) return 2;
                    if (roll < 92) return 3;
                    return 4;
                case 5:
                    if (roll < 32) return 0;
                    if (roll < 62) return 1;
                    if (roll < 86) return 2;
                    if (roll < 96) return 3;
                    return 4;
                default:
                    if (roll < 50) return 0;
                    if (roll < 78) return 1;
                    if (roll < 93) return 2;
                    if (roll < 98) return 3;
                    return 4;
            }
        }

        private void AddDomainLookup(Dictionary<string, DomainRecord> lookup, string key, DomainRecord domain)
        {
            if (lookup == null || domain == null || string.IsNullOrWhiteSpace(key)) return;
            if (!lookup.ContainsKey(key)) lookup[key] = domain;
        }

        private int StableRegionSeed(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text ?? "")
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return hash == int.MinValue ? 1 : Math.Abs(hash);
            }
        }

        private void LoadMapToEditor(HexMapRecord map)
        {
            if (map == null) return;
            currentMap = CloneMap(map);
            NormalizeMap(currentMap);
            RebuildCurrentMapIndex();

            if (txtMapName != null) txtMapName.Text = currentMap.Name;
            if (nudMapWidth != null) nudMapWidth.Value = Math.Max(nudMapWidth.Minimum, Math.Min(nudMapWidth.Maximum, currentMap.Width));
            if (nudMapHeight != null) nudMapHeight.Value = Math.Max(nudMapHeight.Minimum, Math.Min(nudMapHeight.Maximum, currentMap.Height));

            selectedMapCell = null;
            pendingMapEdgeStart = null;
            selectedMapDomainId = null;
            UpdateMapScrollSize();
            pnlHexMap.Invalidate();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            LoadDemandGridForCurrentTarget();
        }

        private HexMapRecord CloneMap(HexMapRecord source)
        {
            return XmlSerialization.Clone(source);
        }

        private void RebuildCurrentMapIndex()
        {
            if (currentMap == null || currentMap.Cells == null)
            {
                currentMapCellIndex = new Dictionary<string, HexCellRecord>();
                currentMapSettlementsByCell = new Dictionary<string, List<MapSettlementRecord>>();
                currentMapSettlementsById = new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase);
                currentMapStrongholdsByCell = new Dictionary<string, List<DomainRecord>>();
                currentMapDomainBySettlementId = new Dictionary<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
                currentMapDomainByHex = new Dictionary<string, DomainRecord>();
                currentMapRoadsByCell = new Dictionary<string, List<MapEdgeRecord>>();
                currentMapRiversByCell = new Dictionary<string, List<MapEdgeRecord>>();
                currentMapFeaturesByCell = new Dictionary<string, List<HexFeatureRecord>>();
                currentMapDungeonsById = new Dictionary<string, DungeonRecord>(StringComparer.OrdinalIgnoreCase);
                currentMapWaterFeatureLabels = new List<MapFeatureLabelRecord>();
                currentMapRiverFeatureLabels = new List<MapFeatureLabelRecord>();
                currentMapDrawOrder = new List<HexCellRecord>();
                currentMapWorldBounds = RectangleF.Empty;
                currentMapMinQ = currentMapMaxQ = currentMapMinR = currentMapMaxR = 0;
                return;
            }

            // Индекс и порядок отрисовки пересобираются только при загрузке/создании карты;
            // во время pan/zoom это убирает сортировки и линейные поиски из горячего Paint-пути.
            currentMapCellIndex = currentMap.Cells
                .GroupBy(c => CellKey(c.Q, c.R))
                .ToDictionary(g => g.Key, g => g.First());
            currentMapDrawOrder = currentMap.Cells
                .OrderBy(c => c.R)
                .ThenBy(c => c.Q)
                .ToList();
            RebuildMapObjectIndexes();
            RebuildMapFeatureLabelIndex();
            RebuildMapWorldBounds();
        }

        private void RebuildMapObjectIndexes()
        {
            currentMapSettlementsByCell = new Dictionary<string, List<MapSettlementRecord>>();
            currentMapSettlementsById = new Dictionary<string, MapSettlementRecord>(StringComparer.OrdinalIgnoreCase);
            currentMapStrongholdsByCell = new Dictionary<string, List<DomainRecord>>();
            currentMapDomainBySettlementId = new Dictionary<string, DomainRecord>(StringComparer.OrdinalIgnoreCase);
            currentMapDomainByHex = new Dictionary<string, DomainRecord>();
            currentMapRoadsByCell = new Dictionary<string, List<MapEdgeRecord>>();
            currentMapRiversByCell = new Dictionary<string, List<MapEdgeRecord>>();
            currentMapFeaturesByCell = new Dictionary<string, List<HexFeatureRecord>>();
            currentMapDungeonsById = new Dictionary<string, DungeonRecord>(StringComparer.OrdinalIgnoreCase);
            if (currentMap == null) return;

            // Эти индексы нужны только для viewport и hit-test. Они не меняют модель карты,
            // но убирают линейные поиски по всем поселениям/доменам/дорогам на каждый Paint.
            foreach (MapSettlementRecord settlement in currentMap.Settlements ?? new List<MapSettlementRecord>())
            {
                if (settlement == null) continue;
                AddIndexedValue(currentMapSettlementsByCell, CellKey(settlement.Q, settlement.R), settlement);
                if (!string.IsNullOrWhiteSpace(settlement.Id) && !currentMapSettlementsById.ContainsKey(settlement.Id))
                {
                    currentMapSettlementsById[settlement.Id] = settlement;
                }
            }

            foreach (DomainRecord domain in currentMap.Domains ?? new List<DomainRecord>())
            {
                if (domain == null) continue;
                if (HasVisibleStronghold(domain))
                {
                    AddIndexedValue(currentMapStrongholdsByCell, CellKey(domain.StrongholdQ, domain.StrongholdR), domain);
                }

                AddDomainSettlementIndex(domain.CapitalSettlementId, domain);
                foreach (string settlementId in domain.SettlementIds ?? new List<string>())
                {
                    AddDomainSettlementIndex(settlementId, domain);
                }

                foreach (DomainHexRecord hex in domain.Hexes ?? new List<DomainHexRecord>())
                {
                    string key = CellKey(hex.Q, hex.R);
                    if (!currentMapDomainByHex.ContainsKey(key)) currentMapDomainByHex[key] = domain;
                }
            }

            IndexMapEdges(currentMapRoadsByCell, currentMap.Roads);
            IndexMapEdges(currentMapRiversByCell, currentMap.Rivers);
            foreach (HexFeatureRecord feature in currentMap.Features ?? new List<HexFeatureRecord>())
            {
                if (feature == null) continue;
                AddIndexedValue(currentMapFeaturesByCell, CellKey(feature.Q, feature.R), feature);
            }

            foreach (DungeonRecord dungeon in currentMap.Dungeons ?? new List<DungeonRecord>())
            {
                if (dungeon == null || string.IsNullOrWhiteSpace(dungeon.Id)) continue;
                if (!currentMapDungeonsById.ContainsKey(dungeon.Id)) currentMapDungeonsById[dungeon.Id] = dungeon;
            }
        }

        private void AddDomainSettlementIndex(string settlementId, DomainRecord domain)
        {
            if (string.IsNullOrWhiteSpace(settlementId) || domain == null) return;
            if (!currentMapDomainBySettlementId.ContainsKey(settlementId))
            {
                currentMapDomainBySettlementId[settlementId] = domain;
            }
        }

        private void IndexMapEdges(Dictionary<string, List<MapEdgeRecord>> index, List<MapEdgeRecord> edges)
        {
            if (index == null || edges == null) return;
            foreach (MapEdgeRecord edge in edges)
            {
                if (edge == null) continue;
                AddIndexedValue(index, CellKey(edge.AQ, edge.AR), edge);
                AddIndexedValue(index, CellKey(edge.BQ, edge.BR), edge);
            }
        }

        private void AddIndexedValue<T>(Dictionary<string, List<T>> index, string key, T value)
        {
            if (index == null || string.IsNullOrWhiteSpace(key) || value == null) return;
            List<T> list;
            if (!index.TryGetValue(key, out list))
            {
                list = new List<T>();
                index[key] = list;
            }

            list.Add(value);
        }

        private void RebuildMapWorldBounds()
        {
            if (currentMap == null || currentMap.Cells == null || currentMap.Cells.Count == 0)
            {
                currentMapWorldBounds = RectangleF.Empty;
                currentMapMinQ = currentMapMaxQ = currentMapMinR = currentMapMaxR = 0;
                return;
            }

            currentMapMinQ = currentMap.Cells.Min(c => c.Q);
            currentMapMaxQ = currentMap.Cells.Max(c => c.Q);
            currentMapMinR = currentMap.Cells.Min(c => c.R);
            currentMapMaxR = currentMap.Cells.Max(c => c.R);

            bool hasEvenRow = false;
            bool hasOddRow = false;
            for (int r = currentMapMinR; r <= currentMapMaxR; r++)
            {
                if ((r & 1) == 0) hasEvenRow = true;
                else hasOddRow = true;
            }

            float minOffset = currentMapMinQ + (hasEvenRow ? 0f : 0.5f);
            float maxOffset = currentMapMaxQ + (hasOddRow ? 0.5f : 0f);
            float left = 38f + MapHexWidth * minOffset - MapHexHorizontalRadius;
            float right = 38f + MapHexWidth * maxOffset + MapHexHorizontalRadius;
            float top = 38f + MapHexRowHeight * currentMapMinR - MapHexSize;
            float bottom = 38f + MapHexRowHeight * currentMapMaxR + MapHexSize;
            currentMapWorldBounds = RectangleF.FromLTRB(left, top, right, bottom);
        }

        private void NormalizeMap(HexMapRecord map)
        {
            if (map.Cells == null) map.Cells = new List<HexCellRecord>();
            if (map.Settlements == null) map.Settlements = new List<MapSettlementRecord>();
            if (map.Roads == null) map.Roads = new List<MapEdgeRecord>();
            if (map.Rivers == null) map.Rivers = new List<MapEdgeRecord>();
            if (map.Domains == null) map.Domains = new List<DomainRecord>();
            if (map.Realms == null) map.Realms = new List<RealmRecord>();
            if (map.VassalLinks == null) map.VassalLinks = new List<VassalLinkRecord>();
            if (map.Features == null) map.Features = new List<HexFeatureRecord>();
            if (map.Dungeons == null) map.Dungeons = new List<DungeonRecord>();

            HashSet<string> existingCells = new HashSet<string>(map.Cells.Select(c => CellKey(c.Q, c.R)));
            for (int r = 0; r < map.Height; r++)
            {
                for (int q = 0; q < map.Width; q++)
                {
                    if (!existingCells.Contains(CellKey(q, r)))
                    {
                        map.Cells.Add(new HexCellRecord { Q = q, R = r });
                    }
                }
            }

            foreach (HexCellRecord cell in map.Cells)
            {
                if (string.IsNullOrWhiteSpace(cell.Terrain)) cell.Terrain = "Grasslands";
                if (string.IsNullOrWhiteSpace(cell.Elevation)) cell.Elevation = "Plains";
                if (string.IsNullOrWhiteSpace(cell.Water)) cell.Water = "None";
                if (cell.WaterFeatureName == null) cell.WaterFeatureName = "";
                NormalizeWaterSurface(cell);
            }

            foreach (MapEdgeRecord edge in map.Roads.Concat(map.Rivers))
            {
                if (edge == null) continue;
                if (string.IsNullOrWhiteSpace(edge.Kind)) edge.Kind = map.Rivers.Contains(edge) ? "River" : "Road";
                if (edge.FeatureName == null) edge.FeatureName = "";
            }

            foreach (MapSettlementRecord settlement in map.Settlements)
            {
                NormalizeSettlementMetadata(settlement);
                EnsureDemandArrays(settlement);
            }

            foreach (HexFeatureRecord feature in map.Features)
            {
                MapDataNormalizer.NormalizeHexFeature(feature);
            }

            foreach (DungeonRecord dungeon in map.Dungeons)
            {
                MapDataNormalizer.NormalizeDungeon(dungeon);
            }

            NormalizeMapDomains(map);
            NormalizeMapRealms(map);
        }

        private void NormalizeSettlementMetadata(MapSettlementRecord settlement)
        {
            if (settlement == null) return;
            if (string.IsNullOrWhiteSpace(settlement.Id)) settlement.Id = Guid.NewGuid().ToString("N");
            settlement.Race = NormalizeSettlementRace(settlement.Race);
            if (settlement.MarketClass < 1 || settlement.MarketClass > 6) settlement.MarketClass = 6;
        }

        private string NormalizeSettlementRace(string race)
        {
            return MapDataNormalizer.SettlementRace(race);
        }

        private string LocalizedSettlementRace(string race)
        {
            string normalized = NormalizeSettlementRace(race);
            if (isEnglish) return normalized;
            if (normalized == "Dwarf") return "Дварф";
            if (normalized == "Elf") return "Эльф";
            if (normalized == "Orc") return "Орк";
            if (normalized == "Beastman") return "Зверолюд";
            return "Человек";
        }

        private string LocalizedRealmDisplayName(RealmRecord realm)
        {
            if (realm == null) return isEnglish ? "None" : "Нет";
            string name = string.IsNullOrWhiteSpace(realm.Name)
                ? (isEnglish ? "(unnamed realm)" : "(безымянная держава)")
                : realm.Name;
            return name + " [" + LocalizedRealmTitle(realm) + "]";
        }

        private string LocalizedRealmTier(string tier)
        {
            return RealmTitleCatalog.RealmTitle("", tier, !isEnglish, "");
        }

        private string LocalizedRealmTitle(RealmRecord realm)
        {
            if (realm == null) return isEnglish ? "Realm" : "Держава";
            return RealmTitleCatalog.RealmTitle(realm.CultureKey, realm.Tier, !isEnglish, realm.TitleOverride);
        }

        private void NormalizeMapRealms(HexMapRecord map)
        {
            if (map == null) return;
            if (map.Realms == null) map.Realms = new List<RealmRecord>();
            if (map.VassalLinks == null) map.VassalLinks = new List<VassalLinkRecord>();

            HashSet<string> realmIds = new HashSet<string>();
            foreach (RealmRecord realm in map.Realms)
            {
                if (realm == null) continue;
                if (string.IsNullOrWhiteSpace(realm.Id)) realm.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(realm.Name)) realm.Name = isEnglish ? "Unnamed realm" : "Безымянная держава";
                if (string.IsNullOrWhiteSpace(realm.Tier)) realm.Tier = "County";
                if (realm.TitleOverride == null) realm.TitleOverride = "";
                if (realm.FemaleTitleOverride == null) realm.FemaleTitleOverride = "";
                if (string.IsNullOrWhiteSpace(realm.CultureKey)) realm.CultureKey = "";
                realmIds.Add(realm.Id);
            }

            map.VassalLinks = map.VassalLinks
                .Where(v => v != null
                    && realmIds.Contains(v.LiegeRealmId)
                    && realmIds.Contains(v.VassalRealmId)
                    && v.LiegeRealmId != v.VassalRealmId)
                .ToList();
        }

        private void SaveCurrentMapToLibrary()
        {
            if (currentMap == null) return;

            currentMap.Name = string.IsNullOrWhiteSpace(txtMapName.Text)
                ? (isEnglish ? "Unnamed map" : "Безымянная карта")
                : txtMapName.Text.Trim();
            currentMap.Width = (int)nudMapWidth.Value;
            currentMap.Height = (int)nudMapHeight.Value;
            currentMap.UpdatedAt = DateTime.Now;

            NormalizeMap(currentMap);
            mapLibrary.RemoveAll(m => m.Id == currentMap.Id);
            mapLibrary.Add(CloneMap(currentMap));
            SaveMapLibrary();
            RefreshMapLibraryUi();
            SelectMap(currentMap.Id);
        }

        private void SelectMap(string id)
        {
            for (int i = 0; i < lstMaps.Items.Count; i++)
            {
                HexMapRecord map = lstMaps.Items[i] as HexMapRecord;
                if (map != null && map.Id == id)
                {
                    lstMaps.SelectedIndex = i;
                    return;
                }
            }
        }

        private void DeleteSelectedMap()
        {
            HexMapRecord selected = lstMaps.SelectedItem as HexMapRecord;
            if (selected == null) return;

            DialogResult result = MessageBox.Show(
                isEnglish ? "Delete selected map?" : "Удалить выбранную карту?",
                "ACKS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            mapLibrary.RemoveAll(m => m.Id == selected.Id);
            SaveMapLibrary();
            RefreshMapLibraryUi();
            CreateNewMap(false);
        }

        private void SaveCurrentGeneratorSettlement()
        {
            if (A == null || A.Length == 0) InitializeData();

            string name = string.IsNullOrWhiteSpace(txtSettlementName.Text)
                ? (isEnglish ? "Unnamed settlement" : "Безымянное поселение")
                : txtSettlementName.Text.Trim();
            ComboBox classSource = tabControl1 != null && tabControl1.SelectedTab == tabPageMap
                ? cmbMapSettlementClass
                : cmbGeneratedSettlementClass;
            int marketClass = ParseClass(classSource.Text);
            if (marketClass < 1) marketClass = ParseClass(cmbGeneratedSettlementClass.Text);
            if (marketClass < 1) marketClass = ParseClass(cmbMapSettlementClass.Text);
            if (marketClass < 1) marketClass = 6;
            if (IsGeneratorClanholdKind()) marketClass = 6;

            MapSettlementRecord record = settlementLibrary
                .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) && s.MarketClass == marketClass);

            if (record == null)
            {
                record = new MapSettlementRecord();
                settlementLibrary.Add(record);
            }

            record.Name = name;
            record.MarketClass = marketClass;
            record.Race = GetGeneratorSettlementRace();
            record.LandValue = IsGeneratorClanholdKind() ? SettlementMetadataClanhold : "";
            record.BaseDemands = (double[])A.Clone();
            record.CurrentDemands = (double[])A.Clone();
            record.Q = -1;
            record.R = -1;
            record.UpdatedAt = DateTime.Now;

            SaveSettlementLibrary();
            RefreshSettlementLibraryUi();
            SelectSettlementInLibrary(record.Id);
            LoadDemandGridForCurrentTarget();
        }

        private void LoadSelectedSettlementIntoGenerator()
        {
            MapSettlementRecord record = lstGeneratorSettlements == null
                ? null
                : lstGeneratorSettlements.SelectedItem as MapSettlementRecord;
            if (record == null) return;

            EnsureDemandArrays(record);
            txtSettlementName.Text = record.Name;
            SelectGeneratorRaceKind(SettlementKindFromRecord(record));
            SelectMarketClassCombo(cmbGeneratedSettlementClass, record.MarketClass);
            SelectMarketClassCombo(cmbMapSettlementClass, record.MarketClass);
            A = (double[])record.BaseDemands.Clone();
            ClearGeneratorDemandBreakdown();
            UpdateDataGridView(dataGridView1);
            UpdateTradeGrids();
            SelectSettlementInLibrary(record.Id);
            LoadDemandGridForCurrentTarget();
        }

        private void DeleteSelectedGeneratorSettlement()
        {
            MapSettlementRecord record = lstGeneratorSettlements == null
                ? null
                : lstGeneratorSettlements.SelectedItem as MapSettlementRecord;
            if (record == null) return;

            DialogResult result = MessageBox.Show(
                isEnglish ? "Delete selected city from the library?" : "Удалить выбранный город из библиотеки?",
                "ACKS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            settlementLibrary.RemoveAll(s => s.Id == record.Id);
            SaveSettlementLibrary();
            RefreshSettlementLibraryUi();
        }

        private void ApplySelectedSettlementToTrade(bool marketA)
        {
            ComboBox sourceCombo = marketA ? cmbTradeMarketASettlement : cmbTradePartnerSettlement;
            MapSettlementRecord record = sourceCombo == null ? null : sourceCombo.SelectedItem as MapSettlementRecord;
            if (record == null) return;

            EnsureDemandArrays(record);
            double[] demands = (double[])record.CurrentDemands.Clone();
            if (marketA)
            {
                A = demands;
                SelectMarketClassCombo(cmbMarketAClass, record.MarketClass);
            }
            else
            {
                PartnerDemands = demands;
                SelectMarketClassCombo(cmbPartnerClass, record.MarketClass);
            }

            UpdateTradeGrids();
        }

        private void UpdateTradeClassFromSelectedSettlement(bool marketA)
        {
            ComboBox sourceCombo = marketA ? cmbTradeMarketASettlement : cmbTradePartnerSettlement;
            ComboBox classCombo = marketA ? cmbMarketAClass : cmbPartnerClass;
            MapSettlementRecord record = sourceCombo == null ? null : sourceCombo.SelectedItem as MapSettlementRecord;
            if (record == null || record.MarketClass < 1 || record.MarketClass > 6) return;

            SelectMarketClassCombo(classCombo, record.MarketClass);
        }

        private void SelectMarketClassCombo(ComboBox combo, int marketClass)
        {
            if (combo == null) return;

            for (int i = 0; i < combo.Items.Count; i++)
            {
                object item = combo.Items[i];
                if (item != null && ParseClass(item.ToString()) == marketClass)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void GenerateAndPlaceSettlementOnCell(HexCellRecord cell)
        {
            int defaultClass = ParseClass(cmbMapSettlementClass.Text);
            if (defaultClass < 1) defaultClass = 6;

            string suggestedName = MakeUniqueSettlementName(isEnglish ? "New settlement" : "Новый город", defaultClass);
            using (MapSettlementGenerationDialog dialog = new MapSettlementGenerationDialog(isEnglish, suggestedName, defaultClass))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null) return;

                MapSettlementRecord libraryRecord = CreateGeneratedSettlementFromMapCell(dialog.Options, cell);
                settlementLibrary.Add(libraryRecord);
                SaveSettlementLibrary();

                currentMap.Settlements.RemoveAll(s => s.Q == cell.Q && s.R == cell.R);
                currentMap.Settlements.Add(CloneSettlementForMap(libraryRecord, cell.Q, cell.R));
                RebuildCurrentMapIndex();

                selectedMapCell = cell;
                RefreshSettlementLibraryUi();
                SelectSettlementInLibrary(libraryRecord.Id);
                LoadDemandGridForCurrentTarget();
                UpdateMapInfoForSelection();
                pnlHexMap.Invalidate();

                MessageBox.Show(isEnglish
                    ? "Settlement \"" + libraryRecord.Name + "\" was generated, saved to the city library, and placed on the map.\n\nTo remove it from the library, open the Generator tab and delete it from the City Library."
                    : "Город \"" + libraryRecord.Name + "\" создан, сохранён в библиотеку городов и поставлен на карту.\n\nЕсли нужно удалить его из библиотеки, откройте вкладку \"Генератор\" и удалите его из библиотеки городов.",
                    isEnglish ? "Settlement generated" : "Город создан",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void PlaceStrongholdOnCell(HexCellRecord cell)
        {
            if (currentMap == null || cell == null) return;
            if (cell.Water != "None")
            {
                MessageBox.Show(
                    isEnglish ? "Strongholds cannot be placed on water." : "Крепости нельзя ставить на воде.",
                    "ACKS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            NormalizeMap(currentMap);
            MapSettlementRecord settlementAtCell = currentMap.Settlements == null
                ? null
                : currentMap.Settlements.FirstOrDefault(s => s.Q == cell.Q && s.R == cell.R);
            DomainRecord existing = GetDomainAtCell(cell);

            if (existing != null)
            {
                DomainRecord edited = XmlSerialization.Clone(existing);
                ConfigureDomainStrongholdAtCell(edited, cell, settlementAtCell);
                using (DomainEditorDialog dialog = new DomainEditorDialog(isEnglish, edited, characterLibrary))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    edited = dialog.Domain;
                }

                ConfigureDomainStrongholdAtCell(edited, cell, settlementAtCell);
                SaveDomainRulerIfRequested(edited);
                RecalculateDomain(edited, true, existing.BaseMorale, existing.CurrentMorale);
                int index = currentMap.Domains.FindIndex(d => d.Id == existing.Id);
                if (index >= 0) currentMap.Domains[index] = edited;
                selectedMapDomainId = edited.Id;
            }
            else
            {
                DomainRecord domain = new DomainRecord
                {
                    Name = settlementAtCell == null
                        ? (isEnglish ? "New stronghold" : "Новая крепость")
                        : (isEnglish ? settlementAtCell.Name + " Domain" : "Домен " + settlementAtCell.Name),
                    ColorArgb = GetNextDomainColor()
                };
                domain.Hexes.Add(new DomainHexRecord
                {
                    Q = cell.Q,
                    R = cell.R,
                    LandValueGp = RollDomainLandValue(domain)
                });
                ConfigureDomainStrongholdAtCell(domain, cell, settlementAtCell);

                using (DomainEditorDialog dialog = new DomainEditorDialog(isEnglish, domain, characterLibrary))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    domain = dialog.Domain;
                }

                if (domain.Hexes == null || domain.Hexes.Count == 0)
                {
                    domain.Hexes = new List<DomainHexRecord>
                    {
                        new DomainHexRecord { Q = cell.Q, R = cell.R, LandValueGp = RollDomainLandValue(domain) }
                    };
                }

                ConfigureDomainStrongholdAtCell(domain, cell, settlementAtCell);
                SaveDomainRulerIfRequested(domain);
                RecalculateDomain(domain, false);
                foreach (DomainHexRecord hex in domain.Hexes.ToList())
                {
                    RemoveCellFromAllDomains(GetCell(hex.Q, hex.R), true);
                }

                currentMap.Domains.Add(domain);
                selectedMapDomainId = domain.Id;
            }

            RebuildCurrentMapIndex();
            RefreshMapDomainList();
            UpdateMapInfoForSelection();
            pnlHexMap.Invalidate();
        }

        private void ConfigureDomainStrongholdAtCell(DomainRecord domain, HexCellRecord cell, MapSettlementRecord settlementAtCell)
        {
            if (domain == null || cell == null) return;

            if (string.IsNullOrWhiteSpace(domain.StrongholdId)) domain.StrongholdId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(domain.StrongholdName))
            {
                string baseName = string.IsNullOrWhiteSpace(domain.Name)
                    ? (isEnglish ? "Stronghold" : "Крепость")
                    : domain.Name;
                domain.StrongholdName = isEnglish ? baseName + " Stronghold" : "Крепость " + baseName;
            }

            if (string.IsNullOrWhiteSpace(domain.StrongholdType)) domain.StrongholdType = "Fortress";
            domain.StrongholdQ = cell.Q;
            domain.StrongholdR = cell.R;
            domain.StrongholdSecuresDomain = true;
            domain.StrongholdActsAsMarketClassVI = true;
            domain.StrongholdIsUnderground = string.Equals(domain.DomainType, "Dwarven Vault", StringComparison.OrdinalIgnoreCase);
            domain.StrongholdNaturalMajesty = string.Equals(domain.DomainType, "Elven Fastness", StringComparison.OrdinalIgnoreCase);
            domain.StrongholdIconKey = GetDomainStrongholdIconKey(domain);

            if (settlementAtCell != null)
            {
                domain.CapitalSettlementId = string.IsNullOrWhiteSpace(domain.CapitalSettlementId)
                    ? settlementAtCell.Id
                    : domain.CapitalSettlementId;
                if (domain.SettlementIds == null) domain.SettlementIds = new List<string>();
                if (!domain.SettlementIds.Contains(settlementAtCell.Id, StringComparer.OrdinalIgnoreCase))
                {
                    domain.SettlementIds.Add(settlementAtCell.Id);
                }

                domain.StrongholdInSettlement = true;
                domain.StrongholdSettlementId = settlementAtCell.Id;
            }
            else
            {
                domain.StrongholdInSettlement = false;
                domain.StrongholdSettlementId = "";
            }
        }

        private MapSettlementRecord CreateGeneratedSettlementFromMapCell(MapSettlementGenerationOptions options, HexCellRecord cell)
        {
            int landValueGp = options.LandValueGp > 0
                ? AcksDomainRules.ClampLandValue(options.LandValueGp)
                : ParseSettlementLandValueGp(options.LandValue, 6);
            double[] demands = options.GenerateDemands
                ? BuildCellAdjustedDemands(RollBaseDemandModifiers(), cell)
                : new double[AcksRules.DemandCount];

            if (options.GenerateDemands)
            {
                AddDemandAdjustment(demands, GetAgeAdjustment(options.AgeIndex));
                if (IsDemandModifierRace(options.Race))
                {
                    AddDemandAdjustment(demands, GetRaceAdjustment(options.Race));
                }

                // Ручное поселение получает тот же land value-модификатор, что и автогенерация региона.
                AddLandValueDemandAdjustment(demands, landValueGp, demandRandom);
            }

            MapSettlementRecord record = new MapSettlementRecord();
            record.Name = MakeUniqueSettlementName(options.SettlementName, options.MarketClass);
            record.MarketClass = options.MarketClass;
            record.Q = -1;
            record.R = -1;
            record.Race = NormalizeSettlementRace(options.Race);
            record.LandValue = BuildSettlementLandValueMetadata(options.LandValue, landValueGp);
            record.BaseDemands = NormalizeDemandArray(demands);
            record.CurrentDemands = (double[])record.BaseDemands.Clone();
            record.UpdatedAt = DateTime.Now;
            if (options.ApplyNeighborInfluence)
            {
                ApplyNearbySettlementInfluenceToNewSettlement(record, cell);
            }

            return record;
        }

        private void ApplyNearbySettlementInfluenceToNewSettlement(MapSettlementRecord record, HexCellRecord cell)
        {
            if (record == null || cell == null || currentMap == null || currentMap.Settlements == null) return;

            record.Q = cell.Q;
            record.R = cell.R;
            EnsureDemandArrays(record);
            foreach (MapSettlementRecord neighbor in currentMap.Settlements.Where(s => s != null))
            {
                if (neighbor.Q == cell.Q && neighbor.R == cell.R) continue;
                EnsureDemandArrays(neighbor);
                int distanceHexes = HexDistance(cell.Q, cell.R, neighbor.Q, neighbor.R);
                bool hasRoad = HasRoadPathBetweenCells(cell.Q, cell.R, neighbor.Q, neighbor.R);
                double distanceMiles = distanceHexes * MapScaleMiles;
                if (!AcksRules.IsTradeRouteInRange(record.MarketClass, neighbor.MarketClass, hasRoad, distanceMiles)) continue;

                double[] neighborCopy = (double[])neighbor.CurrentDemands.Clone();
                AcksRules.ApplyTradeInfluence(record.MarketClass, neighbor.MarketClass, record.CurrentDemands, neighborCopy);
            }

            record.BaseDemands = (double[])record.CurrentDemands.Clone();
            record.Q = -1;
            record.R = -1;
        }

        private bool HasRoadPathBetweenCells(int startQ, int startR, int endQ, int endR)
        {
            if (currentMap == null || currentMap.Roads == null || currentMap.Roads.Count == 0) return false;
            string start = CellKey(startQ, startR);
            string target = CellKey(endQ, endR);
            if (start == target) return true;

            Dictionary<string, List<string>> graph = new Dictionary<string, List<string>>();
            foreach (MapEdgeRecord road in currentMap.Roads.Where(r => r != null))
            {
                string a = CellKey(road.AQ, road.AR);
                string b = CellKey(road.BQ, road.BR);
                if (!graph.ContainsKey(a)) graph[a] = new List<string>();
                if (!graph.ContainsKey(b)) graph[b] = new List<string>();
                graph[a].Add(b);
                graph[b].Add(a);
            }

            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                List<string> nextCells;
                if (!graph.TryGetValue(current, out nextCells)) continue;
                foreach (string next in nextCells)
                {
                    if (!visited.Add(next)) continue;
                    if (next == target) return true;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private bool CanPlaceSettlement(HexCellRecord cell)
        {
            if (cell == null) return false;
            if (cell.Water != "None") return false;
            return cell.Terrain != "Marsh" && cell.Terrain != "DeepForest" && cell.Terrain != "DeepTaiga";
        }

        private MapSettlementRecord CloneSettlementForMap(MapSettlementRecord source, int q, int r)
        {
            EnsureDemandArrays(source);
            MapSettlementRecord clone = new MapSettlementRecord();
            clone.Id = Guid.NewGuid().ToString("N");
            clone.Name = source.Name;
            clone.MarketClass = source.MarketClass;
            clone.Q = q;
            clone.R = r;
            clone.Race = NormalizeSettlementRace(source.Race);
            clone.LandValue = source.LandValue;
            clone.BaseDemands = (double[])source.BaseDemands.Clone();
            clone.CurrentDemands = (double[])clone.BaseDemands.Clone();
            clone.UpdatedAt = DateTime.Now;
            return clone;
        }

        private void AddMapEdgeFromClick(HexCellRecord cell, string kind)
        {
            if (pendingMapEdgeStart == null)
            {
                pendingMapEdgeStart = cell;
                return;
            }

            if (pendingMapEdgeStart.Q == cell.Q && pendingMapEdgeStart.R == cell.R)
            {
                pendingMapEdgeStart = null;
                return;
            }

            if (HexDistance(pendingMapEdgeStart.Q, pendingMapEdgeStart.R, cell.Q, cell.R) != 1)
            {
                MessageBox.Show(isEnglish
                    ? "Roads and rivers are drawn between neighboring hexes."
                    : "Дороги и реки рисуются между соседними гексами.");
                pendingMapEdgeStart = cell;
                return;
            }

            MapEdgeRecord edge = new MapEdgeRecord
            {
                AQ = pendingMapEdgeStart.Q,
                AR = pendingMapEdgeStart.R,
                BQ = cell.Q,
                BR = cell.R,
                Kind = kind
            };

            List<MapEdgeRecord> target = kind == "Road" ? currentMap.Roads : currentMap.Rivers;
            string key = edge.NormalizedKey();
            if (!target.Any(e => e.NormalizedKey() == key))
            {
                target.Add(edge);
                RebuildCurrentMapIndex();
            }

            pendingMapEdgeStart = null;
        }

        private void PlaceHexFeatureOnCell(HexCellRecord cell)
        {
            if (currentMap == null || cell == null) return;
            if (currentMap.Features == null) currentMap.Features = new List<HexFeatureRecord>();
            if (currentMap.Dungeons == null) currentMap.Dungeons = new List<DungeonRecord>();

            LoadDungeonLibrary();
            using (MapHexFeaturePlacementDialog dialog = new MapHexFeaturePlacementDialog(
                isEnglish,
                DungeonCatalog.NaturalFeatures,
                DungeonCatalog.DungeonTypes,
                dungeonLibrary))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null) return;

                HexFeatureRecord feature;
                if (dialog.Options.Mode == MapHexFeaturePlacementMode.Natural)
                {
                    feature = CreateManualNaturalFeature(cell, dialog.Options);
                }
                else
                {
                    feature = CreateManualDungeonFeature(cell, dialog.Options);
                    if (dialog.Options.Mode == MapHexFeaturePlacementMode.GenerateDungeon)
                    {
                        DungeonRecord generated = GenerateDungeonForMapFeature(feature, dialog.Options);
                        AddOrReplaceMapDungeon(generated);
                        feature.DungeonId = generated.Id;
                        feature.Name = generated.Name;
                    }
                    else if (dialog.Options.Mode == MapHexFeaturePlacementMode.LinkDungeon)
                    {
                        DungeonRecord linked = XmlSerialization.Clone(dialog.Options.LibraryDungeon);
                        NormalizeDungeonForEditor(linked);
                        AddOrReplaceMapDungeon(linked);
                        ApplyDungeonToFeature(feature, linked);
                    }
                }

                MapDataNormalizer.NormalizeHexFeature(feature);
                currentMap.Features.Add(feature);
                currentMap.UpdatedAt = DateTime.Now;
                RebuildCurrentMapIndex();
            }
        }

        private HexFeatureRecord CreateManualNaturalFeature(HexCellRecord cell, MapHexFeaturePlacementOptions options)
        {
            HexFeatureDefinition definition = options.NaturalFeature;
            string name = string.IsNullOrWhiteSpace(options.Name)
                ? DungeonCatalog.LocalizeFeatureSubtype(definition.Subtype, !isEnglish)
                : options.Name;

            return new HexFeatureRecord
            {
                Name = name,
                Kind = "Natural",
                Subtype = definition.Subtype,
                Q = cell.Q,
                R = cell.R,
                IconKey = definition.IconKey,
                Description = definition.Description,
                Severity = definition.Severity,
                UpdatedAt = DateTime.Now
            };
        }

        private HexFeatureRecord CreateManualDungeonFeature(HexCellRecord cell, MapHexFeaturePlacementOptions options)
        {
            string dungeonType = options.DungeonType == null
                ? DungeonCatalog.NormalizeDungeonType("")
                : DungeonCatalog.NormalizeDungeonType(options.DungeonType.Name);
            string name = string.IsNullOrWhiteSpace(options.Name)
                ? DungeonCatalog.LocalizeDungeonType(dungeonType, !isEnglish)
                : options.Name;

            return new HexFeatureRecord
            {
                Name = name,
                Kind = "Dungeon",
                Subtype = dungeonType,
                Q = cell.Q,
                R = cell.R,
                IconKey = DungeonCatalog.DungeonTypeIconKey(dungeonType),
                Description = "",
                Severity = "High",
                DungeonType = dungeonType,
                DungeonLevel = DungeonCatalog.ClampDungeonLevel(options.DungeonLevel),
                DungeonSize = string.IsNullOrWhiteSpace(options.DungeonSize) ? "Standard" : options.DungeonSize,
                UpdatedAt = DateTime.Now
            };
        }

        private DungeonRecord GenerateDungeonForMapFeature(HexFeatureRecord feature, MapHexFeaturePlacementOptions options)
        {
            DungeonRecord dungeon = new DungeonGenerator().Generate(new DungeonGenerationOptions
            {
                Name = feature.Name,
                DungeonType = feature.DungeonType,
                Size = feature.DungeonSize,
                RecommendedLevel = DungeonCatalog.ClampDungeonLevel(feature.DungeonLevel),
                Seed = NextDungeonSeedValue().ToString(CultureInfo.InvariantCulture),
                RussianOutput = !isEnglish
            });
            dungeon.Id = Guid.NewGuid().ToString("N");
            NormalizeDungeonForEditor(dungeon);
            return dungeon;
        }

        private void ApplyDungeonToFeature(HexFeatureRecord feature, DungeonRecord dungeon)
        {
            if (feature == null || dungeon == null) return;

            feature.DungeonId = dungeon.Id;
            feature.DungeonType = DungeonCatalog.NormalizeDungeonType(dungeon.DungeonType);
            feature.Subtype = feature.DungeonType;
            feature.DungeonSize = string.IsNullOrWhiteSpace(dungeon.Size) ? "Standard" : dungeon.Size;
            feature.DungeonLevel = DungeonCatalog.ClampDungeonLevel(dungeon.RecommendedLevel);
            feature.Name = string.IsNullOrWhiteSpace(dungeon.Name) ? feature.Name : dungeon.Name;
            feature.IconKey = DungeonCatalog.DungeonTypeIconKey(feature.DungeonType);
            feature.UpdatedAt = DateTime.Now;
        }

        private void AddOrReplaceMapDungeon(DungeonRecord dungeon)
        {
            if (currentMap == null || dungeon == null) return;
            if (currentMap.Dungeons == null) currentMap.Dungeons = new List<DungeonRecord>();

            currentMap.Dungeons.RemoveAll(d => d != null && string.Equals(d.Id, dungeon.Id, StringComparison.OrdinalIgnoreCase));
            currentMap.Dungeons.Add(XmlSerialization.Clone(dungeon));
        }

        private void EraseMapCell(HexCellRecord cell)
        {
            bool changedDomains = false;
            bool changedIndexes = false;
            bool changedFeatureLabels = false;
            bool eraseAll = chkMapEraseAll == null || chkMapEraseAll.Checked;
            bool eraseSettlements = eraseAll || (chkMapEraseSettlements != null && chkMapEraseSettlements.Checked);
            bool eraseRoads = eraseAll || (chkMapEraseRoads != null && chkMapEraseRoads.Checked);
            bool eraseRivers = eraseAll || (chkMapEraseRivers != null && chkMapEraseRivers.Checked);
            bool eraseStrongholds = eraseAll || (chkMapEraseStrongholds != null && chkMapEraseStrongholds.Checked);
            bool eraseDomains = eraseAll || (chkMapEraseDomains != null && chkMapEraseDomains.Checked);
            bool eraseTerrain = eraseAll || (chkMapEraseTerrain != null && chkMapEraseTerrain.Checked);
            bool eraseFeatures = eraseAll || (chkMapEraseFeatures != null && chkMapEraseFeatures.Checked);
            bool eraseNames = eraseAll || (chkMapEraseNames != null && chkMapEraseNames.Checked);

            if (eraseSettlements)
            {
                changedIndexes |= RemoveSettlementsAtCell(cell);
            }

            if (eraseRoads)
            {
                changedIndexes |= currentMap.Roads.RemoveAll(e => EdgeTouchesCell(e, cell)) > 0;
            }

            if (eraseRivers)
            {
                changedIndexes |= currentMap.Rivers.RemoveAll(e => EdgeTouchesCell(e, cell)) > 0;
            }

            if (eraseDomains)
            {
                changedDomains = RemoveCellFromAllDomains(cell, true);
            }
            else if (eraseStrongholds)
            {
                changedDomains = ClearStrongholdsAtCell(cell);
            }

            if (eraseFeatures)
            {
                changedIndexes |= RemoveHexFeaturesAtCell(cell);
            }

            changedIndexes |= changedDomains;

            if (eraseTerrain)
            {
                cell.Terrain = "Grasslands";
                cell.Elevation = "Plains";
                cell.Water = "None";
                NormalizeWaterSurface(cell);
                changedFeatureLabels = true;
            }

            if (eraseNames)
            {
                ClearMapFeatureNamesAtCell(cell);
                changedFeatureLabels = true;
            }

            if (changedDomains)
            {
                RefreshMapDomainList();
            }

            if (changedIndexes)
            {
                RebuildCurrentMapIndex();
            }
            else if (changedFeatureLabels)
            {
                RebuildMapFeatureLabelIndex();
            }
        }

        private bool ClearStrongholdsAtCell(HexCellRecord cell)
        {
            if (currentMap == null || currentMap.Domains == null || cell == null) return false;

            bool changed = false;
            foreach (DomainRecord domain in currentMap.Domains.Where(d => d != null && d.StrongholdQ == cell.Q && d.StrongholdR == cell.R))
            {
                domain.StrongholdId = "";
                domain.StrongholdName = "";
                domain.StrongholdValueGp = 0;
                domain.StrongholdQ = -1;
                domain.StrongholdR = -1;
                domain.StrongholdIconKey = "";
                domain.StrongholdInSettlement = false;
                domain.StrongholdSettlementId = "";
                domain.StrongholdActsAsMarketClassVI = false;
                domain.StrongholdSecuresDomain = false;
                domain.StrongholdIsUnderground = false;
                domain.StrongholdNaturalMajesty = false;
                changed = true;
            }

            return changed;
        }

        private bool RemoveHexFeaturesAtCell(HexCellRecord cell)
        {
            if (currentMap == null || currentMap.Features == null || cell == null) return false;

            // Если стерта метка данжа, удаляем и сам DungeonRecord, но только когда
            // на него больше не ссылается другая особенность карты.
            List<string> removedDungeonIds = currentMap.Features
                .Where(f => f != null && f.Q == cell.Q && f.R == cell.R && !string.IsNullOrWhiteSpace(f.DungeonId))
                .Select(f => f.DungeonId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            bool changed = currentMap.Features.RemoveAll(f => f != null && f.Q == cell.Q && f.R == cell.R) > 0;
            if (changed && removedDungeonIds.Count > 0 && currentMap.Dungeons != null)
            {
                foreach (string dungeonId in removedDungeonIds)
                {
                    bool stillReferenced = currentMap.Features.Any(f => f != null
                        && string.Equals(f.DungeonId, dungeonId, StringComparison.OrdinalIgnoreCase));
                    if (!stillReferenced)
                    {
                        currentMap.Dungeons.RemoveAll(d => d != null
                            && string.Equals(d.Id, dungeonId, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            return changed;
        }

        private void ClearMapFeatureNamesAtCell(HexCellRecord cell)
        {
            if (currentMap == null || cell == null) return;
            if (!string.IsNullOrWhiteSpace(cell.WaterFeatureName))
            {
                string name = cell.WaterFeatureName;
                foreach (HexCellRecord waterCell in currentMap.Cells.Where(c => c.WaterFeatureName == name))
                {
                    waterCell.WaterFeatureName = "";
                }
            }

            List<string> riverNames = currentMap.Rivers
                .Where(e => e != null && EdgeTouchesCell(e, cell) && !string.IsNullOrWhiteSpace(e.FeatureName))
                .Select(e => e.FeatureName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (MapEdgeRecord river in currentMap.Rivers.Where(e => e != null && riverNames.Contains(e.FeatureName)))
            {
                river.FeatureName = "";
            }
        }

        private bool RemoveSettlementsAtCell(HexCellRecord cell)
        {
            if (currentMap == null || cell == null) return false;
            List<string> removedIds = currentMap.Settlements
                .Where(s => s.Q == cell.Q && s.R == cell.R)
                .Select(s => s.Id)
                .ToList();
            if (removedIds.Count == 0) return false;

            currentMap.Settlements.RemoveAll(s => removedIds.Contains(s.Id));
            foreach (DomainRecord domain in currentMap.Domains)
            {
                if (removedIds.Contains(domain.CapitalSettlementId))
                {
                    domain.CapitalSettlementId = "";
                }

                if (domain.SettlementIds != null)
                {
                    domain.SettlementIds.RemoveAll(id => removedIds.Contains(id));
                }

                if (removedIds.Contains(domain.StrongholdSettlementId))
                {
                    domain.StrongholdInSettlement = false;
                    domain.StrongholdSettlementId = "";
                }
            }

            return true;
        }

        private bool EdgeTouchesCell(MapEdgeRecord edge, HexCellRecord cell)
        {
            return (edge.AQ == cell.Q && edge.AR == cell.R) || (edge.BQ == cell.Q && edge.BR == cell.R);
        }

        private void CalculateMapTradeRoutes()
        {
            if (currentMap == null || currentMap.Settlements.Count < 2)
            {
                MessageBox.Show(isEnglish ? "Place at least two settlements." : "Разместите хотя бы два поселения.");
                return;
            }

            foreach (MapSettlementRecord settlement in currentMap.Settlements)
            {
                EnsureDemandArrays(settlement);
                settlement.CurrentDemands = (double[])settlement.BaseDemands.Clone();
            }

            HashSet<string> processedPairs = new HashSet<string>();
            int considered = 0;
            int applied = 0;

            var orderedGroups = currentMap.Settlements
                .GroupBy(s => s.MarketClass)
                .OrderBy(g => g.Key);

            foreach (var group in orderedGroups)
            {
                List<MapSettlementRecord> randomized = group.OrderBy(s => mapRandom.Next()).ToList();
                foreach (MapSettlementRecord source in randomized)
                {
                    List<MapPathResult> paths = FindReachableSettlements(source)
                        .GroupBy(p => p.Target.Id)
                        .Select(g => SelectBestValidTradePath(source, g))
                        .Where(p => p != null)
                        .ToList();

                    foreach (MapPathResult path in paths
                        .OrderBy(p => p.Target.MarketClass)
                        .ThenBy(p => p.DistanceHexes)
                        .ThenBy(p => mapRandom.Next()))
                    {
                        string pairKey = SettlementPairKey(source, path.Target);
                        if (processedPairs.Contains(pairKey)) continue;

                        considered++;
                        bool isRoad = path.HasRoad;
                        double distanceMiles = path.DistanceHexes * MapScaleMiles;
                        if (ApplyTradeInfluence(source, path.Target, isRoad, distanceMiles))
                        {
                            processedPairs.Add(pairKey);
                            applied++;
                        }
                    }
                }
            }

            lblMapInfo.Text = isEnglish
                ? "Trade checked: " + considered + " links; applied: " + applied + "."
                : "Торговля проверена: " + considered + " связей; применено: " + applied + ".";
            pnlHexMap.Invalidate();
        }

        private List<MapPathResult> FindReachableSettlements(MapSettlementRecord source)
        {
            List<MapPathResult> result = new List<MapPathResult>();
            if (source == null) return result;

            Dictionary<string, List<Tuple<string, string>>> graph = BuildRouteGraph();
            string start = CellKey(source.Q, source.R);
            if (!graph.ContainsKey(start)) return result;

            Dictionary<string, MapSettlementRecord> settlementsByCell = currentMap.Settlements
                .Where(s => s.Id != source.Id)
                .GroupBy(s => CellKey(s.Q, s.R))
                .ToDictionary(g => g.Key, g => g.First());

            Queue<Tuple<string, int, bool>> queue = new Queue<Tuple<string, int, bool>>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(Tuple.Create(start, 0, false));
            visited.Add(start + "|False");

            while (queue.Count > 0)
            {
                Tuple<string, int, bool> node = queue.Dequeue();
                if (node.Item2 > 0 && settlementsByCell.ContainsKey(node.Item1))
                {
                    result.Add(new MapPathResult
                    {
                        Target = settlementsByCell[node.Item1],
                        DistanceHexes = node.Item2,
                        HasRoad = node.Item3
                    });
                    continue;
                }

                if (!graph.ContainsKey(node.Item1)) continue;
                foreach (Tuple<string, string> edge in graph[node.Item1])
                {
                    bool nextHasRoad = node.Item3 || edge.Item2 == "Road";
                    string visitedKey = edge.Item1 + "|" + nextHasRoad;
                    if (visited.Contains(visitedKey)) continue;
                    visited.Add(visitedKey);
                    queue.Enqueue(Tuple.Create(edge.Item1, node.Item2 + 1, nextHasRoad));
                }
            }

            return result
                .GroupBy(r => r.Target.Id + "|" + r.HasRoad)
                .Select(g => g.OrderBy(r => r.DistanceHexes).ThenBy(r => r.HasRoad ? 1 : 0).First())
                .ToList();
        }

        private MapPathResult SelectBestValidTradePath(MapSettlementRecord source, IEnumerable<MapPathResult> candidates)
        {
            return candidates
                .Where(p => IsTradeRouteInRange(source, p.Target, p.HasRoad, p.DistanceHexes * MapScaleMiles))
                .OrderBy(p => p.DistanceHexes)
                .ThenBy(p => p.HasRoad ? 1 : 0)
                .ThenBy(p => mapRandom.Next())
                .FirstOrDefault();
        }

        private Dictionary<string, List<Tuple<string, string>>> BuildRouteGraph()
        {
            Dictionary<string, List<Tuple<string, string>>> graph = new Dictionary<string, List<Tuple<string, string>>>();
            AddEdgesToGraph(graph, currentMap.Roads, "Road");
            AddEdgesToGraph(graph, currentMap.Rivers, "River");
            return graph;
        }

        private void AddEdgesToGraph(Dictionary<string, List<Tuple<string, string>>> graph, List<MapEdgeRecord> edges, string kind)
        {
            foreach (MapEdgeRecord edge in edges)
            {
                string a = CellKey(edge.AQ, edge.AR);
                string b = CellKey(edge.BQ, edge.BR);
                if (!graph.ContainsKey(a)) graph[a] = new List<Tuple<string, string>>();
                if (!graph.ContainsKey(b)) graph[b] = new List<Tuple<string, string>>();
                graph[a].Add(Tuple.Create(b, kind));
                graph[b].Add(Tuple.Create(a, kind));
            }
        }

        private bool ApplyTradeInfluence(MapSettlementRecord a, MapSettlementRecord b, bool isRoad, double distance)
        {
            if (!IsTradeRouteInRange(a, b, isRoad, distance)) return false;

            EnsureDemandArrays(a);
            EnsureDemandArrays(b);
            AcksRules.ApplyTradeInfluence(a.MarketClass, b.MarketClass, a.CurrentDemands, b.CurrentDemands);
            return true;
        }

        private bool IsTradeRouteInRange(MapSettlementRecord a, MapSettlementRecord b, bool isRoad, double distance)
        {
            if (a == null || b == null) return false;
            return AcksRules.IsTradeRouteInRange(a.MarketClass, b.MarketClass, isRoad, distance);
        }

        private double[] BuildCellAdjustedDemands(double[] baseDemands, HexCellRecord cell)
        {
            bool hasRiver = currentMap != null && currentMap.Rivers != null && cell != null && currentMap.Rivers.Any(e => EdgeTouchesCell(e, cell));
            return MapDemandService.BuildCellAdjustedDemands(baseDemands, cell, GetNeighborCells(cell), hasRiver);
        }

        private IEnumerable<string> GetWaterInfluencesForCell(HexCellRecord cell)
        {
            bool hasRiver = currentMap != null && currentMap.Rivers != null && cell != null && currentMap.Rivers.Any(e => EdgeTouchesCell(e, cell));
            return MapDemandService.GetWaterInfluences(GetNeighborCells(cell), hasRiver);
        }

        private IEnumerable<HexCellRecord> GetNeighborCells(HexCellRecord cell)
        {
            if (currentMap == null || cell == null) yield break;

            int[][] dirs = (cell.R & 1) == 1
                ? new[]
                {
                    new[] { 1, 0 },
                    new[] { -1, 0 },
                    new[] { 1, -1 },
                    new[] { 0, -1 },
                    new[] { 1, 1 },
                    new[] { 0, 1 }
                }
                : new[]
                {
                    new[] { 1, 0 },
                    new[] { -1, 0 },
                    new[] { 0, -1 },
                    new[] { -1, -1 },
                    new[] { 0, 1 },
                    new[] { -1, 1 }
                };

            foreach (int[] dir in dirs)
            {
                HexCellRecord neighbor = GetCell(cell.Q + dir[0], cell.R + dir[1]);
                if (neighbor != null) yield return neighbor;
            }
        }

        private double[] GetMapWaterAdjustment(string water)
        {
            return MapDemandService.GetWaterAdjustment(water);
        }

        private double[] GetMapTerrainAdjustment(string terrain)
        {
            return MapDemandService.GetTerrainAdjustment(terrain);
        }

        private double[] GetMapElevationAdjustment(string elevation)
        {
            return MapDemandService.GetElevationAdjustment(elevation);
        }

        private void AddDemandAdjustment(double[] target, double[] adjustment)
        {
            MapDemandService.AddDemandAdjustment(target, adjustment);
        }

        private void EnsureDemandArrays(MapSettlementRecord settlement)
        {
            if (settlement == null) return;
            settlement.BaseDemands = MapDemandService.NormalizeDemandArray(settlement.BaseDemands);
            settlement.CurrentDemands = MapDemandService.NormalizeDemandArray(settlement.CurrentDemands);
        }

        private void ShowMapSettlementInfo(MapSettlementRecord settlement)
        {
            if (settlement == null) return;
            EnsureDemandArrays(settlement);

            DomainRecord domain = GetDomainAtCell(new HexCellRecord { Q = settlement.Q, R = settlement.R });
            RealmRecord realm = null;
            if (domain != null && currentMap != null && currentMap.Realms != null)
            {
                realm = currentMap.Realms.FirstOrDefault(r => r.Id == domain.RealmId);
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine((isEnglish ? "Settlement: " : "Поселение: ") + settlement.DisplayName);
            text.AppendLine((isEnglish ? "Market class: " : "Класс рынка: ") + AcksRules.ToRoman(settlement.MarketClass));
            text.AppendLine((isEnglish ? "Race: " : "Раса: ") + LocalizedSettlementRace(settlement.Race));
            text.AppendLine((isEnglish ? "Coordinates: " : "Координаты: ") + "Q " + settlement.Q + ", R " + settlement.R);
            text.AppendLine((isEnglish ? "Land value: " : "Ценность земли: ") + FormatSettlementLandValue(settlement, domain));
            if (domain != null) text.AppendLine((isEnglish ? "Domain: " : "Домен: ") + domain.DisplayName);
            if (realm != null) text.AppendLine((isEnglish ? "Realm: " : "Государство: ") + LocalizedRealmDisplayName(realm));
            using (Form dialog = new Form())
            using (Panel content = new Panel())
            using (Label demandsLabel = new Label())
            using (TextBox summary = new TextBox())
            using (DataGridView demandsGrid = new DataGridView())
            using (Button close = new Button())
            {
                dialog.Text = isEnglish ? "Settlement info" : "Информация о поселении";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.Size = new Size(640, 640);

                content.Dock = DockStyle.Fill;
                content.Padding = new Padding(10);
                content.BackColor = Color.FromArgb(43, 43, 43);

                summary.Multiline = true;
                summary.ReadOnly = true;
                summary.ScrollBars = ScrollBars.Vertical;
                summary.WordWrap = true;
                summary.Dock = DockStyle.Top;
                summary.Height = 118;
                summary.Text = text.ToString();
                summary.BackColor = Color.FromArgb(48, 48, 48);
                summary.ForeColor = Color.White;
                summary.BorderStyle = BorderStyle.FixedSingle;

                demandsLabel.Text = isEnglish ? "Demands" : "Demands";
                demandsLabel.Dock = DockStyle.Top;
                demandsLabel.Height = 28;
                demandsLabel.TextAlign = ContentAlignment.MiddleLeft;
                demandsLabel.ForeColor = Color.White;
                demandsLabel.Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold);

                ConfigureSettlementInfoDemandGrid(demandsGrid, settlement);
                demandsGrid.Dock = DockStyle.Fill;

                close.Text = isEnglish ? "Close" : "Закрыть";
                close.Dock = DockStyle.Bottom;
                close.Height = 34;
                close.DialogResult = DialogResult.OK;
                UiTheme.StyleCommandButton(close, UiTheme.PositiveButtonColor);

                content.Controls.Add(demandsGrid);
                content.Controls.Add(demandsLabel);
                content.Controls.Add(summary);
                dialog.Controls.Add(content);
                dialog.Controls.Add(close);
                dialog.AcceptButton = close;
                UiTheme.ApplyUniformFonts(dialog);
                UiTheme.ApplyThemeColors(dialog);
                dialog.ShowDialog(this);
            }
        }

        private string FormatSettlementLandValue(MapSettlementRecord settlement, DomainRecord domain)
        {
            string metadata = settlement == null ? "" : settlement.LandValue;
            int gp;
            bool clanhold = HasSettlementMetadataToken(metadata, SettlementMetadataClanhold);
            bool hasLandValue = TryParseSettlementLandValueGp(metadata, out gp);
            if (!hasLandValue && domain != null)
            {
                DomainHexRecord domainHex = domain.Hexes == null || settlement == null
                    ? null
                    : domain.Hexes.FirstOrDefault(h => h != null && h.Q == settlement.Q && h.R == settlement.R);
                gp = AcksDomainRules.ClampLandValue(domainHex == null ? domain.FixedLandValueGp : domainHex.LandValueGp);
                hasLandValue = true;
            }

            string landValueText = hasLandValue ? gp.ToString(CultureInfo.InvariantCulture) + "gp" : "-";
            if (clanhold)
            {
                return (isEnglish ? "Clanhold, " : "Клановое, ") + landValueText;
            }

            if (hasLandValue) return landValueText;
            if (string.IsNullOrWhiteSpace(metadata) || metadata.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return "-";
            return metadata;
        }

        private void ShowMapStrongholdInfo(DomainRecord domain)
        {
            if (domain == null) return;

            RealmRecord realm = GetRealmForDomain(domain);
            DomainFinancialSummary financials = AcksDomainRules.CalculateFinancials(domain);
            string strongholdName = GetStrongholdDisplayName(domain);
            string placement = GetSettlementAtStronghold(domain) != null
                ? (isEnglish ? "inside settlement" : "в поселении")
                : (isEnglish ? "separate stronghold" : "отдельная крепость");

            StringBuilder text = new StringBuilder();
            text.AppendLine((isEnglish ? "Stronghold: " : "Крепость: ") + strongholdName);
            text.AppendLine((isEnglish ? "Type: " : "Тип: ") + (string.IsNullOrWhiteSpace(domain.StrongholdType) ? "Fortress" : domain.StrongholdType));
            text.AppendLine((isEnglish ? "Race: " : "Раса: ") + LocalizedSettlementRace(domain.Race));
            text.AppendLine((isEnglish ? "Coordinates: " : "Координаты: ") + "Q " + domain.StrongholdQ + ", R " + domain.StrongholdR);
            text.AppendLine((isEnglish ? "Placement: " : "Размещение: ") + placement);
            text.AppendLine((isEnglish ? "Value: " : "Стоимость: ")
                + FormatGp(domain.StrongholdValueGp)
                + " / "
                + FormatGp(financials.RequiredStrongholdValue)
                + (isEnglish ? " required" : " требуется"));
            text.AppendLine((isEnglish ? "Domain: " : "Домен: ") + domain.DisplayName);
            text.AppendLine((isEnglish ? "Realm: " : "Государство: ") + (realm == null ? (isEnglish ? "None" : "Нет") : LocalizedRealmDisplayName(realm)));
            text.AppendLine((isEnglish ? "Domain ruler: " : "Правитель домена: ") + GetDomainRulerDisplay(domain));
            if (realm != null)
            {
                text.AppendLine((isEnglish ? "Realm ruler: " : "Правитель государства: ")
                    + (string.IsNullOrWhiteSpace(realm.RulerName) ? (isEnglish ? "None" : "Нет") : realm.RulerName));
            }
            text.AppendLine((isEnglish ? "Acts as Class VI base: " : "Считается базой VI класса: ")
                + YesNo(domain.StrongholdActsAsMarketClassVI));
            text.AppendLine((isEnglish ? "Secures domain: " : "Защищает домен: ")
                + YesNo(domain.StrongholdSecuresDomain));
            if (domain.StrongholdIsUnderground)
            {
                text.AppendLine(isEnglish ? "Underground stronghold: yes" : "Подземная крепость: да");
            }
            if (domain.StrongholdNaturalMajesty)
            {
                text.AppendLine(isEnglish ? "Natural majesty site: yes" : "Место природного величия: да");
            }

            using (Form dialog = new Form())
            using (TextBox summary = new TextBox())
            using (Button close = new Button())
            {
                dialog.Text = isEnglish ? "Stronghold info" : "Информация о крепости";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.Size = new Size(520, 430);

                summary.Multiline = true;
                summary.ReadOnly = true;
                summary.ScrollBars = ScrollBars.Vertical;
                summary.WordWrap = true;
                summary.Dock = DockStyle.Fill;
                summary.Text = text.ToString();
                summary.BackColor = Color.FromArgb(48, 48, 48);
                summary.ForeColor = Color.White;
                summary.BorderStyle = BorderStyle.FixedSingle;

                close.Text = isEnglish ? "Close" : "Закрыть";
                close.Dock = DockStyle.Bottom;
                close.Height = 34;
                close.DialogResult = DialogResult.OK;
                UiTheme.StyleCommandButton(close, UiTheme.PositiveButtonColor);

                dialog.Controls.Add(summary);
                dialog.Controls.Add(close);
                dialog.AcceptButton = close;
                dialog.Shown += (s, e) => close.Focus();
                UiTheme.ApplyUniformFonts(dialog);
                UiTheme.ApplyThemeColors(dialog);
                dialog.ShowDialog(this);
            }
        }

        private RealmRecord GetRealmForDomain(DomainRecord domain)
        {
            if (domain == null || currentMap == null || currentMap.Realms == null) return null;
            if (string.IsNullOrWhiteSpace(domain.RealmId)) return null;
            return currentMap.Realms.FirstOrDefault(r => string.Equals(r.Id, domain.RealmId, StringComparison.OrdinalIgnoreCase));
        }

        private string GetDomainRulerDisplay(DomainRecord domain)
        {
            if (domain == null || domain.Ruler == null) return isEnglish ? "None" : "Нет";
            if (domain.Ruler.Snapshot == null || string.IsNullOrWhiteSpace(domain.Ruler.Snapshot.Name)) return isEnglish ? "None" : "Нет";
            return domain.Ruler.DisplayName;
        }

        private string FormatGp(int value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture) + " gp";
        }

        private string YesNo(bool value)
        {
            return isEnglish ? (value ? "yes" : "no") : (value ? "да" : "нет");
        }

        private void ConfigureSettlementInfoDemandGrid(DataGridView grid, MapSettlementRecord settlement)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.BackgroundColor = Color.FromArgb(48, 48, 48);
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = Color.FromArgb(88, 88, 88);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(32, 32, 32);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(32, 32, 32);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.FromArgb(54, 54, 54);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 0);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(46, 46, 46);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = isEnglish ? "Good" : "Товар",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 160
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = isEnglish ? "Base" : "Базовые",
                Width = 84,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = isEnglish ? "Current" : "Текущие",
                Width = 84,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            for (int i = 0; i < MerchandiseNames.Length; i++)
            {
                grid.Rows.Add(
                    GetMerchandiseDisplayName(i),
                    settlement.BaseDemands[i].ToString("0.0", CultureInfo.InvariantCulture),
                    settlement.CurrentDemands[i].ToString("0.0", CultureInfo.InvariantCulture));
            }
        }

        private double[] NormalizeDemandArray(double[] source)
        {
            return MapDemandService.NormalizeDemandArray(source);
        }

        private void UpdateMapInfoForSelection()
        {
            if (lblMapInfo == null) return;

            if (selectedMapCell == null)
            {
                lblMapInfo.Text = isEnglish
                    ? "Choose a tool and click hexes. Roads/rivers use two clicks."
                    : "Выберите инструмент и кликайте по гексам. Дорога/река ставятся двумя кликами.";
                return;
            }

            MapSettlementRecord settlement = currentMap == null
                ? null
                : currentMap.Settlements.FirstOrDefault(s => s.Q == selectedMapCell.Q && s.R == selectedMapCell.R);
            bool waterCell = IsWaterCell(selectedMapCell);
            string terrainText = waterCell ? selectedMapCell.Water : selectedMapCell.Terrain;
            string elevationText = waterCell ? (isEnglish ? "Water" : "Вода") : selectedMapCell.Elevation;
            string info = "Q " + selectedMapCell.Q + ", R " + selectedMapCell.R
                + "\r\n" + (isEnglish ? "Terrain: " : "Местность: ") + selectedMapCell.Terrain
                + "\r\n" + (isEnglish ? "Elevation: " : "Высотность: ") + selectedMapCell.Elevation
                + "\r\n" + (isEnglish ? "Water: " : "Вода: ") + selectedMapCell.Water;
            if (waterCell)
            {
                info = "Q " + selectedMapCell.Q + ", R " + selectedMapCell.R
                    + "\r\n" + (isEnglish ? "Terrain: " : "Местность: ") + terrainText
                    + "\r\n" + (isEnglish ? "Elevation: " : "Высотность: ") + elevationText
                    + "\r\n" + (isEnglish ? "Water: " : "Вода: ") + selectedMapCell.Water;
            }

            DomainRecord domainAtCell = GetDomainAtCell(selectedMapCell);
            bool selectedHexHasRoad = currentMap != null && currentMap.Roads.Any(e => EdgeTouchesCell(e, selectedMapCell));
            bool selectedHexHasRiver = currentMap != null && currentMap.Rivers.Any(e => EdgeTouchesCell(e, selectedMapCell));

            if (selectedHexHasRoad)
            {
                info += "\r\n" + (isEnglish ? "Road: yes" : "\u0414\u043e\u0440\u043e\u0433\u0430: \u0435\u0441\u0442\u044c");
            }
            if (selectedHexHasRiver)
            {
                info += "\r\n" + (isEnglish ? "River: yes" : "\u0420\u0435\u043a\u0430: \u0435\u0441\u0442\u044c");
            }
            List<string> wildernessLines = WildernessHexRules.BuildDisplayLines(
                selectedMapCell,
                selectedHexHasRoad,
                selectedHexHasRiver,
                domainAtCell == null ? null : domainAtCell.Classification,
                isEnglish);
            if (wildernessLines.Count > 0)
            {
                info += "\r\n" + string.Join("\r\n", wildernessLines);
            }
            List<HexFeatureRecord> features = GetHexFeaturesAtCell(selectedMapCell);
            if (features.Count > 0)
            {
                info += "\r\n" + (isEnglish ? "Features: " : "Особенности: ")
                    + string.Join(", ", features.Select(GetHexFeatureDisplayText));
            }
            if (settlement != null)
            {
                info += "\r\n" + (isEnglish ? "Settlement: " : "\u041f\u043e\u0441\u0435\u043b\u0435\u043d\u0438\u0435: ")
                    + settlement.DisplayName;
            }
            DomainRecord strongholdAtCell = currentMap == null || currentMap.Domains == null
                ? null
                : currentMap.Domains.FirstOrDefault(d => d.StrongholdQ == selectedMapCell.Q && d.StrongholdR == selectedMapCell.R);
            if (strongholdAtCell != null)
            {
                string strongholdName = GetStrongholdDisplayName(strongholdAtCell);
                string placement = GetSettlementAtStronghold(strongholdAtCell) != null
                    ? (isEnglish ? "in settlement" : "\u0432 \u043f\u043e\u0441\u0435\u043b\u0435\u043d\u0438\u0438")
                    : (isEnglish ? "separate" : "\u043e\u0442\u0434\u0435\u043b\u044c\u043d\u043e");
                info += "\r\n" + (isEnglish ? "Stronghold: " : "\u041a\u0440\u0435\u043f\u043e\u0441\u0442\u044c: ")
                    + strongholdName + " (" + placement + ", Class VI)";
            }
            if (domainAtCell != null)
            {
                info += "\r\n" + (isEnglish ? "Domain: " : "\u0414\u043e\u043c\u0435\u043d: ")
                    + domainAtCell.DisplayName;
            }
            if (pendingMapEdgeStart != null)
            {
                info += "\r\n" + (isEnglish ? "Start selected: " : "Начало выбрано: ")
                    + pendingMapEdgeStart.Q + "," + pendingMapEdgeStart.R;
            }
            lblMapInfo.Text = info;
        }

        private List<HexFeatureRecord> GetHexFeaturesAtCell(HexCellRecord cell)
        {
            if (cell == null || currentMap == null) return new List<HexFeatureRecord>();
            List<HexFeatureRecord> features;
            if (currentMapFeaturesByCell != null
                && currentMapFeaturesByCell.TryGetValue(CellKey(cell.Q, cell.R), out features))
            {
                return features.Where(f => f != null).ToList();
            }

            return currentMap.Features == null
                ? new List<HexFeatureRecord>()
                : currentMap.Features.Where(f => f != null && f.Q == cell.Q && f.R == cell.R).ToList();
        }

        private string GetHexFeatureDisplayText(HexFeatureRecord feature)
        {
            if (feature == null) return "";
            string name = string.IsNullOrWhiteSpace(feature.Name)
                ? (isEnglish ? "Feature" : "Особенность")
                : feature.Name;
            string subtype = string.IsNullOrWhiteSpace(feature.Subtype) ? feature.Kind : feature.Subtype;
            if (!isEnglish)
            {
                subtype = string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase)
                    ? DungeonCatalog.LocalizeDungeonType(subtype, true)
                    : DungeonCatalog.LocalizeFeatureSubtype(subtype, true);
            }

            return string.IsNullOrWhiteSpace(subtype) ? name : name + " [" + subtype + "]";
        }

        private string GetSelectedTerrainKey()
        {
            int index = cmbMapTerrain.SelectedIndex;
            return index >= 0 && index < mapTerrainKeys.Length ? mapTerrainKeys[index] : "Grasslands";
        }

        private string GetSelectedElevationKey()
        {
            int index = cmbMapElevation.SelectedIndex;
            return index >= 0 && index < mapElevationKeys.Length ? mapElevationKeys[index] : "Plains";
        }

        private string GetSelectedWaterKey()
        {
            int index = cmbMapWater.SelectedIndex;
            return index >= 0 && index < mapWaterKeys.Length ? mapWaterKeys[index] : "None";
        }

        private bool IsWaterCell(HexCellRecord cell)
        {
            return cell != null
                && (string.Equals(cell.Water, "Ocean", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cell.Water, "Sea", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cell.Water, "Lake", StringComparison.OrdinalIgnoreCase));
        }

        private void ShowHexFeatureInfo(HexFeatureRecord feature)
        {
            if (feature == null) return;

            Form dialog = new Form
            {
                Text = GetHexFeatureDisplayText(feature),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(500, 300)
            };

            Label title = new Label
            {
                Text = GetHexFeatureDisplayText(feature),
                Font = UiTheme.CreateFont(FontStyle.Bold),
                Location = new Point(16, 14),
                Size = new Size(468, 24)
            };
            Label details = new Label
            {
                Location = new Point(16, 48),
                Size = new Size(468, 150),
                AutoEllipsis = true,
                Text = BuildHexFeatureInfoText(feature)
            };
            Button close = new Button
            {
                Text = isEnglish ? "Close" : "Закрыть",
                DialogResult = DialogResult.OK,
                Location = new Point(374, 252),
                Size = new Size(110, 28)
            };
            UiTheme.StyleCommandButton(close, UiTheme.NeutralButtonColor);
            dialog.Controls.Add(title);
            dialog.Controls.Add(details);
            dialog.Controls.Add(close);
            dialog.AcceptButton = close;

            if (string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase))
            {
                Button openDungeon = new Button
                {
                    Text = isEnglish ? "Open dungeon" : "Открыть данж",
                    Location = new Point(16, 210),
                    Size = new Size(150, 30)
                };
                UiTheme.StylePositiveButton(openDungeon);
                openDungeon.Click += (s, e) =>
                {
                    OpenDungeonFromMapFeature(feature);
                    dialog.Close();
                };

                Button linkDungeon = new Button
                {
                    Text = isEnglish ? "Link from library" : "Привязать из библиотеки",
                    Location = new Point(176, 210),
                    Size = new Size(244, 30)
                };
                UiTheme.StyleCommandButton(linkDungeon, UiTheme.PositiveButtonColor);
                linkDungeon.Click += (s, e) =>
                {
                    if (TryLinkDungeonFromLibraryToMapFeature(feature))
                    {
                        RebuildCurrentMapIndex();
                        pnlHexMap.Invalidate();
                        dialog.Close();
                    }
                };

                dialog.Controls.Add(openDungeon);
                dialog.Controls.Add(linkDungeon);
            }

            UiTheme.ApplyUniformFonts(dialog);
            UiTheme.ApplyThemeColors(dialog);
            dialog.ShowDialog(this);
        }

        private string BuildHexFeatureInfoText(HexFeatureRecord feature)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine((isEnglish ? "Hex: " : "Гекс: ") + feature.Q + ", " + feature.R);
            text.AppendLine((isEnglish ? "Kind: " : "Тип: ") + (string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase)
                ? (isEnglish ? "Dungeon" : "Данж")
                : (isEnglish ? "Natural feature" : "Природная особенность")));

            string subtype = string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase)
                ? DungeonCatalog.LocalizeDungeonType(feature.Subtype, !isEnglish)
                : DungeonCatalog.LocalizeFeatureSubtype(feature.Subtype, !isEnglish);
            if (!string.IsNullOrWhiteSpace(subtype)) text.AppendLine((isEnglish ? "Subtype: " : "Подтип: ") + subtype);
            if (string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase))
            {
                text.AppendLine((isEnglish ? "Danger level: " : "Уровень опасности: ") + feature.DungeonLevel);
                text.AppendLine((isEnglish ? "Size: " : "Размер: ") + LocalizeDungeonSize(feature.DungeonSize));
            }

            if (!string.IsNullOrWhiteSpace(feature.Severity))
            {
                text.AppendLine((isEnglish ? "Severity: " : "Опасность: ") + LocalizeFeatureSeverity(feature.Severity));
            }
            string description = LocalizeHexFeatureDescription(feature);
            if (!string.IsNullOrWhiteSpace(description))
            {
                text.AppendLine();
                text.Append(description);
            }

            return text.ToString();
        }

        private string LocalizeDungeonSize(string size)
        {
            if (isEnglish || string.IsNullOrWhiteSpace(size)) return size ?? "";
            if (string.Equals(size, "Lair", StringComparison.OrdinalIgnoreCase)) return "логово";
            if (string.Equals(size, "Small", StringComparison.OrdinalIgnoreCase)) return "малый";
            if (string.Equals(size, "Standard", StringComparison.OrdinalIgnoreCase)) return "стандартный";
            if (string.Equals(size, "Large", StringComparison.OrdinalIgnoreCase)) return "большой";
            if (string.Equals(size, "Megadungeon", StringComparison.OrdinalIgnoreCase)) return "мегаданж";
            return size;
        }

        private string LocalizeFeatureSeverity(string severity)
        {
            if (isEnglish || string.IsNullOrWhiteSpace(severity)) return severity ?? "";
            if (string.Equals(severity, "Low", StringComparison.OrdinalIgnoreCase)) return "низкая";
            if (string.Equals(severity, "Mid", StringComparison.OrdinalIgnoreCase)) return "средняя";
            if (string.Equals(severity, "High", StringComparison.OrdinalIgnoreCase)) return "высокая";
            if (string.Equals(severity, "Hazard", StringComparison.OrdinalIgnoreCase)) return "опасность";
            if (string.Equals(severity, "Wonder", StringComparison.OrdinalIgnoreCase)) return "диковина";
            if (string.Equals(severity, "Mystic", StringComparison.OrdinalIgnoreCase)) return "мистическая";
            return severity;
        }

        private string LocalizeHexFeatureDescription(HexFeatureRecord feature)
        {
            if (feature == null) return "";
            if (string.Equals(feature.Kind, "Dungeon", StringComparison.OrdinalIgnoreCase))
            {
                return isEnglish
                    ? "ACKS dungeon: " + DungeonCatalog.LocalizeDungeonType(feature.DungeonType, false)
                        + ", " + LocalizeDungeonSize(feature.DungeonSize)
                        + ", recommended level " + feature.DungeonLevel + "."
                    : "Данж ACKS: " + DungeonCatalog.LocalizeDungeonType(feature.DungeonType, true)
                        + ", размер " + LocalizeDungeonSize(feature.DungeonSize)
                        + ", рекомендуемый уровень " + feature.DungeonLevel + ".";
            }

            string catalogDescription = DungeonCatalog.LocalizeFeatureDescription(feature.Subtype, !isEnglish);
            return string.IsNullOrWhiteSpace(catalogDescription) ? feature.Description : catalogDescription;
        }

        private DungeonRecord GetMapDungeonForFeature(HexFeatureRecord feature)
        {
            if (feature == null || string.IsNullOrWhiteSpace(feature.DungeonId)) return null;
            DungeonRecord dungeon;
            if (currentMapDungeonsById != null
                && currentMapDungeonsById.TryGetValue(feature.DungeonId, out dungeon))
            {
                return dungeon;
            }

            return currentMap == null || currentMap.Dungeons == null
                ? null
                : currentMap.Dungeons.FirstOrDefault(d => d != null
                    && string.Equals(d.Id, feature.DungeonId, StringComparison.OrdinalIgnoreCase));
        }

        private void OpenDungeonFromMapFeature(HexFeatureRecord feature)
        {
            if (feature == null) return;
            DungeonRecord dungeon = GetMapDungeonForFeature(feature);
            if (dungeon == null)
            {
                dungeon = new DungeonGenerator().Generate(new DungeonGenerationOptions
                {
                    Name = feature.Name,
                    DungeonType = feature.DungeonType,
                    Size = feature.DungeonSize,
                    RecommendedLevel = DungeonCatalog.ClampDungeonLevel(feature.DungeonLevel),
                    RussianOutput = !isEnglish
                });
                dungeon.Id = string.IsNullOrWhiteSpace(feature.DungeonId) ? Guid.NewGuid().ToString("N") : feature.DungeonId;
                feature.DungeonId = dungeon.Id;
                if (currentMap != null)
                {
                    if (currentMap.Dungeons == null) currentMap.Dungeons = new List<DungeonRecord>();
                    currentMap.Dungeons.Add(dungeon);
                    RebuildCurrentMapIndex();
                }
            }

            OpenDungeonInDungeonTab(dungeon, feature);
        }

        private void NormalizeWaterSurface(HexCellRecord cell)
        {
            if (cell == null) return;

            if (IsWaterCell(cell))
            {
                // Водный гекс сам является типом поверхности: это защищает карту от лесов и гор,
                // оставшихся под океаном после генерации или импорта.
                cell.Terrain = NormalizeWaterKey(cell.Water);
                cell.Elevation = "Water";
                return;
            }

            if (string.Equals(cell.Terrain, "Ocean", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cell.Terrain, "Sea", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cell.Terrain, "Lake", StringComparison.OrdinalIgnoreCase))
            {
                cell.Terrain = "Grasslands";
            }

            if (string.Equals(cell.Elevation, "Water", StringComparison.OrdinalIgnoreCase))
            {
                cell.Elevation = "Plains";
            }
        }

        private string NormalizeTerrainKey(string value)
        {
            return MapDataNormalizer.TerrainKey(value);
        }

        private string NormalizeElevationKey(string value)
        {
            return MapDataNormalizer.ElevationKey(value);
        }

        private string NormalizeWaterKey(string value)
        {
            return MapDataNormalizer.WaterKey(value);
        }

        private HexCellRecord GetCell(int q, int r)
        {
            if (currentMap == null) return null;
            HexCellRecord cell;
            if (currentMapCellIndex != null && currentMapCellIndex.TryGetValue(CellKey(q, r), out cell)) return cell;
            return currentMap.Cells.FirstOrDefault(c => c.Q == q && c.R == r);
        }

        private string CellKey(int q, int r)
        {
            return MapDataNormalizer.CellKey(q, r);
        }

        private int HexDistance(int aq, int ar, int bq, int br)
        {
            int axialAq = aq - ((ar - (ar & 1)) / 2);
            int axialBq = bq - ((br - (br & 1)) / 2);
            int asCoord = -axialAq - ar;
            int bsCoord = -axialBq - br;
            return (Math.Abs(axialAq - axialBq) + Math.Abs(ar - br) + Math.Abs(asCoord - bsCoord)) / 2;
        }

        private string SettlementPairKey(MapSettlementRecord a, MapSettlementRecord b)
        {
            return string.CompareOrdinal(a.Id, b.Id) <= 0 ? a.Id + "|" + b.Id : b.Id + "|" + a.Id;
        }

    }
}
