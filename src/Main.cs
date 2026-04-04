using Godot;

public partial class Main : Node2D
{
    private GridManager _gridManager;
    private BioFilter.AirflowMeter _airflowMeter;

    public override void _Ready()
    {
        _gridManager = GetNode<GridManager>("GridManager");
        _airflowMeter = GetNode<BioFilter.AirflowMeter>("HUD/AirflowMeter");

        // Wire airflow signal to HUD meter
        _gridManager.AirflowChanged += _airflowMeter.UpdateAirflow;

        // Set initial display
        _airflowMeter.UpdateAirflow(_gridManager.CurrentAirflow);

        GD.Print("BioFilter initialized.");
    }

    public override void _Process(double delta)
    {
    }
}
