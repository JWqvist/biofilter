using Godot;

namespace BioFilter.UI;

/// <summary>
/// Military terminal-style pause menu. Built entirely in code.
/// Sprint 13C: Pixel Art Menus
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    private bool  _isOpen    = false;
    private float _blinkTimer = 0f;
    private bool  _blinkOn   = true;
    private Label _titleLabel = null!;

    private const float PanelW = 380f;
    private const float PanelH = 280f;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        // ── Semi-transparent overlay ──────────────────────────────────────
        var overlay = new ColorRect();
        overlay.LayoutMode   = 3;
        overlay.AnchorRight  = 1f;
        overlay.AnchorBottom = 1f;
        overlay.Color = new Color(0f, 0f, 0f, 0.70f);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(overlay);

        // ── Terminal panel ────────────────────────────────────────────────
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
        panelStyle.BorderColor = new Color("#2d5a3d");
        panelStyle.SetBorderWidthAll(2);
        panelStyle.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        AddSpacer(vbox, 8f);

        // ── Title row ────────────────────────────────────────────────────
        var titleRow = new HBoxContainer();
        titleRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddChild(titleRow);

        _titleLabel = new Label();
        _titleLabel.Text = "  ▣ SYSTEM PAUSE";
        _titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _titleLabel.AddThemeColorOverride("font_color", new Color("#4caf50"));
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleRow.AddChild(_titleLabel);

        var escLabel = new Label();
        escLabel.Text = "[ESC=RESUME]  ";
        escLabel.AddThemeColorOverride("font_color", new Color("#2d5a3d"));
        escLabel.AddThemeFontSizeOverride("font_size", 10);
        escLabel.VerticalAlignment = VerticalAlignment.Bottom;
        titleRow.AddChild(escLabel);

        // Divider
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 2f);
        div.Color = new Color("#2d5a3d");
        vbox.AddChild(div);

        AddSpacer(vbox, 8f);

        // ── Buttons ───────────────────────────────────────────────────────
        var resumeBtn = MakeTerminalButton("▶ RESUME MISSION", new Color("#2d5a3d"), new Color("#4caf50"));
        resumeBtn.Pressed += OnResume;
        vbox.AddChild(resumeBtn);

        AddSpacer(vbox, 4f);

        var menuBtn = MakeTerminalButton("⌂ ABORT TO MAIN MENU", new Color("#2d5a3d"), new Color("#4a9e6a"));
        menuBtn.Pressed += OnQuitToMainMenu;
        vbox.AddChild(menuBtn);

        AddSpacer(vbox, 4f);

        var quitBtn = MakeTerminalButton("✕ TERMINATE PROGRAM", new Color("#3a1a1a"), new Color("#884444"));
        quitBtn.Pressed += OnQuitGame;
        vbox.AddChild(quitBtn);

        AddSpacer(vbox, 12f);

        // ── Warning ───────────────────────────────────────────────────────
        var warnDiv = new ColorRect();
        warnDiv.CustomMinimumSize = new Vector2(0, 1f);
        warnDiv.Color = new Color("#2d5a3d");
        vbox.AddChild(warnDiv);

        AddSpacer(vbox, 4f);

        var warning = new Label();
        warning.Text = "  WARNING: Unsaved progress will be lost";
        warning.AddThemeColorOverride("font_color", new Color("#556644"));
        warning.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(warning);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _blinkTimer += (float)delta;
        if (_blinkTimer >= 0.5f)
        {
            _blinkTimer = 0f;
            _blinkOn    = !_blinkOn;
            _titleLabel.Modulate = _blinkOn ? Colors.White : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Open()
    {
        _isOpen = true;
        Visible = true;
        GetTree().Paused = true;
    }

    public void Close()
    {
        _isOpen = false;
        Visible = false;
        GetTree().Paused = false;
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else         Open();
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private void OnResume() => Close();

    private void OnQuitToMainMenu()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    private void OnQuitGame()
    {
        GetTree().Paused = false;
        GetTree().Quit();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Button MakeTerminalButton(string text, Color bgColor, Color borderColor)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        btn.CustomMinimumSize   = new Vector2(260f, 30f);

        var normal = new StyleBoxFlat();
        normal.BgColor     = bgColor;
        normal.BorderColor = borderColor;
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor     = borderColor with { A = 0.2f };
        hover.BorderColor = borderColor;
        hover.SetBorderWidthAll(2);
        hover.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor     = borderColor with { A = 0.35f };
        pressed.BorderColor = borderColor;
        pressed.SetBorderWidthAll(2);
        pressed.SetCornerRadiusAll(0);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeColorOverride("font_color", borderColor);
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
