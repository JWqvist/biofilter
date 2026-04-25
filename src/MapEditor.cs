using System.Collections.Generic;
using BioFilter;
using Godot;

/// <summary>
/// In-game map editor. Lets players design custom grid layouts and save them as
/// JSON to user://user_maps/{name}.json for use in the main game.
/// Sprint 14.
/// </summary>
public partial class MapEditor : Control
{
    private const int GridW   = GameConfig.GridWidth;
    private const int GridH   = GameConfig.GridHeight;
    private const int TS      = GameConfig.TileSize;
    private const int ToolbarH = 40;

    // Center the 512×320 grid horizontally in the 640px viewport
    private const int GridOffX = (640 - GridW * TS) / 2;  // = 64
    private const int GridOffY = ToolbarH;

    private const int MaxSpawns = 4;
    private const int MaxExits  = 4;

    // ── Grid state ───────────────────────────────────────────────────────────
    private readonly TileType[,] _grid = new TileType[GridW, GridH];
    private readonly List<Vector2I> _spawnPoints = new();

    // ── Editor tool ──────────────────────────────────────────────────────────
    private enum EditorTool { Wall, Spawn, Exit, Erase }
    private EditorTool _currentTool = EditorTool.Wall;
    private bool _isDragging = false;

    // ── Animation ────────────────────────────────────────────────────────────
    private float _time = 0f;

    // ── UI nodes ─────────────────────────────────────────────────────────────
    private Label     _statusLabel   = null!;
    private LineEdit  _nameInput     = null!;
    private Button    _wallBtn       = null!;
    private Button    _spawnBtn      = null!;
    private Button    _exitBtn       = null!;
    private Button    _eraseBtn      = null!;
    private Control   _loadPanel     = null!;
    private VBoxContainer _loadFileList = null!;

    private readonly AirflowCalculator _airflowCalc = new();

    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color BgColor      = Constants.Colors.Background;
    private static readonly Color GridLineColor = Constants.Colors.GridLine;
    private static readonly Color ScanlineColor = Constants.Colors.ScanlineAlt;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildToolbar();
        BuildLoadPanel();
        ClearGrid();
        QueueRedraw();
    }

    // ── UI construction ──────────────────────────────────────────────────────

    private void BuildToolbar()
    {
        var bg = new ColorRect();
        bg.LayoutMode = 1;
        bg.AnchorRight = 1f;
        bg.OffsetBottom = ToolbarH;
        bg.Color = new Color("#0a1208");
        bg.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.AnchorRight = 1f;
        vbox.OffsetBottom = ToolbarH;
        vbox.AddThemeConstantOverride("separation", 2);
        AddChild(vbox);

        // ── Row 1: nav + name + file ops ─────────────────────────────────────
        var row1 = new HBoxContainer();
        row1.CustomMinimumSize = new Vector2(0, 18);
        row1.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(row1);

        var backBtn = MakeButton("< BACK");
        backBtn.Pressed += OnBackPressed;
        row1.AddChild(backBtn);

        var sep1 = new Control();
        sep1.CustomMinimumSize = new Vector2(6, 0);
        row1.AddChild(sep1);

        var nameLabel = new Label();
        nameLabel.Text = "Name:";
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        nameLabel.AddThemeColorOverride("font_color", Constants.Colors.TextDim);
        nameLabel.AddThemeFontSizeOverride("font_size", 11);
        row1.AddChild(nameLabel);

        _nameInput = new LineEdit();
        _nameInput.Text = "my_map";
        _nameInput.CustomMinimumSize = new Vector2(120, 0);
        _nameInput.AddThemeColorOverride("font_color", Constants.Colors.TextPrimary);
        _nameInput.AddThemeFontSizeOverride("font_size", 11);
        row1.AddChild(_nameInput);

        var spacer1 = new Control();
        spacer1.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row1.AddChild(spacer1);

        var saveBtn = MakeButton("SAVE");
        saveBtn.Pressed += OnSavePressed;
        row1.AddChild(saveBtn);

        var loadBtn = MakeButton("LOAD");
        loadBtn.Pressed += OnLoadPressed;
        row1.AddChild(loadBtn);

        var clearBtn = MakeButton("CLEAR");
        clearBtn.Pressed += OnClearPressed;
        row1.AddChild(clearBtn);

        // ── Row 2: tool selector + status ────────────────────────────────────
        var row2 = new HBoxContainer();
        row2.CustomMinimumSize = new Vector2(0, 18);
        row2.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(row2);

        _wallBtn = MakeButton("WALL");
        _wallBtn.Pressed += () => SelectTool(EditorTool.Wall);
        row2.AddChild(_wallBtn);

        _spawnBtn = MakeButton("SPAWN");
        _spawnBtn.Pressed += () => SelectTool(EditorTool.Spawn);
        row2.AddChild(_spawnBtn);

        _exitBtn = MakeButton("EXIT");
        _exitBtn.Pressed += () => SelectTool(EditorTool.Exit);
        row2.AddChild(_exitBtn);

        _eraseBtn = MakeButton("ERASE");
        _eraseBtn.Pressed += () => SelectTool(EditorTool.Erase);
        row2.AddChild(_eraseBtn);

        var spacer2 = new Control();
        spacer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row2.AddChild(spacer2);

        _statusLabel = new Label();
        _statusLabel.VerticalAlignment = VerticalAlignment.Center;
        _statusLabel.AddThemeColorOverride("font_color", Constants.Colors.TextDim);
        _statusLabel.AddThemeFontSizeOverride("font_size", 10);
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row2.AddChild(_statusLabel);

        SelectTool(EditorTool.Wall);
    }

    private void BuildLoadPanel()
    {
        // Full-screen dim overlay
        var dimOverlay = new ColorRect();
        dimOverlay.LayoutMode = 1;
        dimOverlay.AnchorRight = 1f;
        dimOverlay.AnchorBottom = 1f;
        dimOverlay.Color = new Color(0f, 0f, 0f, 0.6f);
        dimOverlay.MouseFilter = MouseFilterEnum.Stop;
        dimOverlay.Visible = false;
        AddChild(dimOverlay);

        var panel = new Panel();
        panel.LayoutMode = 1;
        panel.AnchorLeft   = 0.5f;
        panel.AnchorTop    = 0.5f;
        panel.AnchorRight  = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft   = -150;
        panel.OffsetTop    = -130;
        panel.OffsetRight  =  150;
        panel.OffsetBottom =  130;
        panel.Visible = false;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color("#0d1a0d");
        panelStyle.BorderColor = new Color("#2d5a3d");
        panelStyle.SetBorderWidthAll(2);
        panelStyle.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        _loadPanel = panel;
        // Store overlay reference in metadata so we can toggle it together with panel
        _loadPanel.SetMeta("overlay_path", dimOverlay.GetPath());

        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.AnchorRight = 1f;
        vbox.AnchorBottom = 1f;
        vbox.OffsetLeft   = 10;
        vbox.OffsetTop    = 10;
        vbox.OffsetRight  = -10;
        vbox.OffsetBottom = -10;
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "LOAD MAP";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);
        title.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(title);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        _loadFileList = new VBoxContainer();
        _loadFileList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_loadFileList);

        var cancelBtn = MakeButton("CANCEL");
        cancelBtn.Pressed += HideLoadPanel;
        vbox.AddChild(cancelBtn);
    }

    // ── Tool selection ───────────────────────────────────────────────────────

    private void SelectTool(EditorTool tool)
    {
        _currentTool = tool;
        SetToolButtonActive(_wallBtn,  tool == EditorTool.Wall);
        SetToolButtonActive(_spawnBtn, tool == EditorTool.Spawn);
        SetToolButtonActive(_exitBtn,  tool == EditorTool.Exit);
        SetToolButtonActive(_eraseBtn, tool == EditorTool.Erase);

        _statusLabel.AddThemeColorOverride("font_color", Constants.Colors.TextDim);
        _statusLabel.Text = tool switch
        {
            EditorTool.Wall  => "WALL — left-click place, right-click erase",
            EditorTool.Spawn => $"SPAWN — left edge only (max {MaxSpawns})",
            EditorTool.Exit  => $"EXIT — right edge only (max {MaxExits})",
            EditorTool.Erase => "ERASE — click any tile to remove",
            _                => ""
        };
    }

    // ── Button callbacks ─────────────────────────────────────────────────────

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    private void OnClearPressed()
    {
        ClearGrid();
        QueueRedraw();
        SetStatus("Grid cleared.");
    }

    private void OnSavePressed()
    {
        string error = Validate();
        if (error != "")
        {
            SetStatus(error, error: true);
            return;
        }

        string name = SanitizeName(_nameInput.Text);
        if (name == "") name = "my_map";

        DirAccess.MakeDirRecursiveAbsolute("user://user_maps");

        var tilesArray = new Godot.Collections.Array();
        for (int r = 0; r < GridH; r++)
            for (int c = 0; c < GridW; c++)
                tilesArray.Add((int)_grid[c, r]);

        var dict = new Godot.Collections.Dictionary
        {
            ["name"]   = name,
            ["width"]  = GridW,
            ["height"] = GridH,
            ["tiles"]  = tilesArray
        };

        string path = $"user://user_maps/{name}.json";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            SetStatus($"Error: could not write {path}", error: true);
            return;
        }
        file.StoreString(Json.Stringify(dict));

        SetStatus($"Saved: {name}");
    }

    private void OnLoadPressed()
    {
        // Rebuild file list
        foreach (Node child in _loadFileList.GetChildren())
            child.QueueFree();

        bool found = false;
        var dir = DirAccess.Open("user://user_maps");
        if (dir != null)
        {
            dir.ListDirBegin();
            string fname = dir.GetNext();
            while (fname != "")
            {
                if (!dir.CurrentIsDir() && fname.EndsWith(".json"))
                {
                    string mapName = fname.Replace(".json", "");
                    var btn = MakeButton(mapName);
                    string captured = mapName;
                    btn.Pressed += () =>
                    {
                        LoadMap(captured);
                        HideLoadPanel();
                    };
                    _loadFileList.AddChild(btn);
                    found = true;
                }
                fname = dir.GetNext();
            }
        }

        if (!found)
        {
            var lbl = new Label();
            lbl.Text = "No saved maps found.";
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.AddThemeColorOverride("font_color", Constants.Colors.TextDim);
            lbl.AddThemeFontSizeOverride("font_size", 11);
            _loadFileList.AddChild(lbl);
        }

        ShowLoadPanel();
    }

    private void ShowLoadPanel()
    {
        _loadPanel.Visible = true;
        // Also show the dim overlay we stored via meta
        var overlayPath = (NodePath)_loadPanel.GetMeta("overlay_path");
        GetNode<ColorRect>(overlayPath).Visible = true;
    }

    private void HideLoadPanel()
    {
        _loadPanel.Visible = false;
        var overlayPath = (NodePath)_loadPanel.GetMeta("overlay_path");
        GetNode<ColorRect>(overlayPath).Visible = false;
    }

    // ── Grid state helpers ───────────────────────────────────────────────────

    private void ClearGrid()
    {
        _spawnPoints.Clear();
        for (int c = 0; c < GridW; c++)
            for (int r = 0; r < GridH; r++)
                _grid[c, r] = TileType.Empty;

        // Default: 4 exit tiles centred on the right column
        int mid = GridH / 2;
        _grid[GridW - 1, mid - 2] = TileType.Exit;
        _grid[GridW - 1, mid - 1] = TileType.Exit;
        _grid[GridW - 1, mid]     = TileType.Exit;
        _grid[GridW - 1, mid + 1] = TileType.Exit;
    }

    private void LoadMap(string name)
    {
        var data = MapManager.LoadFromJson(name);
        if (data == null)
        {
            SetStatus($"Could not load: {name}", error: true);
            return;
        }

        System.Array.Copy(data.Grid, _grid, _grid.Length);
        RebuildSpawnPoints();
        _nameInput.Text = name;
        QueueRedraw();
        SetStatus($"Loaded: {name}");
    }

    private void RebuildSpawnPoints()
    {
        _spawnPoints.Clear();
        for (int c = 0; c < GridW; c++)
            for (int r = 0; r < GridH; r++)
                if (_grid[c, r] == TileType.Spawn)
                    _spawnPoints.Add(new Vector2I(c, r));
    }

    private string Validate()
    {
        if (_spawnPoints.Count == 0) return "Need at least 1 spawn tile (left edge, col 0).";
        if (CountTile(TileType.Exit) == 0) return "Need at least 1 exit tile (right edge).";
        if (!_airflowCalc.HasValidPath(_grid, _spawnPoints))
            return "No valid path from spawn to exit — unblock the corridor.";
        return "";
    }

    private int CountTile(TileType type)
    {
        int count = 0;
        for (int c = 0; c < GridW; c++)
            for (int r = 0; r < GridH; r++)
                if (_grid[c, r] == type) count++;
        return count;
    }

    private static string SanitizeName(string raw)
    {
        string s = raw.Trim().Replace(" ", "_").ToLower();
        foreach (char ch in "/<>:\"\\|?*") s = s.Replace(ch.ToString(), "");
        return s;
    }

    // ── Input ────────────────────────────────────────────────────────────────

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            if (_loadPanel.Visible) { HideLoadPanel(); return; }
            OnBackPressed();
            return;
        }

        if (_loadPanel.Visible) return;

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    _isDragging = true;
                    HandleGridClick(mb.Position, rightClick: false);
                }
                else
                {
                    _isDragging = false;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                HandleGridClick(mb.Position, rightClick: true);
            }
        }
        else if (@event is InputEventMouseMotion mm && _isDragging)
        {
            HandleGridClick(mm.Position, rightClick: false);
        }
    }

    private void HandleGridClick(Vector2 screenPos, bool rightClick)
    {
        var gp = ScreenToGrid(screenPos);
        if (!IsValidGrid(gp)) return;
        int c = gp.X, r = gp.Y;

        if (rightClick) { EraseTile(c, r); return; }

        switch (_currentTool)
        {
            case EditorTool.Wall:
                if (_grid[c, r] == TileType.Empty)
                {
                    _grid[c, r] = TileType.Wall;
                    QueueRedraw();
                }
                break;

            case EditorTool.Spawn:
                if (c != 0) { SetStatus("Spawn: left edge only (col 0).", error: true); return; }
                if (_grid[c, r] == TileType.Spawn) break;
                if (CountTile(TileType.Spawn) >= MaxSpawns)
                { SetStatus($"Max {MaxSpawns} spawns reached.", error: true); return; }
                _grid[c, r] = TileType.Spawn;
                _spawnPoints.Add(new Vector2I(c, r));
                QueueRedraw();
                break;

            case EditorTool.Exit:
                if (c != GridW - 1) { SetStatus("Exit: right edge only.", error: true); return; }
                if (_grid[c, r] == TileType.Exit) break;
                if (CountTile(TileType.Exit) >= MaxExits)
                { SetStatus($"Max {MaxExits} exits reached.", error: true); return; }
                _grid[c, r] = TileType.Exit;
                QueueRedraw();
                break;

            case EditorTool.Erase:
                EraseTile(c, r);
                break;
        }
    }

    private void EraseTile(int c, int r)
    {
        if (_grid[c, r] == TileType.Spawn)
            _spawnPoints.Remove(new Vector2I(c, r));
        _grid[c, r] = TileType.Empty;
        QueueRedraw();
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Grid area background
        DrawRect(new Rect2(GridOffX, GridOffY, GridW * TS, GridH * TS), BgColor);

        for (int c = 0; c < GridW; c++)
        {
            for (int r = 0; r < GridH; r++)
            {
                float x = GridOffX + c * TS;
                float y = GridOffY + r * TS;
                var rect = new Rect2(x, y, TS, TS);

                switch (_grid[c, r])
                {
                    case TileType.Empty:
                        DrawRect(rect, r % 2 == 0 ? BgColor : ScanlineColor);
                        DrawRect(rect, GridLineColor, false, 1f);
                        break;
                    case TileType.Wall:
                        DrawWallTile(x, y);
                        break;
                    case TileType.Spawn:
                        DrawSpawnTile(x, y);
                        break;
                    case TileType.Exit:
                        DrawExitTile(x, y);
                        break;
                }
            }
        }

        // Hover highlight
        var mouse = GetViewport().GetMousePosition();
        var hover = ScreenToGrid(mouse);
        if (IsValidGrid(hover))
            DrawRect(new Rect2(GridOffX + hover.X * TS, GridOffY + hover.Y * TS, TS, TS),
                     new Color(1f, 1f, 1f, 0.22f));

        // Corner markers
        DrawCornerMarkers();
    }

    // Matches GridManager visual style exactly
    private void DrawWallTile(float x, float y)
    {
        DrawRect(new Rect2(x, y, TS, TS), Constants.Colors.Wall);
        DrawLine(new Vector2(x, y),        new Vector2(x + TS - 1, y),        Constants.Colors.WallHighlight, 1f);
        DrawLine(new Vector2(x, y),        new Vector2(x, y + TS - 1),        Constants.Colors.WallHighlight, 1f);
        DrawLine(new Vector2(x, y + TS - 1), new Vector2(x + TS, y + TS - 1), Constants.Colors.WallShadow, 1f);
        DrawLine(new Vector2(x + TS - 1, y), new Vector2(x + TS - 1, y + TS), Constants.Colors.WallShadow, 1f);
        DrawRect(new Rect2(x + 2, y + 2, 2, 2),             Constants.Colors.WallRivet);
        DrawRect(new Rect2(x + TS - 4, y + 2, 2, 2),        Constants.Colors.WallRivet);
        DrawRect(new Rect2(x + 2, y + TS - 4, 2, 2),        Constants.Colors.WallRivet);
        DrawRect(new Rect2(x + TS - 4, y + TS - 4, 2, 2),   Constants.Colors.WallRivet);
    }

    private void DrawSpawnTile(float x, float y)
    {
        DrawRect(new Rect2(x, y, TS, TS), Constants.Colors.Spawn);
        var center = new Vector2(x + TS * 0.5f, y + TS * 0.5f);
        float maxR = TS * 0.48f;
        for (int i = 0; i < 3; i++)
        {
            float phase  = _time * 2.5f + i * (Mathf.Tau / 3f);
            float t      = (phase % Mathf.Tau) / Mathf.Tau;
            float radius = t * maxR;
            float alpha  = (1f - t) * 0.85f;
            DrawArc(center, radius, 0f, Mathf.Tau, 16,
                    new Color(Constants.Colors.SpawnRing, alpha), 1f);
        }
    }

    private void DrawExitTile(float x, float y)
    {
        DrawRect(new Rect2(x, y, TS, TS), Constants.Colors.Exit);
        float scanY = (_time * 24f) % (GridH * TS);
        float tileTop = y - GridOffY;
        if (scanY >= tileTop && scanY < tileTop + TS)
        {
            float localY = scanY - tileTop;
            DrawLine(new Vector2(x, y + localY), new Vector2(x + TS, y + localY),
                     new Color(Constants.Colors.ExitScanLine, 0.85f), 1f);
        }
    }

    private void DrawCornerMarkers()
    {
        float gx = GridOffX;
        float gy = GridOffY;
        float gw = GridW * TS;
        float gh = GridH * TS;
        const int Arm = 4;
        var c = Constants.Colors.CornerMarker;
        const float W = 1.5f;
        DrawLine(new Vector2(gx, gy),        new Vector2(gx + Arm, gy),        c, W);
        DrawLine(new Vector2(gx, gy),        new Vector2(gx, gy + Arm),        c, W);
        DrawLine(new Vector2(gx + gw, gy),   new Vector2(gx + gw - Arm, gy),   c, W);
        DrawLine(new Vector2(gx + gw, gy),   new Vector2(gx + gw, gy + Arm),   c, W);
        DrawLine(new Vector2(gx, gy + gh),   new Vector2(gx + Arm, gy + gh),   c, W);
        DrawLine(new Vector2(gx, gy + gh),   new Vector2(gx, gy + gh - Arm),   c, W);
        DrawLine(new Vector2(gx + gw, gy + gh), new Vector2(gx + gw - Arm, gy + gh), c, W);
        DrawLine(new Vector2(gx + gw, gy + gh), new Vector2(gx + gw, gy + gh - Arm), c, W);
    }

    // ── Coordinate helpers ───────────────────────────────────────────────────

    private static Vector2I ScreenToGrid(Vector2 pos)
    {
        float gx = pos.X - GridOffX;
        float gy = pos.Y - GridOffY;
        if (gx < 0 || gy < 0) return new Vector2I(-1, -1);
        return new Vector2I((int)(gx / TS), (int)(gy / TS));
    }

    private static bool IsValidGrid(Vector2I p) =>
        p.X >= 0 && p.X < GridW && p.Y >= 0 && p.Y < GridH;

    // ── Status ───────────────────────────────────────────────────────────────

    private void SetStatus(string msg, bool error = false)
    {
        _statusLabel.AddThemeColorOverride("font_color",
            error ? Constants.Colors.AlertRed : Constants.Colors.GlowGreen);
        _statusLabel.Text = msg;
    }

    // ── Button factories ─────────────────────────────────────────────────────

    private static Button MakeButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);
        btn.MouseFilter = MouseFilterEnum.Stop;
        btn.FocusMode   = FocusModeEnum.All;

        var normal = new StyleBoxFlat();
        normal.BgColor     = new Color("#0a1a0a");
        normal.BorderColor = new Color("#2d5a3d");
        normal.SetBorderWidthAll(2);
        normal.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor     = new Color("#0d2a0d");
        hover.BorderColor = new Color("#4caf50");
        hover.SetBorderWidthAll(2);
        hover.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor     = new Color("#1a3a1a");
        pressed.BorderColor = new Color("#00c853");
        pressed.SetBorderWidthAll(2);
        pressed.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        return btn;
    }

    private static void SetToolButtonActive(Button btn, bool active)
    {
        var s = new StyleBoxFlat();
        s.BgColor     = active ? new Color("#1a3a1a") : new Color("#0a1a0a");
        s.BorderColor = active ? new Color("#00c853") : new Color("#2d5a3d");
        s.SetBorderWidthAll(2);
        s.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", s);
    }
}
