using Godot;

namespace BioFilter.UI;

/// <summary>
/// Pause menu shown when Escape is pressed during gameplay.
/// Displays a semi-transparent overlay with Resume, Quit to Main Menu, and Quit Game.
/// Pauses the game tree while open; uses ProcessModeEnum.Always so it can still
/// receive input while the tree is paused.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    private bool _isOpen = false;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        // ── Overlay (semi-transparent dark background) ───────────────────
        var overlay = new ColorRect();
        overlay.LayoutMode = 3;
        overlay.AnchorRight  = 1f;
        overlay.AnchorBottom = 1f;
        overlay.Color = new Color(0f, 0f, 0f, 0.65f);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop; // block clicks to game below
        AddChild(overlay);

        // ── Center panel ─────────────────────────────────────────────────
        var panel = new Panel();
        panel.LayoutMode = 3;
        panel.AnchorLeft   = 0.5f;
        panel.AnchorTop    = 0.5f;
        panel.AnchorRight  = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft   = -120f;
        panel.OffsetTop    = -100f;
        panel.OffsetRight  = 120f;
        panel.OffsetBottom = 100f;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor    = new Color("#1a1a2e");
        panelStyle.BorderColor = new Color("#5c5c8a");
        panelStyle.SetBorderWidthAll(1);
        panelStyle.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        // ── VBox ─────────────────────────────────────────────────────────
        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        // Spacer top
        var topSpacer = new Control();
        topSpacer.CustomMinimumSize = new Vector2(0, 8f);
        vbox.AddChild(topSpacer);

        // Title
        var title = new Label();
        title.Text = "⏸  PAUSED";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", new Color("#e0e0e0"));
        title.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Resume button
        var resumeBtn = new Button();
        resumeBtn.Text = "▶  Resume";
        resumeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        resumeBtn.Pressed += OnResume;
        vbox.AddChild(resumeBtn);

        // Quit to Main Menu button
        var mainMenuBtn = new Button();
        mainMenuBtn.Text = "⏏  Main Menu";
        mainMenuBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        mainMenuBtn.Pressed += OnQuitToMainMenu;
        vbox.AddChild(mainMenuBtn);

        // Quit Game button
        var quitBtn = new Button();
        quitBtn.Text = "✕  Quit Game";
        quitBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        quitBtn.Pressed += OnQuitGame;
        vbox.AddChild(quitBtn);
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

    // ── Button handlers ───────────────────────────────────────────────────

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
}
