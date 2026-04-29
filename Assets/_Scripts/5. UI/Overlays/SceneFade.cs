using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SceneFade : MonoBehaviour
{
    private Image _fadeImage;
    private void Awake()
    {
        _fadeImage = GetComponent<Image>();
    }

    public IEnumerator FadeIn(float duration)
    {
        Color startColor = new Color(_fadeImage.color.r, _fadeImage.color.g, _fadeImage.color.b, 1f);
        Color endColor = new Color(_fadeImage.color.r, _fadeImage.color.g, _fadeImage.color.b, 0f);
        
        yield return StartCoroutine(FadeCoroutine(startColor, endColor, duration));

        gameObject.SetActive(false);
    }

    public IEnumerator FadeOut(float duration)
    {
        
        Color startColor = new Color(_fadeImage.color.r, _fadeImage.color.g, _fadeImage.color.b, 0f);
        Color endColor = new Color(_fadeImage.color.r, _fadeImage.color.g, _fadeImage.color.b, 1f);

        gameObject.SetActive(true);        
        yield return StartCoroutine(FadeCoroutine(startColor, endColor, duration));

    }
    private IEnumerator FadeCoroutine(Color startColor, Color endColor, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            _fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        _fadeImage.color = endColor;
    }

}
