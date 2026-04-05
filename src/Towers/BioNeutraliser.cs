using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Bio Neutraliser — support tower.
/// Boosts damage of all 8 adjacent towers by GameConfig.BioNeutraliserBoost (25%).
/// Draws connecting lines to boosted neighbors.
/// </summary>
public partial class BioNeutraliser : TowerBase
{
    public override float Range => 1.5f; // visual only
    public override int Cost => GameConfig.BioNeutraliserCost;
    protected override Color TowerColor => Constants.Colors.BioNeutralPurple;
    protected override Color GetInnerColor() => new Color("#1a0020");

    // Injected by TowerManager
    public TowerManager? TowerManagerRef { get; set; }

    private readonly List<TowerBase> _boostedNeighbors = new();
    private float _time = 0f;

    public override void _Ready()
    {
        base._Ready();
    }

    /// <summary>Called after placement — boosts all 8 adjacent towers.</summary>
    public void ApplyBoost()
    {
        if (TowerManagerRef == null) return;

        _boostedNeighbors.Clear();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var neighborPos = new Vector2I(GridPos.X + dx, GridPos.Y + dy);
                var neighbor = TowerManagerRef.GetTowerAt(neighborPos);
                if (neighbor != null && neighbor is not BioNeutraliser)
                {
                    neighbor.DamageMultiplier = GameConfig.BioNeutraliserBoost;
                    _boostedNeighbors.Add(neighbor);
                }
            }
        }
        GD.Print($"BioNeutraliser: boosted {_boostedNeighbors.Count} neighbors");
        QueueRedraw();
    }

    /// <summary>Called on removal — removes boost from all neighbors.</summary>
    public void RemoveBoost()
    {
        foreach (var t in _boostedNeighbors)
        {
            if (Godot.GodotObject.IsInstanceValid(t))
                t.DamageMultiplier = 1.0f;
        }
        _boostedNeighbors.Clear();
    }

    public override void _ExitTree()
    {
        RemoveBoost();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        int ts = GameConfig.TileSize;

        // 3x3 pixel atom/hex pattern in center
        var atomColor = new Color(0.8f, 0.3f, 1.0f, 0.9f);
        float r = ts * 0.22f;
        // 6 dots around center (hexagonal)
        for (int i = 0; i < 6; i++)
        {
            float angle = i * (Mathf.Pi / 3f);
            float ax = Mathf.Cos(angle) * r;
            float ay = Mathf.Sin(angle) * r;
            DrawRect(new Rect2(ax - 1f, ay - 1f, 2f, 2f), atomColor);
        }
        // Center dot
        DrawRect(new Rect2(-1f, -1f, 2f, 2f), atomColor);

        // Draw connecting lines to boosted neighbors (pulsing)
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
        var lineColor = new Color(0.8f, 0.2f, 1.0f, 0.6f * pulse);

        foreach (var neighbor in _boostedNeighbors)
        {
            if (!Godot.GodotObject.IsInstanceValid(neighbor)) continue;
            Vector2 localEnd = ToLocal(neighbor.GlobalPosition);
            DrawLine(Vector2.Zero, localEnd, lineColor, 1.5f);
            DrawCircle(localEnd, 2f, lineColor);
        }
    }
}
