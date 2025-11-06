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
    public float wallJumpLockTime = 0.15f; // Control bloqueado tras wall jump
    public float wallJumpControlTime = 0.25f; // Tiempo hasta recuperar control total
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
    public float fallGravityMultiplier = 2.8f; // HK usa ~2.5-3
    public float lowJumpMultiplier = 2f;
    public float wallSlideGravityMultiplier = 0.3f; // Muy pegado a la pared
    public float coyoteTime = 0.1f; // HK usa ~0.1
    public float jumpBufferTime = 0.1f;

    [Header("Afinación adicional")]
    public float jumpCutMultiplier = 0.5f; // HK corta el salto a la mitad
    public float airControlMultiplier = 1f; // HK tiene control total en el aire
    public float maxFallSpeed = 22f;
    public float wallJumpAirDrag = 0.92f; // Freno suave tras wall jump

    private float defaultGravityScale;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpReleased = true; // Evita saltos automáticos

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // --- INPUT DE SALTO: jump buffering ---
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
            jumpReleased = false;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Detectar cuando se suelta el salto
        if (Input.GetButtonUp("Jump"))
        {
            jumpReleased = true;

            // Jump cut más agresivo (estilo HK)
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        // --- LÓGICA DE ATAQUE ---
        isAttackingDown = false;
        if (Input.GetButtonDown("Fire1"))
        {
            if (verticalInput < 0 && !isGrounded)
            {
                isAttackingDown = true;
            }
            Attack();
        }

        // --- GRAVEDAD VARIABLE (HOLLOW KNIGHT STYLE) ---
        if (isWallSliding)
        {
            // Gravedad muy baja en wall slide (se siente pegajoso)
            rb.gravityScale = defaultGravityScale * wallSlideGravityMultiplier;
        }
        else if (rb.linearVelocity.y < -0.1f) // Cayendo
        {
            // Caída rápida característica de HK
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.1f && !Input.GetButton("Jump")) // Subiendo pero soltó botón
        {
            // Corte de salto (hace que sientas más control)
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        // --- CHEQUEOS DE ENTORNO ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        // Resetear flag de wall jump cuando tocas el suelo
        if (isGrounded)
        {
            wasWallJumping = false;
        }

        // --- LÓGICA DE COYOTE TIME ---
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Decrementar wall jump counter
        if (wallJumpCounter > 0f)
        {
            wallJumpCounter -= Time.deltaTime;
        }

        // --- LÓGICA DE WALL SLIDE ---
        bool isPushingWall = (moveInput * wallSide > 0);

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && isPushingWall)
        {
            if (verticalInput < 0) // Si pulsa "Abajo" (fast fall)
            {
                isWallSliding = false;
                // Caída rápida
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
            }
            else
            {
                isWallSliding = true;
                // Deslizamiento suave
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            isWallSliding = false;
        }

        // --- LÓGICA DE GIRO (FLIP) ---
        // Permitir flip más libre, pero con delay mínimo tras wall jump
        if (wallJumpCounter <= 0.05f) // Solo 0.05s de bloqueo
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
        // Control de movimiento con wall jump
        if (wallJumpCounter > 0f)
        {
            // Durante wall jump, aplicar resistencia progresiva
            float controlAmount = 1f - (wallJumpCounter / wallJumpLockTime);

            if (wallJumpCounter > wallJumpLockTime)
            {
                // Bloqueo total inicial
                return;
            }
            else
            {
                // Recuperación gradual del control
                float targetX = moveInput * moveSpeed * controlAmount;
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocity.x, targetX, wallJumpAirDrag),
                    rb.linearVelocity.y
                );
            }
        }
        else if (!isWallSliding)
        {
            // Control normal
            float targetX = moveInput * moveSpeed;
            float appliedX = isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }

        // --- LÓGICA DE SALTO ---
        if (jumpBufferCounter > 0f && jumpReleased == false)
        {
            // WALL JUMP
            if (isTouchingWall && !isGrounded && wallJumpCounter <= 0f)
            {
                bool isPushingTowardsWall = (moveInput * wallSide > 0);

                if (isPushingTowardsWall || Mathf.Abs(moveInput) < 0.1f)
                {
                    // Salto neutro o hacia la pared (más vertical)
                    rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x * 0.7f, wallJumpForce.y);
                }
                else
                {
                    // Salto alejándose de la pared (más horizontal)
                    rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y * 0.95f);
                }

                wallJumpCounter = wallJumpControlTime;
                wasWallJumping = true;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false;
            }
            // SALTO NORMAL
            else if (coyoteTimeCounter > 0f && !wasWallJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
            else
            {
                // Si no se puede saltar, resetear buffer más rápido
                jumpBufferCounter = 0f;
            }
        }

        // Limitar velocidad de caída (como HK)
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

        Transform currentAttackPoint = isAttackingDown ? downAttackPoint : attackPoint;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemigo enemy = enemyCollider.GetComponent<Enemigo>();
            if (enemy != null)
            {
                int enemyKnockbackDir = isFacingRight ? 1 : -1;
                enemy.TakeDamage(attackDamage, enemyKnockbackDir);

                if (isAttackingDown)
                {
                    // Pogo jump (característico de HK)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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