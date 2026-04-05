using Godot;

namespace BioFilter.UI;

/// <summary>
/// Semi-transparent overlay shown for GameConfig.WavePreviewDuration seconds before each wave.
/// Displays wave number, particle count, and enemy type. Auto-hides when the timer expires.
/// </summary>
public partial class WavePreview : CanvasLayer
{
    private Panel  _panel  = null!;
    private Label  _titleLabel  = null!;
    private Label  _detailLabel = null!;
    private Timer  _timer  = null!;

    // Emitted when the preview has finished so WaveManager can proceed with spawning.
    [Signal] public delegate void PreviewFinishedEventHandler();

    public override void _Ready()
    {
        // ── Panel positioned top-center ──────────────────────────────────────
        _panel = new Panel();
        _panel.LayoutMode = 3; // anchors-based
        _panel.AnchorLeft   = 0.5f;
        _panel.AnchorTop    = 0f;
        _panel.AnchorRight  = 0.5f;
        _panel.AnchorBottom = 0f;
        _panel.OffsetLeft   = -120f;
        _panel.OffsetTop    = 48f;
        _panel.OffsetRight  = 120f;
        _panel.OffsetBottom = 108f;
        _panel.Visible      = false;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.05f, 0.05f, 0.15f, 0.82f);
        style.BorderColor = new Color("#5c5c8a");
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        // ── Labels inside panel ─────────────────────────────────────────────
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.LayoutMode = 1;
        _panel.AddChild(vbox);

        _titleLabel = new Label();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeColorOverride("font_color", new Color("#ffd600"));
        _titleLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_titleLabel);

        _detailLabel = new Label();
        _detailLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _detailLabel.AddThemeColorOverride("font_color", new Color("#cccccc"));
        _detailLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(_detailLabel);

        // ── Auto-hide timer ──────────────────────────────────────────────────
        _timer = new Timer();
        _timer.OneShot = true;
        _timer.WaitTime = GameConfig.WavePreviewDuration;
        _timer.Timeout += OnTimerTimeout;
        AddChild(_timer);
    }

    /// <summary>Shows the wave preview and starts the countdown.</summary>
    public void ShowPreview(int waveNumber, int particleCount)
    {
        _titleLabel.Text  = $"⚠ WAVE {waveNumber} INCOMING";
        _detailLabel.Text = $"{particleCount} particles — {GetEnemyType(waveNumber)}";
        _panel.Visible    = true;
        _timer.Stop();
        _timer.Start();
    }

    private void OnTimerTimeout()
    {
        _panel.Visible = false;
        EmitSignal(SignalName.PreviewFinished);
    }

    private static string GetEnemyType(int waveNumber)
    {
        return waveNumber switch
        {
            1 or 2 or 3 => "Bio Particles",
            4            => "Bio Particles + Spore Specks",
            5 or 6       => "Spore Specks + Radiation Blobs",
            7            => "Bacterial Swarms + Bio Particles",
            8            => "Cell Divisions + Spore Specks",
            9            => "All Enemy Types",
            _            => "⚠ BOSS: Heavy Radiation Blobs + Cell Divisions",
        };
    }
}
