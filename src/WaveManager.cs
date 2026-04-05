using Godot;

namespace BioFilter;

/// <summary>
/// Manages wave progression: Idle (build phase) → Spawning → WaveComplete → Idle.
/// Drives ParticleManager spawning and tracks wave completion.
/// </summary>
public partial class WaveManager : Node2D
{
    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void WaveStartedEventHandler(int waveNumber);
    [Signal] public delegate void WaveCompleteEventHandler(int waveNumber);
    [Signal] public delegate void GameWonEventHandler();

    // ── State machine ─────────────────────────────────────────────────────────
    public enum WaveState { Idle, Spawning, WaitingForClear }

    public WaveState State { get; private set; } = WaveState.Idle;

    // ── Wired by Main ─────────────────────────────────────────────────────────
    public ParticleManager ParticleManagerRef { get; set; }

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
    /// <summary>Called by StartWaveButton. Begins spawning for the next wave.</summary>
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
        CheckWaveComplete();
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
                ParticleManagerRef?.SpawnParticle(_healthMultiplier);
                _particlesSpawned++;
                _particlesAlive++;

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

        // Wave is fully cleared
        int completedWave = _currentWave;
        _currentWave++;

        GD.Print($"[WaveManager] Wave {completedWave + 1} complete.");

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
