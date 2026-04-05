using BioFilter;
using BioFilter.UI;
using Godot;

public partial class Main : Node2D
{
    private GridManager _gridManager;
    private BioFilter.AirflowMeter _airflowMeter;
    private GameState _gameState;
    private ParticleManager _particleManager;
    private BioFilter.LivesMeter _livesMeter;
    private TowerManager _towerManager;
    private CurrencyMeter _currencyMeter;
    private BuildPanel _buildPanel;
    private WaveManager _waveManager;
    private WaveHUD _waveHUD;
    private StartWaveButton _startWaveButton;
    private GameOver _gameOverScreen;
    private WinScreen _winScreen;

    public override void _Ready()
    {
        _gridManager      = GetNode<GridManager>("GridManager");
        _airflowMeter     = GetNode<BioFilter.AirflowMeter>("HUD/TopBar/RightGroup/AirflowMeter");
        _gameState        = GetNode<GameState>("GameState");
        _particleManager  = GetNode<ParticleManager>("ParticleManager");
        _livesMeter       = GetNode<BioFilter.LivesMeter>("HUD/TopBar/LeftGroup/LivesMeter");
        _towerManager     = GetNode<TowerManager>("TowerManager");
        _currencyMeter    = GetNode<CurrencyMeter>("HUD/TopBar/LeftGroup/CurrencyMeter");
        _buildPanel       = GetNode<BuildPanel>("BuildPanel");
        _waveManager      = GetNode<WaveManager>("WaveManager");
        _waveHUD          = GetNode<WaveHUD>("HUD/TopBar/LeftGroup/WaveHUD");
        _startWaveButton  = GetNode<StartWaveButton>("BuildPanel/Panel/HBoxContainer/StartWaveButton");
        _gameOverScreen   = GetNode<GameOver>("GameOver");
        _winScreen        = GetNode<WinScreen>("WinScreen");

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

        // Wire BuildPanel to TowerManager and GridManager
        _buildPanel.TowerSelected += (towerType) =>
        {
            _towerManager.OnTowerSelected(towerType);
            _gridManager.WallPlacementActive = false;
        };
        _buildPanel.TowerDeselected += () =>
        {
            _towerManager.OnTowerDeselected();
            _gridManager.WallPlacementActive = true;
        };
        _buildPanel.UpgradeRequested += _towerManager.OnUpgradeRequested;

        // Wire TowerManager upgrade signals back to BuildPanel
        _towerManager.TowerClicked += _buildPanel.ShowUpgradeButton;
        _towerManager.TowerDeselected += _buildPanel.HideUpgradeButton;

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
        _gameOverScreen.ProcessMode = ProcessModeEnum.Always; // overlay stays responsive
    }

    private void OnGameWon()
    {
        GD.Print("GAME WON — all waves survived!");
        _winScreen.Show(GameConfig.TotalWaves);
        GetTree().Paused = true;
        _winScreen.ProcessMode = ProcessModeEnum.Always; // overlay stays responsive
    }
}
