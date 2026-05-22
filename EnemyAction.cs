using UnityEngine;
using UnityEngine.AI; 

public class EnemyAction : MonoBehaviour
{
    public Transform player; 
    public GameObject projectilePrefab; 

    public float chaseDistance = 15f; 
    public float stopDistance = 1f; 
    private NavMeshAgent agent;

    public float fireRate = 1.5f;   
    private float nextFireTime;

    public int AlasPerdidas = 0;

    [Header("Puntos Débiles (Ojos)")]
    public GameObject[] ojosAlas; 

    // --- COMPORTAMIENTO ---
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;
    }

    // El enemigo persigue al jugador, dispara si está cerca y se detiene si el jugador muere o gana
    void Update()
    {
        if (player == null) return;
        PlayerController scriptJugador = player.GetComponent<PlayerController>();
        if (scriptJugador != null)
        {
            // Si el jugador muere o gana el angel deja de moverse y disparar
            if (scriptJugador.estaMuerto == true || scriptJugador.haGanado == true)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                return;
            }
        }

        // Verificar la victoria
        if (AlasPerdidas >= 2)
        {
            if (scriptJugador != null) scriptJugador.Ganar(); 
            if (agent.isOnNavMesh) agent.isStopped = true; 
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Perseguir al jugador si está dentro de la distancia de persecución
        if (distance <= chaseDistance)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false; 
                agent.SetDestination(player.position);
            }
        }

        // Disparar si el jugador está dentro del rango de disparo
        if (distance <= chaseDistance + 5f)
        {
            // Girar para mirar al jugador
            Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(targetPos);

            // Vereficar si hay línea de visión al jugador
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 1.0f + transform.forward * 1.5f; 
            Vector3 direction = (player.position + Vector3.up * 1.0f - rayOrigin).normalized;

            Debug.DrawRay(rayOrigin, direction * chaseDistance, Color.red);

            if (Physics.Raycast(rayOrigin, direction, out hit, chaseDistance))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    if (Time.time >= nextFireTime)
                    {
                        Shoot();
                        nextFireTime = Time.time + fireRate;
                    }
                }
            }
        }
    }

    // --- DISPARO ---
    // Lnza un proyectil hacia el jugador
    void Shoot()
    {
        Instantiate(projectilePrefab, transform.position + transform.forward * 1.5f + Vector3.up * 1.0f, transform.rotation);
        Debug.Log("disparo del angel");
    }

    // --- RESET ---
    // Resetea el estado del enemigo para reiniciar la partida
    public void ResetEnemy()
    {
        // Reseteamos todos los valores
        AlasPerdidas = 0;
        nextFireTime = Time.time;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        // Volvemos a activar los ojos de las alas
        {
            for (int i = 0; i < ojosAlas.Length; i++)
            {
                if (ojosAlas[i] != null)
                {
                    ojosAlas[i].SetActive(true);
                }
            }
        }
    }
}