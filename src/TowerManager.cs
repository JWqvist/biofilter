using System.Collections.Generic;
using BioFilter.Towers;
using Godot;

namespace BioFilter;

/// <summary>
/// Manages tower placement and upgrades.
/// Intercepts left-click when a tower type is selected (via BuildPanel).
/// When clicking an occupied tower tile in wall mode, signals BuildPanel to show upgrade button.
/// Wall placement (default mode) is handled by GridManager._Input as before.
/// </summary>
public partial class TowerManager : Node2D
{
    // Tower type indices matching BuildPanel order
    public enum TowerType { None = -1, BasicFilter = 0, Electrostatic = 1, UVSteriliser = 2 }

    public TowerType SelectedTower { get; private set; } = TowerType.None;

    // Injected by Main
    public GridManager? GridManagerRef { get; set; }
    public GameState? GameStateRef { get; set; }
    public ParticleManager? ParticleManagerRef { get; set; }

    // Emitted when player clicks an existing tower tile (upgrade flow)
    [Signal] public delegate void TowerClickedEventHandler(int upgradeCost, bool canAfford);
    [Signal] public delegate void TowerDeselectedEventHandler();
    /// <summary>Emitted when a tile is refunded so BuildButton can show the status message.</summary>
    [Signal] public delegate void TileRefundedEventHandler(int refundAmount);

    private PackedScene _basicFilterScene = null!;
    private PackedScene _electrostaticScene = null!;
    private PackedScene _uvSteriliserScene = null!;

    // Map from grid position → placed tower node
    private readonly Dictionary<Vector2I, TowerBase> _placedTowers = new();

    // Currently selected tower for upgrade UI
    private TowerBase? _selectedForUpgrade;

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
        _selectedForUpgrade = null;
        GD.Print($"TowerManager: selected {SelectedTower}");
    }

    public void OnTowerDeselected()
    {
        SelectedTower = TowerType.None;
        _selectedForUpgrade = null;
        GD.Print("TowerManager: deselected (wall mode)");
    }

    /// <summary>Called by BuildPanel when the Upgrade button is pressed.</summary>
    public void OnUpgradeRequested()
    {
        if (_selectedForUpgrade == null || GameStateRef == null) return;
        TowerUpgrade.UpgradeTower(_selectedForUpgrade, GameStateRef);
        // Refresh upgrade button state
        int upgradeCost = TowerUpgrade.UpgradeCost(_selectedForUpgrade);
        bool canAfford = GameStateRef.Currency >= upgradeCost;
        EmitSignal(SignalName.TowerClicked, upgradeCost, canAfford);
    }

    // ── Input handling ────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb) return;
        if (!mb.Pressed) return;
        if (GridManagerRef == null) return;

        // Use GridManager's local coordinate system for tile lookup
        Vector2I tile = GridManagerRef.WorldToGrid(mb.Position);

        if (mb.ButtonIndex == MouseButton.Right)
        {
            // Right-click handled by GridManager.TileRightClicked → Main → RefundTile
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        if (SelectedTower == TowerType.None)
        {
            // Wall mode — check if player clicked an existing tower to select it for upgrade
            if (_placedTowers.TryGetValue(tile, out var existingTower))
            {
                _selectedForUpgrade = existingTower;
                if (!existingTower.IsUpgraded)
                {
                    int upgradeCost = TowerUpgrade.UpgradeCost(existingTower);
                    bool canAfford = GameStateRef != null && GameStateRef.Currency >= upgradeCost;
                    EmitSignal(SignalName.TowerClicked, upgradeCost, canAfford);
                }
                else
                {
                    // Already upgraded — still signal but with 0 cost to indicate max tier
                    EmitSignal(SignalName.TowerClicked, 0, false);
                }
                GetViewport().SetInputAsHandled();
            }
            else
            {
                // Clicked empty space — deselect tower
                if (_selectedForUpgrade != null)
                {
                    _selectedForUpgrade = null;
                    EmitSignal(SignalName.TowerDeselected);
                }
            }
            return;
        }

        // Tower placement mode
        TryPlaceTower(tile.X, tile.Y);
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
        tower.GridPos = new Vector2I(col, row);
        tower.ParticleManagerRef = ParticleManagerRef;

        _placedTowers[new Vector2I(col, row)] = tower;

        GD.Print($"TowerManager: placed {SelectedTower} at ({col},{row})");
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called on right-click. Removes the tile, refunds 50% of its cost, and emits TileRefunded.
    /// Returns the refund amount (0 if nothing to refund).
    /// </summary>
    public int RefundTile(int col, int row)
    {
        if (GridManagerRef == null || GameStateRef == null) return 0;

        TileType tileType = GridManagerRef.GetTileType(col, row);
        if (tileType == TileType.Empty || tileType == TileType.Spawn || tileType == TileType.Exit)
            return 0;

        int originalCost = GetCostForTileType(tileType, col, row);
        int refundAmount = (int)(originalCost * GameConfig.RefundPercent);

        // Remove tower node if it's a placed tower
        var gridPos = new Vector2I(col, row);
        if (_placedTowers.TryGetValue(gridPos, out var tower))
        {
            tower.QueueFree();
            _placedTowers.Remove(gridPos);
        }

        // Clear the grid tile
        GridManagerRef.RemoveTile(col, row);

        // Refund currency
        if (refundAmount > 0)
            GameStateRef.AddCurrency(refundAmount);

        EmitSignal(SignalName.TileRefunded, refundAmount);
        GD.Print($"TowerManager: refunded {tileType} at ({col},{row}) for ${refundAmount}");
        return refundAmount;
    }

    private int GetCostForTileType(TileType tileType, int col, int row)
    {
        // Walls are free; towers have costs
        if (tileType == TileType.Wall) return 0;
        if (tileType != TileType.Tower) return 0;

        // Determine tower type from placed tower node
        var gridPos = new Vector2I(col, row);
        if (_placedTowers.TryGetValue(gridPos, out var tower))
            return tower.Cost;

        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 TileCenter(int col, int row)
    {
        // Returns world position of tile center, relative to TowerManager's parent
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

    private PackedScene? GetSceneForType(TowerType type) => type switch
    {
        TowerType.BasicFilter => _basicFilterScene,
        TowerType.Electrostatic => _electrostaticScene,
        TowerType.UVSteriliser => _uvSteriliserScene,
        _ => null
    };
}
