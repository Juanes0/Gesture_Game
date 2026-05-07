using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)]
    public float parallaxFactor = 0.2f; // 0 = fijo, 1 = sigue igual que la cámara

    private Vector3 lastCameraPos;

    void Start()
    {
        lastCameraPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - lastCameraPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0f);
        lastCameraPos = cameraTransform.position;
    }
}