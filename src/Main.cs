using BioFilter;
using BioFilter.UI;
using Godot;

public partial class Main : Node2D
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

    public override void _Ready()
    {
        _gridManager      = GetNode<GridManager>("GridManager");
        _airflowMeter     = GetNode<BioFilter.AirflowMeter>("HUD/TopBar/RightGroup/AirflowMeter");
        _gameState        = GetNode<GameState>("GameState");
        _particleManager  = GetNode<ParticleManager>("ParticleManager");
        _livesMeter       = GetNode<BioFilter.LivesMeter>("HUD/TopBar/LeftGroup/LivesMeter");
        _towerManager     = GetNode<TowerManager>("TowerManager");
        _currencyMeter    = GetNode<CurrencyMeter>("HUD/TopBar/LeftGroup/CurrencyMeter");
        _buildMenu        = GetNode<BuildMenu>("BuildMenu");
        _buildButton      = GetNode<BuildButton>("HUD/BottomBar/BuildButton");
        _waveManager      = GetNode<WaveManager>("WaveManager");
        _waveHUD          = GetNode<WaveHUD>("HUD/TopBar/LeftGroup/WaveHUD");
        _startWaveButton  = GetNode<StartWaveButton>("HUD/BottomBar/StartWaveButton");
        _gameOverScreen   = GetNode<GameOver>("GameOver");
        _winScreen        = GetNode<WinScreen>("WinScreen");
        _pauseMenu        = GetNode<PauseMenu>("PauseMenu");

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
