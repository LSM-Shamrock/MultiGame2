using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public abstract class SaveData
{
    [JsonIgnore] public virtual string FileName => $"{GetType().Name}.json";
    [JsonIgnore] public virtual string FolderPath => Application.persistentDataPath;
    [JsonIgnore] public string SavePath => Path.Combine(FolderPath, FileName);

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(SavePath, json);
        Debug.Log($"파일 저장됨.\n위치: {SavePath}\n내용:\n{json}");
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public void Load()
    {
        string json = File.ReadAllText(SavePath);
        JsonConvert.PopulateObject(json, this);
        Debug.Log($"파일 불러옴.\n위치: {SavePath}\n내용:\n{json}");
    }

    public bool TryLoad()
    {
        if (!HasSaveFile())
            return false;

        Load();
        return true;
    }
}