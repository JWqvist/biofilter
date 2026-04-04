using BioFilter.UI;
using Godot;

public partial class Main : Node2D
{
    private GridManager _gridManager;
    private BioFilter.AirflowMeter _airflowMeter;
    private BioFilter.GameState _gameState;
    private BioFilter.ParticleManager _particleManager;
    private BioFilter.LivesMeter _livesMeter;
    private BioFilter.TowerManager _towerManager;
    private CurrencyMeter _currencyMeter;
    private BuildPanel _buildPanel;

    // Simple spawn timer for testing
    private float _spawnTimer = 0f;

    public override void _Ready()
    {
        _gridManager = GetNode<GridManager>("GridManager");
        _airflowMeter = GetNode<BioFilter.AirflowMeter>("HUD/AirflowMeter");
        _gameState = GetNode<BioFilter.GameState>("GameState");
        _particleManager = GetNode<BioFilter.ParticleManager>("ParticleManager");
        _livesMeter = GetNode<BioFilter.LivesMeter>("HUD/LivesMeter");
        _towerManager = GetNode<BioFilter.TowerManager>("TowerManager");
        _currencyMeter = GetNode<CurrencyMeter>("HUD/CurrencyMeter");
        _buildPanel = GetNode<BuildPanel>("BuildPanel");

        // Wire airflow signal to HUD meter
        _gridManager.AirflowChanged += _airflowMeter.UpdateAirflow;

        // Wire GameState to HUD
        _gameState.PopulationChanged += _livesMeter.UpdatePopulation;
        _gameState.CurrencyChanged += _currencyMeter.UpdateCurrency;
        _gameState.GameOver += OnGameOver;

        // Give ParticleManager its dependencies
        _particleManager.GridManager = _gridManager;
        _particleManager.GameState = _gameState;
        _particleManager.ConnectSignals();

        // Wire TowerManager
        _towerManager.GridManagerRef = _gridManager;
        _towerManager.GameStateRef = _gameState;
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

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);
        _livesMeter.UpdatePopulation(_gameState.Population);
        _currencyMeter.UpdateCurrency(_gameState.Currency);

        GD.Print("BioFilter initialized.");
    }

    public override void _Process(double delta)
    {
        // Auto-spawn particles every SpawnInterval seconds for testing
        _spawnTimer += (float)delta;
        if (_spawnTimer >= GameConfig.SpawnInterval)
        {
            _spawnTimer = 0f;
            _particleManager.SpawnParticle();
        }
    }

    private void OnGameOver()
    {
        GD.Print("GAME OVER — population reached zero.");
        SetProcess(false);
    }
}
