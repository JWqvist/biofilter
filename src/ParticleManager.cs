using System.Collections.Generic;
using BioFilter.Effects;
using Godot;

namespace BioFilter;

/// <summary>
/// Spawns, tracks, and manages all active bio-particles.
/// Recalculates paths whenever the grid changes via GridManager.AirflowChanged.
/// </summary>
public partial class ParticleManager : Node2D
{
    // ── Scene reference injected from Main ───────────────────────────────────
    public GridManager? GridManager { get; set; }
    public GameState?   GameState   { get; set; }
    public WaveManager? WaveManager { get; set; }

    private PackedScene _particleScene = null!;
    private readonly List<Particle> _activeParticles = new();

    // Cached path (recalculated on grid change)
    private List<Vector2>? _cachedWorldPath;

    // Random number generator for swarm offsets
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _particleScene = GD.Load<PackedScene>("res://scenes/Particle.tscn");
    }

    /// <summary>Called by Main after nodes are wired.</summary>
    public void ConnectSignals()
    {
        if (GridManager != null)
            GridManager.AirflowChanged += OnAirflowChanged;
    }

    // ── Path helpers ──────────────────────────────────────────────────────────
    private List<Vector2>? BuildWorldPath()
    {
        if (GridManager == null) return null;

        var tileGrid = GridManager.GetGrid();
        var start    = new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow);
        var tilePath = Pathfinder.FindPath(tileGrid, start);

        if (tilePath == null || tilePath.Count == 0) return null;

        var worldPath = new List<Vector2>(tilePath.Count);
        foreach (var tile in tilePath)
        {
            float cx = tile.X * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
            float cy = tile.Y * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
            worldPath.Add(new Vector2(cx, cy));
        }
        return worldPath;
    }

    private void RefreshCachedPath()
    {
        _cachedWorldPath = BuildWorldPath();

        foreach (var p in _activeParticles)
            RecalculateParticlePath(p);
    }

    private void RecalculateParticlePath(Particle p)
    {
        if (_cachedWorldPath == null || _cachedWorldPath.Count == 0) return;

        int bestIdx  = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _cachedWorldPath.Count; i++)
        {
            float d = p.Position.DistanceTo(_cachedWorldPath[i]);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }

        var remainingPath = _cachedWorldPath.GetRange(bestIdx, _cachedWorldPath.Count - bestIdx);
        p.Reroute(remainingPath);
    }

    // ── Spawn API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn a particle of the given type.
    /// BacterialSwarm spawns 8 SwarmUnit particles instead of 1 BacterialSwarm.
    /// Returns the number of "alive" counters to add to WaveManager.
    /// </summary>
    public int SpawnParticle(float healthMultiplier = 1.0f, ParticleType type = ParticleType.BioParticle)
    {
        if (_cachedWorldPath == null)
            RefreshCachedPath();

        if (_cachedWorldPath == null || _cachedWorldPath.Count == 0)
        {
            GD.PrintErr("ParticleManager: No path available — cannot spawn particle.");
            return 0;
        }

        if (type == ParticleType.BacterialSwarm)
        {
            // Spawn 8 SwarmUnits with slight positional offsets
            for (int i = 0; i < GameConfig.SwarmUnitCount; i++)
                SpawnSingle(healthMultiplier, ParticleType.SwarmUnit, offset: true);
            return GameConfig.SwarmUnitCount;
        }

        SpawnSingle(healthMultiplier, type);
        return 1;
    }

    /// <summary>Spawn a CellDivision child at a specific world position.</summary>
    public void SpawnDivisionChild(Vector2 worldPosition, float healthMultiplier = 1.0f)
    {
        if (_cachedWorldPath == null)
            RefreshCachedPath();

        if (_cachedWorldPath == null || _cachedWorldPath.Count == 0) return;

        // Find closest waypoint to the death position
        int bestIdx  = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _cachedWorldPath.Count; i++)
        {
            float d = worldPosition.DistanceTo(_cachedWorldPath[i]);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }

        var remainingPath = _cachedWorldPath.GetRange(bestIdx, _cachedWorldPath.Count - bestIdx);

        // Override the first waypoint so the child starts exactly at the parent's position
        var spawnPath = new List<Vector2>(remainingPath);
        if (spawnPath.Count > 0)
            spawnPath[0] = worldPosition;

        var particle = _particleScene.Instantiate<Particle>();
        AddChild(particle);
        particle.Initialize(spawnPath, healthMultiplier, ParticleType.CellDivision, isDivisionChild: true);
        HookParticleSignals(particle);
        _activeParticles.Add(particle);

        WaveManager?.RegisterExtraParticle();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void SpawnSingle(float healthMultiplier, ParticleType type, bool offset = false)
    {
        var particle = _particleScene.Instantiate<Particle>();
        AddChild(particle);

        var path = new List<Vector2>(_cachedWorldPath!);

        if (offset && path.Count > 0)
        {
            // Slight random nudge on first waypoint so swarm units don't stack
            float ox = _rng.RandfRange(-GameConfig.TileSize * 0.4f, GameConfig.TileSize * 0.4f);
            float oy = _rng.RandfRange(-GameConfig.TileSize * 0.4f, GameConfig.TileSize * 0.4f);
            path[0] = path[0] + new Vector2(ox, oy);
        }

        particle.Initialize(path, healthMultiplier, type);
        HookParticleSignals(particle);
        _activeParticles.Add(particle);
    }

    private void HookParticleSignals(Particle particle)
    {
        particle.ReachedExit += () => OnParticleReachedExit(particle);
        particle.Died        += reward => OnParticleDied(particle, reward);
    }

    // ── Signal handlers ───────────────────────────────────────────────────────
    private void OnAirflowChanged(float _)
    {
        RefreshCachedPath();
    }

    private void OnParticleReachedExit(Particle particle)
    {
        SpawnFloatingText("-1", particle.GlobalPosition, Colors.Red);
        GameState?.LosePopulation(GameConfig.PopLostPerParticle);
        GameState?.RecordParticleEscaped();
        RemoveParticle(particle);
    }

    private void OnParticleDied(Particle particle, int reward)
    {
        // CellDivision: spawn 2 children if not already a child
        if (particle.Type == ParticleType.CellDivision && !particle.IsDivisionChild)
        {
            SpawnDivisionChild(particle.GlobalPosition);
            SpawnDivisionChild(particle.GlobalPosition);
        }

        SpawnFloatingText($"+{reward}", particle.GlobalPosition, Constants.Colors.HazardYellow);
        GameState?.AddCurrency(reward);
        RemoveParticle(particle);
    }

    private void SpawnFloatingText(string text, Vector2 worldPos, Color color)
    {
        var ft = new FloatingText();
        ft.Initialize(text, color);
        AddChild(ft);
        ft.GlobalPosition = worldPos + new Vector2(0f, -8f);
    }

    private void RemoveParticle(Particle particle)
    {
        _activeParticles.Remove(particle);
        particle.QueueFree();
        WaveManager?.OnParticleRemoved();
    }
}
