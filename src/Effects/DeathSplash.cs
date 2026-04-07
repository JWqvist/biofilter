using Godot;

namespace BioFilter.Effects;

/// <summary>
/// Unique death splash effect per ParticleType.
/// Use the static <see cref="Create"/> factory to get the right flavour.
/// </summary>
public partial class DeathSplash : Node2D
{
    // ── Splash modes ──────────────────────────────────────────────────────────
    private enum SplashMode
    {
        BioSplat,    // 6-8 pixels flying outward          (BioParticle)
        FlashBurst,  // white circle expands & fades        (SporeSpeck)
        Shockwave,   // ring expansion + 4 pixel fragments  (RadiationBlob)
        TinySpark,   // 2px blink                           (SwarmUnit)
        HalfCircles, // two arcs flying apart               (CellDivision)
        ScatterDots, // 8 tiny dots scattering              (BacterialSwarm)
        SplitFlash,  // white 8×8 square expands & fades   (division split point)
    }

    // ── Shared state ──────────────────────────────────────────────────────────
    private SplashMode _mode      = SplashMode.BioSplat;
    private Color      _color     = new Color("#7fff3a");
    private float      _duration  = GameConfig.SplashDuration;
    private float      _elapsed   = 0f;

    private struct Splinter
    {
        public Vector2 Offset;
        public Vector2 Velocity;
    }
    private Splinter[] _splinters = System.Array.Empty<Splinter>();

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Create the correct death splash for a given particle type and color.</summary>
    public static DeathSplash Create(ParticleType type, Color color)
    {
        var splash = new DeathSplash();
        splash._color = color;

        switch (type)
        {
            case ParticleType.BioParticle:
                splash._mode     = SplashMode.BioSplat;
                splash._duration = GameConfig.SplashDuration;
                break;

            case ParticleType.SporeSpeck:
                splash._mode     = SplashMode.FlashBurst;
                splash._duration = 0.2f;
                splash._color    = Colors.White;
                break;

            case ParticleType.RadiationBlob:
                splash._mode     = SplashMode.Shockwave;
                splash._duration = GameConfig.RadBlobShockwaveDuration;
                // Keep the orange colour passed in
                break;

            case ParticleType.SwarmUnit:
                splash._mode     = SplashMode.TinySpark;
                splash._duration = 0.15f;
                break;

            case ParticleType.CellDivision:
                splash._mode     = SplashMode.HalfCircles;
                splash._duration = GameConfig.SplashDuration;
                splash._color    = new Color("#ee3388"); // pink
                break;

            case ParticleType.BacterialSwarm:
                splash._mode     = SplashMode.ScatterDots;
                splash._duration = GameConfig.SplashDuration;
                break;

            default:
                splash._mode     = SplashMode.BioSplat;
                splash._duration = GameConfig.SplashDuration;
                break;
        }

        return splash;
    }

    /// <summary>White 8×8 flash used at the CellDivision split point.</summary>
    public static DeathSplash CreateSplitFlash()
    {
        var splash = new DeathSplash();
        splash._mode     = SplashMode.SplitFlash;
        splash._color    = Colors.White;
        splash._duration = GameConfig.SplitFlashDuration;
        return splash;
    }

    /// <summary>Fallback: set colour directly (used by legacy call-sites).</summary>
    public void SetColor(Color color) => _color = color;

    // ── Godot lifecycle ───────────────────────────────────────────────────────
    public override void _Ready()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        switch (_mode)
        {
            case SplashMode.BioSplat:
            {
                int count = rng.RandiRange(6, 8);
                _splinters = new Splinter[count];
                for (int i = 0; i < count; i++)
                {
                    float angle = (Mathf.Tau / count) * i + rng.RandfRange(-0.4f, 0.4f);
                    float speed = rng.RandfRange(48f, 80f); // slightly larger than original
                    _splinters[i] = new Splinter
                    {
                        Offset   = Vector2.Zero,
                        Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    };
                }
                break;
            }

            case SplashMode.Shockwave:
            {
                // 4 pixel fragments at 90° angles
                _splinters = new Splinter[4];
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * (Mathf.Pi * 0.5f);
                    _splinters[i] = new Splinter
                    {
                        Offset   = Vector2.Zero,
                        Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 60f,
                    };
                }
                break;
            }

            case SplashMode.HalfCircles:
            {
                // Left half goes left+up, right half goes right+down
                _splinters = new Splinter[2];
                _splinters[0] = new Splinter { Offset = Vector2.Zero, Velocity = new Vector2(-50f, -40f) };
                _splinters[1] = new Splinter { Offset = Vector2.Zero, Velocity = new Vector2( 50f,  40f) };
                break;
            }

            case SplashMode.ScatterDots:
            {
                _splinters = new Splinter[8];
                for (int i = 0; i < 8; i++)
                {
                    float angle = (Mathf.Tau / 8f) * i + rng.RandfRange(-0.2f, 0.2f);
                    float speed = rng.RandfRange(35f, 65f);
                    _splinters[i] = new Splinter
                    {
                        Offset   = Vector2.Zero,
                        Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    };
                }
                break;
            }
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= _duration)
        {
            QueueFree();
            return;
        }

        float dt = (float)delta;
        for (int i = 0; i < _splinters.Length; i++)
            _splinters[i].Offset += _splinters[i].Velocity * dt;

        QueueRedraw();
    }

    public override void _Draw()
    {
        float t     = Mathf.Clamp(_elapsed / _duration, 0f, 1f);
        float alpha = 1f - t;

        switch (_mode)
        {
            // ── BioSplat: 6-8 slightly-large pixels ──────────────────────────
            case SplashMode.BioSplat:
            {
                var c = new Color(_color, alpha);
                foreach (var s in _splinters)
                    DrawRect(new Rect2(s.Offset.X - 1.5f, s.Offset.Y - 1.5f, 3f, 3f), c);
                break;
            }

            // ── FlashBurst: white circle expands to radius 10 ─────────────────
            case SplashMode.FlashBurst:
            {
                float radius = t * 10f;
                DrawCircle(Vector2.Zero, radius, new Color(1f, 1f, 1f, alpha));
                break;
            }

            // ── Shockwave: expanding ring (0→20 px) + 4 orange pixel fragments ─
            case SplashMode.Shockwave:
            {
                float radius = t * 20f;
                var ringColor = new Color(_color, alpha);
                DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 48, ringColor, 2f);

                var fragColor = new Color(_color, alpha);
                foreach (var s in _splinters)
                    DrawRect(new Rect2(s.Offset.X - 1.5f, s.Offset.Y - 1.5f, 3f, 3f), fragColor);
                break;
            }

            // ── TinySpark: 2px blink (visible first half only) ───────────────
            case SplashMode.TinySpark:
            {
                if (t < 0.5f)
                {
                    float sparkAlpha = 1f - t * 2f;
                    DrawRect(new Rect2(-1f, -1f, 2f, 2f), new Color(1f, 1f, 0.5f, sparkAlpha));
                }
                break;
            }

            // ── HalfCircles: two arcs flying apart ───────────────────────────
            case SplashMode.HalfCircles:
            {
                float r = 5f;
                var leftColor  = new Color(0.6f, 0.07f, 0.27f, alpha); // dark pink
                var rightColor = new Color(0.93f, 0.2f,  0.53f, alpha); // bright pink

                // Left half: arc from 90° (down) to 270° (up), passing through 180° (left)
                DrawArc(_splinters[0].Offset, r,
                        Mathf.Pi * 0.5f, Mathf.Pi * 1.5f,
                        20, leftColor, 3f);

                // Right half: arc from -90° (up) to 90° (down), passing through 0° (right)
                DrawArc(_splinters[1].Offset, r,
                        -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f,
                        20, rightColor, 3f);
                break;
            }

            // ── ScatterDots: 8 small dots ────────────────────────────────────
            case SplashMode.ScatterDots:
            {
                var c = new Color(_color, alpha);
                foreach (var s in _splinters)
                    DrawRect(new Rect2(s.Offset.X - 1f, s.Offset.Y - 1f, 2f, 2f), c);
                break;
            }

            // ── SplitFlash: white 8×8 square expanding and fading ─────────────
            case SplashMode.SplitFlash:
            {
                float size = 8f + t * 12f; // grows from 8 to 20 px
                float hs   = size * 0.5f;
                DrawRect(new Rect2(-hs, -hs, size, size), new Color(1f, 1f, 1f, alpha));
                break;
            }
        }
    }
}
