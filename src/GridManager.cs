using Godot;

public partial class GridManager : Node2D
{
    public const int GRID_WIDTH = 30;
    public const int GRID_HEIGHT = 20;
    public const int TILE_SIZE = 16;

    private TileType[,] _grid = new TileType[GRID_WIDTH, GRID_HEIGHT];
    private Vector2I _hoverTile = new Vector2I(-1, -1);

    // Colors
    private static readonly Color ColorEmpty = new Color("#1a1a2e");
    private static readonly Color ColorGridLine = new Color("#2a2a3e");
    private static readonly Color ColorWall = new Color("#4a4a6a");
    private static readonly Color ColorSpawn = new Color("#d50000");
    private static readonly Color ColorExit = new Color("#ff6d00");
    private static readonly Color ColorHover = new Color(1f, 1f, 1f, 0.3f);

    public override void _Ready()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int col = 0; col < GRID_WIDTH; col++)
        {
            for (int row = 0; row < GRID_HEIGHT; row++)
            {
                _grid[col, row] = TileType.Empty;
            }
        }

        // Spawn: column 0, row 10 (left middle)
        _grid[0, 10] = TileType.Spawn;

        // Exit: entire column 29 (right edge)
        for (int row = 0; row < GRID_HEIGHT; row++)
        {
            _grid[29, row] = TileType.Exit;
        }
    }

    public void PlaceTile(int col, int row, TileType type)
    {
        if (!IsValidCoord(col, row)) return;
        if (_grid[col, row] == TileType.Spawn) return;
        if (_grid[col, row] == TileType.Exit) return;
        _grid[col, row] = type;
        QueueRedraw();
    }

    public void RemoveTile(int col, int row)
    {
        if (!IsValidCoord(col, row)) return;
        if (_grid[col, row] == TileType.Spawn) return;
        if (_grid[col, row] == TileType.Exit) return;
        _grid[col, row] = TileType.Empty;
        QueueRedraw();
    }

    public TileType GetTileType(int col, int row)
    {
        if (!IsValidCoord(col, row)) return TileType.Empty;
        return _grid[col, row];
    }

    private bool IsValidCoord(int col, int row)
    {
        return col >= 0 && col < GRID_WIDTH && row >= 0 && row < GRID_HEIGHT;
    }

    public Vector2I WorldToGrid(Vector2 worldPos)
    {
        int col = (int)(worldPos.X / TILE_SIZE);
        int row = (int)(worldPos.Y / TILE_SIZE);
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

    public override void _Draw()
    {
        for (int col = 0; col < GRID_WIDTH; col++)
        {
            for (int row = 0; row < GRID_HEIGHT; row++)
            {
                var rect = new Rect2(col * TILE_SIZE, row * TILE_SIZE, TILE_SIZE, TILE_SIZE);
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

                // Hover highlight
                if (_hoverTile.X == col && _hoverTile.Y == row)
                {
                    DrawRect(rect, ColorHover);
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2I tile = WorldToGrid(mouseMotion.Position);
            if (IsValidCoord(tile.X, tile.Y))
                SetHoverTile(tile);
            else
                SetHoverTile(new Vector2I(-1, -1));
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            Vector2I tile = WorldToGrid(mouseButton.Position);
            if (!IsValidCoord(tile.X, tile.Y)) return;

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (GetTileType(tile.X, tile.Y) == TileType.Empty)
                    PlaceTile(tile.X, tile.Y, TileType.Wall);
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                if (GetTileType(tile.X, tile.Y) == TileType.Wall)
                    RemoveTile(tile.X, tile.Y);
            }
        }
    }
}
