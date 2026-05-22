using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class MpBarUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI MpText;
    [SerializeField, ChildrenGroupField] private Image[] FillImages;

    public void Initialize()
    {
        GameManager.Instance.LocalPlayerCore.PlayerMP.OnValueChanged += OnMpChanged;
    }

    private void OnMpChanged(float prev, float curr)
    {
        MpText.text = $"{(int)curr}";

        for (int i = 0; i < FillImages.Length; i++)
        {
            Image image = FillImages[i];
            Color color = image.color;
            float fill = image.fillAmount;

            if (curr >= i + 1)
            {
                fill = 1f;
                color.a = 1f;
            }
            else if (curr > i)
            {
                fill = curr % 1f;
                color.a = 0.1f;
            }
            else
            {
                fill = 0f;
                color.a = 0f;
            }

            image.color = color;
            image.fillAmount = fill;
        }
    }
}
