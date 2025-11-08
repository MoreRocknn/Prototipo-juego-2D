using UnityEngine;

public class CamaraScript : MonoBehaviour
{
    public Transform JUGADOR1111;
    public float VelocidadCamara = 5f;

    [Header("Zona Muerta Vertical")]
    public float verticalDeadzone = 1.5f;

    public Vector3 offset = new Vector3(0, 0, -10);

    // ==========================================================
    // MÉTODO 'SNAPTOPLAYER' CORREGIDO
    // ==========================================================
    public void SnapToPlayer()
    {
        if (JUGADOR1111 == null)
            return;

        // 1. Calculamos la 'Y' objetivo del jugador (igual que en LateUpdate)
        float playerY = JUGADOR1111.position.y + offset.y;

        // 2. Calculamos dónde DEBERÍA estar la cámara para que el jugador
        // quede "descansando" dentro de la zona muerta.
        // Esta es la misma lógica que tu 'else if (playerY < bottomThreshold)'
        // en LateUpdate, que define la posición de reposo inferior.
        float targetCamY = playerY + verticalDeadzone;

        // 3. Asignamos la posición de la cámara instantáneamente
        transform.position = new Vector3(
            JUGADOR1111.position.x + offset.x,
            targetCamY,
            offset.z
        );
    }
    // ==========================================================

    void LateUpdate()
    {
        if (JUGADOR1111 == null)
            return;

        float posX = JUGADOR1111.position.x + offset.x;
        float posY = transform.position.y;

        float playerY = JUGADOR1111.position.y + offset.y;
        float topThreshold = transform.position.y + verticalDeadzone;
        float bottomThreshold = transform.position.y - verticalDeadzone;

        if (playerY > topThreshold)
        {
            posY = playerY - verticalDeadzone;
        }
        else if (playerY < bottomThreshold)
        {
            posY = playerY + verticalDeadzone;
        }
        Vector3 posDeseada = new Vector3(posX, posY, offset.z);
        transform.position = Vector3.Lerp(transform.position, posDeseada, VelocidadCamara * Time.deltaTime);
    }
}