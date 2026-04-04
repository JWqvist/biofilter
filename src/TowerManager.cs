using BioFilter.Towers;
using Godot;

namespace BioFilter;

/// <summary>
/// Manages tower placement.
/// Intercepts left-click when a tower type is selected (via BuildPanel).
/// Wall placement (default mode) is handled by GridManager._Input as before.
/// </summary>
public partial class TowerManager : Node2D
{
    // Tower type indices matching BuildPanel order
    public enum TowerType { None = -1, BasicFilter = 0, Electrostatic = 1, UVSteriliser = 2 }

    public TowerType SelectedTower { get; private set; } = TowerType.None;

    // Injected by Main
    public GridManager GridManagerRef { get; set; }
    public GameState GameStateRef { get; set; }
    public ParticleManager ParticleManagerRef { get; set; }

    private PackedScene _basicFilterScene;
    private PackedScene _electrostaticScene;
    private PackedScene _uvSteriliserScene;

    public override void _Ready()
    {
        _basicFilterScene = GD.Load<PackedScene>("res://scenes/Towers/BasicFilter.tscn");
        _electrostaticScene = GD.Load<PackedScene>("res://scenes/Towers/Electrostatic.tscn");
        _uvSteriliserScene = GD.Load<PackedScene>("res://scenes/Towers/UVSteriliser.tscn");
    }

    // ── BuildPanel callbacks ──────────────────────────────────────────────────

    public void OnTowerSelected(int towerType)
    {
        SelectedTower = (TowerType)towerType;
        GD.Print($"TowerManager: selected {SelectedTower}");
    }

    public void OnTowerDeselected()
    {
        SelectedTower = TowerType.None;
        GD.Print("TowerManager: deselected (wall mode)");
    }

    // ── Input handling ────────────────────────────────────────────────────────

    public override void _Input(InputEvent @event)
    {
        // Only intercept left-clicks when a tower is selected
        if (SelectedTower == TowerType.None) return;
        if (@event is not InputEventMouseButton mb) return;
        if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left) return;
        if (GridManagerRef == null) return;

        Vector2I tile = GridManagerRef.WorldToGrid(mb.Position);
        TryPlaceTower(tile.X, tile.Y);

        // Mark event handled so GridManager doesn't also process it
        GetViewport().SetInputAsHandled();
    }

    private void TryPlaceTower(int col, int row)
    {
        if (GridManagerRef == null || GameStateRef == null) return;

        // Only place on empty tiles
        if (GridManagerRef.GetTileType(col, row) != TileType.Empty) return;

        int cost = GetCostForType(SelectedTower);
        if (!GameStateRef.SpendCurrency(cost))
        {
            GD.Print($"TowerManager: not enough currency (need {cost})");
            return;
        }

        // Register tile in grid
        bool placed = GridManagerRef.PlaceTile(col, row, TileType.Tower);
        if (!placed)
        {
            // Refund — placement rejected (would block airflow)
            GameStateRef.AddCurrency(cost);
            GD.Print("TowerManager: placement rejected (airflow)");
            return;
        }

        // Instantiate tower scene
        var scene = GetSceneForType(SelectedTower);
        if (scene == null) return;

        var tower = scene.Instantiate<TowerBase>();
        AddChild(tower);
        tower.GlobalPosition = TileCenter(col, row);
        tower.ParticleManagerRef = ParticleManagerRef;

        GD.Print($"TowerManager: placed {SelectedTower} at ({col},{row})");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 TileCenter(int col, int row)
    {
        return new Vector2(
            col * GameConfig.TileSize + GameConfig.TileSize * 0.5f,
            row * GameConfig.TileSize + GameConfig.TileSize * 0.5f
        );
    }

    private int GetCostForType(TowerType type) => type switch
    {
        TowerType.BasicFilter => GameConfig.BasicFilterCost,
        TowerType.Electrostatic => GameConfig.ElectrostaticCost,
        TowerType.UVSteriliser => GameConfig.UVSteriliserCost,
        _ => 0
    };

    private PackedScene GetSceneForType(TowerType type) => type switch
    {
        TowerType.BasicFilter => _basicFilterScene,
        TowerType.Electrostatic => _electrostaticScene,
        TowerType.UVSteriliser => _uvSteriliserScene,
        _ => null
    };
}
