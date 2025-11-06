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
    public float wallSlideSpeed = 1.5f;
    public Vector2 wallJumpForce = new Vector2(10f, 14f);

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
    public float fallGravityMultiplier = 3.5f;
    public float lowJumpMultiplier = 2.2f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;

    [Header("Afinación adicional")]
    public float jumpCutMultiplier = 0.85f;
    public float airControlMultiplier = 0.92f;
    public float maxFallSpeed = 25f;

    private float defaultGravityScale;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

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
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime; // Decrementar buffer si no se pulsa
        }

        // Si sueltas el botón de salto, aplicamos un "jump cut"
        if (Input.GetButtonUp("Jump"))
        {
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

        // --- GRAVEDAD VARIABLE (HOLLOW KNIGHT) ---
        // ¡¡ARREGLO 1!! Aplicar gravedad extra SOLO si estamos cayendo Y NO estamos tocando una pared
        if (rb.linearVelocity.y < 0f && !isTouchingWall)
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump")) // Está subiendo pero soltó el botón
        {
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else // Está subiendo y manteniendo el botón, o está quieto
        {
            rb.gravityScale = defaultGravityScale;
        }

        // --- CHEQUEOS DE ENTORNO ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        // --- LÓGICA DE COYOTE TIME ---
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // --- LÓGICA DE WALL SLIDE ---
        bool isPushingWall = (moveInput * wallSide > 0);

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && isPushingWall)
        {
            if (verticalInput < 0) // Si pulsa "Abajo"
            {
                isWallSliding = false; // Nos soltamos
            }
            else // Si no pulsa abajo Y está pulsando hacia la pared
            {
                isWallSliding = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            isWallSliding = false;
        }

        // --- LÓGICA DE GIRO (FLIP) ---
        if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
        else if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        if (!isWallSliding)
        {
            float targetX = moveInput * moveSpeed;
            float appliedX = isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }

        if (jumpBufferCounter > 0f)
        {
            if (isTouchingWall && !isGrounded)
            {
                bool isClimbing = (moveInput * wallSide > 0);

                if (isClimbing)
                {
                    rb.linearVelocity = new Vector2(-wallSide * (wallJumpForce.x * 0.4f), wallJumpForce.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y);
                }

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false; 
            }
            else if (coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
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
            //Debes cambiar "Enemigo" por el nombre de tu script de enemigo
            Enemigo enemy = enemyCollider.GetComponent<Enemigo>(); 
            if (enemy != null)
             {
                int enemyKnockbackDir = isFacingRight ? 1 : -1;
                enemy.TakeDamage(attackDamage, enemyKnockbackDir);

               if (isAttackingDown)
              {
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