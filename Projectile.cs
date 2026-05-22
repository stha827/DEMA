using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;   
    public float lifeTime = 5f; 

    // Inicializa el proyectil y lo destruye después de un tiempo
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Mueve el proyectil hacia adelante cada frame
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // Detecta colisiones con otros objetos
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController scriptDelJugador = other.GetComponent<PlayerController>();
            if (scriptDelJugador != null)
            {
                scriptDelJugador.Morir();
            }
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}