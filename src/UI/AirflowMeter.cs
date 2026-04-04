using Godot;

namespace BioFilter;

/// <summary>
/// HUD element that displays the current airflow percentage.
/// Color changes: green (> 60%), yellow (20-60%), red (< 20%).
/// </summary>
public partial class AirflowMeter : Label
{
    private float _currentAirflow = 1.0f;

    // Color thresholds use GameConfig values so they stay in sync with AirflowMinPercent
    private static readonly Color ColorGood = new Color("#00c853");    // green
    private static readonly Color ColorWarn = new Color("#ffd600");    // yellow
    private static readonly Color ColorDanger = new Color("#d50000");  // red

    private const float ThresholdGood = 0.60f;   // > 60% → green

    public override void _Ready()
    {
        UpdateDisplay(_currentAirflow);
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

        if (airflow > ThresholdGood)
            AddThemeColorOverride("font_color", ColorGood);
        else if (airflow >= GameConfig.AirflowMinPercent)
            AddThemeColorOverride("font_color", ColorWarn);
        else
            AddThemeColorOverride("font_color", ColorDanger);
    }
}
