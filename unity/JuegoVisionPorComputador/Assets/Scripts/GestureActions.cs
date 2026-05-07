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
    

    [Header("Shoot Settings - Estilo Cuphead")]
    public string paramShooting = "isShooting";
    public float shootCooldown = 0.25f;           // Tiempo entre disparos (ajusta para que se sienta como Cuphead)
    public float shootAnimationDuration = 0.4f;   // Duración de la animación de disparo

    private float lastShootTime = 0f;
    private bool isShooting = false;

    [Header("Crouch")]
    public string paramCrouching = "isCrouching";   // nombre exacto en el Animator
    public float crouchSpeedMultiplier = 0.6f;      // velocidad mientras agachado (60% normal)
    private float normalMoveSpeed;                  // guarda la velocidad original

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Tooltip("Distancia desde el centro del personaje hasta sus pies + un pequeño margen")]
    public float feetOffset = 0.8f;     // ← Ajusta esto según el tamaño de tu sprite (prueba valores entre 0.6 y 1.2)

    [Tooltip("Cuánto debe penetrar el raycast en el suelo para considerarlo grounded")]
    public float raycastDistance = 0.2f;   // pequeño margen (0.1f ~ 0.3f suele ser bueno)

    [Header("Collider Crouch")]
    public BoxCollider2D bodyCollider;     // Arrastra aquí el collider del jugador

    // Valores originales (se guardan en Start)
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Valores cuando está agachado (ajústalos según tu sprite)
    public Vector2 crouchColliderSize = new Vector2(1.0f, 0.6f);      // ancho x alto
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.3f);
        
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
        normalMoveSpeed = moveSpeed;
        if (bodyCollider != null)
        {
            originalColliderSize = bodyCollider.size;
            originalColliderOffset = bodyCollider.offset;
        }
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


        // ── SALTO (solo cuando levantas la mano y estás en el suelo) ────────
        if (y < 0.40f && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            DoJump();
            lastJumpTime = Time.time;
        }

        // ── AGACHARSE (CROUCH) ────────────────────────────────────────
        bool shouldCrouch = y > 0.75f;

        anim.SetBool(paramCrouching, shouldCrouch);

        if (bodyCollider != null)
        {
            if (shouldCrouch)
            {
                bodyCollider.size = crouchColliderSize;
                bodyCollider.offset = crouchColliderOffset;
            }
            else
            {
                bodyCollider.size = originalColliderSize;
                bodyCollider.offset = originalColliderOffset;
            }
        }

        bool wantsToShoot = (gesto == "SHOOT") && isGrounded;   // Solo dispara en el piso

        if (wantsToShoot && Time.time - lastShootTime > shootCooldown)
        {
            DoShoot();
            lastShootTime = Time.time;
        }

        // Apagar la animación de disparo después de su duración
        if (isShooting && Time.time - lastShootTime > shootAnimationDuration)
        {
            isShooting = false;
            anim.SetBool(paramShooting, false);
        }

        // Velocidad reducida
        moveSpeed = shouldCrouch ? normalMoveSpeed * crouchSpeedMultiplier : normalMoveSpeed;

        // Animación de correr agachado
        bool isCrouchRunning = shouldCrouch && Mathf.Abs(rb.linearVelocity.x) > 0.2f && isGrounded;

        anim.SetBool("isCrouchRunning", isCrouchRunning);   // ← Nuevo parámetro


        // ── ANIMACIONES ───────────────────────────────────────────────
        CheckGrounded();   // ← Importante: debe estar antes de actualizar animaciones

        bool isRunningNow = Mathf.Abs(rb.linearVelocity.x) > 0.2f && isGrounded;
        anim.SetBool(paramRunning, isRunningNow);
        anim.SetBool(paramJumping, !isGrounded);
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

    void DoShoot()
    {
        if (!isGrounded) return;

        isShooting = true;
        anim.SetBool(paramShooting, true);

        if (bulletPrefab == null) return;

        // Posición de spawn (desde el cañón)
        Vector3 spawnOffset = new Vector3(facingRight ? 0.85f : -0.85f, 0.12f, 0f);
        Vector3 spawnPos = transform.position + spawnOffset;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.Init(facingRight ? 1f : -1f);


        // Aplicar velocidad
        Rigidbody2D bRb = bullet.GetComponent<Rigidbody2D>();
        if (bRb != null)
        {
            float direction = facingRight ? 1f : -1f;
            bRb.linearVelocity = new Vector2(direction * bulletSpeed, 0f);
        }
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