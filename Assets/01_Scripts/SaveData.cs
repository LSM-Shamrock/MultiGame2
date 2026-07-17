using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public abstract class SaveData
{
    public virtual string FileName => $"{GetType().Name}.json";
    public virtual string FolderPath => Application.persistentDataPath;
    public string SavePath => Path.Combine(FolderPath, FileName);

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