using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Bottom HUD panel for selecting tower types and upgrading selected towers.
/// Tower build buttons are disabled during the wave phase.
/// </summary>
public partial class BuildPanel : CanvasLayer
{
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();
    [Signal] public delegate void UpgradeRequestedEventHandler();

    private int _selectedTower = -1;
    private bool _isWaveActive = false;

    private Button _basicFilterBtn = null!;
    private Button _electrostaticBtn = null!;
    private Button _uvSteriliserBtn = null!;
    private Button _upgradeBtn = null!;
    private Label _statusLabel = null!;
    private Label _phaseLabel = null!;
    private WaveManager _waveManager = null!;

    private static readonly string[] TowerNames = { "Basic Filter", "Electrostatic", "UV Steriliser" };

    public override void _Ready()
    {
        var panel = GetNode<HBoxContainer>("Panel/HBoxContainer");
        _basicFilterBtn = panel.GetNode<Button>("BasicFilterBtn");
        _electrostaticBtn = panel.GetNode<Button>("ElectrostaticBtn");
        _uvSteriliserBtn = panel.GetNode<Button>("UVSteriliserBtn");
        _upgradeBtn = panel.GetNode<Button>("UpgradeBtn");
        _upgradeBtn.Visible = false;
        _upgradeBtn.Pressed += OnUpgradeBtnPressed;

        _statusLabel = GetNode<Label>("Panel/StatusLabel");
        _statusLabel.Text = "Wall mode";

        // Phase indicator label — shows BUILD PHASE / WAVE PHASE
        _phaseLabel = GetNode<Label>("Panel/PhaseLabel");
        SetPhase(false);

        _basicFilterBtn.Pressed += () => OnTowerButtonPressed(0, _basicFilterBtn);
        _electrostaticBtn.Pressed += () => OnTowerButtonPressed(1, _electrostaticBtn);
        _uvSteriliserBtn.Pressed += () => OnTowerButtonPressed(2, _uvSteriliserBtn);

        _basicFilterBtn.Text = $"Basic Filter [${GameConfig.BasicFilterCost}]";
        _electrostaticBtn.Text = $"Electrostatic [${GameConfig.ElectrostaticCost}]";
        _uvSteriliserBtn.Text = $"UV Steriliser [${GameConfig.UVSteriliserCost}]";

        // Connect to WaveManager signals
        _waveManager = GetNode<WaveManager>("/root/Main/VBoxContainer/GameArea/WaveManager");
        _waveManager.WaveStarted += OnWaveStarted;
        _waveManager.WaveComplete += OnWaveComplete;
    }

    private void OnWaveStarted(int waveNumber)
    {
        _isWaveActive = true;
        SetPhase(true);
        // Disable tower placement buttons during wave
        _basicFilterBtn.Disabled = true;
        _electrostaticBtn.Disabled = true;
        _uvSteriliserBtn.Disabled = true;
        // Deselect any active tower selection
        _selectedTower = -1;
        ClearHighlights();
        HideUpgradeButton();
        SetStatus("Wave in progress…");
    }

    private void OnWaveComplete(int waveNumber)
    {
        _isWaveActive = false;
        SetPhase(false);
        // Re-enable tower placement buttons
        _basicFilterBtn.Disabled = false;
        _electrostaticBtn.Disabled = false;
        _uvSteriliserBtn.Disabled = false;
        SetStatus("Wall mode");
    }

    private void SetPhase(bool isWave)
    {
        if (_phaseLabel == null) return;
        if (isWave)
        {
            _phaseLabel.Text = "⚡ WAVE PHASE";
            _phaseLabel.AddThemeColorOverride("font_color", new Color("#ff6d00"));
        }
        else
        {
            _phaseLabel.Text = "🛠 BUILD PHASE";
            _phaseLabel.AddThemeColorOverride("font_color", new Color("#00c853"));
        }
    }

    private void OnTowerButtonPressed(int towerType, Button btn)
    {
        if (_isWaveActive) return; // Ignore during wave
        HideUpgradeButton();

        if (_selectedTower == towerType)
        {
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

    public void ShowUpgradeButton(int upgradeCost, bool canAfford)
    {
        if (_isWaveActive) return;
        _selectedTower = -1;
        ClearHighlights();
        SetStatus("Tower selected");

        if (upgradeCost <= 0)
        {
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
    }

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
