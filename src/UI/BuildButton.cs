using Godot;

namespace BioFilter.UI;

/// <summary>
/// Small "🛠 Build" button in the bottom-left HUD bar.
/// Visible only during the BUILD phase, hidden during WAVE phase.
/// Pressing it toggles the BuildMenu popup open/close.
/// </summary>
public partial class BuildButton : Button
{
    private BuildMenu   _buildMenu   = null!;
    private WaveManager _waveManager = null!;
    private Label       _phaseLabel  = null!;
    private Label       _statusLabel = null!;

    public override void _Ready()
    {
        _waveManager = GetNode<WaveManager>("/root/Main/VBoxContainer/GameArea/WaveManager");
        _waveManager.WaveStarted  += OnWaveStarted;
        _waveManager.WaveComplete += OnWaveComplete;

        _phaseLabel  = GetNode<Label>("../PhaseLabel");
        _statusLabel = GetNode<Label>("../StatusLabel");

        Pressed += OnPressed;

        // Start in build phase
        ApplyPhase(false);
    }

    /// <summary>Called by Main.cs right after resolving both nodes.</summary>
    public void Initialize(BuildMenu menu)
    {
        _buildMenu = menu;
        _buildMenu.TowerSelected   += (t) => SetStatus(TowerName(t));
        _buildMenu.TowerDeselected += ()  => SetStatus("Wall mode");
    }

    private void OnPressed() => _buildMenu?.Toggle();

    private void OnWaveStarted(int _)  => ApplyPhase(true);
    private void OnWaveComplete(int _) => ApplyPhase(false);

    private void ApplyPhase(bool isWave)
    {
        Visible = !isWave;

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

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
            _statusLabel.Text = text;
    }

    private static string TowerName(int t) => t switch
    {
        0 => "Basic Filter",
        1 => "Electrostatic",
        2 => "UV Steriliser",
        _ => "Unknown"
    };
}
