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
    public float Speed  { get; set; }
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

    // ── Animation ─────────────────────────────────────────────────────────────
    private float _localTime = 0f;
    private float _maxHealth  = 1f; // stored on init for proportional health bar

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
                // Nuclear yellow-green color
                _color = new Color("#d4e600"); // nuclear yellow-green
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

        _maxHealth = Health;

        if (_path != null && _path.Count > 0)
            GlobalPosition = _path[0]; // use GlobalPosition so world coords are correct
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

        float dt = (float)delta;
        _localTime += dt;

        if (_path == null || _path.Count == 0) return;
        if (_waypointIndex >= _path.Count) return;

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
        splash.SetColor(_color);
    }

    public override void _Draw()
    {
        float half = _visualSize * 0.5f;
        float healthProp = _maxHealth > 0f ? Mathf.Clamp(Health / _maxHealth, 0f, 1f) : 1f;

        switch (Type)
        {
            // ── BioParticle ─────────────────────────────────────────────────
            case ParticleType.BioParticle:
            {
                // Outer glow: 12×12 dim square
                DrawRect(new Rect2(-6, -6, 12, 12), new Color(_color, 0.3f));

                // Pulsing main square: 10×10 ↔ 9×9 at 2 Hz
                float mainSize = 9.5f + 0.5f * Mathf.Sin(_localTime * Mathf.Tau * 2f);
                float hm = mainSize * 0.5f;
                DrawRect(new Rect2(-hm, -hm, mainSize, mainSize), _color);

                // 4 pseudopods oscillating in/out at cardinal directions
                float podOsc = Mathf.Sin(_localTime * 2f) * 1.5f;

                // North (oscillates up/down)
                DrawRect(new Rect2(-1.5f, -hm - 3f + podOsc, 3, 3), _color);
                // South (opposite phase)
                DrawRect(new Rect2(-1.5f,  hm       - podOsc, 3, 3), _color);
                // East (oscillates left/right)
                DrawRect(new Rect2( hm       + podOsc, -1.5f, 3, 3), _color);
                // West (opposite phase)
                DrawRect(new Rect2(-hm - 3f - podOsc, -1.5f, 3, 3), _color);
                break;
            }

            // ── SporeSpeck ──────────────────────────────────────────────────
            case ParticleType.SporeSpeck:
            {
                // Trail: 2-3 fading squares behind movement direction
                if (_velocity.Length() > 0.1f)
                {
                    Vector2 trailDir = -_velocity.Normalized();
                    DrawRect(new Rect2(trailDir * 5f  - new Vector2(1.5f, 1.5f), new Vector2(3, 3)), new Color(_color, 0.30f));
                    DrawRect(new Rect2(trailDir * 9f  - new Vector2(1.5f, 1.5f), new Vector2(3, 3)), new Color(_color, 0.20f));
                    DrawRect(new Rect2(trailDir * 13f - new Vector2(1.5f, 1.5f), new Vector2(3, 3)), new Color(_color, 0.10f));
                }

                // Bright 4×4 center dot
                DrawRect(new Rect2(-2, -2, 4, 4), _color);

                // 4 rotating sparks (1×3 lines)
                for (int i = 0; i < 4; i++)
                {
                    float angle = _localTime * 2.5f + i * (Mathf.Pi * 0.5f);
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 from = dir * 3f;
                    Vector2 to   = dir * 6f;
                    DrawLine(from, to, _color, 1.5f);
                }
                break;
            }

            // ── RadiationBlob ───────────────────────────────────────────────
            case ParticleType.RadiationBlob:
            {
                float sz = 13f;
                float hs = sz * 0.5f;

                // Outer glow (nuclear yellow-green)
                float glowPulse = (Mathf.Sin(_localTime * 3f) + 1f) * 0.5f * 0.25f + 0.1f;
                DrawRect(new Rect2(-hs - 2, -hs - 2, sz + 4, sz + 4),
                         new Color(0.4f, 0.9f, 0.0f, glowPulse)); // green glow

                // Main body: nuclear yellow
                DrawRect(new Rect2(-hs, -hs, sz, sz), new Color("#d4e600"));

                // Radiation symbol: 3 green wedge-arms rotating slowly
                var radGreen = new Color("#00cc44"); // bright green
                float rotBase = _localTime * 0.25f;
                for (int i = 0; i < 3; i++)
                {
                    float angle = rotBase + i * (Mathf.Tau / 3f);
                    float spread = 0.38f;
                    float len = hs * 0.72f;
                    Vector2 dir1 = new Vector2(Mathf.Cos(angle - spread), Mathf.Sin(angle - spread));
                    Vector2 dir2 = new Vector2(Mathf.Cos(angle + spread), Mathf.Sin(angle + spread));
                    DrawLine(Vector2.Zero, dir1 * len, radGreen, 2f);
                    DrawLine(Vector2.Zero, dir2 * len, radGreen, 2f);
                    DrawLine(dir1 * len, dir2 * len, radGreen, 1.5f);
                }
                // Center dot
                DrawRect(new Rect2(-1.5f, -1.5f, 3, 3), radGreen);

                // Health bar
                float barY = hs + 2f;
                DrawRect(new Rect2(-hs, barY, sz, 1.5f), new Color(0.15f, 0.15f, 0.15f, 0.9f));
                Color barCol = healthProp > 0.5f ? new Color("#00cc44") : new Color("#ff2222");
                DrawRect(new Rect2(-hs, barY, sz * healthProp, 1.5f), barCol);
                break;
            }

            // ── SwarmUnit ───────────────────────────────────────────────────
            case ParticleType.SwarmUnit:
            {
                // Per-unit phase offset using instance id
                float phase = (GetInstanceId() % 16UL) * (Mathf.Tau / 16f);
                float jitterX = Mathf.Sin(_localTime * 3f + phase)        * 1.5f;
                float jitterY = Mathf.Cos(_localTime * 3f + phase * 1.3f) * 1.5f;

                DrawRect(new Rect2(-1.5f + jitterX, -1.5f + jitterY, 3, 3), _color);
                break;
            }

            // ── CellDivision ────────────────────────────────────────────────
            case ParticleType.CellDivision:
            {
                float seamSpeed = healthProp < 0.5f ? 10f : 4f;
                float wobble    = healthProp < 0.5f ? Mathf.Sin(_localTime * 8f) * 2f : 0f;
                float cellHalf  = 5f; // 10×10 shape

                // Outer glow: dark purple
                DrawRect(new Rect2(-cellHalf - 1, -cellHalf - 1, 12, 12),
                         new Color(0.5f, 0.0f, 0.3f, 0.35f));

                // Main body: red-purple
                float wx = wobble * 0.4f;
                DrawRect(new Rect2(-cellHalf + wx, -cellHalf, 10, 10), new Color("#cc2266"));

                // Bright inner highlight (top-left corner feel)
                DrawRect(new Rect2(-cellHalf + wx, -cellHalf, 10, 2), new Color("#ff44aa", 0.6f));
                DrawRect(new Rect2(-cellHalf + wx, -cellHalf, 2, 10), new Color("#ff44aa", 0.6f));

                // Division seam: bright pink pulse
                float seamAlpha = (Mathf.Sin(_localTime * seamSpeed) + 1f) * 0.5f;
                float seamY     = Mathf.Sin(_localTime * seamSpeed * 0.25f) * 2f;
                DrawLine(new Vector2(-cellHalf + wx, seamY),
                         new Vector2(cellHalf + wx, seamY),
                         new Color("#ff88cc", seamAlpha), 1.5f);

                // Small "division dots" on seam when about to split
                if (healthProp < 0.5f)
                {
                    float dotX = wx;
                    DrawRect(new Rect2(dotX - 1, seamY - 1, 2, 2), new Color(1f, 1f, 1f, 0.8f));
                }
                break;
            }

            default:
                DrawRect(new Rect2(-half, -half, _visualSize, _visualSize), _color);
                break;
        }
    }
}
