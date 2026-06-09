using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_Matchmaking : MonoBehaviour
{
    [ChildField] public Button CancleButton;
    [ChildField] public TMP_InputField JoinCodeField;
    [ChildField] public TextMeshProUGUI StateText;
}
