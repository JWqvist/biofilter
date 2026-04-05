using Godot;

namespace BioFilter;

/// <summary>
/// Tracks population, currency, and wave statistics (escaped particles, airflow).
/// Emits signals for HUD updates, game-over condition, and bonus notifications.
/// </summary>
public partial class GameState : Node
{
    public int Population { get; private set; } = GameConfig.StartingPopulation;
    public int Currency { get; private set; } = GameConfig.StartingCurrency;

    // ── Wave tracking ────────────────────────────────────────────────────────
    private int _particlesEscapedThisWave = 0;
    private float _minAirflowThisWave = 1.0f;

    [Signal] public delegate void PopulationChangedEventHandler(int newValue);
    [Signal] public delegate void CurrencyChangedEventHandler(int newValue);
    [Signal] public delegate void GameOverEventHandler();
    [Signal] public delegate void BonusEarnedEventHandler(string message, int amount);

    public void LosePopulation(int amount)
    {
        Population -= amount;
        if (Population < 0) Population = 0;
        EmitSignal(SignalName.PopulationChanged, Population);
        if (Population <= 0)
            EmitSignal(SignalName.GameOver);
    }

    public void AddCurrency(int amount)
    {
        Currency += amount;
        EmitSignal(SignalName.CurrencyChanged, Currency);
    }

    /// <summary>Returns true and deducts if there's enough currency; false otherwise.</summary>
    public bool SpendCurrency(int amount)
    {
        if (Currency < amount) return false;
        Currency -= amount;
        EmitSignal(SignalName.CurrencyChanged, Currency);
        return true;
    }

    // ── Wave bonus tracking ──────────────────────────────────────────────────

    /// <summary>Called at the beginning of each wave to reset tracking.</summary>
    public void RecordWaveStart()
    {
        _particlesEscapedThisWave = 0;
        _minAirflowThisWave = 1.0f;
    }

    /// <summary>Called whenever a particle reaches the exit.</summary>
    public void RecordParticleEscaped()
    {
        _particlesEscapedThisWave++;
    }

    /// <summary>Called each frame during a wave to track minimum airflow.</summary>
    public void RecordAirflow(float airflow)
    {
        if (airflow < _minAirflowThisWave)
            _minAirflowThisWave = airflow;
    }

    /// <summary>
    /// Called at wave complete. Calculates and awards bonuses.
    /// Returns total bonus awarded.
    /// </summary>
    public int AwardWaveBonuses()
    {
        int total = 0;

        if (_particlesEscapedThisWave == 0)
        {
            AddCurrency(GameConfig.PerfectWaveBonus);
            total += GameConfig.PerfectWaveBonus;
            EmitSignal(SignalName.BonusEarned, "+50 PERFECT WAVE!", GameConfig.PerfectWaveBonus);
        }

        if (_minAirflowThisWave >= GameConfig.EfficiencyAirflowThreshold)
        {
            AddCurrency(GameConfig.EfficiencyBonus);
            total += GameConfig.EfficiencyBonus;
            EmitSignal(SignalName.BonusEarned, "+25 EFFICIENCY BONUS!", GameConfig.EfficiencyBonus);
        }

        return total;
    }
}
