using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Magnetic Cage — traps particles.
/// Pulls particles within range and holds them (Speed = 0) for
/// GameConfig.MagneticCageHoldSeconds before releasing.
/// Visual: brown square with 4 inward-pointing arrow triangles.
/// </summary>
public partial class MagneticCage : TowerBase
{
    public override float Range => GameConfig.MagneticCageRange;
    public override int Cost => GameConfig.MagneticCageCost;
    protected override Color TowerColor => Constants.Colors.MagneticBrown;
    protected override Color GetInnerColor() => new Color("#1a0f0a");

    private float _time = 0f;

    // Map particle -> time held so far
    private readonly Dictionary<Particle, float> _heldParticles = new();
    private readonly Dictionary<Particle, float> _releaseCooldown = new(); // time until can be caught again
    // Original speeds before we stopped them
    private readonly Dictionary<Particle, float> _originalSpeeds = new();

    public override void _Process(double delta)
    {
        _time += (float)delta;
        float dt = (float)delta;

        var inRange = GetNearbyParticles(Range);

        // Start holding newly arrived particles
        foreach (var p in inRange)
        {
            if (!_heldParticles.ContainsKey(p) && !_releaseCooldown.ContainsKey(p))
            {
                // Freeze particle
                _originalSpeeds[p] = p.Speed;
                p.Speed = 0f;
                _heldParticles[p] = 0f;
            }
        }

        // Tick hold timers and release when expired
        var toRelease = new List<Particle>();
        foreach (var (p, elapsed) in _heldParticles)
        {
            if (!Godot.GodotObject.IsInstanceValid(p))
            {
                toRelease.Add(p);
                continue;
            }

            float newElapsed = elapsed + dt;
            _heldParticles[p] = newElapsed;

            if (newElapsed >= GameConfig.MagneticCageHoldSeconds)
            {
                toRelease.Add(p);
            }
        }

        foreach (var p in toRelease)
        {
            if (Godot.GodotObject.IsInstanceValid(p) && _originalSpeeds.TryGetValue(p, out float origSpeed))
                p.Speed = origSpeed;
            _heldParticles.Remove(p);
            _originalSpeeds.Remove(p);
            _releaseCooldown[p] = GameConfig.MagneticCageHoldSeconds; // cooldown = same as hold time
        }

        // Tick cooldowns
        var cooldownExpired = new List<Particle>();
        foreach (var (p, cd) in _releaseCooldown)
        {
            float newCd = cd - dt;
            if (!Godot.GodotObject.IsInstanceValid(p) || newCd <= 0)
                cooldownExpired.Add(p);
            else
                _releaseCooldown[p] = newCd;
        }
        foreach (var p in cooldownExpired)
            _releaseCooldown.Remove(p);

        QueueRedraw();
    }

    public override void _ExitTree()
    {
        // Release all held particles when tower is removed
        foreach (var (p, _) in _heldParticles)
        {
            if (Godot.GodotObject.IsInstanceValid(p) && _originalSpeeds.TryGetValue(p, out float origSpeed))
                p.Speed = origSpeed;
        }
        _heldParticles.Clear();
        _originalSpeeds.Clear();
    }

    public override void _Draw()
    {
        base._Draw();

        float half = GameConfig.TileSize * 0.4f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 5f);
        var arrowColor = new Color(1f, 0.7f, 0.2f, 0.7f + 0.3f * pulse);

        // 4 inward-pointing arrow triangles at N/S/E/W
        float arrowDist = half;
        float tipSize = 3.5f;
        Vector2[] dirs = {
            new Vector2(0, -1), // North (points inward = down)
            new Vector2(0,  1), // South (points inward = up)
            new Vector2(-1, 0), // West (points inward = right)
            new Vector2( 1, 0), // East (points inward = left)
        };

        foreach (var dir in dirs)
        {
            Vector2 base1 = dir * arrowDist;
            Vector2 tip   = dir * (arrowDist - tipSize * 2f);
            Vector2 perp  = new Vector2(-dir.Y, dir.X) * tipSize;

            // Triangle: two base points and tip pointing inward
            DrawLine(base1 - perp, tip, arrowColor, 1f);
            DrawLine(base1 + perp, tip, arrowColor, 1f);
            DrawLine(base1 - perp, base1 + perp, arrowColor, 1f);
        }

        // Draw circle showing hold range
        if (_heldParticles.Count > 0)
        {
            float radiusPx = Range * GameConfig.TileSize;
            DrawArc(Vector2.Zero, radiusPx, 0, Mathf.Tau,
                24, new Color(1f, 0.7f, 0.2f, 0.2f * pulse), 1f);
        }
    }
}
