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
        ISceneInstance<GameScene>.SceneInstance.OnGameFinished += OnGameFinished;
        OkButton.onClick.AddListener(OnOkButtonClick);
    }

    private async void OnOkButtonClick()
    {
        await MatchingManager.Instance.ExitGameToLobbyAsync();
    }

    private void OnGameFinished(GameFinishData data)
    {
        SoundManager.Instance.BgmPitch.Value = 1f;
        SoundManager.Instance.StopBgm();

        if (data.WinnerClientId == null)
        {
            ResultText.text = "무승부";
            ResultText.color = Color.gray;
            SoundManager.Instance.PlaySfx(_sfx_draw);

            SubText.text = data.GameFinishType switch
            {
                GameFinishType.CoreDestroyed => "양쪽 코어 파괴됨",
                GameFinishType.Timeout => "시간 초과",
                _ => ""
            };
        }
        else if (data.WinnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            ResultText.text = "승리";
            ResultText.color = new Color(0.0f, 0.2f, 1.0f);
            SoundManager.Instance.PlaySfx(_sfx_win);

            SubText.text = data.GameFinishType switch
            {
                GameFinishType.CoreDestroyed => "적 코어 파괴 성공",
                GameFinishType.ClientDisconected => "적 연결 끊김",
                _ => ""
            };
        }
        else
        {
            ResultText.text = "패배";
            ResultText.color = new Color(1.0f, 0.0f, 0.0f);
            SoundManager.Instance.PlaySfx(_sfx_lose);

            SubText.text = data.GameFinishType switch
            {
                GameFinishType.CoreDestroyed => "내 코어 파괴됨",
                GameFinishType.ClientDisconected => "내 연결 끊김",
                _ => ""
            };
        }

        StartCoroutine(ShowRoutine());

    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSeconds(1f);

        OkButton.interactable = false;
        MainPanel.gameObject.SetActive(true);

        float fadeTime = 1f;
        float t = 0f;
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
