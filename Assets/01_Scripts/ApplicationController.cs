using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    private int DEFAULT_WINDOW_WIDTH = 960;
    private int DEFAULT_WINDOW_HEIGHT = 640;

    private void Awake()
    {
        DontDestroyOnLoad(this);
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