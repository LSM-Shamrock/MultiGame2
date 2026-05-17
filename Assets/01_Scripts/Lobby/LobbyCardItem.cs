using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyCardItem : MonoBehaviour
{
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private Image FadeImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;


}
