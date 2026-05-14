using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyUI : MonoBehaviour
{
    [SerializeField, ChildField] 
    private Button _playbutton;

    private void Start()
    {

    }
}
