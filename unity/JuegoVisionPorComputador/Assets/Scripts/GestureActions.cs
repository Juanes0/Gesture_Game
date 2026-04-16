using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class GestureActions : MonoBehaviour
{
    [Header("Jugador")]
    public PlayerNumber playerNumber = PlayerNumber.Player1;

    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;

    [Header("Proyectil")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;

    [Header("Animación")]
    public string paramRunning = "isRunning";
    public string paramJumping = "isJumping";
    public string paramShooting = "isShooting";

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Tooltip("Distancia desde el centro del personaje hasta sus pies + un pequeño margen")]
    public float feetOffset = 0.8f;     // ← Ajusta esto según el tamaño de tu sprite (prueba valores entre 0.6 y 1.2)

    [Tooltip("Cuánto debe penetrar el raycast en el suelo para considerarlo grounded")]
    public float raycastDistance = 0.2f;   // pequeño margen (0.1f ~ 0.3f suele ser bueno)

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private float moveDirection = 0f;
    private bool isGrounded = true;
    private bool facingRight = true;

    // ← NUEVO: Cooldown para evitar saltos múltiples y que deje de funcionar
    private float lastJumpTime = 0f;
    private readonly float jumpCooldown = 0.6f;   // tiempo mínimo entre saltos

    public enum PlayerNumber { Player1, Player2 }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        var state = (playerNumber == PlayerNumber.Player1)
                    ? GestureReceiver.Player1State
                    : GestureReceiver.Player2State;

        float x = state.x;
        float y = state.y;
        string gesto = state.gesture.ToUpper();

        // Debug (puedes comentarlo después)
        Debug.Log($"[{playerNumber}] X:{x:F3} | Y:{y:F3} | G:{gesto} | Grounded:{isGrounded}");

        // ── MOVIMIENTO HORIZONTAL (zonas exactas como en Python) ─────────────
        float moveDir = 0f;
        if (playerNumber == PlayerNumber.Player1)
        {
            if (x < 0.67f) moveDir = -1f;      // LEFT
            else if (x > 0.83f) moveDir = 1f;  // RIGHT
        }
        else // P2
        {
            if (x < 0.17f) moveDir = -1f;      // LEFT
            else if (x > 0.33f) moveDir = 1f;  // RIGHT
        }
        moveDirection = moveDir;

        if (Mathf.Abs(moveDirection) > 0.1f)
            Flip(moveDirection > 0);

        CheckGrounded();

        // ── ANIMACIONES ─────────────────────────────────────────────────────
        bool isRunningNow = Mathf.Abs(rb.linearVelocity.x) > 0.2f && isGrounded;
        anim.SetBool(paramRunning, isRunningNow);
        anim.SetBool(paramJumping, !isGrounded);

        // ── SALTO (solo cuando levantas la mano y estás en el suelo) ────────
        if (y < 0.40f && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            DoJump();
            lastJumpTime = Time.time;
        }

        // ── GESTOS ──────────────────────────────────────────────────────────
        if (gesto == "SHOOT") DoShoot();
        else if (gesto == "ATTACK") DoAttack();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
    }

    void DoAttack() { Debug.Log($"[{playerNumber}] ATTACK"); }
    void DoShoot()
    {
        anim.SetBool(paramShooting, true);
        Invoke(nameof(StopShooting), 0.6f);
        if (bulletPrefab == null) return;
        Vector3 spawn = transform.position + new Vector3(facingRight ? 0.7f : -0.7f, 0.2f, 0);
        GameObject bul = Instantiate(bulletPrefab, spawn, Quaternion.identity);
        if (bul.GetComponent<Rigidbody2D>() is Rigidbody2D bRb)
            bRb.linearVelocity = new Vector2(facingRight ? bulletSpeed : -bulletSpeed, 0);
        Destroy(bul, 3f);
    }
    void StopShooting() => anim.SetBool(paramShooting, false);

    void Flip(bool toRight)
    {
        if (facingRight == toRight) return;
        facingRight = toRight;
        sr.flipX = !toRight;
    }
    private void CheckGrounded()
    {
        // Origen del raycast: justo debajo de los pies del personaje
        Vector2 origin = (Vector2)transform.position - new Vector2(0f, feetOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, raycastDistance, groundLayer);

        isGrounded = hit.collider != null;

        // Debug visual: ahora la línea debería salir desde los pies
        Color color = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(origin, Vector2.down * raycastDistance, color, 0.05f);   // duración corta para que no se acumule
    }
}