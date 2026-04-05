using Godot;

namespace BioFilter;

/// <summary>HUD label showing current population.</summary>
public partial class LivesMeter : Label
{
    public override void _Ready()
    {
        UpdateDisplay(GameConfig.StartingPopulation);
    }

    public void UpdatePopulation(int population)
    {
        UpdateDisplay(population);
    }

    private void UpdateDisplay(int population)
    {
        Text = $"Pop: {population}";
    }
}
