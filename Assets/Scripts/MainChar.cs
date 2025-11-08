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

    [Header("Wall Grab (Clic Derecho)")]
    public bool canWallGrab = true;
    public KeyCode wallGrabKey = KeyCode.LeftShift;  // Cambiar a Shift por defecto
    public float wallGrabStaminaMax = 3f;
    private float wallGrabStamina;
    private bool isWallGrabbing = false;

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
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.5f;
    public float wallSlideGravityMultiplier = 0.3f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("Afinación adicional")]
    public float jumpCutMultiplier = 0.5f;
    public float airControlMultiplier = 1f;
    public float maxFallSpeed = 22f;
    public float wallJumpAirDrag = 0.92f;

    [Header("=== DOWN ATTACK BOUNCE (Hollow Knight) ===")]
    public float downAttackBounceForce = 25f;
    public float downAttackSmallBounceForce = 12f;

    [Header("=== LÍMITE DE REBOTES CONSECUTIVOS ===")]
    public int maxConsecutiveBounces = 3;
    public float bounceResetTime = 0.5f;
    private int consecutiveBounces = 0;
    private float lastBounceTime = -1f;

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

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;
        wallGrabStamina = wallGrabStaminaMax;

        if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
            jumpReleased = false;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpReleased = true;

            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        isAttackingDown = false;
        if (Input.GetButtonDown("Fire1"))
        {
            if (verticalInput < 0 && !isGrounded)
            {
                isAttackingDown = true;
            }
            Attack();
        }

        // Sistema de gravedad con Wall Grab
        if (isWallGrabbing)
        {
            rb.gravityScale = 0f;
        }
        else if (isWallSliding)
        {
            rb.gravityScale = defaultGravityScale * wallSlideGravityMultiplier;
        }
        else if (rb.linearVelocity.y < -0.5f)
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.5f && !Input.GetButton("Jump"))
        {
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (isGrounded)
        {
            wasWallJumping = false;
            consecutiveBounces = 0;
            wallGrabStamina = wallGrabStaminaMax;
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

        // Wall Grab y Wall Slide
        bool isPushingWall = (moveInput * wallSide > 0);
        bool wantsToGrab = canWallGrab && Input.GetKey(wallGrabKey);
        if (isTouchingWall && !isGrounded && wantsToGrab)
        {
            isWallGrabbing = true;
            isWallSliding = false;

            if (wallGrabStaminaMax > 0)
            {
                wallGrabStamina -= Time.deltaTime;
                if (wallGrabStamina <= 0)
                {
                    isWallGrabbing = false;
                }
            }
        }
        else if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && isPushingWall)
        {
            isWallGrabbing = false;

            if (verticalInput < 0)
            {
                isWallSliding = false;
            }
            else
            {
                isWallSliding = true;
            }
        }
        else
        {
            isWallGrabbing = false;
            isWallSliding = false;
        }

        if (Time.time - lastBounceTime > bounceResetTime && !isGrounded)
        {
            consecutiveBounces = 0;
        }

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
        if (isWallGrabbing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }

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
        else if (!isWallSliding)
        {
            float targetX = moveInput * moveSpeed;
            float appliedX = isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }

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
                isWallGrabbing = false;
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

        if (isAttackingDown)
        {
            Transform currentAttackPoint = downAttackPoint;

            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);
            Collider2D[] hitGround = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, groundLayer);

            bool hitSomething = false;

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

            if (hitGround.Length > 0)
            {
                hitSomething = true;
                Debug.Log("¡Pegaste al suelo!");
            }

            if (hitSomething)
            {
                if (consecutiveBounces < maxConsecutiveBounces)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce);
                    consecutiveBounces++;
                    lastBounceTime = Time.time;
                    Debug.Log($"¡Rebote automático! ({consecutiveBounces}/{maxConsecutiveBounces})");
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce * 0.3f);
                    Debug.Log("¡Límite de rebotes alcanzado! Rebote reducido");
                }
            }
        }
        else
        {
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