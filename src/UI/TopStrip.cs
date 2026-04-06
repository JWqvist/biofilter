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

    // ── Colors ────────────────────────────────────────────────────────────
    private static readonly Color ColBg        = new Color("#0a0f0a");
    private static readonly Color ColTopBorder = new Color("#1e3a1e");
    private static readonly Color ColBarFilled = new Color("#4caf50");
    private static readonly Color ColBarEmpty  = new Color("#1a2a1a");
    private static readonly Color ColText      = new Color("#c8e6c0");
    private static readonly Color ColSeparator = new Color("#2d5a3d");

    private const int BarSegments = 20;
    private const float PixS = 1.0f; // pixel font scale

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // Size is set by Main.tscn layout
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void OnWaveStarted(int waveNumber)
    {
        _currentWave = waveNumber;
        QueueRedraw();
    }

    public void OnWaveComplete(int waveNumber)
    {
        _currentWave = waveNumber;
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
                Color sc = i < filled ? ColBarFilled : ColBarEmpty;
                DrawRect(new Rect2(sx, 4f, segW, h - 8f), sc);
            }
        }




    }
}
