using UnityEngine;

public class CameaFollow : MonoBehaviour
{
    public Transform JUGADOR1111;
    public float VelocidadCamara = 15;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(JUGADOR1111 != null)
        {
            
            Vector3 posDeseada = new Vector3(JUGADOR1111.position.x, JUGADOR1111.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, posDeseada, VelocidadCamara * Time.deltaTime);
        }
    }
    
}
