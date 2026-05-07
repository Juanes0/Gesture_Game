using UnityEngine;

public class HittableObject : MonoBehaviour
{
    public Sprite hitSprite;   // Arrastra el sprite de explosión aquí
    private SpriteRenderer sr;
    private bool hit = false;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || !other.CompareTag("Bullet")) return;
        hit = true;
        sr.sprite = hitSprite;    // Cambia el sprite instantáneamente
        Destroy(gameObject, 0.3f);
    }
}