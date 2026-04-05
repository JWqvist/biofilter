using Godot;

namespace BioFilter.Effects;

/// <summary>
/// Death splash effect — spawns 6-8 tiny squares flying outward when a particle dies.
/// Pure Godot drawing, auto-queues free after animation completes.
/// </summary>
public partial class DeathSplash : Node2D
{
    private const float Duration = 0.4f;
    private const float SquareSize = 2f;
    private const int ParticleCount = 7;
    private const float SpreadSpeed = 40f; // pixels per second

    private static readonly Color SplashColor = new Color("#7fff3a");

    private struct Splinter
    {
        public Vector2 Offset;
        public Vector2 Velocity;
    }

    private Splinter[] _splinters = new Splinter[ParticleCount];
    private float _elapsed = 0f;

    public override void _Ready()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < ParticleCount; i++)
        {
            float angle = (Mathf.Tau / ParticleCount) * i + rng.RandfRange(-0.3f, 0.3f);
            float speed = rng.RandfRange(SpreadSpeed * 0.6f, SpreadSpeed * 1.4f);
            _splinters[i] = new Splinter
            {
                Offset = Vector2.Zero,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed
            };
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration)
        {
            QueueFree();
            return;
        }

        float dt = (float)delta;
        for (int i = 0; i < ParticleCount; i++)
            _splinters[i].Offset += _splinters[i].Velocity * dt;

        QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = 1f - (_elapsed / Duration);
        var color = new Color(SplashColor.R, SplashColor.G, SplashColor.B, alpha);

        for (int i = 0; i < ParticleCount; i++)
        {
            var pos = _splinters[i].Offset;
            DrawRect(new Rect2(pos.X - SquareSize * 0.5f, pos.Y - SquareSize * 0.5f,
                               SquareSize, SquareSize), color);
        }
    }
}
