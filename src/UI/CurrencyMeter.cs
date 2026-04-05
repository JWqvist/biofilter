using Godot;
using BioFilter.UI;

namespace BioFilter.UI;

/// <summary>
/// HUD: Currency display — now delegates to CurrencyWidget (pixel art).
/// </summary>
public partial class CurrencyMeter : Control
{
    private CurrencyWidget _widget = null!;

    public override void _Ready()
    {
        _widget = new CurrencyWidget();
        AddChild(_widget);
        _widget.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _widget.UpdateCurrency(GameConfig.StartingCurrency);
    }

    public void UpdateCurrency(int amount)
    {
        _widget?.UpdateCurrency(amount);
    }
}
