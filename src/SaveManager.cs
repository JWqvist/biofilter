using Godot;
using System.Text.Json;

namespace BioFilter;

public static class SaveManager
{
    private const string SavePath = "user://savegame.json";

    public static void Save(SaveData data)
    {
        string json = JsonSerializer.Serialize(data);
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        f.StoreString(json);
    }

    public static SaveData? Load()
    {
        if (!HasSave()) return null;
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        string json = f.GetAsText();
        return JsonSerializer.Deserialize<SaveData>(json);
    }

    public static bool HasSave() => FileAccess.FileExists(SavePath);

    public static void DeleteSave()
    {
        if (HasSave())
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
    }
}
