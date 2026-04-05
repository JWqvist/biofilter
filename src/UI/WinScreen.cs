using Godot;

namespace BioFilter.UI;

/// <summary>
/// Full-screen overlay shown when all waves are survived.
/// Triggered by WaveManager.GameWon signal.
/// </summary>
public partial class WinScreen : CanvasLayer
{
    private Label _titleLabel;
    private Label _subtitleLabel;
    private Label _statLabel;
    private Button _playAgainButton;

    public override void _Ready()
    {
        _titleLabel    = GetNode<Label>("Panel/VBoxContainer/TitleLabel");
        _subtitleLabel = GetNode<Label>("Panel/VBoxContainer/SubtitleLabel");
        _statLabel     = GetNode<Label>("Panel/VBoxContainer/StatLabel");
        _playAgainButton = GetNode<Button>("Panel/VBoxContainer/PlayAgainButton");

        _playAgainButton.Pressed += OnPlayAgainPressed;

        // Hidden until triggered
        Visible = false;
    }

    public void Show(int wavesWon)
    {
        _statLabel.Text = $"Waves survived: {wavesWon}";
        Visible = true;
    }

    private void OnPlayAgainPressed()
    {
        GetTree().ReloadCurrentScene();
    }
}
