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
    Armored,       // resistant to BasicFilter, weak to UV
    Carrier,       // releases 3 BioParticles on death
    Saboteur,      // disables the tower that kills it
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
    public float SlowMultiplier     { get; set; } = 1.0f;
    public float PoisonDps           { get; private set; } = 0f;
    public float PoisonRemaining     { get; private set; } = 0f;
    private float _poisonTickTimer   = 0f;
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
    /// <summary>Emitted by Saboteur on death, carrying the killer tower's world position.</summary>
    [Signal] public delegate void SaboteurKilledByTowerEventHandler(Vector2 towerPos);

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
                _color      = new Color("#ff8c00"); // nuclear orange (body fill; glow uses #d4e600 in _Draw)
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

            case ParticleType.Armored:
                Health      = GameConfig.ArmoredHealth * healthMultiplier;
                Speed       = GameConfig.ArmoredSpeed;
                _color      = new Color("#607d8b");
                _visualSize = GameConfig.TileSize * 0.75f;
                break;

            case ParticleType.Carrier:
                Health      = GameConfig.CarrierHealth * healthMultiplier;
                Speed       = GameConfig.CarrierSpeed;
                _color      = new Color("#8d6e63");
                _visualSize = GameConfig.TileSize * 0.65f;
                break;

            case ParticleType.Saboteur:
                Health      = GameConfig.SaboteurHealth * healthMultiplier;
                Speed       = GameConfig.SaboteurSpeed;
                _color      = new Color("#7b1fa2");
                _visualSize = GameConfig.TileSize * 0.7f;
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
            // Division children get world-space coords; normal spawns use local (parent-relative) coords
            if (IsDivisionChild)
                GlobalPosition = _path[0];
            else
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

    /// <summary>
    /// Deals damage to this particle.
    /// Pass an optional <paramref name="damageMultiplier"/> to account for type-specific
    /// resistances/vulnerabilities (e.g. Armored vs BasicFilter or UV).
    /// </summary>
    public void TakeDamage(float amount, float damageMultiplier = 1.0f)
    {
        if (_isDead) return;
        Health -= amount * damageMultiplier;
        if (Health <= 0f)
        {
            _isDead = true;
            SpawnDeathSplash();
            // SporeSpeck gives quarter credits (fast, weak scout)
            int reward = Type == ParticleType.SporeSpeck
                ? Mathf.Max(1, GameConfig.CurrencyPerKill / 4)
                : GameConfig.CurrencyPerKill;
            EmitSignal(SignalName.Died, reward);
        }
    }

    /// <summary>
    /// Variant of TakeDamage that also carries the killer tower position for Saboteur logic.
    /// If the particle dies and is a Saboteur, emits SaboteurKilledByTower.
    /// </summary>
    public void TakeDamageFromTower(float amount, float damageMultiplier, Vector2 towerWorldPos)
    {
        if (_isDead) return;
        Health -= amount * damageMultiplier;
        if (Health <= 0f)
        {
            _isDead = true;
            SpawnDeathSplash();
            int reward = Type == ParticleType.SporeSpeck
                ? Mathf.Max(1, GameConfig.CurrencyPerKill / 4)
                : GameConfig.CurrencyPerKill;
            if (Type == ParticleType.Saboteur)
                EmitSignal(SignalName.SaboteurKilledByTower, towerWorldPos);
            EmitSignal(SignalName.Died, reward);
        }
    }

    // ── Godot ─────────────────────────────────────────────────────────────────
    public void ApplyPoison(float dps, float duration)
    {
        PoisonDps       = dps;
        PoisonRemaining = Mathf.Max(PoisonRemaining, duration); // refresh if already poisoned
        _poisonTickTimer = Mathf.Min(_poisonTickTimer, GameConfig.ToxicSprayerDotTickRate);
    }

    public override void _Process(double delta)
    {
        if (_isDead) return;

        float dt = (float)delta;
        _localTime += dt;

        if (_path == null || _path.Count == 0) return;
        if (_waypointIndex >= _path.Count) return;

        // RadiationBlob is immune to slow
        // SporeSpeck only half affected by slow
        float slowFactor = Type == ParticleType.RadiationBlob ? 1.0f
                         : Type == ParticleType.SporeSpeck    ? 1.0f - (1.0f - SlowMultiplier) * 0.5f
                         : SlowMultiplier;
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
        var splash = DeathSplash.Create(Type, _color);
        GetParent()?.AddChild(splash);
        splash.GlobalPosition = GlobalPosition;
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

            // ── CellDivision ──────────────────────────────────────────────
            case ParticleType.CellDivision:
            {
                bool lowHealth  = healthProp < 0.5f;
                float seamSpeed = lowHealth ? 10f : 4f;
                // At low health the two halves wobble apart visibly
                float wobble = lowHealth ? Mathf.Sin(_localTime * 8f) * 2.5f : 0f;
                float r = 5f; // semi-circle radius

                var leftColor  = new Color("#991144"); // darker red-purple
                var rightColor = new Color("#ee3388"); // brighter red-purple

                Vector2 leftCenter  = new Vector2(-wobble, 0f);
                Vector2 rightCenter = new Vector2( wobble, 0f);

                // Left semi-circle (arc from 90° to 270°, left half)
                DrawArc(leftCenter,  r, Mathf.Pi * 0.5f,  Mathf.Pi * 1.5f, 24, leftColor,  r * 2f);
                // Right semi-circle (arc from -90° to 90°, right half)
                DrawArc(rightCenter, r, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f, 24, rightColor, r * 2f);

                // Outer glow ring
                float glowAlpha = 0.25f + 0.15f * Mathf.Sin(_localTime * 3f);
                DrawArc(Vector2.Zero, r + 2f, 0f, Mathf.Tau, 32,
                        new Color(1f, 0.2f, 0.6f, glowAlpha), 2f);

                // Seam: bright white/pink vertical line pulsing at x = midpoint between halves
                float seamAlpha = lowHealth
                    ? (Mathf.Sin(_localTime * seamSpeed) * 0.5f + 0.5f) * 0.9f + 0.1f
                    : (Mathf.Sin(_localTime * seamSpeed) * 0.5f + 0.5f) * 0.5f + 0.2f;
                Color seamColor = lowHealth
                    ? new Color(1f, 0.9f, 1f, seamAlpha)    // near-white when critical
                    : new Color(1f, 0.5f, 0.8f, seamAlpha); // soft pink normally
                float seamX = (leftCenter.X + rightCenter.X) * 0.5f;
                DrawLine(new Vector2(seamX, -r), new Vector2(seamX, r), seamColor, 1.5f);

                // Extra glow dot on seam at low health
                if (lowHealth)
                {
                    float dotAlpha = (Mathf.Sin(_localTime * seamSpeed * 1.5f) + 1f) * 0.5f;
                    DrawRect(new Rect2(seamX - 1f, -1f, 2f, 2f), new Color(1f, 1f, 1f, dotAlpha));
                }
                break;
            }

            // ── Armored ───────────────────────────────────────────────────────────────
            case ParticleType.Armored:
            {
                float sz = 12f;
                float hs = sz * 0.5f;

                // Metallic grey-blue body
                DrawRect(new Rect2(-hs, -hs, sz, sz), _color);

                // Diagonal hash lines (armor pattern)
                var hashColor = new Color(1f, 1f, 1f, 0.35f);
                for (int i = -2; i <= 4; i += 2)
                {
                    float d = i * 2f;
                    DrawLine(new Vector2(-hs + d, -hs), new Vector2(-hs + d + sz, -hs + sz), hashColor, 1f);
                }

                // Bright border
                DrawRect(new Rect2(-hs, -hs, sz, sz), new Color(0.8f, 0.9f, 1.0f, 0.8f), false, 1f);
                break;
            }

            // ── Carrier ───────────────────────────────────────────────────────────────
            case ParticleType.Carrier:
            {
                float sz = 11f;
                float hs = sz * 0.5f;
                float r  = 2.5f; // corner radius approximation (use inset rect + corners)

                // Rounded body (approximated with inset rects)
                DrawRect(new Rect2(-hs + r, -hs, sz - r * 2f, sz), _color);
                DrawRect(new Rect2(-hs, -hs + r, sz, sz - r * 2f), _color);

                // 3 small white payload dots
                float[] dotX = { -3f, 0f, 3f };
                foreach (float dx in dotX)
                    DrawRect(new Rect2(dx - 1f, -1.5f, 2.5f, 2.5f), Colors.White);

                // Subtle pulse ring
                float pulseAlpha = (Mathf.Sin(_localTime * 3f) + 1f) * 0.5f * 0.2f + 0.05f;
                DrawRect(new Rect2(-hs - 1, -hs - 1, sz + 2, sz + 2), new Color(_color, pulseAlpha), false, 1f);
                break;
            }

            // ── Saboteur ───────────────────────────────────────────────────────────────
            case ParticleType.Saboteur:
            {
                float sz = 11f;
                float hs = sz * 0.5f;

                // Purple body
                DrawRect(new Rect2(-hs, -hs, sz, sz), _color);

                // Yellow lightning bolt (pixel art)
                var bolt = new Color("#ffeb3b");
                // Top segment (angled right)
                DrawRect(new Rect2(-1.5f, -hs + 1f, 3f, 2.5f), bolt);
                DrawRect(new Rect2(-0.5f, -hs + 3f, 3f, 2f),   bolt);
                // Middle diagonal
                DrawRect(new Rect2(-2f, -hs + 4.5f, 4f, 1.5f), bolt);
                // Bottom segment (angled right)
                DrawRect(new Rect2(-2.5f, -hs + 6f, 3f, 2f),   bolt);
                DrawRect(new Rect2(-3f,  -hs + 7.5f, 2.5f, 1.5f), bolt);

                // Border
                DrawRect(new Rect2(-hs, -hs, sz, sz), new Color(0.9f, 0.5f, 1.0f, 0.9f), false, 1f);
                break;
            }

            default:
                DrawRect(new Rect2(-half, -half, _visualSize, _visualSize), _color);
                break;
        }

        // ── Health bar above every particle ────────────────────────────────
        // Bar width matches visual size; drawn 4px above the top edge
        float bw = _visualSize;
        float bh = 2f;
        float bx = -bw * 0.5f;
        float by = -half - 4f;
        // Background track
        DrawRect(new Rect2(bx, by, bw, bh), new Color(0.08f, 0.08f, 0.08f, 0.85f));
        // Filled portion: green when healthy, red when low
        Color healthColor = healthProp > 0.5f ? new Color("#22cc44") : new Color("#ff3322");
        DrawRect(new Rect2(bx, by, bw * healthProp, bh), healthColor);
    }
}
