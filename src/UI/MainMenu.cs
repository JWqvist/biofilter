using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Main menu — retro CRT terminal style.
/// Keeps scene node structure but upgrades styling + adds scanline + blink effects.
/// Sprint 13C: Pixel Art Menus
/// </summary>
public partial class MainMenu : Control
{
    private Button       _playButton    = null!;
    private Button       _loadButton    = null!;
    private Button       _map1Button   = null!;
    private Button       _map2Button   = null!;
    private Label        _hazardLabel  = null!;
    private Label        _titleLabel   = null!;
    private HBoxContainer _userMapsRow = null!;
    private Button       _editorButton = null!;

    // Tracks which user-map button is currently active (by name)
    private string _activeUserMap = "";

    // Scanline effect
    private ColorRect _scanline  = null!;
    private float     _scanY     = 0f;
    private float     _viewH     = 600f;

    // Hazard blink
    private float _blinkTimer   = 0f;
    private bool  _blinkOn      = true;

    public override void _Ready()
    {
        // Background image
        var bg = GetNode<TextureRect>("Background");
        bg.Texture = GD.Load<Texture2D>("res://assets/menu_background.png");

        // Dark green overlay
        var overlay = GetNode<ColorRect>("Overlay");
        overlay.Color = new Color(0.04f, 0.06f, 0.04f, 0.70f);

        // Title — large green pixel art style
        _titleLabel = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        _titleLabel.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);

        // Hazard label — blinking yellow
        _hazardLabel = GetNode<Label>("CenterContainer/VBoxContainer/HazardLabel");
        _hazardLabel.AddThemeColorOverride("font_color", Constants.Colors.HazardYellow);
        _hazardLabel.AddThemeFontSizeOverride("font_size", 12);

        // Subtitle
        var subtitle = GetNode<Label>("CenterContainer/VBoxContainer/SubtitleLabel");
        subtitle.AddThemeColorOverride("font_color", Constants.Colors.TextDim);

        // ── Play button — pixel art border, green glow ────────────────────
        _playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor     = new Color("#0a1a0a");
        normalStyle.BorderColor = new Color("#2d5a3d");
        normalStyle.SetBorderWidthAll(2);
        normalStyle.SetCornerRadiusAll(0);
        _playButton.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor     = new Color("#0d2a0d");
        hoverStyle.BorderColor = new Color("#4caf50");
        hoverStyle.SetBorderWidthAll(2);
        hoverStyle.SetCornerRadiusAll(0);
        _playButton.AddThemeStyleboxOverride("hover", hoverStyle);

        var pressedStyle = new StyleBoxFlat();
        pressedStyle.BgColor     = new Color("#1a3a1a");
        pressedStyle.BorderColor = new Color("#00c853");
        pressedStyle.SetBorderWidthAll(2);
        pressedStyle.SetCornerRadiusAll(0);
        _playButton.AddThemeStyleboxOverride("pressed", pressedStyle);

        _playButton.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);
        _playButton.AddThemeFontSizeOverride("font_size", 15);
        _playButton.Pressed += OnPlayPressed;
        _playButton.MouseFilter = Control.MouseFilterEnum.Stop;
        _playButton.FocusMode = Control.FocusModeEnum.All;

        // ── Load Game button ──────────────────────────────────────────────
        _loadButton = new Button();
        _loadButton.Text = SaveManager.HasSave ? "LOAD GAME  [F9]" : "LOAD GAME  [F9]  — no save";
        _loadButton.Disabled = !SaveManager.HasSave;
        ApplyMapButtonStyle(_loadButton);
        _loadButton.CustomMinimumSize = new Vector2(160f, 34f);
        _loadButton.Pressed += OnLoadPressed;
        _loadButton.MouseFilter = Control.MouseFilterEnum.Stop;
        _loadButton.FocusMode = Control.FocusModeEnum.All;
        var vboxForLoad = _playButton.GetParent<VBoxContainer>();
        vboxForLoad.AddChild(_loadButton);
        vboxForLoad.MoveChild(_loadButton, _playButton.GetIndex() + 1);

        // ── Map selection buttons ────────────────────────────────────
        _map1Button = GetNode<Button>("CenterContainer/VBoxContainer/MapRow/Map1Button");
        _map2Button = GetNode<Button>("CenterContainer/VBoxContainer/MapRow/Map2Button");
        ApplyMapButtonStyle(_map1Button);
        ApplyMapButtonStyle(_map2Button);
        _map1Button.Pressed += () => OnMapSelected(1);
        _map2Button.Pressed += () => OnMapSelected(2);

        // ── User maps row ─────────────────────────────────────────────────────
        _userMapsRow = GetNode<HBoxContainer>("CenterContainer/VBoxContainer/UserMapsRow");
        if (MapManager.CurrentMap == 0 && MapManager.CustomMap != null)
            _activeUserMap = MapManager.CustomMap.Name;
        RefreshUserMaps();

        // ── Map editor button ─────────────────────────────────────────────────
        _editorButton = GetNode<Button>("CenterContainer/VBoxContainer/EditorButton");
        ApplyMapButtonStyle(_editorButton);
        _editorButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MapEditor.tscn");

        // Highlight the current selection
        UpdateMapButtons();

        // ── Version label (bottom-right) ──────────────────────────────────
        var versionLabel = new Label();
        versionLabel.Text = "v0.13c";
        versionLabel.LayoutMode   = 3;
        versionLabel.AnchorLeft   = 1f;
        versionLabel.AnchorTop    = 1f;
        versionLabel.AnchorRight  = 1f;
        versionLabel.AnchorBottom = 1f;
        versionLabel.OffsetLeft   = -60f;
        versionLabel.OffsetTop    = -22f;
        versionLabel.OffsetRight  = -4f;
        versionLabel.OffsetBottom = -4f;
        versionLabel.AddThemeColorOverride("font_color", new Color("#2d5a3d"));
        versionLabel.AddThemeFontSizeOverride("font_size", 9);
        AddChild(versionLabel);

        // ── Scanline overlay ──────────────────────────────────────────────
        _scanline = new ColorRect();
        _scanline.LayoutMode = 3;
        _scanline.AnchorLeft  = 0f;
        _scanline.AnchorTop   = 0f;
        _scanline.AnchorRight = 1f;
        _scanline.AnchorBottom = 0f;
        _scanline.OffsetTop    = 0f;
        _scanline.OffsetBottom = 3f;
        _scanline.Color = new Color(0f, 1f, 0.25f, 0.06f);
        _scanline.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_scanline);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Scanline sweep
        _viewH = GetViewportRect().Size.Y;
        _scanY += dt * (_viewH * 0.4f);
        if (_scanY > _viewH + 3f) _scanY = -3f;
        _scanline.OffsetTop    = _scanY;
        _scanline.OffsetBottom = _scanY + 3f;

        // Hazard blink
        _blinkTimer += dt;
        if (_blinkTimer >= 0.6f)
        {
            _blinkTimer = 0f;
            _blinkOn    = !_blinkOn;
            _hazardLabel.Modulate = _blinkOn ? Colors.White : new Color(1f, 1f, 1f, 0.2f);
        }
    }

    private void OnMapSelected(int mapNumber)
    {
        MapManager.CurrentMap = mapNumber;
        MapManager.CustomMap  = null;
        _activeUserMap        = "";
        UpdateMapButtons();
    }

    private void UpdateMapButtons()
    {
        SetMapButtonActive(_map1Button, MapManager.CurrentMap == 1);
        SetMapButtonActive(_map2Button, MapManager.CurrentMap == 2);
        // User map buttons: highlight the active one
        foreach (var child in _userMapsRow.GetChildren())
        {
            if (child is Button btn)
                SetMapButtonActive(btn, MapManager.CurrentMap == 0 && btn.Text == _activeUserMap);
        }
    }

    private void RefreshUserMaps()
    {
        // Remove existing user-map buttons
        foreach (Node child in _userMapsRow.GetChildren())
            child.QueueFree();

        var dir = DirAccess.Open("user://user_maps");
        if (dir == null) return;

        dir.ListDirBegin();
        string fname = dir.GetNext();
        while (fname != "")
        {
            if (!dir.CurrentIsDir() && fname.EndsWith(".json"))
            {
                string mapName = fname.Replace(".json", "");
                var btn = new Button();
                btn.Text = mapName;
                ApplyMapButtonStyle(btn);
                btn.CustomMinimumSize = new Vector2(86, 34);
                string captured = mapName;
                btn.Pressed += () => OnUserMapSelected(captured);
                _userMapsRow.AddChild(btn);
            }
            fname = dir.GetNext();
        }
    }

    private void OnUserMapSelected(string mapName)
    {
        var data = MapManager.LoadFromJson(mapName);
        if (data == null) return;
        MapManager.CurrentMap = 0;
        MapManager.CustomMap  = data;
        _activeUserMap        = mapName;
        UpdateMapButtons();
    }

    private static void ApplyMapButtonStyle(Button btn)
    {
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

        btn.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.MouseFilter = Control.MouseFilterEnum.Stop;
        btn.FocusMode   = Control.FocusModeEnum.All;
    }

    private static void SetMapButtonActive(Button btn, bool active)
    {
        // Swap border colour: bright green when active, dim when not
        var active_style = new StyleBoxFlat();
        active_style.BgColor     = active ? new Color("#0d2a0d") : new Color("#0a1a0a");
        active_style.BorderColor = active ? new Color("#00c853") : new Color("#2d5a3d");
        active_style.SetBorderWidthAll(2);
        active_style.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", active_style);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F9)
            OnLoadPressed();
    }

    private void OnPlayPressed()
    {
        // New game — clear any existing save so a fresh run starts clean
        SaveManager.DeleteSave();
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    private void OnLoadPressed()
    {
        if (!SaveManager.HasSave) return;
        SaveManager.PendingLoad = true;
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
