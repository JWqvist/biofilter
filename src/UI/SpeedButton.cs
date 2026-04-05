using Godot;

namespace BioFilter.UI;

/// <summary>
/// Toggle button that switches game speed between 1x and 2x.
/// Resets to 1x at wave complete.
/// </summary>
public partial class SpeedButton : Button
{
    private bool _isFast = false;

    public override void _Ready()
    {
        Pressed += OnToggle;
        UpdateLabel();
    }

    private void OnToggle()
    {
        _isFast = !_isFast;
        Engine.TimeScale = _isFast ? GameConfig.SpeedFast : GameConfig.SpeedNormal;
        UpdateLabel();
    }

    /// <summary>Call when a wave completes to reset speed to 1x.</summary>
    public void ResetSpeed()
    {
        _isFast = false;
        Engine.TimeScale = GameConfig.SpeedNormal;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        Text = _isFast ? "▶▶ 2x" : "▶ 1x";
    }
}
