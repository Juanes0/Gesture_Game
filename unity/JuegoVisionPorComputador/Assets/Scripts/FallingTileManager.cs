using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FallingTileManager : MonoBehaviour
{
    [Header("Tiempos")]
    public float timeBeforeFall = 1.2f;   // Segundos de parpadeo antes de caer
    public float timeBeforeRespawn = 3f;  // Segundos antes de reaparecer

    [Header("Parpadeo")]
    public float blinkInterval = 0.1f;    // Qué tan rápido parpadea

    [Header("Caída")]
    public float fallGravity = 4f;        // Gravedad al caer
    public float fallTorque = 30f;        // Rotación al caer (0 para sin rotación)

    [Header("Layer del jugador")]
    public LayerMask playerLayer;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    // Llamado desde FallingTileDetector cuando un jugador pisa la zona
    public void TriggerTileAt(Vector3 worldPosition)
    {
        // Convertir posición del mundo a celda del tilemap
        Vector3Int cellPos = tilemap.WorldToCell(worldPosition);

        // Verificar que haya un tile ahí
        if (!tilemap.HasTile(cellPos)) return;

        StartCoroutine(FallRoutine(cellPos));
    }

    IEnumerator FallRoutine(Vector3Int cellPos)
    {
        // Obtener datos del tile antes de borrarlo
        TileBase tile = tilemap.GetTile(cellPos);
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
        Matrix4x4 matrix = tilemap.GetTransformMatrix(cellPos);
        Color tileColor = tilemap.GetColor(cellPos);

        // ── Fase 1: Parpadeo ──────────────────────────────────────────
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < timeBeforeFall)
        {
            visible = !visible;
            tilemap.SetColor(cellPos, visible ? tileColor : Color.clear);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // Restaurar color y quitar el tile del tilemap
        tilemap.SetColor(cellPos, tileColor);
        tilemap.SetTile(cellPos, null);

        // ── Fase 2: Crear GameObject que cae ─────────────────────────
        GameObject fallingBlock = CreateFallingBlock(worldPos, tile, tileColor);

        // ── Fase 3: Esperar y destruir el bloque que cae ─────────────
        yield return new WaitForSeconds(1.5f);
        if (fallingBlock != null) Destroy(fallingBlock);

        // ── Fase 4: Esperar y reaparecer ─────────────────────────────
        yield return new WaitForSeconds(timeBeforeRespawn);
        tilemap.SetTile(cellPos, tile);
        tilemap.SetTransformMatrix(cellPos, matrix);
        tilemap.SetColor(cellPos, tileColor);
    }

    GameObject CreateFallingBlock(Vector3 position, TileBase tile, Color color)
    {
        // Crear un sprite temporal que simula el bloque cayendo
        GameObject block = new GameObject("FallingBlock");
        block.transform.position = position;

        // SpriteRenderer con el sprite del tile
        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        if (tile is Tile t) sr.sprite = t.sprite;
        sr.color = color;
        sr.sortingOrder = tilemapRenderer.sortingOrder;

        // Rigidbody para la caída
        Rigidbody2D rb = block.AddComponent<Rigidbody2D>();
        rb.gravityScale = fallGravity;
        if (fallTorque > 0f)
            rb.AddTorque(Random.Range(-fallTorque, fallTorque));

        return block;
    }
}