using System;
using BioFilter.Simulator;

namespace BioFilter.Tests;

/// <summary>
/// Runs the WaveSimulator with several tower configurations and prints
/// a formatted balance report to the console.
///
/// Headless — does not require Godot. Run via:
///   dotnet script src/Tests/SimulationReport.cs
/// or include in a standalone console project.
/// </summary>
public static class SimulationReport
{
    public static void Run()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          BioFilter — Pre-Player QA Balance Report        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── Scenario 1: Minimum viable defence (3 Basic Filters) ─────────────
        RunScenario(
            "Scenario 1: Minimal — 3 Basic Filters, 10 walls",
            numWalls: 10,
            towerTypes: new[]
            {
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.BasicFilter,
            });

        Console.WriteLine();

        // ── Scenario 2: Recommended mixed build ──────────────────────────────
        RunScenario(
            "Scenario 2: Balanced — 2 Basic + 1 Electrostatic + 1 UV, 20 walls",
            numWalls: 20,
            towerTypes: new[]
            {
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.Electrostatic,
                WaveSimulator.TowerType.UVSteriliser,
            });

        Console.WriteLine();

        // ── Scenario 3: Heavy mixed (6 towers) ───────────────────────────────
        RunScenario(
            "Scenario 3: Heavy — 3 Basic + 1 Electrostatic + 2 UV, 25 walls",
            numWalls: 25,
            towerTypes: new[]
            {
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.BasicFilter,
                WaveSimulator.TowerType.Electrostatic,
                WaveSimulator.TowerType.UVSteriliser,
                WaveSimulator.TowerType.UVSteriliser,
            });

        Console.WriteLine();

        // ── Scenario 4: Only UV spam ─────────────────────────────────────────
        RunScenario(
            "Scenario 4: UV Spam — 4 UV Sterilisers, 15 walls",
            numWalls: 15,
            towerTypes: new[]
            {
                WaveSimulator.TowerType.UVSteriliser,
                WaveSimulator.TowerType.UVSteriliser,
                WaveSimulator.TowerType.UVSteriliser,
                WaveSimulator.TowerType.UVSteriliser,
            });

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("END OF REPORT");
    }

    private static void RunScenario(string title, int numWalls, WaveSimulator.TowerType[] towerTypes)
    {
        Console.WriteLine($"┌─ {title}");
        Console.WriteLine("│");
        var sim = new WaveSimulator();
        sim.RunSimulation(towerTypes.Length, towerTypes, numWalls);
        Console.WriteLine("└───────────────────────────────────────────────────────────");
    }
}
