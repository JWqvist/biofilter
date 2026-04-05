using Godot;
using BioFilter.UI;

namespace BioFilter;

/// <summary>
/// HUD: Population display — now delegates to PopulationWidget (pixel art).
/// Kept as a thin wrapper so Main.tscn node names don't need changing.
/// </summary>
public partial class LivesMeter : Control
{
    private PopulationWidget _widget = null!;

    public override void _Ready()
    {
        _widget = new PopulationWidget();
        AddChild(_widget);
        // Stretch widget to fill this control
        _widget.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _widget.UpdatePopulation(GameConfig.StartingPopulation);
    }

    public void UpdatePopulation(int population)
    {
        _widget?.UpdatePopulation(population);
    }
}
