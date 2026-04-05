using Godot;

namespace BioFilter.UI;

/// <summary>
/// Main menu scene — game entry point.
/// Shows title, subtitle and a Play button.
/// </summary>
public partial class MainMenu : Control
{
    private Button _playButton;

    public override void _Ready()
    {
        _playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
        _playButton.Pressed += OnPlayPressed;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
