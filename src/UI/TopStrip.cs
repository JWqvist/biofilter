using Godot;

namespace BioFilter.UI;

/// <summary>
/// Widescreen HUD top strip (640×24px).
/// Replaces old WaveHUD + AirflowMeter.
/// Left half: wave number + segmented progress bar.
/// Right half: airflow percentage with color indicator.
/// </summary>
public partial class TopStrip : Control
{
    // ── State ─────────────────────────────────────────────────────────────
    private int   _currentWave  = 0;
    private int   _totalWaves   = GameConfig.TotalWaves;
    private float _time          = 0f;
    private bool  _isBuildPhase  = true;

    // ── Colors ────────────────────────────────────────────────────────────
    private static readonly Color ColBg        = new Color("#0a0f0a");
    private static readonly Color ColTopBorder = new Color("#1e3a1e");
    private static readonly Color ColBarFilled = new Color("#4caf50");
    private static readonly Color ColBarEmpty  = new Color("#1a2a1a");
    private static readonly Color ColText      = new Color("#c8e6c0");
    private static readonly Color ColSeparator = new Color("#2d5a3d");

    private const int BarSegments = 20;
    private const float PixS = 1.0f; // pixel font scale

    // Threat colors per wave (1-indexed)
    private static readonly Color[] WaveThreatColors =
    {
        new Color("#4caf50"), // Wave 1  - green
        new Color("#4caf50"), // Wave 2  - green
        new Color("#cddc39"), // Wave 3  - yellow-green
        new Color("#ffeb3b"), // Wave 4  - yellow
        new Color("#ff9800"), // Wave 5  - orange
        new Color("#ff9800"), // Wave 6  - orange
        new Color("#f44336"), // Wave 7  - red
        new Color("#f44336"), // Wave 8  - red
        new Color("#b71c1c"), // Wave 9  - dark red
        new Color("#7b1fa2"), // Wave 10 - purple (boss)
    };

    private static Color GetWaveColor(int wave1indexed)
    {
        int idx = Mathf.Clamp(wave1indexed - 1, 0, WaveThreatColors.Length - 1);
        return WaveThreatColors[idx];
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // Size is set by Main.tscn layout
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void OnWaveStarted(int waveNumber)
    {
        _currentWave = waveNumber;
        _isBuildPhase = false;
        QueueRedraw();
    }

    public void OnWaveComplete(int waveNumber)
    {
        _currentWave = waveNumber;
        _isBuildPhase = true;
        QueueRedraw();
    }

// ── Rendering ─────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;

        // Background
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        // Bottom border (subtle separator from game area)
        DrawRect(new Rect2(0, h - 1, w, 1), ColTopBorder);

        float pad   = 4f;
        float textY = (h - PixelFont.CharHeight(PixS)) * 0.5f;

        // ── Full width: WAVE XX/YY + bar ────────────────────────────────────
        float lx = pad;

        string waveLabel = $"WAVE {_currentWave:D2}/{_totalWaves:D2}";
        PixelFont.DrawString(this, waveLabel, new Vector2(lx, textY), PixS, ColText);
        lx += PixelFont.MeasureString(waveLabel, PixS) + 4f;

        // Segmented bar — fits full remaining width
        // Bar ends at grid width boundary (not full viewport width)
        float gridWidth = GameConfig.GridWidth * GameConfig.TileSize;
        float barEnd    = Mathf.Min(gridWidth - pad, w - pad);
        float barW      = barEnd - lx;
        if (barW > 0)
        {
            float segGap  = 1f;
            float segW    = (barW - segGap * (BarSegments - 1)) / BarSegments;
            int   filled  = _totalWaves > 0 ? (int)(_currentWave * BarSegments / (float)_totalWaves) : 0;

            for (int i = 0; i < BarSegments; i++)
            {
                float sx = lx + i * (segW + segGap);

                // Which wave does this segment represent?
                int segWave = Mathf.Max(1, (int)Mathf.Ceil((i + 1) * _totalWaves / (float)BarSegments));
                Color threatCol = GetWaveColor(segWave);

                Color sc;
                if (i < filled)
                {
                    // Completed wave — solid threat color, slightly dimmed
                    sc = new Color(threatCol.R * 0.5f, threatCol.G * 0.5f, threatCol.B * 0.5f, 1f);
                }
                else if (i == filled)
                {
                    // Current/next wave — full threat color, blinking
                    float blink = (Mathf.Sin(_time * Mathf.Tau * 2f) + 1f) * 0.5f;
                    sc = new Color(threatCol.R, threatCol.G, threatCol.B, 0.5f + blink * 0.5f);
                }
                else
                {
                    // Future wave — dim threat color so player can see what's coming
                    sc = new Color(threatCol.R * 0.25f, threatCol.G * 0.25f, threatCol.B * 0.25f, 0.8f);
                }
                DrawRect(new Rect2(sx, 4f, segW, h - 8f), sc);
            }
        }




    }
}
