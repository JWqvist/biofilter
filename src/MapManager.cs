using Godot;
using System.Collections.Generic;

namespace BioFilter;

/// <summary>
/// Simple map selection registry.
/// Set CurrentMap before loading Main.tscn to choose which map is played.
/// 0 = custom user map (see CustomMap), 1 = Map 1, 2 = Map 2.
/// </summary>
public static class MapManager
{
    /// <summary>0 = custom, 1 = classic single spawn, 2 = dual spawn (top + bottom left).</summary>
    public static int CurrentMap { get; set; } = 1;

    /// <summary>Set when CurrentMap == 0 to define the active custom map.</summary>
    public static CustomMapData? CustomMap { get; set; } = null;

    public class CustomMapData
    {
        public string Name = "custom";
        public TileType[,] Grid = new TileType[GameConfig.GridWidth, GameConfig.GridHeight];
        public List<Vector2I> SpawnPoints = new();
    }

    /// <summary>
    /// Loads a user map from user://user_maps/{name}.json into a CustomMapData instance.
    /// Returns null if the file doesn't exist or fails to parse.
    /// </summary>
    public static CustomMapData? LoadFromJson(string name)
    {
        string path = $"user://user_maps/{name}.json";
        if (!FileAccess.FileExists(path)) return null;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return null;

        string json = file.GetAsText();
        var variant = Json.ParseString(json);
        if (variant.VariantType != Variant.Type.Dictionary) return null;
        var dict = variant.AsGodotDictionary();

        var data = new CustomMapData { Name = name };
        int w = dict.ContainsKey("width")  ? dict["width"].AsInt32()  : GameConfig.GridWidth;
        int h = dict.ContainsKey("height") ? dict["height"].AsInt32() : GameConfig.GridHeight;

        if (dict.ContainsKey("tiles"))
        {
            var tilesArr = dict["tiles"].AsGodotArray();
            int idx = 0;
            for (int r = 0; r < h && r < GameConfig.GridHeight; r++)
            {
                for (int c = 0; c < w && c < GameConfig.GridWidth; c++, idx++)
                {
                    if (idx >= tilesArr.Count) break;
                    var tt = (TileType)tilesArr[idx].AsInt32();
                    data.Grid[c, r] = tt;
                    if (tt == TileType.Spawn)
                        data.SpawnPoints.Add(new Vector2I(c, r));
                }
            }
        }
        return data;
    }
}
