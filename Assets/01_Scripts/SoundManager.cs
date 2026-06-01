using UnityEngine;

[AutoInjectionTarget]
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance ?? (_instance = FindAnyObjectByType<SoundManager>());

    public ObservableValue<float> BgmVolume { get; } = new(1f, Mathf.Clamp01);
    public ObservableValue<float> SfxVolume { get; } = new(1f, Mathf.Clamp01);

    [SerializeField, ChildField("BgmPlayer")] private AudioSource _bgmPlayer;
    [SerializeField, ChildField("SfxPlayer")] private AudioSource _sfxPlayer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        BgmVolume.OnValueChanged += OnBgmVolumeChanged;
        SfxVolume.OnValueChanged += OnSfxVolumeChanged;
    }

    private void OnBgmVolumeChanged(float value)
    {
        _bgmPlayer.volume = value;
    }
    private void OnSfxVolumeChanged(float value)
    {
        _sfxPlayer.volume = value;
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
