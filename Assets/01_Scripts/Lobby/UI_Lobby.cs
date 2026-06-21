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
    [ChildField] public Button PvEButton;
    [ChildField] public Button SettingButton;
    [ChildField] public TMP_InputField LobbyIdInput;
    [ChildField] public TMP_InputField PlayerNameInput;
    [ChildField] public TextMeshProUGUI DeckInvalidText;
    [AssetField("Bgm_Lobby")] public AudioClip Bgm;

    private bool _isDeckValide = true;

    private void Start()
    {
        PlayButton.onClick.AddListener(OnClick_AutoMatching);
        CreateButton.onClick.AddListener(OnClick_Create);
        JoinButton.onClick.AddListener(OnClick_Join);
        PvEButton.onClick.AddListener(OnClick_PvE);
        
        PlayerNameInput.onValueChanged.AddListener(OnPlayerNameInputChanged);
        
        SettingButton.onClick.AddListener(OnClick_SettingButton);

        if (LobbyManager.Instance)
        {
            LobbyManager.Instance.CurrentDeckCardIds.OnAnyValueChanged += OnDeckCardIdsChanged;
        }

        SoundManager.Instance.PlayBgm(Bgm);
    }
    private void OnDestroy()
    {
        PlayButton.onClick.RemoveAllListeners();
        CreateButton.onClick.RemoveAllListeners();
        JoinButton.onClick.RemoveAllListeners();
        PvEButton.onClick.RemoveAllListeners();

        PlayerNameInput.onValueChanged.RemoveAllListeners();
        
        SettingButton.onClick.RemoveAllListeners();

        if (LobbyManager.Instance)
        {
            LobbyManager.Instance.CurrentDeckCardIds.OnAnyValueChanged -= OnDeckCardIdsChanged;
        }
    }
    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(LobbyIdInput.text) && _isDeckValide;
    }

    private void OnPlayerNameInputChanged(string value)
    {
        LobbyManager.Instance.PlayerName = value;
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

    private async void OnClick_Create()
    {
        await MatchingManager.Instance.CreateLobbyIdAsync();
    }
    private async void OnClick_Join()
    {
        await MatchingManager.Instance.JoinWithLobbyIdAsync(LobbyIdInput.text);
    }
    private async void OnClick_AutoMatching()
    {
        await MatchingManager.Instance.AutoMatchingAsync();
    }
    private async void OnClick_PvE()
    {
        await MatchingManager.Instance.PvEAsync();
    }

    private void OnClick_SettingButton()
    {
        PopupManager.Instance.ShowPopup<UI_SettingPopup>();
    }
}

