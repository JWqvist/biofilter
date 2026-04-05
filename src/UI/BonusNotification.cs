using Godot;

namespace BioFilter.UI;

/// <summary>
/// Displays floating bonus text (e.g. "+50 PERFECT WAVE!") that fades out automatically.
/// Positioned top-center in a CanvasLayer.
/// </summary>
public partial class BonusNotification : CanvasLayer
{
    private Label _label = null!;
    private Timer _timer = null!;
    private float _fadeDuration = GameConfig.BonusNotificationDuration;
    private float _elapsed = 0f;
    private bool _fading = false;

    public override void _Ready()
    {
        _label = new Label();
        _label.LayoutMode = 3;
        _label.AnchorLeft   = 0.5f;
        _label.AnchorTop    = 0f;
        _label.AnchorRight  = 0.5f;
        _label.AnchorBottom = 0f;
        _label.OffsetLeft   = -140f;
        _label.OffsetTop    = 115f;
        _label.OffsetRight  = 140f;
        _label.OffsetBottom = 135f;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.AddThemeColorOverride("font_color", new Color("#ffd600"));
        _label.AddThemeFontSizeOverride("font_size", 12);
        _label.Visible = false;
        AddChild(_label);

        _timer = new Timer();
        _timer.OneShot = true;
        _timer.WaitTime = _fadeDuration;
        _timer.Timeout += () => { _label.Visible = false; _fading = false; };
        AddChild(_timer);
    }

    public override void _Process(double delta)
    {
        if (!_fading || !_label.Visible) return;
        _elapsed += (float)delta;
        float alpha = 1f - Mathf.Clamp(_elapsed / _fadeDuration, 0f, 1f);
        _label.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    /// <summary>Shows a bonus message (e.g. "+50 PERFECT WAVE!") and fades out.</summary>
    public void ShowBonus(string message)
    {
        _label.Text    = message;
        _label.Visible = true;
        _label.Modulate = Colors.White;
        _elapsed = 0f;
        _fading  = true;
        _timer.Stop();
        _timer.Start();
    }
}
