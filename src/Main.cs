using Godot;

public partial class Main : Node2D
{
    private GridManager _gridManager;

    public override void _Ready()
    {
        _gridManager = GetNode<GridManager>("GridManager");
        GD.Print("BioFilter initialized.");
    }

    public override void _Process(double delta)
    {
    }
}
