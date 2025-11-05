using UnityEngine;

public class Enemigo : MonoBehaviour
{
    public int Health = 3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void TakeDamage(int damage)
    {
        Health = -damage;
        if (Health < 0)
        {
            Die();
        }
    }

    // Update is called once per frame
    void Die()
    {
        Destroy(gameObject);
    }
}
