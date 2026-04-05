using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Electrostatic tower — slow aura.
/// Applies SlowMultiplier to all particles within range; resets particles that leave.
/// Draws zigzag lightning arcs to slowed particles.
/// </summary>
public partial class Electrostatic : TowerBase
{
    public override float Range => IsUpgraded ? UpgradedRange : GameConfig.ElectrostaticRange;
    public override int Cost => GameConfig.ElectrostaticCost;
    protected override Color TowerColor => Constants.Colors.Electrostatic;

    // Upgraded stats (set by TowerUpgrade)
    public float UpgradedSlowPercent { get; set; } = GameConfig.ElectrostaticSlowPercent;
    public float UpgradedRange { get; set; } = GameConfig.ElectrostaticRange;

    private float ActiveSlowPercent => IsUpgraded ? UpgradedSlowPercent : GameConfig.ElectrostaticSlowPercent;

    // Tracks which particles we're currently slowing so we can reset on exit
    private readonly HashSet<Particle> _slowedParticles = new();

    // Arc drawing
    private List<Particle> _arcTargets = new();
    private float _arcJitter = 0f; // time accumulator for arc refresh
    private const float ArcRefreshRate = 0.05f; // refresh arcs every 50ms
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        base._Ready();
        _rng.Randomize();
    }

    public override void _Process(double delta)
    {
        if (ParticleManagerRef == null) return;

        var inRange = new HashSet<Particle>(GetNearbyParticles(Range));

        // Apply slow to newly in-range particles
        foreach (var p in inRange)
        {
            p.SlowMultiplier = ActiveSlowPercent;
            _slowedParticles.Add(p);
        }

        // Reset particles that left range
        var toRemove = new List<Particle>();
        foreach (var p in _slowedParticles)
        {
            if (!inRange.Contains(p))
            {
                if (Godot.GodotObject.IsInstanceValid(p))
                    p.SlowMultiplier = 1.0f;
                toRemove.Add(p);
            }
        }
        foreach (var p in toRemove)
            _slowedParticles.Remove(p);

        // Refresh arc targets
        _arcJitter += (float)delta;
        if (_arcJitter >= ArcRefreshRate)
        {
            _arcJitter = 0f;
            _arcTargets = new List<Particle>(inRange);
            QueueRedraw();
        }
    }

    public override void _ExitTree()
    {
        // Reset all slowed particles when tower is removed
        foreach (var p in _slowedParticles)
        {
            if (Godot.GodotObject.IsInstanceValid(p))
                p.SlowMultiplier = 1.0f;
        }
        _slowedParticles.Clear();
    }

    public override void _Draw()
    {
        base._Draw();
        DrawLightningArcs();
    }

    private void DrawLightningArcs()
    {
        if (_arcTargets.Count == 0) return;

        var arcColor = new Color(0.6f, 1.0f, 1.0f, 0.75f); // white/cyan

        foreach (var target in _arcTargets)
        {
            if (!Godot.GodotObject.IsInstanceValid(target)) continue;

            Vector2 end = ToLocal(target.GlobalPosition);
            DrawZigzagLine(Vector2.Zero, end, arcColor, 2);
        }
    }

    private void DrawZigzagLine(Vector2 from, Vector2 to, Color color, int segments)
    {
        var points = new Vector2[segments + 2];
        points[0] = from;
        points[segments + 1] = to;

        Vector2 dir = (to - from);
        Vector2 perp = new Vector2(-dir.Y, dir.X).Normalized();

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / (segments + 1);
            Vector2 mid = from.Lerp(to, t);
            float jitter = _rng.RandfRange(-4f, 4f);
            points[i] = mid + perp * jitter;
        }

        for (int i = 0; i < points.Length - 1; i++)
            DrawLine(points[i], points[i + 1], color, 1f);
    }
}
