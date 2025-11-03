using System.Collections;
using TMPro;
using UnityEngine;

public class narrativeText_script : MonoBehaviour
{
    Rigidbody rb;
    Collider textcol;
    TextMeshPro narrativeText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        narrativeText.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            narrativeText.enabled = true;
        }

    }

    IEnumerator Azerty()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
