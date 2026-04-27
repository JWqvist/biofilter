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

    /// <summary>Emitted whenever a particle is killed by a tower (used by AudioManager).</summary>
    [Signal] public delegate void ParticleKilledEventHandler();

    private PackedScene _particleScene = null!;
    private readonly List<Particle> _activeParticles = new();

    // Cached paths: one per spawn point (recalculated on grid change)
    // Index 0 = primary / Map1 spawn; Index 1 = secondary (Map2 only)
    private readonly List<List<Vector2>?> _cachedWorldPaths = new();

    // Round-robin index for multi-spawn cycling
    private int _spawnRoundRobinIndex = 0;

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

    private static List<Vector2> TilePathToWorld(List<Vector2I> tilePath)
    {
        var world = new List<Vector2>(tilePath.Count);
        foreach (var tile in tilePath)
        {
            float cx = tile.X * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
            float cy = tile.Y * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
            world.Add(new Vector2(cx, cy));
        }
        return world;
    }

    private void RefreshCachedPath()
    {
        _cachedWorldPaths.Clear();
        if (GridManager == null) return;

        var tileGrid = GridManager.GetGrid();
        var spawnPoints = GridManager.SpawnPoints;

        if (spawnPoints.Count == 0)
        {
            // Fallback: use GameConfig spawn
            var fallbackTile = Pathfinder.FindPath(tileGrid, new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow));
            _cachedWorldPaths.Add(fallbackTile != null ? TilePathToWorld(fallbackTile) : null);
        }
        else
        {
            var tilePaths = Pathfinder.FindPathMultiSpawn(tileGrid, spawnPoints);
            foreach (var tp in tilePaths)
                _cachedWorldPaths.Add(tp != null ? TilePathToWorld(tp) : null);
        }

        foreach (var p in _activeParticles)
            RecalculateParticlePath(p);
    }

    /// <summary>Returns the first non-null cached world path (used for CellDivision children).</summary>
    private List<Vector2>? PrimaryWorldPath()
    {
        foreach (var p in _cachedWorldPaths)
            if (p != null) return p;
        return null;
    }

    private void RecalculateParticlePath(Particle p)
    {
        // Find best path across all cached paths by closest waypoint
        List<Vector2>? bestPath = null;
        int bestIdx = 0;
        float bestDist = float.MaxValue;

        foreach (var worldPath in _cachedWorldPaths)
        {
            if (worldPath == null || worldPath.Count == 0) continue;
            for (int i = 0; i < worldPath.Count; i++)
            {
                float d = p.Position.DistanceTo(worldPath[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx  = i;
                    bestPath = worldPath;
                }
            }
        }

        if (bestPath == null) return;
        var remaining = bestPath.GetRange(bestIdx, bestPath.Count - bestIdx);
        p.Reroute(remaining);
    }

    // ── Spawn API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn a particle of the given type.
    /// BacterialSwarm spawns 8 SwarmUnit particles instead of 1 BacterialSwarm.
    /// Returns the number of "alive" counters to add to WaveManager.
    /// </summary>
    public int SpawnParticle(float healthMultiplier = 1.0f, ParticleType type = ParticleType.BioParticle)
    {
        if (_cachedWorldPaths.Count == 0)
            RefreshCachedPath();

        var primary = PrimaryWorldPath();
        if (primary == null || primary.Count == 0)
        {
            GD.PrintErr("ParticleManager: No path available — cannot spawn particle.");
            return 0;
        }

        if (type == ParticleType.BacterialSwarm)
        {
            // Spawn 8 SwarmUnits — alternate between spawn points
            for (int i = 0; i < GameConfig.SwarmUnitCount; i++)
                SpawnSingleFromPath(GetNextPath(), healthMultiplier, ParticleType.SwarmUnit, offset: true);
            return GameConfig.SwarmUnitCount;
        }

        SpawnSingleFromPath(GetNextPath(), healthMultiplier, type);
        return 1;
    }

    /// <summary>Round-robin pick of the next valid world path.</summary>
    private List<Vector2> GetNextPath()
    {
        // Filter to non-null paths
        var valid = new List<List<Vector2>>();
        foreach (var p in _cachedWorldPaths)
            if (p != null && p.Count > 0) valid.Add(p);

        if (valid.Count == 0) return PrimaryWorldPath()!;

        var chosen = valid[_spawnRoundRobinIndex % valid.Count];
        _spawnRoundRobinIndex++;
        return chosen;
    }

    /// <summary>
    /// Spawns 3 BioParticle children at the given position (Carrier death payload).
    /// </summary>
    public void SpawnCarrierPayload(Vector2 pos, float healthMult = 0.5f)
    {
        if (_cachedWorldPaths.Count == 0)
            RefreshCachedPath();

        var primary = PrimaryWorldPath();
        if (primary == null || primary.Count == 0) return;

        // Find closest waypoint to spawn from
        int bestIdx  = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < primary.Count; i++)
        {
            float d = pos.DistanceTo(primary[i]);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }

        for (int i = 0; i < 3; i++)
        {
            var remainingPath = primary.GetRange(bestIdx, primary.Count - bestIdx);
            var spawnPath = new List<Vector2>(remainingPath);

            // Slight offset so they don't all stack
            float ox = _rng.RandfRange(-GameConfig.TileSize * 0.3f, GameConfig.TileSize * 0.3f);
            float oy = _rng.RandfRange(-GameConfig.TileSize * 0.3f, GameConfig.TileSize * 0.3f);
            if (spawnPath.Count > 0)
                spawnPath[0] = pos + new Vector2(ox, oy);

            var particle = _particleScene.Instantiate<Particle>();
            AddChild(particle);
            particle.Initialize(spawnPath, healthMult, ParticleType.BioParticle, isDivisionChild: true);
            HookParticleSignals(particle);
            _activeParticles.Add(particle);

            WaveManager?.RegisterExtraParticle();
        }
    }

    /// <summary>Spawn a CellDivision child at a specific world position with an optional spawn offset.</summary>
    public void SpawnDivisionChild(Vector2 worldPosition, float healthMultiplier = 1.0f, Vector2 spawnOffset = default)
    {
        if (_cachedWorldPaths.Count == 0)
            RefreshCachedPath();

        // Use the closest cached path to the death position
        List<Vector2>? bestCachedPath = null;
        int bestMatchIdx = 0;
        float bestDist = float.MaxValue;

        foreach (var worldPath in _cachedWorldPaths)
        {
            if (worldPath == null || worldPath.Count == 0) continue;
            for (int i = 0; i < worldPath.Count; i++)
            {
                float d = worldPosition.DistanceTo(worldPath[i]);
                if (d < bestDist) { bestDist = d; bestMatchIdx = i; bestCachedPath = worldPath; }
            }
        }

        if (bestCachedPath == null) return;

        Vector2 actualSpawn = worldPosition + spawnOffset;
        var remainingPath = bestCachedPath.GetRange(bestMatchIdx, bestCachedPath.Count - bestMatchIdx);

        var spawnPath = new List<Vector2>(remainingPath);
        if (spawnPath.Count > 0)
            spawnPath[0] = actualSpawn;

        var particle = _particleScene.Instantiate<Particle>();
        AddChild(particle);
        particle.Initialize(spawnPath, healthMultiplier, ParticleType.CellDivision, isDivisionChild: true);
        HookParticleSignals(particle);
        _activeParticles.Add(particle);

        WaveManager?.RegisterExtraParticle();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void SpawnSingleFromPath(List<Vector2> sourcePath, float healthMultiplier, ParticleType type, bool offset = false)
    {
        var particle = _particleScene.Instantiate<Particle>();
        AddChild(particle);

        var path = new List<Vector2>(sourcePath);

        if (offset && path.Count > 0)
        {
            // Larger random nudge so swarm units spread across ~2.4 tiles instead of clustering
            float ox = _rng.RandfRange(-GameConfig.TileSize * 1.2f, GameConfig.TileSize * 1.2f);
            float oy = _rng.RandfRange(-GameConfig.TileSize * 1.2f, GameConfig.TileSize * 1.2f);
            path[0] = path[0] + new Vector2(ox, oy);
        }

        particle.Initialize(path, healthMultiplier, type);
        HookParticleSignals(particle);
        _activeParticles.Add(particle);
    }

    /// <summary>
    /// Returns true if no active particle is within <paramref name="minDistPx"/> pixels of the primary spawn point.
    /// Used by WaveManager to enforce minimum spacing between spawns.
    /// </summary>
    public bool IsSpawnPointClear(float minDistPx)
    {
        var primary = PrimaryWorldPath();
        if (primary == null || primary.Count == 0) return true;
        Vector2 spawnPos = primary[0];
        foreach (var p in _activeParticles)
        {
            if (!IsInstanceValid(p)) continue;
            if (p.GlobalPosition.DistanceTo(spawnPos) < minDistPx)
                return false;
        }
        return true;
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
        // Carrier: release 3 mini BioParticles on death
        if (particle.Type == ParticleType.Carrier)
        {
            SpawnCarrierPayload(particle.GlobalPosition, 0.5f);
        }

        // CellDivision: spawn 2 children with offset positions + split flash
        if (particle.Type == ParticleType.CellDivision && !particle.IsDivisionChild)
        {
            SpawnDivisionChild(particle.GlobalPosition, spawnOffset: new Vector2(0f, -GameConfig.TileSize * 0.4f));
            SpawnDivisionChild(particle.GlobalPosition, spawnOffset: new Vector2(0f,  GameConfig.TileSize * 0.4f));

            // White split flash at death position
            var flash = DeathSplash.CreateSplitFlash();
            AddChild(flash);
            flash.GlobalPosition = particle.GlobalPosition;
        }

        SpawnFloatingText($"+{reward}", particle.GlobalPosition, Constants.Colors.HazardYellow);
        GameState?.AddCurrency(reward);
        GameState?.RecordParticleKilled();
        EmitSignal(SignalName.ParticleKilled);
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
