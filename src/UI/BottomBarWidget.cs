using Godot;
using BioFilter;
using BioFilter.UI;

namespace BioFilter.UI;

/// <summary>
/// Pixel-art BottomBar that replaces the individual Button/Label nodes.
/// Draws BUILD button, phase label, status text, speed toggle, and START WAVE button
/// entirely via DrawRect/PixelFont in _Draw().
/// </summary>
public partial class BottomBarWidget : Control
{
    // ── Signals ─────────────────────────────────────────────────────────────
    [Signal] public delegate void BuildPressedEventHandler();
    [Signal] public delegate void StartWavePressedEventHandler();
    [Signal] public delegate void SpeedToggledEventHandler(float newSpeed);

    // ── State ────────────────────────────────────────────────────────────────
    private bool   _isBuildPhase = true;
    private bool   _isFast       = false;
    private string _statusText   = "Wall mode";

    // ── Hit rects (updated every _Draw) ─────────────────────────────────────
    private Rect2 _buildRect    = new Rect2(-1, -1, 0, 0);
    private Rect2 _startWaveRect= new Rect2(-1, -1, 0, 0);
    private Rect2 _speedRect    = new Rect2(-1, -1, 0, 0);

    // ── Visual constants ─────────────────────────────────────────────────────
    private const float PixSm = 1.5f; // pixel font scale

    private static readonly Color ColBg          = Constants.Colors.MetalDark;           // #1e2420
    private static readonly Color ColBorder       = new Color("#2d5a3d");                 // build btn border
    private static readonly Color ColBorderBright = new Color("#00c853");                 // start wave border
    private static readonly Color ColBuildPhase   = new Color("#00c853");                 // build phase green
    private static readonly Color ColWavePhase    = new Color("#ff6d00");                 // wave phase orange
    private static readonly Color ColWhite        = Constants.Colors.TextPrimary;         // #c8e6c0
    private static readonly Color ColDim          = Constants.Colors.TextDim;             // #6a8a6a
    private static readonly Color ColTopEdge      = Constants.Colors.CornerMarker;        // #4caf50

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        // Accept mouse input for click detection
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>
    /// Wire to WaveManager so the widget tracks build/wave phase automatically.
    /// </summary>
    public void Initialize(WaveManager waveManager)
    {
        waveManager.WaveStarted  += OnWaveStarted;
        waveManager.WaveComplete += OnWaveComplete;
    }

    private void OnWaveStarted(int _)
    {
        _isBuildPhase = false;
        QueueRedraw();
    }

    private void OnWaveComplete(int _)
    {
        _isBuildPhase = true;
        ResetSpeed();
        QueueRedraw();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Update the centre status text.</summary>
    public void SetStatus(string text)
    {
        _statusText = text;
        QueueRedraw();
    }

    /// <summary>Reset speed to 1× (called on game-over / win / wave complete).</summary>
    public void ResetSpeed()
    {
        _isFast = false;
        Engine.TimeScale = GameConfig.SpeedNormal;
        QueueRedraw();
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;

        // Full background
        DrawRect(new Rect2(0, 0, w, h), ColBg);

        // Top green separator line
        DrawRect(new Rect2(0, 0, w, 1), ColTopEdge);

        float pad   = 4f;
        float btnH  = h - 4f;   // buttons occupy most of height, leaving 2px top+bottom margin
        float textY = 2f + (btnH - PixelFont.CharHeight(PixSm)) * 0.5f;

        float x = pad;

        // ── [BUILD] button ───────────────────────────────────────────────────
        string buildStr = $"{PixelFont.BlockChar} BUILD";
        float  buildW   = PixelFont.MeasureString(buildStr, PixSm) + 10f;

        _buildRect = new Rect2(x, 2f, buildW, btnH);
        DrawPanelBorder(_buildRect, ColBorder);
        PixelFont.DrawString(this, buildStr, new Vector2(x + 5f, textY), PixSm,
            _isBuildPhase ? ColBuildPhase : ColDim);

        x += buildW + pad;

        // ── PHASE label ───────────────────────────────────────────────────────
        string phaseStr = _isBuildPhase ? "BUILD PHASE" : "WAVE PHASE";
        Color  phaseCol = _isBuildPhase ? ColBuildPhase : ColWavePhase;
        float  phaseW   = PixelFont.MeasureString(phaseStr, PixSm) + 4f;

        PixelFont.DrawString(this, phaseStr, new Vector2(x, textY), PixSm, phaseCol);
        x += phaseW + pad;

        // ── Right-side buttons (speed + start wave) ──────────────────────────
        string speedStr = _isFast ? $"{PixelFont.PlayChar}{PixelFont.PlayChar} 2X"
                                  : $"{PixelFont.PlayChar} 1X";
        float  speedW   = PixelFont.MeasureString(speedStr, PixSm) + 10f;

        string waveStr  = $"{PixelFont.PlayChar} START WAVE";
        float  waveW    = PixelFont.MeasureString(waveStr, PixSm) + 10f;

        // Right edge, buttons placed right-to-left
        float rx = w - pad;

        if (_isBuildPhase)
        {
            rx -= waveW;
            _startWaveRect = new Rect2(rx, 2f, waveW, btnH);
            DrawPanelBorder(_startWaveRect, ColBorderBright);
            PixelFont.DrawString(this, waveStr, new Vector2(rx + 5f, textY), PixSm, ColBorderBright);
            rx -= pad;
        }
        else
        {
            _startWaveRect = new Rect2(-1, -1, 0, 0);
        }

        rx -= speedW;
        _speedRect = new Rect2(rx, 2f, speedW, btnH);
        DrawPanelBorder(_speedRect, ColDim);
        PixelFont.DrawString(this, speedStr, new Vector2(rx + 5f, textY), PixSm, ColWhite);
        rx -= pad;

        // ── STATUS text (fills the middle gap) ───────────────────────────────
        float statusW = rx - x;
        if (statusW > 0)
        {
            PixelFont.DrawString(this, _statusText, new Vector2(x, textY), PixSm, ColDim);
        }
    }

    /// <summary>Draw a 1px border rect around <paramref name="r"/> without fill.</summary>
    private void DrawPanelBorder(Rect2 r, Color col)
    {
        DrawRect(new Rect2(r.Position.X,              r.Position.Y,              r.Size.X, 1),       col); // top
        DrawRect(new Rect2(r.Position.X,              r.Position.Y + r.Size.Y - 1, r.Size.X, 1),     col); // bottom
        DrawRect(new Rect2(r.Position.X,              r.Position.Y,              1,        r.Size.Y), col); // left
        DrawRect(new Rect2(r.Position.X + r.Size.X - 1, r.Position.Y,            1,        r.Size.Y), col); // right
    }

    // ── Input ────────────────────────────────────────────────────────────────

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
            float newSpeed = _isFast ? GameConfig.SpeedFast : GameConfig.SpeedNormal;
            EmitSignal(SignalName.SpeedToggled, newSpeed);
            QueueRedraw();
        }
    }
}
