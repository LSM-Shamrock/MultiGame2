using UnityEngine;

[AutoInjectionTarget]
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance ?? (_instance = FindAnyObjectByType<SoundManager>());

    public ObservableValue<float> BgmVolume { get; } = new(1f, Mathf.Clamp01);
    public ObservableValue<float> SfxVolume { get; } = new(1f, Mathf.Clamp01);

    [SerializeField, ComponentField]
    private AudioSource _audioSource;

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
        _audioSource.volume = value;
    }
    private void OnSfxVolumeChanged(float value)
    {

    }

    public void PlayBgm(AudioClip clip)
    {
        _audioSource.clip = clip;
        _audioSource.Play();
    }
    public void StopBgm()
    {
        _audioSource.Stop();
    }
}
