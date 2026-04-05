using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// UV Steriliser — projectile shooter.
/// Fires a projectile at the nearest particle in range at a fixed fire rate.
/// Shows a brief white muzzle flash when firing.
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

    // Muzzle flash
    private float _flashTimer = 0f;
    private bool _flashing = false;
    private const float FlashDuration = 0.15f;

    private PackedScene _projectileScene = null!;

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

        if (_flashing)
        {
            _flashTimer += (float)delta;
            if (_flashTimer >= FlashDuration)
                _flashing = false;
            QueueRedraw();
        }
    }

    private void TryFire()
    {
        var particles = GetNearbyParticles(Range);
        if (particles.Count == 0) return;

        // Find nearest
        Particle? nearest = null;
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

        // Trigger muzzle flash
        _flashing = true;
        _flashTimer = 0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        if (_flashing)
        {
            float t = _flashTimer / FlashDuration;
            float alpha = (1f - t) * 0.8f;
            DrawCircle(Vector2.Zero, GameConfig.TileSize * 0.6f,
                new Color(1f, 1f, 1f, alpha));
        }
    }
}
