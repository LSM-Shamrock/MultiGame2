using UnityEngine;
using UnityEngine.SceneManagement;

public static class Entrypoint 
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
            SceneManager.LoadScene(0);
    }
}
