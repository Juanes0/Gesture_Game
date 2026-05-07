using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Pinchos - Daño por segundo")]
    public float spikeDamagePerSecond = 25f;
    public float invincibilityAfterDamage = 1f;

    [Header("Respawn")]
    public float respawnDelay = 3f;

    [Header("Referencias")]
    public HealthUI healthUI;

    [Header("Animaciones")]
    public string hurtTrigger = "hurt";     // Nombre exacto del Trigger en el Animator
    public string deathTrigger = "death";      // Nombre exacto del Bool en el Animator

    public bool IsAlive { get; private set; } = true;

    private bool isInvincible = false;
    private GestureActions gestureActions;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;                  // ← NUEVO
    private Color originalColor;

    void Awake()
    {
        gestureActions = GetComponent<GestureActions>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();    // ← NUEVO
        currentHP = maxHP;
        originalColor = sr.color;
    }

    void Start()
    {
        healthUI?.UpdateUI(currentHP, maxHP);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);
        healthUI?.UpdateUI(currentHP, maxHP);

        // Dispara animación de daño
        anim?.SetTrigger(hurtTrigger);

        StartCoroutine(InvincibilityFrames());

        if (currentHP <= 0f)
            Die();
    }

    public void InstantDie()
    {
        if (!IsAlive) return;
        currentHP = 0f;
        healthUI?.UpdateUI(currentHP, maxHP);
        Die();
    }

    void Die()
    {
        IsAlive = false;
        gestureActions.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        // Activa animación de muerte
        anim?.SetTrigger(deathTrigger);


        GameManager.Instance.OnPlayerDied(this);
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GameManager.Instance.GetRespawnPosition(this);
        transform.position = spawnPos;

        currentHP = maxHP;
        IsAlive = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        gestureActions.enabled = true;

        anim?.Play("Idle"); 

        healthUI?.UpdateUI(currentHP, maxHP);
        GameManager.Instance.OnPlayerRespawned(this);
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        // El parpadeo ahora es más sutil porque la animación hurt ya da feedback
        for (float t = 0; t < invincibilityAfterDamage; t += 0.2f)
        {
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.4f);
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor;   // ← Restaura el color original, no blanco
        yield return new WaitForSeconds(0.1f);
        }
        isInvincible = false;
    }
}