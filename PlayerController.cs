using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // Ajustes de físicas y movimiento
    public float velocidad = 5.5f;
    public float gravedad = -9.81f;
    public float fuerzaSalto = 1.5f; 
    
    // Cámara y sensibilidad
    public Camera camaraPrincipal;
    public float sensibilidadMouse = 100f;
    float xRotation = 0f;

    // Referencias de componentes
    CharacterController controller;
    Vector3 velocidadCaida;

    // Estados de la partida
    public bool estaMuerto = false;
    public bool haGanado = false;

    // Conexión con otros scripts
    public AuthManager authManager; 

    [Header("Ajustes de Combate")]
    public float rangoDisparo = 20f; 
    [Header("Ajustes de Aparición")]
    public Transform puntoDeAparicion; 

    // Lista  para guardar qué enemigos  ya han sido derrotados y evitar bucles
    private List<EnemyAction> enemigosMuertos = new List<EnemyAction>();

    void Start()
    {
        Time.timeScale = 1f; 
        controller = GetComponent<CharacterController>();
        
        if (authManager == null) {
            authManager = Object.FindFirstObjectByType<AuthManager>();
        }
        ResetState();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Si la partida ha terminado, bloqueamos el control
        if (estaMuerto || haGanado) return;

        ManejarRotacion();
        ManejarMovimiento();

        // Lógica de disparo con el click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            DispararAlOjo();
        }
    }

    // Maneja la rotación de la cámara y del jugador basada en el movimiento del mouse
    void ManejarRotacion()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadMouse * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camaraPrincipal.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // Maneja el movimiento del jugador
    void ManejarMovimiento()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 mover = transform.right * x + transform.forward * z;
        controller.Move(mover * velocidad * Time.deltaTime);

        // Gravedad
        if (controller.isGrounded && velocidadCaida.y < 0)
        {
            velocidadCaida.y = -2f;
        }
        
        velocidadCaida.y += gravedad * Time.deltaTime;
        controller.Move(velocidadCaida * Time.deltaTime);
    }

    // Lógica de disparo mediante raycast para detectar impactos en el ojo del enemigo
    void DispararAlOjo()
    {
        RaycastHit hit;
        
        // Para limitar el rango de disparo del jugador
        if (Physics.Raycast(camaraPrincipal.transform.position, camaraPrincipal.transform.forward, out hit, rangoDisparo))
        {            
            //Comprobamos que el objeto impactado sea el ojo del enemigo
            WingHitbox ojo = hit.collider.GetComponent<WingHitbox>();
            if (ojo != null)
            {
                ojo.RecibirDisparo();
            }
        }
    }

    // Devuelve al jugador al punto de aparición
    public void ResetState()
    {
        if (puntoDeAparicion != null)
        {
            if (controller != null) controller.enabled = false;

            transform.position = puntoDeAparicion.position;
            transform.rotation = puntoDeAparicion.rotation;

            if (controller != null) controller.enabled = true;
        }

        estaMuerto = false;
        haGanado = false;
        velocidadCaida = Vector3.zero;
        xRotation = 0f;

        // Limpiamos la lista de bajas al reiniciar la ronda
        enemigosMuertos.Clear();
    }

    // Lógica para manejar la muerte del jugador
    public void Morir()
    {
        if (!estaMuerto)
        {
            estaMuerto = true;
            Debug.Log("Caos ha muerto.");
            
            if (authManager != null) {
                authManager.JugadorHaMuerto();
            }
        }
    }

    // Método para registrar la baja de un enemigo, evitando el bucle del Update
    public void RegistrarBajaEnemigo(EnemyAction enemigo)
    {
        if (!haGanado && enemigo != null)
        {
            // Si este enemigo ya lo teníamos registrado como muerto, ignoramos el aviso repetido del Update
            if (enemigosMuertos.Contains(enemigo)) return;

            // Guardamos el enemigo en la lista de bajas reales
            enemigosMuertos.Add(enemigo);
            Debug.Log("Enemigos derrotados actualmente: " + enemigosMuertos.Count);

            // Comprobamos si las bajas totales igualan a los enemigos de la escena
            if (authManager != null && authManager.enemies != null && enemigosMuertos.Count >= authManager.enemies.Length)
            {
                haGanado = true;

                if (authManager != null) {
                    authManager.GanarPartida();
                }
            }
            else
            {
                Debug.Log("Enemigo derrotado. Quedan más enemigos");
            }
        }
    }

    // Mantengo este método vacío por compatibilidad si algún otro script lo busca
    public void Ganar() {}
}