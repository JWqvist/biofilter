using BioFilter;
using Godot;

namespace BioFilter.UI;

/// <summary>
/// Popup build menu — retro CRT terminal / military bio-lab style.
/// Built entirely in code. UI is built in _Ready — no sub-scene required.
/// Sprint 13C: Pixel Art Menus
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    [Signal] public delegate void TowerSelectedEventHandler(int towerType);
    [Signal] public delegate void TowerDeselectedEventHandler();
    [Signal] public delegate void UpgradeRequestedEventHandler();

    // ── State ──────────────────────────────────────────────────────────────
    private int  _selectedTower = -1;   // -1 = wall mode, -2 = upgrade mode
    private bool _isWaveActive  = false;
    private bool _isOpen        = false;

    // ── UI references ──────────────────────────────────────────────────────
    private Panel         _popup           = null!;
    private Button[]      _itemBtns        = null!;
    private Button        _upgradeBtn      = null!;
    private WaveManager   _waveManager     = null!;
    private Label         _waveLockLabel   = null!;
    private VBoxContainer _itemsBox        = null!;

    // Blinking cursor
    private float _blinkTimer  = 0f;
    private bool  _cursorOn    = true;
    private Label _cursorLabel = null!;

    // ── Item metadata ──────────────────────────────────────────────────────
    // Index 0 = wall; indices 1+ = towers
    private static readonly (string Name, string Cost, Color Color)[] Items =
    {
        ("■ Wall [W]",             "[free]",                                   new Color("#8bc34a")),
        ($"■ Basic Filter [1]",   $"[${GameConfig.BasicFilterCost}]",        new Color("#00c853")),
        ($"■ Electrostatic [2]",  $"[${GameConfig.ElectrostaticCost}]",      new Color("#2979ff")),
        ($"■ UV Steriliser [3]",  $"[${GameConfig.UVSteriliserCost}]",       new Color("#aa00ff")),
        ($"■ Vortex Sep.",        $"[${GameConfig.VortexSeparatorCost}]",    new Color("#00bcd4")),
        ($"■ Power Core",         $"[${GameConfig.PowerCoreCost}]",          new Color("#ffd700")),
        ($"■ Bio Neutraliser",    $"[${GameConfig.BioNeutraliserCost}]",     new Color("#9c27b0")),
        ($"■ Magnetic Cage",      $"[${GameConfig.MagneticCageCost}]",       new Color("#795548")),
    };

    private static readonly Color BorderGreen  = new Color("#2d5a3d");
    private static readonly Color BgDark       = new Color("#0d1208");
    private static readonly Color RowSelected  = new Color("#1a3a1a");
    private static readonly Color RowHover     = new Color("#1e3e1e");

    public override void _Ready()
    {
        // ── Popup panel ───────────────────────────────────────────────────
        _popup = new Panel();
        _popup.LayoutMode = 3;
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

        var style = new StyleBoxFlat();
        style.BgColor     = BgDark;
        style.BorderColor = BorderGreen;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(0);
        _popup.AddThemeStyleboxOverride("panel", style);
        AddChild(_popup);

        // ── VBox ─────────────────────────────────────────────────────────
        var vbox = new VBoxContainer();
        vbox.LayoutMode = 1;
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 0);
        _popup.AddChild(vbox);

        // ── Title bar ────────────────────────────────────────────────────
        var titleBar = new HBoxContainer();
        titleBar.CustomMinimumSize = new Vector2(0, GameConfig.BuildMenuTitleHeight);

        var titleStyle = new StyleBoxFlat();
        titleStyle.BgColor = new Color("#0a1a0a");
        titleStyle.SetBorderWidthAll(0);
        titleBar.AddThemeStyleboxOverride("panel", titleStyle);
        vbox.AddChild(titleBar);

        var titleLabel = new Label();
        titleLabel.Text = "▣ BUILD MENU";
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleLabel.AddThemeColorOverride("font_color", new Color("#4caf50"));
        titleLabel.AddThemeFontSizeOverride("font_size", 11);
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        titleBar.AddChild(titleLabel);

        _cursorLabel = new Label();
        _cursorLabel.Text = "|";
        _cursorLabel.AddThemeColorOverride("font_color", new Color("#4caf50"));
        _cursorLabel.AddThemeFontSizeOverride("font_size", 11);
        _cursorLabel.VerticalAlignment = VerticalAlignment.Center;
        titleBar.AddChild(_cursorLabel);

        var closeSpacer = new Control();
        closeSpacer.CustomMinimumSize = new Vector2(4, 0);
        titleBar.AddChild(closeSpacer);

        var closeBtn = new Button();
        closeBtn.Text    = "✕";
        closeBtn.Flat    = false;
        closeBtn.CustomMinimumSize = new Vector2(GameConfig.BuildMenuTitleHeight, GameConfig.BuildMenuTitleHeight);
        closeBtn.AddThemeColorOverride("font_color", new Color("#cc4444"));
        closeBtn.AddThemeFontSizeOverride("font_size", 11);
        var closeBtnStyle = new StyleBoxFlat();
        closeBtnStyle.BgColor     = new Color("#1a0a0a");
        closeBtnStyle.BorderColor = new Color("#3a1a1a");
        closeBtnStyle.SetBorderWidthAll(1);
        closeBtnStyle.SetCornerRadiusAll(0);
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        var closeBtnHover = new StyleBoxFlat();
        closeBtnHover.BgColor     = new Color("#2a1a1a");
        closeBtnHover.BorderColor = new Color("#cc4444");
        closeBtnHover.SetBorderWidthAll(1);
        closeBtnHover.SetCornerRadiusAll(0);
        closeBtn.AddThemeStyleboxOverride("hover", closeBtnHover);
        closeBtn.Pressed += Close;
        titleBar.AddChild(closeBtn);

        // ── Divider ──────────────────────────────────────────────────────
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 2f);
        div.Color = BorderGreen;
        vbox.AddChild(div);

        // ── Wave-locked message (hidden by default) ───────────────────────
        _waveLockLabel = new Label();
        _waveLockLabel.Text = "  ⚠ WAVE PHASE — BUILD LOCKED";
        _waveLockLabel.AddThemeColorOverride("font_color", new Color("#cc3333"));
        _waveLockLabel.AddThemeFontSizeOverride("font_size", 10);
        _waveLockLabel.Visible = false;
        _waveLockLabel.CustomMinimumSize = new Vector2(0, 24f);
        _waveLockLabel.VerticalAlignment = VerticalAlignment.Center;
        vbox.AddChild(_waveLockLabel);

        // ── Items container ───────────────────────────────────────────────
        _itemsBox = new VBoxContainer();
        _itemsBox.AddThemeConstantOverride("separation", 0);
        vbox.AddChild(_itemsBox);

        _itemBtns = new Button[GameConfig.BuildMenuItemCount];
        for (int i = 0; i < GameConfig.BuildMenuItemCount; i++)
        {
            int captured = i;
            var (name, cost, color) = Items[i];

            var btn = new Button();
            btn.Flat = true;
            btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            btn.CustomMinimumSize   = new Vector2(0, GameConfig.BuildMenuItemHeight);
            btn.MouseFilter = Control.MouseFilterEnum.Stop;
            btn.Pressed += () => OnItemPressed(captured);
            _itemBtns[i] = btn;

            var normalStyle = new StyleBoxFlat();
            normalStyle.BgColor = BgDark;
            normalStyle.SetBorderWidthAll(0);
            btn.AddThemeStyleboxOverride("normal", normalStyle);
            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = RowHover;
            hoverStyle.SetBorderWidthAll(0);
            btn.AddThemeStyleboxOverride("hover", hoverStyle);
            var pressStyle = new StyleBoxFlat();
            pressStyle.BgColor = RowSelected;
            pressStyle.SetBorderWidthAll(0);
            btn.AddThemeStyleboxOverride("pressed", pressStyle);

            // Content inside button
            var hbox = new HBoxContainer();
            hbox.LayoutMode  = 1;
            hbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            hbox.MouseFilter = Control.MouseFilterEnum.Ignore;
            hbox.AddThemeConstantOverride("separation", 4);
            btn.AddChild(hbox);

            // Color square icon
            var spacerLeft = new Control();
            spacerLeft.CustomMinimumSize = new Vector2(4, 0);
            hbox.AddChild(spacerLeft);

            var colorRect = new ColorRect();
            colorRect.CustomMinimumSize = new Vector2(10, 10);
            colorRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            colorRect.Color = color;
            hbox.AddChild(colorRect);

            var nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            nameLabel.AddThemeColorOverride("font_color", new Color("#c8e6c0"));
            nameLabel.AddThemeFontSizeOverride("font_size", 10);
            nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            nameLabel.VerticalAlignment = VerticalAlignment.Center;
            hbox.AddChild(nameLabel);

            var costLabel = new Label();
            costLabel.Text = cost;
            costLabel.AddThemeColorOverride("font_color", new Color("#6a8a6a"));
            costLabel.AddThemeFontSizeOverride("font_size", 9);
            costLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            costLabel.VerticalAlignment = VerticalAlignment.Center;
            hbox.AddChild(costLabel);

            var spacerRight = new Control();
            spacerRight.CustomMinimumSize = new Vector2(4, 0);
            hbox.AddChild(spacerRight);

            _itemsBox.AddChild(btn);

            // Row divider (thin dim line)
            if (i < GameConfig.BuildMenuItemCount - 1)
            {
                var rowDiv = new ColorRect();
                rowDiv.CustomMinimumSize = new Vector2(0, 1f);
                rowDiv.Color = new Color("#1a2a1a");
                _itemsBox.AddChild(rowDiv);
            }
        }

        // ── Upgrade button ────────────────────────────────────────────────
        _upgradeBtn = new Button();
        _upgradeBtn.Text    = "Upgrade";
        _upgradeBtn.Flat    = false;
        _upgradeBtn.Visible = false;
        _upgradeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var upgNormal = new StyleBoxFlat();
        upgNormal.BgColor     = new Color("#0a2a0a");
        upgNormal.BorderColor = BorderGreen;
        upgNormal.SetBorderWidthAll(1);
        upgNormal.SetCornerRadiusAll(0);
        _upgradeBtn.AddThemeStyleboxOverride("normal", upgNormal);
        _upgradeBtn.AddThemeColorOverride("font_color", new Color("#4caf50"));
        _upgradeBtn.AddThemeFontSizeOverride("font_size", 11);
        _upgradeBtn.Pressed += OnUpgradeBtnPressed;
        vbox.AddChild(_upgradeBtn);

        // ── Connect WaveManager ───────────────────────────────────────────
        _waveManager = GetNode<WaveManager>("/root/Main/VBoxContainer/GameArea/WaveManager");
        _waveManager.WaveStarted  += OnWaveStarted;
        _waveManager.WaveComplete += OnWaveComplete;
    }

    public override void _Process(double delta)
    {
        if (!_isOpen) return;
        _blinkTimer += (float)delta;
        if (_blinkTimer >= 0.5f)
        {
            _blinkTimer = 0f;
            _cursorOn   = !_cursorOn;
            if (_cursorLabel != null)
                _cursorLabel.Modulate = _cursorOn ? Colors.White : new Color(1f, 1f, 1f, 0f);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

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

    // ── Private ───────────────────────────────────────────────────────────

    private void OnItemPressed(int index)
    {
        HideUpgradeButton();

        if (index == 0)
        {
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

    private void OnWaveStarted(int _)
    {
        _isWaveActive = true;
        _waveLockLabel.Visible = true;
        _itemsBox.Visible = false;
        Close();
    }

    private void OnWaveComplete(int _)
    {
        _isWaveActive = false;
        _waveLockLabel.Visible = false;
        _itemsBox.Visible = true;
    }

    private void RefreshHighlights()
    {
        if (_itemBtns == null) return;
        for (int i = 0; i < _itemBtns.Length; i++)
        {
            bool active = (i == 0 && _selectedTower == -1) ||
                          (i > 0  && _selectedTower == i - 1);

            var activeStyle = new StyleBoxFlat();
            activeStyle.BgColor = active ? RowSelected : BgDark;
            activeStyle.SetBorderWidthAll(0);
            _itemBtns[i].AddThemeStyleboxOverride("normal", activeStyle);
        }
    }

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
