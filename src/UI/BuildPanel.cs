using Godot;

namespace BioFilter.UI;

/// <summary>
/// Bottom HUD panel for selecting tower types.
/// Manages build mode: wall vs tower selection.
/// </summary>
public partial class BuildPanel : CanvasLayer
{
    // Signals to TowerManager
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();

    private int _selectedTower = -1; // -1 = none (wall mode)

    private Button _basicFilterBtn;
    private Button _electrostaticBtn;
    private Button _uvSteriliserBtn;

    public override void _Ready()
    {
        var panel = GetNode<HBoxContainer>("Panel/HBoxContainer");
        _basicFilterBtn = panel.GetNode<Button>("BasicFilterBtn");
        _electrostaticBtn = panel.GetNode<Button>("ElectrostaticBtn");
        _uvSteriliserBtn = panel.GetNode<Button>("UVSteriliserBtn");

        _basicFilterBtn.Pressed += () => OnTowerButtonPressed(0, _basicFilterBtn);
        _electrostaticBtn.Pressed += () => OnTowerButtonPressed(1, _electrostaticBtn);
        _uvSteriliserBtn.Pressed += () => OnTowerButtonPressed(2, _uvSteriliserBtn);

        // Set button labels with costs from GameConfig
        _basicFilterBtn.Text = $"Basic Filter [${GameConfig.BasicFilterCost}]";
        _electrostaticBtn.Text = $"Electrostatic [${GameConfig.ElectrostaticCost}]";
        _uvSteriliserBtn.Text = $"UV Steriliser [${GameConfig.UVSteriliserCost}]";
    }

    private void OnTowerButtonPressed(int towerType, Button btn)
    {
        if (_selectedTower == towerType)
        {
            // Deselect — go back to wall placement mode
            _selectedTower = -1;
            ClearHighlights();
            EmitSignal(SignalName.TowerDeselected);
        }
        else
        {
            _selectedTower = towerType;
            HighlightButton(btn);
            EmitSignal(SignalName.TowerSelected, towerType);
        }
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
