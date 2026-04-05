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

    // Colors
    private static readonly Color ColorEmpty = Constants.Colors.Background;
    private static readonly Color ColorGridLine = Constants.Colors.GridLine;
    private static readonly Color ColorWall = Constants.Colors.Wall;
    private static readonly Color ColorSpawn = Constants.Colors.ParticleSpawn;
    private static readonly Color ColorExit = Constants.Colors.Exit;
    private static readonly Color ColorHover = new Color(1f, 1f, 1f, 0.3f);
    private static readonly Color ColorBlocked = new Color(1f, 0f, 0f, 0.4f); // flash on rejected placement

    // Border colors for pixel art look
    private static readonly Color BorderWall         = new Color("#4a6a4a");
    private static readonly Color BorderWallHighlight = new Color("#5a8a5a");
    private static readonly Color BorderBasicFilter   = new Color("#3daa50");
    private static readonly Color BorderElectrostatic = new Color("#1a8a9a");
    private static readonly Color BorderUVSteriliser  = new Color("#8a4aaa");
    private static readonly Color BorderExit          = new Color("#e06000");

    public override void _Ready()
    {
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

    public Vector2I WorldToGrid(Vector2 viewportPos)
    {
        // Convert viewport/canvas position to GridManager-local coordinate space
        Vector2 localPos = ToLocal(viewportPos);
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
        QueueRedraw(); // needed for pulsing spawn border
    }

    public override void _Draw()
    {
        for (int col = 0; col < GameConfig.GridWidth; col++)
        {
            for (int row = 0; row < GameConfig.GridHeight; row++)
            {
                var rect = new Rect2(col * GameConfig.TileSize, row * GameConfig.TileSize, GameConfig.TileSize, GameConfig.TileSize);
                TileType tile = _grid[col, row];

                // Fill tile
                Color fillColor = tile switch
                {
                    TileType.Wall => ColorWall,
                    TileType.Tower => ColorWall,
                    TileType.Spawn => ColorSpawn,
                    TileType.Exit => ColorExit,
                    _ => ColorEmpty
                };
                DrawRect(rect, fillColor);

                // Grid line: draw 1px border in darker shade for empty tiles
                if (tile == TileType.Empty)
                {
                    DrawRect(rect, ColorGridLine, false, 1f);
                }

                // Pixel art borders per tile type
                switch (tile)
                {
                    case TileType.Wall:
                        DrawRect(rect, BorderWall, false, 1f);
                        // 2x2 highlight in top-left corner for depth
                        DrawRect(new Rect2(rect.Position, new Vector2(2f, 2f)), BorderWallHighlight);
                        break;

                    case TileType.Tower:
                        // Tower type stored separately in TowerManager
                        DrawRect(rect, BorderBasicFilter, false, 1f);
                        break;

                    case TileType.Spawn:
                        // Pulsing 2px red border
                        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
                        var spawnBorder = new Color(1f, 0.1f + 0.3f * pulse, 0.1f);
                        DrawRect(rect, spawnBorder, false, 2f);
                        break;

                    case TileType.Exit:
                        DrawRect(rect, BorderExit, false, 1f);
                        break;
                }

                // Hover highlight
                if (_hoverTile.X == col && _hoverTile.Y == row)
                {
                    DrawRect(rect, ColorHover);
                }
            }
        }

        // Range preview circle: draw when hovering a valid placement tile with tower selected
        if (_previewTowerType != TowerManager.TowerType.None &&
            _hoverTile.X >= 0 && _hoverTile.Y >= 0 &&
            IsValidCoord(_hoverTile.X, _hoverTile.Y) &&
            _grid[_hoverTile.X, _hoverTile.Y] == TileType.Empty)
        {
            float radiusPx = _previewRange * GameConfig.TileSize;
            var center = new Vector2(
                _hoverTile.X * GameConfig.TileSize + GameConfig.TileSize * 0.5f,
                _hoverTile.Y * GameConfig.TileSize + GameConfig.TileSize * 0.5f);
            var circleColor = new Color(_previewColor, GameConfig.RangePreviewAlpha);
            DrawCircle(center, radiusPx, circleColor);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Convert to local grid coordinates
            Vector2 localPos = ToLocal(mouseMotion.Position);
            Vector2I tile = new Vector2I((int)(localPos.X / GameConfig.TileSize), (int)(localPos.Y / GameConfig.TileSize));
            if (IsValidCoord(tile.X, tile.Y))
                SetHoverTile(tile);
            else
                SetHoverTile(new Vector2I(-1, -1));
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            Vector2 localPos = ToLocal(mouseButton.Position);
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
