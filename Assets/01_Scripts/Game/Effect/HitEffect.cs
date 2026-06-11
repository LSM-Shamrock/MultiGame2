using System.Linq;
using UnityEngine;

[AutoInjectionTarget]
public class HitEffect : MonoBehaviour
{
    [SerializeField, ChildField("HitEffectSprite")]
    private Animator _animator;

    private void Hide()
    {
        gameObject.SetActive(false);
        ISceneInstance<EffectPool>.SceneInstance.ReturnHitEffectToPool(this);
    }

    public void Show(AttackHitData data, Vector2 position)
    {
        transform.position = position;
        gameObject.SetActive(true);

        var clip = _animator.runtimeAnimatorController.animationClips.First(c => c.name == data.EffectAnimation);

        _animator.speed = clip.length / data.EffectTime;
        _animator.Play(data.EffectAnimation, 0, 0f);

        Invoke(nameof(Hide), data.EffectTime);
    }
}
