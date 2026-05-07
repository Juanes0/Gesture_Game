using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Jugadores")]
    public PlayerHealth player1;
    public PlayerHealth player2;

    [Header("Offset de respawn (distancia al jugador vivo)")]
    public float respawnOffsetX = 2f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        Debug.Log($"{deadPlayer.name} murió. Respawn en {deadPlayer.GetComponent<PlayerHealth>().respawnDelay}s");

        // Aquí podrías activar UI de "¡Jugador X eliminado!" si lo deseas
    }

    public void OnPlayerRespawned(PlayerHealth player)
    {
        Debug.Log($"{player.name} respawneó.");
    }

    // Devuelve una posición cerca del jugador vivo
    public Vector3 GetRespawnPosition(PlayerHealth deadPlayer)
    {
        PlayerHealth alivePlayer = (deadPlayer == player1) ? player2 : player1;

        // Si el otro también está muerto (edge case), respawn en el centro
        if (!alivePlayer.IsAlive)
            return Vector3.zero;

        // Aparece al lado del jugador vivo, alternando izquierda/derecha
        float offset = (deadPlayer == player1) ? -respawnOffsetX : respawnOffsetX;
        return alivePlayer.transform.position + new Vector3(offset, 0f, 0f);
    }
}