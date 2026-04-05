using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Vortex Separator — forces particles to take longer routes.
/// Adds a detour penalty to A* pathfinding for tiles near this tower,
/// causing particles to reroute around it and travel further.
/// Visual: rotating cyan spiral.
/// </summary>
public partial class VortexSeparator : TowerBase
{
    public override float Range => GameConfig.VortexSeparatorRange;
    public override int Cost => GameConfig.VortexSeparatorCost;
    protected override Color TowerColor => Constants.Colors.VortexCyan;
    protected override Color GetInnerColor() => new Color("#001a1f");

    // Injected by TowerManager so we can trigger rerouting
    public GridManager? GridManagerRef { get; set; }

    private float _time = 0f;
    private bool _penaltyApplied = false;

    // Keep reference to our penalised tiles for cleanup
    private readonly List<Vector2I> _penalisedTiles = new();

    public override void _Ready()
    {
        base._Ready();
    }

    /// <summary>Called once after placement — registers vortex penalty tiles.</summary>
    public void ApplyVortexPenalty()
    {
        _penaltyApplied = true;
        // Tiles within range that are Empty (passable) get a penalty registered
        // We do this by telling the Pathfinder about our penalty zones
        // For now: store our tile positions; Pathfinder will check at path-build time
        int rangeInt = (int)GameConfig.VortexSeparatorRange;
        for (int dx = -rangeInt; dx <= rangeInt; dx++)
        {
            for (int dy = -rangeInt; dy <= rangeInt; dy++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= GameConfig.VortexSeparatorRange)
                {
                    _penalisedTiles.Add(new Vector2I(GridPos.X + dx, GridPos.Y + dy));
                }
            }
        }
        VortexPenaltyRegistry.Register(GridPos, _penalisedTiles);
    }

    /// <summary>Called on removal — unregisters vortex penalty.</summary>
    public void RemoveVortexPenalty()
    {
        VortexPenaltyRegistry.Unregister(GridPos);
    }

    public override void _ExitTree()
    {
        RemoveVortexPenalty();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        // Rotating spiral: 4 short lines radiating from center, rotating over time
        float half = GameConfig.TileSize * 0.35f;
        var spiralColor = new Color(0f, 0.8f, 0.9f, 0.9f);
        for (int i = 0; i < 4; i++)
        {
            float angle = _time * 2.5f + i * Mathf.Pi * 0.5f;
            Vector2 inner = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;
            Vector2 outer = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * half;
            DrawLine(inner, outer, spiralColor, 1.5f);

            // Small perpendicular at tip for spiral look
            float perpAngle = angle + Mathf.Pi * 0.25f;
            Vector2 tip = outer;
            Vector2 perpEnd = tip + new Vector2(Mathf.Cos(perpAngle), Mathf.Sin(perpAngle)) * (half * 0.4f);
            DrawLine(tip, perpEnd, spiralColor, 1f);
        }
    }
}
