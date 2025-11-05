using UnityEngine;

public class MainChar : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    public float wallSlideSpeed = 1.5f;
    public Vector2 wallJumpForce = new Vector2(10f, 14f);
    public float checkRadius = 0.2f;
    public Transform groundCheck, wallCheck;
    public LayerMask groundLayer, wallLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded, isTouchingWall, isWallSliding;
    private bool isWallJumping;
    private bool jumpPressed;
    private float wallJumpDirection;

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

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else isWallSliding = false;

        wallJumpDirection = wallCheck.localPosition.x > 0 ? -1 : 1;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed)
        {
            if (isWallSliding)
            {
                // salto normal desde la pared
                isWallJumping = true;
                rb.linearVelocity = new Vector2(wallJumpForce.x * wallJumpDirection, wallJumpForce.y);
            }
            else if (isWallJumping && !isGrounded)
            {
                // 👇 segunda pulsación: volver hacia la pared
                float backForce = 8f;
                rb.linearVelocity = new Vector2(-wallJumpDirection * backForce, rb.linearVelocity.y);
            }
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            jumpPressed = false;
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Comprueba si el objeto con el que chocó tiene el tag "Enemy"
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Busca el script EnemyHealth en el enemigo
            Enemigo enemy = collision.gameObject.GetComponent<Enemigo>();

            // Si encontró el script, le hace daño
            if (enemy != null)
            {
                enemy.TakeDamage(1); // Llama a la función del otro script
            }
        }
    }
}

