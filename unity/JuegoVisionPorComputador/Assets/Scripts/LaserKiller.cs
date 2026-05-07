using UnityEngine;

public class LaserKiller : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[LaserKiller] Tocó: {other.gameObject.name} | Tag: {other.tag}");

        // GetComponentInParent en lugar de GetComponent
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            Debug.Log($"[LaserKiller] InstantDie en {ph.gameObject.name}");
            ph.InstantDie();
        }
        else
            Debug.Log("[LaserKiller] No encontró PlayerHealth en ese objeto ni en sus padres");
    }
}