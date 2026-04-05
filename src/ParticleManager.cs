using System.Collections.Generic;
using Godot;

namespace BioFilter;

/// <summary>
/// Spawns, tracks, and manages all active bio-particles.
/// Recalculates paths whenever the grid changes via GridManager.AirflowChanged.
/// </summary>
public partial class ParticleManager : Node2D
{
    // ── Scene reference injected from Main ───────────────────────────────────
    public GridManager GridManager { get; set; }
    public GameState GameState { get; set; }
    public WaveManager WaveManager { get; set; }

    private PackedScene _particleScene;
    private readonly List<Particle> _activeParticles = new();

    // Cached path (recalculated on grid change)
    private List<Vector2> _cachedWorldPath;

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
    private List<Vector2> BuildWorldPath()
    {
        if (GridManager == null) return null;

        var tileGrid = GridManager.GetGrid();
        var start = new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow);
        var tilePath = Pathfinder.FindPath(tileGrid, start);

        if (tilePath == null || tilePath.Count == 0) return null;

        // Convert tile coords → world positions (tile center)
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

        // Update all existing particles to new path from their nearest waypoint
        foreach (var p in _activeParticles)
            RecalculateParticlePath(p);
    }

    private void RecalculateParticlePath(Particle p)
    {
        if (_cachedWorldPath == null || _cachedWorldPath.Count == 0) return;

        // Find the closest waypoint ahead in the new path
        int bestIdx = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _cachedWorldPath.Count; i++)
        {
            float d = p.Position.DistanceTo(_cachedWorldPath[i]);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = i;
            }
        }

        // Slice path from bestIdx so particle continues forward
        var remainingPath = _cachedWorldPath.GetRange(bestIdx, _cachedWorldPath.Count - bestIdx);
        // Use Reroute so health and position are NOT reset
        p.Reroute(remainingPath);
    }

    // ── Spawn API ─────────────────────────────────────────────────────────────
    public void SpawnParticle(float healthMultiplier = 1.0f)
    {
        if (_cachedWorldPath == null)
            RefreshCachedPath();

        if (_cachedWorldPath == null || _cachedWorldPath.Count == 0)
        {
            GD.PrintErr("ParticleManager: No path available — cannot spawn particle.");
            return;
        }

        var particle = _particleScene.Instantiate<Particle>();
        AddChild(particle);

        particle.Initialize(new List<Vector2>(_cachedWorldPath), healthMultiplier);
        particle.ReachedExit += () => OnParticleReachedExit(particle);
        particle.Died += reward => OnParticleDied(particle, reward);
        _activeParticles.Add(particle);
    }

    // ── Signal handlers ───────────────────────────────────────────────────────
    private void OnAirflowChanged(float _)
    {
        RefreshCachedPath();
    }

    private void OnParticleReachedExit(Particle particle)
    {
        GameState?.LosePopulation(GameConfig.PopLostPerParticle);
        RemoveParticle(particle);
    }

    private void OnParticleDied(Particle particle, int reward)
    {
        GameState?.AddCurrency(reward);
        RemoveParticle(particle);
    }

    private void RemoveParticle(Particle particle)
    {
        _activeParticles.Remove(particle);
        particle.QueueFree();
        WaveManager?.OnParticleRemoved();
    }
}
