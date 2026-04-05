using Godot;

namespace BioFilter;

/// <summary>
/// HUD element that displays the current airflow percentage.
/// Color changes: green (> 60%), yellow (20-60%), red (< 20%).
/// Flashes red when below 30%; solid red when below 20%.
/// </summary>
public partial class AirflowMeter : Label
{
    private float _currentAirflow = 1.0f;
    private float _flashTime = 0f;

    // Color thresholds use GameConfig values so they stay in sync with AirflowMinPercent
    private static readonly Color ColorGood   = new Color("#00c853");  // green
    private static readonly Color ColorWarn   = new Color("#ffd600");  // yellow
    private static readonly Color ColorDanger = new Color("#d50000");  // red

    private const float ThresholdGood = 0.60f; // > 60% → green

    public override void _Ready()
    {
        UpdateDisplay(_currentAirflow);
    }

    public override void _Process(double delta)
    {
        if (_currentAirflow <= GameConfig.AirflowWarnFlashThreshold)
        {
            _flashTime += (float)delta;
            // Flash between red and dark-red at ~4 Hz
            float flash = 0.5f + 0.5f * Mathf.Sin(_flashTime * Mathf.Pi * 8f);
            var flashColor = new Color(ColorDanger.R, ColorDanger.G * flash, ColorDanger.B, 1f);
            AddThemeColorOverride("font_color", flashColor);
        }
    }

    /// <summary>
    /// Call this whenever GridManager.CurrentAirflow changes.
    /// </summary>
    public void UpdateAirflow(float airflow)
    {
        _currentAirflow = airflow;
        UpdateDisplay(airflow);
    }

    private void UpdateDisplay(float airflow)
    {
        int percent = (int)(airflow * 100f);
        Text = $"Airflow: {percent}%";

        // Only set static color when not in flash zone
        if (airflow > GameConfig.AirflowWarnFlashThreshold)
        {
            _flashTime = 0f;
            if (airflow > ThresholdGood)
                AddThemeColorOverride("font_color", ColorGood);
            else
                AddThemeColorOverride("font_color", ColorWarn);
        }
        // Flash handled in _Process when airflow <= AirflowWarnFlashThreshold
    }
}
