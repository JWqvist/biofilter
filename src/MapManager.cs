namespace BioFilter;

/// <summary>
/// Simple map selection registry.
/// Set CurrentMap before loading Main.tscn to choose which map is played.
/// </summary>
public static class MapManager
{
    /// <summary>1 = classic single spawn, 2 = dual spawn (top + bottom left).</summary>
    public static int CurrentMap { get; set; } = 1;
}
