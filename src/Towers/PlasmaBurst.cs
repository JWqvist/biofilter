using BioFilter;
using BioFilter.Towers;
using Godot;
using System.Collections.Generic;

/// <summary>Long-range tower that fires an AoE plasma explosion.</summary>
public partial class PlasmaBurst : TowerBase
{
    public override float Range => GameConfig.PlasmaBurstRange;
    public override int   Cost  => GameConfig.PlasmaBurstCost;
    protected override Color TowerColor => new Color("#1a1a3a");

    private float _charge = 0f;
    private bool  _justFired = false;
    private float _flashTimer = 0f;

    public override void _Process(double delta)
    {
        base._Process(delta);
        float dt = (float)delta;
        _charge += dt;
        if (_flashTimer > 0f) { _flashTimer -= dt; QueueRedraw(); }

        if (_charge >= GameConfig.PlasmaBurstFireRate)
        {
            _charge = 0f;
            FirePlasma();
        }
        QueueRedraw();
    }

    private void FirePlasma()
    {
        var nearby = GetNearbyParticles(Range * GameConfig.TileSize);
        if (nearby.Count == 0) return;

        // Target the particle closest to the center of the group
        Vector2 center = Vector2.Zero;
        foreach (var p in nearby) center += p.GlobalPosition;
        center /= nearby.Count;

        // AoE damage to all within burst radius
        float radiusPx = GameConfig.PlasmaBurstRadius * GameConfig.TileSize;
        foreach (var p in nearby)
        {
            if (p.GlobalPosition.DistanceTo(center) <= radiusPx)
                p.TakeDamage(GameConfig.PlasmaBurstDamage * DamageMultiplier);
        }
        _flashTimer = 0.2f;
    }

    public override void _Draw()
    {
        base._Draw();
        float chargeRatio = _charge / GameConfig.PlasmaBurstFireRate;
        var blue = new Color("#2979ff");

        // Charging diamond — grows with charge
        float sz = 2f + chargeRatio * 3f;
        DrawRect(new Rect2(-sz, -sz, sz * 2f, sz * 2f), new Color(blue, 0.15f + chargeRatio * 0.5f));

        // Rotating outer ring segments
        float angle = (float)Engine.GetProcessFrames() * 0.04f;
        for (int i = 0; i < 4; i++)
        {
            float a = angle + i * Mathf.Pi * 0.5f;
            float r = 5f;
            float rx = Mathf.Cos(a) * r;
            float ry = Mathf.Sin(a) * r;
            DrawRect(new Rect2(rx - 1, ry - 1, 2, 2), new Color(blue, 0.4f + chargeRatio * 0.6f));
        }

        // White flash on fire
        if (_flashTimer > 0f)
        {
            float alpha = _flashTimer / 0.2f;
            DrawRect(new Rect2(-7, -7, 14, 14), new Color(1f, 1f, 1f, alpha * 0.6f));
        }
    }
}
