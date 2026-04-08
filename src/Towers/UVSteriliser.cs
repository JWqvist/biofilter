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
    protected override Color GetInnerColor() => Constants.Colors.UVSterilizerInner;

    private float _localTime = 0f;

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
        base._Process(delta);
        _localTime += (float)delta;
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

        if (IsDisabled) return;

        // Armored enemies take bonus damage from UV
        float typeMultiplier = nearest.Type == ParticleType.Armored
            ? GameConfig.ArmorUVBonus
            : 1.0f;

        if (nearest.Type == ParticleType.Saboteur)
            HookSaboteurSignal(nearest);

        var projectile = _projectileScene.Instantiate<Projectile>();
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
        projectile.Initialize(nearest, ActiveDamage * DamageMultiplier * typeMultiplier);

        // Trigger muzzle flash
        _flashing = true;
        _flashTimer = 0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        int ts = GameConfig.TileSize;
        float half = ts * 0.5f;

        // Pixel ring: 8-direction DrawRect pattern simulating a circle
        float ringR = ts * 0.28f;
        var ringColor = new Color(Constants.Colors.UVSterilizer, 0.9f);
        ringColor = new Color(ringColor.R + 0.3f, ringColor.G + 0.1f, ringColor.B + 0.3f, 0.9f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (Mathf.Pi / 4f);
            float rx = Mathf.Cos(angle) * ringR;
            float ry = Mathf.Sin(angle) * ringR;
            DrawRect(new Rect2(rx - 1, ry - 1, 2, 2), ringColor);
        }

        // Rotating pixel rays: 4 short lines at angles, rotating with time
        float rayLen = ts * 0.28f;
        var rayColor = new Color(0.8f, 0.5f, 1.0f, 0.85f);
        for (int i = 0; i < 4; i++)
        {
            float angle = _localTime * 2.2f + i * (Mathf.Pi * 0.5f);
            Vector2 inner = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ringR + 1f);
            Vector2 outer = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ringR + rayLen * 0.6f);
            DrawLine(inner, outer, rayColor, 1f);
        }

        // Muzzle flash
        if (_flashing)
        {
            float t = _flashTimer / FlashDuration;
            float alpha = (1f - t) * 0.8f;
            DrawCircle(Vector2.Zero, ts * 0.6f, new Color(1f, 1f, 1f, alpha));
        }
    }
}
