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
    private bool jumpPressed;
    private bool isFacingRight = true;
    private int wallSide = 1; 
    public Transform attackPoint;     
    public float attackRange = 0.5f;  
    public LayerMask enemyLayer;      
    public int attackDamage = 1;
    public float playerKnockbackForce = 3f;
    public Transform downAttackPoint;
    public GameObject sideAttackEffect;
    public GameObject downAttackEffect;
    private bool isAttackingDown = false;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // === INPUTS ===
        moveInput = Input.GetAxisRaw("Horizontal");

        // Detectar si PULSAS el botón de salto
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }

        // ¡¡AQUÍ ESTÁ EL ARREGLO!!
        // Detectar si SUELTAS el botón de salto
        if (Input.GetButtonUp("Jump"))
        {
            // Y si estás subiendo (velocidad Y positiva)
            if (rb.linearVelocity.y > 0)
            {
                // Corta la velocidad vertical (ej: a la mitad)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
        }

        // Detectar el botón de ataque
        isAttackingDown = false; // Resetea el ataque hacia abajo
        if (Input.GetButtonDown("Fire1"))
        {
            float verticalInput = Input.GetAxisRaw("Vertical");

            // Comprueba si pulsas "S" Y no estás en el suelo
            if (verticalInput < 0 && !isGrounded)
            {
                isAttackingDown = true;
            }

            Attack(); // Llama a la función de ataque
        }


        // === CHEQUEOS DE FÍSICA ===
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        // === LÓGICA DE VOLTEO (FLIP) ===
        if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
        else if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }

        // === LÓGICA DE WALL SLIDE ===
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    void FixedUpdate()
    {
        if (!isWallSliding)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        if (jumpPressed)
        {
            if (isWallSliding)
            {
                rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y);
            }
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            jumpPressed = false;
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
        // 1. Elige qué efecto mostrar (lateral o abajo)
        GameObject effectToShow = isAttackingDown ? downAttackEffect : sideAttackEffect;

        // 2. ¡¡NUEVO!! Obtén el sistema de partículas y dale a "Play"
        if (effectToShow != null)
        {
            // Busca el componente "ParticleSystem"
            ParticleSystem ps = effectToShow.GetComponent<ParticleSystem>();

            // Si lo encuentra, le da a "Play"
            if (ps != null)
            {
                ps.Play();
            }
        }

        // 3. Knockback del Jugador: (Esto ya lo tenías)
        if (!isAttackingDown)
        {
            float knockbackDir = isFacingRight ? -1 : 1;
            rb.AddForce(new Vector2(knockbackDir * playerKnockbackForce, 0), ForceMode2D.Impulse);
        }

        // 4. Elige el punto de ataque (Lateral o Abajo)
        Transform currentAttackPoint = isAttackingDown ? downAttackPoint : attackPoint;

        // 5. Detectar enemigos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

        // 6. Aplicar daño a cada enemigo
        foreach (Collider2D enemyCollider in hitEnemies)
        {
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
    // void OnDrawGizmos()
    // {
    //Gizmos.color = Color.green;
    //Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
    // Gizmos.color = Color.blue;
    // Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    // Gizmos.color = Color.red; 
    // Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    //Gizmos.color = Color.yellow;
    //Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
    //}  
}