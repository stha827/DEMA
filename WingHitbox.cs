using UnityEngine;

public class WingHitbox : MonoBehaviour
{
    public EnemyAction angel; 

    // Método para recibir el disparo
    public void RecibirDisparo()
    {
        if (angel != null)
        {
            angel.AlasPerdidas++;
            Debug.Log("------> Total perdidas: " + angel.AlasPerdidas);
            
            // Hacemos desaparecer el ojo
            gameObject.SetActive(false); 
        }
        else 
        {
            Debug.LogError("Al objeto " + gameObject.name + " le falta la referencia del Ángel.");
        }
    }
}