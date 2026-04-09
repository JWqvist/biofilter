using System.Collections.Generic;
using Godot;

namespace BioFilter.Effects;

/// <summary>
/// Airflow visualizer — spawns "air dot" particles that drift along the path
/// from spawn to exit. Speed and color reflect current airflow percentage.
/// Connect to GridManager.AirflowChanged to refresh.
/// </summary>
public partial class AirflowVisualizer : Node2D
{
    private const int DotCount = 8;
    private const float DotSize = 2f;

    // Base speed in pixels/sec at 100% airflow
    private const float BaseSpeed = 20f;

    private static readonly Color ColorNormal   = new Color(0.627f, 0.769f, 1.0f, 0.3f);  // #a0c4ff @ 30%
    private static readonly Color ColorWarning  = new Color(1.0f,   0.85f,  0.2f, 0.4f);  // yellow
    private static readonly Color ColorCritical = new Color(1.0f,   0.25f,  0.15f, 0.5f); // red

    private struct AirDot
    {
        public float PathT;     // 0..1 along the path
        public float Speed;     // individual speed variation
        public int   PathIndex; // which spawn path to follow
    }

    private List<AirDot> _dots = new();
    private List<Vector2> _path = new();
    private List<List<Vector2>> _paths = new();
    private float _airflow = 1.0f;
    private float _totalPathLength = 0f;

    /// <summary>Set multiple paths (one per spawn point).</summary>
    public void SetPaths(List<List<Vector2>> worldPaths)
    {
        if (worldPaths == null || worldPaths.Count == 0) return;
        // Store all paths; dots are distributed across all paths
        _paths = worldPaths;
        _path  = worldPaths[0]; // primary path for backwards compat
        _totalPathLength = 0f;
        foreach (var p in _path)
            _ = p; // just keep for compat
        // Recalculate length for primary
        for (int i = 1; i < _path.Count; i++)
            _totalPathLength += _path[i].DistanceTo(_path[i - 1]);
        // Reset dots
        for (int i = 0; i < _dots.Count; i++)
            _dots[i] = new AirDot { PathT = (float)i / _dots.Count, PathIndex = i % Mathf.Max(1, _paths.Count), Speed = _dots[i].Speed };
    }

    public void SetPath(List<Vector2> worldPath)
    {
        _path = worldPath;
        _totalPathLength = 0f;
        for (int i = 1; i < _path.Count; i++)
            _totalPathLength += _path[i].DistanceTo(_path[i - 1]);

        InitDots();
    }

    public void OnAirflowChanged(float airflow)
    {
        _airflow = airflow;
        QueueRedraw();
    }

    private void InitDots()
    {
        _dots.Clear();
        if (_path.Count < 2 || _totalPathLength <= 0f) return;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < DotCount; i++)
        {
            _dots.Add(new AirDot
            {
                PathT = rng.RandfRange(0f, 1f),
                Speed = rng.RandfRange(0.7f, 1.3f)
            });
        }
    }

    public override void _Process(double delta)
    {
        if (_path.Count < 2 || _totalPathLength <= 0f) return;

        float speedMultiplier;
        if (_airflow < GameConfig.AirflowCriticalThreshold)
            speedMultiplier = 0.1f;   // nearly stopped at critical
        else if (_airflow < GameConfig.AirflowWarnFlashThreshold)
            speedMultiplier = 0.3f;   // slow at warning
        else
            speedMultiplier = _airflow;

        float baseAdvance = BaseSpeed * speedMultiplier * (float)delta / _totalPathLength;

        for (int i = 0; i < _dots.Count; i++)
        {
            var dot = _dots[i];
            dot.PathT += baseAdvance * dot.Speed;
            if (dot.PathT > 1f) dot.PathT -= 1f;
            _dots[i] = dot;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_path.Count < 2) return;

        Color dotColor;
        if (_airflow < GameConfig.AirflowCriticalThreshold)
            dotColor = ColorCritical;
        else if (_airflow < GameConfig.AirflowWarnFlashThreshold)
            dotColor = ColorWarning;
        else
            dotColor = ColorNormal;

        foreach (var dot in _dots)
        {
            var activePath = (_paths.Count > dot.PathIndex && _paths[dot.PathIndex].Count > 1)
                ? _paths[dot.PathIndex]
                : _path;
            var pos = SamplePathList(activePath, dot.PathT);
            DrawRect(new Rect2(pos.X - DotSize * 0.5f, pos.Y - DotSize * 0.5f,
                               DotSize, DotSize), dotColor);
        }
    }

    private static Vector2 SamplePathList(List<Vector2> path, float t)
    {
        if (path.Count == 0) return Vector2.Zero;
        if (path.Count == 1) return path[0];
        float totalLen = 0f;
        for (int i = 1; i < path.Count; i++) totalLen += path[i].DistanceTo(path[i-1]);
        float target = Mathf.Clamp(t, 0f, 1f) * totalLen;
        float walked = 0f;
        for (int i = 1; i < path.Count; i++) {
            float segLen = path[i].DistanceTo(path[i-1]);
            if (walked + segLen >= target) {
                float localT = (target - walked) / segLen;
                return path[i-1].Lerp(path[i], localT);
            }
            walked += segLen;
        }
        return path[path.Count - 1];
    }

    private Vector2 SamplePath(float t)
    {
        if (_path.Count == 0) return Vector2.Zero;
        if (_path.Count == 1) return _path[0];

        float targetDist = t * _totalPathLength;
        float accumulated = 0f;

        for (int i = 1; i < _path.Count; i++)
        {
            float segLen = _path[i].DistanceTo(_path[i - 1]);
            if (accumulated + segLen >= targetDist)
            {
                float localT = (targetDist - accumulated) / segLen;
                return _path[i - 1].Lerp(_path[i], localT);
            }
            accumulated += segLen;
        }

        return _path[_path.Count - 1];
    }
}
