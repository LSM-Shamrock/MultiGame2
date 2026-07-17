using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

[Serializable]
public class LocalSaveData
{
    public float BgmVolume = 0.5f;
    public float SfxVolume = 0.5f;
    public int[] Deck;
}

public class SaveManager : SingletonBehaviour<SaveManager>
{
    private const string FILE_NAME = "LocalSavedata.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    public ObservableValue<LocalSaveData> SaveData;

    public void Save()
    {
        string json = JsonConvert.SerializeObject(SaveData, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            SaveData.Value = new LocalSaveData();
            Save();
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData.Value = JsonConvert.DeserializeObject<LocalSaveData>(json);
    }
}
