using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Abstract base class for all filter module towers.
/// Renders a colored square matching the tile size, color defined by subclass.
/// </summary>
public abstract partial class TowerBase : Node2D
{
    public abstract float Range { get; }
    public abstract int Cost { get; }
    protected abstract Color TowerColor { get; }

    // Injected by TowerManager after placement
    public ParticleManager ParticleManagerRef { get; set; }

    // ── Upgrade state ──────────────────────────────────────────────────────
    public bool IsUpgraded { get; set; } = false;

    /// <summary>Grid position (col, row) set by TowerManager on placement.</summary>
    public Vector2I GridPos { get; set; }

    public override void _Ready()
    {
        QueueRedraw();
    }

    /// <summary>Returns all active particles within the given range (in tiles).</summary>
    protected List<Particle> GetNearbyParticles(float rangeTiles)
    {
        var result = new List<Particle>();
        if (ParticleManagerRef == null) return result;

        float rangePixels = rangeTiles * GameConfig.TileSize;
        float rangePixelsSq = rangePixels * rangePixels;

        foreach (var child in ParticleManagerRef.GetChildren())
        {
            if (child is Particle p)
            {
                float distSq = GlobalPosition.DistanceSquaredTo(p.GlobalPosition);
                if (distSq <= rangePixelsSq)
                    result.Add(p);
            }
        }
        return result;
    }

    public override void _Draw()
    {
        int size = GameConfig.TileSize;
        var rect = new Rect2(-size * 0.5f, -size * 0.5f, size, size);
        DrawRect(rect, TowerColor);

        // Draw a small gold corner indicator when upgraded
        if (IsUpgraded)
        {
            int corner = size / 4;
            var badge = new Rect2(-size * 0.5f, -size * 0.5f, corner, corner);
            DrawRect(badge, Colors.Gold);
        }
    }
}
