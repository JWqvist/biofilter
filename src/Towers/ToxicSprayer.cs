using BioFilter;
using BioFilter.Towers;
using Godot;

/// <summary>Applies damage-over-time poison to all particles in range.</summary>
public partial class ToxicSprayer : TowerBase
{
    public override float Range => GameConfig.ToxicSprayerRange;
    public override int   Cost  => GameConfig.ToxicSprayerCost;
    protected override Color TowerColor => new Color("#dd2222");

    public override void _Process(double delta)
    {
        base._Process(delta);
        var nearby = GetNearbyParticles(Range * GameConfig.TileSize);
        foreach (var p in nearby)
            p.ApplyPoison(GameConfig.ToxicSprayerDotDamage, GameConfig.ToxicSprayerDotDuration);
    }

    public override void _Draw()
    {
        base._Draw();
        float t = (float)Engine.GetProcessFrames() * 0.03f;
        var toxic = new Color("#dd2222");
        var dim   = new Color("#76ff03", 0.4f);

        // Pulsing cloud at corners
        float pulse = (Mathf.Sin(t * 2f) + 1f) * 0.5f * 0.3f + 0.1f;
        DrawRect(new Rect2(-6, -6, 4, 4), new Color("#dd2222", pulse));
        DrawRect(new Rect2( 2, -6, 4, 4), new Color("#dd2222", pulse));
        DrawRect(new Rect2(-6,  2, 4, 4), new Color("#dd2222", pulse));
        DrawRect(new Rect2( 2,  2, 4, 4), new Color("#dd2222", pulse));

        // 3 dripping droplets in triangle formation
        Vector2[] centers = {
            new Vector2(-3f, -3f),
            new Vector2( 3f, -3f),
            new Vector2( 0f,  1f),
        };
        for (int i = 0; i < 3; i++)
        {
            float drip = (Mathf.Sin(t * 1.5f + i * 2.094f) + 1f) * 0.5f * 3f;
            DrawRect(new Rect2(centers[i].X - 1, centers[i].Y - 1 + drip, 2, 2), toxic);
            DrawRect(new Rect2(centers[i].X - 0.5f, centers[i].Y + 1 + drip, 1, 2), dim);
        }
    }
}
