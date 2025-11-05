using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    public int health = 3;

    [Header("Inmunidad")]
    public float invincibilityTime = 0.5f; 
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    public Vector2 knockbackForce = new Vector2(3f, 5f); 
    private Rigidbody2D rb;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, float knockbackDirection)
    {
        if (isInvincible)
        {
            return;
        }
        health -= damage;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            float forceX = knockbackForce.x * knockbackDirection;
            float forceY = knockbackForce.y;
            rb.linearVelocity = new Vector2(forceX, forceY);
        }

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red; 

        yield return new WaitForSeconds(invincibilityTime);

        spriteRenderer.color = originalColor;
        isInvincible = false;
    }
}