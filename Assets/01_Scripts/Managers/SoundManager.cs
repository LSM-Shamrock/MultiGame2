using Newtonsoft.Json;
using UnityEngine;

public class SoundOptionData : SaveData
{
    [JsonProperty] public float BgmVolume;
    [JsonProperty] public float SfxVolume;
}

[AutoInjectionTarget]
public class SoundManager : SingletonBehaviour<SoundManager>
{
    public ObservableValue<float> BgmVolume { get; } = new(0.5f, Mathf.Clamp01);
    public ObservableValue<float> SfxVolume { get; } = new(0.5f, Mathf.Clamp01);

    private SoundOptionData _soundOptionData = new();
    [SerializeField, ChildField("BgmPlayer")] private AudioSource _bgmPlayer;
    [SerializeField, ChildField("SfxPlayer")] private AudioSource _sfxPlayer;

    public override void Initialize()
    {
        base.Initialize();
    
        BgmVolume.OnValueChanged += OnBgmVolumeChanged;
        SfxVolume.OnValueChanged += OnSfxVolumeChanged;

        if (_soundOptionData.TryLoad() == false)
        {
            _soundOptionData.BgmVolume = BgmVolume.Value;
            _soundOptionData.SfxVolume = SfxVolume.Value;
            _soundOptionData.Save();
        }
        BgmVolume.Value = _soundOptionData.BgmVolume;
        SfxVolume.Value = _soundOptionData.SfxVolume;
    }

    private void OnBgmVolumeChanged(float value)
    {
        _bgmPlayer.volume = value;
        
        if (_soundOptionData.BgmVolume != value)
        {
            _soundOptionData.BgmVolume = value;
            _soundOptionData.Save();
        }
    }
    private void OnSfxVolumeChanged(float value)
    {
        _sfxPlayer.volume = value;

        if (_soundOptionData.SfxVolume != value)
        {
            _soundOptionData.SfxVolume = value;
            _soundOptionData.Save();
        }
    }

    public void PlayBgm(AudioClip clip)
    {
        _bgmPlayer.clip = clip;
        _bgmPlayer.Play();
    }
    public void StopBgm()
    {
        _bgmPlayer.Stop();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        _bgmPlayer.PlayOneShot(clip, volumeScale);
    }
}
