using Godot;
using System.Collections.Generic;

namespace BioFilter.UI;

/// <summary>
/// Static pixel font renderer. Draws text as pixel art using DrawRect() calls.
/// Character grid: 5 wide × 7 tall pixels. Spacing: 1px gap on right and bottom.
/// Bit encoding per row: bit4=leftmost col, bit0=rightmost col.
/// </summary>
public static class PixelFont
{
    // ── Character width/height helpers ─────────────────────────────────────
    public static float CharWidth(float pixelSize)  => pixelSize * 6f; // 5px + 1px gap
    public static float CharHeight(float pixelSize) => pixelSize * 8f; // 7px + 1px gap

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Draw a string starting at <paramref name="pos"/>.</summary>
    public static void DrawString(CanvasItem canvas, string text, Vector2 pos, float pixelSize, Color color)
    {
        float x = pos.X;
        foreach (char c in text)
        {
            DrawChar(canvas, c, new Vector2(x, pos.Y), pixelSize, color);
            x += CharWidth(pixelSize);
        }
    }

    /// <summary>Draw a single character at <paramref name="pos"/>.</summary>
    public static void DrawChar(CanvasItem canvas, char c, Vector2 pos, float pixelSize, Color color)
    {
        if (!Glyphs.TryGetValue(char.ToUpper(c), out var rows))
            rows = Glyphs.GetValueOrDefault('?', _blank);

        for (int row = 0; row < 7; row++)
        {
            byte rowBits = rows[row];
            for (int col = 0; col < 5; col++)
            {
                if ((rowBits & (1 << (4 - col))) != 0)
                {
                    float px = pos.X + col * pixelSize;
                    float py = pos.Y + row * pixelSize;
                    canvas.DrawRect(new Rect2(px, py, pixelSize, pixelSize), color);
                }
            }
        }
    }

    /// <summary>Measure the width of a string in pixels.</summary>
    public static float MeasureString(string text, float pixelSize)
        => text.Length * CharWidth(pixelSize);

    // ── Glyph data ──────────────────────────────────────────────────────────
    // Each glyph is 7 bytes (rows top→bottom).
    // Each byte: bits 4..0 map to columns 0..4 (left→right). 1=lit, 0=dark.

    private static readonly byte[] _blank = { 0, 0, 0, 0, 0, 0, 0 };

    private static readonly Dictionary<char, byte[]> Glyphs = new()
    {
        // ── Digits ──────────────────────────────────────────────────────────
        ['0'] = new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 },
        ['1'] = new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['2'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b01000, 0b10000, 0b11111 },
        ['3'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b00001, 0b10001, 0b01110 },
        ['4'] = new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 },
        ['5'] = new byte[] { 0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110 },
        ['6'] = new byte[] { 0b01110, 0b10000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 },
        ['7'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 },
        ['8'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
        ['9'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00001, 0b01110 },

        // ── Capital Letters ─────────────────────────────────────────────────
        ['A'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['B'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 },
        ['C'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
        ['D'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 },
        ['E'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
        ['F'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['G'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01110 },
        ['H'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['I'] = new byte[] { 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['J'] = new byte[] { 0b00001, 0b00001, 0b00001, 0b00001, 0b00001, 0b10001, 0b01110 },
        ['K'] = new byte[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 },
        ['L'] = new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
        ['M'] = new byte[] { 0b10001, 0b11011, 0b10101, 0b10001, 0b10001, 0b10001, 0b10001 },
        ['N'] = new byte[] { 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001 },
        ['O'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['P'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['Q'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 },
        ['R'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
        ['S'] = new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['T'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['U'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['V'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
        ['W'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001 },
        ['X'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 },
        ['Y'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['Z'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 },

        // ── Symbols ─────────────────────────────────────────────────────────
        [' '] = new byte[] { 0, 0, 0, 0, 0, 0, 0 },
        ['!'] = new byte[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00000, 0b00100 },
        ['?'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b00100, 0b00000, 0b00100 },
        ['.'] = new byte[] { 0, 0, 0, 0, 0, 0, 0b00100 },
        [':'] = new byte[] { 0, 0b00100, 0b00100, 0, 0b00100, 0b00100, 0 },
        ['-'] = new byte[] { 0, 0, 0, 0b11111, 0, 0, 0 },
        ['+'] = new byte[] { 0, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0 },
        ['/'] = new byte[] { 0b00001, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b10000 },
        ['%'] = new byte[] { 0b11000, 0b11001, 0b00010, 0b00100, 0b01000, 0b10011, 0b00011 },
        ['$'] = new byte[] { 0b00100, 0b01110, 0b10100, 0b01110, 0b00101, 0b01110, 0b00100 },
        ['#'] = new byte[] { 0b01010, 0b01010, 0b11111, 0b01010, 0b11111, 0b01010, 0b01010 },
        ['*'] = new byte[] { 0, 0b10101, 0b01110, 0b11111, 0b01110, 0b10101, 0 },

        // ── Special block glyphs ─────────────────────────────────────────────
        // ▶ (play / arrow right)
        ['\x01'] = new byte[] { 0b10000, 0b11000, 0b11100, 0b11110, 0b11100, 0b11000, 0b10000 },
        // ■ (filled square)
        ['\x02'] = new byte[] { 0, 0b01110, 0b01110, 0b01110, 0b01110, 0b01110, 0 },
        // ░ (light shade / empty segment)
        ['\x03'] = new byte[] { 0b10101, 0b01010, 0b10101, 0b01010, 0b10101, 0b01010, 0b10101 },
        // █ (full block)
        ['\x04'] = new byte[] { 0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111 },
        // ⚠ (warning triangle — simplified)
        ['\x05'] = new byte[] { 0b00100, 0b01010, 0b01010, 0b10001, 0b11111, 0b10001, 0b11111 },
        // 👤 (person icon — simplified)
        ['\x06'] = new byte[] { 0b00100, 0b01010, 0b00100, 0b01110, 0b00100, 0b01010, 0b01010 },
    };

    // ── Convenience constants for special chars ──────────────────────────────
    public const char PlayChar    = '\x01'; // ▶
    public const char BlockChar   = '\x02'; // ■
    public const char ShadeChar   = '\x03'; // ░
    public const char FullBlock   = '\x04'; // █
    public const char WarnChar    = '\x05'; // ⚠
    public const char PersonChar  = '\x06'; // 👤
}
