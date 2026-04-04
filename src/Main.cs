using Godot;

public partial class Main : Node2D
{
    private GridManager _gridManager;
    private BioFilter.AirflowMeter _airflowMeter;
    private BioFilter.GameState _gameState;
    private BioFilter.ParticleManager _particleManager;
    private BioFilter.LivesMeter _livesMeter;

    // Simple spawn timer for testing
    private float _spawnTimer = 0f;

    public override void _Ready()
    {
        _gridManager = GetNode<GridManager>("GridManager");
        _airflowMeter = GetNode<BioFilter.AirflowMeter>("HUD/AirflowMeter");
        _gameState = GetNode<BioFilter.GameState>("GameState");
        _particleManager = GetNode<BioFilter.ParticleManager>("ParticleManager");
        _livesMeter = GetNode<BioFilter.LivesMeter>("HUD/LivesMeter");

        // Wire airflow signal to HUD meter
        _gridManager.AirflowChanged += _airflowMeter.UpdateAirflow;

        // Wire GameState to HUD
        _gameState.PopulationChanged += _livesMeter.UpdatePopulation;
        _gameState.GameOver += OnGameOver;

        // Give ParticleManager its dependencies
        _particleManager.GridManager = _gridManager;
        _particleManager.GameState = _gameState;
        _particleManager.ConnectSignals();

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);
        _livesMeter.UpdatePopulation(_gameState.Population);

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
