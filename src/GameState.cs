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

    // ── Stats tracking (Sprint 13C) ──────────────────────────────────────────
    public int ParticlesKilled  { get; private set; } = 0;
    public int WavesSurvived    { get; private set; } = 0;
    public float CurrentAirflow { get; private set; } = 1.0f;

    // ── Wave tracking ────────────────────────────────────────────────────────
    private int _particlesEscapedThisWave = 0;
    private int _populationAtWaveStart = 0;
    private float _totalAirflowThisWave = 0f;
    private int _airflowSampleCount = 0;

    [Signal] public delegate void PopulationChangedEventHandler(int newValue);
    [Signal] public delegate void CurrencyChangedEventHandler(int newValue);
    [Signal] public delegate void GameOverEventHandler();
    [Signal] public delegate void BonusEarnedEventHandler(string message, int amount);

    public void RestoreState(int population, int currency)
    {
        Population = population;
        Currency = currency;
        EmitSignal(SignalName.PopulationChanged, Population);
        EmitSignal(SignalName.CurrencyChanged, Currency);
    }

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
        _populationAtWaveStart = Population;
        _totalAirflowThisWave = 0f;
        _airflowSampleCount = 0;
    }

    /// <summary>Called whenever a particle reaches the exit.</summary>
    public void RecordParticleEscaped()
    {
        _particlesEscapedThisWave++;
    }

    /// <summary>Called each frame during a wave to track minimum airflow.</summary>
    public void RecordAirflow(float airflow)
    {
        CurrentAirflow = airflow;
        _totalAirflowThisWave += airflow;
        _airflowSampleCount++;
    }

    /// <summary>Called when a particle is destroyed by a tower.</summary>
    public void RecordParticleKilled() => ParticlesKilled++;

    /// <summary>Called when a wave is completed successfully.</summary>
    public void RecordWaveSurvived() => WavesSurvived++;

    /// <summary>
    /// Called at wave complete. Calculates and awards bonuses.
    /// Returns total bonus awarded.
    /// </summary>
    public int AwardWaveBonuses()
    {
        int total = 0;

        bool lostPopThisWave = Population < _populationAtWaveStart;
        float avgAirflow = _airflowSampleCount > 0 ? _totalAirflowThisWave / _airflowSampleCount : 1.0f;

        // Perfect wave: 0 particles escaped AND no population lost
        if (_particlesEscapedThisWave == 0 && !lostPopThisWave)
        {
            AddCurrency(GameConfig.PerfectWaveBonus);
            total += GameConfig.PerfectWaveBonus;
            EmitSignal(SignalName.BonusEarned, "+50 PERFECT WAVE!", GameConfig.PerfectWaveBonus);
        }

        // Efficiency bonus: average airflow above threshold AND no population lost
        if (avgAirflow >= GameConfig.EfficiencyAirflowThreshold && !lostPopThisWave)
        {
            // Scale bonus by airflow average (100% airflow = full bonus, 60% = minimum)
            float scale = (avgAirflow - GameConfig.EfficiencyAirflowThreshold) / (1f - GameConfig.EfficiencyAirflowThreshold);
            int bonus = (int)(GameConfig.EfficiencyBonus * (0.5f + scale * 0.5f));
            AddCurrency(bonus);
            total += bonus;
            EmitSignal(SignalName.BonusEarned, $"+{bonus} EFFICIENCY!", bonus);
        }

        return total;
    }
}
