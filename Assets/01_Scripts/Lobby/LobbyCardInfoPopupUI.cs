using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyCardInfoPopupUI : MonoBehaviour
{
    [SerializeField, ChildField] private GameObject MainPanel;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;


}
