using System.Collections.Generic;
using Godot;

namespace BioFilter;

/// <summary>
/// Static registry of Vortex Separator penalty zones.
/// Pathfinder checks this to add extra cost to tiles near Vortex towers.
/// </summary>
public static class VortexPenaltyRegistry
{
    // Map: owner tower GridPos -> set of penalised tile positions
    private static readonly Dictionary<Vector2I, List<Vector2I>> _penalties = new();

    // Flat set of all currently penalised tiles (for fast lookup)
    private static readonly HashSet<Vector2I> _penaltySet = new();

    public static void Register(Vector2I ownerPos, List<Vector2I> tiles)
    {
        _penalties[ownerPos] = tiles;
        RebuildSet();
    }

    public static void Unregister(Vector2I ownerPos)
    {
        _penalties.Remove(ownerPos);
        RebuildSet();
    }

    public static bool IsPenalised(Vector2I tile) => _penaltySet.Contains(tile);

    private static void RebuildSet()
    {
        _penaltySet.Clear();
        foreach (var list in _penalties.Values)
            foreach (var t in list)
                _penaltySet.Add(t);
    }
}
