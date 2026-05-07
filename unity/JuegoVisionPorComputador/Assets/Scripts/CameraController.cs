using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Jugadores")]
    public Transform player1;
    public Transform player2;

    [Header("Zoom")]
    public float minOrthoSize = 5f;      // Zoom máximo (jugadores cerca)
    public float maxOrthoSize = 10f;     // Zoom mínimo (jugadores lejos)
    public float zoomPadding = 2f;       // Margen extra alrededor de los jugadores
    public float zoomSpeed = 3f;         // Velocidad de interpolación del zoom

    [Header("Seguimiento")]
    public float followSpeed = 5f;       // Velocidad de seguimiento del centro
    public Vector2 boundsOffset = new Vector2(0f, 1f); // Offset vertical de la cámara

    [Header("Límite de separación")]
    public float maxPlayerDistance = 14f; // Distancia máxima antes de bloquear
    public float blockZoneThickness = 0.5f; // Grosor de la "pared invisible"

    [Header("Debug")]
    public bool showGizmos = true;

    private Camera cam;
    private PlayerBoundary p1Boundary;
    private PlayerBoundary p2Boundary;

    // Distancia actual entre jugadores (pública para que otros scripts la lean)
    public float CurrentDistance { get; private set; }
    public bool IsAtLimit { get; private set; }

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Obtener o agregar el componente de límite a cada jugador
        p1Boundary = player1.GetComponent<PlayerBoundary>() 
                     ?? player1.gameObject.AddComponent<PlayerBoundary>();
        p2Boundary = player2.GetComponent<PlayerBoundary>() 
                     ?? player2.gameObject.AddComponent<PlayerBoundary>();

        p1Boundary.Init(this, player2);
        p2Boundary.Init(this, player1);
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        Vector3 p1Pos = player1.position;
        Vector3 p2Pos = player2.position;

        // ── Centro entre los dos jugadores ───────────────────────────
        Vector3 center = (p1Pos + p2Pos) * 0.5f;
        center.z = transform.position.z; // Mantener Z de la cámara

        // Aplicar offset vertical
        center.y += boundsOffset.y;

        // Mover cámara suavemente al centro
        transform.position = Vector3.Lerp(
            transform.position,
            center,
            followSpeed * Time.deltaTime
        );

        // ── Zoom según distancia ──────────────────────────────────────
        CurrentDistance = Vector2.Distance(p1Pos, p2Pos);
        IsAtLimit = CurrentDistance >= maxPlayerDistance;

        float targetSize = Mathf.Clamp(
            (CurrentDistance * 0.5f) + zoomPadding,
            minOrthoSize,
            maxOrthoSize
        );

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            zoomSpeed * Time.deltaTime
        );
    }

    // Devuelve el límite de la cámara en X para bloquear a un jugador
    public float GetCameraLeftEdge()  => transform.position.x - cam.orthographicSize * cam.aspect;
    public float GetCameraRightEdge() => transform.position.x + cam.orthographicSize * cam.aspect;

    void OnDrawGizmos()
    {
        if (!showGizmos || player1 == null || player2 == null) return;

        // Línea entre jugadores
        Gizmos.color = IsAtLimit ? Color.red : Color.cyan;
        Gizmos.DrawLine(player1.position, player2.position);

        // Punto central
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere((player1.position + player2.position) * 0.5f, 0.15f);
    }
}