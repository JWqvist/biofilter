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
        ("WALL",            "[W]", "FREE",   "Direct particle flow",  -1, new Color("#3a5a3a")),
        ("BASIC FILTER",    "[1]", $"${GameConfig.BasicFilterCost}",   "Damages particles nearby", 0, new Color("#00c853")),
        ("ELECTROSTATIC",   "[2]", $"${GameConfig.ElectrostaticCost}", "Slows particles nearby",   1, new Color("#2979ff")),
        ("UV STERILISER",   "[3]", $"${GameConfig.UVSteriliserCost}",  "Shoots at particles",      2, new Color("#aa00ff")),
        ("VORTEX SEP.",     "",    $"${GameConfig.VortexSeparatorCost}","Forces longer routes",    3, new Color("#00bcd4")),
        ("POWER CORE",      "",    $"${GameConfig.PowerCoreCost}",      "+$5 per wave",             4, new Color("#ffd700")),
        ("BIO NEUTRALISER", "",    $"${GameConfig.BioNeutraliserCost}", "Boosts adjacent +25%",    5, new Color("#9c27b0")),
        ("MAGNETIC CAGE",   "",    $"${GameConfig.MagneticCageCost}",   "Traps particles 2s",       6, new Color("#795548")),
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
        const int cols = 3;
        const int cardW = 140;
        const int cardH = 110;
        const int padX = 8;
        const int padY = 28;
        const int gapX = 6;
        const int gapY = 6;

        for (int i = 0; i < _modules.Length; i++)
        {
            var (name, hotkey, cost, desc, type, color) = _modules[i];
            int col = i % cols;
            int row = i / cols;
            float x = padX + col * (cardW + gapX);
            float y = padY + row * (cardH + gapY);

            var card = new ModuleCard(i, name, hotkey, cost, desc, color, cardW, cardH);
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
        private readonly string _name;
        private readonly string _hotkey;
        private readonly string _cost;
        private readonly string _desc;
        private readonly Color  _color;
        private readonly int    _w;
        private readonly int    _h;

        public ModuleCard(int index, string name, string hotkey, string cost, string desc, Color color, int w, int h)
        {
            _index = index; _name = name; _hotkey = hotkey; _cost = cost;
            _desc = desc; _color = color; _w = w; _h = h;
            MouseFilter = MouseFilterEnum.Stop;
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
            const int previewSize = 48;
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

            switch (_index)
            {
                case 0: // Wall
                    DrawRect(new Rect2(ox + 4*s, oy + 4*s, size - 8*s, size - 8*s), bright);
                    break;
                case 1: // Basic Filter — cross
                    DrawRect(new Rect2(cx - s, oy + 4*s, 2*s, size - 8*s), bright);
                    DrawRect(new Rect2(ox + 4*s, cy - s, size - 8*s, 2*s), bright);
                    break;
                case 2: // Electrostatic — lightning bolt
                    DrawRect(new Rect2(cx + s, oy + 4*s, 2*s, size*0.4f), bright);
                    DrawRect(new Rect2(cx - 2*s, cy - s, 4*s, 2*s), bright);
                    DrawRect(new Rect2(cx - 3*s, cy + s, 2*s, size*0.35f), bright);
                    break;
                case 3: // UV — ring
                    for (int a = 0; a < 8; a++) {
                        float angle = a * Mathf.Pi / 4f;
                        float rx = cx + Mathf.Cos(angle) * size * 0.3f;
                        float ry = cy + Mathf.Sin(angle) * size * 0.3f;
                        DrawRect(new Rect2(rx - s, ry - s, 2*s, 2*s), bright);
                    }
                    break;
                case 4: // Vortex — spiral dots
                    for (int a = 0; a < 6; a++) {
                        float t = a / 6f;
                        float r = t * size * 0.35f;
                        float angle = t * Mathf.Pi * 4f;
                        DrawRect(new Rect2(cx + Mathf.Cos(angle)*r - s, cy + Mathf.Sin(angle)*r - s, 2*s, 2*s), bright);
                    }
                    break;
                case 5: // Power Core — diamond
                    DrawRect(new Rect2(cx - s, oy + 4*s, 2*s, size - 8*s), bright);
                    DrawRect(new Rect2(ox + 4*s, cy - s, size - 8*s, 2*s), bright);
                    DrawRect(new Rect2(cx - 3*s, cy - 3*s, 2*s, 2*s), bright);
                    DrawRect(new Rect2(cx + s, cy + s, 2*s, 2*s), bright);
                    break;
                case 6: // Bio Neutraliser — hex dots
                    for (int a = 0; a < 6; a++) {
                        float angle = a * Mathf.Pi / 3f;
                        float rx = cx + Mathf.Cos(angle) * size * 0.28f;
                        float ry = cy + Mathf.Sin(angle) * size * 0.28f;
                        DrawRect(new Rect2(rx - s, ry - s, 2*s, 2*s), bright);
                    }
                    DrawRect(new Rect2(cx - s, cy - s, 2*s, 2*s), bright);
                    break;
                case 7: // Magnetic Cage — inward arrows
                    DrawRect(new Rect2(cx - s, oy + 4*s, 2*s, size*0.25f), bright);
                    DrawRect(new Rect2(cx - s, oy + size*0.7f, 2*s, size*0.25f), bright);
                    DrawRect(new Rect2(ox + 4*s, cy - s, size*0.25f, 2*s), bright);
                    DrawRect(new Rect2(ox + size*0.7f, cy - s, size*0.25f, 2*s), bright);
                    break;
            }
        }
    }
}
