using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Basic Filter — damage aura tower.
/// Deals damage to all particles within range every tick.
/// Flashes a green glow circle when dealing damage.
/// </summary>
public partial class BasicFilter : TowerBase
{
    public override float Range => IsUpgraded ? UpgradedRange : GameConfig.BasicFilterRange;
    public override int Cost => GameConfig.BasicFilterCost;
    protected override Color TowerColor => Constants.Colors.BasicFilter;
    protected override Color GetInnerColor() => Constants.Colors.BasicFilterInner;

    // Upgraded stats (set by TowerUpgrade)
    public float UpgradedDamage { get; set; } = GameConfig.BasicFilterDamage;
    public float UpgradedRange { get; set; } = GameConfig.BasicFilterRange;
    public float UpgradedTickRate { get; set; } = GameConfig.BasicFilterTickRate;

    private float ActiveDamage => IsUpgraded ? UpgradedDamage : GameConfig.BasicFilterDamage;
    private float ActiveTickRate => IsUpgraded ? UpgradedTickRate : GameConfig.BasicFilterTickRate;

    private float _tickTimer = 0f;

    // Glow pulse effect
    private float _glowTimer = 0f;
    private const float GlowDuration = 0.2f;
    private bool _glowing = false;

    public override void _Process(double delta)
    {
        _tickTimer += (float)delta;
        if (_tickTimer >= ActiveTickRate)
        {
            _tickTimer = 0f;
            if (DamageNearby())
            {
                _glowing = true;
                _glowTimer = 0f;
                QueueRedraw();
            }
        }

        if (_glowing)
        {
            _glowTimer += (float)delta;
            if (_glowTimer >= GlowDuration)
                _glowing = false;
            QueueRedraw();
        }
    }

    private bool DamageNearby()
    {
        var particles = GetNearbyParticles(Range);
        foreach (var p in particles)
            p.TakeDamage(ActiveDamage * DamageMultiplier);
        return particles.Count > 0;
    }

    public override void _Draw()
    {
        base._Draw();

        int ts = GameConfig.TileSize;
        float half = ts * 0.5f;
        var bright = Constants.Colors.BasicFilterBright;

        // Center 3x3 cross/plus in bright green
        // Horizontal bar: 5 wide x 1 tall (centered)
        DrawRect(new Rect2(-2, -1, 5, 1), bright);
        // Vertical bar: 1 wide x 5 tall (centered)
        DrawRect(new Rect2(-1, -2, 1, 5), bright);

        // Corner dots: 1x1 px at each inner corner (3px from edge)
        float d = half - 3f;
        DrawRect(new Rect2(-d - 1, -d - 1, 2, 2), bright);
        DrawRect(new Rect2(d - 1,  -d - 1, 2, 2), bright);
        DrawRect(new Rect2(-d - 1,  d - 1, 2, 2), bright);
        DrawRect(new Rect2(d - 1,   d - 1, 2, 2), bright);

        // Glow on damage
        if (_glowing)
        {
            float t = _glowTimer / GlowDuration;
            float alpha = (1f - t) * 0.5f;
            DrawCircle(Vector2.Zero, ts * 0.7f,
                new Color(0.49f, 1.0f, 0.23f, alpha));
        }
    }
}
