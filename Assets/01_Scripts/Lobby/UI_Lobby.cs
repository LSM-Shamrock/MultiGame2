using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_Lobby : MonoBehaviour
{
    [ChildField] public Button PlayButton;
    [ChildField] public Button PvEButton;
    [ChildField] public Button CreateButton;
    [ChildField] public Button JoinButton;
    [ChildField] public Button SettingButton;
    [ChildField] public TMP_InputField LobbyIdInput;
    [ChildField] public TMP_InputField PlayerNameInput;
    [ChildField] public TextMeshProUGUI DeckInvalidText;
    [AssetField("Bgm_Lobby")] public AudioClip Bgm;

    private bool _isDeckValid = true;
    private bool IsDeckValid
    {
        get => _isDeckValid;
        set
        {
            _isDeckValid = value;
            PlayButton.interactable = _isDeckValid;
            PvEButton.interactable = _isDeckValid;
            CreateButton.interactable = _isDeckValid;
            JoinButton.interactable = _isDeckValid;
            DeckInvalidText.gameObject.SetActive(!_isDeckValid);
        }
    }

    private void Start()
    {
        PlayButton.onClick.AddListener(OnClick_AutoMatching);
        PvEButton.onClick.AddListener(OnClick_PvE);
        CreateButton.onClick.AddListener(OnClick_Create);
        JoinButton.onClick.AddListener(OnClick_Join);
        
        PlayerNameInput.onValueChanged.AddListener(OnPlayerNameInputChanged);
        PlayerNameInput.text = LobbyManager.Instance.PlayerName.Value;

        SettingButton.onClick.AddListener(OnClick_SettingButton);

        if (LobbyManager.Instance)
            LobbyManager.Instance.DeckCardIds.OnValueChanged += OnDeckCardIdChanged;
        
        SoundManager.Instance.PlayBgm(Bgm);
    }
    private void OnDestroy()
    {
        PlayButton.onClick.RemoveAllListeners();
        PvEButton.onClick.RemoveAllListeners();
        CreateButton.onClick.RemoveAllListeners();
        JoinButton.onClick.RemoveAllListeners();

        PlayerNameInput.onValueChanged.RemoveAllListeners();
        
        SettingButton.onClick.RemoveAllListeners();

        if (LobbyManager.Instance)
            LobbyManager.Instance.DeckCardIds.OnValueChanged -= OnDeckCardIdChanged;
    }
    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(LobbyIdInput.text) && IsDeckValid;
    }

    private void OnPlayerNameInputChanged(string value)
    {
        LobbyManager.Instance.PlayerName.Value = value;
    }

    private void OnDeckCardIdChanged(int index, int deckCardId)
    {
        OnDeckCardIdsChanged(LobbyManager.Instance.DeckCardIds.Values);
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
        IsDeckValid = valid;
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

