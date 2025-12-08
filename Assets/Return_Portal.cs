using UnityEngine;
using System.Collections; // Required for Coroutines
using UnityEngine.SceneManagement; // Required for scene operations

public class DelayedSceneChange : MonoBehaviour
{
    [Header("Settings")]
    public string targetSceneName = "Libqry";
    public float delayInSeconds = 2.0f;
    private bool hasCollided = false; // Prevents multiple trigger calls

    // Called when another Collider enters this object's trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasCollided)
        {
            hasCollided = true;
            Debug.Log("Collision detected");

            // Start the Coroutine to handle the delay and scene load
            StartCoroutine(DelayAndLoadScene());
        }
    }

    // Coroutine to handle the delay
    private IEnumerator DelayAndLoadScene()
    {
        // Wait for the specified amount of time
        yield return new WaitForSeconds(delayInSeconds);

        // Once the delay is over, load the new scene
        SceneManager.LoadScene(targetSceneName);
    }
}