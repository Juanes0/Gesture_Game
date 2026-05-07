using System.Collections;
using System.Numerics;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Patrol, Ranger }

    [Header("Tipo de enemigo")]
    public EnemyType enemyType = EnemyType.Patrol;

    [Header("Vida")]
    public float maxHP = 50f;
    private float currentHP;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;

    [Header("Patrulla (solo tipo Patrol)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Detección")]
    public float detectionRange = 6f;   // Rango para detectar al jugador
    public float attackRange = 1.2f;    // Rango para atacar
    public LayerMask playerLayer;

    [Header("Ataque")]
    public float attackDamage = 20f;
    public float attackCooldown = 1.2f;
    public float damageDelay = 0.3f;    // Segundos hasta el frame de daño

    [Header("Knockback al jugador")]
    public float knockbackForce = 4f;

    [Header("Invencibilidad al recibir daño")]
    public float invincibilityTime = 0.4f;

    [Header("Animaciones")]
    public string paramWalk = "isWalking";
    public string paramHurt = "hurt";
    public string paramDeath = "death";
    public string paramAttack = "attack";

    // Estado interno
    private enum State { Patrolling, Chasing, Attacking, Hurt, Dead }
    private State state = State.Patrolling;

    private Transform currentTarget;       // Jugador detectado
    private Transform patrolTarget;        // Punto A o B actual
    private bool isInvincible = false;
    private float lastAttackTime = -99f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private UnityEngine.Vector3 originPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        currentHP = maxHP;

        
        originPosition = transform.position;
        patrolTarget = pointB != null ? pointB : pointA;
    }

    void Update()
    {
        if (state == State.Dead || state == State.Hurt) return;

        DetectPlayers();

        switch (state)
        {
            case State.Patrolling: HandlePatrol();  break;
            case State.Chasing:   HandleChase();   break;
            case State.Attacking: break; // Manejado por corrutina
        }

        UpdateAnimations();
    }

    // ── Detección ────────────────────────────────────────────────────────
    void DetectPlayers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);

        float closestDist = Mathf.Infinity;
        currentTarget = null;

        foreach (Collider2D hit in hits)
        {
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph == null || !ph.IsAlive) continue;

            float dist = UnityEngine.Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                currentTarget = hit.transform;
            }
        }

        if (currentTarget == null)
        {
            // Sin jugador cerca
            if (state == State.Chasing || state == State.Attacking) 
            {
                if (enemyType == EnemyType.Ranger)
                    StartCoroutine(ReturnToOrigin());  // Ranger vuelve a su sitio
                else
                    state = State.Patrolling;           // Patrol retoma la ruta
            }
            return;
        }

        if (closestDist <= attackRange)
        {
            if (state != State.Attacking && Time.time - lastAttackTime > attackCooldown)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            state = State.Chasing;
        }
    }

    IEnumerator ReturnToOrigin()
    {
        state = State.Patrolling; // Reutilizamos el estado para el movimiento

        while (UnityEngine.Vector2.Distance(transform.position, originPosition) > 0.2f)
        {
            // Si detecta un jugador mientras regresa, cancela el retorno
            if (currentTarget != null) yield break;

            MoveTowards(originPosition);
            yield return null;
        }

        rb.linearVelocity = UnityEngine.Vector2.zero;
        state = State.Patrolling; // Se queda quieto esperando
    }

    // ── Patrulla ──────────────────────────────────────────────────────────
    void HandlePatrol()
    {
        if (pointA == null || pointB == null) return;

        MoveTowards(patrolTarget.position);

        // Llegó al destino → cambiar al otro punto
        if (UnityEngine.Vector2.Distance(transform.position, patrolTarget.position) < 0.3f)
        {
            rb.linearVelocity = UnityEngine.Vector2.zero;
            patrolTarget = (patrolTarget == pointA) ? pointB : pointA;
        }
    }

    // ── Persecución ───────────────────────────────────────────────────────
    void HandleChase()
    {
        if (currentTarget == null) { state = State.Patrolling; return; }
        MoveTowards(currentTarget.position, useChaseSpeed: true);
    }

    void MoveTowards(UnityEngine.Vector3 target, bool useChaseSpeed = false)
    {
        float speed = useChaseSpeed ? chaseSpeed : moveSpeed;
        float dir = target.x > transform.position.x ? 1f : -1f;

        rb.linearVelocity = new UnityEngine.Vector2(dir * speed, rb.linearVelocity.y);
        sr.flipX = dir < 0;
    }

    // ── Ataque ────────────────────────────────────────────────────────────
    IEnumerator AttackRoutine()
    {
        state = State.Attacking;
        lastAttackTime = Time.time;
        rb.linearVelocity = UnityEngine.Vector2.zero;

        anim?.SetTrigger(paramAttack);

        // Esperar al frame de daño
        yield return new WaitForSeconds(damageDelay);

        // Aplicar daño si el jugador sigue en rango
        if (currentTarget != null)
        {
            float dist = UnityEngine.Vector2.Distance(transform.position, currentTarget.position);
            if (dist <= attackRange)
            {
                PlayerHealth ph = currentTarget.GetComponentInParent<PlayerHealth>();
                if (ph != null && ph.IsAlive)
                {
                    ph.TakeDamage(attackDamage);

                    // Knockback al jugador
                    Rigidbody2D playerRb = currentTarget.GetComponentInParent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        float kbDir = currentTarget.position.x > transform.position.x ? 1f : -1f;
                        playerRb.linearVelocity = new UnityEngine.Vector2(kbDir * knockbackForce, knockbackForce * 0.5f);
                    }
                }
            }
        }

        // Esperar el resto de la animación de ataque
        yield return new WaitForSeconds(attackCooldown - damageDelay);

        state = currentTarget != null ? State.Chasing : State.Patrolling;
    }

    // ── Recibir daño ──────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (state == State.Dead || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);

        if (currentHP <= 0f) { Die(); return; }

        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        state = State.Hurt;
        isInvincible = true;
        anim?.SetTrigger(paramHurt);

        // Parpadeo
        for (float t = 0; t < invincibilityTime; t += 0.1f)
        {
            sr.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            yield return new WaitForSeconds(0.05f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
        }

        isInvincible = false;
        state = currentTarget != null ? State.Chasing : State.Patrolling;
    }

    void Die()
    {
        state = State.Dead;
        rb.linearVelocity = UnityEngine.Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        anim?.SetTrigger(paramDeath);
        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 2f); // Se destruye tras la animación de muerte
    }

    // ── Animaciones ───────────────────────────────────────────────────────
    void UpdateAnimations()
    {
        bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim?.SetBool(paramWalk, moving);
    }

    // ── Debug visual ──────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}