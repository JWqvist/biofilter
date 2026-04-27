using Godot;
using System;
using System.Text.Json;

namespace BioFilter;

public static class SaveManager
{
    private const string SavePath = "user://savegame.json";

    public static bool HasSave => FileAccess.FileExists(SavePath);

    /// <summary>Set true before loading Main.tscn to trigger save restoration in Main._Ready.</summary>
    public static bool PendingLoad { get; set; } = false;

    public static void Save(SaveData data)
    {
        string json = JsonSerializer.Serialize(data);
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PrintErr($"SaveManager: cannot open {SavePath} for writing");
            return;
        }
        f.StoreString(json);
        GD.Print($"SaveManager: saved (wave {data.WaveIndex}, ${data.Currency}, pop {data.Population})");
    }

    public static SaveData? Load()
    {
        if (!FileAccess.FileExists(SavePath)) return null;
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (f == null) return null;
        try
        {
            return JsonSerializer.Deserialize<SaveData>(f.GetAsText());
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SaveManager: failed to deserialize save — {ex.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (!FileAccess.FileExists(SavePath)) return;
        var dir = DirAccess.Open("user://");
        dir?.Remove("savegame.json");
        GD.Print("SaveManager: save deleted");
    }
}
