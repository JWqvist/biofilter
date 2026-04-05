using Godot;

namespace BioFilter.UI;

/// <summary>
/// Button that starts the next wave.
/// Visible only during build phase (WaveManager.Idle state).
/// </summary>
public partial class StartWaveButton : Button
{
    private WaveManager _waveManager;

    public void Initialize(WaveManager waveManager)
    {
        _waveManager = waveManager;
        _waveManager.WaveStarted += OnWaveStarted;
        _waveManager.WaveComplete += OnWaveComplete;
    }

    public override void _Ready()
    {
        Text = "▶ Start Wave";
        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        _waveManager?.StartWave();
    }

    private void OnWaveStarted(int waveNumber)
    {
        Visible = false;
    }

    private void OnWaveComplete(int waveNumber)
    {
        Visible = true;
    }
}
