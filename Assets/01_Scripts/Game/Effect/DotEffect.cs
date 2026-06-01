using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[AutoInjectionTarget]
public class DotEffect : MonoBehaviour
{
    [SerializeField, ChildField("DotEffectSprite")] private Animator _animator;
    [SerializeField, ChildField("DotEffectSprite")] private SpriteRenderer _spriteRenderer;

    private FieldObject _target;

    private void LateUpdate()
    {
        transform.position = _target.ColliderCenter;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        EffectPool.Instance.ReturnDotEffectToPool(this);
    }

    public void Show(DotEffectData data, FieldObject target)
    {
        if (target == null)
        {
            Hide();
            return;
        }

        _target = target;
        transform.position = _target.ColliderCenter;
        transform.localScale = _target.transform.localScale;
        gameObject.SetActive(true);

        var clip = _animator.runtimeAnimatorController.animationClips.First(c => c.name == data.EffectAnimation);

        _animator.speed = clip.length / (data.DotCount * data.DotInterval);
        _animator.Play(data.EffectAnimation, 0, 0f);

        StartCoroutine(FadeRoutine(data));
    }

    public IEnumerator FadeRoutine(DotEffectData data)
    {
        var color = _spriteRenderer.color;
        var alphaDecrease = color.a / data.DotCount;

        var waitForInterval = new WaitForSeconds(data.DotInterval);

        for (int i = 0; i < data.DotCount; i++)
        {
            yield return waitForInterval;

            color.a -= alphaDecrease;
            _spriteRenderer.color = color;
        }

        Hide();
    }
}
