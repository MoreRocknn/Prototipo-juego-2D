using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sistema de Respawn")]
    public Vector2 lastCheckpoint;
    public bool hasCheckpoint = false;
    public int playerHealth = 3;
    public int maxHealth = 3;

    
    [Header("Configuración")]
    public Vector2 spawnInicial = new Vector2(0, 0); // Posición inicial del juego
    public void TakeDamage(int damage)
    {
        playerHealth -= damage;
        if (playerHealth <= 0)
        {
            // Respawn
        }
    }
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Establecer spawn inicial como primer checkpoint
        if (!hasCheckpoint)
        {
            lastCheckpoint = spawnInicial;
        }
    }

    public void SetCheckpoint(Vector2 position)
    {
        lastCheckpoint = position;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint guardado en: {position}");
    }

    public Vector2 GetRespawnPosition()
    {
        return lastCheckpoint;
    }
}

