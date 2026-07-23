using Newtonsoft.Json;
using System.IO;
using UnityEngine;


public interface ISaveData
{
    [JsonIgnore] 
    public string SavePath => $"{GetType().Name}.json";
    
    public string SerializeObject()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
    public void PopulateObject(string json)
    {
        JsonConvert.PopulateObject(json, this);
    }
}

public class SaveManager : SingletonBehaviour<SaveManager>
{
    public string SavePathRoot => Application.persistentDataPath;

    public string GetSavePath(ISaveData data)
    {
        return Path.Combine(SavePathRoot, data.SavePath);
    }
    public void Save(ISaveData data)
    {
        string json = data.SerializeObject();
        string path = GetSavePath(data);
        File.WriteAllText(path, json);
        Debug.Log($"파일 저장됨.\n위치: {path}\n내용:\n{json}");
    }
    public bool HasSaveFile(ISaveData data)
    {
        string path = GetSavePath(data);
        return File.Exists(path);
    }
    public void Load(ISaveData data)
    {
        string path = GetSavePath(data);
        string json = File.ReadAllText(path);
        data.PopulateObject(json);
        Debug.Log($"파일 불러옴.\n위치: {path}\n내용:\n{json}");
    }
    public bool TryLoad(ISaveData data)
    {
        if (!HasSaveFile(data))
            return false;

        Load(data);
        return true;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.E))
            System.Diagnostics.Process.Start($"\"{SavePathRoot}\"");
    }
}
