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
        ToxicSprayer     = 7,
        PlasmaBurst      = 8,
    }

    public TowerType SelectedTower { get; private set; } = TowerType.None;

    // Injected by Main
    public GridManager? GridManagerRef { get; set; }
    public bool IsWaveActive { get; set; } = false;
    public GameState? GameStateRef { get; set; }
    public ParticleManager? ParticleManagerRef { get; set; }
    public WaveManager? WaveManagerRef { get; set; }

    // Emitted when player clicks an existing tower tile (upgrade flow)
    [Signal] public delegate void TowerClickedEventHandler(int upgradeCost, bool canAfford);
    [Signal] public delegate void TowerDeselectedEventHandler();
    /// <summary>Emitted when a tile is refunded so BuildButton can show the status message.</summary>
    [Signal] public delegate void TileRefundedEventHandler(int refundAmount);
    /// <summary>Emitted when a tower is successfully placed (used by AudioManager).</summary>
    [Signal] public delegate void TowerPlacedEventHandler();

    private PackedScene _basicFilterScene = null!;
    private PackedScene _electrostaticScene = null!;
    private PackedScene _uvSteriliserScene = null!;
    private PackedScene _vortexSeparatorScene = null!;
    private PackedScene _powerCoreScene = null!;
    private PackedScene _bioNeutraliserScene = null!;
    private PackedScene _magneticCageScene = null!;
    private PackedScene _toxicSprayerScene = null!;
    private PackedScene _plasmaBurstScene   = null!;

    // Map from grid position -> placed tower node
    private readonly Dictionary<Vector2I, TowerBase> _placedTowers = new();

    // Currently selected tower for upgrade UI
    private TowerBase? _selectedForUpgrade;

    // Hover tracking for range-circle overlay on placed towers
    private Vector2I _hoveredTile = new Vector2I(-1, -1);

    public override void _Ready()
    {
        _basicFilterScene     = GD.Load<PackedScene>("res://scenes/Towers/BasicFilter.tscn");
        _electrostaticScene   = GD.Load<PackedScene>("res://scenes/Towers/Electrostatic.tscn");
        _uvSteriliserScene    = GD.Load<PackedScene>("res://scenes/Towers/UVSteriliser.tscn");
        _vortexSeparatorScene = GD.Load<PackedScene>("res://scenes/Towers/VortexSeparator.tscn");
        _powerCoreScene       = GD.Load<PackedScene>("res://scenes/Towers/PowerCore.tscn");
        _bioNeutraliserScene  = GD.Load<PackedScene>("res://scenes/Towers/BioNeutraliser.tscn");
        _magneticCageScene    = GD.Load<PackedScene>("res://scenes/Towers/MagneticCage.tscn");
        _toxicSprayerScene    = GD.Load<PackedScene>("res://scenes/Towers/ToxicSprayer.tscn");
        _plasmaBurstScene     = GD.Load<PackedScene>("res://scenes/Towers/PlasmaBurst.tscn");
    }

    // ── Public helpers ────────────────────────────────────────────────────────

    /// <summary>Returns the tower placed at the given grid position, or null.</summary>
    public TowerBase? GetTowerAt(Vector2I gridPos)
    {
        _placedTowers.TryGetValue(gridPos, out var tower);
        return tower;
    }

    /// <summary>Read-only view of all placed towers (used by save system).</summary>
    public System.Collections.Generic.IReadOnlyDictionary<Vector2I, TowerBase> GetPlacedTowers()
        => _placedTowers;

    /// <summary>Removes and frees all placed tower nodes (used by load system).</summary>
    public void ClearAllTowers()
    {
        foreach (var tower in _placedTowers.Values)
            tower.QueueFree();
        _placedTowers.Clear();
    }

    /// <summary>
    /// Places a tower directly without spending currency (used by load system).
    /// Registers the tile in the grid and instantiates the tower node.
    /// </summary>
    public void PlaceTowerDirect(int col, int row, TowerType type)
    {
        if (GridManagerRef == null) return;

        bool placed = GridManagerRef.PlaceTile(col, row, TileType.Tower);
        if (!placed) return;

        var scene = GetSceneForType(type);
        if (scene == null) return;

        var tower = scene.Instantiate<TowerBase>();
        AddChild(tower);
        tower.Position = TileCenter(col, row);
        tower.GridPos = new Vector2I(col, row);
        tower.TowerTypeId = type;
        tower.ParticleManagerRef = ParticleManagerRef;

        switch (tower)
        {
            case VortexSeparator vortex:
                vortex.GridManagerRef = GridManagerRef;
                vortex.ApplyVortexPenalty();
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
        }

        _placedTowers[new Vector2I(col, row)] = tower;
    }

    // ── Per-frame hover tracking ──────────────────────────────────────────────

    public override void _Process(double _delta)
    {
        if (GridManagerRef == null) return;
        var tile = GridManagerRef.MouseToTile();
        if (tile != _hoveredTile)
        {
            _hoveredTile = tile;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Draws a translucent range circle over an already-placed tower when the player hovers it.
    /// Only shown in wall/idle mode (not while a tower type is selected for placement).
    /// </summary>
    public override void _Draw()
    {
        // Only show when not in tower-placement mode (Sprint 9 covers that case)
        if (SelectedTower != TowerType.None) return;
        if (_hoveredTile.X < 0 || _hoveredTile.Y < 0) return;
        if (!_placedTowers.TryGetValue(_hoveredTile, out var tower)) return;

        float range = tower.Range;
        if (range <= 0f) return;

        float radiusPx = range * GameConfig.TileSize;
        var center = TileCenter(_hoveredTile.X, _hoveredTile.Y);
        var baseColor = tower.RangeColor;

        // Filled translucent disc + outline
        DrawCircle(center, radiusPx, new Color(baseColor, 0.12f));
        DrawArc(center, radiusPx, 0f, Mathf.Tau, 64, new Color(baseColor, 0.55f), 1.2f);
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
        if (IsWaveActive) return; // block all building during wave
        Vector2I tile = GridManagerRef.MouseToTile();

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
        tower.TowerTypeId = SelectedTower;
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

        EmitSignal(SignalName.TowerPlaced);
        GD.Print($"TowerManager: placed {SelectedTower} at ({col},{row})");
    }

    // ── Sell mode ─────────────────────────────────────────────────────────────

    /// <summary>When true, hovering over a wall or tower shows a red sell-mode tint.</summary>
    public bool SellModeActive { get; set; } = false;

    // ── Refund ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called on right-click. Removes the tile, refunds 50% of its cost, and emits TileRefunded.
    /// Sell is only allowed during the build phase (not during an active wave).
    /// Returns the refund amount (0 if nothing to refund).
    /// </summary>
    public int RefundTile(int col, int row)
    {
        if (IsWaveActive) return 0; // sell only during build phase
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

    // ── Save / Load ───────────────────────────────────────────────────────────

    /// <summary>Returns a snapshot of all placed towers for saving.</summary>
    public System.Collections.Generic.List<TowerSaveEntry> GetPlacedTowerData()
    {
        var result = new System.Collections.Generic.List<TowerSaveEntry>();
        foreach (var (pos, tower) in _placedTowers)
        {
            result.Add(new TowerSaveEntry
            {
                X        = pos.X,
                Y        = pos.Y,
                Type     = (int)GetTypeForTower(tower),
                Upgraded = tower.IsUpgraded,
            });
        }
        return result;
    }

    /// <summary>Reconstructs all towers from save data (bypasses cost/airflow checks).</summary>
    public void LoadTowers(System.Collections.Generic.List<TowerSaveEntry> entries)
    {
        if (GridManagerRef == null) return;

        // Two-pass: non-BioNeutraliser first so ApplyBoost finds already-placed neighbours
        void PlaceEntry(TowerSaveEntry e)
        {
            var scene = GetSceneForType((TowerType)e.Type);
            if (scene == null) return;

            GridManagerRef.ForcePlaceTile(e.X, e.Y, TileType.Tower);

            var tower = scene.Instantiate<BioFilter.Towers.TowerBase>();
            AddChild(tower);
            tower.Position          = TileCenter(e.X, e.Y);
            tower.GridPos           = new Vector2I(e.X, e.Y);
            tower.ParticleManagerRef = ParticleManagerRef;

            switch (tower)
            {
                case VortexSeparator vortex:
                    vortex.GridManagerRef = GridManagerRef;
                    vortex.ApplyVortexPenalty();
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
            }

            if (e.Upgraded)
                TowerUpgrade.ApplyUpgrade(tower);

            _placedTowers[new Vector2I(e.X, e.Y)] = tower;
        }

        foreach (var e in entries)
            if (e.Type != (int)TowerType.BioNeutraliser)
                PlaceEntry(e);
        foreach (var e in entries)
            if (e.Type == (int)TowerType.BioNeutraliser)
                PlaceEntry(e);

        GridManagerRef.TriggerAirflowRefresh();
        GD.Print($"TowerManager: loaded {entries.Count} tower(s) from save");
    }

    private static TowerType GetTypeForTower(BioFilter.Towers.TowerBase tower) => tower switch
    {
        BasicFilter     => TowerType.BasicFilter,
        Electrostatic   => TowerType.Electrostatic,
        UVSteriliser    => TowerType.UVSteriliser,
        VortexSeparator => TowerType.VortexSeparator,
        PowerCore       => TowerType.PowerCore,
        BioNeutraliser  => TowerType.BioNeutraliser,
        MagneticCage    => TowerType.MagneticCage,
        ToxicSprayer    => TowerType.ToxicSprayer,
        PlasmaBurst     => TowerType.PlasmaBurst,
        _               => TowerType.None,
    };

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
        TowerType.ToxicSprayer    => GameConfig.ToxicSprayerCost,
        TowerType.PlasmaBurst     => GameConfig.PlasmaBurstCost,
        _                         => 0
    };

    private float GetRangeForType(TowerType type) => type switch
    {
        TowerType.BasicFilter     => GameConfig.BasicFilterRange,
        TowerType.Electrostatic   => GameConfig.ElectrostaticRange,
        TowerType.UVSteriliser    => GameConfig.UVSteriliserRange,
        TowerType.VortexSeparator => GameConfig.VortexSeparatorRange,
        TowerType.PowerCore       => 0f,
        TowerType.BioNeutraliser  => GameConfig.BioNeutraliserRange,
        TowerType.MagneticCage    => GameConfig.MagneticCageRange,
        TowerType.ToxicSprayer    => GameConfig.ToxicSprayerRange,
        TowerType.PlasmaBurst     => GameConfig.PlasmaBurstRange,
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
        TowerType.ToxicSprayer    => new Color("#76ff03"),
        TowerType.PlasmaBurst     => new Color("#2979ff"),
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
        TowerType.ToxicSprayer    => _toxicSprayerScene,
        TowerType.PlasmaBurst     => _plasmaBurstScene,
        _                         => null
    };
}
