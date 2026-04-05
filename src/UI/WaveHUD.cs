using Godot;
using BioFilter.UI;

namespace BioFilter.UI;

/// <summary>
/// HUD: Wave progress — now delegates to WaveWidget (pixel art).
/// </summary>
public partial class WaveHUD : Control
{
    private WaveWidget _widget = null!;

    public override void _Ready()
    {
        _widget = new WaveWidget();
        AddChild(_widget);
        _widget.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public void OnWaveStarted(int waveNumber)
    {
        _widget?.OnWaveStarted(waveNumber);
    }

    public void OnWaveComplete(int waveNumber)
    {
        _widget?.OnWaveComplete(waveNumber);
    }
}
