using BioFilter.UI;
using Godot;

namespace BioFilter;

/// <summary>
/// Manages wave progression: Idle (build phase) → Preview → Spawning → WaveComplete → Idle.
/// Shows WavePreview for GameConfig.WavePreviewDuration before spawning particles.
/// </summary>
public partial class WaveManager : Node2D
{
    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void WaveStartedEventHandler(int waveNumber);
    [Signal] public delegate void WaveCompleteEventHandler(int waveNumber);
    [Signal] public delegate void GameWonEventHandler();

    // ── State machine ─────────────────────────────────────────────────────────
    public enum WaveState { Idle, Preview, Spawning, WaitingForClear }

    public WaveState State { get; private set; } = WaveState.Idle;

    // ── Wired by Main ─────────────────────────────────────────────────────────
    public ParticleManager? ParticleManagerRef { get; set; }
    public WavePreview?     WavePreviewRef     { get; set; }
    public GameState?       GameStateRef       { get; set; }

    // ── Internal state ────────────────────────────────────────────────────────
    private int _currentWave = 0;          // 0-indexed; 1-indexed for display
    private int _particlesToSpawn = 0;
    private int _particlesSpawned = 0;
    private int _particlesAlive = 0;
    private float _spawnTimer = 0f;
    private float _healthMultiplier = 1f;

    // ── Public accessors ──────────────────────────────────────────────────────
    public int CurrentWaveNumber => _currentWave + 1; // 1-indexed for display
    public int TotalWaves => GameConfig.TotalWaves;

    // ── Wave start ────────────────────────────────────────────────────────────
    /// <summary>Called by StartWaveButton. Shows preview then begins spawning.</summary>
    public void StartWave()
    {
        if (State != WaveState.Idle) return;

        _particlesToSpawn = GameConfig.WaveBaseParticleCount
                            + (_currentWave * GameConfig.WaveParticleCountIncrease);
        _healthMultiplier = GameConfig.WaveHealthMultiplierBase
                            + (_currentWave * GameConfig.WaveHealthMultiplierIncrease);
        _particlesSpawned = 0;
        _particlesAlive = 0;
        _spawnTimer = 0f;

        GameStateRef?.RecordWaveStart();

        if (WavePreviewRef != null)
        {
            State = WaveState.Preview;
            WavePreviewRef.ShowPreview(CurrentWaveNumber, _particlesToSpawn);
            WavePreviewRef.PreviewFinished += OnPreviewFinished;
        }
        else
        {
            BeginSpawning();
        }
    }

    private void OnPreviewFinished()
    {
        if (WavePreviewRef != null)
            WavePreviewRef.PreviewFinished -= OnPreviewFinished;
        BeginSpawning();
    }

    private void BeginSpawning()
    {
        State = WaveState.Spawning;
        EmitSignal(SignalName.WaveStarted, CurrentWaveNumber);

        GD.Print($"[WaveManager] Wave {CurrentWaveNumber}/{TotalWaves} started — " +
                 $"{_particlesToSpawn} particles, health x{_healthMultiplier:F1}");
    }

    // ── Particle tracking ─────────────────────────────────────────────────────
    /// <summary>Call when a particle is removed (dead or escaped).</summary>
    public void OnParticleRemoved()
    {
        _particlesAlive--;
        if (_particlesAlive < 0) _particlesAlive = 0;
        CheckWaveComplete();
    }

    /// <summary>
    /// Called when a CellDivision splits — adds extra particles to the alive counter
    /// so the wave doesn't end prematurely.
    /// </summary>
    public void RegisterExtraParticle()
    {
        _particlesAlive++;
    }

    // ── Enemy type selection ──────────────────────────────────────────────────

    /// <summary>Returns the particle type to spawn for a given spawn index in this wave.</summary>
    private ParticleType GetTypeForSpawn(int spawnIndex)
    {
        int wave = CurrentWaveNumber;

        return wave switch
        {
            // Waves 1-3: BioParticle only
            <= 3 => ParticleType.BioParticle,

            // Wave 4: alternating BioParticle + SporeSpeck
            4 => (spawnIndex % 2 == 0) ? ParticleType.BioParticle : ParticleType.SporeSpeck,

            // Waves 5-6: SporeSpeck + RadiationBlob (1 blob every 3)
            5 or 6 => (spawnIndex % 3 == 0) ? ParticleType.RadiationBlob : ParticleType.SporeSpeck,

            // Wave 7: Armored mix with BacterialSwarm (new special type introduced)
            7 => (spawnIndex % 3) switch
            {
                0 => ParticleType.Armored,
                1 => ParticleType.BacterialSwarm,
                _ => ParticleType.BioParticle,
            },

            // Wave 8: Carrier + CellDivision + SporeSpeck
            8 => (spawnIndex % 3) switch
            {
                0 => ParticleType.Carrier,
                1 => ParticleType.CellDivision,
                _ => ParticleType.SporeSpeck,
            },

            // Wave 9: Saboteur mix with all existing types
            9 => (spawnIndex % 6) switch
            {
                0 => ParticleType.Saboteur,
                1 => ParticleType.Armored,
                2 => ParticleType.BioParticle,
                3 => ParticleType.Carrier,
                4 => ParticleType.RadiationBlob,
                _ => ParticleType.SporeSpeck,
            },

            // Wave 10 boss: all special types — heavy with RadiationBlob
            _ => (spawnIndex % 6) switch
            {
                0 => ParticleType.RadiationBlob,
                1 => ParticleType.Armored,
                2 => ParticleType.Carrier,
                3 => ParticleType.Saboteur,
                4 => ParticleType.CellDivision,
                _ => ParticleType.BacterialSwarm,
            },
        };
    }

    /// <summary>Health multiplier override for specific waves/types (boss wave 10).</summary>
    private float GetHealthMultForType(ParticleType type)
    {
        if (CurrentWaveNumber >= 10 && type == ParticleType.RadiationBlob)
            return 2.0f; // boss wave: flat 2x base health (prevents ~6000 HP blobs)
        return _healthMultiplier;
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        if (State == WaveState.Spawning)
        {
            _spawnTimer += (float)delta;
            if (_spawnTimer >= GameConfig.SpawnInterval && _particlesSpawned < _particlesToSpawn)
            {
                _spawnTimer = 0f;

                var type = GetTypeForSpawn(_particlesSpawned);
                float hm = GetHealthMultForType(type);

                int aliveCount = ParticleManagerRef?.SpawnParticle(hm, type) ?? 1;
                _particlesAlive += aliveCount;
                _particlesSpawned++;

                if (_particlesSpawned >= _particlesToSpawn)
                    State = WaveState.WaitingForClear;
            }
        }
    }

    // ── Wave completion ───────────────────────────────────────────────────────
    private void CheckWaveComplete()
    {
        if (State != WaveState.WaitingForClear) return;
        if (_particlesAlive > 0) return;

        int completedWave = _currentWave;
        _currentWave++;

        GD.Print($"[WaveManager] Wave {completedWave + 1} complete.");

        GameStateRef?.AwardWaveBonuses();

        if (_currentWave >= GameConfig.TotalWaves)
        {
            State = WaveState.Idle;
            EmitSignal(SignalName.WaveComplete, completedWave + 1);
            EmitSignal(SignalName.GameWon);
            GD.Print("[WaveManager] All waves survived — game won!");
        }
        else
        {
            State = WaveState.Idle;
            EmitSignal(SignalName.WaveComplete, completedWave + 1);
        }
    }
}
