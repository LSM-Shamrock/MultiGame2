using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_Lobby : MonoBehaviour
{
    [ChildField] public Button PlayButton;
    [ChildField] public Button CreateButton;
    [ChildField] public Button JoinButton;
    [ChildField] public Button SettingButton;
    [ChildField] public TMP_InputField LobbyIdInput;
    [ChildField] public TMP_InputField PlayerNameInput;
    [ChildField] public TextMeshProUGUI DeckInvalidText;
    [AssetField("Bgm_Lobby")] public AudioClip Bgm;

    private bool _isDeckValide = true;

    private void Start()
    {
        PlayerNameInput.onValueChanged.AddListener(OnPlayerNameInputChanged);
        CreateButton.onClick.AddListener(OnClick_CreateButton);
        JoinButton.onClick.AddListener(OnClick_JoinButton);
        PlayButton.onClick.AddListener(OnClick_AutoMatching);
        
        SettingButton.onClick.AddListener(OnClick_SettingButton);

        if (GameManager.Instance)
        {
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged += OnDeckCardIdsChanged;
        }

        SoundManager.Instance.PlayBgm(Bgm);
    }
    private void OnDestroy()
    {
        CreateButton.onClick.RemoveAllListeners();
        JoinButton.onClick.RemoveAllListeners();
        PlayButton.onClick.RemoveAllListeners();
        PlayerNameInput.onValueChanged.RemoveAllListeners();

        SettingButton.onClick.RemoveAllListeners();

        if (GameManager.Instance)
        {
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged -= OnDeckCardIdsChanged;
        }
    }
    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(LobbyIdInput.text) && _isDeckValide;
    }

    private void OnPlayerNameInputChanged(string value)
    {
        GameManager.Instance.PlayerName = value;
    }
    private void OnDeckCardIdsChanged(IReadOnlyList<int> deckCardIds)
    {
        bool valid = true;
        foreach (int cardId in deckCardIds)
        {
            if (cardId == -1)
            {
                valid = false;
                break;
            }
        }
        _isDeckValide = valid;

        PlayButton.interactable = _isDeckValide;
        CreateButton.interactable = _isDeckValide;
        JoinButton.interactable = _isDeckValide;
        DeckInvalidText.gameObject.SetActive(!_isDeckValide);
    }

    private async void OnClick_CreateButton()
    {
        await GameManager.Instance.CreateLobbyIdAsync();
    }
    private async void OnClick_JoinButton()
    {
        await GameManager.Instance.JoinWithLobbyIdAsync(LobbyIdInput.text);
    }
    private async void OnClick_AutoMatching()
    {
        await GameManager.Instance.AutoMatchingAsync();
    }

    private void OnClick_SettingButton()
    {
        PopupManager.Instance.ShowPopup<UI_SettingPopup>();
    }
}

