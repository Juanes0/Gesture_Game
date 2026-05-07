using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra la cámara web en un RawImage de la UI.
/// Python (gesture_sender.py) accede a la misma cámara en paralelo para MediaPipe.
/// En Windows, ambos pueden abrir la cámara simultáneamente sin conflicto.
/// </summary>
public class CameraDisplay : MonoBehaviour
{
    [Header("UI Camera Display")]
    public RawImage cameraRawImage;     // ← Arrastra aquí el RawImage de la UI

    [Header("Configuración de Cámara")]
    [Tooltip("Índice del dispositivo de cámara (0 = primera cámara del sistema)")]
    public int deviceIndex = 0;
    public int width  = 1280;
    public int height = 720;
    public int fps    = 30;

    private WebCamTexture webCamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("❌ No se encontró ninguna cámara en el sistema.");
            return;
        }

        // Log de cámaras disponibles
        Debug.Log($"📷 Cámaras disponibles: {devices.Length}");
        for (int i = 0; i < devices.Length; i++)
            Debug.Log($"  [{i}] {devices[i].name}");

        if (deviceIndex >= devices.Length)
        {
            Debug.LogWarning($"⚠️ deviceIndex={deviceIndex} fuera de rango. Usando índice 0.");
            deviceIndex = 0;
        }

        string deviceName = devices[deviceIndex].name;
        webCamTexture = new WebCamTexture(deviceName, width, height, fps);

        if (cameraRawImage != null)
        {
            cameraRawImage.texture = webCamTexture;
            webCamTexture.Play();

            // Esperar un frame para verificar que la cámara inició
            StartCoroutine(VerificarCamara());
        }
        else
        {
            Debug.LogError("❌ Falta asignar el RawImage en el Inspector del objeto CameraDisplay.");
        }
    }

    private System.Collections.IEnumerator VerificarCamara()
    {
        // Esperar hasta que la textura tenga datos reales
        float timeout = 3f;
        float elapsed = 0f;

        while (!webCamTexture.didUpdateThisFrame && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (webCamTexture.didUpdateThisFrame)
        {
            Debug.Log($"✅ Cámara iniciada: {webCamTexture.deviceName} " +
                      $"({webCamTexture.width}x{webCamTexture.height} @ {webCamTexture.requestedFPS}fps)");

            // Corregir rotación si la cámara viene invertida verticalmente
            // (común en algunas webcams en Unity)
            if (webCamTexture.videoVerticallyMirrored)
            {
                cameraRawImage.rectTransform.localScale = new Vector3(1, -1, 1);
                Debug.Log("🔄 Imagen corregida verticalmente.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ La cámara no envió frames en 3 segundos. " +
                             "Si Python también la usa, verifica que ambos puedan acceder simultáneamente.");
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            Debug.Log("🛑 Cámara detenida.");
        }
    }
}