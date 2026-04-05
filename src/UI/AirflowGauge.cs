using Godot;

namespace BioFilter.UI;

/// <summary>
/// Pixel-art semicircle airflow gauge (speedometer style).
/// 12 arc segments, green→yellow→red gradient.
/// Needle from center to current angle.
/// Danger flash when &lt; 20%.
/// </summary>
public partial class AirflowGauge : Control
{
    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColBg      = new("#0d1208");
    private static readonly Color ColBorder  = new("#2d5a3d");
    private static readonly Color ColGreen   = new("#00c853");
    private static readonly Color ColYellow  = new("#ffd600");
    private static readonly Color ColRed     = new("#d50000");
    private static readonly Color ColNeedle  = new("#ffffff");
    private static readonly Color ColLabel   = new("#00ff41");
    private static readonly Color ColDark    = new("#1a2a1a");

    // ── State ────────────────────────────────────────────────────────────────
    private float _airflow    = 1.0f;  // 0..1
    private float _flashTimer = 0f;

    // ── Layout ───────────────────────────────────────────────────────────────
    private const float PixSm  = 1.5f;
    private const float PixMed = 2f;
    private const int   Segments = 12;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(140, 90);
    }

    public override void _Process(double delta)
    {
        if (_airflow < 0.20f)
        {
            _flashTimer += (float)delta;
            QueueRedraw();
        }
        else
        {
            _flashTimer = 0f;
        }
    }

    public override void _Draw()
    {
        var size   = Size;
        float w    = size.X;
        float h    = size.Y;

        // Panel background + border
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        DrawRect(new Rect2(0, 0, w, 1), ColBorder);
        DrawRect(new Rect2(0, h - 1, w, 1), ColBorder);
        DrawRect(new Rect2(0, 0, 1, h), ColBorder);
        DrawRect(new Rect2(w - 1, 0, 1, h), ColBorder);

        float pad   = 8f;
        float labelH = PixelFont.CharHeight(PixSm) + 3;

        // "AIRFLOW" label at top
        string label = "AIRFLOW";
        float labelW = PixelFont.MeasureString(label, PixSm);
        PixelFont.DrawString(this, label,
            new Vector2((w - labelW) * 0.5f, pad), PixSm, ColLabel);

        // Arc center & radius
        float arcTop = pad + labelH;
        float arcBot = h - pad - PixelFont.CharHeight(PixMed) - 6;
        float arcH   = arcBot - arcTop;
        float radius = Mathf.Min(arcH * 1.1f, (w - pad * 2) * 0.5f);
        float cx     = w * 0.5f;
        float cy     = arcBot;   // center at bottom of arc area (semicircle pointing up)

        // Draw danger flash tint
        if (_airflow < 0.20f)
        {
            float flash = 0.5f + 0.5f * Mathf.Sin(_flashTimer * Mathf.Pi * 6f);
            DrawRect(new Rect2(0, 0, w, h), new Color(1, 0, 0, flash * 0.15f));
        }

        // ── Draw arc segments ─────────────────────────────────────────────────
        // Arc from 180° to 0° (left to right), segments arranged left=low, right=high
        float segAngle = Mathf.Pi / Segments; // radians per segment
        float segThick = radius * 0.22f;
        float innerR   = radius - segThick;
        float outerR   = radius;

        for (int i = 0; i < Segments; i++)
        {
            // Angle for center of this segment (going right = increasing airflow)
            float t       = (i + 0.5f) / Segments;          // 0..1 across arc
            float angle   = Mathf.Pi - t * Mathf.Pi;        // π → 0 (left to right)

            // Segment threshold: segment i lights up when airflow > i/Segments
            bool lit = _airflow >= (float)i / Segments;

            Color segCol;
            if (!lit)
            {
                segCol = ColDark;
            }
            else
            {
                // Color: red at low (left), yellow mid, green at high (right)
                float segT = (float)i / (Segments - 1);
                if (segT < 0.5f)
                    segCol = ColRed.Lerp(ColYellow, segT * 2f);
                else
                    segCol = ColYellow.Lerp(ColGreen, (segT - 0.5f) * 2f);
            }

            // Draw segment as a thick arc approximation using small rects
            DrawArcSegment(cx, cy, innerR, outerR, angle, segAngle * 0.85f, segCol);
        }

        // ── Needle ────────────────────────────────────────────────────────────
        float needleAngle = Mathf.Pi - _airflow * Mathf.Pi; // maps 0→π, 1→0
        float nx = cx + Mathf.Cos(needleAngle) * radius * 0.85f;
        float ny = cy - Mathf.Sin(needleAngle) * radius * 0.85f; // y is inverted
        DrawLine(new Vector2(cx, cy), new Vector2(nx, ny), ColNeedle, 2f);
        // Pivot dot
        DrawCircle(new Vector2(cx, cy), 3f, ColNeedle);

        // ── Percentage number below arc ───────────────────────────────────────
        int pct    = (int)(_airflow * 100f);
        string pctStr = pct.ToString("D3") + "%";
        float pctW  = PixelFont.MeasureString(pctStr, PixMed);
        Color pctCol = _airflow >= 0.60f ? ColGreen :
                       _airflow >= 0.20f ? ColYellow : ColRed;
        PixelFont.DrawString(this, pctStr,
            new Vector2((w - pctW) * 0.5f, arcBot + 4), PixMed, pctCol);
    }

    private void DrawArcSegment(float cx, float cy, float innerR, float outerR,
                                float centerAngle, float halfSpan, Color color)
    {
        // Approximate segment with a grid of small squares
        int steps = 6;
        for (int ri = 0; ri < steps; ri++)
        {
            float r = innerR + (outerR - innerR) * (ri + 0.5f) / steps;
            int angSteps = Mathf.Max(2, (int)(r * halfSpan * 2 / 3f));
            for (int ai = 0; ai < angSteps; ai++)
            {
                float a = centerAngle - halfSpan + halfSpan * 2 * (ai + 0.5f) / angSteps;
                float px = cx + Mathf.Cos(a) * r;
                float py = cy - Mathf.Sin(a) * r;
                float rectSize = (outerR - innerR) / steps * 1.1f;
                DrawRect(new Rect2(px - rectSize * 0.5f, py - rectSize * 0.5f, rectSize, rectSize), color);
            }
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void UpdateAirflow(float airflow)
    {
        _airflow = Mathf.Clamp(airflow, 0f, 1f);
        QueueRedraw();
    }
}
