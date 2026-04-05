using System.Collections.Generic;

namespace BioFilter;

/// <summary>
/// Calculates airflow percentage based on grid state.
/// Uses BFS flood-fill from spawn to measure path availability and corridor widths.
/// </summary>
public class AirflowCalculator
{
    private static bool IsPassable(TileType t) =>
        t == TileType.Empty || t == TileType.Spawn || t == TileType.Exit;

    /// <summary>
    /// Returns true if there is at least one valid path from spawn to any exit tile.
    /// </summary>
    public bool HasValidPath(TileType[,] grid)
    {
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        bool[,] visited = new bool[cols, rows];
        var queue = new Queue<(int col, int row)>();

        queue.Enqueue((GameConfig.SpawnCol, GameConfig.SpawnRow));
        visited[GameConfig.SpawnCol, GameConfig.SpawnRow] = true;

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
    /// 1.0 = fully open, 0.0 = path completely blocked.
    /// Uses BFS to find reachable tiles and samples corridor widths.
    /// </summary>
    public float CalculateAirflow(TileType[,] grid)
    {
        if (!HasValidPath(grid))
            return 0.0f;

        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        // BFS to get all reachable passable tiles from spawn
        bool[,] reachable = new bool[cols, rows];
        var queue = new Queue<(int col, int row)>();

        queue.Enqueue((GameConfig.SpawnCol, GameConfig.SpawnRow));
        reachable[GameConfig.SpawnCol, GameConfig.SpawnRow] = true;

        int[] dc = { 0, 0, 1, -1 };
        int[] dr = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (col, row) = queue.Dequeue();
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

        // Sample vertical width (number of passable tiles) at each column
        // from spawn column to exit column and accumulate restriction score
        float totalRestriction = 0f;
        float openGridRestriction = 0f; // what restriction looks like on an empty grid
        int sampleCols = 0;

        for (int col = GameConfig.SpawnCol; col < GameConfig.GridWidth - 1; col++)
        {
            int width = 0;
            for (int row = 0; row < rows; row++)
            {
                if (reachable[col, row] && IsPassable(grid[col, row]))
                    width++;
            }

            if (width == 0) continue; // no reachable tiles in this column

            sampleCols++;

            // Determine restriction weight for this column based on width
            float colWeight;
            if (width >= GameConfig.ChokeWidthHigh)
                colWeight = GameConfig.ChokeWeightHigh;
            else if (width == GameConfig.ChokeWidthMedium)
                colWeight = GameConfig.ChokeWeightMedium;
            else // width == 1
                colWeight = GameConfig.ChokeWeightLow;

            totalRestriction += colWeight;
            openGridRestriction += GameConfig.ChokeWeightHigh; // best case = all wide open corridors
        }

        if (sampleCols == 0)
            return 1.0f;

        float maxRestriction = GameConfig.ChokeWeightLow * sampleCols; // worst case = all single-tile
        float range = maxRestriction - openGridRestriction;
        if (range <= 0f)
            return 1.0f; // all columns fully open, no restriction possible

        // Normalize: 0.0 = open grid restriction, 1.0 = fully blocked
        // Airflow is inverse of how restricted relative to worst case, scaled so open grid = 1.0
        float normalizedRestriction = (totalRestriction - openGridRestriction) / range;
        float airflow = 1.0f - normalizedRestriction;

        // Clamp to valid range
        if (airflow < 0f) airflow = 0f;
        if (airflow > 1f) airflow = 1f;

        return airflow;
    }
}
