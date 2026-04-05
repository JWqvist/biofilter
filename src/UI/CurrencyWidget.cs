using Godot;

namespace BioFilter.UI;

/// <summary>
/// Pixel-art currency display widget.
/// Terminal green on dark background. Zero-padded 4-digit number.
/// Yellow flash on gain, red flash on spend.
/// </summary>
public partial class CurrencyWidget : Control
{
    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColBg        = new("#0a0f0a");
    private static readonly Color ColBorder    = new("#00ff41");
    private static readonly Color ColGreen     = new("#00ff41");
    private static readonly Color ColDimGreen  = new("#007a1f");
    private static readonly Color ColYellow    = new("#ffd600");
    private static readonly Color ColRed       = new("#d50000");

    // ── State ────────────────────────────────────────────────────────────────
    private int   _currency    = GameConfig.StartingCurrency;
    private float _flashTimer  = 0f;
    private bool  _flashing    = false;
    private bool  _flashGain   = true;  // true=yellow gain, false=red spend
    private const float FlashDuration = 0.35f;

    // ── Layout ───────────────────────────────────────────────────────────────
    private const float PixSm  = 1.5f;
    private const float PixBig = 3f;
    private const float Pad    = 5f;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(110, 70);
    }

    public override void _Process(double delta)
    {
        if (_flashing)
        {
            _flashTimer += (float)delta;
            if (_flashTimer >= FlashDuration) _flashing = false;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var size = Size;
        float w = size.X;
        float h = size.Y;

        // Panel
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        DrawRect(new Rect2(0, 0, w, 1), ColBorder);
        DrawRect(new Rect2(0, h - 1, w, 1), ColBorder);
        DrawRect(new Rect2(0, 0, 1, h), ColBorder);
        DrawRect(new Rect2(w - 1, 0, 1, h), ColBorder);

        // Flash tint
        if (_flashing)
        {
            float alpha = 1f - (_flashTimer / FlashDuration);
            Color tint = _flashGain ? ColYellow : ColRed;
            DrawRect(new Rect2(0, 0, w, h), new Color(tint.R, tint.G, tint.B, alpha * 0.30f));
        }

        // Row 1: "CREDITS" header
        float row1Y = Pad;
        string header = "CREDITS";
        float headerW = PixelFont.MeasureString(header, PixSm);
        PixelFont.DrawString(this, header, new Vector2((w - headerW) * 0.5f, row1Y), PixSm, ColDimGreen);

        // Divider line
        float divY = row1Y + PixelFont.CharHeight(PixSm) + 2;
        DrawRect(new Rect2(Pad, divY, w - Pad * 2, 1), ColDimGreen);

        // Row 2: "$ 0247"
        float row2Y = divY + 4;
        Color numColor = ColGreen;
        if (_flashing)
            numColor = _flashGain ? ColYellow : ColRed;

        // Draw $ symbol
        float dollarX = Pad + 2;
        PixelFont.DrawChar(this, '$', new Vector2(dollarX, row2Y), PixBig, numColor);

        // Draw zero-padded number
        string numStr = _currency.ToString("D4");
        float numX = dollarX + PixelFont.CharWidth(PixBig) + 2;
        PixelFont.DrawString(this, numStr, new Vector2(numX, row2Y), PixBig, numColor);
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void UpdateCurrency(int amount)
    {
        bool gained = amount > _currency;
        bool spent  = amount < _currency;
        _currency   = amount;

        if (gained || spent)
        {
            _flashing   = true;
            _flashTimer = 0f;
            _flashGain  = gained;
        }
        QueueRedraw();
    }
}
