using UnityEngine;

public class VATTestPlayer : MonoBehaviour
{
    public Material vatMaterial;
    public float fps = 30f;
    private float prevFPS = 0;
    private float frameCounter = 0f;
    private float stepSize = 0;
    private int frameCount = 1;

    void FixedUpdate()
    {
        if (fps != prevFPS)
        {
            frameCount = vatMaterial.GetInt("_Framecount");
            stepSize = (fps / frameCount) / frameCount;
        }
        vatMaterial.SetFloat("_Time_position", frameCounter);
        frameCounter = (frameCounter + stepSize) % 1f;
    }
}
