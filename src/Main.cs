using BioFilter;
using BioFilter.Effects;
using BioFilter.UI;
using Godot;

public partial class Main : Node
{
    private GridManager     _gridManager     = null!;
    private GameState       _gameState       = null!;
    private ParticleManager _particleManager = null!;
    private TowerManager    _towerManager    = null!;
    private BuildMenu       _buildMenu       = null!;
    private WaveManager     _waveManager     = null!;
    private GameOver        _gameOverScreen  = null!;
    private WinScreen       _winScreen       = null!;
    private PauseMenu       _pauseMenu       = null!;
    private Timer           _statusTimer     = null!;

    // New widescreen HUD nodes
    private TopStrip  _topStrip   = null!;
    private RightPanel _rightPanel = null!;

    // Sprint 9 UI nodes (created programmatically)
    private WavePreview       _wavePreview       = null!;
    private BonusNotification _bonusNotification = null!;
    private AirflowVignette   _airflowVignette   = null!;

    // Sprint 10 VFX nodes
    private AirflowVisualizer _airflowVisualizer = null!;
    private AmbientDust       _ambientDust       = null!;

    public override void _Ready()
    {
        // Force fullscreen on startup
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

        // New widescreen layout nodes
        _topStrip      = GetNode<TopStrip>("TopStrip");
        _rightPanel    = GetNode<RightPanel>("RightPanel");
        _gridManager   = GetNode<GridManager>("GameArea/GridManager");
        _gameState     = GetNode<GameState>("GameState");
        _particleManager = GetNode<ParticleManager>("GameArea/ParticleManager");
        _towerManager  = GetNode<TowerManager>("GameArea/TowerManager");
        _buildMenu     = GetNode<BuildMenu>("BuildMenu");
        _waveManager   = GetNode<WaveManager>("GameArea/WaveManager");
        _gameOverScreen = GetNode<GameOver>("GameOver");
        _winScreen     = GetNode<WinScreen>("WinScreen");
        _pauseMenu     = GetNode<PauseMenu>("PauseMenu");

        // ── Sprint 9: Create new UI nodes ─────────────────────────────────
        _wavePreview = new WavePreview();
        AddChild(_wavePreview);

        _bonusNotification = new BonusNotification();
        AddChild(_bonusNotification);

        _airflowVignette = new AirflowVignette();
        AddChild(_airflowVignette);

        // Wire RightPanel phase + signals
        _rightPanel.Initialize(_waveManager);
        _rightPanel.BuildPressed     += () => _buildMenu.Toggle();
        _rightPanel.StartWavePressed += () => _waveManager.StartWave();
        _rightPanel.SpeedToggled     += (_newSpeed) => { /* speed applied inside RightPanel */ };

        // Status timer — auto-clears status text after a few seconds
        _statusTimer = new Timer();
        _statusTimer.OneShot = true;
        _statusTimer.WaitTime = 2.0;
        _statusTimer.Timeout += () => _rightPanel.SetStatus(_waveManager.State == WaveManager.WaveState.Idle
            ? "Wall mode"
            : "WAVE ACTIVE");
        AddChild(_statusTimer);

        // Wire airflow signal to TopStrip and vignette
        _gridManager.AirflowChanged += _rightPanel.UpdateAirflow;
        _gridManager.AirflowChanged += _airflowVignette.UpdateAirflow;
        _gridManager.AirflowChanged += (af) => _gameState.RecordAirflow(af);

        // Wire GameState to RightPanel
        _gameState.PopulationChanged += _rightPanel.UpdatePopulation;
        _gameState.CurrencyChanged   += _rightPanel.UpdateCurrency;
        _gameState.GameOver          += OnGameOver;
        _gameState.BonusEarned       += (msg, _amount) => _bonusNotification.ShowBonus(msg);

        // Give ParticleManager its dependencies
        _particleManager.GridManager   = _gridManager;
        _particleManager.GameState     = _gameState;
        _particleManager.WaveManager   = _waveManager;
        _particleManager.ConnectSignals();

        // Wire TowerManager
        _towerManager.GridManagerRef     = _gridManager;
        _towerManager.GameStateRef       = _gameState;
        _towerManager.ParticleManagerRef = _particleManager;
        _towerManager.WaveManagerRef     = _waveManager;

        // Wire right-click refund: GridManager.TileRightClicked → TowerManager.RefundTile
        _gridManager.TileRightClicked += (col, row) =>
        {
            _towerManager.RefundTile(col, row);
        };

        // Wire refund status message
        _towerManager.TileRefunded += (refundAmount) =>
        {
            _rightPanel.SetStatus(refundAmount > 0
                ? $"Refunded: ${refundAmount}"
                : "Removed");
            _statusTimer.Stop();
            _statusTimer.Start();
        };

        // Wire BuildMenu signals to TowerManager / GridManager
        _buildMenu.TowerSelected += (towerType) =>
        {
            _towerManager.OnTowerSelected(towerType);
            _gridManager.WallPlacementActive = false;
            _rightPanel.SetStatus(TowerName(towerType));
        };
        _buildMenu.TowerDeselected += () =>
        {
            _towerManager.OnTowerDeselected();
            _gridManager.WallPlacementActive = true;
            _rightPanel.SetStatus("Wall mode");
        };
        _buildMenu.UpgradeRequested += _towerManager.OnUpgradeRequested;

        // Wire TowerManager upgrade signals back to BuildMenu
        _towerManager.TowerClicked    += _buildMenu.ShowUpgradeButton;
        _towerManager.TowerDeselected += _buildMenu.HideUpgradeButton;

        // Wire WaveManager
        _waveManager.ParticleManagerRef = _particleManager;
        _waveManager.WavePreviewRef     = _wavePreview;
        _waveManager.GameStateRef       = _gameState;
        _waveManager.WaveStarted        += _topStrip.OnWaveStarted;
        _waveManager.WaveComplete       += _topStrip.OnWaveComplete;
        _waveManager.WaveComplete       += (_) => _gameState.RecordWaveSurvived();
        _waveManager.GameWon            += OnGameWon;

        // Set initial display
        _rightPanel.UpdateAirflow(_gridManager.CurrentAirflow);
        _rightPanel.UpdatePopulation(_gameState.Population);
        _rightPanel.UpdateCurrency(_gameState.Currency);

        // ── Sprint 10: VFX nodes ──────────────────────────────────────────
        _airflowVisualizer = new AirflowVisualizer();
        var gameArea = GetNode<Control>("GameArea");
        gameArea.AddChild(_airflowVisualizer);

        // Set GameArea offset so GridManager can convert mouse coords correctly
        // Defer to next frame so layout is calculated
        CallDeferred(nameof(SetGameAreaOffset));
        CallDeferred(nameof(RefreshAirflow));

        _ambientDust = new AmbientDust();
        float gameW = GameConfig.GridWidth  * GameConfig.TileSize;
        float gameH = GameConfig.GridHeight * GameConfig.TileSize;
        _ambientDust.Initialize(gameW, gameH);
        gameArea.AddChild(_ambientDust);

        // Wire airflow signal to AirflowVisualizer
        _gridManager.AirflowChanged += _airflowVisualizer.OnAirflowChanged;

        // Give it the initial path
        var tilePath = BioFilter.Pathfinder.FindPath(
            _gridManager.GetGrid(),
            new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow));
        if (tilePath != null && tilePath.Count > 0)
        {
            var worldPath = new System.Collections.Generic.List<Vector2>();
            foreach (var tile in tilePath)
            {
                float cx = tile.X * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
                float cy = tile.Y * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
                worldPath.Add(new Vector2(cx, cy));
            }
            _airflowVisualizer.SetPath(worldPath);
        }

        // Refresh visualizer path when grid/airflow changes
        _gridManager.AirflowChanged += (_) =>
        {
            var tp = BioFilter.Pathfinder.FindPath(
                _gridManager.GetGrid(),
                new Vector2I(GameConfig.SpawnCol, GameConfig.SpawnRow));
            if (tp != null && tp.Count > 0)
            {
                var wp = new System.Collections.Generic.List<Vector2>();
                foreach (var t in tp)
                {
                    float cx2 = t.X * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
                    float cy2 = t.Y * GameConfig.TileSize + GameConfig.TileSize * 0.5f;
                    wp.Add(new Vector2(cx2, cy2));
                }
                _airflowVisualizer.SetPath(wp);
            }
        };

        GD.Print("BioFilter initialized (widescreen HUD).");
    }

    private void OnGameOver()
    {
        GD.Print("GAME OVER — population reached zero.");
        _rightPanel.ResetSpeed();
        _gameOverScreen.Show(_gameState.Population);
        GetTree().Paused = true;
        _gameOverScreen.ProcessMode = ProcessModeEnum.Always;
    }

    private void OnGameWon()
    {
        GD.Print("GAME WON — all waves survived!");
        _rightPanel.ResetSpeed();
        _winScreen.Show(GameConfig.TotalWaves);
        GetTree().Paused = true;
        _winScreen.ProcessMode = ProcessModeEnum.Always;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel")) // Escape key
        {
            TogglePauseMenu();
            return;
        }

        // Hotkeys: only active during build phase
        if (_waveManager.State == WaveManager.WaveState.Idle && !GetTree().Paused)
        {
            if (e is InputEventKey key && key.Pressed && !key.Echo)
            {
                switch (key.Keycode)
                {
                    case Key.Key1:
                        SelectTower(TowerManager.TowerType.BasicFilter, 0);
                        break;
                    case Key.Key2:
                        SelectTower(TowerManager.TowerType.Electrostatic, 1);
                        break;
                    case Key.Key3:
                        SelectTower(TowerManager.TowerType.UVSteriliser, 2);
                        break;
                    case Key.W:
                        DeselectTower();
                        break;
                    case Key.R:
                        DeselectTower();
                        break;
                }
            }
        }
    }

    private static string TowerName(int t) => t switch
    {
        0 => "Basic Filter",
        1 => "Electrostatic",
        2 => "UV Steriliser",
        _ => "Unknown"
    };

    private void SelectTower(TowerManager.TowerType type, int menuIndex)
    {
        _towerManager.OnTowerSelected(menuIndex);
        _gridManager.WallPlacementActive = false;
        _rightPanel.SetStatus($"{type} selected");
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void DeselectTower()
    {
        _towerManager.OnTowerDeselected();
        _gridManager.WallPlacementActive = true;
        _rightPanel.SetStatus("Wall mode");
    }

    private void TogglePauseMenu() => _pauseMenu.Toggle();

    private void SetGameAreaOffset()
    {
        var gameArea = GetNodeOrNull<Control>("GameArea");
        if (gameArea != null) _gridManager.GameAreaOffset = gameArea.GlobalPosition;
    }

    private void RefreshAirflow()
    {
        _gridManager.TriggerAirflowRefresh();
        _rightPanel.UpdateAirflow(_gridManager.CurrentAirflow);
    }
}
