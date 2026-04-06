namespace BioFilter;

/// <summary>
/// Central configuration for all tunable game variables.
/// Adjust values here — no hunting through multiple files.
/// </summary>
public static class GameConfig
{
    // ─── Grid ───────────────────────────────────────────────────────────────
    public const int GridWidth = 32;
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

    // ─── Build Menu ───────────────────────────────────────────────────────────
    public const int BuildMenuItemCount  = 8;   // wall + 7 tower types
    public const int BuildMenuWidth      = 220;  // popup width in pixels
    public const int BuildMenuItemHeight = 44;   // height per item row
    public const int BuildMenuTitleHeight = 24;  // height of title bar
    public const int BuildMenuBottomMargin = 50; // pixels above bottom of screen

    // ─── UI Layout ─────────────────────────────────────────────────────────────
    public const int TopBarHeight    = 40;  // pixels (legacy)
    public const int BottomBarHeight = 40;  // pixels (legacy)
    public const int TopStripHeight  = 24;  // widescreen HUD top strip
    public const int RightPanelWidth = 128; // widescreen HUD right panel
    // Grid area: 512×320 (32*16 × 20*16), positioned at (0, TopStripHeight)

    // ─── Economy (Refund) ────────────────────────────────────────────────────
    public const float RefundPercent = 0.5f; // 50% refund on tower removal

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

    // ─── Hotkeys ─────────────────────────────────────────────────────────────
    public const string HotkeyBasicFilter   = "hotkey_basic_filter";     // Key 1
    public const string HotkeyElectrostatic = "hotkey_electrostatic";    // Key 2
    public const string HotkeyUVSteriliser  = "hotkey_uv_steriliser";    // Key 3
    public const string HotkeyWallMode      = "hotkey_wall_mode";         // Key W
    public const string HotkeyDeselect      = "hotkey_deselect";          // Key R

    // ─── Range Preview ──────────────────────────────────────────────────────
    public const float RangePreviewAlpha = 0.25f;

    // ─── Wave Preview ──────────────────────────────────────────────────────
    public const float WavePreviewDuration = 3.0f;

    // ─── Wave Bonuses ───────────────────────────────────────────────────────
    public const int PerfectWaveBonus    = 50;   // +$50 for 0 escaped particles
    public const int EfficiencyBonus     = 25;   // +$25 if airflow > threshold all wave
    public const float EfficiencyAirflowThreshold = 0.60f; // 60% airflow threshold for efficiency bonus
    public const float BonusNotificationDuration  = 2.5f;  // seconds bonus text fades

    // ─── Speed Button ───────────────────────────────────────────────────────
    public const float SpeedNormal = 1.0f;
    public const float SpeedFast   = 2.0f;

    // ─── Airflow Warning ────────────────────────────────────────────────────
    public const float AirflowWarnFlashThreshold  = 0.30f; // 30% → flash meter red
    public const float AirflowCriticalThreshold   = 0.20f; // 20% → vignette

    // ─── New Filter Modules (Sprint 12) ──────────────────────────────────────
    public const int VortexSeparatorCost = 125;
    public const float VortexSeparatorRange = 3f;
    public const int PowerCoreCost = 150;
    public const int PowerCoreIncomePerWave = 5;
    public const int BioNeutraliserCost = 100;
    public const float BioNeutraliserBoost = 1.25f;
    public const int MagneticCageCost = 175;
    public const float MagneticCageHoldSeconds = 2.0f;
    public const float MagneticCageRange = 2.5f;
    public const float VortexPenaltyWeight = 6f;   // extra A* cost per tile near vortex

    // ─── Enemy Types (Sprint 11) ─────────────────────────────────────────────
    // SporeSpeck — fast scout
    public const float SporeSpeckHealth = 30f;
    public const float SporeSpeckSpeed  = 3.0f;

    // RadiationBlob — slow tank (immune to slow)
    public const float RadiationBlobHealth = 400f;
    public const float RadiationBlobSpeed  = 0.8f;

    // BacterialSwarm — spawns SwarmUnit particles
    public const float SwarmUnitHealth = 15f;
    public const float SwarmUnitSpeed  = 2.0f;
    public const int   SwarmUnitCount  = 8;

    // CellDivision — splits on death into 2 children
    public const float CellDivisionHealth      = 80f;
    public const float CellDivisionChildHealth = 30f;
    public const float CellDivisionSpeed       = 1.5f;
}
