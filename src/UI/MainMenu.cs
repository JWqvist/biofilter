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
    private Button _playButton  = null!;
    private Label  _hazardLabel = null!;
    private Label  _titleLabel  = null!;

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

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
