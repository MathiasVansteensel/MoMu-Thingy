using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DelayedSceneChange : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetSceneName = "Libqry";
    public float delayInSeconds = 2.0f;

    [Header("Fade Settings")]
    public Image fadeImage;          // Fullscreen UI Image
    public float fadeDuration = 1.0f;

    private bool hasCollided = false;

    private void Start()
    {
        // Ensure fade starts transparent
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasCollided)
        {
            hasCollided = true;
            Debug.Log("Collision detected");

            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        // Optional delay before fade starts
        yield return new WaitForSeconds(delayInSeconds);

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Load scene after fade
        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Color c = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }
}
