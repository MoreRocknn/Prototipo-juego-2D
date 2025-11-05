using System.Collections; // ��MUY IMPORTANTE!! A�ade esto al inicio.
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    // (Asumo que ya tienes una variable de vida)
    public int health = 3;

    [Header("Inmunidad")]
    public float invincibilityTime = 0.5f; // Medio segundo de inmunidad
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    public Vector2 knockbackForce = new Vector2(3f, 5f); // Fuerza (X=hacia atr�s, Y=hacia arriba)
    private Rigidbody2D rb;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Esta funci�n P�BLICA es llamada por el jugador
    public void TakeDamage(int damage, float knockbackDirection)
    {
        // Si es inmune, ignora el golpe y no hagas nada
        if (isInvincible)
        {
            return;
        }

        health -= damage;
        if (rb != null)
        {
            // Detiene la velocidad actual para que el golpe se sienta
            rb.linearVelocity = Vector2.zero;

            // Calcula la fuerza X (3 * 1 o 3 * -1) y la fuerza Y (5)
            float forceX = knockbackForce.x * knockbackDirection;
            float forceY = knockbackForce.y;

            // Aplica el "salto"
            rb.linearVelocity = new Vector2(forceX, forceY);
        }

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Si sobrevive al golpe, activa la inmunidad
            StartCoroutine(InvincibilityFrames());
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // Esta es la "Corutina" que controla el tiempo de inmunidad
    private IEnumerator InvincibilityFrames()
    {
        // 1. Activa inmunidad
        isInvincible = true;

        // 2. Feedback visual (parpadeo)
        // Puedes hacer esto m�s complejo, pero un simple cambio de color funciona
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red; // Se pone rojo al ser golpeado

        // 3. Espera el tiempo de inmunidad
        yield return new WaitForSeconds(invincibilityTime);

        // 4. Desactiva inmunidad y vuelve al color original
        spriteRenderer.color = originalColor;
        isInvincible = false;
    }
}