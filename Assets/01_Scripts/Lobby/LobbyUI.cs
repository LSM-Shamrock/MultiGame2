using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : ProjectBehaviour
{
    [ChildField] public Button PlayButton;
    [ChildField] public Button CreateButton;
    [ChildField] public Button JoinButton;
    [ChildField] public TMP_InputField JoinCodeInput;
    [ChildField] public TMP_InputField PlayerNameInput;

    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(JoinCodeInput.text);
    }
}
