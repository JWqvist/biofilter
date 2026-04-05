using Godot;

namespace BioFilter.UI;

/// <summary>
/// Pixel-art wave progress display widget.
/// Shows "TACTICAL DISPLAY  WAVE XX", a 20-segment progress bar, and particle count.
/// Current wave segment blinks. Cyan border.
/// </summary>
public partial class WaveWidget : Control
{
    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColBg       = new("#090d10");
    private static readonly Color ColBorder   = new("#00bcd4");
    private static readonly Color ColCyan     = new("#00bcd4");
    private static readonly Color ColDimCyan  = new("#005f6b");
    private static readonly Color ColFilled   = new("#00bcd4");
    private static readonly Color ColEmpty    = new("#1a2a30");
    private static readonly Color ColBlink    = new("#ffffff");
    private static readonly Color ColWhite    = new("#ccdddd");

    // ── State ────────────────────────────────────────────────────────────────
    private int _currentWave    = 0;
    private int _totalWaves     = GameConfig.TotalWaves;
    private int _particleCount  = 0;
    private float _blinkTimer   = 0f;
    private bool  _blinkState   = true;

    // ── Layout ───────────────────────────────────────────────────────────────
    private const float PixSm  = 1.5f;
    private const float PixMed = 2f;
    private const float Pad    = 5f;
    private const int   SegCount = 20;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 80);
    }

    public override void _Process(double delta)
    {
        _blinkTimer += (float)delta;
        if (_blinkTimer >= 0.5f)
        {
            _blinkTimer = 0f;
            _blinkState = !_blinkState;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var size  = Size;
        float w   = size.X;
        float h   = size.Y;

        // Panel
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        DrawRect(new Rect2(0, 0, w, 1), ColBorder);
        DrawRect(new Rect2(0, h - 1, w, 1), ColBorder);
        DrawRect(new Rect2(0, 0, 1, h), ColBorder);
        DrawRect(new Rect2(w - 1, 0, 1, h), ColBorder);

        // Row 1: "TACTICAL DISPLAY" + "WAVE XX"
        float row1Y = Pad;
        PixelFont.DrawString(this, "TACTICAL DISPLAY", new Vector2(Pad, row1Y), PixSm, ColCyan);

        string waveStr = "WAVE " + _currentWave.ToString("D2");
        float waveW    = PixelFont.MeasureString(waveStr, PixSm);
        PixelFont.DrawString(this, waveStr,
            new Vector2(w - Pad - waveW, row1Y), PixSm, ColWhite);

        // Row 2: segmented progress bar
        float row2Y = row1Y + PixelFont.CharHeight(PixSm) + 4;
        float barW   = w - Pad * 2;
        float segW   = barW / SegCount;
        float segH   = 12f;
        float gap    = 2f;

        for (int i = 0; i < SegCount; i++)
        {
            // Map segment index to wave number (1-based)
            int segWave = (int)((float)(i + 1) / SegCount * _totalWaves + 0.5f);
            segWave = Mathf.Clamp(segWave, 1, _totalWaves);

            float sx = Pad + i * segW;
            float sw = segW - gap;

            Color segCol;
            bool isCurrent = segWave == _currentWave;

            if (isCurrent)
            {
                // Blinking current wave segment
                segCol = _blinkState ? ColBlink : ColDimCyan;
            }
            else if (segWave <= _currentWave)
            {
                segCol = ColFilled;
            }
            else
            {
                // Draw empty as shade pattern
                segCol = ColEmpty;
            }

            DrawRect(new Rect2(sx, row2Y, sw, segH), segCol);

            // Add shade dots for empty segments
            if (segWave > _currentWave && !isCurrent)
            {
                // Light dot in center
                DrawRect(new Rect2(sx + sw * 0.5f - 1, row2Y + segH * 0.5f - 1, 2, 2), ColDimCyan);
            }
        }

        // Row 3: "▶▶▶ HOSTILE PARTICLES: XX"
        float row3Y = row2Y + segH + 4;
        PixelFont.DrawChar(this, PixelFont.PlayChar, new Vector2(Pad,     row3Y), PixSm, ColCyan);
        PixelFont.DrawChar(this, PixelFont.PlayChar, new Vector2(Pad + 5, row3Y), PixSm, ColCyan);
        PixelFont.DrawChar(this, PixelFont.PlayChar, new Vector2(Pad + 10,row3Y), PixSm, ColCyan);

        string particleStr = "HOSTILE PARTICLES: " + _particleCount.ToString();
        PixelFont.DrawString(this, particleStr,
            new Vector2(Pad + 18, row3Y), PixSm, ColWhite);
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void OnWaveStarted(int waveNumber)
    {
        _currentWave  = waveNumber;
        QueueRedraw();
    }

    public void OnWaveComplete(int waveNumber)
    {
        _currentWave = waveNumber;
        QueueRedraw();
    }

    public void UpdateParticleCount(int count)
    {
        _particleCount = count;
        QueueRedraw();
    }
}
