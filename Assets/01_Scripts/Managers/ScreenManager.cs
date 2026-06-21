using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<ScreenManager>());
    private static ScreenManager _instance;

    private int DEFAULT_WINDOW_WIDTH = 960;
    private int DEFAULT_WINDOW_HEIGHT = 640;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullScreen();
        }
    }

    private void ToggleFullScreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
        {
            Resolution maxResolution = Screen.currentResolution;
            Screen.SetResolution(maxResolution.width, maxResolution.height, FullScreenMode.FullScreenWindow);

            Debug.Log($"전체화면 전환: {maxResolution.width} x {maxResolution.height}");
        }
        else
        {
            Screen.SetResolution(DEFAULT_WINDOW_WIDTH, DEFAULT_WINDOW_HEIGHT, FullScreenMode.Windowed);

            Debug.Log($"창모드 전환: {DEFAULT_WINDOW_WIDTH} x {DEFAULT_WINDOW_HEIGHT}");
        }
    }
}