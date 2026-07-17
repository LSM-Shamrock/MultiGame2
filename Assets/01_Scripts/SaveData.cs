using Newtonsoft.Json;
using System.IO;

public abstract class SaveData
{
    [JsonIgnore]
    public abstract string SavePath { get; }

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public void Load()
    {
        string json = File.ReadAllText(SavePath);
        JsonConvert.PopulateObject(json, this);
    }

    public bool TryLoad()
    {
        if (!HasSaveFile())
            return false;

        Load();
        return true;
    }
}