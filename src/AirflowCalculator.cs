using System.Collections.Generic;
using Godot;

namespace BioFilter;

/// <summary>
/// Calculates airflow percentage based on grid state.
/// Uses BFS flood-fill from spawn, then measures the minimum vertical corridor
/// width across all columns between spawn and exit.
/// Airflow% = min_width / GameConfig.GridHeight
/// </summary>
public class AirflowCalculator
{
    private static bool IsPassable(TileType t) =>
        t == TileType.Empty || t == TileType.Spawn || t == TileType.Exit;

    /// <summary>
    /// Returns the list of active spawn points for the current map.
    /// </summary>
    private static List<Vector2I> GetSpawnPoints()
    {
        if (MapManager.CurrentMap == 2)
        {
            return new List<Vector2I>
            {
                new Vector2I(GameConfig.Map2Spawn1Col, GameConfig.Map2Spawn1Row),
                new Vector2I(GameConfig.Map2Spawn2Col, GameConfig.Map2Spawn2Row),
            };
        }
        return new List<Vector2I> { new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow) };
    }

    /// <summary>
    /// Returns true if there is at least one valid path from any spawn to any exit tile.
    /// Uses proper BFS flood-fill, starting from ALL spawn points simultaneously.
    /// </summary>
    public bool HasValidPath(TileType[,] grid)
    {
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        bool[,] visited = new bool[cols, rows];
        var queue = new Queue<(int col, int row)>();

        // Seed BFS from all spawn points
        foreach (var sp in GetSpawnPoints())
        {
            if (sp.X < 0 || sp.X >= cols || sp.Y < 0 || sp.Y >= rows) continue;
            if (visited[sp.X, sp.Y]) continue;
            visited[sp.X, sp.Y] = true;
            queue.Enqueue((sp.X, sp.Y));
        }

        if (queue.Count == 0) return false;

        int[] dc = { 0, 0, 1, -1 };
        int[] dr = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (col, row) = queue.Dequeue();

            if (grid[col, row] == TileType.Exit)
                return true;

            for (int d = 0; d < 4; d++)
            {
                int nc = col + dc[d];
                int nr = row + dr[d];

                if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                if (visited[nc, nr]) continue;
                if (!IsPassable(grid[nc, nr])) continue;

                visited[nc, nr] = true;
                queue.Enqueue((nc, nr));
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates airflow as a 0.0–1.0 value.
    /// 1.0 = fully open (all GameConfig.GridHeight tiles passable in every column).
    /// Uses BFS reachability then measures minimum vertical width (chokepoint).
    /// Airflow% = min_width / GameConfig.GridHeight
    ///   - Full open  (20 tiles) = 100%
    ///   - 4 tiles wide          = 20%  (AirflowMinPercent threshold)
    ///   - 1 tile wide           = 5%   (below minimum, rejected)
    ///   - 0 tiles               = 0%   (fully blocked)
    /// </summary>
    public float CalculateAirflow(TileType[,] grid)
    {
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        // Step 1: BFS flood-fill from ALL spawn points simultaneously
        bool[,] reachable = new bool[cols, rows];
        var queue = new Queue<(int col, int row)>();

        foreach (var sp in GetSpawnPoints())
        {
            if (sp.X < 0 || sp.X >= cols || sp.Y < 0 || sp.Y >= rows) continue;
            if (reachable[sp.X, sp.Y]) continue;
            reachable[sp.X, sp.Y] = true;
            queue.Enqueue((sp.X, sp.Y));
        }
        if (queue.Count == 0) return 0.0f;

        int[] dc = { 0, 0, 1, -1 };
        int[] dr = { 1, -1, 0, 0 };

        bool foundExit = false;

        while (queue.Count > 0)
        {
            var (col, row) = queue.Dequeue();

            if (grid[col, row] == TileType.Exit)
                foundExit = true;

            for (int d = 0; d < 4; d++)
            {
                int nc = col + dc[d];
                int nr = row + dr[d];
                if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                if (reachable[nc, nr]) continue;
                if (!IsPassable(grid[nc, nr])) continue;
                reachable[nc, nr] = true;
                queue.Enqueue((nc, nr));
            }
        }

        // No valid path = 0% airflow
        if (!foundExit) return 0.0f;

        // Step 2: For each column between spawn and exit (exclusive of exit column),
        // count how many rows are reachable and passable (vertical corridor width).
        // Step 3: Find the minimum width (the chokepoint).
        int exitCol = GameConfig.GridWidth - 1; // exit is the rightmost column
        int minWidth = GameConfig.GridHeight;   // start at max possible

        // Skip spawn col (col 0) - it has only 1 passable tile by design
        // Start from col 1 to measure actual corridor widths player creates
        for (int col = GameConfig.SpawnCol + 1; col < exitCol; col++)
        {
            int width = 0;
            for (int row = 0; row < rows; row++)
            {
                if (reachable[col, row] && IsPassable(grid[col, row]))
                    width++;
            }
            if (width < minWidth)
                minWidth = width;
        }

        // Step 4: Airflow% = min_width / GridHeight
        float airflow = (float)minWidth / GameConfig.GridHeight;

        // Clamp to valid range
        if (airflow < 0f) airflow = 0f;
        if (airflow > 1f) airflow = 1f;

        return airflow;
    }
}
