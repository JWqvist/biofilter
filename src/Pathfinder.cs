using System.Collections.Generic;
using Godot;

namespace BioFilter;

/// <summary>
/// A* pathfinder for BioFilter grid.
/// Treats Wall and Tower tiles as impassable.
/// Returns a list of tile coordinates from start to the nearest exit tile.
/// </summary>
public static class Pathfinder
{
    private class Node
    {
        public Vector2I Position;
        public Node? Parent;
        public float G; // cost from start
        public float H; // heuristic to goal
        public float F => G + H;
    }

    /// <summary>
    /// Find a path from <paramref name="start"/> to the nearest tile of type Exit on the right edge.
    /// Returns null if no path exists.
    /// </summary>
    public static List<Vector2I>? FindPath(TileType[,] grid, Vector2I start)
    {
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        // Collect all exit tiles (rightmost column)
        var exits = new HashSet<Vector2I>();
        for (int row = 0; row < rows; row++)
        {
            var exitPos = new Vector2I(cols - 1, row);
            if (grid[exitPos.X, exitPos.Y] == TileType.Exit)
                exits.Add(exitPos);
        }

        if (exits.Count == 0) return null;

        var open = new List<Node>();
        var closed = new HashSet<Vector2I>();
        var openSet = new Dictionary<Vector2I, Node>();

        Node startNode = new Node
        {
            Position = start,
            Parent = null,
            G = 0,
            H = HeuristicToExits(start, exits)
        };
        open.Add(startNode);
        openSet[start] = startNode;

        while (open.Count > 0)
        {
            // Pick node with lowest F
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].F < open[bestIdx].F) bestIdx = i;

            Node current = open[bestIdx];
            open.RemoveAt(bestIdx);
            openSet.Remove(current.Position);

            if (exits.Contains(current.Position))
                return ReconstructPath(current);

            closed.Add(current.Position);

            // 4-directional neighbors
            foreach (var dir in new[] {
                new Vector2I(1, 0), new Vector2I(-1, 0),
                new Vector2I(0, 1), new Vector2I(0, -1)
            })
            {
                Vector2I neighborPos = current.Position + dir;
                if (neighborPos.X < 0 || neighborPos.X >= cols ||
                    neighborPos.Y < 0 || neighborPos.Y >= rows)
                    continue;

                if (closed.Contains(neighborPos)) continue;

                TileType tile = grid[neighborPos.X, neighborPos.Y];
                if (tile == TileType.Wall || tile == TileType.Tower) continue;

                float gNew = current.G + 1f;
                // Add vortex penalty for tiles near a Vortex Separator
                if (VortexPenaltyRegistry.IsPenalised(neighborPos))
                    gNew += GameConfig.VortexPenaltyWeight;
                float hNew = HeuristicToExits(neighborPos, exits);

                if (openSet.TryGetValue(neighborPos, out Node? existing))
                {
                    if (gNew < existing.G)
                    {
                        existing.G = gNew;
                        existing.Parent = current;
                    }
                }
                else
                {
                    var neighbor = new Node
                    {
                        Position = neighborPos,
                        Parent = current,
                        G = gNew,
                        H = hNew
                    };
                    open.Add(neighbor);
                    openSet[neighborPos] = neighbor;
                }
            }
        }

        return null; // no path found
    }

    private static float HeuristicToExits(Vector2I pos, HashSet<Vector2I> exits)
    {
        float best = float.MaxValue;
        foreach (var exit in exits)
        {
            float d = Mathf.Abs(pos.X - exit.X) + Mathf.Abs(pos.Y - exit.Y);
            if (d < best) best = d;
        }
        return best;
    }

    private static List<Vector2I> ReconstructPath(Node? node)
    {
        var path = new List<Vector2I>();
        while (node != null)
        {
            path.Add(node.Position);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Finds a path from each spawn point to the nearest exit tile.
    /// Returns one List&lt;Vector2I&gt; per spawn point; null entries mean no path was found from that spawn.
    /// </summary>
    public static List<List<Vector2I>?> FindPathMultiSpawn(
        TileType[,] grid,
        IEnumerable<Vector2I> spawnPoints)
    {
        var results = new List<List<Vector2I>?>();
        foreach (var spawn in spawnPoints)
            results.Add(FindPath(grid, spawn));
        return results;
    }
}
