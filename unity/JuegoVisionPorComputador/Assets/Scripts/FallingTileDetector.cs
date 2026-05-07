using UnityEngine;

public class FallingTileDetector : MonoBehaviour
{
    private FallingTileManager manager;

    void Awake()
    {
        manager = GetComponentInParent<FallingTileManager>();
    }

    // Se activa cuando el jugador está encima del tilemap
    void OnTriggerStay2D(Collider2D other)
    {
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null || !ph.IsAlive) return;

        // Detectar el tile exactamente debajo de los pies del jugador
        Vector3 feetPosition = other.bounds.center - new Vector3(0f, other.bounds.extents.y, 0f);
        manager.TriggerTileAt(feetPosition);
    }
}