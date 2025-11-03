using System.Collections;
using TMPro;
using UnityEngine;

public class narrativeText_script : MonoBehaviour
{
    Rigidbody rb;
    Collider textcol;

    [SerializeField] private TextMeshPro textToFade;  // For UI Text
    // If using 3D TextMeshPro, change to: private TextMeshPro textToFade;

    [SerializeField] private float fadeDuration = 2f;  // Seconds to fully fade in

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Debug.Log("IIIIIIIIIII HAVE ENTERED THE SPHERE!");
            StartCoroutine(FadeInText());
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            StartCoroutine(FadeOutText());
        }
    }

    private IEnumerator FadeInText()
    {
        if (textToFade == null)
        {
            Debug.LogWarning("No TextMeshPro object assigned!");
            yield break;
        }

        Color originalColor = textToFade.color;
        float elapsedTime = 0f;

        // Start with 0 alpha
        textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        while (elapsedTime < fadeDuration)
        {
            float newAlpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final alpha = 1
        textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
    }
    
    private IEnumerator FadeOutText()
    {
        if (textToFade == null)
        {
            Debug.LogWarning("No TextMeshPro object assigned!");
            yield break;
        }

        Color originalColor = textToFade.color;
        float elapsedTime = 0f;

        // Ensure text starts fully visible
        textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        while (elapsedTime < fadeDuration)
        {
            float newAlpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final alpha = 0
        textToFade.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

}
