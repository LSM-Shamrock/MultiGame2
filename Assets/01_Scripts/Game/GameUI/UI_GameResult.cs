using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_GameResult : MonoBehaviour
{
    [SerializeField, ChildField("MainPanel")] private Image MainPanel;
    [SerializeField, ChildField("ResultText")] private TextMeshProUGUI ResultText;
    [SerializeField, ChildField("SubText")] private TextMeshProUGUI SubText;
    [SerializeField, ChildField("OkButton")] private Button OkButton;
    [SerializeField, AssetField("Sfx_Game_Result_Win")] private AudioClip _sfx_win;
    [SerializeField, AssetField("Sfx_Game_Result_Lose")] private AudioClip _sfx_lose;
    [SerializeField, AssetField("Sfx_Game_Result_Draw")] private AudioClip _sfx_draw;

    private void Start()
    {
        GameScene.SceneInstance.OnGameFinished += OnGameFinished;
        OkButton.onClick.AddListener(OnOkButtonClick);
    }

    private async void OnOkButtonClick()
    {
        await GameManager.Instance.ExitGameToLobbyAsync();
    }

    private void OnGameFinished(ulong? winnerClientId)
    {
        SoundManager.Instance.StopBgm();

        if (winnerClientId == null)
        {
            ResultText.text = "무승부";
            ResultText.color = Color.gray;
            SubText.text = "양쪽 코어 파괴됨";

            SoundManager.Instance.PlaySfx(_sfx_draw);
        }
        if (winnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            ResultText.text = "승리";
            ResultText.color = new Color(0.0f, 0.2f, 1.0f);
            SubText.text = "적 코어 파괴 성공";

            SoundManager.Instance.PlaySfx(_sfx_win);
        }
        else
        {
            ResultText.text = "패배";
            ResultText.color = new Color(1.0f, 0.0f, 0.0f);
            SubText.text = "내 코어 파괴됨";

            SoundManager.Instance.PlaySfx(_sfx_lose);
        }

        StartCoroutine(ShowRoutine());

    }

    private IEnumerator ShowRoutine()
    {
        var fadeTime = 0.5f;
        var t = 0f;

        OkButton.interactable = false;
        MainPanel.gameObject.SetActive(true);

        while (t < 1f)
        {
            var colorA = new Color(0.0f, 0.0f, 0.0f, 0.4f);
            var colorB = new Color(0.4f, 0.4f, 0.4f, 1.0f);
            MainPanel.color = Color.Lerp(colorA, colorB, t);

            yield return null;
            t += Time.deltaTime / fadeTime;
        }

        OkButton.interactable = true;

        var autoNextWaiting = 5f;
        yield return new WaitForSeconds(autoNextWaiting);

        OnOkButtonClick();
    }
}
