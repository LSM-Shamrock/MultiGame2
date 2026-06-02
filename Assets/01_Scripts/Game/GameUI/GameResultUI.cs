using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class GameResultUI : MonoBehaviour
{
    [SerializeField, ChildField("MainPanel")] private GameObject MainPanel;
    [SerializeField, ChildField("ResultText")] private TextMeshProUGUI ResultText;
    [SerializeField, ChildField("SubText")] private TextMeshProUGUI SubText;
    [SerializeField, ChildField("OkButton")] private Button OkButton;
}
