using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSequence : MonoBehaviour
{
    public CanvasGroup backgroundArt;
    public CanvasGroup logoGroup;
    public RectTransform logoRect;

    public float logoStartScale = 0.95f;
    public float logoEndScale = 1.0f;

    public AudioSource bgmSource;

    public float logoFadeInDelay = 1.0f;
    public float logoFadeDuration = 2.0f;
    public float logoHoldDuration = 20.0f;
    public float fadeToRealityDuration = 3.0f;

    public string nextSceneName = "MainScene";

    void Start()
    {
        if (backgroundArt != null) backgroundArt.alpha = 1f;
        if (logoGroup != null) logoGroup.alpha = 0f;
        if (logoRect != null) logoRect.localScale = Vector3.one * logoStartScale;

        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.Play();

        StartCoroutine(RunIntro());
    }

    IEnumerator RunIntro()
    {
        float totalIntroDuration = logoFadeInDelay + logoFadeDuration + logoHoldDuration + fadeToRealityDuration;
        
        // Debug check
        if (backgroundArt == null)
        {
            Debug.LogError("backgroundArt is NULL! Make sure it's assigned in the Inspector.");
        }
        else
        {
            Debug.Log($"Starting background fade over {totalIntroDuration} seconds. Current alpha: {backgroundArt.alpha}");
            StartCoroutine(FadeCanvasGroup(backgroundArt, 1f, 0f, totalIntroDuration));
        }

        if (logoFadeInDelay > 0f)
            yield return new WaitForSeconds(logoFadeInDelay);

        yield return StartCoroutine(FadeInLogo());
        
        yield return new WaitForSeconds(logoHoldDuration);

        if (logoGroup != null)
            StartCoroutine(FadeCanvasGroup(logoGroup, logoGroup.alpha, 0f, fadeToRealityDuration));

        yield return new WaitForSeconds(fadeToRealityDuration);

        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null || duration <= 0f)
            yield break;

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    IEnumerator FadeInLogo()
    {
        if (logoGroup == null || logoRect == null || logoFadeDuration <= 0f)
            yield break;

        float time = 0f;
        logoGroup.alpha = 0f;
        logoRect.localScale = Vector3.one * logoStartScale;

        while (time < logoFadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / logoFadeDuration);

            logoGroup.alpha = Mathf.Lerp(0f, 1f, t);
            float scale = Mathf.Lerp(logoStartScale, logoEndScale, t);
            logoRect.localScale = Vector3.one * scale;

            yield return null;
        }

        logoGroup.alpha = 1f;
        logoRect.localScale = Vector3.one * logoEndScale;
    }
}
