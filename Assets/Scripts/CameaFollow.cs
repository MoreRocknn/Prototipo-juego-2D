using UnityEngine;

public class CamaraScript : MonoBehaviour // O como se llame tu script
{
    public Transform JUGADOR1111;
    public float VelocidadCamara = 5f;

    [Header("Zona Muerta Vertical")]
    public float verticalDeadzone = 1.5f;

    
    public Vector3 offset = new Vector3(0, 0, -10);


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
