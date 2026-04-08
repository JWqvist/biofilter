using System.Collections.Generic;
using BioFilter;
using Godot;

namespace BioFilter.Towers;

/// <summary>
/// Abstract base class for all filter module towers.
/// Renders a colored square matching the tile size, color defined by subclass.
/// </summary>
public abstract partial class TowerBase : Node2D
{
    public abstract float Range { get; }
    public abstract int Cost { get; }
    protected abstract Color TowerColor { get; }

    // Injected by TowerManager after placement
    public ParticleManager? ParticleManagerRef { get; set; }

    // Upgrade state
    public bool IsUpgraded { get; set; } = false;

    /// <summary>Damage multiplier applied by BioNeutraliser support towers (default 1.0).</summary>
    public float DamageMultiplier { get; set; } = 1.0f;

    /// <summary>Grid position (col, row) set by TowerManager on placement.</summary>
    public Vector2I GridPos { get; set; }

    // ── Saboteur disable mechanic ──────────────────────────────────────────────────
    /// <summary>True while this tower is disabled by a Saboteur kill.</summary>
    public bool IsDisabled { get; private set; } = false;
    private float _disabledTimer = 0f;
    private float _disabledFlashTimer = 0f;

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (IsDisabled)
        {
            _disabledTimer    += (float)delta;
            _disabledFlashTimer += (float)delta;
            if (_disabledTimer >= GameConfig.SaboteurDisableDuration)
            {
                IsDisabled = false;
                _disabledTimer = 0f;
                _disabledFlashTimer = 0f;
            }
            QueueRedraw();
        }
    }

    /// <summary>
    /// Called by subclasses (or ParticleManager) when a Saboteur particle is killed by this tower.
    /// Connects the SaboteurKilledByTower signal from the particle so we can disable ourselves.
    /// </summary>
    public void HookSaboteurSignal(Particle particle)
    {
        particle.SaboteurKilledByTower += OnSaboteurKilledByTower;
    }

    private void OnSaboteurKilledByTower(Vector2 towerPos)
    {
        if (towerPos.DistanceSquaredTo(GlobalPosition) < 4f) // tolerance: ~2px
        {
            IsDisabled = true;
            _disabledTimer = 0f;
            _disabledFlashTimer = 0f;
            QueueRedraw();
        }
    }

    /// <summary>Returns all active particles within the given range (in tiles).</summary>
    protected List<Particle> GetNearbyParticles(float rangeTiles)
    {
        var result = new List<Particle>();
        if (ParticleManagerRef == null) return result;

        float rangePixels = rangeTiles * GameConfig.TileSize;
        float rangePixelsSq = rangePixels * rangePixels;

        foreach (var child in ParticleManagerRef.GetChildren())
        {
            if (child is Particle p)
            {
                float distSq = GlobalPosition.DistanceSquaredTo(p.GlobalPosition);
                if (distSq <= rangePixelsSq)
                    result.Add(p);
            }
        }
        return result;
    }

    public override void _Draw()
    {
        int size = GameConfig.TileSize;
        float half = size * 0.5f;
        var rect = new Rect2(-half, -half, size, size);

        // Inner dark fill
        DrawRect(new Rect2(-half + 1, -half + 1, size - 2, size - 2), GetInnerColor());
        // Outer colored frame (1px border)
        DrawRect(rect, TowerColor, false, 1f);

        // Draw a small gold corner indicator when upgraded
        if (IsUpgraded)
        {
            int corner = size / 4;
            DrawRect(new Rect2(-half, -half, corner, corner), Colors.Gold);
        }

        // Red X overlay when disabled by Saboteur (flashing)
        if (IsDisabled)
        {
            bool flashOn = (int)(_disabledFlashTimer * 4f) % 2 == 0;
            if (flashOn)
            {
                var xColor = new Color(1f, 0.1f, 0.1f, 0.85f);
                DrawLine(new Vector2(-half + 2, -half + 2), new Vector2(half - 2, half - 2), xColor, 2f);
                DrawLine(new Vector2(half - 2, -half + 2), new Vector2(-half + 2, half - 2), xColor, 2f);
                DrawRect(rect, new Color(1f, 0.1f, 0.1f, 0.25f));
            }
        }
    }

    /// <summary>Override in subclasses to return the inner fill color. Default: dark metal.</summary>
    protected virtual Color GetInnerColor() => Constants.Colors.MetalDark;
}
