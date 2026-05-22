using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class MpBarUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI MpText;
    [SerializeField, ChildrenGroupField] private Image[] FillImages;

    private float _value;
    public float Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged();
        }
    }


    private void LateUpdate()
    {
        Value += Time.deltaTime / 2f;
    }

    private void OnValueChanged()
    {
        MpText.text = $"{(int)Value}";

        for (int i = 0; i < FillImages.Length; i++)
        {
            Image image = FillImages[i];
            Color color = image.color;
            float fill = image.fillAmount;

            if (Value >= i + 1)
            {
                fill = 1f;
                color.a = 1f;
            }
            else if (Value > i)
            {
                fill = Value % 1f;
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
