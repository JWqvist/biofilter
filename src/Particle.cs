using System.Collections.Generic;
using Godot;

namespace BioFilter;

/// <summary>
/// A bio-particle that follows a pre-calculated path through the filter grid.
/// Smooth movement with momentum — no instant 90° turns.
/// </summary>
public partial class Particle : Node2D
{
    // ── State ────────────────────────────────────────────────────────────────
    public float Health { get; private set; } = GameConfig.ParticleBaseHealth;
    public float Speed { get; private set; } = GameConfig.ParticleBaseSpeed;
    public float SlowMultiplier { get; set; } = 1.0f;

    // ── Path ─────────────────────────────────────────────────────────────────
    private List<Vector2> _path;
    private int _waypointIndex = 0;
    private Vector2 _velocity = Vector2.Zero;

    private const float WaypointReachRadius = 2.0f; // pixels

    // ── Visuals ───────────────────────────────────────────────────────────────
    private static readonly Color ParticleColor = Constants.Colors.BioParticle;
    private static readonly float VisualSize = GameConfig.TileSize * 0.6f;

    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void ReachedExitEventHandler();
    [Signal] public delegate void DiedEventHandler(int currencyReward);

    // ── Public API ────────────────────────────────────────────────────────────
    public void Initialize(List<Vector2> worldPath, float healthMultiplier = 1.0f)
    {
        _path = worldPath;
        Health = GameConfig.ParticleBaseHealth * healthMultiplier;
        _waypointIndex = 0;
        if (_path != null && _path.Count > 0)
            Position = _path[0];
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health <= 0f)
        {
            int reward = (int)(GameConfig.CurrencyPerKill);
            EmitSignal(SignalName.Died, reward);
        }
    }

    // ── Godot ─────────────────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        if (_path == null || _path.Count == 0) return;
        if (_waypointIndex >= _path.Count) return;

        float dt = (float)delta;
        float effectiveSpeed = Speed * SlowMultiplier * GameConfig.TileSize; // pixels/sec

        Vector2 target = _path[_waypointIndex];
        Vector2 toTarget = target - Position;

        if (toTarget.Length() <= WaypointReachRadius)
        {
            _waypointIndex++;
            if (_waypointIndex >= _path.Count)
            {
                EmitSignal(SignalName.ReachedExit);
                return;
            }
            target = _path[_waypointIndex];
            toTarget = target - Position;
        }

        // Smooth steering: lerp velocity towards desired direction
        Vector2 desiredVelocity = toTarget.Normalized() * effectiveSpeed;
        _velocity = _velocity.Lerp(desiredVelocity, GameConfig.ParticleSteeringWeight);

        Position += _velocity * dt;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float half = VisualSize * 0.5f;
        DrawRect(new Rect2(-half, -half, VisualSize, VisualSize), ParticleColor);
    }
}
