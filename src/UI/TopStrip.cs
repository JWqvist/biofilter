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
    private float _airflow      = 1.0f;   // 0..1

    // ── Colors ────────────────────────────────────────────────────────────
    private static readonly Color ColBg        = new Color("#0a0f0a");
    private static readonly Color ColTopBorder = new Color("#1e3a1e");
    private static readonly Color ColBarFilled = new Color("#4caf50");
    private static readonly Color ColBarEmpty  = new Color("#1a2a1a");
    private static readonly Color ColText      = new Color("#c8e6c0");
    private static readonly Color ColSeparator = new Color("#2d5a3d");
    // Airflow
    private static readonly Color ColAirflowGreen = new Color("#4caf50");
    private static readonly Color ColAirflowAmber = new Color("#ff8f00");
    private static readonly Color ColAirflowRed   = new Color("#c62828");

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

    public void UpdateAirflow(float airflow)
    {
        _airflow = airflow;
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

        float half  = w * 0.5f;
        float pad   = 4f;
        float textY = (h - PixelFont.CharHeight(PixS)) * 0.5f;

        // ── Left half: WAVE XX/YY + bar ────────────────────────────────────
        float lx = pad;

        string waveLabel = $"WAVE {_currentWave:D2}/{_totalWaves:D2}";
        PixelFont.DrawString(this, waveLabel, new Vector2(lx, textY), PixS, ColText);
        lx += PixelFont.MeasureString(waveLabel, PixS) + 4f;

        // Segmented bar — fits remaining left half
        float barEnd    = half - pad;
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

        // ── Separator ─────────────────────────────────────────────────────
        DrawRect(new Rect2(half, 2f, 1f, h - 4f), ColSeparator);

        // ── Right half: AIRFLOW: XX% + bar ────────────────────────────────
        Color airflowCol = _airflow >= 0.60f ? ColAirflowGreen
                         : _airflow >= 0.30f ? ColAirflowAmber
                                             : ColAirflowRed;

        string prefix = _airflow < 0.30f ? "! " : "";
        string airStr = $"{prefix}AIRFLOW: {(int)(_airflow * 100f):D2}%";
        float  rx     = half + pad;

        PixelFont.DrawString(this, airStr, new Vector2(rx, textY), PixS, airflowCol);
        rx += PixelFont.MeasureString(airStr, PixS) + 4f;

        // Small airflow bar to the right of text
        float aBarEnd = w - pad;
        float aBarW   = aBarEnd - rx;
        if (aBarW > 2f)
        {
            int aFilled = (int)(_airflow * BarSegments);
            float aSegW = (aBarW - (BarSegments - 1)) / BarSegments;
            if (aSegW < 1f) aSegW = 1f;
            for (int i = 0; i < BarSegments; i++)
            {
                float sx = rx + i * (aSegW + 1f);
                if (sx + aSegW > aBarEnd) break;
                Color sc = i < aFilled ? airflowCol : ColBarEmpty;
                DrawRect(new Rect2(sx, 4f, aSegW, h - 8f), sc);
            }
        }
    }
}
