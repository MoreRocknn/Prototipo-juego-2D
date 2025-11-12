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
    public float knockbackInvincibilityTime = 0.3f;
    private bool isKnockbackInvincible = false;

    [Header("=== DETECCIÓN DE BORDES ===")]
    public Transform edgeCheckPoint;
    public float edgeCheckDistance = 0.5f;
    public LayerMask groundLayer;
    public bool showEdgeDebug = true;
    public float edgeCheckOffset = 0.8f;
    private bool isAtEdge = false;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer;
    public LayerMask wallLayer;
    public Transform detectionPoint;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float edgeWaitTime = 3f;
    public float chaseTimeout = 5f;
    public float maxEdgeWaitTime = 10f;

    [Header("=== ATAQUE ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;

    [Header("=== PATRULLA ===")]
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

    // Componentes cacheados
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private Animator animator;
    private Color originalColor;

    // Estado
    private bool isFacingRight = true;
    private float attackTimer = 0f;
    private float guardTimer = 0f;
    private bool isAttacking = false;
    private Vector2 startPosition;
    private float patrolTarget;
    private bool movingRight = true;
    private float waitTimer = 0f;
    private float edgeWaitTimer = 0f;
    private float chaseTimer = 0f;
    private float totalEdgeTime = 0f;
    private float playerIgnoreTimer = 0f;

    void Start()
    {
        // Cachear componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Buscar jugador
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

        // Inicializar patrulla
        startPosition = transform.position;
        patrolTarget = startPosition.x + patrolDistance;

        // Crear detection point si no existe
        if (detectionPoint == null)
        {
            detectionPoint = transform;
        }

        // Crear edge check point si no existe
        if (edgeCheckPoint == null)
        {
            GameObject edgeCheck = new GameObject("EdgeCheckPoint");
            edgeCheck.transform.SetParent(transform);
            edgeCheck.transform.localPosition = new Vector3(edgeCheckOffset, -0.5f, 0);
            edgeCheckPoint = edgeCheck.transform;
            Debug.LogWarning($"{gameObject.name}: EdgeCheckPoint creado automáticamente. Ajusta su altura (Y) en el inspector.");
        }

        // Estado inicial
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void Update()
    {
        // No hacer nada si está invencible o aturdido
        if (isInvincible || currentState == EnemyState.Stunned)
        {
            return;
        }

        // Actualizar timers
        playerIgnoreTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        // Verificar borde
        CheckEdge();

        // Calcular detección del jugador
        Vector3 detectionPos = detectionPoint.position;
        float distanceToPlayer = player != null ? Vector2.Distance(detectionPos, player.position) : Mathf.Infinity;

        bool canSeePlayer = CanSeePlayer(detectionPos, distanceToPlayer);
        bool playerDetected = canSeePlayer && (playerIgnoreTimer <= 0f);
        bool playerInAttackRange = playerDetected && distanceToPlayer <= attackRange;

        // Mirar al jugador si está detectado
        if (playerDetected && player != null && currentState != EnemyState.Attack)
        {
            LookAtPlayer();
        }

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(playerDetected);
                break;

            case EnemyState.Patrol:
                HandlePatrol(playerDetected);
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

    bool CanSeePlayer(Vector3 detectionPos, float distance)
    {
        if (player == null || distance > detectionRange)
        {
            return false;
        }

        Vector2 directionToPlayer = (player.position - detectionPos).normalized;
        RaycastHit2D hit = Physics2D.Raycast(detectionPos, directionToPlayer, distance, wallLayer | PlayerLayer);

        if (hit.collider != null)
        {
            bool canSee = hit.collider.CompareTag("Player");

            if (showDebugGizmos)
            {
                Debug.DrawLine(detectionPos, hit.point, canSee ? Color.green : Color.red);
            }

            return canSee;
        }

        return false;
    }

    void CheckEdge()
    {
        if (edgeCheckPoint == null) return;

        float direction = isFacingRight ? 1f : -1f;
        Vector2 checkStartPoint = new Vector2(
            transform.position.x + (edgeCheckOffset * direction),
            edgeCheckPoint.position.y
        );

        RaycastHit2D hit = Physics2D.Raycast(checkStartPoint, Vector2.down, edgeCheckDistance, groundLayer);
        isAtEdge = (hit.collider == null);

        if (showEdgeDebug)
        {
            Debug.DrawRay(checkStartPoint, Vector2.down * edgeCheckDistance, isAtEdge ? Color.red : Color.cyan);
        }
    }

    void HandleIdle(bool playerDetected)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (playerDetected)
        {
            EnterGuardState();
        }
    }

    void HandlePatrol(bool playerDetected)
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

        if (isAtEdge)
        {
            Debug.Log($"{gameObject.name}: Borde detectado, cambiando dirección");
            movingRight = !movingRight;
            waitTimer = waitTimeAtPatrolPoint;
            Flip();
            return;
        }

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // Ajustar dirección visual
        if ((movingRight && !isFacingRight) || (!movingRight && isFacingRight))
        {
            Flip();
        }

        // Verificar límites de patrulla
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

        // Si el jugador se aleja, volver a patrullar
        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Jugador fuera de rango");
            ReturnToPatrol();
            return;
        }

        LookAtPlayer();
        guardTimer += Time.deltaTime;

        // Contador de tiempo en el borde
        if (isAtEdge)
        {
            totalEdgeTime += Time.deltaTime;

            // Si pasaron 10 segundos en el borde, volver a patrullar
            if (totalEdgeTime >= maxEdgeWaitTime)
            {
                Debug.Log($"{gameObject.name}: Tiempo máximo en borde alcanzado");
                TurnAroundAndPatrol();
                return;
            }
        }
        else
        {
            totalEdgeTime = 0f;
        }

        // Intentar atacar si está en rango
        if (inAttackRange && guardTimer >= guardTime && attackTimer <= 0)
        {
            ExitGuardState();
            EnterAttackState();
            ResetTimers();
        }
        // Si no está en rango de ataque pero ya esperó
        else if (!inAttackRange && guardTimer >= guardTime)
        {
            if (!isAtEdge)
            {
                // Perseguir
                ExitGuardState();
                currentState = EnemyState.Chase;
                chaseTimer = 0f;
                totalEdgeTime = 0f;
            }
            else
            {
                edgeWaitTimer += Time.deltaTime;

                // Después de 3 segundos en el borde, intentar volver a patrullar
                if (edgeWaitTimer >= edgeWaitTime)
                {
                    Debug.Log($"{gameObject.name}: Esperó {edgeWaitTime}s en el borde");
                    TurnAroundAndPatrol();
                }
            }
        }

        if (!isAtEdge)
        {
            edgeWaitTimer = 0f;
        }
    }

    void HandleChase(bool playerDetected, bool playerInAttackRange)
    {
        chaseTimer += Time.deltaTime;

        // Timeout de persecución
        if (chaseTimer >= chaseTimeout)
        {
            Debug.Log($"{gameObject.name}: Timeout de persecución alcanzado");
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            ReturnToPatrol();
            return;
        }

        // Perder de vista al jugador
        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Perdió de vista al jugador");
            ReturnToPatrol();
            return;
        }

        // Borde durante persecución
        if (isAtEdge)
        {
            Debug.Log($"{gameObject.name}: Borde detectado durante persecución");
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            LookAtPlayer();
            EnterGuardState();
            return;
        }

        // Perseguir
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);

        LookAtPlayer();

        // Intentar atacar si está en rango
        if (playerInAttackRange)
        {
            if (attackTimer <= 0)
            {
                chaseTimer = 0f;
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

        Debug.Log($"{gameObject.name}: En Guardia");
    }

    void ExitGuardState()
    {
        if (guardEffect != null)
        {
            guardEffect.SetActive(false);
        }

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
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

        Debug.Log($"{gameObject.name}: Atacando");
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        if (attackEffect != null && attackPoint != null)
        {
            GameObject effect = Instantiate(attackEffect, attackPoint.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, PlayerLayer);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    MainChar playerScript = hit.GetComponent<MainChar>();
                    if (playerScript != null)
                    {
                        playerScript.TakeDamage(attackDamage);
                        Debug.Log($"{gameObject.name}: Golpeó al jugador por {attackDamage} de daño");
                    }
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackTimer = attackCooldown;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }

        EnterGuardState();
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        bool playerOnRight = player.position.x > transform.position.x;

        if ((playerOnRight && !isFacingRight) || (!playerOnRight && isFacingRight))
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

        Debug.Log($"{gameObject.name}: Volteado - Mirando {(isFacingRight ? "derecha" : "izquierda")}");
    }

    void ReturnToPatrol()
    {
        ExitGuardState();
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
        ResetTimers();
    }

    void TurnAroundAndPatrol()
    {
        ExitGuardState();
        movingRight = !isFacingRight;
        Flip();
        waitTimer = waitTimeAtPatrolPoint;
        currentState = EnemyState.Patrol;
        ResetTimers();
        playerIgnoreTimer = waitTimeAtPatrolPoint + 2f;
    }

    void ResetTimers()
    {
        edgeWaitTimer = 0f;
        chaseTimer = 0f;
        totalEdgeTime = 0f;
    }

    public void TakeDamage(int damage, float knockbackDirection)
    {
        if (isInvincible || isKnockbackInvincible)
        {
            Debug.Log($"{gameObject.name}: Invencible - Daño ignorado");
            return;
        }

        health -= damage;
        Debug.Log($"{gameObject.name}: Recibió {damage} de daño. Vida: {health}");

        // Aplicar knockback
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(
                knockbackForce.x * knockbackDirection,
                knockbackForce.y
            );
        }

        ExitGuardState();
        currentState = EnemyState.Stunned;
        ResetTimers();

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
            StartCoroutine(KnockbackInvincibility());
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}: Murió");
        Destroy(gameObject);
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float flashDuration = invincibilityTime / 10f;

        for (int i = 0; i < 5; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isInvincible = false;
        currentState = EnemyState.Idle;
    }

    IEnumerator KnockbackInvincibility()
    {
        isKnockbackInvincible = true;
        yield return new WaitForSeconds(knockbackInvincibilityTime);
        isKnockbackInvincible = false;
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

        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionPos, detectionRange);

        // Rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPos, attackRange);

        // Punto de ataque
        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        // Zona de patrulla
        if (shouldPatrol && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector2(startPosition.x - patrolDistance, startPosition.y),
                new Vector2(startPosition.x + patrolDistance, startPosition.y)
            );
        }

        // Detección de bordes
        if (edgeCheckPoint != null)
        {
            float direction = isFacingRight ? 1f : -1f;
            Vector2 checkStartPoint = new Vector2(
                transform.position.x + (edgeCheckOffset * direction),
                edgeCheckPoint.position.y
            );

            Gizmos.color = isAtEdge ? Color.red : Color.cyan;
            Gizmos.DrawLine(checkStartPoint, checkStartPoint + Vector2.down * edgeCheckDistance);
            Gizmos.DrawWireSphere(checkStartPoint + Vector2.down * edgeCheckDistance, 0.1f);
        }

        // Raycast de detección del jugador
        if (Application.isPlaying && player != null)
        {
            Vector2 directionToPlayer = (player.position - detectionPos).normalized;
            float distance = Vector2.Distance(detectionPos, player.position);

            if (distance <= detectionRange)
            {
                RaycastHit2D hit = Physics2D.Raycast(detectionPos, directionToPlayer, distance, wallLayer | PlayerLayer);

                if (hit.collider != null)
                {
                    Gizmos.color = hit.collider.CompareTag("Player") ? Color.green : Color.red;
                    Gizmos.DrawLine(detectionPos, hit.point);
                }
            }
        }
    }
}