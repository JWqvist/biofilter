using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Bottom HUD panel for selecting tower types and upgrading selected towers.
/// Manages build mode: wall vs tower selection.
/// 
/// Upgrade flow:
///   1. Player clicks an existing tower tile (TowerManager handles click)
///   2. TowerManager emits TowerClicked(cost, canAfford)
///   3. BuildPanel shows "Upgrade [$cost]" button (disabled if can't afford)
///   4. Player presses upgrade → calls TowerManager.OnUpgradeRequested()
/// </summary>
public partial class BuildPanel : CanvasLayer
{
    // Signals to TowerManager
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();
    [Signal] public delegate void UpgradeRequestedEventHandler();

    private int _selectedTower = -1; // -1 = none (wall mode)

    private Button _basicFilterBtn = null!;
    private Button _electrostaticBtn = null!;
    private Button _uvSteriliserBtn = null!;
    private Button _upgradeBtn = null!;
    private Label _statusLabel = null!;

    private static readonly string[] TowerNames = { "Basic Filter", "Electrostatic", "UV Steriliser" };

    public override void _Ready()
    {
        var panel = GetNode<HBoxContainer>("Panel/HBoxContainer");
        _basicFilterBtn = panel.GetNode<Button>("BasicFilterBtn");
        _electrostaticBtn = panel.GetNode<Button>("ElectrostaticBtn");
        _uvSteriliserBtn = panel.GetNode<Button>("UVSteriliserBtn");

        // Upgrade button — hidden by default, shown when a tower is selected
        _upgradeBtn = panel.GetNode<Button>("UpgradeBtn");
        _upgradeBtn.Visible = false;
        _upgradeBtn.Pressed += OnUpgradeBtnPressed;

        // Status label — shows current build mode
        _statusLabel = GetNode<Label>("Panel/StatusLabel");
        _statusLabel.Text = "Wall mode";

        _basicFilterBtn.Pressed += () => OnTowerButtonPressed(0, _basicFilterBtn);
        _electrostaticBtn.Pressed += () => OnTowerButtonPressed(1, _electrostaticBtn);
        _uvSteriliserBtn.Pressed += () => OnTowerButtonPressed(2, _uvSteriliserBtn);

        // Set button labels with costs from GameConfig
        _basicFilterBtn.Text = $"Basic Filter [${GameConfig.BasicFilterCost}]";
        _electrostaticBtn.Text = $"Electrostatic [${GameConfig.ElectrostaticCost}]";
        _uvSteriliserBtn.Text = $"UV Steriliser [${GameConfig.UVSteriliserCost}]";
    }

    // ── Tower build buttons ───────────────────────────────────────────────────

    private void OnTowerButtonPressed(int towerType, Button btn)
    {
        HideUpgradeButton();

        if (_selectedTower == towerType)
        {
            // Deselect — go back to wall placement mode
            _selectedTower = -1;
            ClearHighlights();
            SetStatus("Wall mode");
            EmitSignal(SignalName.TowerDeselected);
        }
        else
        {
            _selectedTower = towerType;
            HighlightButton(btn);
            SetStatus($"Tower: {TowerNames[towerType]}");
            EmitSignal(SignalName.TowerSelected, towerType);
        }
    }

    // ── Upgrade button ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TowerManager when the player clicks an existing tower tile.
    /// upgradeCost == 0 means tower is already at max tier.
    /// </summary>
    public void ShowUpgradeButton(int upgradeCost, bool canAfford)
    {
        // Deselect any build mode selection
        _selectedTower = -1;
        ClearHighlights();
        SetStatus("Tower selected");

        if (upgradeCost <= 0)
        {
            // Already max tier
            _upgradeBtn.Text = "MAX TIER";
            _upgradeBtn.Disabled = true;
            _upgradeBtn.Visible = true;
        }
        else
        {
            _upgradeBtn.Text = $"Upgrade [${upgradeCost}]";
            _upgradeBtn.Disabled = !canAfford;
            _upgradeBtn.Visible = true;
        }
    }

    public void HideUpgradeButton()
    {
        if (_upgradeBtn != null)
            _upgradeBtn.Visible = false;
    }

    private void OnUpgradeBtnPressed()
    {
        EmitSignal(SignalName.UpgradeRequested);
        // Button will be refreshed when TowerManager re-emits TowerClicked
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
            _statusLabel.Text = text;
    }

    private void HighlightButton(Button active)
    {
        _basicFilterBtn.Modulate = _basicFilterBtn == active ? Colors.Yellow : Colors.White;
        _electrostaticBtn.Modulate = _electrostaticBtn == active ? Colors.Yellow : Colors.White;
        _uvSteriliserBtn.Modulate = _uvSteriliserBtn == active ? Colors.Yellow : Colors.White;
    }

    private void ClearHighlights()
    {
        _basicFilterBtn.Modulate = Colors.White;
        _electrostaticBtn.Modulate = Colors.White;
        _uvSteriliserBtn.Modulate = Colors.White;
    }
}
