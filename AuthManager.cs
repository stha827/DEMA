using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using System.Collections;
using UnityEngine.Video; 
using System.Collections.Generic; 
public class AuthManager : MonoBehaviour
{
    [Header("Objetos del Mundo")]
    public GameObject menuCamera;
    public GameObject mainPlayer;
    public GameObject[] enemies; 
    public GameObject gameUI; 

    [Header("Referencias de Scripts")]
    public PlayerController playerScript; 
    public GameTimer timerScript;
    public EnemyAction enemyScript; 

    [Header("Sistema de Vidas e Interfaz")]
    public int vidasMaximas = 3;
    private int vidasActuales;
    public GameObject[] corazonesUI; 

    [Header("Paneles de Final de Partida")]
    public GameObject victoryPanel;
    public GameObject gameOverPanel;
    public VideoPlayer videoVictoria;
    public VideoPlayer videoDerrota;

    [Header("Música del Juego")]
    public AudioSource musicaJuego; 

    [Header("Interfaz de Login e Intro")]
    public GameObject loginUIParent;   
    public GameObject panelLogin;
    public GameObject panelRegistro;
    public GameObject introVideoPanel; 
    public VideoPlayer miVideoPlayer;  

    [Header("Campos de Texto UI")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passLoginField;
    public TMP_Text warningLoginText;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passRegisterField;
    public TMP_InputField confirmPassRegisterField; 
    public TMP_Text warningRegisterText;

    // Listas para almacenar las posiciones iniciales de cada enemigo
    private List<Vector3> enemiesStartPos = new List<Vector3>();
    private List<Quaternion> enemiesStartRot = new List<Quaternion>();

    private FirebaseAuth auth;
    private Coroutine corrutinaIntro;

    // Flags para comunicar el hilo de Firebase con el hilo principal de Unity en el .exe
    private bool debeIrAlLogin = false;
    private bool registroCompletado = false;

    // --- CICLO DE VIDA DEL JUEGO ---
    // En Start inicializamos el estado de la partida, ocultamos elementos y preparamos Firebase
    void Start()
    {
        mainPlayer.SetActive(false);
        gameUI.SetActive(false);
        victoryPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        // Guardamos las posiciones y ocultamos todos los enemigos de la lista
        if (enemies != null) {
            foreach (GameObject currentEnemy in enemies) {
                if (currentEnemy != null) {
                    enemiesStartPos.Add(currentEnemy.transform.position);
                    enemiesStartRot.Add(currentEnemy.transform.rotation);
                    currentEnemy.SetActive(false);
                }
            }
        }
        
        if(menuCamera != null) menuCamera.SetActive(true);
        if(loginUIParent != null) loginUIParent.SetActive(true);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                auth = FirebaseAuth.DefaultInstance;
            }
        });
    }

    void Update()
    {
        // Maneja el cambio de interfaz de forma segura en el hilo principal de la Build
        if (registroCompletado)
        {
            registroCompletado = false;
            warningRegisterText.text = "";
            
            if (debeIrAlLogin)
            {
                debeIrAlLogin = false;
                AbrirLogin();
            }
        }
    }

    // --- FUNCIONES DE AUTENTICACIÓN ---
    public void BotonLogin() { StartCoroutine(ManejarLogin(emailLoginField.text.Trim(), passLoginField.text)); }
    
    private IEnumerator ManejarLogin(string _email, string _password) {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(() => loginTask.IsCompleted);
        if (loginTask.Exception == null) { 
            // Inicializar vidas al loguearse
            ResetearVisualDeCorazones();
            corrutinaIntro = StartCoroutine(ReproducirIntro()); 
        }
        else { 
            warningLoginText.text = "Error de acceso."; 
        }
    }
    // Función que se llama al pulsar el botón de registro.
    public void BotonRegistrar() 
    { 
        StartCoroutine(ManejarRegistro(emailRegisterField.text.Trim(), passRegisterField.text)); 
    }

    // Se encarga de validar que las contraseñas coincidan y luego crear la cuenta con Firebase, mostrando mensajes de error en caso de que algo falle
    private IEnumerator ManejarRegistro(string _email, string _password) 
    {
        if (_password != confirmPassRegisterField.text) 
        {
            warningRegisterText.text = "Las contraseñas no coinciden.";
            yield break;
        }

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception == null) { 
            if (auth.CurrentUser != null)
            {
                string userId = auth.CurrentUser.UserId;
                
                // Estructuramos los datos iniciales a la base de datos
                Dictionary<string, object> nuevoUsuario = new Dictionary<string, object>
                {
                    { "email", _email },
                    { "partidasGanadas", 0 },
                    { "partidasPerdidas", 0 }
                };

                // Guardamos el documento usando el ID único del usuario creado
                var firestoreTask = FirebaseFirestore.DefaultInstance
                    .Collection("usuarios")
                    .Document(userId)
                    .SetAsync(nuevoUsuario);

                // Forzamos la espera para que Firestore responda antes de mover la UI
                yield return new WaitUntil(() => firestoreTask.IsCompleted);

                if (firestoreTask.Exception != null)
                {
                    Debug.LogError("Error al registrar en Firestore: " + firestoreTask.Exception);
                }
            }

            // Si el registro es correcto  limpiamos el error y lo mandamos al login para que entre a través del hilo principal
            debeIrAlLogin = true;
            registroCompletado = true;
        }
        else 
        { 
            warningRegisterText.text = "Error al crear la cuenta."; 
        }
    }

    // --- LÓGICA DE FLUJO DE PARTIDA ---
    // Función que se encarga de reproducir la intro y luego arrancar el gameplay
    private IEnumerator ReproducirIntro() {
        if (loginUIParent != null) loginUIParent.SetActive(false);
        victoryPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        if (musicaJuego != null) musicaJuego.Stop();

        PrepararPosicionesYEstados();

        if (timerScript != null) timerScript.ResetTimer();

        if (introVideoPanel != null && miVideoPlayer != null) {
            introVideoPanel.SetActive(true);
            miVideoPlayer.Play();
            
            yield return new WaitForSeconds(0.5f);
            float tiempoVideo = (float)miVideoPlayer.length;
            if (tiempoVideo <= 0) tiempoVideo = 5f; 

            float tiempoPasado = 0f;
            while (tiempoPasado < tiempoVideo) {
                tiempoPasado += Time.deltaTime;
                yield return null; 
            }
        }

        ArrancarGameplay();
    }

    // Función que se llama al pulsar el botón de "Skip" para detener la corrutina y el video y arrancar el gameplay directamente
    public void BotonSkipIntro() {
        if (corrutinaIntro != null) StopCoroutine(corrutinaIntro);
        if (miVideoPlayer != null) miVideoPlayer.Stop();
        ArrancarGameplay();
    }

    // Función que se encarga de activar el jugador, la UI, la música y demás elements
    private void ArrancarGameplay() {
        if (introVideoPanel != null) introVideoPanel.SetActive(false);

        if(menuCamera != null) menuCamera.SetActive(false);
        mainPlayer.SetActive(true);
        
        // Activamos todos los enemigos guardados en la lista
        if (enemies != null) {
            foreach (GameObject currentEnemy in enemies) {
                if (currentEnemy != null) currentEnemy.SetActive(true);
            }
        }

        gameUI.SetActive(true);
        if (timerScript != null) timerScript.ActivarTimer();

        if (musicaJuego != null) musicaJuego.Play();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- GESTIÓN DE DAÑO ---
    // Función que se llama desde el script del enemigo cuando detecta que ha golpeado al jugador
    public void JugadorHaMuerto()
    {
        vidasActuales--;
        // Si nos quedan 2 vidas significa que el corazón en el índice 2 (el tercero) se apaga
        if (corazonesUI != null && vidasActuales >= 0 && vidasActuales < corazonesUI.Length)
        {
            if (corazonesUI[vidasActuales] != null)
            {
                corazonesUI[vidasActuales].SetActive(false);
            }
        }

        Debug.Log("Vidas restantes: " + vidasActuales);

        if (vidasActuales > 0)
        {
            StartCoroutine(ResetearRondaRapida());
        }
        else
        {
            PerderPartida();
        }
    }

    // Función que se encarga de resetear la posición del jugador y el enemigo así como otros estados necesarios
    private IEnumerator ResetearRondaRapida()
    {
        mainPlayer.SetActive(false);
        
        // Apagamos todos los enemigos antes de reubicarlos
        if (enemies != null) {
            foreach (GameObject currentEnemy in enemies) {
                if (currentEnemy != null) currentEnemy.SetActive(false);
            }
        }

        if (musicaJuego != null) musicaJuego.Stop();

        yield return new WaitForSeconds(0.2f);

        PrepararPosicionesYEstados();
        ArrancarGameplay();
    }

    // Función que se encarga de resetear posiciones, estados y más elementos necesarios para reinicar de cero
    private void PrepararPosicionesYEstados()
    {
        if (playerScript != null) playerScript.ResetState(); 
        
        // Reseteamos las posiciones y los estados de cada enemigo del array
        if (enemies != null) {
            for (int i = 0; i < enemies.Length; i++) {
                if (enemies[i] != null && i < enemiesStartPos.Count) {
                    enemies[i].SetActive(true); 
                    enemies[i].transform.position = enemiesStartPos[i]; 
                    enemies[i].transform.rotation = enemiesStartRot[i];

                    // Si cada clon tiene su propio script de control, lo reseteamos localmente
                    EnemyAction currentEnemyScript = enemies[i].GetComponent<EnemyAction>();
                    if (currentEnemyScript != null) currentEnemyScript.ResetEnemy();

                    enemies[i].SetActive(false); 
                }
            }
        }
    }

    // --- VIDAS ---
    // Función para volver a encender todos los corazones al reiniciar la partida entera
    private void ResetearVisualDeCorazones()
    {
        vidasActuales = vidasMaximas;

        if (corazonesUI != null)
        {
            for (int i = 0; i < corazonesUI.Length; i++)
            {
                if (corazonesUI[i] != null)
                {
                    corazonesUI[i].SetActive(true);
                }
            }
        }
    }

    // --- FINALES DE PARTIDA ---
    // Función que se llama desde el script del enemigo cuando detecta que el jugador ha derrotado al enemigo
    public void GanarPartida() { 
        if (playerScript != null && !playerScript.haGanado) 
        {
            return; 
        }

        // --- GUARDADO DE VICTORIA EN FIRESTORE ---
        if (auth != null && auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;
            FirebaseFirestore.DefaultInstance
                .Collection("usuarios")
                .Document(userId)
                .UpdateAsync("partidasGanadas", FieldValue.Increment(1));
        }
        
        StartCoroutine(FinalDePartida(victoryPanel, videoVictoria)); 
    }
    
    // Función que se llama desde el script del enemigo cuando detecta que el jugador ha sido derrotado
    public void PerderPartida() { 
        // --- GUARDADO DE DERROTA EN FIRESTORE ---
        if (auth != null && auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;
            FirebaseFirestore.DefaultInstance
                .Collection("usuarios")
                .Document(userId)
                .UpdateAsync("partidasPerdidas", FieldValue.Increment(1));
        }

        StartCoroutine(FinalDePartida(gameOverPanel, videoDerrota)); 
    }

    // Función que se encarga de mostrar el panel de victoria o derrota, reproducir el video correspondiente, detener la música y más elementos necesarios
    private IEnumerator FinalDePartida(GameObject panelFinal, VideoPlayer videoFinal) {
        mainPlayer.SetActive(false);
        gameUI.SetActive(false);
        
        // Apagamos todos los enemigos al finalizar por completo el juego
        if (enemies != null) {
            foreach (GameObject currentEnemy in enemies) {
                if (currentEnemy != null) currentEnemy.SetActive(false);
            }
        }
        
        if (musicaJuego != null) musicaJuego.Stop();

        panelFinal.SetActive(true);

        if (videoFinal != null) {
            videoFinal.gameObject.SetActive(true); 
            videoFinal.Play();
        }

        if(menuCamera != null) menuCamera.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return null;
    }

    public void BotonReiniciar() {
        // Al pulsar reiniciar restablecemos los 3 corazones encendidos y lanzamos la intro
        ResetearVisualDeCorazones();
        corrutinaIntro = StartCoroutine(ReproducirIntro());
    }

    // --- NAVEGACIÓN ENTRE PANELES DE LOGIN Y REGISTRO ---
    public void AbrirRegistro() { panelLogin.SetActive(false); panelRegistro.SetActive(true); }
    public void AbrirLogin() { panelLogin.SetActive(true); panelRegistro.SetActive(false); }
}