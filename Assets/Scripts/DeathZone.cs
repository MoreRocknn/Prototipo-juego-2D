using UnityEngine;
using System.Collections;


public class DeathZone : MonoBehaviour
{
    public float respawnDelay = 1f; // Tiempo antes de reaparecer
    public bool destroyplayer = false; // Si true, destruye y recrea. Si false, solo teletransporta

    [Header("Efectos (opcional)")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private AudioSource audioSource;
    private bool isRespawning = false; // Evitar múltiples respawns

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // VERIFICAR configuración
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null && showDebugMessages)
        {
            Debug.Log($"DeadZone configurado: IsTrigger={boxCol.isTrigger}");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[DeadZone] Trigger activado por: {other.name}, Tag: {other.tag}");
        }

        // Solo afectar al jugador y evitar múltiples llamadas
        if (other.CompareTag("Player") && !isRespawning)
        {
            Debug.Log("¡Jugador cayó en DeadZone! Iniciando respawn...");
            StartCoroutine(RespawnPlayer(other.gameObject));
        }
    }

    // MÉTODO ALTERNATIVO por si OnTriggerEnter2D no funciona
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[DeadZone] Collision detectada con: {collision.gameObject.name}");
        }

        if (collision.gameObject.CompareTag("Player") && !isRespawning)
        {
            Debug.Log("¡Jugador cayó en DeadZone (via Collision)! Iniciando respawn...");
            StartCoroutine(RespawnPlayer(collision.gameObject));
        }
    }

    IEnumerator RespawnPlayer(GameObject player)
    {
        isRespawning = true;

        Debug.Log("=== INICIANDO RESPAWN ===");

        // Obtener componentes del jugador
        MainChar playerController = player.GetComponent<MainChar>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        // Desactivar controles
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Controles desactivados");
        }

        // Desactivar colisiones temporalmente
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Detener físicas
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            Debug.Log("Físicas detenidas");
        }

        // Efecto de muerte
        if (deathEffect != null)
        {
            Instantiate(deathEffect, player.transform.position, Quaternion.identity);
        }

        // Sonido
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Hacer invisible al jugador (opcional)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            Debug.Log("Jugador invisible");
        }

        // Esperar delay
        yield return new WaitForSeconds(respawnDelay);

        // Obtener posición de respawn
        Vector2 respawnPos = Vector2.zero;
        if (GameManager.Instance != null)
        {
            respawnPos = GameManager.Instance.GetRespawnPosition();
            Debug.Log($"Respawn en checkpoint: {respawnPos}");
        }
        else
        {
            Debug.LogWarning("¡GameManager no encontrado! Respawneando en (0,0)");
        }

        // Teletransportar
        player.transform.position = respawnPos;
        Debug.Log($"Jugador teletransportado a: {respawnPos}");

        // Restaurar físicas
        if (rb != null)
        {
            rb.gravityScale = 3.5f; // O tu valor por defecto
            rb.linearVelocity = Vector2.zero;
        }

        // Reactivar colisiones
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // Hacer visible
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Reactivar controles
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log("=== RESPAWN COMPLETADO ===");
        isRespawning = false;
    }
}
    

