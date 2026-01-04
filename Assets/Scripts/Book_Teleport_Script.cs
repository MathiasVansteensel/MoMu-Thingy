using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class Book_Teleport_Script : MonoBehaviour
{
    [Header("Teleport")]
    public string targetSceneName;
    public bool useBuildIndex = false;
    public int targetSceneBuildIndex = 0;

    [Header("Pickup / Trigger")]
    public AudioClip pickupSound;
    public float delayBeforeLoad = 0f;
    public bool destroyOnPickup = false;

    [Header("Fade Settings")]
    public Image fadeImage;          // Fullscreen UI Image
    public float fadeDuration = 1.0f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable xrInteractable;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Collider c = GetComponent<Collider>();
        if (c == null)
        {
            Debug.LogWarning("Book_Teleport_Script: No Collider found.");
        }

        xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnSelectEntered);
            xrInteractable.selectExited.AddListener(OnSelectExited);
        }

        // Ensure fade starts transparent
        if (fadeImage != null)
        {
            Color col = fadeImage.color;
            col.a = 0f;
            fadeImage.color = col;
        }
    }

    void OnDestroy()
    {
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.RemoveListener(OnSelectEntered);
            xrInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        StartCoroutine(HandlePickupAndTeleport());
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    IEnumerator HandlePickupAndTeleport()
    {
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (delayBeforeLoad > 0f)
            yield return new WaitForSeconds(delayBeforeLoad);

        // Fade out before scene change
        if (fadeImage != null)
            yield return StartCoroutine(FadeOut());

        if (useBuildIndex)
        {
            SceneManager.LoadScene(targetSceneBuildIndex);
        }
        else
        {
            if (!string.IsNullOrEmpty(targetSceneName))
                SceneManager.LoadScene(targetSceneName);
            else
                Debug.LogWarning("Book_Teleport_Script: targetSceneName is empty.");
        }

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color col = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            col.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = col;
            yield return null;
        }

        col.a = 1f;
        fadeImage.color = col;
    }
}
