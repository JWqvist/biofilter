using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Power Core — passive income tower.
/// Generates +GameConfig.PowerCoreIncomePerWave currency at the start of each wave.
/// Visual: pulsing gold square with 4 rays.
/// </summary>
public partial class PowerCore : TowerBase
{
    public override float Range => 0f; // no range — passive
    public override int Cost => GameConfig.PowerCoreCost;
    protected override Color TowerColor => new Color("#ffd700");

    // Injected by TowerManager
    public WaveManager? WaveManagerRef { get; set; }
    public GameState? GameStateRef { get; set; }

    private float _time = 0f;

    public override void _Ready()
    {
        base._Ready();
    }

    /// <summary>Called once after placement to connect WaveStarted signal.</summary>
    public void ConnectWaveManager()
    {
        if (WaveManagerRef != null)
            WaveManagerRef.WaveStarted += OnWaveStarted;
    }

    private void OnWaveStarted(int _waveNumber)
    {
        GameStateRef?.AddCurrency(GameConfig.PowerCoreIncomePerWave);
        GD.Print($"PowerCore: +${GameConfig.PowerCoreIncomePerWave} at wave {_waveNumber}");
    }

    public override void _ExitTree()
    {
        if (WaveManagerRef != null)
            WaveManagerRef.WaveStarted -= OnWaveStarted;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        // Pulsing glow
        float pulse = 0.4f + 0.4f * Mathf.Sin(_time * 3f);
        float radius = GameConfig.TileSize * 0.45f * (0.8f + 0.2f * pulse);
        DrawCircle(Vector2.Zero, radius, new Color(1f, 0.85f, 0f, pulse * 0.4f));

        // 4 rays at 45° angles
        float rayLen = GameConfig.TileSize * 0.55f;
        var rayColor = new Color(1f, 0.9f, 0.1f, 0.85f);
        for (int i = 0; i < 4; i++)
        {
            float angle = _time * 0.8f + i * Mathf.Pi * 0.5f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 inner = dir * 3f;
            Vector2 outer = dir * rayLen;
            DrawLine(inner, outer, rayColor, 1.5f);
        }
    }
}
