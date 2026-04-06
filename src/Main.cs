using BioFilter;
using BioFilter.Effects;
using BioFilter.UI;
using Godot;

public partial class Main : Node
{
    private GridManager    _gridManager     = null!;
    private BioFilter.AirflowMeter _airflowMeter = null!;
    private GameState      _gameState       = null!;
    private ParticleManager _particleManager = null!;
    private BioFilter.LivesMeter _livesMeter = null!;
    private TowerManager   _towerManager    = null!;
    private CurrencyMeter  _currencyMeter   = null!;
    private BuildMenu      _buildMenu       = null!;
    private WaveManager    _waveManager     = null!;
    private WaveHUD        _waveHUD         = null!;
    private GameOver       _gameOverScreen  = null!;
    private WinScreen      _winScreen       = null!;
    private PauseMenu      _pauseMenu       = null!;
    private Timer          _statusTimer     = null!;
    private BottomBarWidget _bottomBarWidget = null!;

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

        _gridManager      = GetNode<GridManager>("VBoxContainer/GameArea/GridManager");
        _airflowMeter     = GetNode<BioFilter.AirflowMeter>("VBoxContainer/TopBar/AirflowMeter");
        _gameState        = GetNode<GameState>("GameState");
        _particleManager  = GetNode<ParticleManager>("VBoxContainer/GameArea/ParticleManager");
        _livesMeter       = GetNode<BioFilter.LivesMeter>("VBoxContainer/TopBar/LivesMeter");
        _towerManager     = GetNode<TowerManager>("VBoxContainer/GameArea/TowerManager");
        _currencyMeter    = GetNode<CurrencyMeter>("VBoxContainer/TopBar/CurrencyMeter");
        _buildMenu        = GetNode<BuildMenu>("BuildMenu");
        _waveManager      = GetNode<WaveManager>("VBoxContainer/GameArea/WaveManager");
        _waveHUD          = GetNode<WaveHUD>("VBoxContainer/TopBar/WaveHUD");
        _gameOverScreen   = GetNode<GameOver>("GameOver");
        _winScreen        = GetNode<WinScreen>("WinScreen");
        _pauseMenu        = GetNode<PauseMenu>("PauseMenu");
        _bottomBarWidget  = GetNode<BottomBarWidget>("VBoxContainer/BottomBar/BottomBarWidget");

        // ── Sprint 9: Create new UI nodes ─────────────────────────────────────
        _wavePreview = new WavePreview();
        AddChild(_wavePreview);

        _bonusNotification = new BonusNotification();
        AddChild(_bonusNotification);

        _airflowVignette = new AirflowVignette();
        AddChild(_airflowVignette);

        // Wire BottomBarWidget phase + signals
        _bottomBarWidget.Initialize(_waveManager);
        _bottomBarWidget.BuildPressed     += () => _buildMenu.Toggle();
        _bottomBarWidget.StartWavePressed += () => _waveManager.StartWave();
        _bottomBarWidget.SpeedToggled     += (_newSpeed) => { /* speed applied inside BottomBarWidget */ };

        // Status timer — auto-clears status text after a few seconds
        _statusTimer = new Timer();
        _statusTimer.OneShot = true;
        _statusTimer.WaitTime = 2.0;
        _statusTimer.Timeout += () => _bottomBarWidget.SetStatus("Wall mode");
        AddChild(_statusTimer);

        // Wire airflow signal to HUD meter and vignette
        _gridManager.AirflowChanged += _airflowMeter.UpdateAirflow;
        _gridManager.AirflowChanged += _airflowVignette.UpdateAirflow;
        _gridManager.AirflowChanged += (af) => _gameState.RecordAirflow(af);

        // Wire GameState to HUD
        _gameState.PopulationChanged += _livesMeter.UpdatePopulation;
        _gameState.CurrencyChanged   += _currencyMeter.UpdateCurrency;
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
            _bottomBarWidget.SetStatus(refundAmount > 0
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
            _bottomBarWidget.SetStatus(TowerName(towerType));
        };
        _buildMenu.TowerDeselected += () =>
        {
            _towerManager.OnTowerDeselected();
            _gridManager.WallPlacementActive = true;
            _bottomBarWidget.SetStatus("Wall mode");
        };
        _buildMenu.UpgradeRequested += _towerManager.OnUpgradeRequested;

        // Wire TowerManager upgrade signals back to BuildMenu
        _towerManager.TowerClicked    += _buildMenu.ShowUpgradeButton;
        _towerManager.TowerDeselected += _buildMenu.HideUpgradeButton;

        // Wire WaveManager (Sprint 9: inject WavePreview and GameState)
        _waveManager.ParticleManagerRef = _particleManager;
        _waveManager.WavePreviewRef     = _wavePreview;
        _waveManager.GameStateRef       = _gameState;
        _waveManager.WaveStarted        += _waveHUD.OnWaveStarted;
        _waveManager.WaveComplete       += _waveHUD.OnWaveComplete;
        _waveManager.WaveComplete       += (_) => _gameState.RecordWaveSurvived();
        _waveManager.GameWon            += OnGameWon;

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);
        _livesMeter.UpdatePopulation(_gameState.Population);
        _currencyMeter.UpdateCurrency(_gameState.Currency);

        // ── Sprint 10: VFX nodes ──────────────────────────────────────────────
        _airflowVisualizer = new AirflowVisualizer();
        var gameArea = GetNode<Control>("VBoxContainer/GameArea");
        gameArea.AddChild(_airflowVisualizer);

        // Set GameArea offset so GridManager can convert mouse coords correctly
        // Defer to next frame so layout is calculated
        CallDeferred(nameof(SetGameAreaOffset));

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

        GD.Print("BioFilter initialized.");
    }

    private void OnGameOver()
    {
        GD.Print("GAME OVER — population reached zero.");
        _bottomBarWidget.ResetSpeed();
        _gameOverScreen.Show(_gameState.Population);
        GetTree().Paused = true;
        _gameOverScreen.ProcessMode = ProcessModeEnum.Always;
    }

    private void OnGameWon()
    {
        GD.Print("GAME WON — all waves survived!");
        _bottomBarWidget.ResetSpeed();
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
        _bottomBarWidget.SetStatus($"{type} selected [hotkey]");
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void DeselectTower()
    {
        _towerManager.OnTowerDeselected();
        _gridManager.WallPlacementActive = true;
        _bottomBarWidget.SetStatus("Wall mode");
    }

    private void TogglePauseMenu() => _pauseMenu.Toggle();

    private void SetGameAreaOffset()
    {
        var gameArea = GetNode<Control>("VBoxContainer/GameArea");
        _gridManager.GameAreaOffset = gameArea.GlobalPosition;
    }
}
