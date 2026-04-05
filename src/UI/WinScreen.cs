using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Full-screen win screen — military terminal style, built entirely in code.
/// Sprint 13C: Pixel Art Menus
/// </summary>
public partial class WinScreen : CanvasLayer
{
    private Label _killsLabel  = null!;
    private Label _popLabel    = null!;
    private Label _airLabel    = null!;

    private float _blinkTimer  = 0f;
    private bool  _blinkVisible = true;
    private Label _blinkLabel  = null!;

    private const float PanelW = 420f;
    private const float PanelH = 320f;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        var overlay = new ColorRect();
        overlay.LayoutMode   = 3;
        overlay.AnchorRight  = 1f;
        overlay.AnchorBottom = 1f;
        overlay.Color = new Color(0f, 0f, 0f, 0.80f);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(overlay);

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
        panelStyle.BgColor     = new Color("#0a140a");
        panelStyle.BorderColor = new Color("#00c853");
        panelStyle.SetBorderWidthAll(2);
        panelStyle.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        AddSpacer(vbox, 10f);

        _blinkLabel = MakeLabel("██████ MISSION SUCCESS ██████", 18, new Color("#00c853"), HorizontalAlignment.Center);
        vbox.AddChild(_blinkLabel);

        AddSpacer(vbox, 4f);

        var divColor = new ColorRect();
        divColor.CustomMinimumSize = new Vector2(0, 2f);
        divColor.Color = new Color("#00c853");
        vbox.AddChild(divColor);

        AddSpacer(vbox, 8f);

        vbox.AddChild(MakeLabel("  ✓ BUNKER SECURED", 13, new Color("#00ff66"), HorizontalAlignment.Left));
        vbox.AddChild(MakeLabel($"  All {GameConfig.TotalWaves} waves repelled.", 11, Constants.Colors.TextDim, HorizontalAlignment.Left));

        AddSpacer(vbox, 12f);

        _killsLabel = MakeLabel("  Particles neutralised: —", 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        _popLabel   = MakeLabel("  Final population: —", 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        _airLabel   = MakeLabel("  Airflow maintained: —%", 11, Constants.Colors.TextPrimary, HorizontalAlignment.Left);
        vbox.AddChild(_killsLabel);
        vbox.AddChild(_popLabel);
        vbox.AddChild(_airLabel);

        AddSpacer(vbox, 16f);

        var deployBtn = MakeTerminalButton("▶ DEPLOY AGAIN", new Color("#00c853"));
        deployBtn.Pressed += OnDeployPressed;
        vbox.AddChild(deployBtn);

        AddSpacer(vbox, 6f);

        var menuBtn = MakeTerminalButton("⌂ RETURN TO BASE", new Color("#228833"));
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
            _blinkTimer   = 0f;
            _blinkVisible = !_blinkVisible;
            _blinkLabel.Modulate = _blinkVisible ? Colors.White : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    public void Show(int wavesWon)
    {
        var gs = GetNodeOrNull<GameState>("/root/Main/GameState");
        if (gs != null)
        {
            _killsLabel.Text = $"  Particles neutralised: {gs.ParticlesKilled}";
            _popLabel.Text   = $"  Final population: {gs.Population}";
            int airPct = Mathf.RoundToInt(gs.CurrentAirflow * 100f);
            _airLabel.Text   = $"  Airflow maintained: {airPct}%";
        }
        else
        {
            _killsLabel.Text = "  Particles neutralised: —";
            _popLabel.Text   = "  Final population: —";
            _airLabel.Text   = "  Airflow maintained: —%";
        }
        Visible = true;
    }

    private void OnDeployPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    private void OnMenuPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

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
        normal.BgColor     = new Color("#041204");
        normal.BorderColor = borderColor;
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor     = new Color("#0a2a0a");
        hover.BorderColor = new Color("#33ff77");
        hover.SetBorderWidthAll(2);
        hover.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor     = new Color("#143014");
        pressed.BorderColor = new Color("#66ffaa");
        pressed.SetBorderWidthAll(2);
        pressed.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeColorOverride("font_color", new Color("#00ff66"));
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
