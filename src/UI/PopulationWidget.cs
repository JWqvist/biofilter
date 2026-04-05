using Godot;

namespace BioFilter.UI;

/// <summary>
/// Pixel-art population counter widget.
/// Dark panel (#0d1208) with green border (#2d5a3d).
/// Shows ⚠ POPULATION header, person icons, and red pixel-art number.
/// Flashes white + scales when population drops; pulses red/orange when critical (&lt; 20).
/// </summary>
public partial class PopulationWidget : Control
{
    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColBg       = new("#0d1208");
    private static readonly Color ColBorder   = new("#2d5a3d");
    private static readonly Color ColYellow   = new("#f5c518");
    private static readonly Color ColRed      = new("#cc0000");
    private static readonly Color ColOrange   = new("#ff6600");
    private static readonly Color ColWhite    = new(1, 1, 1, 1);
    private static readonly Color ColPersonOk = new("#44aa44");
    private static readonly Color ColPersonCrit = new("#cc0000");
    private static readonly Color ColDimGreen = new("#2d5a3d");

    // ── State ────────────────────────────────────────────────────────────────
    private int _population = GameConfig.StartingPopulation;

    // Flash animation when population drops
    private float _flashTimer   = 0f;
    private bool  _flashing     = false;
    private const float FlashDuration = 0.4f;

    // Pulse for critical
    private float _pulseTimer   = 0f;

    // ── Layout constants ─────────────────────────────────────────────────────
    private const float PixSm  = 1.5f;  // small text
    private const float PixMed = 2f;    // medium text
    private const float PixBig = 3f;    // big numbers
    private const float Pad    = 6f;

    // ── Godot overrides ──────────────────────────────────────────────────────
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(180, 80);
    }

    public override void _Process(double delta)
    {
        bool needRedraw = false;

        if (_flashing)
        {
            _flashTimer += (float)delta;
            if (_flashTimer >= FlashDuration) _flashing = false;
            needRedraw = true;
        }

        if (_population < 20)
        {
            _pulseTimer += (float)delta;
            needRedraw = true;
        }

        if (needRedraw) QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        float w = size.X;
        float h = size.Y;

        // ── Background panel ─────────────────────────────────────────────────
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        // Border
        DrawRect(new Rect2(0, 0, w, 1), ColBorder);
        DrawRect(new Rect2(0, h - 1, w, 1), ColBorder);
        DrawRect(new Rect2(0, 0, 1, h), ColBorder);
        DrawRect(new Rect2(w - 1, 0, 1, h), ColBorder);

        // Flash overlay
        if (_flashing)
        {
            float alpha = 1f - (_flashTimer / FlashDuration);
            DrawRect(new Rect2(0, 0, w, h), new Color(1, 1, 1, alpha * 0.35f));
        }

        // ── Row 1: "⚠ POPULATION" header ────────────────────────────────────
        float row1Y = Pad;
        PixelFont.DrawChar(this, PixelFont.WarnChar, new Vector2(Pad, row1Y), PixSm, ColYellow);
        float warnW = PixelFont.CharWidth(PixSm);
        PixelFont.DrawString(this, "POPULATION", new Vector2(Pad + warnW + 2, row1Y), PixSm, ColYellow);

        // ── Row 2: person icons + number ────────────────────────────────────
        float row2Y = row1Y + PixelFont.CharHeight(PixSm) + 4;
        Color personColor = _population < 20 ? ColPersonCrit : ColPersonOk;
        float iconX = Pad;
        // Draw 3 person icons
        for (int i = 0; i < 3; i++)
        {
            PixelFont.DrawChar(this, PixelFont.PersonChar, new Vector2(iconX, row2Y), PixMed, personColor);
            iconX += PixelFont.CharWidth(PixMed);
        }

        // Separator
        iconX += 4;

        // Number — use critical pulse color if needed
        Color numColor = ColRed;
        if (_population < 20)
        {
            float t = (_pulseTimer * 4f) % 1f;
            numColor = t < 0.5f ? ColRed : ColOrange;
        }

        string popStr = _population.ToString("D3");
        PixelFont.DrawString(this, popStr, new Vector2(iconX, row2Y), PixBig, numColor);

        // ── Row 3: "BUNKER STATUS" ────────────────────────────────────────────
        float row3Y = row2Y + PixelFont.CharHeight(PixBig) + 3;
        PixelFont.DrawString(this, "BUNKER STATUS", new Vector2(Pad, row3Y), PixSm, ColDimGreen);
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void UpdatePopulation(int population)
    {
        if (population < _population)
        {
            _flashing   = true;
            _flashTimer = 0f;
        }
        _population  = population;
        _pulseTimer  = 0f;
        QueueRedraw();
    }
}
