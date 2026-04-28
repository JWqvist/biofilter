using System.Collections.Generic;

namespace BioFilter;

public class TileSaveEntry
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class TowerSaveEntry
{
    public int  X        { get; set; }
    public int  Y        { get; set; }
    public int  Type     { get; set; }   // TowerManager.TowerType cast to int
    public bool Upgraded { get; set; }
}

public class SaveData
{
    public int Version    { get; set; } = 1;
    public int MapNumber  { get; set; }
    public int Currency   { get; set; }
    public int Population { get; set; }
    public int WaveIndex  { get; set; }  // 0-indexed: equals WaveManager._currentWave
    public List<TileSaveEntry>  Walls  { get; set; } = new();   // only Wall tiles
    public List<TowerSaveEntry> Towers { get; set; } = new();
}
