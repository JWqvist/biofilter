namespace BioFilter;

/// <summary>
/// Central configuration for all tunable game variables.
/// Adjust values here — no hunting through multiple files.
/// </summary>
public static class GameConfig
{
    // ─── Grid ───────────────────────────────────────────────────────────────
    public const int GridWidth = 30;
    public const int GridHeight = 20;
    public const int TileSize = 16; // pixels per tile

    // ─── Spawn & Exit ───────────────────────────────────────────────────────
    public const int SpawnCol = 0;
    public const int SpawnRow = 10;
    // Exit = entire right edge (col GridWidth - 1)

    // ─── Airflow ────────────────────────────────────────────────────────────
    public const float AirflowMinPercent = 0.20f;    // 20% minimum before building blocked
    public const int ChokeWidthHigh = 3;             // 3+ tiles wide = low restriction
    public const int ChokeWidthMedium = 2;           // 2 tiles wide = medium restriction
    // 1 tile wide = high restriction
    public const float ChokeWeightHigh = 0.1f;
    public const float ChokeWeightMedium = 0.25f;
    public const float ChokeWeightLow = 0.6f;

    // ─── Particles ──────────────────────────────────────────────────────────────────────
    public const float ParticleBaseSpeed = 1.5f;     // tiles per second
    public const float ParticleBaseHealth = 100f;    // was 30 — tripled for better scaling
    public const float ParticleSlowFactor = 0.5f;    // multiplier when slowed
    public const float ParticleSteeringWeight = 0.7f; // how much momentum affects turning

    // ─── Economy ────────────────────────────────────────────────────────────
    public const int StartingCurrency = 200;
    public const int CurrencyPerKill = 12;           // was 10 — slightly more income to encourage building

    // ─── Lives ──────────────────────────────────────────────────────────────
    public const int StartingPopulation = 100;
    public const int PopLostPerParticle = 1;

    // ─── Waves ──────────────────────────────────────────────────────────────
    public const int TotalWaves = 10;
    public const int WaveBaseParticleCount = 10;
    public const int WaveParticleCountIncrease = 5;  // +5 per wave
    public const float WaveHealthMultiplierBase = 1.0f;
    public const float WaveHealthMultiplierIncrease = 0.45f; // was 0.3 — steeper ramp: W10 at 5.05x
    public const float SpawnInterval = 1.0f;         // seconds between particle spawns

    // ─── Towers ─────────────────────────────────────────────────────────────
    public const int BasicFilterCost = 50;
    public const float BasicFilterDamage = 8f;       // was 10 — lower to require more towers
    public const float BasicFilterRange = 2f;        // tiles
    public const float BasicFilterTickRate = 0.5f;   // damage ticks per second

    public const int ElectrostaticCost = 75;
    public const float ElectrostaticSlowPercent = 0.5f; // 50% slow
    public const float ElectrostaticRange = 2f;

    public const int UVSteriliserCost = 100;
    public const float UVSteriliserDamage = 15f;     // was 20 — lower, but fires fast
    public const float UVSteriliserRange = 4f;
    public const float UVSteriliserFireRate = 1.2f;  // was 1.0 — slightly faster

    // ─── Upgrade multipliers ─────────────────────────────────────────────────
    public const float UpgradeCostMultiplier = 2.0f;
    public const float UpgradeDamageMultiplier = 1.8f;
    public const float UpgradeRangeMultiplier = 1.3f;
    public const float UpgradeSlowMultiplier = 1.5f;
    public const float UpgradeFireRateMultiplier = 1.6f;
}
