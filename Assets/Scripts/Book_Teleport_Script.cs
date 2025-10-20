// ...existing code...
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class Book_Teleport_Script : MonoBehaviour
{
    [Header("Teleport")]
    [Tooltip("Scene name to load. Add the scene to Build Settings.")]
    public string targetSceneName;

    [Tooltip("If true, uses the build index instead of the scene name.")]
    public bool useBuildIndex = false;
    public int targetSceneBuildIndex = 0;

    [Header("Pickup / Trigger")]
    [Tooltip("Optional pickup sound played at the object's position.")]
    public AudioClip pickupSound;

    [Tooltip("Delay before loading the scene (seconds).")]
    public float delayBeforeLoad = 0f;

    [Tooltip("Destroy this teleporter object after pickup.")]
    public bool destroyOnPickup = false;

    // XR interactable reference
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable xrInteractable;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure proper physics settings to avoid tunneling / falling through
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Do NOT force colliders to be triggers here. Trigger colliders won't block physics and will make objects pass through the map.
        Collider c = GetComponent<Collider>();
        if (c == null)
        {
            Debug.LogWarning("Book_Teleport_Script: No Collider found on teleporter object. Add a Collider (non-trigger) so it collides with the world.");
        }
        else if (c.isTrigger)
        {
            Debug.Log("Book_Teleport_Script: Collider is set as a trigger. For physical collisions (prevent falling through) use a non-trigger collider and a Rigidbody.");
        }

        xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (xrInteractable != null)
        {
            // Called when the interactor selects (grabs) this object
            xrInteractable.selectEntered.AddListener(OnSelectEntered);
            xrInteractable.selectExited.AddListener(OnSelectExited);
        }
        else
        {
            Debug.LogWarning("Book_Teleport_Script: No XRBaseInteractable found. Add XRGrabInteractable / XRSocketInteractor etc. to use XR interaction.");
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

    // XR callback - when grabbed
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Make object kinematic while held so it doesn't jitter or fall through geometry
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        StartCoroutine(HandlePickupAndTeleport());
    }

    // XR callback - when released
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    // kept for backwards compatibility if you still want trigger-based pickups (optional)
    void OnTriggerEnter(Collider other)
    {
        // Intentionally left empty when using XR toolkit. If you want non-XR trigger pickups,
        // add a condition here (e.g. check tag or component) and call StartCoroutine(HandlePickupAndTeleport()).
    }

    IEnumerator HandlePickupAndTeleport()
    {
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (delayBeforeLoad > 0f)
            yield return new WaitForSeconds(delayBeforeLoad);

        if (useBuildIndex)
        {
            SceneManager.LoadScene(targetSceneBuildIndex);
        }
        else
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("Book_Teleport_Script: targetSceneName is empty.");
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
// ...existing code...