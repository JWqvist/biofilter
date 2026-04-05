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
    private BuildButton    _buildButton     = null!;
    private WaveManager    _waveManager     = null!;
    private WaveHUD        _waveHUD         = null!;
    private StartWaveButton _startWaveButton = null!;
    private GameOver       _gameOverScreen  = null!;
    private WinScreen      _winScreen       = null!;
    private PauseMenu      _pauseMenu       = null!;
    private Label          _statusLabel     = null!;
    private Timer          _statusTimer     = null!;

    // Sprint 9 UI nodes (created programmatically)
    private WavePreview       _wavePreview       = null!;
    private BonusNotification _bonusNotification = null!;
    private AirflowVignette   _airflowVignette   = null!;
    private SpeedButton       _speedButton       = null!;

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
        _buildButton      = GetNode<BuildButton>("VBoxContainer/BottomBar/BuildButton");
        _waveManager      = GetNode<WaveManager>("VBoxContainer/GameArea/WaveManager");
        _waveHUD          = GetNode<WaveHUD>("VBoxContainer/TopBar/WaveHUD");
        _startWaveButton  = GetNode<StartWaveButton>("VBoxContainer/BottomBar/StartWaveButton");
        _gameOverScreen   = GetNode<GameOver>("GameOver");
        _winScreen        = GetNode<WinScreen>("WinScreen");
        _pauseMenu        = GetNode<PauseMenu>("PauseMenu");
        _statusLabel      = GetNode<Label>("VBoxContainer/BottomBar/StatusLabel");

        // ── Sprint 9: Create new UI nodes ─────────────────────────────────────
        _wavePreview = new WavePreview();
        AddChild(_wavePreview);

        _bonusNotification = new BonusNotification();
        AddChild(_bonusNotification);

        _airflowVignette = new AirflowVignette();
        AddChild(_airflowVignette);

        // SpeedButton added to BottomBar before StartWaveButton
        _speedButton = new SpeedButton();
        _speedButton.CustomMinimumSize = new Vector2(60f, 0f);
        var bottomBar = GetNode<HBoxContainer>("VBoxContainer/BottomBar");
        // Insert before StartWaveButton (last child)
        bottomBar.AddChild(_speedButton);
        bottomBar.MoveChild(_speedButton, _startWaveButton.GetIndex());

        // Status timer — auto-clears status label after a few seconds
        _statusTimer = new Timer();
        _statusTimer.OneShot = true;
        _statusTimer.WaitTime = 2.0;
        _statusTimer.Timeout += () => _statusLabel.Text = "Wall mode";
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
            _statusLabel.Text = refundAmount > 0
                ? $"Refunded: ${refundAmount}"
                : "Removed";
            _statusTimer.Stop();
            _statusTimer.Start();
        };

        // Wire BuildButton → BuildMenu
        _buildButton.Initialize(_buildMenu);

        // Wire BuildMenu signals to TowerManager / GridManager
        _buildMenu.TowerSelected += (towerType) =>
        {
            _towerManager.OnTowerSelected(towerType);
            _gridManager.WallPlacementActive = false;
        };
        _buildMenu.TowerDeselected += () =>
        {
            _towerManager.OnTowerDeselected();
            _gridManager.WallPlacementActive = true;
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
        _waveManager.WaveComplete       += (_) => _speedButton.ResetSpeed();
        _waveManager.GameWon            += OnGameWon;

        // Wire StartWaveButton
        _startWaveButton.Initialize(_waveManager);

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);
        _livesMeter.UpdatePopulation(_gameState.Population);
        _currencyMeter.UpdateCurrency(_gameState.Currency);

        // ── Sprint 10: VFX nodes ──────────────────────────────────────────────
        _airflowVisualizer = new AirflowVisualizer();
        var gameArea = GetNode<Control>("VBoxContainer/GameArea");
        gameArea.AddChild(_airflowVisualizer);

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
        _speedButton.ResetSpeed();
        _gameOverScreen.Show(0);
        GetTree().Paused = true;
        _gameOverScreen.ProcessMode = ProcessModeEnum.Always;
    }

    private void OnGameWon()
    {
        GD.Print("GAME WON — all waves survived!");
        _speedButton.ResetSpeed();
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

    private void SelectTower(TowerManager.TowerType type, int menuIndex)
    {
        _towerManager.OnTowerSelected(menuIndex);
        _gridManager.WallPlacementActive = false;
        _statusLabel.Text = $"{type} selected [hotkey]";
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void DeselectTower()
    {
        _towerManager.OnTowerDeselected();
        _gridManager.WallPlacementActive = true;
        _statusLabel.Text = "Wall mode";
    }

    private void TogglePauseMenu() => _pauseMenu.Toggle();
}
