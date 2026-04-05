using Godot;

namespace BioFilter.Effects;

/// <summary>
/// Floating text popup (e.g. "+12" on kill, "-1" on escape).
/// Floats upward over 1 second then fades out and queues free.
/// </summary>
public partial class FloatingText : Node2D
{
    private const float Duration = 1.0f;
    private const float FloatSpeed = 30f; // pixels per second upward

    private string _text = "";
    private Color _color = Colors.Yellow;
    private float _elapsed = 0f;

    public void Initialize(string text, Color color)
    {
        _text = text;
        _color = color;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration)
        {
            QueueFree();
            return;
        }

        Position = new Vector2(Position.X, Position.Y - FloatSpeed * (float)delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = 1f - (_elapsed / Duration);
        var color = new Color(_color.R, _color.G, _color.B, alpha);

        // Draw shadow for readability
        DrawString(ThemeDB.FallbackFont, new Vector2(1f, 1f), _text,
                   HorizontalAlignment.Center, -1, 10,
                   new Color(0, 0, 0, alpha * 0.8f));

        // Draw main text
        DrawString(ThemeDB.FallbackFont, Vector2.Zero, _text,
                   HorizontalAlignment.Center, -1, 10, color);
    }
}
