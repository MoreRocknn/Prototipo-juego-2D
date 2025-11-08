using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int health = 3;
    public float invincibilityTime = 0.5f;
    private bool isInvincible = false;
    public Vector2 knockbackForce = new Vector2(3f, 5f);

    [Header("=== PROTECCIÓN ANTI-VUELO ===")]
    public float knockbackInvincibilityTime = 0.3f; // Invencibilidad tras recibir hit
    private bool isKnockbackInvincible = false;     // Evita que el jugador abuse del knockback

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer;
    public Transform detectionPoint;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;

    [Header("=== ATAQUE ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;

    [Header("=== PATRULLA (opcional) ===")]
    public bool shouldPatrol = true;
    public float patrolDistance = 5f;
    public float waitTimeAtPatrolPoint = 2f;

    [Header("=== EFECTOS VISUALES ===")]
    public GameObject guardEffect;
    public GameObject attackEffect;
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;

    [Header("=== DEBUG ===")]
    public bool showDebugGizmos = true;

    private enum EnemyState
    {
        Idle,
        Patrol,
        Guard,
        Chase,
        Attack,
        Stunned
    }

    private EnemyState currentState = EnemyState.Idle;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private Animator animator;

    private bool isFacingRight = true;
    private float attackTimer = 0f;
    private float guardTimer = 0f;
    private bool isAttacking = false;
    private Vector2 startPosition;
    private float patrolTarget;
    private bool movingRight = true;
    private float waitTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemigo {gameObject.name} encontró al jugador");
        }
        else
        {
            Debug.LogError("¡No se encontró GameObject con Tag 'Player'!");
        }

        startPosition = transform.position;
        patrolTarget = startPosition.x + patrolDistance;

        if (detectionPoint == null)
        {
            detectionPoint = transform;
        }

        if (shouldPatrol)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    void Update()
    {
        if (isInvincible || currentState == EnemyState.Stunned)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        Vector3 detectionPos = detectionPoint != null ? detectionPoint.position : transform.position;
        float distanceToPlayer = player != null ? Vector2.Distance(detectionPos, player.position) : Mathf.Infinity;
        bool playerDetected = distanceToPlayer <= detectionRange;
        bool playerInAttackRange = distanceToPlayer <= attackRange;

        // SIEMPRE mirar hacia el jugador si está detectado
        if (playerDetected && player != null && currentState != EnemyState.Attack)
        {
            LookAtPlayer();
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(playerDetected, distanceToPlayer);
                break;

            case EnemyState.Patrol:
                HandlePatrol(playerDetected, distanceToPlayer);
                break;

            case EnemyState.Guard:
                HandleGuard(playerDetected, playerInAttackRange);
                break;

            case EnemyState.Chase:
                HandleChase(playerDetected, playerInAttackRange);
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }

        UpdateAnimations();
    }

    void HandleIdle(bool playerDetected, float distance)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (playerDetected)
        {
            EnterGuardState();
        }
    }

    void HandlePatrol(bool playerDetected, float distance)
    {
        if (playerDetected)
        {
            EnterGuardState();
            return;
        }

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if (movingRight && !isFacingRight)
        {
            Flip();
        }
        else if (!movingRight && isFacingRight)
        {
            Flip();
        }

        if (movingRight && transform.position.x >= patrolTarget)
        {
            movingRight = false;
            waitTimer = waitTimeAtPatrolPoint;
        }
        else if (!movingRight && transform.position.x <= startPosition.x - patrolDistance)
        {
            movingRight = true;
            waitTimer = waitTimeAtPatrolPoint;
        }
    }

    void HandleGuard(bool playerDetected, bool inAttackRange)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (!playerDetected)
        {
            ExitGuardState();
            if (shouldPatrol)
                currentState = EnemyState.Patrol;
            else
                currentState = EnemyState.Idle;
            return;
        }

        LookAtPlayer();

        guardTimer += Time.deltaTime;

        if (inAttackRange && guardTimer >= guardTime && attackTimer <= 0)
        {
            ExitGuardState();
            EnterAttackState();
        }
        else if (!inAttackRange && guardTimer >= guardTime * 0.5f)
        {
            ExitGuardState();
            currentState = EnemyState.Chase;
        }
    }

    void HandleChase(bool playerDetected, bool inAttackRange)
    {
        if (!playerDetected)
        {
            if (shouldPatrol)
                currentState = EnemyState.Patrol;
            else
                currentState = EnemyState.Idle;
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);

        LookAtPlayer();
        if (inAttackRange)
        {
            if (attackTimer <= 0)
            {
                EnterAttackState();
            }
            else
            {
                EnterGuardState();
            }
        }
    }

    void HandleAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    void EnterGuardState()
    {
        currentState = EnemyState.Guard;
        guardTimer = 0f;

        if (guardEffect != null)
        {
            guardEffect.SetActive(true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = guardColor;
        }

        Debug.Log($"{gameObject.name}: ¡En GUARDIA!");
    }

    void ExitGuardState()
    {
        if (guardEffect != null)
        {
            guardEffect.SetActive(false);
        }

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = Color.white;
        }
    }

    void EnterAttackState()
    {
        currentState = EnemyState.Attack;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = attackColor;
        }

        Debug.Log($"{gameObject.name}: ¡ATACA!");
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, attackPoint.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, PlayerLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                MainChar playerScript = hit.GetComponent<MainChar>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(attackDamage);
                    Debug.Log($"{gameObject.name}: ¡Golpeó al jugador por {attackDamage} de daño!");
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackTimer = attackCooldown;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = Color.white;
        }

        EnterGuardState();
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        bool playerOnRight = player.position.x > transform.position.x;

        if (playerOnRight && !isFacingRight)
        {
            Flip();
        }
        else if (!playerOnRight && isFacingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        Debug.Log($"{gameObject.name}: Volteado - FacingRight={isFacingRight}");
    }

    public void TakeDamage(int damage, float knockbackDirection)
    {
        // ============================================
        // ARREGLADO: Protección anti-vuelo
        // ============================================
        if (isInvincible || isKnockbackInvincible)
        {
            Debug.Log($"{gameObject.name}: Invencible - Hit ignorado");
            return;
        }

        health -= damage;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            float forceX = knockbackForce.x * knockbackDirection;
            float forceY = knockbackForce.y;
            rb.linearVelocity = new Vector2(forceX, forceY);
        }

        ExitGuardState();
        currentState = EnemyState.Stunned;

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
            StartCoroutine(KnockbackInvincibility()); // NUEVO: Evita spam de hits
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}: Murió");
        Destroy(gameObject);
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(invincibilityTime / 10);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(invincibilityTime / 10);
        }

        isInvincible = false;
        currentState = EnemyState.Idle;
    }

    // ============================================
    // NUEVO: Invencibilidad temporal al knockback
    // ============================================
    private IEnumerator KnockbackInvincibility()
    {
        isKnockbackInvincible = true;
        Debug.Log($"{gameObject.name}: Knockback invincibility activada");

        yield return new WaitForSeconds(knockbackInvincibilityTime);

        isKnockbackInvincible = false;
        Debug.Log($"{gameObject.name}: Knockback invincibility terminada");
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("isGuarding", currentState == EnemyState.Guard);
            animator.SetBool("isAttacking", currentState == EnemyState.Attack);
            animator.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 detectionPos = detectionPoint != null ? detectionPoint.position : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionPos, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPos, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (shouldPatrol && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPosition - Vector2.right * patrolDistance, startPosition + Vector2.right * patrolDistance);
        }
    }
}