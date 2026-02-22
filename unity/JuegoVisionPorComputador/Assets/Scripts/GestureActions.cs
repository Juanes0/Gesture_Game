using UnityEngine;

/// <summary>
/// GestureActions — reacciona a los gestos recibidos por GestureReceiver.
///
/// SETUP RÁPIDO DE PRUEBA:
///   1. Crea un Sprite 2D (ej: un cuadrado) en la escena.
///   2. Añade un Rigidbody2D al sprite.
///   3. Adjunta este script al sprite.
///   4. Asegúrate de que GestureReceiver también existe en la escena.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GestureActions : MonoBehaviour
{
    [Header("Parámetros de movimiento")]
    public float jumpForce  = 8f;
    public float attackTime = 0.2f;   // segundos que el objeto cambia de color

    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Color         originalColor;
    private bool          isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            originalColor = sr.color;

        // Suscribirse al evento de gestos
        GestureReceiver.OnGesture += HandleGesture;
        Debug.Log("[GestureActions] Suscrito a gestos.");
    }

    void OnDestroy()
    {
        // Siempre desuscribirse para evitar memory leaks
        GestureReceiver.OnGesture -= HandleGesture;
    }

    // ── Reaccionar al gesto ─────────────────────────────────────────────────
    void HandleGesture(string gesture)
    {
        switch (gesture.ToUpper())
        {
            case "JUMP":
                DoJump();
                break;

            case "ATTACK":
                DoAttack();
                break;

            case "SHOOT":
                DoShoot();
                break;

            default:
                Debug.Log($"[GestureActions] Gesto desconocido: {gesture}");
                break;
        }
    }

    // ── Acciones ────────────────────────────────────────────────────────────
    void DoJump()
    {
        if (!isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);   // resetear Y
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;

        Debug.Log("¡SALTO!");
    }

    void DoAttack()
    {
        Debug.Log("¡ATAQUE!");

        // Feedback visual: cambiar color brevemente a rojo
        if (sr != null)
        {
            sr.color = Color.red;
            Invoke(nameof(ResetColor), attackTime);
        }

        // TODO: aquí añadirás la lógica real de ataque (hitbox, animación, etc.)
    }

    void DoShoot()
    {
        Debug.Log("¡DISPARO!");

        // Feedback visual: cambiar color a amarillo
        if (sr != null)
        {
            sr.color = Color.yellow;
            Invoke(nameof(ResetColor), attackTime);
        }

        // TODO: aquí instanciarás el proyectil
    }

    void ResetColor()
    {
        if (sr != null)
            sr.color = originalColor;
    }

    // ── Detectar suelo ──────────────────────────────────────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        // Cualquier colisión por debajo = suelo
        if (col.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }
}