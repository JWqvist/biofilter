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
        var bright = new Color("#4caf50");
        var dim    = new Color("#2e7d32");
        var brighter = new Color("#69f0ae");

        bool pulsing = _glowing;
        Color cellColor = pulsing ? brighter : bright;

        // 3×3 filter mesh grid
        // Each cell: 2×2 px, 1px gap, centered in tile
        // Total mesh width = 3*2 + 2*1 = 8px; offset = -4
        float meshOff = -4f;
        float cellSz  = 2f;
        float gap     = 1f;
        float step    = cellSz + gap;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float cx = meshOff + col * step;
                float cy = meshOff + row * step;

                bool isCenter = row == 1 && col == 1;
                bool isChecker = (row + col) % 2 == 0;

                Color c;
                if (isCenter)
                    c = pulsing ? brighter : brighter;
                else
                    c = pulsing ? cellColor : (isChecker ? bright : dim);

                float sz = isCenter ? 3f : cellSz;
                float offset = isCenter ? -0.5f : 0f;
                DrawRect(new Rect2(cx + offset, cy + offset, sz, sz), c);
            }
        }

        // Flow arrows: tiny 1px triangles on left and right inner edges
        // Left side arrow (pointing right)
        float arrowX = meshOff - 3f;
        float arrowY = -1f;
        DrawRect(new Rect2(arrowX,       arrowY,     1f, 3f), dim);
        DrawRect(new Rect2(arrowX + 1f,  arrowY + 1f, 1f, 1f), dim);

        // Right side arrow (pointing right)
        float arrowRX = meshOff + 3f * step + 1f;
        DrawRect(new Rect2(arrowRX,      arrowY,      1f, 3f), dim);
        DrawRect(new Rect2(arrowRX + 1f, arrowY + 1f, 1f, 1f), dim);

        // Glow pulse on damage
        if (_glowing)
        {
            float t = _glowTimer / GlowDuration;
            float alpha = (1f - t) * 0.5f;
            DrawCircle(Vector2.Zero, ts * 0.7f,
                new Color(0.29f, 0.69f, 0.31f, alpha));
        }
    }
}
