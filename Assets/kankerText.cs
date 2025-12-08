using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class kankerText : MonoBehaviour
{
    HashSet<TextMeshPro> textMeshes = new();

    public float maxDistance = 4;
    public float falloff = 2.5f;

    void Awake()
    {
        var texts = GameObject.FindGameObjectsWithTag("HoereText");
        foreach (var textObj in texts)
        {
            TextMeshPro tm = textObj.GetComponentInChildren<TextMeshPro>();
            if (tm == null) continue;

            textMeshes.Add(tm);
        }
        //Debug.Log(textMeshes.Count);
    }

    //TODO: add support for dynamically spawned meshes by hooking instanciate event and checking types for tmpGUI
    void Update()
    {
        var playerPos = transform.position;

        foreach (var text in textMeshes)
        {
            if (text == null) continue;
            var textPos = text.transform.position;
            float distance = Vector3.Distance(textPos, playerPos);
            float fadeFac = 1 - Mathf.Min(1, Mathf.Pow(distance / maxDistance, falloff));
            //Debug.Log(fadeFac);
            text.alpha = fadeFac;
        }
    }

    void OnDrawGizmos()
    {
        foreach (var text in textMeshes)
        {
            if (text == null) continue;
            var textPos = text.transform.position;
            Gizmos.DrawWireSphere(textPos, maxDistance);
        }
    }
}
