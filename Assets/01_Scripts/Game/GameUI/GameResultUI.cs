using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class GameResultUI : MonoBehaviour
{
    [SerializeField, ChildField("MainPanel")] private GameObject MainPanel;
    [SerializeField, ChildField("ResultText")] private TextMeshProUGUI ResultText;
    [SerializeField, ChildField("SubText")] private TextMeshProUGUI SubText;
    [SerializeField, ChildField("OkButton")] private Button OkButton;

    private void Start()
    {
        GameScene.Instance.OnGameFinished += OnGameFinished;
        OkButton.onClick.AddListener(OnOkButtonClick);
    }

    private void OnGameFinished(ulong? winnerClientId)
    {
        if (winnerClientId == null)
        {
            ResultText.text = "무승부";
            ResultText.color = Color.gray;
            SubText.text = "양쪽 코어 파괴됨";
        }
        if (winnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            ResultText.text = "승리";
            ResultText.color = new Color(0.0f, 0.2f, 1.0f);
            SubText.text = "적 코어 파괴 성공";
        }
        else
        {
            ResultText.text = "패배";
            ResultText.color = new Color(1.0f, 0.0f, 0.0f);
            SubText.text = "내 코어 파괴됨";
        }

        MainPanel.SetActive(true);
    }
    private async void OnOkButtonClick()
    {
        await GameManager.Instance.ExitGameToLobbyAsync();
    }
}
