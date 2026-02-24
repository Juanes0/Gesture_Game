using UnityEngine;

/// <summary>
/// GestureActions v3 — Plataformero 2 jugadores
///
/// SETUP:
///   1. Crea dos sprites en la escena (Jugador1 y Jugador2)
///   2. A cada uno: Add Component → Rigidbody2D + BoxCollider2D + este script
///   3. En el Inspector de cada uno, selecciona el Player Number:
///      → Jugador1: Player Number = Player1
///      → Jugador2: Player Number = Player2
///   4. Rigidbody2D en ambos:
///      - Gravity Scale: 3
///      - Constraints → Freeze Rotation Z ✅
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class GestureActions : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    public enum PlayerNumber { Player1, Player2 }

    [Header("Jugador")]
    public PlayerNumber playerNumber = PlayerNumber.Player1;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Proyectil (opcional)")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;

    [Header("Feedback visual")]
    public float flashTime = 0.2f;

    // ── Componentes ──────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;

    // ── Estado ───────────────────────────────────────────────────────────────
    private float moveDirection = 0f;
    private bool isGrounded = false;
    private bool facingRight = true;

    // ── Unity ────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        // Suscribirse al evento del jugador correcto
        if (playerNumber == PlayerNumber.Player1)
            GestureReceiver.OnGestureP1 += HandleGesture;
        else
            GestureReceiver.OnGestureP2 += HandleGesture;

        Debug.Log($"[GestureActions] {playerNumber} listo.");
    }

    void OnDestroy()
    {
        if (playerNumber == PlayerNumber.Player1)
            GestureReceiver.OnGestureP1 -= HandleGesture;
        else
            GestureReceiver.OnGestureP2 -= HandleGesture;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    // ── Procesar mensaje del jugador ─────────────────────────────────────────
    void HandleGesture(string msg)
    {
        switch (msg.ToUpper())
        {
            case "LEFT":
                moveDirection = -1f;
                Flip(false);
                break;

            case "RIGHT":
                moveDirection = 1f;
                Flip(true);
                break;

            case "STOP":
                moveDirection = 0f;
                break;

            case "JUMP":
                DoJump();
                break;

            case "ATTACK":
                DoAttack();
                break;

            case "SHOOT":
                DoShoot();
                break;
        }
    }

    // ── Acciones ─────────────────────────────────────────────────────────────
    void DoJump()
    {
        if (!isGrounded) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
        Debug.Log($"[{playerNumber}] SALTO");
    }

    void DoAttack()
    {
        Debug.Log($"[{playerNumber}] ATAQUE");
        sr.color = Color.red;
        Invoke(nameof(ResetColor), flashTime);
        // TODO: activar hitbox
    }

    void DoShoot()
    {
        Debug.Log($"[{playerNumber}] DISPARO");
        sr.color = Color.yellow;
        Invoke(nameof(ResetColor), flashTime);

        if (bulletPrefab == null) return;

        Vector3 spawnPos = transform.position + new Vector3(facingRight ? 0.6f : -0.6f, 0f, 0f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D bRb = bullet.GetComponent<Rigidbody2D>();
        if (bRb != null)
            bRb.linearVelocity = new Vector2(facingRight ? bulletSpeed : -bulletSpeed, 0f);

        Destroy(bullet, 3f);
    }

    void Flip(bool toRight)
    {
        if (facingRight == toRight) return;
        facingRight = toRight;
        sr.flipX = !toRight;
    }

    void ResetColor() => sr.color = originalColor;

    void OnCollisionEnter2D(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
            if (contact.normal.y > 0.5f) { isGrounded = true; return; }
    }

    void OnCollisionExit2D(Collision2D col) => isGrounded = false;
}