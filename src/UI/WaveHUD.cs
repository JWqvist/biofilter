using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>HUD label showing current wave progress: "Wave: X / Y"</summary>
public partial class WaveHUD : Label
{
    public override void _Ready()
    {
        UpdateDisplay(0, GameConfig.TotalWaves);
    }

    public void OnWaveStarted(int waveNumber)
    {
        UpdateDisplay(waveNumber, GameConfig.TotalWaves);
    }

    public void OnWaveComplete(int waveNumber)
    {
        UpdateDisplay(waveNumber, GameConfig.TotalWaves);
    }

    private void UpdateDisplay(int current, int total)
    {
        Text = $"Wave: {current} / {total}";
    }
}
