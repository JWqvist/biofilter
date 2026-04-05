using System.Collections.Generic;
using BioFilter.Effects;
using Godot;

namespace BioFilter;

/// <summary>
/// Particle type determines stats, visuals, and special behaviours.
/// </summary>
public enum ParticleType
{
    BioParticle,   // standard
    SporeSpeck,    // fast scout
    RadiationBlob, // slow tank; immune to slow
    BacterialSwarm,// meta-type: ParticleManager spawns 8 SwarmUnits instead
    SwarmUnit,     // individual unit spawned by BacterialSwarm
    CellDivision,  // splits into 2 children on death
}

/// <summary>
/// A bio-particle that follows a pre-calculated path through the filter grid.
/// Smooth movement with momentum — no instant 90° turns.
/// </summary>
public partial class Particle : Node2D
{
    // ── State ────────────────────────────────────────────────────────────────
    public float Health { get; private set; }
    public float Speed  { get; private set; }
    public float SlowMultiplier { get; set; } = 1.0f;
    public ParticleType Type { get; private set; } = ParticleType.BioParticle;
    public bool IsDivisionChild { get; private set; } = false;
    private bool _isDead = false;

    // ── Path ─────────────────────────────────────────────────────────────────
    private List<Vector2>? _path;
    private int _waypointIndex = 0;
    private Vector2 _velocity = Vector2.Zero;

    private const float WaypointReachRadius = 2.0f; // pixels

    // ── Visuals (set per-type in Initialize) ──────────────────────────────────
    private Color _color = Constants.Colors.BioParticle;
    private float _visualSize = GameConfig.TileSize * 0.6f;

    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void ReachedExitEventHandler();
    [Signal] public delegate void DiedEventHandler(int currencyReward);

    // ── Public API ────────────────────────────────────────────────────────────
    public void Initialize(
        List<Vector2> worldPath,
        float healthMultiplier = 1.0f,
        ParticleType type = ParticleType.BioParticle,
        bool isDivisionChild = false)
    {
        Type = type;
        IsDivisionChild = isDivisionChild;
        _path = worldPath;
        _isDead = false;
        _waypointIndex = 0;
        _velocity = Vector2.Zero;

        // Per-type stats & visuals
        switch (type)
        {
            case ParticleType.SporeSpeck:
                Health      = GameConfig.SporeSpeckHealth * healthMultiplier;
                Speed       = GameConfig.SporeSpeckSpeed;
                _color      = new Color("#aaff00");
                _visualSize = GameConfig.TileSize * 0.4f;
                break;

            case ParticleType.RadiationBlob:
                Health      = GameConfig.RadiationBlobHealth * healthMultiplier;
                Speed       = GameConfig.RadiationBlobSpeed;
                _color      = new Color("#ff8c00");
                _visualSize = GameConfig.TileSize * 0.9f;
                break;

            case ParticleType.SwarmUnit:
                Health      = GameConfig.SwarmUnitHealth * healthMultiplier;
                Speed       = GameConfig.SwarmUnitSpeed;
                _color      = new Color("#88ff44");
                _visualSize = GameConfig.TileSize * 0.3f;
                break;

            case ParticleType.CellDivision:
                Health = isDivisionChild
                    ? GameConfig.CellDivisionChildHealth * healthMultiplier
                    : GameConfig.CellDivisionHealth * healthMultiplier;
                Speed       = GameConfig.CellDivisionSpeed;
                _color      = new Color("#ff44aa");
                _visualSize = isDivisionChild
                    ? GameConfig.TileSize * 0.4f
                    : GameConfig.TileSize * 0.7f;
                break;

            default: // BioParticle
                Health      = GameConfig.ParticleBaseHealth * healthMultiplier;
                Speed       = GameConfig.ParticleBaseSpeed;
                _color      = Constants.Colors.BioParticle;
                _visualSize = GameConfig.TileSize * 0.6f;
                break;
        }

        if (_path != null && _path.Count > 0)
            Position = _path[0];
    }

    /// <summary>
    /// Updates the particle's path without resetting health, position, or velocity.
    /// Used by ParticleManager when the grid changes mid-wave.
    /// </summary>
    public void Reroute(List<Vector2> remainingPath)
    {
        _path = remainingPath;
        _waypointIndex = 0;
        // Do NOT reset Health, Position, or _velocity
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        Health -= amount;
        if (Health <= 0f)
        {
            _isDead = true;
            SpawnDeathSplash();
            int reward = GameConfig.CurrencyPerKill;
            EmitSignal(SignalName.Died, reward);
        }
    }

    // ── Godot ─────────────────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        if (_isDead) return;
        if (_path == null || _path.Count == 0) return;
        if (_waypointIndex >= _path.Count) return;

        float dt = (float)delta;

        // RadiationBlob is immune to slow — ignore SlowMultiplier
        float slowFactor = (Type == ParticleType.RadiationBlob) ? 1.0f : SlowMultiplier;
        float effectiveSpeed = Speed * slowFactor * GameConfig.TileSize; // pixels/sec

        Vector2 target   = _path[_waypointIndex];
        Vector2 toTarget = target - Position;

        if (toTarget.Length() <= WaypointReachRadius)
        {
            _waypointIndex++;
            if (_waypointIndex >= _path.Count)
            {
                EmitSignal(SignalName.ReachedExit);
                return;
            }
            target   = _path[_waypointIndex];
            toTarget = target - Position;
        }

        Vector2 desiredVelocity = toTarget.Normalized() * effectiveSpeed;
        _velocity = _velocity.Lerp(desiredVelocity, GameConfig.ParticleSteeringWeight);

        Position += _velocity * dt;
        QueueRedraw();
    }

    private void SpawnDeathSplash()
    {
        var splash = new DeathSplash();
        GetParent()?.AddChild(splash);
        splash.GlobalPosition = GlobalPosition;
    }

    public override void _Draw()
    {
        float half = _visualSize * 0.5f;
        DrawRect(new Rect2(-half, -half, _visualSize, _visualSize), _color);
    }
}
