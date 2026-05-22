using UnityEngine;
using TMPro; 

public class GameTimer : MonoBehaviour
{
    public float timeLeft = 90f;
    public TextMeshProUGUI timerText;

    private bool timerRunning = false;
    private float tiempoInicial; 

    // Usamos Awake en vez de Start para asegurarnos de que guarde los 90s antes de que el AuthManager toque nada
    void Awake()
    {
        tiempoInicial = timeLeft;
        
        if (tiempoInicial <= 0) 
        {
            tiempoInicial = 90f;
            timeLeft = 90f;
        }
    }

    // Actualiza el temporizador en cada frame
    void Update()
    {
        if (timerRunning)
        {
            if (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                UpdateDisplay(timeLeft);
            }
            else
            {
                timeLeft = 0;
                timerRunning = false;
                GameOver();
            }
        }
    }

    // Método para activar el temporizador
    public void ActivarTimer()
    {
        timerRunning = true;
    }

    // Método para desactivar el temporizador
    public void ResetTimer()
    {
        timerRunning = false;
        
        if (tiempoInicial <= 0) tiempoInicial = 90f;

        timeLeft = tiempoInicial;
        UpdateDisplay(timeLeft);   
    }

    // Método para actualizar el texto del temporizador
    void UpdateDisplay(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Método para manejar el fin del juego cuando el tiempo se agota
    void GameOver()
    {
        Debug.Log("Tiempo agotado");
        AuthManager auth = FindObjectOfType<AuthManager>();
        if (auth != null)
        {
            auth.PerderPartida();
        }
    }
}