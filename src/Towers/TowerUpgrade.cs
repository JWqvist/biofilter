using BioFilter.Towers;
using Godot;

namespace BioFilter;

/// <summary>
/// Tower upgrade system — one upgrade tier per tower type.
/// Checks player currency, deducts cost, applies stat multipliers from GameConfig.
/// </summary>
public static class TowerUpgrade
{
    /// <summary>
    /// Attempt to upgrade the given tower. Returns true if successful.
    /// Deducts currency from GameState. Reports failure reason if insufficient funds.
    /// </summary>
    public static bool UpgradeTower(TowerBase tower, GameState gameState)
    {
        if (tower == null || gameState == null) return false;
        if (tower.IsUpgraded)
        {
            GD.Print("TowerUpgrade: tower already at max tier");
            return false;
        }

        int upgradeCost = UpgradeCost(tower);
        if (!gameState.SpendCurrency(upgradeCost))
        {
            GD.Print($"TowerUpgrade: insufficient currency (need {upgradeCost})");
            return false;
        }

        ApplyUpgrade(tower);
        GD.Print($"TowerUpgrade: upgraded {tower.GetType().Name} for ${upgradeCost}");
        return true;
    }

    /// <summary>Returns the upgrade cost for a given tower (base cost * multiplier).</summary>
    public static int UpgradeCost(TowerBase tower)
    {
        return (int)(tower.Cost * GameConfig.UpgradeCostMultiplier);
    }

    // ── Apply stat multipliers based on tower type ────────────────────────────

    /// <summary>Applies upgrade stats without deducting currency. Used by the load system.</summary>
    public static void ApplyUpgrade(TowerBase tower)
    {
        switch (tower)
        {
            case BasicFilter bf:
                bf.UpgradedDamage = GameConfig.BasicFilterDamage * GameConfig.UpgradeDamageMultiplier;
                bf.UpgradedRange = GameConfig.BasicFilterRange * GameConfig.UpgradeRangeMultiplier;
                bf.UpgradedTickRate = GameConfig.BasicFilterTickRate / GameConfig.UpgradeFireRateMultiplier;
                break;

            case Electrostatic es:
                es.UpgradedSlowPercent = GameConfig.ElectrostaticSlowPercent * GameConfig.UpgradeSlowMultiplier;
                es.UpgradedRange = GameConfig.ElectrostaticRange * GameConfig.UpgradeRangeMultiplier;
                break;

            case UVSteriliser uv:
                uv.UpgradedDamage = GameConfig.UVSteriliserDamage * GameConfig.UpgradeDamageMultiplier;
                uv.UpgradedRange = GameConfig.UVSteriliserRange * GameConfig.UpgradeRangeMultiplier;
                uv.UpgradedFireRate = GameConfig.UVSteriliserFireRate * GameConfig.UpgradeFireRateMultiplier;
                break;
        }
        tower.IsUpgraded = true;
        tower.QueueRedraw(); // repaint with upgraded color tint
    }
}
