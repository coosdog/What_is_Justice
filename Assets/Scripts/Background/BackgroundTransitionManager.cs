using System.Collections;
using UnityEngine;

public sealed class BackgroundTransitionManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine _transitionRoutine;

    private void Awake()
    {
        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void TransitionTo(BackgroundData backgroundData)
    {
        if (backgroundData == null || backgroundData.Sprite == null || backgroundRenderer == null)
        {
            return;
        }

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
        }

        _transitionRoutine = StartCoroutine(TransitionRoutine(backgroundData.Sprite));
    }

    private IEnumerator TransitionRoutine(Sprite nextSprite)
    {
        if (fadeDuration <= 0f)
        {
            backgroundRenderer.sprite = nextSprite;
            SetAlpha(1f);
            _transitionRoutine = null;
            yield break;
        }

        yield return FadeTo(0f, fadeDuration * 0.5f);
        backgroundRenderer.sprite = nextSprite;
        yield return FadeTo(1f, fadeDuration * 0.5f);
        _transitionRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        Color startColor = backgroundRenderer.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = backgroundRenderer.color;
        color.a = alpha;
        backgroundRenderer.color = color;
    }
}
