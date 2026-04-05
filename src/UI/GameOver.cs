using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Full-screen game over screen — military terminal style, built entirely in code.
/// Sprint 13C: Pixel Art Menus
/// </summary>
public partial class GameOver : CanvasLayer
{
    private Label  _wavesLabel      = null!;
    private Label  _killsLabel      = null!;
    private Label  _popLabel        = null!;

    // Blink state
    private float  _blinkTimer      = 0f;
    private bool   _blinkVisible    = true;
    private Label  _blinkLabel      = null!;

    private const float PanelW = 420f;
    private const float PanelH = 320f;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        // Full-screen dark overlay
        var overlay = new ColorRect();
        overlay.LayoutMode   = 3;
        overlay.AnchorRight  = 1f;
        overlay.AnchorBottom = 1f;
        overlay.Color = new Color(0f, 0f, 0f, 0.80f);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(overlay);

        // Terminal panel (centered)
        var panel = new Panel();
        panel.LayoutMode   = 3;
        panel.AnchorLeft   = 0.5f;
        panel.AnchorTop    = 0.5f;
        panel.AnchorRight  = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft   = -PanelW * 0.5f;
        panel.OffsetTop    = -PanelH * 0.5f;
        panel.OffsetRight  =  PanelW * 0.5f;
        panel.OffsetBottom =  PanelH * 0.5f;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color("#0d1208");
        panelStyle.BorderColor = new Color("#cc0000");
        panelStyle.SetBorderWidthAll(2);
        panelStyle.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        // Inner VBox
        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        AddSpacer(vbox, 10f);

        // ── Blinking title ───────────────────────────────────────────────
        _blinkLabel = MakeLabel("██████ CRITICAL FAILURE ██████", 18, new Color("#cc0000"), HorizontalAlignment.Center);
        vbox.AddChild(_blinkLabel);

        AddSpacer(vbox, 4f);

        // Red divider
        var divColor = new ColorRect();
        divColor.CustomMinimumSize = new Vector2(0, 2f);
        divColor.Color = new Color("#cc0000");
        vbox.AddChild(divColor);

        AddSpacer(vbox, 8f);

        // Subtitle
        vbox.AddChild(MakeLabel("  ⚠ BUNKER BREACHED", 13, new Color("#ff4444"), HorizontalAlignment.Left));
        vbox.AddChild(MakeLabel("  All personnel lost.", 11, Constants.Colors.TextDim, HorizontalAlignment.Left));

        AddSpacer(vbox, 12f);

        // Stats
        _wavesLabel = MakeLabel("  Waves survived: — / " + GameConfig.TotalWaves, 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        _killsLabel = MakeLabel("  Particles neutralised: —", 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        _popLabel   = MakeLabel("  Bunker population at fall: 0", 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        vbox.AddChild(_wavesLabel);
        vbox.AddChild(_killsLabel);
        vbox.AddChild(_popLabel);

        AddSpacer(vbox, 16f);

        // Retry button
        var retryBtn = MakeTerminalButton("▶ RETRY MISSION", new Color("#cc0000"));
        retryBtn.Pressed += OnRetryPressed;
        vbox.AddChild(retryBtn);

        AddSpacer(vbox, 6f);

        // Main menu button
        var menuBtn = MakeTerminalButton("⌂ RETURN TO BASE", new Color("#aa3333"));
        menuBtn.Pressed += OnMenuPressed;
        vbox.AddChild(menuBtn);

        AddSpacer(vbox, 10f);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _blinkTimer += (float)delta;
        if (_blinkTimer >= 0.5f)
        {
            _blinkTimer  = 0f;
            _blinkVisible = !_blinkVisible;
            _blinkLabel.Modulate = _blinkVisible ? Colors.White : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    /// <summary>Show the game-over screen and populate stats from GameState.</summary>
    public void Show(int population = 0)
    {
        // Try to resolve GameState from the scene tree
        var gs = GetNodeOrNull<GameState>("/root/Main/GameState");
        if (gs != null)
        {
            _wavesLabel.Text = $"  Waves survived: {gs.WavesSurvived} / {GameConfig.TotalWaves}";
            _killsLabel.Text = $"  Particles neutralised: {gs.ParticlesKilled}";
        }
        else
        {
            _wavesLabel.Text = $"  Waves survived: — / {GameConfig.TotalWaves}";
            _killsLabel.Text = "  Particles neutralised: —";
        }
        _popLabel.Text = $"  Bunker population at fall: {population}";
        Visible = true;
    }

    // ── Button handlers ───────────────────────────────────────────────────

    private void OnRetryPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    private void OnMenuPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Label MakeLabel(string text, int fontSize, Color color, HorizontalAlignment align)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.HorizontalAlignment = align;
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return lbl;
    }

    private static Button MakeTerminalButton(string text, Color borderColor)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        btn.CustomMinimumSize   = new Vector2(240f, 32f);

        var normal = new StyleBoxFlat();
        normal.BgColor     = new Color("#1a0000");
        normal.BorderColor = borderColor;
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor     = new Color("#2a0a0a");
        hover.BorderColor = new Color("#ff2222");
        hover.SetBorderWidthAll(2);
        hover.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor     = new Color("#3a1010");
        pressed.BorderColor = new Color("#ff5555");
        pressed.SetBorderWidthAll(2);
        pressed.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeColorOverride("font_color", new Color("#ff4444"));
        btn.AddThemeFontSizeOverride("font_size", 12);
        return btn;
    }

    private static void AddSpacer(VBoxContainer vbox, float height)
    {
        var sp = new Control();
        sp.CustomMinimumSize = new Vector2(0, height);
        vbox.AddChild(sp);
    }
}
