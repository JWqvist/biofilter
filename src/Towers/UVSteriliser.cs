using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// UV Steriliser — projectile shooter.
/// Fires a projectile at the nearest particle in range at a fixed fire rate.
/// </summary>
public partial class UVSteriliser : TowerBase
{
    public override float Range => GameConfig.UVSteriliserRange;
    public override int Cost => GameConfig.UVSteriliserCost;
    protected override Color TowerColor => Constants.Colors.UVSterilizer;

    private float _fireTimer = 0f;
    private float FireInterval => 1.0f / GameConfig.UVSteriliserFireRate;

    private PackedScene _projectileScene;

    public override void _Ready()
    {
        base._Ready();
        _projectileScene = GD.Load<PackedScene>("res://scenes/Projectile.tscn");
    }

    public override void _Process(double delta)
    {
        _fireTimer += (float)delta;
        if (_fireTimer >= FireInterval)
        {
            _fireTimer = 0f;
            TryFire();
        }
    }

    private void TryFire()
    {
        var particles = GetNearbyParticles(Range);
        if (particles.Count == 0) return;

        // Find nearest
        Particle nearest = null;
        float nearestDistSq = float.MaxValue;
        foreach (var p in particles)
        {
            float dSq = GlobalPosition.DistanceSquaredTo(p.GlobalPosition);
            if (dSq < nearestDistSq)
            {
                nearestDistSq = dSq;
                nearest = p;
            }
        }

        if (nearest == null) return;

        var projectile = _projectileScene.Instantiate<Projectile>();
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
        projectile.Initialize(nearest);
    }
}
