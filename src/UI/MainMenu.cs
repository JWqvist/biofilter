using BioFilter;
using Godot;

namespace BioFilter.UI;

public partial class MainMenu : Control
{
    private Button _playButton = null!;

    public override void _Ready()
    {
        // Background image
        var bg = GetNode<TextureRect>("Background");
        bg.Texture = GD.Load<Texture2D>("res://assets/menu_background.png");

        // Dark overlay to keep text readable
        var overlay = GetNode<ColorRect>("Overlay");
        overlay.Color = new Color(0.05f, 0.08f, 0.05f, 0.65f);

        // Title styling
        var title = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Constants.Colors.GlowGreen);

        // Subtitle
        var subtitle = GetNode<Label>("CenterContainer/VBoxContainer/SubtitleLabel");
        subtitle.AddThemeColorOverride("font_color", Constants.Colors.TextDim);

        // Hazard label
        var hazard = GetNode<Label>("CenterContainer/VBoxContainer/HazardLabel");
        hazard.AddThemeColorOverride("font_color", Constants.Colors.HazardYellow);

        _playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
        _playButton.Pressed += OnPlayPressed;
        _playButton.MouseFilter = Control.MouseFilterEnum.Stop;
        _playButton.FocusMode = Control.FocusModeEnum.All;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
