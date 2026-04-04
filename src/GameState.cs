using Godot;

namespace BioFilter;

/// <summary>
/// Tracks population and currency.
/// Emits signals for HUD updates and game-over condition.
/// </summary>
public partial class GameState : Node
{
    public int Population { get; private set; } = GameConfig.StartingPopulation;
    public int Currency { get; private set; } = GameConfig.StartingCurrency;

    [Signal] public delegate void PopulationChangedEventHandler(int newValue);
    [Signal] public delegate void CurrencyChangedEventHandler(int newValue);
    [Signal] public delegate void GameOverEventHandler();

    public void LosePopulation(int amount)
    {
        Population -= amount;
        if (Population < 0) Population = 0;
        EmitSignal(SignalName.PopulationChanged, Population);
        if (Population <= 0)
            EmitSignal(SignalName.GameOver);
    }

    public void AddCurrency(int amount)
    {
        Currency += amount;
        EmitSignal(SignalName.CurrencyChanged, Currency);
    }

    /// <summary>Returns true and deducts if there's enough currency; false otherwise.</summary>
    public bool SpendCurrency(int amount)
    {
        if (Currency < amount) return false;
        Currency -= amount;
        EmitSignal(SignalName.CurrencyChanged, Currency);
        return true;
    }
}
