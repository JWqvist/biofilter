using Godot;

namespace BioFilter.UI;

/// <summary>
/// Full-screen CanvasLayer that draws a red border vignette when airflow is critically low.
/// Attach to Main scene. GridManager.AirflowCritical → UpdateAirflow().
/// </summary>
public partial class AirflowVignette : CanvasLayer
{
    private ColorRect _overlay = null!;
    private float _currentAirflow = 1.0f;
    private float _time = 0f;

    public override void _Ready()
    {
        // Full-screen transparent ColorRect — we'll draw the vignette via shader-like approach
        _overlay = new ColorRect();
        _overlay.LayoutMode = 3;
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.Color = Colors.Transparent;
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        _overlay.Visible = false;
        AddChild(_overlay);
    }

    public override void _Process(double delta)
    {
        if (_currentAirflow > GameConfig.AirflowCriticalThreshold)
        {
            _overlay.Visible = false;
            return;
        }

        _overlay.Visible = true;
        _time += (float)delta;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * Mathf.Pi * 3f);
        float intensity = Mathf.Lerp(0.25f, 0.55f, pulse);
        _overlay.Color = new Color(0.8f, 0f, 0f, intensity * 0.35f);
    }

    /// <summary>Called by Main when GridManager.AirflowChanged fires.</summary>
    public void UpdateAirflow(float airflow)
    {
        _currentAirflow = airflow;
        if (airflow > GameConfig.AirflowCriticalThreshold)
        {
            _time = 0f;
            _overlay.Visible = false;
        }
    }
}
