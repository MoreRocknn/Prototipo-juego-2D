using UnityEngine;
using System.Collections;

public class MainChar : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    private Rigidbody2D rb;
    private float moveInput;

    [Header("Detección")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Pared y salto")]
    public float wallSlideSpeed = 2.5f;
    public Vector2 wallJumpForce = new Vector2(13f, 17f);
    public float wallJumpLockTime = 0.15f;
    public float wallJumpControlTime = 0.25f;
    private float wallJumpCounter = 0f;
    private bool wasWallJumping = false;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isFacingRight = true;
    private int wallSide = 1;

    [Header("Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public int attackDamage = 1;
    public float playerKnockbackForce = 3f;
    public Transform downAttackPoint;
    public GameObject sideAttackEffect;
    public GameObject downAttackEffect;
    private bool isAttackingDown = false;

    [Header("Gravedad / Saltos tipo Hollow Knight")]
    public float fallGravityMultiplier = 2.5f;  // Ajustado para caída más rápida
    public float lowJumpMultiplier = 2.5f;      // Ajustado para mejor control
    public float wallSlideGravityMultiplier = 0.3f; // (Ya no se usa para gravedad, pero se mantiene)
    public float coyoteTime = 0.12f;            // Más generoso como en HK
    public float jumpBufferTime = 0.15f;        // Más generoso como en HK

    [Header("Afinación adicional")]
    public float jumpCutMultiplier = 0.5f;
    public float airControlMultiplier = 1f;
    public float maxFallSpeed = 22f;
    public float wallJumpAirDrag = 0.92f;

    [Header("=== DOWN ATTACK BOUNCE (Hollow Knight) ===")]
    public float downAttackBounceForce = 25f;       // Rebote automático al pegar hacia abajo
    public float downAttackSmallBounceForce = 12f;  // Rebote pequeño (no usado por ahora)
    private bool holdingJumpOnDownAttack = false;   // Control del rebote

    [Header("=== POGO STICK (Saltos consecutivos al suelo) ===")]
    public float pogoJumpForce = 14f;               // Fuerza del pogo jump (AHORA NO SE USA)
    public float pogoWindow = 0.2f;                 // Ventana de tiempo para hacer pogo
    private float lastDownAttackTime = -1f;         // Tiempo del último down attack
    private bool canPogoJump = false;               // Si puede hacer pogo jump

    [Header("=== SISTEMA DE VIDA ===")]
    public int maxHealth = 3;
    public int currentHealth = 3;
    public float damageInvincibilityTime = 1f;
    public Vector2 damageKnockbackForce = new Vector2(5f, 5f);
    public Color damageColor = new Color(1f, 0.3f, 0.3f);
    private bool isDamageInvincible = false;

    private float defaultGravityScale;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpReleased = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;

        currentHealth = maxHealth;

        if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Sistema de salto con buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
            jumpReleased = false;
            holdingJumpOnDownAttack = true;

            // POGO JUMP: Si acabas de hacer down attack y tocas Jump rápido
            // ===================================================================
            // ===          CAMBIO 1: BLOQUE DE POGO JUMP COMENTADO          ===
            // === Esto evita el rebote exagerado al pulsar Jump y Attack    ===
            // ===================================================================
            /*
            if (canPogoJump && Time.time - lastDownAttackTime < pogoWindow)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoJumpForce);
                canPogoJump = false;
                Debug.Log("¡Pogo Jump!");
            }
            */
            // ===================================================================

        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpReleased = true;
            holdingJumpOnDownAttack = false;

            // Jump cut más agresivo (como Hollow Knight)
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        // Sistema de ataque
        isAttackingDown = false;
        if (Input.GetButtonDown("Fire1"))
        {
            if (verticalInput < 0 && !isGrounded)
            {
                isAttackingDown = true;
                lastDownAttackTime = Time.time;
                canPogoJump = true;  // Habilita pogo jump después del down attack
            }
            Attack();
        }

        // ===================================================================
        // ===       CAMBIO 2.A: MODIFICACIÓN GRAVEDAD WALLSLIDE         ===
        // ===================================================================
        // Sistema de gravedad mejorado (como Hollow Knight)
        if (isWallSliding)
        {
            //rb.gravityScale = defaultGravityScale * wallSlideGravityMultiplier; // <- Original
            rb.gravityScale = 0f; // <--- AHORA ES CERO para evitar conflictos
        }
        else if (rb.linearVelocity.y < -0.5f)  // Cayendo
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.5f && !Input.GetButton("Jump"))  // Subiendo sin mantener salto
        {
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
        // ===================================================================


        // Detección de suelo y pared
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (isGrounded)
        {
            wasWallJumping = false;
            canPogoJump = false;  // Reset pogo al tocar suelo normalmente
        }

        // Coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (wallJumpCounter > 0f)
        {
            wallJumpCounter -= Time.deltaTime;
        }


        // ===================================================================
        // ===     CAMBIO 2.B: MODIFICACIÓN DETECCIÓN WALLSLIDE          ===
        // ===================================================================
        // Wall slide
        bool isPushingWall = (moveInput * wallSide > 0);

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && isPushingWall)
        {
            if (verticalInput < 0)
            {
                isWallSliding = false;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
            }
            else
            {
                isWallSliding = true;
                // rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed); // <-- SE QUITA ESTO
            }
        }
        else
        {
            isWallSliding = false;
        }
        // ===================================================================


        // Flip del personaje
        if (wallJumpCounter <= 0.05f)
        {
            if (moveInput < 0 && isFacingRight)
            {
                Flip();
            }
            else if (moveInput > 0 && !isFacingRight)
            {
                Flip();
            }
        }
    }

    void FixedUpdate()
    {
        // ===================================================================
        // ===       CAMBIO 2.C: LÓGICA DE MOVIMIENTO EN FIXEDUPDATE     ===
        // ===================================================================

        // Control durante wall jump
        if (wallJumpCounter > 0f)
        {
            float controlAmount = 1f - (wallJumpCounter / wallJumpLockTime);

            if (wallJumpCounter > wallJumpLockTime)
            {
                return;
            }
            else
            {
                float targetX = moveInput * moveSpeed * controlAmount;
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocity.x, targetX, wallJumpAirDrag),
                    rb.linearVelocity.y
                );
            }
        }
        // --- INICIO DEL BLOQUE AÑADIDO ---
        // Si estamos deslizando, aplicamos velocidad de deslizamiento
        else if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
        }
        // --- FIN DEL BLOQUE AÑADIDO ---

        // Si no (antes era "else if (!isWallSliding)")
        else
        {
            float targetX = moveInput * moveSpeed;
            float appliedX = isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }
        // ===================================================================


        // Sistema de salto con buffer
        if (jumpBufferCounter > 0f && jumpReleased == false)
        {
            if (isTouchingWall && !isGrounded && wallJumpCounter <= 0f)
            {
                bool isPushingTowardsWall = (moveInput * wallSide > 0);

                if (isPushingTowardsWall || Mathf.Abs(moveInput) < 0.1f)
                {
                    rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x * 0.7f, wallJumpForce.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y * 0.95f);
                }

                wallJumpCounter = wallJumpControlTime;
                wasWallJumping = true;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false;
            }
            else if (coyoteTimeCounter > 0f && !wasWallJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
            else
            {
                jumpBufferCounter = 0f;
            }
        }

        // Limitar velocidad de caída
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        wallSide *= -1;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    public void TakeDamage(int damage)
    {
        if (isDamageInvincible)
        {
            Debug.Log("Jugador invencible, daño ignorado");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Jugador recibió {damage} de daño. Vida: {currentHealth}/{maxHealth}");

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float knockbackDir = 1f;

        if (enemies.Length > 0)
        {
            float closestDist = Mathf.Infinity;
            GameObject closestEnemy = null;

            foreach (GameObject enemy in enemies)
            {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                knockbackDir = transform.position.x > closestEnemy.transform.position.x ? 1f : -1f;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(knockbackDir * damageKnockbackForce.x, damageKnockbackForce.y);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageInvincibility());
        }
    }

    IEnumerator DamageInvincibility()
    {
        isDamageInvincible = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        float flashInterval = damageInvincibilityTime / 10f;
        for (int i = 0; i < 5; i++)
        {
            if (sr != null) sr.color = damageColor;
            yield return new WaitForSeconds(flashInterval);
            if (sr != null) sr.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        isDamageInvincible = false;
        Debug.Log("Invencibilidad terminada");
    }

    void Die()
    {
        Debug.Log("¡Jugador murió!");

        if (GameManager.Instance != null)
        {
            StartCoroutine(RespawnAfterDeath());
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    IEnumerator RespawnAfterDeath()
    {
        enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(1f);

        currentHealth = maxHealth;

        if (GameManager.Instance != null)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
        }

        if (sr != null) sr.enabled = true;
        rb.linearVelocity = Vector2.zero;
        isDamageInvincible = false;
        enabled = true;

        Debug.Log("Jugador respawneado");
    }

    void Attack()
    {
        GameObject effectToShow = isAttackingDown ? downAttackEffect : sideAttackEffect;

        if (effectToShow != null)
        {
            ParticleSystem ps = effectToShow.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }

        if (!isAttackingDown)
        {
            float knockbackDir = isFacingRight ? -1 : 1;
            rb.AddForce(new Vector2(knockbackDir * playerKnockbackForce, 0), ForceMode2D.Impulse);
        }

        // ===== SISTEMA DE REBOTE AL ATACAR HACIA ABAJO (Como Hollow Knight) =====
        if (isAttackingDown)
        {
            Transform currentAttackPoint = downAttackPoint;

            // Detectar enemigos
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

            // Detectar suelo/plataformas
            Collider2D[] hitGround = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, groundLayer);

            bool hitSomething = false;

            // Daño a enemigos
            foreach (Collider2D enemyCollider in hitEnemies)
            {
                Enemigo enemy = enemyCollider.GetComponent<Enemigo>();
                if (enemy != null)
                {
                    hitSomething = true;
                    int enemyKnockbackDir = isFacingRight ? 1 : -1;
                    enemy.TakeDamage(attackDamage, enemyKnockbackDir);
                }
            }

            // Detectar si pegamos al suelo
            if (hitGround.Length > 0)
            {
                hitSomething = true;
                Debug.Log("¡Pegaste al suelo!");
            }

            // REBOTE AUTOMÁTICO (funciona tanto con enemigos como con suelo)
            if (hitSomething)
            {
                // Rebote automático sin necesidad de mantener Jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce);
                Debug.Log("¡Rebote automático!");

                canPogoJump = true;  // Habilita el pogo jump
                lastDownAttackTime = Time.time;
            }
        }
        else
        {
            // Ataque lateral (código original)
            Transform currentAttackPoint = attackPoint;
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

            foreach (Collider2D enemyCollider in hitEnemies)
            {
                Enemigo enemy = enemyCollider.GetComponent<Enemigo>();
                if (enemy != null)
                {
                    int enemyKnockbackDir = isFacingRight ? 1 : -1;
                    enemy.TakeDamage(attackDamage, enemyKnockbackDir);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.blue;
        if (wallCheck != null)
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);

        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        Gizmos.color = Color.yellow;
        if (downAttackPoint != null)
            Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
    }
}