using BioFilter;
using Godot;

public partial class GridManager : Node2D
{
    private int GridWidth => GameConfig.GridWidth;
    private int GridHeight => GameConfig.GridHeight;
    private int TileSize => GameConfig.TileSize;

    private TileType[,] _grid = new TileType[GameConfig.GridWidth, GameConfig.GridHeight];
    private Vector2I _hoverTile = new Vector2I(-1, -1);
    private float _time = 0f;

    // Ambient corrupted-pixel effect
    private readonly RandomNumberGenerator _rng = new();
    private Vector2I _corruptedTile = new Vector2I(-1, -1);
    private float _corruptedTimer = 0f;
    private float _corruptedInterval = 2.5f;
    private float _corruptedFlash = 0f;
    private const float CorruptedFlashDuration = 0.15f;

    // Range preview: set by TowerManager when a tower type is selected
    private TowerManager.TowerType _previewTowerType = TowerManager.TowerType.None;
    private float _previewRange = 0f;
    private Color _previewColor = Colors.White;

    private readonly BioFilter.AirflowCalculator _airflowCalculator = new();

    /// <summary>
    /// When true, left-click places walls. When false, TowerManager owns left-click.
    /// </summary>
    public bool WallPlacementActive { get; set; } = true;

    /// <summary>Current airflow percentage (0.0–1.0). Cached after each placement/removal.</summary>
    public float CurrentAirflow { get; private set; } = 1.0f;

    /// <summary>Emitted whenever CurrentAirflow changes so the HUD can update.</summary>
    [Signal]
    public delegate void AirflowChangedEventHandler(float airflow);

    /// <summary>Emitted when airflow drops below the critical warning threshold.</summary>
    [Signal]
    public delegate void AirflowCriticalEventHandler(float airflow);

    /// <summary>Emitted when a wall tile is right-click removed (for refund: walls cost 0 so no currency, but TowerManager hooks this for tower removal).</summary>
    [Signal]
    public delegate void TileRightClickedEventHandler(int col, int row);

    // Colors (sourced from Constants.Colors)
    private static readonly Color ColorEmpty      = Constants.Colors.Background;
    private static readonly Color ColorScanline   = Constants.Colors.ScanlineAlt;
    private static readonly Color ColorGridLine   = Constants.Colors.GridLine;
    private static readonly Color ColorWall       = Constants.Colors.Wall;
    private static readonly Color ColorSpawn      = Constants.Colors.Spawn;
    private static readonly Color ColorExit       = Constants.Colors.Exit;
    private static readonly Color ColorHover      = new Color(1f, 1f, 1f, 0.3f);
    private static readonly Color ColorBlocked    = new Color(1f, 0f, 0f, 0.4f);

    public override void _Ready()
    {
        _rng.Randomize();
        InitializeGrid();
        RefreshAirflow();
    }

    private void InitializeGrid()
    {
        for (int col = 0; col < GameConfig.GridWidth; col++)
        {
            for (int row = 0; row < GameConfig.GridHeight; row++)
            {
                _grid[col, row] = TileType.Empty;
            }
        }

        // Spawn: column 0, row 10 (left middle)
        _grid[GameConfig.SpawnCol, GameConfig.SpawnRow] = TileType.Spawn;

        // Exit: entire column 29 (right edge)
        for (int row = 0; row < GameConfig.GridHeight; row++)
        {
            _grid[GameConfig.GridWidth - 1, row] = TileType.Exit;
        }
    }

    /// <summary>
    /// Attempts to place a tile. Returns false if placement would drop airflow below the minimum.
    /// </summary>
    public bool PlaceTile(int col, int row, TileType type)
    {
        if (!IsValidCoord(col, row)) return false;
        if (_grid[col, row] == TileType.Spawn) return false;
        if (_grid[col, row] == TileType.Exit) return false;
        if (_grid[col, row] == type) return false; // already this type

        // Test airflow on a copy before committing
        TileType[,] testGrid = CloneGrid();
        testGrid[col, row] = type;

        float testAirflow = _airflowCalculator.CalculateAirflow(testGrid);
        if (testAirflow < GameConfig.AirflowMinPercent)
            return false; // reject — would block too much

        _grid[col, row] = type;
        RefreshAirflow();
        QueueRedraw();
        return true;
    }

    /// <summary>
    /// Removes a tile and recalculates airflow. Returns the TileType that was removed.
    /// </summary>
    public TileType RemoveTile(int col, int row)
    {
        if (!IsValidCoord(col, row)) return TileType.Empty;
        if (_grid[col, row] == TileType.Spawn) return TileType.Empty;
        if (_grid[col, row] == TileType.Exit) return TileType.Empty;

        TileType removed = _grid[col, row];
        _grid[col, row] = TileType.Empty;
        RefreshAirflow();
        QueueRedraw();
        return removed;
    }

    /// <summary>Returns a snapshot of the current grid for pathfinding.</summary>
    public TileType[,] GetGrid()
    {
        return CloneGrid();
    }

    public TileType GetTileType(int col, int row)
    {
        if (!IsValidCoord(col, row)) return TileType.Empty;
        return _grid[col, row];
    }

    private bool IsValidCoord(int col, int row)
    {
        return col >= 0 && col < GameConfig.GridWidth && row >= 0 && row < GameConfig.GridHeight;
    }

    public Vector2I WorldToGrid(Vector2 localPos)
    {
        // Already in local space - just convert to grid coords
        int col = (int)(localPos.X / GameConfig.TileSize);
        int row = (int)(localPos.Y / GameConfig.TileSize);
        return new Vector2I(col, row);
    }

    public void SetHoverTile(Vector2I tile)
    {
        if (_hoverTile != tile)
        {
            _hoverTile = tile;
            QueueRedraw();
        }
    }

    /// <summary>Sets the range preview for hovering while a tower type is selected.</summary>
    public void SetRangePreview(TowerManager.TowerType towerType, float range, Color color)
    {
        _previewTowerType = towerType;
        _previewRange = range;
        _previewColor = color;
        QueueRedraw();
    }

    /// <summary>Clears the range preview circle.</summary>
    public void ClearRangePreview()
    {
        _previewTowerType = TowerManager.TowerType.None;
        QueueRedraw();
    }

    /// <summary>Public method to force airflow recalculation and emit AirflowChanged (used by VortexSeparator).</summary>
    public void TriggerAirflowRefresh() => RefreshAirflow();

        /// <summary>Recalculates and caches CurrentAirflow, then emits AirflowChanged signal.</summary>
    private void RefreshAirflow()
    {
        CurrentAirflow = _airflowCalculator.CalculateAirflow(_grid);
        EmitSignal(SignalName.AirflowChanged, CurrentAirflow);
        if (CurrentAirflow <= GameConfig.AirflowCriticalThreshold)
            EmitSignal(SignalName.AirflowCritical, CurrentAirflow);
    }

    private TileType[,] CloneGrid()
    {
        int cols = _grid.GetLength(0);
        int rows = _grid.GetLength(1);
        TileType[,] copy = new TileType[cols, rows];
        System.Array.Copy(_grid, copy, _grid.Length);
        return copy;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        // Corrupted pixel ambient effect
        _corruptedTimer += (float)delta;
        if (_corruptedTimer >= _corruptedInterval)
        {
            _corruptedTimer = 0f;
            _corruptedInterval = _rng.RandfRange(2.0f, 3.5f);
            // Pick a random empty tile
            int attempts = 20;
            while (attempts-- > 0)
            {
                int tc = _rng.RandiRange(0, GameConfig.GridWidth - 2); // avoid exit col
                int tr = _rng.RandiRange(0, GameConfig.GridHeight - 1);
                if (_grid[tc, tr] == TileType.Empty)
                {
                    _corruptedTile = new Vector2I(tc, tr);
                    _corruptedFlash = CorruptedFlashDuration;
                    break;
                }
            }
        }
        if (_corruptedFlash > 0f)
        {
            _corruptedFlash -= (float)delta;
            if (_corruptedFlash < 0f) _corruptedFlash = 0f;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        int ts = GameConfig.TileSize;

        for (int col = 0; col < GameConfig.GridWidth; col++)
        {
            for (int row = 0; row < GameConfig.GridHeight; row++)
            {
                float x = col * ts;
                float y = row * ts;
                var rect = new Rect2(x, y, ts, ts);
                TileType tile = _grid[col, row];

                switch (tile)
                {
                    case TileType.Empty:
                        // Scanline effect: alternate rows get slightly lighter base
                        DrawRect(rect, row % 2 == 0 ? ColorEmpty : ColorScanline);
                        // 1px grid border
                        DrawRect(rect, ColorGridLine, false, 1f);
                        break;

                    case TileType.Wall:
                        DrawWallTile(x, y, ts);
                        break;

                    case TileType.Tower:
                        // Tower draws itself; leave transparent
                        break;

                    case TileType.Spawn:
                        DrawSpawnTile(x, y, ts);
                        break;

                    case TileType.Exit:
                        DrawExitTile(x, y, ts);
                        break;
                }

                // Hover highlight
                if (_hoverTile.X == col && _hoverTile.Y == row)
                    DrawRect(rect, ColorHover);
            }
        }

        // Corrupted pixel ambient flash
        if (_corruptedFlash > 0f && _corruptedTile.X >= 0 && _corruptedTile.Y >= 0)
        {
            float cx = _corruptedTile.X * ts;
            float cy = _corruptedTile.Y * ts;
            float alpha = (_corruptedFlash / CorruptedFlashDuration) * 0.6f;
            var cpColor = new Color(Constants.Colors.CorruptedPixel, alpha);
            DrawRect(new Rect2(cx + ts * 0.5f - 1f, cy + ts * 0.5f - 1f, 2f, 2f), cpColor);
        }

        // Corner markers: pixel bracket decorations at grid corners
        DrawCornerMarkers(ts);

        // Range preview circle
        if (_previewTowerType != TowerManager.TowerType.None &&
            _hoverTile.X >= 0 && _hoverTile.Y >= 0 &&
            IsValidCoord(_hoverTile.X, _hoverTile.Y) &&
            _grid[_hoverTile.X, _hoverTile.Y] == TileType.Empty)
        {
            float radiusPx = _previewRange * GameConfig.TileSize;
            var center = new Vector2(
                _hoverTile.X * ts + ts * 0.5f,
                _hoverTile.Y * ts + ts * 0.5f);
            var circleColor = new Color(_previewColor, GameConfig.RangePreviewAlpha);
            DrawCircle(center, radiusPx, circleColor);
        }
    }

    private void DrawWallTile(float x, float y, int ts)
    {
        var rect = new Rect2(x, y, ts, ts);
        // Main fill
        DrawRect(rect, ColorWall);
        // Top-left highlight edges (1px)
        DrawLine(new Vector2(x, y), new Vector2(x + ts - 1, y), Constants.Colors.WallHighlight, 1f);          // top
        DrawLine(new Vector2(x, y), new Vector2(x, y + ts - 1), Constants.Colors.WallHighlight, 1f);          // left
        // Bottom-right shadow edges (1px)
        DrawLine(new Vector2(x, y + ts - 1), new Vector2(x + ts, y + ts - 1), Constants.Colors.WallShadow, 1f);  // bottom
        DrawLine(new Vector2(x + ts - 1, y), new Vector2(x + ts - 1, y + ts), Constants.Colors.WallShadow, 1f);  // right
        // Corner rivets: 2x2 pixel dots 4px from each corner
        var rivet = Constants.Colors.WallRivet;
        if (ts >= 12)
        {
            DrawRect(new Rect2(x + 2, y + 2, 2, 2), rivet);                   // top-left
            DrawRect(new Rect2(x + ts - 4, y + 2, 2, 2), rivet);              // top-right
            DrawRect(new Rect2(x + 2, y + ts - 4, 2, 2), rivet);              // bottom-left
            DrawRect(new Rect2(x + ts - 4, y + ts - 4, 2, 2), rivet);         // bottom-right
        }
    }

    private void DrawSpawnTile(float x, float y, int ts)
    {
        var rect = new Rect2(x, y, ts, ts);
        // Base fill
        DrawRect(rect, ColorSpawn);
        // 3 animated concentric rings pulsing outward
        var center = new Vector2(x + ts * 0.5f, y + ts * 0.5f);
        float maxR = ts * 0.48f;
        for (int i = 0; i < 3; i++)
        {
            float phase = _time * 2.5f + i * (Mathf.Tau / 3f);
            float t = (phase % Mathf.Tau) / Mathf.Tau; // 0..1
            float radius = t * maxR;
            float alpha = (1f - t) * 0.85f;
            DrawArc(center, radius, 0f, Mathf.Tau, 16,
                new Color(Constants.Colors.SpawnRing, alpha), 1f);
        }
    }

    private void DrawExitTile(float x, float y, int ts)
    {
        var rect = new Rect2(x, y, ts, ts);
        DrawRect(rect, ColorExit);
        // Vertical scan line: bright green bar sweeping top→bottom
        float scanSpeed = 24f; // px/sec
        float gridH = GameConfig.GridHeight * ts;
        float scanY = ((_time * scanSpeed) % gridH);
        // Only draw if scan line passes through this tile
        float tileTop = y;
        float tileBot = y + ts;
        float lineY = scanY;
        // scan line is relative to grid top, so check if it falls in this tile
        if (lineY >= tileTop && lineY < tileBot)
        {
            float localY = lineY - tileTop;
            DrawLine(
                new Vector2(x, y + localY),
                new Vector2(x + ts, y + localY),
                new Color(Constants.Colors.ExitScanLine, 0.85f), 1f);
        }
        // Dim trailing glow (2px above scan line)
        float trailY = lineY - 2f;
        if (trailY >= tileTop && trailY < tileBot)
        {
            float localY = trailY - tileTop;
            DrawLine(
                new Vector2(x, y + localY),
                new Vector2(x + ts, y + localY),
                new Color(Constants.Colors.ExitScanLine, 0.3f), 1f);
        }
    }

    private void DrawCornerMarkers(int ts)
    {
        int gw = GameConfig.GridWidth * ts;
        int gh = GameConfig.GridHeight * ts;
        int arm = 4; // bracket arm length in pixels
        var c = Constants.Colors.CornerMarker;
        float w = 1.5f;

        // Top-left
        DrawLine(new Vector2(0, 0), new Vector2(arm, 0), c, w);
        DrawLine(new Vector2(0, 0), new Vector2(0, arm), c, w);
        // Top-right
        DrawLine(new Vector2(gw, 0), new Vector2(gw - arm, 0), c, w);
        DrawLine(new Vector2(gw, 0), new Vector2(gw, arm), c, w);
        // Bottom-left
        DrawLine(new Vector2(0, gh), new Vector2(arm, gh), c, w);
        DrawLine(new Vector2(0, gh), new Vector2(0, gh - arm), c, w);
        // Bottom-right
        DrawLine(new Vector2(gw, gh), new Vector2(gw - arm, gh), c, w);
        DrawLine(new Vector2(gw, gh), new Vector2(gw, gh - arm), c, w);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            // Use GetLocalMousePosition() — correctly handles canvas scaling
            Vector2 localPos = GetLocalMousePosition();
            Vector2I tile = new Vector2I((int)(localPos.X / GameConfig.TileSize), (int)(localPos.Y / GameConfig.TileSize));
            if (IsValidCoord(tile.X, tile.Y))
                SetHoverTile(tile);
            else
                SetHoverTile(new Vector2I(-1, -1));
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            Vector2 localPos = GetLocalMousePosition();
            Vector2I tile = new Vector2I((int)(localPos.X / GameConfig.TileSize), (int)(localPos.Y / GameConfig.TileSize));
            if (!IsValidCoord(tile.X, tile.Y)) return;

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (WallPlacementActive && GetTileType(tile.X, tile.Y) == TileType.Empty)
                    PlaceTile(tile.X, tile.Y, TileType.Wall);
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                // Emit signal so TowerManager can handle refunds (for both walls and towers)
                EmitSignal(SignalName.TileRightClicked, tile.X, tile.Y);
            }
        }
    }
}
