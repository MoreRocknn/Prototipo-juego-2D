using UnityEngine;

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

    // --- ¡NUEVAS VARIABLES PARA EL FLIP! ---
    private bool isFacingRight = true;
    private int wallSide = 1; // 1 para derecha, -1 para izquierda

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // === INPUTS ===
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
            jumpPressed = true;

        // === CHEQUEOS DE FÍSICA ===
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        // === LÓGICA DE VOLTEO (FLIP) ===
        // Si nos movemos a la izquierda y miramos a la derecha...
        if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
        // Si nos movemos a la derecha y miramos a la izquierda...
        else if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }

        // === LÓGICA DE WALL SLIDE ===
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed); // Desliza
        }
        else
        {
            isWallSliding = false;
        }
    }

    void FixedUpdate()
    {
        // === APLICAR MOVIMIENTO ===
        // ¡¡CORREGIDO!! Solo aplica movimiento si NO estás en la pared
        if (!isWallSliding)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // === LÓGICA DE SALTO ===
        if (jumpPressed)
        {
            if (isWallSliding)
            {
                // Salta FUERA de la pared (usa 'wallSide' que se basa en isFacingRight)
                rb.linearVelocity = new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y);
            }
            else if (isGrounded)
            {
                // Salto normal
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            jumpPressed = false;
        }
    }

    // --- ¡NUEVA FUNCIÓN DE FLIP! ---
    void Flip()
    {
        // Cambia la dirección a la que mira
        isFacingRight = !isFacingRight;
        wallSide *= -1; // Invierte el lado de la pared

        // Invierte la escala local del personaje en el eje X
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    // --- (Tus otras funciones siguen igual) ---

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemigo enemy = collision.gameObject.GetComponent<Enemigo>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
            }
        }
    }
}