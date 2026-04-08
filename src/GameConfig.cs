namespace BioFilter;

/// <summary>
/// Central configuration for all tunable game variables.
/// Adjust values here — no hunting through multiple files.
/// </summary>
public static class GameConfig
{
    // ─── Grid ───────────────────────────────────────────────────────────────
    /// <summary>Number of tile columns in the game grid.</summary>
    public const int GridWidth = 32;
    /// <summary>Number of tile rows in the game grid.</summary>
    public const int GridHeight = 20;
    /// <summary>Size of each tile in pixels.</summary>
    public const int TileSize = 16;

    // ─── Spawn & Exit ───────────────────────────────────────────────────────
    /// <summary>Column index where particles spawn (left edge).</summary>
    public const int SpawnCol = 0;
    /// <summary>Row index where particles spawn (Map 1 / primary).</summary>
    public const int SpawnRow = 10;
    // Exit = entire right edge (col GridWidth - 1)

    // ─── Map 2 Spawn Points ─────────────────────────────────────────────────
    /// <summary>Map 2 first spawn column (left edge).</summary>
    public const int Map2Spawn1Col = 0;
    /// <summary>Map 2 first spawn row (top-left area).</summary>
    public const int Map2Spawn1Row = 5;
    /// <summary>Map 2 second spawn column (left edge).</summary>
    public const int Map2Spawn2Col = 0;
    /// <summary>Map 2 second spawn row (bottom-left area).</summary>
    public const int Map2Spawn2Row = 15;

    // ─── Airflow ────────────────────────────────────────────────────────────
    /// <summary>Minimum airflow fraction (30%) before placement is blocked.</summary>
    public const float AirflowMinPercent = 0.30f;
    /// <summary>Corridor width (in tiles) considered high airflow (low restriction).</summary>
    public const int ChokeWidthHigh = 3;
    /// <summary>Corridor width (in tiles) considered medium airflow restriction.</summary>
    public const int ChokeWidthMedium = 2;
    // 1 tile wide = high restriction
    /// <summary>A* extra cost weight for a high-airflow (wide) corridor tile.</summary>
    public const float ChokeWeightHigh = 0.1f;
    /// <summary>A* extra cost weight for a medium-airflow corridor tile.</summary>
    public const float ChokeWeightMedium = 0.25f;
    /// <summary>A* extra cost weight for a low-airflow (1-tile-wide) corridor tile.</summary>
    public const float ChokeWeightLow = 0.6f;

    // ─── Particles ──────────────────────────────────────────────────────────
    /// <summary>Standard BioParticle movement speed in tiles per second.</summary>
    public const float ParticleBaseSpeed = 1.5f;
    /// <summary>
    /// Base health of a wave-1 BioParticle.
    /// A single Basic Filter (DPS = BasicFilterDamage / BasicFilterTickRate = 16)
    /// should kill this in ~3 seconds while the particle is in range.
    /// </summary>
    public const float ParticleBaseHealth = 48f;
    /// <summary>Speed multiplier applied to a particle while it is slowed (used by MagneticCage).</summary>
    public const float ParticleSlowFactor = 0.5f;
    /// <summary>Lerp weight for particle steering — higher = tighter turns, lower = more momentum.</summary>
    public const float ParticleSteeringWeight = 0.7f;

    // ─── Economy ────────────────────────────────────────────────────────────
    /// <summary>Currency the player starts with. Must be exactly divisible into 4 Basic Filters (4 × $50).</summary>
    public const int StartingCurrency = 200;
    /// <summary>Currency awarded to the player for each particle killed by a tower.</summary>
    public const int CurrencyPerKill = 12;

    // ─── Lives ──────────────────────────────────────────────────────────────
    /// <summary>Starting population (lives). Game over when this reaches 0.</summary>
    public const int StartingPopulation = 100;
    /// <summary>Population lost for each particle that reaches the exit.</summary>
    public const int PopLostPerParticle = 1;

    // ─── Waves ──────────────────────────────────────────────────────────────
    /// <summary>Total number of waves in a full game session.</summary>
    public const int TotalWaves = 10;
    /// <summary>Number of particles spawned in wave 1.</summary>
    public const int WaveBaseParticleCount = 10;
    /// <summary>Additional particles added per wave beyond wave 1.</summary>
    public const int WaveParticleCountIncrease = 5;
    /// <summary>Health multiplier applied to all particles at wave 1 (baseline = 1.0).</summary>
    public const float WaveHealthMultiplierBase = 1.0f;
    /// <summary>
    /// Health multiplier increase per wave. At 0.30 per wave:
    /// W1=1.0×, W3=1.6×, W6=2.5×, W10=3.7×.
    /// Produces a smooth easy→hard curve without exponential blowup.
    /// </summary>
    public const float WaveHealthMultiplierIncrease = 0.30f;
    /// <summary>
    /// Gap in seconds between particle spawns within a wave.
    /// Must be ≥ 1.2 s to give the player time to react to each particle.
    /// </summary>
    public const float SpawnInterval = 1.8f; // increased for more space between particles // increased for more space between particles

    // ─── Build Menu ───────────────────────────────────────────────────────────
    /// <summary>Total items in the build popup (1 wall + 7 tower types).</summary>
    public const int BuildMenuItemCount  = 10;
    /// <summary>Pixel width of the build popup panel.</summary>
    public const int BuildMenuWidth      = 220;
    /// <summary>Pixel height of each item row in the build popup.</summary>
    public const int BuildMenuItemHeight = 44;
    /// <summary>Pixel height of the title bar in the build popup.</summary>
    public const int BuildMenuTitleHeight = 24;
    /// <summary>Minimum pixels from the bottom of the screen to the bottom of the build popup.</summary>
    public const int BuildMenuBottomMargin = 50;

    // ─── UI Layout ─────────────────────────────────────────────────────────────
    /// <summary>Height in pixels of the legacy top bar (kept for backward compatibility).</summary>
    public const int TopBarHeight    = 40;
    /// <summary>Height in pixels of the legacy bottom bar (kept for backward compatibility).</summary>
    public const int BottomBarHeight = 40;
    /// <summary>Height in pixels of the widescreen HUD top strip.</summary>
    public const int TopStripHeight  = 24;
    /// <summary>Width in pixels of the widescreen HUD right panel.</summary>
    public const int RightPanelWidth = 128;
    // Grid area: 512×320 (32*16 × 20*16), positioned at (0, TopStripHeight)

    // ─── Economy (Refund) ────────────────────────────────────────────────────
    /// <summary>Fraction of the original tower cost returned on removal (50% refund).</summary>
    public const float RefundPercent = 0.5f;

    // ─── Towers ─────────────────────────────────────────────────────────────
    /// <summary>Build cost of the Basic Filter tower. Four of these cost exactly StartingCurrency ($200).</summary>
    public const int BasicFilterCost = 50;
    /// <summary>
    /// Damage per tick dealt by the Basic Filter to all particles in range.
    /// DPS = BasicFilterDamage / BasicFilterTickRate = 8 / 0.5 = 16.
    /// At wave 1 (ParticleBaseHealth = 48 HP), kills in exactly 3 seconds.
    /// </summary>
    public const float BasicFilterDamage = 8f;
    /// <summary>Damage aura radius of the Basic Filter in tiles.</summary>
    public const float BasicFilterRange = 2f;
    /// <summary>Seconds between each damage tick of the Basic Filter (lower = faster).</summary>
    public const float BasicFilterTickRate = 0.5f;

    /// <summary>Build cost of the Electrostatic tower.</summary>
    public const int ElectrostaticCost = 75;
    /// <summary>Speed multiplier applied to particles slowed by the Electrostatic tower (0.5 = 50% speed).</summary>
    public const float ElectrostaticSlowPercent = 0.5f;
    /// <summary>Slow aura radius of the Electrostatic tower in tiles.</summary>
    public const float ElectrostaticRange = 2f;

    /// <summary>Build cost of the UV Steriliser tower.</summary>
    public const int UVSteriliserCost = 100;
    /// <summary>
    /// Damage per projectile fired by the UV Steriliser.
    /// Must be ≥ wave-3 BioParticle health (48 × 1.6 = 76.8) to guarantee a one-shot kill on waves 1–3.
    /// </summary>
    public const float UVSteriliserDamage = 80f;
    /// <summary>Targeting range of the UV Steriliser in tiles.</summary>
    public const float UVSteriliserRange = 4f;
    /// <summary>Projectiles fired per second by the UV Steriliser.</summary>
    public const float UVSteriliserFireRate = 1.0f;

    // ─── Upgrade multipliers ─────────────────────────────────────────────────
    /// <summary>Tower upgrade cost = base cost × this multiplier.</summary>
    public const float UpgradeCostMultiplier = 2.0f;
    /// <summary>Damage multiplier applied on tower upgrade.</summary>
    public const float UpgradeDamageMultiplier = 1.8f;
    /// <summary>Range multiplier applied on tower upgrade.</summary>
    public const float UpgradeRangeMultiplier = 1.3f;
    /// <summary>Slow-percent multiplier applied to Electrostatic on upgrade (capped at ~75% slow in practice).</summary>
    public const float UpgradeSlowMultiplier = 1.5f;
    /// <summary>Fire-rate multiplier applied on tower upgrade (UV Steriliser).</summary>
    public const float UpgradeFireRateMultiplier = 1.6f;

    // ─── Hotkeys ─────────────────────────────────────────────────────────────
    /// <summary>Godot input action name for selecting the Basic Filter (Key 1).</summary>
    public const string HotkeyBasicFilter   = "hotkey_basic_filter";
    /// <summary>Godot input action name for selecting the Electrostatic tower (Key 2).</summary>
    public const string HotkeyElectrostatic = "hotkey_electrostatic";
    /// <summary>Godot input action name for selecting the UV Steriliser (Key 3).</summary>
    public const string HotkeyUVSteriliser  = "hotkey_uv_steriliser";
    /// <summary>Godot input action name for switching to wall placement mode (Key W).</summary>
    public const string HotkeyWallMode      = "hotkey_wall_mode";
    /// <summary>Godot input action name for deselecting the current tower (Key R).</summary>
    public const string HotkeyDeselect      = "hotkey_deselect";

    // ─── Range Preview ──────────────────────────────────────────────────────
    /// <summary>Alpha transparency of the tower range preview overlay circle.</summary>
    public const float RangePreviewAlpha = 0.25f;

    // ─── Wave Preview ──────────────────────────────────────────────────────
    /// <summary>How long in seconds the wave preview banner is displayed before spawning begins.</summary>
    public const float WavePreviewDuration = 3.0f;

    // ─── Wave Bonuses ───────────────────────────────────────────────────────
    /// <summary>Bonus currency awarded when 0 particles escape and no population is lost in a wave.</summary>
    public const int PerfectWaveBonus    = 50;
    /// <summary>Maximum bonus currency for maintaining airflow above the efficiency threshold all wave.</summary>
    public const int EfficiencyBonus     = 25;
    /// <summary>Average airflow fraction required throughout a wave to qualify for the efficiency bonus.</summary>
    public const float EfficiencyAirflowThreshold = 0.60f;
    /// <summary>Duration in seconds that bonus notification text is shown before fading.</summary>
    public const float BonusNotificationDuration  = 2.5f;

    // ─── Speed Button ───────────────────────────────────────────────────────
    /// <summary>Normal game speed multiplier (1× real time).</summary>
    public const float SpeedNormal = 1.0f;
    /// <summary>Fast game speed multiplier (2× real time).</summary>
    public const float SpeedFast   = 2.0f;

    // ─── Airflow Warning ────────────────────────────────────────────────────
    /// <summary>Airflow fraction at which the airflow meter begins flashing red (warning).</summary>
    public const float AirflowWarnFlashThreshold  = 0.30f;
    /// <summary>Airflow fraction at which the screen vignette activates (critical).</summary>
    public const float AirflowCriticalThreshold   = 0.20f;

    // ─── New Filter Modules ───────────────────────────────────────────────────
    /// <summary>Build cost of the Vortex Separator tower.</summary>
    public const int VortexSeparatorCost = 125;
    /// <summary>Radius in tiles over which the Vortex Separator applies its A* detour penalty.</summary>
    public const float VortexSeparatorRange = 3f;
    /// <summary>Build cost of the Power Core passive income tower.</summary>
    public const int PowerCoreCost = 150;
    /// <summary>Currency added to the player's balance at the start of each wave per Power Core placed.</summary>
    public const int PowerCoreIncomePerWave = 5;
    /// <summary>Build cost of the Bio Neutraliser support tower.</summary>
    public const int BioNeutraliserCost = 100;
    /// <summary>Damage multiplier applied to adjacent towers by the Bio Neutraliser (1.25 = +25% damage).</summary>
    public const float BioNeutraliserBoost = 1.25f;
    /// <summary>Visual range preview radius (in tiles) shown when selecting the Bio Neutraliser; covers the 8 adjacent tiles.</summary>
    public const float BioNeutraliserRange = 1.5f;
    /// <summary>Build cost of the Magnetic Cage trap tower.</summary>
    public const int MagneticCageCost = 175;
    /// <summary>Duration in seconds a particle is held immobile by the Magnetic Cage before being released.</summary>
    public const float MagneticCageHoldSeconds = 2.0f;
    /// <summary>Radius in tiles within which the Magnetic Cage captures particles.</summary>
    public const float MagneticCageRange = 2.5f;
    /// <summary>Extra A* pathfinding cost added per tile near a Vortex Separator, encouraging particles to reroute.</summary>
    public const float VortexPenaltyWeight = 6f;

    // ─── Enemy Types ─────────────────────────────────────────────────────────
    // SporeSpeck — fast scout (quarter currency reward; only half-affected by slow)
    /// <summary>Base health of the SporeSpeck fast scout particle.</summary>
    public const float SporeSpeckHealth = 30f;
    /// <summary>Movement speed of the SporeSpeck in tiles per second.</summary>
    public const float SporeSpeckSpeed  = 3.0f;

    // RadiationBlob — slow tank (immune to slow effects)
    /// <summary>
    /// Base health of the RadiationBlob tank particle.
    /// At wave 10, boss multiplier (2×) yields 480 HP — requiring 3–4 Basic Filters
    /// to kill before the blob crosses the 28-tile corridor at 0.8 t/s.
    /// </summary>
    public const float RadiationBlobHealth = 240f;
    /// <summary>Movement speed of the RadiationBlob in tiles per second (slow by design).</summary>
    public const float RadiationBlobSpeed  = 0.8f;

    // BacterialSwarm — meta-type: spawns SwarmUnitCount individual SwarmUnit particles
    /// <summary>Health of each SwarmUnit spawned by a BacterialSwarm.</summary>
    public const float SwarmUnitHealth = 15f;
    /// <summary>Movement speed of each SwarmUnit in tiles per second.</summary>
    public const float SwarmUnitSpeed  = 2.0f;
    /// <summary>Number of SwarmUnit particles spawned per BacterialSwarm.</summary>
    public const int   SwarmUnitCount  = 8;

    // Armored — resistant to BasicFilter, weak to UV
    /// <summary>Base health of the Armored particle.</summary>
    public const float ArmoredHealth           = 120f;
    /// <summary>Movement speed of the Armored particle in tiles per second.</summary>
    public const float ArmoredSpeed            = 1.2f;
    /// <summary>Damage multiplier when Armored takes damage from BasicFilter (30% = 70% resistance).</summary>
    public const float ArmorBasicFilterResist  = 0.3f;
    /// <summary>Damage multiplier when Armored takes damage from UV Steriliser (150% = vulnerability).</summary>
    public const float ArmorUVBonus            = 1.5f;

    // Carrier — releases mini-particles on death
    /// <summary>Base health of the Carrier particle.</summary>
    public const float CarrierHealth           = 60f;
    /// <summary>Movement speed of the Carrier particle in tiles per second.</summary>
    public const float CarrierSpeed            = 1.0f;

    // Saboteur — disables the tower that kills it
    /// <summary>Base health of the Saboteur particle.</summary>
    public const float SaboteurHealth          = 80f;
    /// <summary>Movement speed of the Saboteur particle in tiles per second.</summary>
    public const float SaboteurSpeed           = 1.3f;
    /// <summary>Duration in seconds that a tower is disabled after being killed by a Saboteur.</summary>
    public const float SaboteurDisableDuration = 5f;

    // CellDivision — splits on death into 2 smaller child particles
    /// <summary>Health of a CellDivision parent particle.</summary>
    public const float CellDivisionHealth      = 80f;
    /// <summary>Health of each child particle spawned when a CellDivision parent dies.</summary>
    public const float CellDivisionChildHealth = 30f;
    /// <summary>Movement speed of CellDivision particles (parent and children) in tiles per second.</summary>
    public const float CellDivisionSpeed       = 1.5f;

    // ─── Effects ────────────────────────────────────────────────────────────
    /// <summary>Duration in seconds of the particle death splash animation.</summary>
    public const float SplashDuration             = 0.4f;
    /// <summary>Duration in seconds of the RadiationBlob shockwave effect on death.</summary>
    public const float RadBlobShockwaveDuration    = 0.5f;
    /// <summary>Duration in seconds of the CellDivision split flash effect.</summary>
    public const float SplitFlashDuration         = 0.3f;

    // ─── Toxic Sprayer (DoT tower) ────────────────────────────────────────────
    /// <summary>Build cost in credits.</summary>
    public const int   ToxicSprayerCost        = 125;
    /// <summary>Damage per DoT tick.</summary>
    public const float ToxicSprayerDotDamage   = 5f;
    /// <summary>Seconds between DoT ticks.</summary>
    public const float ToxicSprayerDotTickRate = 0.5f;
    /// <summary>Total poison duration in seconds.</summary>
    public const float ToxicSprayerDotDuration = 4f;
    /// <summary>Detection range in tiles.</summary>
    public const float ToxicSprayerRange       = 2.5f;

    // ─── Plasma Burst (AoE tower) ─────────────────────────────────────────────
    /// <summary>Build cost in credits.</summary>
    public const int   PlasmaBurstCost     = 175;
    /// <summary>Damage per explosion hit.</summary>
    public const float PlasmaBurstDamage   = 60f;
    /// <summary>Explosion radius in tiles.</summary>
    public const float PlasmaBurstRadius   = 2.5f;
    /// <summary>Maximum targeting range in tiles.</summary>
    public const float PlasmaBurstRange    = 5f;
    /// <summary>Seconds between shots.</summary>
    public const float PlasmaBurstFireRate = 2.0f;

}