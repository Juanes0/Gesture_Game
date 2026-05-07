using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración Bala")]
    public float speed = 18f;        // Velocidad alta (importante)
    public float lifetime = 2.5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Init(float direction)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(direction * speed, 0f);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
            if (enemy != null)
                enemy.TakeDamage(20f);

            Destroy(gameObject);
            return;
        }

        // Tocó cualquier otra cosa (suelo, pared)
        Destroy(gameObject);
        }
}