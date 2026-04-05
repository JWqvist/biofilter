using Godot;

namespace BioFilter.UI;

/// <summary>
/// HUD label showing current currency.
/// Connects to GameState.CurrencyChanged signal.
/// </summary>
public partial class CurrencyMeter : Label
{
    public void UpdateCurrency(int amount)
    {
        Text = $"${amount}";
    }
}
