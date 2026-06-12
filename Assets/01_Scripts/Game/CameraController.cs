using System;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour, ISceneInstance<CameraController>
{
    private Vector2 originalPosition = Vector2.zero;

    private float shakeDurationMax = 1f;
    private float shakeAmplitudeMax = 5f;
    private float shakeFrequency = 20f;

    private void Start()
    {
        ((ISceneInstance<CameraController>)this).InitSceneInstance();
    }

    private IEnumerator ShakeRoutine(float duration, float amplitude, float frequency)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;

            transform.localPosition = originalPosition + new Vector2(x, y) * amplitude;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    public void CoreDamageEffect(float damageRatio)
    {
        float duration = shakeDurationMax * Mathf.Pow(damageRatio, 0.5f);
        float amplitude = shakeAmplitudeMax * damageRatio * damageRatio;
        float frequency = shakeFrequency;

        StartCoroutine(ShakeRoutine(duration , amplitude, frequency));
    }
}
