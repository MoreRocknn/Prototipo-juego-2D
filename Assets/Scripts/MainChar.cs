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

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
            jumpPressed = true;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);
        if (Input.GetButtonDown("Fire1")) 
        {
            Attack();
        }
        if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
        else if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed); 
        }
        else
        {
            isWallSliding = false;
        }
        isAttackingDown = false;
        if (Input.GetButtonDown("Fire1"))
        {
            float verticalInput = Input.GetAxisRaw("Vertical"); 

  
            if (verticalInput < 0 && !isGrounded)
            {
                isAttackingDown = true;
            }

            Attack(); 
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
        // 1. Efecto Visual: Inicia la corutina del "slash"
        StartCoroutine(ShowAttackEffect());

        // 2. Knockback del Jugador: (Solo si ataca de lado)
        if (!isAttackingDown)
        {
            // Te da un empujoncito hacia atrás
            float knockbackDir = isFacingRight ? -1 : 1;
            rb.AddForce(new Vector2(knockbackDir * playerKnockbackForce, 0), ForceMode2D.Impulse);
        }

        // 3. Elige el punto de ataque (Lateral o Abajo)
        Transform currentAttackPoint = isAttackingDown ? downAttackPoint : attackPoint;

        // 4. Detectar enemigos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

        // 5. Aplicar daño a cada enemigo
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemigo enemy = enemyCollider.GetComponent<Enemigo>();
            if (enemy != null)
            {
                // Determina la dirección del knockback para el ENEMIGO
                int enemyKnockbackDir = isFacingRight ? 1 : -1;

                // Llama a la función de daño del enemigo
                enemy.TakeDamage(attackDamage, enemyKnockbackDir);

                // 6. ¡Pogo-Jump! (Rebote al golpear hacia abajo)
                if (isAttackingDown)
                {
                    // Resetea tu velocidad vertical y te da un salto
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
            }
        }
    }
    private IEnumerator ShowAttackEffect()
    {
        // Elige qué efecto mostrar (lateral o abajo)
        GameObject effectToShow = isAttackingDown ? downAttackEffect : sideAttackEffect;

        if (effectToShow != null)
        {
            // 1. Lo activa
            effectToShow.SetActive(true);

            // 2. Espera 0.1 segundos
            yield return new WaitForSeconds(0.1f);

            // 3. Lo desactiva
            effectToShow.SetActive(false);
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
    }  
}