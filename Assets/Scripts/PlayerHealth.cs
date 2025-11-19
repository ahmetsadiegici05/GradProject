using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Trap"))
        {
            Die();
        }
    }

    private void Die()
    {
       
        Debug.Log("Player died!");

       
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
