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
    // Tower type indices matching BuildPanel/BuildMenu order
    public enum TowerType
    {
        None             = -1,
        BasicFilter      = 0,
        Electrostatic    = 1,
        UVSteriliser     = 2,
        VortexSeparator  = 3,
        PowerCore        = 4,
        BioNeutraliser   = 5,
        MagneticCage     = 6,
    }

    public TowerType SelectedTower { get; private set; } = TowerType.None;

    // Injected by Main
    public GridManager? GridManagerRef { get; set; }
    public GameState? GameStateRef { get; set; }
    public ParticleManager? ParticleManagerRef { get; set; }
    public WaveManager? WaveManagerRef { get; set; }

    // Emitted when player clicks an existing tower tile (upgrade flow)
    [Signal] public delegate void TowerClickedEventHandler(int upgradeCost, bool canAfford);
    [Signal] public delegate void TowerDeselectedEventHandler();
    /// <summary>Emitted when a tile is refunded so BuildButton can show the status message.</summary>
    [Signal] public delegate void TileRefundedEventHandler(int refundAmount);

    private PackedScene _basicFilterScene = null!;
    private PackedScene _electrostaticScene = null!;
    private PackedScene _uvSteriliserScene = null!;
    private PackedScene _vortexSeparatorScene = null!;
    private PackedScene _powerCoreScene = null!;
    private PackedScene _bioNeutraliserScene = null!;
    private PackedScene _magneticCageScene = null!;

    // Map from grid position -> placed tower node
    private readonly Dictionary<Vector2I, TowerBase> _placedTowers = new();

    // Currently selected tower for upgrade UI
    private TowerBase? _selectedForUpgrade;

    public override void _Ready()
    {
        _basicFilterScene     = GD.Load<PackedScene>("res://scenes/Towers/BasicFilter.tscn");
        _electrostaticScene   = GD.Load<PackedScene>("res://scenes/Towers/Electrostatic.tscn");
        _uvSteriliserScene    = GD.Load<PackedScene>("res://scenes/Towers/UVSteriliser.tscn");
        _vortexSeparatorScene = GD.Load<PackedScene>("res://scenes/Towers/VortexSeparator.tscn");
        _powerCoreScene       = GD.Load<PackedScene>("res://scenes/Towers/PowerCore.tscn");
        _bioNeutraliserScene  = GD.Load<PackedScene>("res://scenes/Towers/BioNeutraliser.tscn");
        _magneticCageScene    = GD.Load<PackedScene>("res://scenes/Towers/MagneticCage.tscn");
    }

    // ── Public helpers ────────────────────────────────────────────────────────

    /// <summary>Returns the tower placed at the given grid position, or null.</summary>
    public TowerBase? GetTowerAt(Vector2I gridPos)
    {
        _placedTowers.TryGetValue(gridPos, out var tower);
        return tower;
    }

    // ── BuildPanel callbacks ──────────────────────────────────────────────────

    public void OnTowerSelected(int towerType)
    {
        SelectedTower = (TowerType)towerType;
        _selectedForUpgrade = null;
        // Update range preview on GridManager
        if (GridManagerRef != null)
        {
            float range = GetRangeForType(SelectedTower);
            Color color = GetColorForType(SelectedTower);
            GridManagerRef.SetRangePreview(SelectedTower, range, color);
        }
        GD.Print($"TowerManager: selected {SelectedTower}");
    }

    public void OnTowerDeselected()
    {
        SelectedTower = TowerType.None;
        _selectedForUpgrade = null;
        GridManagerRef?.ClearRangePreview();
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

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb) return;
        if (!mb.Pressed) return;
        if (GridManagerRef == null) return;

        // Only handle clicks when grid active and no menu open
        if (!GridManagerRef.IsMouseOverGrid()) return;
        if (!GridManagerRef.WallPlacementActive) return; // block during wave
        Vector2I tile = GridManagerRef.MouseToTile();
        if (!GridManagerRef.IsMouseOverGrid()) return;

        if (mb.ButtonIndex == MouseButton.Right)
        {
            // Right-click handled by GridManager.TileRightClicked -> Main -> RefundTile
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        if (SelectedTower == TowerType.None)
        {
            // Wall mode -- check if player clicked an existing tower to select it for upgrade
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
                    // Already upgraded -- still signal but with 0 cost to indicate max tier
                    EmitSignal(SignalName.TowerClicked, 0, false);
                }
                GetViewport().SetInputAsHandled();
            }
            else
            {
                // Clicked empty space -- deselect tower
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
            // Refund -- placement rejected (would block airflow)
            GameStateRef.AddCurrency(cost);
            GD.Print("TowerManager: placement rejected (airflow)");
            return;
        }

        // Instantiate tower scene
        var scene = GetSceneForType(SelectedTower);
        if (scene == null) return;

        var tower = scene.Instantiate<TowerBase>();
        AddChild(tower);
        tower.Position = TileCenter(col, row);
        tower.GridPos = new Vector2I(col, row);
        tower.ParticleManagerRef = ParticleManagerRef;

        // Sprint 12: wire extra dependencies for new tower types
        switch (tower)
        {
            case VortexSeparator vortex:
                vortex.GridManagerRef = GridManagerRef;
                vortex.ApplyVortexPenalty();
                // Trigger path recalculation so existing particles reroute
                GridManagerRef.TriggerAirflowRefresh();
                break;

            case PowerCore powerCore:
                powerCore.WaveManagerRef = WaveManagerRef;
                powerCore.GameStateRef   = GameStateRef;
                powerCore.ConnectWaveManager();
                break;

            case BioNeutraliser neutraliser:
                neutraliser.TowerManagerRef = this;
                neutraliser.ApplyBoost();
                break;

            case MagneticCage:
                // no extra wiring needed
                break;
        }

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
            // Sprint 12: cleanup for new tower types
            if (tower is BioNeutraliser bn)
                bn.RemoveBoost();
            if (tower is VortexSeparator vs)
            {
                vs.RemoveVortexPenalty();
                // Recalc path so particles get shorter routes back
                GridManagerRef.TriggerAirflowRefresh();
            }

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
        TowerType.BasicFilter     => GameConfig.BasicFilterCost,
        TowerType.Electrostatic   => GameConfig.ElectrostaticCost,
        TowerType.UVSteriliser    => GameConfig.UVSteriliserCost,
        TowerType.VortexSeparator => GameConfig.VortexSeparatorCost,
        TowerType.PowerCore       => GameConfig.PowerCoreCost,
        TowerType.BioNeutraliser  => GameConfig.BioNeutraliserCost,
        TowerType.MagneticCage    => GameConfig.MagneticCageCost,
        _                         => 0
    };

    private float GetRangeForType(TowerType type) => type switch
    {
        TowerType.BasicFilter     => GameConfig.BasicFilterRange,
        TowerType.Electrostatic   => GameConfig.ElectrostaticRange,
        TowerType.UVSteriliser    => GameConfig.UVSteriliserRange,
        TowerType.VortexSeparator => GameConfig.VortexSeparatorRange,
        TowerType.PowerCore       => 0f,
        TowerType.BioNeutraliser  => 1.5f,
        TowerType.MagneticCage    => GameConfig.MagneticCageRange,
        _                         => 0f
    };

    private Color GetColorForType(TowerType type) => type switch
    {
        TowerType.BasicFilter     => new Color("#3daa50"),
        TowerType.Electrostatic   => new Color("#1a8a9a"),
        TowerType.UVSteriliser    => new Color("#8a4aaa"),
        TowerType.VortexSeparator => new Color("#00bcd4"),
        TowerType.PowerCore       => new Color("#ffd700"),
        TowerType.BioNeutraliser  => new Color("#9c27b0"),
        TowerType.MagneticCage    => new Color("#795548"),
        _                         => Colors.White
    };

    private PackedScene? GetSceneForType(TowerType type) => type switch
    {
        TowerType.BasicFilter     => _basicFilterScene,
        TowerType.Electrostatic   => _electrostaticScene,
        TowerType.UVSteriliser    => _uvSteriliserScene,
        TowerType.VortexSeparator => _vortexSeparatorScene,
        TowerType.PowerCore       => _powerCoreScene,
        TowerType.BioNeutraliser  => _bioNeutraliserScene,
        TowerType.MagneticCage    => _magneticCageScene,
        _                         => null
    };
}
