using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class CameraEffectController : MonoBehaviour, ISceneInstance<CameraEffectController>
{
    [SerializeField, ChildField]
    private Image DamageEffectImage;

    private Vector2 originalPosition = Vector2.zero;
    private float shakeDurationMax = 1f;
    private float shakeAmplitudeMax = 2f;
    private float shakeFrequency = 20f;

    private void Start()
    {
        ((ISceneInstance<CameraEffectController>)this).InitSceneInstance();
    }

    private IEnumerator DamageEffectRoutine(float duration, float shakeAmplitude, float shakeFrequency, float imageAlpha)
    {
        float elapsed = 0f;
        Color color = DamageEffectImage.color;

        while (elapsed < duration)
        {
            float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f;

            transform.localPosition = originalPosition + new Vector2(x, y) * shakeAmplitude;

            float t = elapsed / duration;
            color.a = imageAlpha * (1 - t);
            DamageEffectImage.color = color;    

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    public void DamageEffect(float damageRatio)
    {
        float duration = shakeDurationMax * damageRatio;
        float shakeAmplitude = shakeAmplitudeMax * damageRatio;
        float shakeFrequency = this.shakeFrequency;

        float imageAlpha = Mathf.Lerp(0.25f, 1.0f, damageRatio);

        StartCoroutine(DamageEffectRoutine(duration, shakeAmplitude, shakeFrequency, imageAlpha));
    }
}
