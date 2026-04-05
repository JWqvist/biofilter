using Godot;
using BioFilter.UI;

namespace BioFilter;

/// <summary>
/// HUD: Airflow meter — now delegates to AirflowGauge (pixel art semicircle).
/// </summary>
public partial class AirflowMeter : Control
{
    private AirflowGauge _gauge = null!;

    public override void _Ready()
    {
        _gauge = new AirflowGauge();
        AddChild(_gauge);
        _gauge.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _gauge.UpdateAirflow(1.0f);
    }

    public void UpdateAirflow(float airflow)
    {
        _gauge?.UpdateAirflow(airflow);
    }
}
