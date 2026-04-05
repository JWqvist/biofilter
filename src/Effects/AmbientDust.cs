using Godot;

namespace BioFilter.Effects;

/// <summary>
/// Ambient dust — 8-12 tiny 1x1 pixel white dots slowly drifting across the game area.
/// Alpha 0.15 — barely visible, purely atmospheric. Loops forever.
/// </summary>
public partial class AmbientDust : Node2D
{
    private const int DotCount = 10;
    private const float DotSize = 1f;
    private static readonly Color DotColor = new Color(1f, 1f, 1f, 0.15f);

    private const float MinSpeed = 3f;
    private const float MaxSpeed = 8f;

    private struct DustDot
    {
        public Vector2 Position;
        public Vector2 Velocity;
    }

    private DustDot[] _dots = new DustDot[DotCount];
    private float _areaWidth;
    private float _areaHeight;

    public void Initialize(float areaWidth, float areaHeight)
    {
        _areaWidth  = areaWidth;
        _areaHeight = areaHeight;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < DotCount; i++)
        {
            float angle = rng.RandfRange(0f, Mathf.Tau);
            float speed = rng.RandfRange(MinSpeed, MaxSpeed);
            _dots[i] = new DustDot
            {
                Position = new Vector2(
                    rng.RandfRange(0f, _areaWidth),
                    rng.RandfRange(0f, _areaHeight)
                ),
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed
            };
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < DotCount; i++)
        {
            _dots[i].Position += _dots[i].Velocity * dt;

            // Wrap around edges
            float x = _dots[i].Position.X;
            float y = _dots[i].Position.Y;
            if (x < 0f) x += _areaWidth;
            if (x > _areaWidth) x -= _areaWidth;
            if (y < 0f) y += _areaHeight;
            if (y > _areaHeight) y -= _areaHeight;
            _dots[i].Position = new Vector2(x, y);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < DotCount; i++)
        {
            var pos = _dots[i].Position;
            DrawRect(new Rect2(pos.X, pos.Y, DotSize, DotSize), DotColor);
        }
    }
}
