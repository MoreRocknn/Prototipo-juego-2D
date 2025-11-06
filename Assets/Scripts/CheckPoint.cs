using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public bool isActivated = false;

    [Header("Efectos visuales (opcional)")]
    public GameObject inactiveVisual; // Sprite cuando no está activado
    public GameObject activeVisual;   // Sprite cuando está activado
    public ParticleSystem activationEffect; // Partículas al activar

    [Header("Audio (opcional)")]
    public AudioClip activationSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        UpdateVisuals();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Solo activar si es el jugador
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        isActivated = true;

        // Guardar posición en GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
        }

        // Efectos visuales
        UpdateVisuals();

        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        // Sonido
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }

        Debug.Log("¡Checkpoint activado!");
    }

    void UpdateVisuals()
    {
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);

        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
    }

    // Opcional: Permitir reactivar checkpoint para curar
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isActivated)
        {
            // Aquí podrías añadir lógica para curar al jugador
            // si mantiene presionado un botón, etc.
        }
    }

    // Gizmo para visualizar en editor
    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
