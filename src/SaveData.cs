using System.Collections.Generic;

namespace BioFilter;

public class TileSaveEntry  { public int X { get; set; } public int Y { get; set; } public int Type { get; set; } }
public class TowerSaveEntry { public int X { get; set; } public int Y { get; set; } public int Type { get; set; } }

public class SaveData
{
    public int Wave       { get; set; }
    public int Lives      { get; set; }
    public int Currency   { get; set; }
    public int MapNumber  { get; set; }
    public List<TileSaveEntry>  Tiles  { get; set; } = new();
    public List<TowerSaveEntry> Towers { get; set; } = new();
}
