using System.Collections;
using UnityEngine;

public class EnemigoVolador : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int health = 3;
    public float invincibilityTime = 0.5f;
    private bool isInvincible = false;
    public Vector2 knockbackForce = new Vector2(4f, 3f);

    [Header("=== PROTECCIÓN ANTI-VUELO ===")]
    public float knockbackInvincibilityTime = 0.3f;
    private bool isKnockbackInvincible = false;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;
    public LayerMask PlayerLayer;
    public LayerMask wallLayer;
    public Transform detectionPoint;

    [Header("=== COMPORTAMIENTO DE VUELO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float fleeSpeed = 7f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float repositionTime = 2f; // Tiempo que huye antes de volver a atacar
    public float repositionDistance = 5f; // Distancia mínima de reposición

    [Header("=== MOVIMIENTO VERTICAL ===")]
    public float hoverHeight = 0.5f; // Altura adicional sobre el jugador
    public float verticalSpeed = 4f;
    public float smoothTime = 0.3f; // Suavizado del movimiento

    [Header("=== PATRULLA AÉREA ===")]
    public bool shouldPatrol = true;
    public Vector2 patrolAreaSize = new Vector2(8f, 4f); // Área de patrulla
    public float waitTimeAtPatrolPoint = 2f;
    public float patrolPointRadius = 0.5f; // Qué tan cerca debe estar del punto para considerarlo alcanzado

    [Header("=== ATAQUE ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;

    [Header("=== EFECTOS VISUALES ===")]
    public GameObject guardEffect;
    public GameObject attackEffect;
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;
    public Color fleeColor = new Color(1f, 0.5f, 0f); // Naranja

    [Header("=== DEBUG ===")]
    public bool showDebugGizmos = true;

    private enum EnemyState
    {
        Idle,
        Patrol,
        Guard,
        Alert,
        Chase,
        Attack,
        Flee,
        Reposition
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
    private Vector2 currentPatrolTarget;
    private float waitTimer = 0f;
    private float fleeTimer = 0f;
    private Vector2 fleeDirection;
    private Vector2 velocitySmooth = Vector2.zero;
    private bool hasReachedRepositionDistance = false;

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

        // Configurar Rigidbody2D para vuelo
        if (rb != null)
        {
            rb.gravityScale = 0f; // Sin gravedad
        }

        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemigo Volador {gameObject.name} encontró al jugador");
        }
        else
        {
            Debug.LogError("¡No se encontró GameObject con Tag 'Player'!");
        }

        // Inicializar patrulla
        startPosition = transform.position;
        GenerateNewPatrolPoint();

        // Crear detection point si no existe
        if (detectionPoint == null)
        {
            detectionPoint = transform;
        }

        // Crear attack point si no existe
        if (attackPoint == null)
        {
            GameObject attackPt = new GameObject("AttackPoint");
            attackPt.transform.SetParent(transform);
            attackPt.transform.localPosition = new Vector3(1f, 0f, 0f);
            attackPoint = attackPt.transform;
        }

        // Estado inicial
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void Update()
    {
        // No hacer nada si está invencible en ciertas condiciones
        if (isInvincible && currentState != EnemyState.Flee && currentState != EnemyState.Reposition)
        {
            return;
        }

        // Actualizar timers
        attackTimer -= Time.deltaTime;

        // Calcular detección del jugador
        Vector3 detectionPos = detectionPoint.position;
        float distanceToPlayer = player != null ? Vector2.Distance(detectionPos, player.position) : Mathf.Infinity;

        bool canSeePlayer = CanSeePlayer(detectionPos, distanceToPlayer);
        bool playerInAttackRange = canSeePlayer && distanceToPlayer <= attackRange;

        // Mirar al jugador si está detectado (excepto en estados especiales)
        if (canSeePlayer && player != null && currentState != EnemyState.Attack &&
            currentState != EnemyState.Flee && currentState != EnemyState.Reposition)
        {
            LookAtPlayer();
        }

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(canSeePlayer);
                break;

            case EnemyState.Patrol:
                HandlePatrol(canSeePlayer);
                break;

            case EnemyState.Guard:
                HandleGuard(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Alert:
                HandleAlert(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Chase:
                HandleChase(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;

            case EnemyState.Flee:
                HandleFlee();
                break;

            case EnemyState.Reposition:
                HandleReposition(canSeePlayer);
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

    void HandleIdle(bool playerDetected)
    {
        rb.linearVelocity = Vector2.zero;

        if (playerDetected)
        {
            EnterAlertState();
        }
    }

    void HandlePatrol(bool playerDetected)
    {
        if (playerDetected)
        {
            EnterAlertState();
            return;
        }

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Evitar quedarse muy bajo
        float minHeight = startPosition.y - patrolAreaSize.y / 2f;
        if (transform.position.y < minHeight)
        {
            Vector2 upwardDirection = Vector2.up;
            rb.linearVelocity = upwardDirection * moveSpeed;
            return;
        }

        // Moverse hacia el punto de patrulla
        Vector2 direction = (currentPatrolTarget - (Vector2)transform.position).normalized;
        Vector2 targetVelocity = direction * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime);

        // Voltear según dirección
        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }

        // Verificar si alcanzó el punto de patrulla
        if (Vector2.Distance(transform.position, currentPatrolTarget) < patrolPointRadius)
        {
            waitTimer = waitTimeAtPatrolPoint;
            GenerateNewPatrolPoint();
        }
    }

    void HandleGuard(bool playerDetected, bool inAttackRange)
    {
        // Mantenerse a la altura del jugador mientras vigila
        if (player != null)
        {
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(transform.position.x, targetY);
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = new Vector2(0, direction.y * verticalSpeed * 0.5f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Jugador fuera de rango");
            ReturnToPatrol();
            return;
        }

        LookAtPlayer();
        guardTimer += Time.deltaTime;

        if (inAttackRange && guardTimer >= guardTime && attackTimer <= 0)
        {
            ExitGuardState();
            EnterAttackState();
            guardTimer = 0f;
        }
        else if (!inAttackRange && guardTimer >= guardTime)
        {
            ExitGuardState();
            EnterChaseState();
        }
    }

    void HandleAlert(bool playerDetected, bool inAttackRange)
    {
        // Estado de alerta - se acerca al jugador ajustando altura
        if (!playerDetected)
        {
            ReturnToPatrol();
            return;
        }

        if (player != null)
        {
            // Moverse hacia el jugador y ajustar altura simultáneamente
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(player.position.x, targetY);
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

            // Velocidad más rápida en alerta para acercarse
            Vector2 targetVelocity = direction * (chaseSpeed * 0.8f);
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime * 0.5f);

            LookAtPlayer();

            // Cambiar a guardia cuando esté más cerca
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange * 0.6f)
            {
                EnterGuardState();
            }
        }
    }

    void HandleChase(bool playerDetected, bool playerInAttackRange)
    {
        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Perdió de vista al jugador");
            ReturnToPatrol();
            return;
        }

        if (player != null)
        {
            // Calcular posición objetivo a la altura del jugador
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(player.position.x, targetY);

            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 targetVelocity = direction * chaseSpeed;
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime);

            LookAtPlayer();

            if (playerInAttackRange && attackTimer <= 0)
            {
                EnterAttackState();
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

    void HandleFlee()
    {
        fleeTimer += Time.deltaTime;

        // Huir en dirección opuesta al jugador
        Vector2 targetVelocity = fleeDirection * fleeSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime * 0.5f);

        // Verificar si alcanzó distancia segura
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer >= repositionDistance)
            {
                hasReachedRepositionDistance = true;
            }
        }

        // Después del tiempo de huida y alcanzó distancia segura, reposicionarse
        if (fleeTimer >= repositionTime && hasReachedRepositionDistance)
        {
            EnterRepositionState();
        }
    }

    void HandleReposition(bool playerDetected)
    {
        // Detenerse y prepararse para volver a atacar
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);

        if (rb.linearVelocity.magnitude < 0.1f)
        {
            if (playerDetected)
            {
                EnterAlertState();
            }
            else
            {
                ReturnToPatrol();
            }
        }
    }

    void EnterAlertState()
    {
        currentState = EnemyState.Alert;
        guardTimer = 0f;
        Debug.Log($"{gameObject.name}: ¡Alerta! Jugador detectado");
    }

    void EnterGuardState()
    {
        currentState = EnemyState.Guard;
        guardTimer = 0f;

        if (guardEffect != null)
        {
            guardEffect.SetActive(true);
        }

        if (spriteRenderer != null && !isInvincible)
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

    void EnterChaseState()
    {
        currentState = EnemyState.Chase;
        Debug.Log($"{gameObject.name}: Persiguiendo");
    }

    void EnterAttackState()
    {
        currentState = EnemyState.Attack;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = attackColor;
        }

        Debug.Log($"{gameObject.name}: Atacando");
    }

    void EnterFleeState()
    {
        currentState = EnemyState.Flee;
        fleeTimer = 0f;
        hasReachedRepositionDistance = false;

        // Calcular dirección de huida (opuesta al jugador)
        if (player != null)
        {
            fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
        }
        else
        {
            fleeDirection = isFacingRight ? Vector2.left : Vector2.right;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = fleeColor;
        }

        Debug.Log($"{gameObject.name}: ¡Huyendo para reposicionarse!");
    }

    void EnterRepositionState()
    {
        currentState = EnemyState.Reposition;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log($"{gameObject.name}: Reposicionándose");
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Instanciar efecto de ataque
        if (attackEffect != null && attackPoint != null)
        {
            GameObject effect = Instantiate(attackEffect, attackPoint);
            effect.transform.localPosition = Vector3.zero;

            float effectScale = 0.7f;
            effect.transform.localScale = new Vector3(
                (isFacingRight ? 1f : -1f) * effectScale,
                effectScale,
                effectScale
            );

            effect.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10;
            }

            Destroy(effect, attackDuration + 0.5f);
        }

        // Realizar el ataque
        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);

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
    }

    void ReturnToPatrol()
    {
        ExitGuardState();
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
        guardTimer = 0f;
    }

    void GenerateNewPatrolPoint()
    {
        // Generar punto aleatorio dentro del área de patrulla
        float randomX = startPosition.x + Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f);
        // Asegurar que el punto no esté muy bajo
        float minY = startPosition.y - patrolAreaSize.y / 4f; // Solo la parte superior del área
        float maxY = startPosition.y + patrolAreaSize.y / 2f;
        float randomY = Random.Range(minY, maxY);
        currentPatrolTarget = new Vector2(randomX, randomY);

        Debug.Log($"{gameObject.name}: Nuevo punto de patrulla en {currentPatrolTarget}");
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

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Después del daño, huir para reposicionarse
            StartCoroutine(FlashAndFlee());
            StartCoroutine(KnockbackInvincibility());
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}: Murió");
        // Aquí puedes añadir efectos de muerte, puntuación, etc.
        Destroy(gameObject);
    }

    IEnumerator FlashAndFlee()
    {
        isInvincible = true;

        float flashDuration = invincibilityTime / 10f;

        // Efecto visual de parpadeo en rojo
        for (int i = 0; i < 5; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isInvincible = false;

        // Entrar en modo huida
        EnterFleeState();
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
            animator.SetBool("isFleeing", currentState == EnemyState.Flee);
            animator.SetFloat("speed", rb.linearVelocity.magnitude);
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

        // Área de patrulla
        if (shouldPatrol)
        {
            Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(patrolAreaSize.x, patrolAreaSize.y, 0f));

            // Punto de patrulla actual
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentPatrolTarget, patrolPointRadius);
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
            }
        }

        // Distancia de reposición
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(detectionPos, repositionDistance);

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