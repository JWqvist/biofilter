using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Popup build menu that lists wall mode + towers.
/// UI is built entirely in code — no sub-scene required.
/// Open() / Close() / Toggle() control visibility.
/// Auto-closes when an item is selected or the player clicks outside.
/// Cannot be opened during the wave phase.
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();
    [Signal] public delegate void UpgradeRequestedEventHandler();

    // ── State ─────────────────────────────────────────────────────────────
    private int  _selectedTower = -1;   // -1 = wall mode, -2 = upgrade mode
    private bool _isWaveActive  = false;
    private bool _isOpen        = false;

    // ── UI references (built in _Ready) ──────────────────────────────────
    private Panel         _popup        = null!;
    private Button[]      _itemBtns     = null!;
    private Button        _upgradeBtn   = null!;
    private WaveManager   _waveManager  = null!;

    // ── Item metadata ─────────────────────────────────────────────────────
    // Index 0 = wall; indices 1-3 = towers (type 0-2)
    private static readonly (string Name, string Cost, string Desc, Color Color)[] Items =
    {
        ("■ Wall",          "[free]",                         "Direct particle flow",    new Color("#8bc34a")),
        ("■ Basic Filter",  $"[${GameConfig.BasicFilterCost}]",  "Damages particles nearby", new Color("#00bcd4")),
        ("■ Electrostatic", $"[${GameConfig.ElectrostaticCost}]","Slows particles nearby",   new Color("#ce93d8")),
        ("■ UV Steriliser", $"[${GameConfig.UVSteriliserCost}]", "Shoots at particles",      new Color("#fff176")),
    };

    public override void _Ready()
    {
        // ── Popup panel ──────────────────────────────────────────────────
        _popup = new Panel();
        // Width ~220px, positioned above bottom bar (offset_bottom = -50)
        _popup.LayoutMode = 3; // anchors
        _popup.AnchorLeft   = 0f;
        _popup.AnchorTop    = 1f;
        _popup.AnchorRight  = 0f;
        _popup.AnchorBottom = 1f;
        _popup.OffsetLeft   = 4f;
        _popup.OffsetTop    = -(GameConfig.BuildMenuItemCount * GameConfig.BuildMenuItemHeight
                                + GameConfig.BuildMenuTitleHeight + GameConfig.BuildMenuBottomMargin);
        _popup.OffsetRight  = GameConfig.BuildMenuWidth + 4f;
        _popup.OffsetBottom = -GameConfig.BuildMenuBottomMargin;
        _popup.Visible      = false;

        // Dark background via StyleBoxFlat
        var style = new StyleBoxFlat();
        style.BgColor       = new Color("#1a1a2e");
        style.BorderColor   = new Color("#5c5c8a");
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        _popup.AddThemeStyleboxOverride("panel", style);
        AddChild(_popup);

        // ── VBox layout ──────────────────────────────────────────────────
        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 0);
        vbox.GrowHorizontal = Control.GrowDirection.Both;
        vbox.GrowVertical   = Control.GrowDirection.Both;
        // Fill the panel
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _popup.AddChild(vbox);

        // ── Title bar ─────────────────────────────────────────────────────
        var titleBar = new HBoxContainer();
        titleBar.CustomMinimumSize = new Vector2(0, GameConfig.BuildMenuTitleHeight);
        vbox.AddChild(titleBar);

        var titleLabel = new Label();
        titleLabel.Text = "🛠 Build Menu";
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleLabel.AddThemeColorOverride("font_color", new Color("#e0e0e0"));
        titleLabel.AddThemeFontSizeOverride("font_size", 11);
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        titleBar.AddChild(titleLabel);

        var closeBtn = new Button();
        closeBtn.Text    = "✕";
        closeBtn.Flat    = true;
        closeBtn.CustomMinimumSize = new Vector2(GameConfig.BuildMenuTitleHeight, GameConfig.BuildMenuTitleHeight);
        closeBtn.Pressed += Close;
        titleBar.AddChild(closeBtn);

        // ── Separator ────────────────────────────────────────────────────
        vbox.AddChild(new HSeparator());

        // ── Item rows ─────────────────────────────────────────────────────
        _itemBtns = new Button[GameConfig.BuildMenuItemCount];
        for (int i = 0; i < GameConfig.BuildMenuItemCount; i++)
        {
            int captured = i;
            var (name, cost, desc, color) = Items[i];

            // Wrapper VBox for button + desc
            var itemVBox = new VBoxContainer();
            itemVBox.CustomMinimumSize = new Vector2(0, GameConfig.BuildMenuItemHeight);
            vbox.AddChild(itemVBox);

            // Row button
            var btn = new Button();
            btn.Flat = true;
            btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            btn.CustomMinimumSize   = new Vector2(0, GameConfig.BuildMenuItemHeight - 14);
            btn.Pressed += () => OnItemPressed(captured);
            _itemBtns[i] = btn;

            // HBox inside button for color square + name + cost
            var hbox = new HBoxContainer();
            hbox.MouseFilter = Control.MouseFilterEnum.Ignore;
            hbox.LayoutMode  = 1;
            hbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            btn.AddChild(hbox);

            var colorRect = new ColorRect();
            colorRect.CustomMinimumSize = new Vector2(10, 10);
            colorRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            colorRect.Color = color;
            hbox.AddChild(colorRect);

            var hSpacer = new Control();
            hSpacer.CustomMinimumSize = new Vector2(4, 0);
            hbox.AddChild(hSpacer);

            var nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            nameLabel.AddThemeColorOverride("font_color", new Color("#e0e0e0"));
            nameLabel.AddThemeFontSizeOverride("font_size", 10);
            nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            hbox.AddChild(nameLabel);

            var costLabel = new Label();
            costLabel.Text = cost;
            costLabel.AddThemeColorOverride("font_color", new Color("#aaaaaa"));
            costLabel.AddThemeFontSizeOverride("font_size", 10);
            costLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            hbox.AddChild(costLabel);

            itemVBox.AddChild(btn);

            var descLabel = new Label();
            descLabel.Text = desc;
            descLabel.AddThemeColorOverride("font_color", new Color("#888888"));
            descLabel.AddThemeFontSizeOverride("font_size", 9);
            descLabel.OffsetLeft = 14;
            descLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            itemVBox.AddChild(descLabel);

            vbox.AddChild(new HSeparator());
        }

        // ── Upgrade button (hidden until a tower is right-clicked) ────────
        _upgradeBtn = new Button();
        _upgradeBtn.Text    = "Upgrade";
        _upgradeBtn.Flat    = false;
        _upgradeBtn.Visible = false;
        _upgradeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _upgradeBtn.Pressed += OnUpgradeBtnPressed;
        vbox.AddChild(_upgradeBtn);

        // ── Connect WaveManager ───────────────────────────────────────────
        _waveManager = GetNode<WaveManager>("/root/Main/WaveManager");
        _waveManager.WaveStarted  += OnWaveStarted;
        _waveManager.WaveComplete += OnWaveComplete;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void Open()
    {
        if (_isWaveActive) return;
        _isOpen        = true;
        _popup.Visible = true;
        RefreshHighlights();
    }

    public void Close()
    {
        _isOpen        = false;
        _popup.Visible = false;
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else         Open();
    }

    /// <summary>Called by TowerManager when a tower tile is right-clicked.</summary>
    public void ShowUpgradeButton(int upgradeCost, bool canAfford)
    {
        if (_isWaveActive) return;
        _selectedTower = -2;
        RefreshHighlights();
        HideUpgradeButton();

        _upgradeBtn.Text     = upgradeCost <= 0 ? "MAX TIER" : $"Upgrade [${upgradeCost}]";
        _upgradeBtn.Disabled = upgradeCost <= 0 || !canAfford;
        _upgradeBtn.Visible  = true;

        Open();
    }

    public void HideUpgradeButton()
    {
        if (_upgradeBtn != null)
            _upgradeBtn.Visible = false;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void OnItemPressed(int index)
    {
        HideUpgradeButton();

        if (index == 0)
        {
            // Wall mode — deselect any tower
            _selectedTower = -1;
            EmitSignal(SignalName.TowerDeselected);
        }
        else
        {
            int towerType = index - 1;
            if (_selectedTower == towerType)
            {
                _selectedTower = -1;
                EmitSignal(SignalName.TowerDeselected);
            }
            else
            {
                _selectedTower = towerType;
                EmitSignal(SignalName.TowerSelected, towerType);
            }
        }

        Close();
    }

    private void OnUpgradeBtnPressed()
    {
        EmitSignal(SignalName.UpgradeRequested);
        Close();
    }

    private void OnWaveStarted(int _) { _isWaveActive = true;  Close(); }
    private void OnWaveComplete(int _) { _isWaveActive = false; }

    private void RefreshHighlights()
    {
        if (_itemBtns == null) return;
        for (int i = 0; i < _itemBtns.Length; i++)
        {
            bool active = (i == 0 && _selectedTower == -1) ||
                          (i > 0  && _selectedTower == i - 1);
            _itemBtns[i].Modulate = active ? Colors.Yellow : Colors.White;
        }
    }

    // ── Click-outside detection ────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent ev)
    {
        if (!_isOpen) return;
        if (ev is InputEventMouseButton mb && mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            var popupRect = _popup.GetGlobalRect();
            if (!popupRect.HasPoint(mb.GlobalPosition))
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
