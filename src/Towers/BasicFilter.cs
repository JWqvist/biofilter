using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Basic Filter — damage aura tower.
/// Deals damage to all particles within range every tick.
/// </summary>
public partial class BasicFilter : TowerBase
{
    public override float Range => IsUpgraded ? UpgradedRange : GameConfig.BasicFilterRange;
    public override int Cost => GameConfig.BasicFilterCost;
    protected override Color TowerColor => Constants.Colors.BasicFilter;

    // Upgraded stats (set by TowerUpgrade)
    public float UpgradedDamage { get; set; } = GameConfig.BasicFilterDamage;
    public float UpgradedRange { get; set; } = GameConfig.BasicFilterRange;
    public float UpgradedTickRate { get; set; } = GameConfig.BasicFilterTickRate;

    private float ActiveDamage => IsUpgraded ? UpgradedDamage : GameConfig.BasicFilterDamage;
    private float ActiveTickRate => IsUpgraded ? UpgradedTickRate : GameConfig.BasicFilterTickRate;

    private float _tickTimer = 0f;

    public override void _Process(double delta)
    {
        _tickTimer += (float)delta;
        if (_tickTimer >= ActiveTickRate)
        {
            _tickTimer = 0f;
            DamageNearby();
        }
    }

    private void DamageNearby()
    {
        var particles = GetNearbyParticles(Range);
        foreach (var p in particles)
            p.TakeDamage(ActiveDamage);
    }
}
