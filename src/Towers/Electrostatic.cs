using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Electrostatic tower — slow aura.
/// Applies SlowMultiplier to all particles within range; resets particles that leave.
/// </summary>
public partial class Electrostatic : TowerBase
{
    public override float Range => GameConfig.ElectrostaticRange;
    public override int Cost => GameConfig.ElectrostaticCost;
    protected override Color TowerColor => Constants.Colors.Electrostatic;

    // Tracks which particles we're currently slowing so we can reset on exit
    private readonly HashSet<Particle> _slowedParticles = new();

    public override void _Process(double delta)
    {
        if (ParticleManagerRef == null) return;

        var inRange = new HashSet<Particle>(GetNearbyParticles(Range));

        // Apply slow to newly in-range particles
        foreach (var p in inRange)
        {
            p.SlowMultiplier = GameConfig.ElectrostaticSlowPercent;
            _slowedParticles.Add(p);
        }

        // Reset particles that left range
        var toRemove = new List<Particle>();
        foreach (var p in _slowedParticles)
        {
            if (!inRange.Contains(p))
            {
                // Only reset if we're still the one slowing it (avoid overwriting other effects)
                if (Godot.GodotObject.IsInstanceValid(p))
                    p.SlowMultiplier = 1.0f;
                toRemove.Add(p);
            }
        }
        foreach (var p in toRemove)
            _slowedParticles.Remove(p);
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
}
