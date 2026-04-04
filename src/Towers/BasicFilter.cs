using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Basic Filter — damage aura tower.
/// Deals damage to all particles within range every tick.
/// </summary>
public partial class BasicFilter : TowerBase
{
    public override float Range => GameConfig.BasicFilterRange;
    public override int Cost => GameConfig.BasicFilterCost;
    protected override Color TowerColor => Constants.Colors.BasicFilter;

    private float _tickTimer = 0f;

    public override void _Process(double delta)
    {
        _tickTimer += (float)delta;
        if (_tickTimer >= GameConfig.BasicFilterTickRate)
        {
            _tickTimer = 0f;
            DamageNearby();
        }
    }

    private void DamageNearby()
    {
        var particles = GetNearbyParticles(Range);
        foreach (var p in particles)
            p.TakeDamage(GameConfig.BasicFilterDamage);
    }
}
