using BioFilter;
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

    public override void _Ready()
    {
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

        // Status timer — auto-clears status label after a few seconds
        _statusTimer = new Timer();
        _statusTimer.OneShot = true;
        _statusTimer.WaitTime = 2.0;
        _statusTimer.Timeout += () => _statusLabel.Text = "Wall mode";
        AddChild(_statusTimer);

        // Wire airflow signal to HUD meter
        _gridManager.AirflowChanged += _airflowMeter.UpdateAirflow;

        // Wire GameState to HUD
        _gameState.PopulationChanged += _livesMeter.UpdatePopulation;
        _gameState.CurrencyChanged   += _currencyMeter.UpdateCurrency;
        _gameState.GameOver          += OnGameOver;

        // Give ParticleManager its dependencies
        _particleManager.GridManager   = _gridManager;
        _particleManager.GameState     = _gameState;
        _particleManager.WaveManager   = _waveManager;
        _particleManager.ConnectSignals();

        // Wire TowerManager
        _towerManager.GridManagerRef     = _gridManager;
        _towerManager.GameStateRef       = _gameState;
        _towerManager.ParticleManagerRef = _particleManager;

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

        // Wire WaveManager
        _waveManager.ParticleManagerRef = _particleManager;
        _waveManager.WaveStarted        += _waveHUD.OnWaveStarted;
        _waveManager.WaveComplete       += _waveHUD.OnWaveComplete;
        _waveManager.GameWon            += OnGameWon;

        // Wire StartWaveButton
        _startWaveButton.Initialize(_waveManager);

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);
        _livesMeter.UpdatePopulation(_gameState.Population);
        _currencyMeter.UpdateCurrency(_gameState.Currency);

        GD.Print("BioFilter initialized.");
    }

    private void OnGameOver()
    {
        GD.Print("GAME OVER — population reached zero.");
        _gameOverScreen.Show(0);
        GetTree().Paused = true;
        _gameOverScreen.ProcessMode = ProcessModeEnum.Always;
    }

    private void OnGameWon()
    {
        GD.Print("GAME WON — all waves survived!");
        _winScreen.Show(GameConfig.TotalWaves);
        GetTree().Paused = true;
        _winScreen.ProcessMode = ProcessModeEnum.Always;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel")) // Escape key
            TogglePauseMenu();
    }

    private void TogglePauseMenu() => _pauseMenu.Toggle();
}
