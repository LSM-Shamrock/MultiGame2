using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_GameResult : MonoBehaviour
{
    [SerializeField, AssetField("Sfx_Game_Result_Win")] private AudioClip _sfx_win;
    [SerializeField, AssetField("Sfx_Game_Result_Lose")] private AudioClip _sfx_lose;
    [SerializeField, AssetField("Sfx_Game_Result_Draw")] private AudioClip _sfx_draw;
    
    [SerializeField, ChildField("MainPanel")] private Image MainPanel;
    [SerializeField, ChildField("OkButton")] private Button OkButton;
    [SerializeField, ChildField("ResultText")] private TextMeshProUGUI ResultText;
    
    [SerializeField, ChildField("Display_Normal")] private GameObject Display_Normal;
    [SerializeField, ChildField("Display_OpponentDisconnect")] private GameObject Display_OpponentDisconnect;
    
    [SerializeField, ChildField("LocalPlayerNameText")] private TextMeshProUGUI LocalPlayerNameText;
    [SerializeField, ChildField("OpponentPlayerNameText")] private TextMeshProUGUI OpponentPlayerNameText;
    [SerializeField, ChildrenArrayField("LocalCorePoints")] private GameObject[] LocalCorePoints;
    [SerializeField, ChildrenArrayField("OpponentCorePoints")] private GameObject[] OpponentCorePoints;

    private void Start()
    {
        ISceneInstance<GameScene>.SceneInstance.OnGameFinished += OnGameFinished;
        ISceneInstance<GameScene>.SceneInstance.OnGameFinishedDueToOpponentDisconnect += OnGameFinishedDueToOpponentDisconnect;
        OkButton.onClick.AddListener(OnOkButtonClick);
    }

    private async void OnOkButtonClick()
    {
        await MatchingManager.Instance.ExitGameToLobbyAsync();
    }

    private void SetWin()
    {
        ResultText.text = "승리";
        ResultText.color = new Color(0.0f, 0.2f, 1.0f);
        SoundManager.Instance.PlaySfx(_sfx_win);
    }
    private void SetDefeat()
    {
        ResultText.text = "패배";
        ResultText.color = new Color(1.0f, 0.0f, 0.0f);
        SoundManager.Instance.PlaySfx(_sfx_lose);
    }
    private void SetDraw()
    {
        ResultText.text = "무승부";
        ResultText.color = Color.gray;
        SoundManager.Instance.PlaySfx(_sfx_draw);
    }

    private void OnGameFinished(GameFinishData data)
    {
        Display_Normal.SetActive(true);
        Display_OpponentDisconnect.SetActive(false);

        SoundManager.Instance.BgmPitch.Value = 1f;
        SoundManager.Instance.StopBgm();

        PlayerGameFinishData local;
        PlayerGameFinishData opponent;
        if (data.PlayerData1.ClientId == NetworkManager.Singleton.LocalClientId)
        {
            local = data.PlayerData1;
            opponent = data.PlayerData2;
        }
        else
        {
            local = data.PlayerData2;
            opponent = data.PlayerData1;
        }

        SetResult(local.CorePoint, opponent.CorePoint);
        SetLocalInfo(local);
        SetOpponentInfo(opponent);

        StartCoroutine(ShowRoutine());
    }
    private void SetResult(int localCorePoint, int opponentCorePoint)
    {
        if (localCorePoint > opponentCorePoint)
        {
            SetWin();
        }
        else if (localCorePoint < opponentCorePoint)
        {
            SetDefeat();
        }
        else
        {
            SetDraw();
        }
    }
    private void SetLocalInfo(PlayerGameFinishData data)
    {
        LocalPlayerNameText.text = data.PlayerName;

        var points = LocalCorePoints;
        for (int i = 0; i < points.Length; i++)
            points[i].SetActive(i < data.CorePoint);
    }
    private void SetOpponentInfo(PlayerGameFinishData data)
    {
        OpponentPlayerNameText.text = data.PlayerName;

        var points = OpponentCorePoints;
        for (int i = 0; i < points.Length; i++)
            points[i].SetActive(i < data.CorePoint);
    }

    private void OnGameFinishedDueToOpponentDisconnect()
    {
        Display_Normal.SetActive(false);
        Display_OpponentDisconnect.SetActive(true);

        SoundManager.Instance.BgmPitch.Value = 1f;
        SoundManager.Instance.StopBgm();

        SetWin();

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
