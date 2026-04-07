using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Fullscreen build menu overlay with pixel-art module cards.
/// Opens on Build button press, closes on Escape or clicking outside cards.
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();
    [Signal] public delegate void UpgradeRequestedEventHandler();

    private int  _selectedTower = -1;
    private bool _isWaveActive  = false;
    private bool _isOpen        = false;

    /// <summary>True when open — GridManager uses this to block click-through.</summary>
    public static bool IsOpen { get; private set; } = false;

    // Module definitions: (name, hotkey, cost label, description, tower type index, card color)
    private static readonly (string Name, string Hotkey, string Cost, string Desc, int Type, Color Color)[] _modules =
    {
        ("WALL",            "[W]", "FREE",                              "Direct particle flow",   -1, new Color("#3a5a3a")),
        ("BASIC FILTER",    "[1]", $"${GameConfig.BasicFilterCost}",   "Damages nearby",          0, new Color("#00c853")),
        ("ELECTROSTATIC",   "[2]", $"${GameConfig.ElectrostaticCost}", "Slows nearby",             1, new Color("#2979ff")),
        ("UV STERILISER",   "[3]", $"${GameConfig.UVSteriliserCost}",  "Shoots particles",         2, new Color("#aa00ff")),
        ("VORTEX SEP.",     "",    $"${GameConfig.VortexSeparatorCost}","Forces longer routes",    3, new Color("#00bcd4")),
        ("POWER CORE",      "",    $"${GameConfig.PowerCoreCost}",      "+$5/wave",                 4, new Color("#ffd700")),
        ("BIO NEUTRAL.",    "",    $"${GameConfig.BioNeutraliserCost}", "Boosts adjacent +25%",    5, new Color("#9c27b0")),
        ("MAGNETIC CAGE",   "",    $"${GameConfig.MagneticCageCost}",   "Traps particles 2s",      6, new Color("#795548")),
        ("TOXIC SPRAYER",   "",    $"${GameConfig.ToxicSprayerCost}",   "Poisons (DoT)",            7, new Color("#dd2222")),
        ("PLASMA BURST",    "",    $"${GameConfig.PlasmaBurstCost}",    "AoE explosion",            8, new Color("#2979ff")),
    };

    private Panel _overlay = null!;
    private int _hoverIndex = -1;

    public override void _Ready()
    {
        Layer = 10;

        // Full-screen semi-transparent overlay
        _overlay = new Panel();
        _overlay.LayoutMode = 3;
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.Visible = false;

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.02f, 0.06f, 0.02f, 0.93f);
        bgStyle.BorderColor = new Color("#2d5a3d");
        bgStyle.SetBorderWidthAll(2);
        _overlay.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(_overlay);

        // Title bar
        var titleLabel = new Label();
        titleLabel.Text = "▣ SELECT MODULE";
        titleLabel.LayoutMode = 3;
        titleLabel.AnchorLeft = 0f; titleLabel.AnchorTop = 0f;
        titleLabel.AnchorRight = 1f; titleLabel.AnchorBottom = 0f;
        titleLabel.OffsetTop = 4f; titleLabel.OffsetBottom = 22f;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeColorOverride("font_color", new Color("#4a9e6a"));
        titleLabel.AddThemeFontSizeOverride("font_size", 11);
        _overlay.AddChild(titleLabel);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = "✕";
        closeBtn.LayoutMode = 3;
        closeBtn.AnchorLeft = 1f; closeBtn.AnchorTop = 0f;
        closeBtn.AnchorRight = 1f; closeBtn.AnchorBottom = 0f;
        closeBtn.OffsetLeft = -20f; closeBtn.OffsetTop = 2f;
        closeBtn.OffsetRight = -2f; closeBtn.OffsetBottom = 20f;
        closeBtn.AddThemeColorOverride("font_color", new Color("#cc0000"));
        closeBtn.Pressed += Close;
        _overlay.AddChild(closeBtn);

        // Module cards grid
        // Viewport is 640x360 — 3 rows of cards must fit.
        // 3 rows * 110px + 2 gaps * 4px + padY 22 = 22+330+8 = 360px exactly.
        const int cols = 4;    // 4 columns fits more cards
        const int cardW = 148; // (640 - 12*2 - 5*3) / 4 ≈ 148
        const int cardH = 90;  // shorter cards
        const int padX = 6;
        const int padY = 22;
        const int gapX = 5;
        const int gapY = 4;

        for (int i = 0; i < _modules.Length; i++)
        {
            var (name, hotkey, cost, desc, type, color) = _modules[i];
            int col = i % cols;
            int row = i / cols;
            float x = padX + col * (cardW + gapX);
            float y = padY + row * (cardH + gapY);

            var card = new ModuleCard(i, name, hotkey, cost, desc, color, cardW, cardH, type);
            card.LayoutMode = 3;
            card.AnchorLeft = 0f; card.AnchorTop = 0f;
            card.OffsetLeft = x; card.OffsetTop = y;
            card.OffsetRight = x + cardW; card.OffsetBottom = y + cardH;
            int captured = i;
            card.GuiInput += (e) => OnCardInput(e, captured);
            card.MouseEntered += () => { _hoverIndex = captured; QueueRedrawCards(); };
            card.MouseExited  += () => { if (_hoverIndex == captured) { _hoverIndex = -1; QueueRedrawCards(); } };
            _overlay.AddChild(card);
        }

        _overlay.GuiInput += OnOverlayInput;
    }

    private void QueueRedrawCards()
    {
        foreach (var child in _overlay.GetChildren())
            if (child is ModuleCard mc) mc.QueueRedraw();
    }

    private void OnCardInput(InputEvent e, int index)
    {
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            SelectModule(index);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnOverlayInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.Pressed)
            GetViewport().SetInputAsHandled(); // eat all clicks on overlay
    }

    private void SelectModule(int index)
    {
        var (_, _, _, _, type, _) = _modules[index];
        _selectedTower = type;
        if (type == -1)
            EmitSignal(SignalName.TowerDeselected);
        else
            EmitSignal(SignalName.TowerSelected, type);
        Close();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isOpen && @event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open()
    {
        if (_isWaveActive) return;
        _isOpen = true;
        BuildMenu.IsOpen = true;
        _overlay.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        BuildMenu.IsOpen = false;
        _overlay.Visible = false;
    }

    public void Toggle()
    {
        if (_isOpen) Close(); else Open();
    }

    public void OnWaveStarted(int _)  { _isWaveActive = true;  Close(); }
    public void OnWaveComplete(int _) { _isWaveActive = false; }

    public void ShowUpgradeButton(int cost, bool canAfford) { /* upgrade handled by TowerManager directly */ }
    public void HideUpgradeButton() { }

    // ── Inner class: Module card ─────────────────────────────────────────────

    private partial class ModuleCard : Control
    {
        private readonly int    _index;
        private readonly int    _type;  // TowerType enum value (-1=wall)
        private readonly string _name;
        private readonly string _hotkey;
        private readonly string _cost;
        private readonly string _desc;
        private readonly Color  _color;
        private readonly int    _w;
        private readonly int    _h;

        private float _localTime = 0f;

        public ModuleCard(int index, string name, string hotkey, string cost, string desc, Color color, int w, int h, int type = -2)
        {
            _index = index; _type = type; _name = name; _hotkey = hotkey; _cost = cost;
            _desc = desc; _color = color; _w = w; _h = h;
            MouseFilter = MouseFilterEnum.Stop;
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            _localTime += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var rect = new Rect2(0, 0, _w, _h);
            bool hover = GetParent()?.GetParent() is BuildMenu bm && bm._hoverIndex == _index;

            // Card background
            DrawRect(rect, new Color(0.04f, 0.1f, 0.04f, 1f));

            // Border — bright if hovered
            var borderColor = hover ? Colors.Yellow : _color;
            DrawRect(new Rect2(0, 0, _w, 2), borderColor);
            DrawRect(new Rect2(0, _h - 2, _w, 2), borderColor);
            DrawRect(new Rect2(0, 0, 2, _h), borderColor);
            DrawRect(new Rect2(_w - 2, 0, 2, _h), borderColor);

            // Pixel art tower preview (48×48 centered in top area)
            const int previewSize = 36;
            float px = (_w - previewSize) * 0.5f;
            float py = 6f;
            DrawTowerPreview(px, py, previewSize);

            // Name text
            DrawString(ThemeDB.FallbackFont, new Vector2(4, py + previewSize + 14),
                _name, HorizontalAlignment.Left, _w - 8, 9, _color);

            // Hotkey + cost
            string label = _hotkey.Length > 0 ? $"{_hotkey} {_cost}" : _cost;
            DrawString(ThemeDB.FallbackFont, new Vector2(4, py + previewSize + 26),
                label, HorizontalAlignment.Left, _w - 8, 8, Colors.White);

            // Description
            DrawString(ThemeDB.FallbackFont, new Vector2(4, py + previewSize + 38),
                _desc, HorizontalAlignment.Left, _w - 8, 7, new Color(0.6f, 0.8f, 0.6f));
        }

        private void DrawTowerPreview(float ox, float oy, int size)
        {
            float s = size / 16f; // scale factor (16px design → size px)
            var c = _color;
            var dark = c * 0.3f; dark.A = 1f;
            var bright = c; bright.A = 1f;

            // Outer frame
            DrawRect(new Rect2(ox, oy, size, size), dark);
            DrawRect(new Rect2(ox, oy, size, 2 * s), bright);
            DrawRect(new Rect2(ox, oy + size - 2 * s, size, 2 * s), bright);
            DrawRect(new Rect2(ox, oy, 2 * s, size), bright);
            DrawRect(new Rect2(ox + size - 2 * s, oy, 2 * s, size), bright);

            float cx = ox + size * 0.5f;
            float cy = oy + size * 0.5f;

            // Use _type for icon selection: -1=wall, 0=BasicFilter, 1=Electro, 2=UV,
            // 3=Vortex, 4=PowerCore, 5=BioNeutraliser, 6=MagneticCage, 7=ToxicSprayer, 8=PlasmaBurst
            int iconIdx = _type == -1 ? 0 : _type + 1; // wall=0, towers offset by 1
            switch (iconIdx)
            {
                case 0: // Wall — grey panel with highlight edge
                {
                    DrawRect(new Rect2(ox + 4*s, oy + 4*s, size - 8*s, size - 8*s), bright);
                    // highlight top edge
                    DrawRect(new Rect2(ox + 4*s, oy + 4*s, size - 8*s, s), Colors.White * 0.6f);
                    break;
                }
                case 1: // Basic Filter — 3×3 filter mesh with pulsing center
                {
                    var meshBright = new Color("#4caf50");
                    var meshDim    = new Color("#2e7d32");
                    var meshCenter = new Color("#69f0ae");
                    float pulse = 0.7f + 0.3f * Mathf.Sin(_localTime * 4f);
                    float cellSz = 2f * s;
                    float gap    = 1f * s;
                    float step   = cellSz + gap;
                    float meshOff = -4f * s;
                    for (int row = 0; row < 3; row++)
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            float bx = cx + meshOff + col * step;
                            float by = cy + meshOff + row * step;
                            bool isCenter2 = row == 1 && col == 1;
                            bool isChecker = (row + col) % 2 == 0;
                            Color cellC;
                            float sz;
                            if (isCenter2)
                            {
                                cellC = new Color(meshCenter.R, meshCenter.G, meshCenter.B, pulse);
                                sz = 3f * s;
                                bx -= 0.5f * s;
                                by -= 0.5f * s;
                            }
                            else
                            {
                                cellC = isChecker ? meshBright : meshDim;
                                sz = cellSz;
                            }
                            DrawRect(new Rect2(bx, by, sz, sz), cellC);
                        }
                    }
                    break;
                }
                case 2: // Electrostatic — animated lightning bolt
                {
                    float brightness = 0.6f + 0.4f * Mathf.Sin(_localTime * 8f);
                    var boltColor = new Color(0f, brightness, brightness, 0.9f);
                    float top = cy - size * 0.3f;
                    float bot = cy + size * 0.3f;
                    var pts = new Godot.Vector2[]
                    {
                        new Godot.Vector2(cx + s, top),
                        new Godot.Vector2(cx - 2*s, cy),
                        new Godot.Vector2(cx + 2*s, cy),
                        new Godot.Vector2(cx - s, bot),
                    };
                    for (int i = 0; i < pts.Length - 1; i++)
                        DrawLine(pts[i], pts[i + 1], boltColor, 1.5f);
                    break;
                }
                case 3: // UV Steriliser — ring + 4 rotating rays
                {
                    float ringR = size * 0.28f;
                    var ringColor = new Color(bright.R + 0.3f, bright.G + 0.1f, bright.B + 0.3f, 0.9f);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * (Mathf.Pi / 4f);
                        float rx = cx + Mathf.Cos(angle) * ringR;
                        float ry = cy + Mathf.Sin(angle) * ringR;
                        DrawRect(new Rect2(rx - s, ry - s, 2*s, 2*s), ringColor);
                    }
                    float rayLen = size * 0.28f;
                    var rayColor = new Color(0.8f, 0.5f, 1.0f, 0.85f);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = _localTime * 2.2f + i * (Mathf.Pi * 0.5f);
                        var inner = new Godot.Vector2(cx + Mathf.Cos(angle) * (ringR + 1f),
                                                      cy + Mathf.Sin(angle) * (ringR + 1f));
                        var outer = new Godot.Vector2(cx + Mathf.Cos(angle) * (ringR + rayLen * 0.6f),
                                                      cy + Mathf.Sin(angle) * (ringR + rayLen * 0.6f));
                        DrawLine(inner, outer, rayColor, 1f);
                    }
                    break;
                }
                case 4: // Vortex Sep. — rotating spiral
                {
                    float half2 = size * 0.35f;
                    var spiralColor = new Color(0f, 0.8f, 0.9f, 0.9f);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = _localTime * 2.5f + i * Mathf.Pi * 0.5f;
                        var innerPt = new Godot.Vector2(cx + Mathf.Cos(angle) * 2f,
                                                        cy + Mathf.Sin(angle) * 2f);
                        var outerPt = new Godot.Vector2(cx + Mathf.Cos(angle) * half2,
                                                        cy + Mathf.Sin(angle) * half2);
                        DrawLine(innerPt, outerPt, spiralColor, 1.5f);
                        float perpAngle = angle + Mathf.Pi * 0.25f;
                        var perpEnd = new Godot.Vector2(outerPt.X + Mathf.Cos(perpAngle) * (half2 * 0.4f),
                                                        outerPt.Y + Mathf.Sin(perpAngle) * (half2 * 0.4f));
                        DrawLine(outerPt, perpEnd, spiralColor, 1f);
                    }
                    break;
                }
                case 5: // Power Core — pulsing diamond with rays
                {
                    float pulse = 0.4f + 0.4f * Mathf.Sin(_localTime * 3f);
                    float d = size * 0.28f * (0.9f + 0.1f * pulse);
                    var gold = new Color(bright, 0.9f);
                    DrawLine(new Godot.Vector2(cx, cy - d), new Godot.Vector2(cx + d, cy), gold, 1.5f);
                    DrawLine(new Godot.Vector2(cx + d, cy), new Godot.Vector2(cx, cy + d), gold, 1.5f);
                    DrawLine(new Godot.Vector2(cx, cy + d), new Godot.Vector2(cx - d, cy), gold, 1.5f);
                    DrawLine(new Godot.Vector2(cx - d, cy), new Godot.Vector2(cx, cy - d), gold, 1.5f);
                    float rayLen = size * 0.5f;
                    var rayColor = new Color(1f, 0.9f, 0.1f, 0.8f);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = _localTime * 0.8f + i * Mathf.Pi * 0.5f;
                        var innerPt = new Godot.Vector2(cx + Mathf.Cos(angle) * (d + 1f),
                                                        cy + Mathf.Sin(angle) * (d + 1f));
                        var outerPt = new Godot.Vector2(cx + Mathf.Cos(angle) * rayLen,
                                                        cy + Mathf.Sin(angle) * rayLen);
                        DrawLine(innerPt, outerPt, rayColor, 1.5f);
                    }
                    break;
                }
                case 6: // Bio Neutraliser — rotating hex dots
                {
                    var atomColor = new Color(0.8f, 0.3f, 1.0f, 0.9f);
                    float r = size * 0.22f;
                    float rotOffset = _localTime * 1.5f;
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = rotOffset + i * (Mathf.Pi / 3f);
                        float ax = cx + Mathf.Cos(angle) * r;
                        float ay = cy + Mathf.Sin(angle) * r;
                        DrawRect(new Rect2(ax - s, ay - s, 2*s, 2*s), atomColor);
                    }
                    DrawRect(new Rect2(cx - s, cy - s, 2*s, 2*s), atomColor);
                    break;
                }
                case 7: // Magnetic Cage — was type 6 — inward arrows pulsing toward center
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_localTime * 5f);
                    var arrowColor = new Color(1f, 0.7f, 0.2f, 0.7f + 0.3f * pulse);
                    float arrowDist = size * 0.4f;
                    float tipSize   = 3.5f * s;
                    // Animate: arrows move inward slightly with pulse
                    float pulseDist = arrowDist - pulse * 2f * s;
                    Godot.Vector2[] dirs =
                    {
                        new Godot.Vector2(0, -1),
                        new Godot.Vector2(0,  1),
                        new Godot.Vector2(-1, 0),
                        new Godot.Vector2( 1, 0),
                    };
                    foreach (var dir in dirs)
                    {
                        var base1 = new Godot.Vector2(cx + dir.X * pulseDist, cy + dir.Y * pulseDist);
                        var tip   = new Godot.Vector2(cx + dir.X * (pulseDist - tipSize * 2f),
                                                      cy + dir.Y * (pulseDist - tipSize * 2f));
                        var perp  = new Godot.Vector2(-dir.Y * tipSize, dir.X * tipSize);
                        DrawLine(base1 - perp, tip, arrowColor, 1f);
                        DrawLine(base1 + perp, tip, arrowColor, 1f);
                        DrawLine(base1 - perp, base1 + perp, arrowColor, 1f);
                    }
                    break;
                }
            }
        }
    }
}
