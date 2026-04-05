using Godot;

namespace BioFilter.UI;

/// <summary>
/// Full-screen overlay shown when population reaches zero.
/// Triggered by GameState.GameOver signal.
/// </summary>
public partial class GameOver : CanvasLayer
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _statLabel = null!;
    private Button _restartButton = null!;

    public override void _Ready()
    {
        _titleLabel    = GetNode<Label>("Panel/VBoxContainer/TitleLabel");
        _subtitleLabel = GetNode<Label>("Panel/VBoxContainer/SubtitleLabel");
        _statLabel     = GetNode<Label>("Panel/VBoxContainer/StatLabel");
        _restartButton = GetNode<Button>("Panel/VBoxContainer/RestartButton");

        _restartButton.Pressed += OnRestartPressed;

        // Hidden until triggered
        Visible = false;
    }

    public void Show(int population = 0)
    {
        _statLabel.Text = $"Population: {population}";
        Visible = true;
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
