using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeDamager : MonoBehaviour
{
    // Este script va en un GameObject vacío con TilemapCollider2D
    // El Tilemap de pinchos debe tener: TilemapCollider2D (Is Trigger = true)

    // Rastreamos qué jugadores están actualmente dentro
    private HashSet<PlayerHealth> playersInside = new HashSet<PlayerHealth>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            playersInside.Add(ph);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            playersInside.Remove(ph);
    }

    void Update()
    {
        foreach (PlayerHealth ph in playersInside)
        {
            // Daño por segundo proporcional al tiempo
            ph.TakeDamage(ph.spikeDamagePerSecond * Time.deltaTime);
        }
    }
}