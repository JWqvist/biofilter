using Godot;

namespace BioFilter.UI;

/// <summary>
/// Widescreen HUD right panel (128×360px).
/// Replaces old LivesMeter, CurrencyMeter, BuildButton, StartWaveButton, BottomBarWidget.
/// Draws population, credits, threat bar, build/start-wave buttons, and phase status.
/// </summary>
public partial class RightPanel : Control
{
    // ── Signals ───────────────────────────────────────────────────────────
    [Signal] public delegate void BuildPressedEventHandler();
    [Signal] public delegate void StartWavePressedEventHandler();
    [Signal] public delegate void SpeedToggledEventHandler(float newSpeed);

    // ── State ─────────────────────────────────────────────────────────────
    private int    _population   = GameConfig.StartingPopulation;
    private int    _currency     = GameConfig.StartingCurrency;
    private int    _currentWave  = 0;
    private bool   _isBuildPhase = true;
    private bool   _isFast       = false;
    private string _statusText   = "BUILD PHASE";

    // ── Hit rects ─────────────────────────────────────────────────────────
    private Rect2 _buildRect     = new Rect2(-1, -1, 0, 0);
    private Rect2 _startWaveRect = new Rect2(-1, -1, 0, 0);
    private Rect2 _speedRect     = new Rect2(-1, -1, 0, 0);

    // ── Colors ────────────────────────────────────────────────────────────
    private static readonly Color ColBg          = new Color("#0d1208");
    private static readonly Color ColBorder      = new Color("#1e3a1e");
    private static readonly Color ColLabel       = new Color("#4a7a4a");
    private static readonly Color ColAmber       = new Color("#ff8f00");
    private static readonly Color ColGreen       = new Color("#4caf50");
    private static readonly Color ColRed         = new Color("#c62828");
    private static readonly Color ColText        = new Color("#c8e6c0");
    private static readonly Color ColDivider     = new Color("#1e3a1e");
    private static readonly Color ColBarEmpty    = new Color("#1a2a1a");
    private static readonly Color ColBtnBg       = new Color("#1e3a1e");
    private static readonly Color ColBtnBorder   = new Color("#2d5a3d");
    private static readonly Color ColSpeedDim    = new Color("#2a4a2a");

    private const float PixS  = 1.0f;
    private const float PixSm = 1.0f;
    private const int   ThreatSegs = 10;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void UpdatePopulation(int population)
    {
        _population = population;
        QueueRedraw();
    }

    public void UpdateCurrency(int currency)
    {
        _currency = currency;
        QueueRedraw();
    }

    public void Initialize(WaveManager waveManager)
    {
        waveManager.WaveStarted  += OnWaveStarted;
        waveManager.WaveComplete += OnWaveComplete;
    }

    public void OnWaveStarted(int waveNumber)
    {
        _currentWave  = waveNumber;
        _isBuildPhase = false;
        _statusText   = $"WAVE {waveNumber:D2} ACTIVE";
        QueueRedraw();
    }

    public void OnWaveComplete(int waveNumber)
    {
        _currentWave  = waveNumber;
        _isBuildPhase = true;
        _statusText   = "BUILD PHASE";
        ResetSpeed();
        QueueRedraw();
    }

    public void SetStatus(string text)
    {
        _statusText = text;
        QueueRedraw();
    }

    public void ResetSpeed()
    {
        _isFast = false;
        Engine.TimeScale = GameConfig.SpeedNormal;
        QueueRedraw();
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;

        // Background
        DrawRect(new Rect2(0, 0, w, h), ColBg);
        // Left border (separates from grid)
        DrawRect(new Rect2(0, 0, 2f, h), ColBorder);

        float pad   = 5f;
        float inner = w - pad * 2f - 2f; // account for left border
        float ox    = 2f + pad;          // x offset after border + padding
        float y     = 4f;

        // ── POP ────────────────────────────────────────────────────────────
        y = DrawSection(y, ox, inner, "POP",
            $"{_population:D3}",
            _population < 20 ? ColRed : ColAmber);

        // ── CREDITS ────────────────────────────────────────────────────────
        y = DrawSection(y, ox, inner, "CREDITS", $"$ {_currency:D4}", ColGreen);

        // ── THREAT ─────────────────────────────────────────────────────────
        y = DrawThreatSection(y, ox, inner);

        // ── Buttons ────────────────────────────────────────────────────────
        y += 4f;

        float btnW  = inner;
        float btnH  = 14f;

        // BUILD button
        _buildRect = new Rect2(ox, y, btnW, btnH);
        DrawRect(new Rect2(ox, y, btnW, btnH), ColBtnBg);
        DrawBorder(_buildRect, ColBtnBorder);
        string buildStr = "BUILD MODULE";
        float  bsTW = PixelFont.MeasureString(buildStr, PixS);
        float  bsTX = ox + (btnW - bsTW) * 0.5f;
        float  bsTY = y + (btnH - PixelFont.CharHeight(PixS)) * 0.5f;
        PixelFont.DrawString(this, buildStr, new Vector2(bsTX, bsTY), PixS, ColText);
        y += btnH + 4f;

        // START WAVE button (hidden during wave)
        if (_isBuildPhase)
        {
            _startWaveRect = new Rect2(ox, y, btnW, btnH);
            DrawRect(new Rect2(ox, y, btnW, btnH), new Color("#1a3a1a"));
            DrawBorder(_startWaveRect, ColGreen);
            string waveStr = $"START WAVE {_currentWave + 1:D2}";
            float  wsTW = PixelFont.MeasureString(waveStr, PixS);
            float  wsTX = ox + (btnW - wsTW) * 0.5f;
            float  wsTY = y + (btnH - PixelFont.CharHeight(PixS)) * 0.5f;
            PixelFont.DrawString(this, waveStr, new Vector2(wsTX, wsTY), PixS, ColGreen);
            y += btnH + 4f;
        }
        else
        {
            _startWaveRect = new Rect2(-1, -1, 0, 0);
        }

        // SPEED button
        string speedStr = _isFast ? "SPEED: 2X" : "SPEED: 1X";
        _speedRect = new Rect2(ox, y, btnW, btnH);
        DrawRect(new Rect2(ox, y, btnW, btnH), ColSpeedDim);
        DrawBorder(_speedRect, ColBorder);
        float spTW = PixelFont.MeasureString(speedStr, PixS);
        float spTX = ox + (btnW - spTW) * 0.5f;
        float spTY = y + (btnH - PixelFont.CharHeight(PixS)) * 0.5f;
        PixelFont.DrawString(this, speedStr, new Vector2(spTX, spTY), PixS,
            _isFast ? ColAmber : ColLabel);
        y += btnH + 4f;

        // ── STATUS ─────────────────────────────────────────────────────────
        DrawRect(new Rect2(ox, y, inner, 1f), ColDivider);
        y += 3f;
        PixelFont.DrawString(this, "STATUS:", new Vector2(ox, y), PixS, ColLabel);
        y += PixelFont.CharHeight(PixS) + 2f;
        Color statusCol = _isBuildPhase ? ColGreen : ColAmber;
        PixelFont.DrawString(this, _statusText, new Vector2(ox, y), PixS, statusCol);
    }

    /// <summary>Draw a labeled value section. Returns new y after divider.</summary>
    private float DrawSection(float y, float ox, float inner, string label, string value, Color valueCol)
    {
        PixelFont.DrawString(this, label, new Vector2(ox, y), PixS, ColLabel);
        y += PixelFont.CharHeight(PixS) + 1f;
        PixelFont.DrawString(this, value, new Vector2(ox, y), PixS, valueCol);
        y += PixelFont.CharHeight(PixS) + 3f;
        DrawRect(new Rect2(ox, y, inner, 1f), ColDivider);
        return y + 4f;
    }

    /// <summary>Draw THREAT section with segmented bar.</summary>
    private float DrawThreatSection(float y, float ox, float inner)
    {
        PixelFont.DrawString(this, "THREAT", new Vector2(ox, y), PixS, ColLabel);
        y += PixelFont.CharHeight(PixS) + 1f;

        float threat = GameConfig.TotalWaves > 0
            ? (float)_currentWave / GameConfig.TotalWaves
            : 0f;
        int pct = (int)(threat * 100f);

        // Bar
        float segGap = 1f;
        float segW   = (inner - segGap * (ThreatSegs - 1)) / ThreatSegs;
        int   filled = (int)(threat * ThreatSegs);
        Color barCol = threat < 0.4f ? ColGreen
                     : threat < 0.7f ? ColAmber
                                     : ColRed;
        for (int i = 0; i < ThreatSegs; i++)
        {
            float sx = ox + i * (segW + segGap);
            DrawRect(new Rect2(sx, y, segW, 5f), i < filled ? barCol : ColBarEmpty);
        }
        y += 7f;

        // Percentage label
        string pctStr = $"{pct}%";
        PixelFont.DrawString(this, pctStr, new Vector2(ox, y), PixS, barCol);
        y += PixelFont.CharHeight(PixS) + 3f;
        DrawRect(new Rect2(ox, y, inner, 1f), ColDivider);
        return y + 4f;
    }

    private void DrawBorder(Rect2 r, Color col)
    {
        DrawRect(new Rect2(r.Position.X, r.Position.Y, r.Size.X, 1f), col);
        DrawRect(new Rect2(r.Position.X, r.Position.Y + r.Size.Y - 1f, r.Size.X, 1f), col);
        DrawRect(new Rect2(r.Position.X, r.Position.Y, 1f, r.Size.Y), col);
        DrawRect(new Rect2(r.Position.X + r.Size.X - 1f, r.Position.Y, 1f, r.Size.Y), col);
    }

    // ── Input ─────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed || mb.ButtonIndex != MouseButton.Left)
            return;

        var pos = mb.Position;

        if (_buildRect.HasPoint(pos))
        {
            EmitSignal(SignalName.BuildPressed);
            return;
        }

        if (_isBuildPhase && _startWaveRect.HasPoint(pos))
        {
            EmitSignal(SignalName.StartWavePressed);
            return;
        }

        if (_speedRect.HasPoint(pos))
        {
            _isFast = !_isFast;
            Engine.TimeScale = _isFast ? GameConfig.SpeedFast : GameConfig.SpeedNormal;
            EmitSignal(SignalName.SpeedToggled, _isFast ? GameConfig.SpeedFast : GameConfig.SpeedNormal);
            QueueRedraw();
        }
    }
}
