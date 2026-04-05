using System;
using System.Collections.Generic;

namespace BioFilter.Simulator;

/// <summary>
/// Headless wave simulator for balance tuning.
/// Runs a full 10-wave session without Godot rendering.
/// 
/// Usage (as a dotnet-script or standalone):
///   var sim = new WaveSimulator();
///   sim.RunSimulation(numTowers: 6,
///       towerTypes: new[] { TowerType.BasicFilter, TowerType.BasicFilter,
///                           TowerType.Electrostatic, TowerType.BasicFilter,
///                           TowerType.UVSteriliser, TowerType.BasicFilter },
///       numWalls: 20);
/// </summary>
public class WaveSimulator
{
    public enum TowerType { BasicFilter, Electrostatic, UVSteriliser }

    // ── Sim tower state ───────────────────────────────────────────────────────

    private class SimTower
    {
        public TowerType Type;
        public float Damage;
        public float Range;       // tiles
        public float TickRate;    // damage events per second (BasicFilter) or fire rate (UV)
        public float SlowPercent; // Electrostatic: 0-1
        public bool Upgraded;
    }

    // ── Result types ──────────────────────────────────────────────────────────

    public class WaveResult
    {
        public int Wave;
        public int ParticlesSpawned;
        public int ParticlesKilled;
        public int ParticlesEscaped;
        public float DifficultyScore; // 0=trivial, 1=perfect, 2+=death spiral
        public int CurrencyEarned;
        public int CurrencyBalance;
        public string Rating = string.Empty;
    }

    public class SimResult
    {
        public List<WaveResult> Waves = new();
        public int FinalCurrencyBalance;
        public string Summary = string.Empty;
    }

    // ── Corridor geometry constants ───────────────────────────────────────────

    // A single-tile-wide corridor has particles spending time = length / speed
    // We model corridor as 28 tiles long (cols 1-28, with spawn/exit at edges)
    private const int CorridorLength = 28; // tiles

    // ── Run simulation ────────────────────────────────────────────────────────

    public SimResult RunSimulation(int numTowers, TowerType[] towerTypes, int numWalls)
    {
        var result = new SimResult();
        int currency = GameConfig.StartingCurrency;

        // Build towers spread along the corridor
        var towers = BuildTowerSetup(towerTypes);

        // Subtract build costs
        foreach (var t in towers)
            currency -= GetBuildCost(t.Type);

        Console.WriteLine("=== BioFilter Wave Simulator ===");
        Console.WriteLine($"Setup: {towers.Count} towers, {numWalls} walls, starting currency: {currency}");
        Console.WriteLine();

        for (int wave = 1; wave <= GameConfig.TotalWaves; wave++)
        {
            var waveResult = SimulateWave(wave, towers, currency);
            result.Waves.Add(waveResult);
            currency = waveResult.CurrencyBalance;

            // Auto-upgrade most effective tower if we can afford it between waves 3-7
            if (wave >= 3 && wave <= 7)
                TryAutoUpgrade(towers, ref currency);

            PrintWaveResult(waveResult);
        }

        result.FinalCurrencyBalance = currency;
        result.Summary = BuildSummary(result.Waves);
        Console.WriteLine();
        Console.WriteLine("=== BALANCE SUMMARY ===");
        Console.WriteLine(result.Summary);

        return result;
    }

    // ── Wave simulation logic ─────────────────────────────────────────────────

    private WaveResult SimulateWave(int wave, List<SimTower> towers, int startCurrency)
    {
        int particleCount = GameConfig.WaveBaseParticleCount
            + (wave - 1) * GameConfig.WaveParticleCountIncrease;

        float healthMult = GameConfig.WaveHealthMultiplierBase
            + (wave - 1) * GameConfig.WaveHealthMultiplierIncrease;

        float particleHealth = GameConfig.ParticleBaseHealth * healthMult;
        float particleSpeed = GameConfig.ParticleBaseSpeed; // tiles per second

        // Calculate effective speed through corridor considering Electrostatic slows.
        // Electrostatic covers a range along the path — particles slow when in range.
        float slowMultiplier = CalcSlowMultiplier(towers);

        // Time each particle spends in the corridor at normal vs slowed speed
        // Assume electrostatic towers cover ~30% of corridor length
        float normalFraction = 0.7f;
        float slowedFraction = 0.3f;
        float timeInCorridor = (CorridorLength * normalFraction / particleSpeed)
            + (CorridorLength * slowedFraction / (particleSpeed * slowMultiplier));

        // Total damage dealt to each particle by all damage towers
        float totalDamagePerParticle = CalcDamagePerParticle(towers, timeInCorridor);

        // Particles killed vs escaped
        int killed = 0;
        int escaped = 0;

        for (int i = 0; i < particleCount; i++)
        {
            float remainingHealth = particleHealth - totalDamagePerParticle;
            if (remainingHealth <= 0)
                killed++;
            else
                escaped++;
        }

        int currencyEarned = killed * GameConfig.CurrencyPerKill;
        int newBalance = startCurrency + currencyEarned;

        // Difficulty score: escaped/spawned (0=great, 1=all escaped=game over risk)
        float diffScore = (float)escaped / particleCount;

        string rating = diffScore switch
        {
            < 0.05f => "✅ EASY",
            < 0.25f => "🟡 MEDIUM",
            < 0.60f => "🔶 HARD",
            _ => "🔴 DEATH SPIRAL"
        };

        return new WaveResult
        {
            Wave = wave,
            ParticlesSpawned = particleCount,
            ParticlesKilled = killed,
            ParticlesEscaped = escaped,
            DifficultyScore = diffScore,
            CurrencyEarned = currencyEarned,
            CurrencyBalance = newBalance,
            Rating = rating
        };
    }

    // ── Tower helpers ─────────────────────────────────────────────────────────

    private List<SimTower> BuildTowerSetup(TowerType[] types)
    {
        var towers = new List<SimTower>();
        foreach (var t in types)
        {
            towers.Add(new SimTower
            {
                Type = t,
                Damage = GetBaseDamage(t),
                Range = GetBaseRange(t),
                TickRate = GetTickRate(t),
                SlowPercent = t == TowerType.Electrostatic ? GameConfig.ElectrostaticSlowPercent : 0f,
                Upgraded = false
            });
        }
        return towers;
    }

    private float CalcDamagePerParticle(List<SimTower> towers, float timeInCorridor)
    {
        float total = 0f;
        foreach (var tower in towers)
        {
            if (tower.Type == TowerType.Electrostatic) continue; // no direct damage

            // Time particle spends within this tower's range
            // Range covers (range * 2) / corridorLength fraction of the path
            float coverageFraction = Math.Min(1f, (tower.Range * 2f) / CorridorLength);
            float timeInRange = timeInCorridor * coverageFraction;

            float dps = tower.Type switch
            {
                TowerType.BasicFilter => tower.Damage * tower.TickRate,
                TowerType.UVSteriliser => tower.Damage * tower.TickRate,
                _ => 0f
            };

            total += dps * timeInRange;
        }
        return total;
    }

    private float CalcSlowMultiplier(List<SimTower> towers)
    {
        float slowPercent = 0f;
        foreach (var t in towers)
            if (t.Type == TowerType.Electrostatic)
                slowPercent = Math.Max(slowPercent, t.SlowPercent);

        // In the actual game: particle speed is multiplied directly by SlowMultiplier.
        // Electrostatic sets p.SlowMultiplier = ActiveSlowPercent (0.5 = 50% speed).
        // So a slowPercent of 0.5 means the particle moves at 50% speed.
        float speedMultiplier = (slowPercent > 0f) ? slowPercent : 1.0f;
        return Math.Max(0.2f, speedMultiplier); // floor at 20% speed
    }

    private void TryAutoUpgrade(List<SimTower> towers, ref int currency)
    {
        foreach (var t in towers)
        {
            if (t.Upgraded) continue;
            int upgradeCost = (int)(GetBuildCost(t.Type) * GameConfig.UpgradeCostMultiplier);
            if (currency >= upgradeCost)
            {
                ApplyUpgrade(t);
                currency -= upgradeCost;
                Console.WriteLine($"  [Auto-upgrade] {t.Type} upgraded. Currency left: {currency}");
                return; // one upgrade per inter-wave phase
            }
        }
    }

    private void ApplyUpgrade(SimTower tower)
    {
        tower.Upgraded = true;
        tower.Damage *= GameConfig.UpgradeDamageMultiplier;
        tower.Range *= GameConfig.UpgradeRangeMultiplier;
        tower.TickRate *= GameConfig.UpgradeFireRateMultiplier;
        tower.SlowPercent *= GameConfig.UpgradeSlowMultiplier;
    }

    private int GetBuildCost(TowerType t) => t switch
    {
        TowerType.BasicFilter => GameConfig.BasicFilterCost,
        TowerType.Electrostatic => GameConfig.ElectrostaticCost,
        TowerType.UVSteriliser => GameConfig.UVSteriliserCost,
        _ => 0
    };

    private float GetBaseDamage(TowerType t) => t switch
    {
        TowerType.BasicFilter => GameConfig.BasicFilterDamage,
        TowerType.UVSteriliser => GameConfig.UVSteriliserDamage,
        _ => 0f
    };

    private float GetBaseRange(TowerType t) => t switch
    {
        TowerType.BasicFilter => GameConfig.BasicFilterRange,
        TowerType.Electrostatic => GameConfig.ElectrostaticRange,
        TowerType.UVSteriliser => GameConfig.UVSteriliserRange,
        _ => 2f
    };

    private float GetTickRate(TowerType t) => t switch
    {
        TowerType.BasicFilter => 1f / GameConfig.BasicFilterTickRate,
        TowerType.UVSteriliser => GameConfig.UVSteriliserFireRate,
        _ => 1f
    };

    // ── Reporting ─────────────────────────────────────────────────────────────

    private static void PrintWaveResult(WaveResult r)
    {
        Console.WriteLine($"Wave {r.Wave,2}: {r.ParticlesSpawned,3} spawned | "
            + $"{r.ParticlesKilled,3} killed | "
            + $"{r.ParticlesEscaped,3} escaped | "
            + $"+${r.CurrencyEarned} earned | "
            + $"balance ${r.CurrencyBalance} | "
            + $"{r.Rating}");
    }

    private static string BuildSummary(List<WaveResult> waves)
    {
        var lines = new List<string>();
        lines.Add("Target: W1-3 EASY, W4-6 MEDIUM, W7-9 HARD, W10 VERY HARD");
        lines.Add("");

        foreach (var w in waves)
        {
            string target = w.Wave switch
            {
                <= 3 => "EASY",
                <= 6 => "MEDIUM",
                <= 9 => "HARD",
                _ => "DEATH SPIRAL"
            };
            string actual = w.DifficultyScore < 0.05f ? "EASY"
                : w.DifficultyScore < 0.25f ? "MEDIUM"
                : w.DifficultyScore < 0.60f ? "HARD"
                : "DEATH SPIRAL";

            string match = actual == target ? "✓" : "✗";
            lines.Add($"  W{w.Wave}: target={target} actual={actual} escaped={w.ParticlesEscaped}/{w.ParticlesSpawned} {match}");
        }

        return string.Join("\n", lines);
    }
}
