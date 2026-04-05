using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// UV Steriliser — projectile shooter.
/// Fires a projectile at the nearest particle in range at a fixed fire rate.
/// </summary>
public partial class UVSteriliser : TowerBase
{
    public override float Range => IsUpgraded ? UpgradedRange : GameConfig.UVSteriliserRange;
    public override int Cost => GameConfig.UVSteriliserCost;
    protected override Color TowerColor => Constants.Colors.UVSterilizer;

    // Upgraded stats (set by TowerUpgrade)
    public float UpgradedDamage { get; set; } = GameConfig.UVSteriliserDamage;
    public float UpgradedRange { get; set; } = GameConfig.UVSteriliserRange;
    public float UpgradedFireRate { get; set; } = GameConfig.UVSteriliserFireRate;

    private float ActiveDamage => IsUpgraded ? UpgradedDamage : GameConfig.UVSteriliserDamage;
    private float ActiveFireRate => IsUpgraded ? UpgradedFireRate : GameConfig.UVSteriliserFireRate;
    private float FireInterval => 1.0f / ActiveFireRate;

    private float _fireTimer = 0f;

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
        projectile.Initialize(nearest, ActiveDamage);
    }
}
